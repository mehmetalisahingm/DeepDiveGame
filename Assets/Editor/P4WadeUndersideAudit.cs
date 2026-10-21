using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // World check (after #185/#194): what lies under the Beach_Wade / Shore_Ramp slabs, whether a diver who ends
    // up there can get out with plain player input, whether the 15 coast fish wander into coast geometry, and
    // whether the supplied home plant stands in the east side corridor. Same method as P4RouteAudit: the scene is
    // opened additively, the deterministic town layout and home rig are built on top, real physics queries and a
    // real CharacterController do the measuring, and the scene is closed unsaved.
    // Coordinates: Unity world space, metres, y up, +z north (sea), +x east.
    public static class P4WadeUndersideAudit
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        public const string OutputPath = "docs/reports/evidence/p4-wade/wade.json";

        private const float Dt = 0.02f;
        private const float Cell = 0.25f;
        private const float EscapeSeconds = 10f;
        private const float FishSimSeconds = 300f;

        // The two slopes the task names, plus the ledge they hang off: it is the same kind of overhang.
        public static readonly string[] Plates = { "Beach_Wade", "Shore_Ramp", "Shore_Ledge" };
        // DiveTestAreaShoreFillSetup's pieces under those three. Switched off to rebuild the pre-fill start set.
        public static readonly string[] Fills = { "Beach_Wade_Fill", "Beach_Wade_Toe", "Shore_Ramp_Fill", "Shore_Ramp_Toe", "Shore_Ledge_Fill" };
        // Measured for information only (not one of the task's slabs): the pier north of the ledge.
        public const string InfoPlate = "Dock_Town";
        // How close a fish body may come to scenery anywhere in its wander set.
        public const float FishClearance = 0.2f;
        public static readonly string[] CoastGeometry = { "Beach_Wade", "Shore_Ramp", "Shore_Ledge", "Dock_Town", "Beach_Platform" };

        // ProductDaySmokeDriver: where the solo product-day diver jammed, and the HoldForShore stand it aims for.
        public static readonly Vector3 ProductDayStuck = new Vector3(-7.6f, 3.3f, -5.0f);
        public static readonly Vector3 HoldForShoreStand = new Vector3(-7.6f, 5.5f, -5.0f);

        [Serializable] public sealed class Pocket
        {
            public string plate;
            public Vector3 plateMin, plateMax;
            public int columns, fittingColumns;
            public float fittingAreaM2, capsuleCentreVolumeM3;
            public Vector3 fitMin, fitMax;          // feet positions where the upright capsule fits under the slab
            public float minGap, maxGap;            // underside - floor, over fitting columns
            public float highestJammedHeadY;        // capsule top when pressed against the underside
            public bool headAlwaysUnderwater;
            public string[] floorsBelow;
        }

        [Serializable] public sealed class Escape
        {
            public string start, strategy, plate;
            public Vector3 from, inputWorld, end;
            public bool startFits, escaped, stuck;
            public float seconds;
            public string endEnvironment, endTouching;
        }

        [Serializable] public sealed class EscapeSummary
        {
            public string strategy;
            public int starts, closed, escaped, trapped;
            public float worstSeconds;
            public Vector3[] trappedFrom;
        }

        [Serializable] public sealed class FishHit { public string collider; public float envelopeFraction; }

        [Serializable] public sealed class Fish
        {
            public string name, species;
            public Vector3 home;
            public float wanderRadius, verticalRadius, bodyRadius, envelopeMinClearance;
            public FishHit[] envelopeHits;
            public int simFrames, simCentreInside, simBodyTouch, simStalledFrames;
            public string[] simCentreInsideColliders;
            public float simMaxPenetration, simLongestStallSeconds;
        }

        [Serializable] public sealed class Plant
        {
            public Vector3 position, boundsMin, boundsMax;
            public float corridorMinX, corridorMaxX, corridorMinZ, corridorMaxZ;
            public bool overlapsEastCorridor, hasCollider;
            public string[] overlapsFurniture;
        }

        [Serializable] public sealed class Audit
        {
            public string branchBase = "codex/p4-integration @ 6816fba";
            public string unity = Application.unityVersion;
            public string scene = ScenePath, prefab = DiverPath;
            public string coordinates = "Unity world, metres, y up, +z north (sea), +x east";
            public float radius, height, skin, swimSpeed, walkSpeed, surfaceY, deadband, dt = Dt, escapeSeconds = EscapeSeconds;
            public Pocket[] pockets;
            public Pocket infoPocket;
            public string[] fills;
            public int oldPocketStarts, oldPocketStartsNowSolid;
            public Escape[] escapes;
            public EscapeSummary[] escapeSummary;
            public Vector3 productDayStuck = ProductDayStuck, holdForShoreStand = HoldForShoreStand;
            public bool holdForShoreStandFits;
            public string holdForShoreStandBlocker;
            public float wadeSurfaceAtHoldForShore, wadeUndersideAtHoldForShore;
            public Fish[] fish;
            public bool fishMotionReadsColliders;
            public string fishMotionNote;
            public Plant plant;
            public string[] notMeasured;
        }

        private sealed class Ctx
        {
            public Scene Scene;
            public float Radius, Height, Skin, Step, Slope, SwimSpeed, WalkSpeed, SurfaceY;
            public Vector3 Center;
            public WaterField Field;
            public BoxCollider Swim;
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            var cc = prefab.GetComponent<CharacterController>();
            var player = new SerializedObject(prefab.GetComponent<NetworkPlayer>());
            var ctx = new Ctx
            {
                Radius = cc.radius, Height = cc.height, Skin = cc.skinWidth, Step = cc.stepOffset, Slope = cc.slopeLimit,
                Center = cc.center, SwimSpeed = player.FindProperty("swimSpeed").floatValue,
                WalkSpeed = player.FindProperty("walkSpeed").floatValue
            };

            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            ctx.Scene = scene;
            try
            {
                ContinuousTownLayout.Build(scene);
                HomeSceneSetup.EnsureRig(scene);
                Physics.SyncTransforms();
                // Fish and the special-event hull are NetworkObjects that move or appear at run time; the questions
                // here are about the authored scenery, so their colliders are switched off (the scene is not saved).
                foreach (var dynamicCollider in All<Collider>(scene).Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    dynamicCollider.enabled = false;
                Physics.SyncTransforms();
                ctx.Field = All<WaterField>(scene).Single();
                ctx.Swim = All<SwimVolume>(scene).Single().GetComponent<BoxCollider>();
                ctx.SurfaceY = ctx.Field.Bodies[0].SurfaceY;

                var audit = new Audit
                {
                    radius = ctx.Radius, height = ctx.Height, skin = ctx.Skin, swimSpeed = ctx.SwimSpeed, walkSpeed = ctx.WalkSpeed,
                    surfaceY = ctx.SurfaceY, deadband = ctx.Field.Deadband
                };
                var starts = new List<(string id, string plate, Vector3 feet)>
                {
                    ("product-day-stuck", "Beach_Wade", ProductDayStuck)
                };
                audit.fills = Fills.Where(n => Root(scene, n) != null).ToArray();
                audit.pockets = Plates.Select(p => MeasurePocket(ctx, p)).ToArray();
                audit.infoPocket = Root(scene, InfoPlate) != null ? MeasurePocket(ctx, InfoPlate) : null;
                // The pre-fill start set (1 m lattice under each slab, jammed and mid gap), rebuilt with the fills off
                // so before and after start from the same points.
                WithFillsOff(scene, () => { foreach (var p in Plates) LatticeStarts(ctx, p, starts); });
                audit.oldPocketStarts = starts.Count;
                audit.oldPocketStartsNowSolid = starts.Count(s => !Fits(ctx, s.feet, out _));
                foreach (var p in Plates) EdgeStarts(ctx, p, starts);
                audit.escapes = Escapes(ctx, starts).ToArray();
                audit.escapeSummary = audit.escapes.GroupBy(e => e.strategy).Select(g => new EscapeSummary
                {
                    strategy = g.Key, starts = g.Count(), closed = g.Count(e => !e.startFits), escaped = g.Count(e => e.escaped),
                    trapped = g.Count(e => e.startFits && !e.escaped),
                    worstSeconds = R(g.Where(e => e.escaped).Select(e => e.seconds).DefaultIfEmpty(0f).Max()),
                    trappedFrom = g.Where(e => e.startFits && !e.escaped).Select(e => e.from).ToArray()
                }).ToArray();

                audit.holdForShoreStandFits = Fits(ctx, HoldForShoreStand, out audit.holdForShoreStandBlocker);
                var wade = Root(scene, "Beach_Wade").GetComponent<Collider>();
                audit.wadeSurfaceAtHoldForShore = R(SurfaceDown(wade, HoldForShoreStand.x, HoldForShoreStand.z));
                audit.wadeUndersideAtHoldForShore = R(UndersideUp(wade, HoldForShoreStand.x, HoldForShoreStand.z));

                audit.fish = All<FishActor>(scene).OrderBy(f => f.name, StringComparer.Ordinal).Select(f => MeasureFish(ctx, f)).ToArray();
                audit.fishMotionReadsColliders = false;
                audit.fishMotionNote = "FishMotion.Step/PickWanderTarget only ask IWaterBounds.Contains (SwimVolumeBounds = the SwimVolume box); " +
                                       "no Physics query, so slabs/platform inside the box are not obstacles. Kept that way (no runtime physics); homes are " +
                                       "placed so the whole wander set (|offset| <= r, |offset.y| <= r/2, convex, contains home) clears scenery. " +
                                       "Flee (FleeSpeed, unbounded distance) is bounded by the same box only.";
                audit.plant = MeasurePlant(scene);
                audit.notMeasured = new[]
                {
                    "network latency / 1-2-4 process timing: the escape sim is the host FixedUpdate rule replayed in EditMode",
                    "CurrentDrift: the escape sim uses zero current (CrewEnvironmentRules drift adds to swim velocity in game)",
                    "camera pitch: 'up' is the jump/ascend axis only; a player pitching the camera changes the input mix",
                    "fish flee paths: only the wander envelope and a 300 s wander simulation per fish are measured",
                    "Dock_Town underside: measured as infoPocket only; no escape run and no fill (outside this task)",
                    "fish visuals: body radius is the conservative renderer half-extent, not the mesh hull"
                };

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P4_WADE_UNDERSIDE_AUDIT_WRITTEN " + OutputPath + " " + string.Join(" ",
                    audit.escapeSummary.Select(s => s.strategy + "=" + s.escaped + "/" + s.starts)));
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- a) the water under a slab -----------------------------------------------------------

        // Columns (0.25 m) over the slab's footprint where an upright diver fits with the slab, or one of its fills,
        // as the first thing above the head. Works the same before and after the fill.
        private static Pocket MeasurePocket(Ctx ctx, string plateName)
        {
            var plate = Root(ctx.Scene, plateName).GetComponent<Collider>();
            var b = plate.bounds;
            var pocket = new Pocket { plate = plateName, plateMin = R(b.min), plateMax = R(b.max), minGap = float.MaxValue, headAlwaysUnderwater = true };
            var fitMin = Vector3.positiveInfinity;
            var fitMax = Vector3.negativeInfinity;
            var floors = new HashSet<string>();
            for (var x = b.min.x + Cell * 0.5f; x < b.max.x; x += Cell)
            for (var z = b.min.z + Cell * 0.5f; z < b.max.z; z += Cell)
            {
                pocket.columns++;
                float? lowest = null, highest = null;
                var under = float.NaN;
                for (var y = ctx.Skin; y + ctx.Height < b.max.y; y += 0.1f)
                {
                    var feet = new Vector3(x, y, z);
                    if (!Fits(ctx, feet, out _)) continue;
                    if (!Physics.Raycast(feet + Vector3.up * ctx.Height, Vector3.up, out var ceiling, 20f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!IsSlabOrFill(ceiling.collider, plateName)) continue;
                    lowest ??= y;
                    highest = y;
                    under = ceiling.point.y;
                }
                if (!highest.HasValue) continue;
                if (Physics.Raycast(new Vector3(x, lowest.Value + 0.01f, z), Vector3.down, out var floor, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    floors.Add(floor.collider.name);
                    var gap = under - floor.point.y;
                    pocket.minGap = Mathf.Min(pocket.minGap, gap);
                    pocket.maxGap = Mathf.Max(pocket.maxGap, gap);
                }
                pocket.fittingColumns++;
                pocket.capsuleCentreVolumeM3 += (highest.Value - lowest.Value + 0.1f) * Cell * Cell;
                fitMin = Vector3.Min(fitMin, new Vector3(x, lowest.Value, z));
                fitMax = Vector3.Max(fitMax, new Vector3(x, highest.Value, z));
                var head = highest.Value + ctx.Height;
                pocket.highestJammedHeadY = Mathf.Max(pocket.highestJammedHeadY, head);
                if (ctx.SurfaceY - head < -ctx.Field.Deadband) pocket.headAlwaysUnderwater = false;
            }
            pocket.fittingAreaM2 = R(pocket.fittingColumns * Cell * Cell);
            pocket.capsuleCentreVolumeM3 = R(pocket.capsuleCentreVolumeM3);
            pocket.minGap = pocket.fittingColumns > 0 ? R(pocket.minGap) : 0f;
            pocket.maxGap = R(pocket.maxGap);
            pocket.highestJammedHeadY = R(pocket.highestJammedHeadY);
            pocket.fitMin = pocket.fittingColumns > 0 ? R(fitMin) : Vector3.zero;
            pocket.fitMax = pocket.fittingColumns > 0 ? R(fitMax) : Vector3.zero;
            pocket.floorsBelow = floors.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            return pocket;
        }

        private static bool IsSlabOrFill(Collider c, string plateName) =>
            c.name == plateName || c.name.StartsWith(plateName + "_", StringComparison.Ordinal);

        // Escape starts as the first audit took them: every 1 m lattice column whose first collider above the sea bed
        // is the slab, the capsule jammed against the underside and at mid gap. Called with the fills off.
        private static void LatticeStarts(Ctx ctx, string plateName, List<(string, string, Vector3)> starts)
        {
            var plate = Root(ctx.Scene, plateName).GetComponent<Collider>();
            var b = plate.bounds;
            for (var x = b.min.x + Cell * 0.5f; x < b.max.x; x += 1f)
            for (var z = b.min.z + Cell * 0.5f; z < b.max.z; z += 1f)
            {
                var under = UndersideUp(plate, x, z);
                if (float.IsNaN(under)) continue;
                if (!Physics.Raycast(new Vector3(x, under - 0.01f, z), Vector3.down, out var floor, 20f, ~0, QueryTriggerInteraction.Ignore)) continue;
                float? lowest = null, highest = null;
                for (var y = floor.point.y + ctx.Skin; y + ctx.Height <= under + 1e-4f; y += 0.1f)
                {
                    if (!Fits(ctx, new Vector3(x, y, z), out _)) continue;
                    lowest ??= y;
                    highest = y;
                }
                if (!highest.HasValue) continue;
                starts.Add((plateName + "-jammed", plateName, new Vector3(x, highest.Value, z)));
                starts.Add((plateName + "-mid", plateName, new Vector3(x, R((lowest.Value + highest.Value) * 0.5f), z)));
            }
        }

        // Free, underwater, open-sky water 0.5 and 1.5 m outside the slab footprint, every 1/8 of an edge, feet at
        // y 0.5 / 2.5 / 4.5: where a diver who swam along the closed slab ends up.
        private static void EdgeStarts(Ctx ctx, string plateName, List<(string, string, Vector3)> starts)
        {
            var b = Root(ctx.Scene, plateName).GetComponent<Collider>().bounds;
            for (var d = 0.5f; d <= 1.5f; d += 1f)
            for (var t = 0f; t <= 1f; t += 0.125f)
            foreach (var y in new[] { 0.5f, 2.5f, 4.5f })
            foreach (var feet in new[]
                     {
                         new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), y, b.max.z + d),
                         new Vector3(b.min.x - d, y, Mathf.Lerp(b.min.z, b.max.z, t)),
                         new Vector3(b.max.x + d, y, Mathf.Lerp(b.min.z, b.max.z, t))
                     })
            {
                if (feet.y + ctx.Height > ctx.SurfaceY - 0.5f || !Fits(ctx, feet, out _)) continue;
                if (Physics.Raycast(feet + Vector3.up * ctx.Height, Vector3.up, ctx.SurfaceY + 1f - feet.y, ~0, QueryTriggerInteraction.Ignore)) continue;
                starts.Add((plateName + "-edge", plateName, R(feet)));
            }
        }

        private static void WithFillsOff(Scene scene, Action body)
        {
            var fills = All<Collider>(scene).Where(c => Fills.Contains(c.name) && c.enabled).ToArray();
            try
            {
                foreach (var c in fills) c.enabled = false;
                Physics.SyncTransforms();
                body();
            }
            finally
            {
                foreach (var c in fills) c.enabled = true;
                Physics.SyncTransforms();
            }
        }

        // The slab's underside straight above the sea bed at (x, z): the first collider hit going up from just
        // over the floor, when that collider is the slab. NaN when the column is closed or the slab is not first.
        private static float UndersideUp(Collider plate, float x, float z)
        {
            foreach (var y0 in new[] { 0.05f, 1f, 2f, 3f, 4f, 5f })
            {
                var origin = new Vector3(x, y0, z);
                if (Physics.CheckSphere(origin, 0.01f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!Physics.Raycast(origin, Vector3.up, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) return float.NaN;
                return hit.collider == plate ? hit.point.y : float.NaN;
            }
            return float.NaN;
        }

        private static float SurfaceDown(Collider plate, float x, float z) =>
            plate.Raycast(new Ray(new Vector3(x, plate.bounds.max.y + 1f, z), Vector3.down), out var hit, 30f) ? hit.point.y : float.NaN;

        private static bool Fits(Ctx ctx, Vector3 feet, out string blocker)
        {
            var a = feet + Vector3.up * ctx.Radius;
            var c = feet + Vector3.up * (ctx.Height - ctx.Radius);
            var hits = Physics.OverlapCapsule(a, c, ctx.Radius, ~0, QueryTriggerInteraction.Ignore);
            blocker = hits.Length > 0 ? string.Join(",", hits.Select(h => h.name).Distinct()) : null;
            return hits.Length == 0;
        }

        // ---- b) can a trapped diver get out? -----------------------------------------------------

        private static IEnumerable<Escape> Escapes(Ctx ctx, List<(string id, string plate, Vector3 feet)> starts)
        {
            foreach (var (id, plateName, feet) in starts)
            {
                var plate = Root(ctx.Scene, plateName).GetComponent<Collider>();
                var open = NearestOpenWater(ctx, plate, feet);
                var shore = ShoreDirection(plate);
                foreach (var (strategy, input) in new[]
                         {
                             ("up", Vector3.up),
                             ("up+nearest-open-water", Vector3.ClampMagnitude(open + Vector3.up, 1f)),
                             ("up+toward-shore", Vector3.ClampMagnitude(shore + Vector3.up, 1f))
                         })
                    yield return Simulate(ctx, id, plateName, strategy, feet, input);
            }
        }

        // The horizontal direction (8 headings) in which an upright diver at `feet` first reaches a column whose
        // way up to the surface is clear. That is "the nearest open water" a player would swim toward.
        private static Vector3 NearestOpenWater(Ctx ctx, Collider plate, Vector3 feet)
        {
            var best = Vector3.forward;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < 8; i++)
            {
                var d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                for (var s = 0.1f; s < 15f && s < bestDistance; s += 0.1f)
                {
                    var q = feet + d * s;
                    if (!Fits(ctx, q, out _)) break;
                    if (Physics.Raycast(q + Vector3.up * ctx.Height * 0.5f, Vector3.up, ctx.SurfaceY + 1f - q.y, ~0, QueryTriggerInteraction.Ignore)) continue;
                    bestDistance = s;
                    best = d;
                    break;
                }
            }
            return best;
        }

        // Toward the slab's dry top end: what a player who "swims back to the beach" holds.
        private static Vector3 ShoreDirection(Collider plate)
        {
            if (plate.name == "Beach_Wade") return Vector3.back;   // top at z = -11, foot north
            if (plate.name == "Shore_Ramp") return Vector3.right;  // top at the ledge, x = 7
            return Vector3.back;                                    // Shore_Ledge: the beach strip is south
        }

        private static Escape Simulate(Ctx ctx, string id, string plate, string strategy, Vector3 feet, Vector3 input)
        {
            var e = new Escape { start = id, plate = plate, strategy = strategy, from = R(feet), inputWorld = R(input) };
            e.startFits = Fits(ctx, feet, out var blocker);
            if (!e.startFits)
            {
                // Solid now: nobody can be here any more, so there is nothing to escape from.
                e.end = e.from;
                e.endEnvironment = "solid";
                e.endTouching = blocker;
                return e;
            }
            var go = new GameObject("Audit_P4Wade_Diver");
            SceneManager.MoveGameObjectToScene(go, ctx.Scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = ctx.Height; cc.radius = ctx.Radius; cc.center = ctx.Center;
                cc.slopeLimit = ctx.Slope; cc.stepOffset = ctx.Step; cc.skinWidth = ctx.Skin;
                go.transform.position = feet;
                Physics.SyncTransforms();
                var tracker = ctx.Field.CreateTracker();
                var env = EnvironmentLocomotion.Underwater;
                var lastMoveAt = 0f;
                var lastPos = feet;
                var frames = Mathf.RoundToInt(EscapeSeconds / Dt);
                for (var f = 0; f < frames; f++)
                {
                    var t = go.transform;
                    env = tracker.Classify(new WaterProbe(t.position, t.TransformVector(cc.center), ctx.Radius, ctx.Height, Vector3.up));
                    if (env != EnvironmentLocomotion.Underwater)
                    {
                        e.escaped = true;
                        e.seconds = R(f * Dt);
                        break;
                    }
                    // NetworkPlayer.FixedUpdate + PlayerPresentationRules.FilterMoveInput for an Underwater diver.
                    var move = Vector3.ClampMagnitude(input, 1f);
                    var velocity = move * ctx.SwimSpeed;
                    cc.Move(velocity * Dt);
                    if (Vector3.Distance(t.position, lastPos) > 0.01f) { lastPos = t.position; lastMoveAt = f * Dt; }
                }
                if (!e.escaped) { e.seconds = EscapeSeconds; e.stuck = EscapeSeconds - lastMoveAt > 1f; }
                e.end = R(go.transform.position);
                e.endEnvironment = env.ToString();
                var touching = Physics.OverlapCapsule(go.transform.position + Vector3.up * ctx.Radius,
                    go.transform.position + Vector3.up * (ctx.Height - ctx.Radius), ctx.Radius + ctx.Skin + 0.02f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c != cc).Select(c => c.name).Distinct().ToArray();
                e.endTouching = string.Join(",", touching);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            return e;
        }

        // ---- c) the coast fish -------------------------------------------------------------------

        private static Fish MeasureFish(Ctx ctx, FishActor actor)
        {
            var tuning = actor.Species.Swim;
            var home = actor.transform.position;
            var renderers = actor.GetComponentsInChildren<Renderer>(true);
            var body = renderers.Length == 0 ? 0.3f : renderers.Max(r => Mathf.Max(r.bounds.extents.x, r.bounds.extents.y, r.bounds.extents.z));
            var fish = new Fish
            {
                name = actor.name, species = actor.Species.SpeciesId, home = R(home), wanderRadius = tuning.WanderRadius,
                verticalRadius = tuning.WanderRadius * 0.5f, bodyRadius = R(body)
            };

            // The wander envelope: every candidate PickWanderTarget can accept. RandomOffset keeps |offset| <= 1 and
            // |offset.y| <= 0.5 (a sphere clipped to a slab, not an ellipsoid), times WanderRadius, inside the water box.
            var counts = new Dictionary<string, int>();
            var total = 0;
            var r = tuning.WanderRadius;
            var clearance = 2f;
            for (var x = -r; x <= r + 1e-3f; x += Cell)
            for (var y = -r * 0.5f; y <= r * 0.5f + 1e-3f; y += Cell)
            for (var z = -r; z <= r + 1e-3f; z += Cell)
            {
                if (x * x + y * y + z * z > r * r + 1e-3f) continue;
                var p = home + new Vector3(x, y, z);
                if (!InSwimBox(ctx, p)) continue;
                total++;
                if (Physics.CheckSphere(p, clearance, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Bisect the free radius (ClosestPoint does not work on SeaFloor's non-convex mesh).
                    float lo = 0f, hi = clearance;
                    for (var i = 0; i < 8; i++)
                    {
                        var mid = (lo + hi) * 0.5f;
                        if (Physics.CheckSphere(p, mid, ~0, QueryTriggerInteraction.Ignore)) hi = mid; else lo = mid;
                    }
                    clearance = lo;
                }
                foreach (var name in Physics.OverlapSphere(p, FishClearance, ~0, QueryTriggerInteraction.Ignore)
                             .Select(c => c.name).Distinct())
                    counts[name] = counts.TryGetValue(name, out var n) ? n + 1 : 1;
            }
            fish.envelopeMinClearance = R(clearance);
            fish.envelopeHits = counts.OrderByDescending(k => k.Value)
                .Select(k => new FishHit { collider = k.Key, envelopeFraction = R(total == 0 ? 0f : (float)k.Value / total) }).ToArray();

            // The real wander rule, no threats, fixed seed: how often the body is inside / touching geometry.
            var water = new BoxWater(ctx);
            var motion = new FishMotion(tuning, home, new System.Random(StableSeed(actor.name)));
            var pos = home;
            var inside = new HashSet<string>();
            var frames = Mathf.RoundToInt(FishSimSeconds / Dt);
            var streak = 0;
            for (var f = 0; f < frames; f++)
            {
                var next = motion.Step(pos, Array.Empty<Vector3>(), Dt, water);
                streak = next == pos ? streak + 1 : 0;
                if (streak > 0) fish.simStalledFrames++;
                fish.simLongestStallSeconds = Mathf.Max(fish.simLongestStallSeconds, R(streak * Dt));
                pos = next;
                fish.simFrames++;
                var centre = Physics.OverlapSphere(pos, 0.01f, ~0, QueryTriggerInteraction.Ignore);
                if (centre.Length > 0)
                {
                    fish.simCentreInside++;
                    foreach (var c in centre) inside.Add(c.name);
                    foreach (var c in centre) fish.simMaxPenetration = Mathf.Max(fish.simMaxPenetration, BoxDepth(c, pos));
                }
                if (Physics.CheckSphere(pos, body, ~0, QueryTriggerInteraction.Ignore))
                    fish.simBodyTouch++;
            }
            fish.simCentreInsideColliders = inside.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            fish.simMaxPenetration = R(fish.simMaxPenetration);
            return fish;
        }

        // FNV-1a over the name: the same seed in every process (string.GetHashCode is not guaranteed to be).
        private static int StableSeed(string name)
        {
            var h = 2166136261u;
            foreach (var ch in name) h = (h ^ ch) * 16777619u;
            return (int)(h & 0x7fffffff);
        }

        // How far inside a box collider a point is (distance to its nearest face); 0 for other shapes.
        private static float BoxDepth(Collider c, Vector3 p)
        {
            if (!(c is BoxCollider box)) return 0f;
            var local = Vector3.Scale(box.transform.InverseTransformPoint(p) - box.center, box.transform.lossyScale);
            var half = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
            return Mathf.Max(0f, Mathf.Min(half.x - Mathf.Abs(local.x), half.y - Mathf.Abs(local.y), half.z - Mathf.Abs(local.z)));
        }

        // SwimVolumeBounds as the host runs it: the water box and nothing else.
        private sealed class BoxWater : IWaterBounds
        {
            private readonly Ctx ctx;
            public BoxWater(Ctx ctx) => this.ctx = ctx;
            public bool Contains(Vector3 point) => InSwimBox(ctx, point);
        }

        // SwimVolume.Contains, which is empty in EditMode (its registry fills in OnEnable at play time).
        private static bool InSwimBox(Ctx ctx, Vector3 p)
        {
            var local = ctx.Swim.transform.InverseTransformPoint(p) - ctx.Swim.center;
            var half = ctx.Swim.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        // ---- the home plant ----------------------------------------------------------------------

        private static Plant MeasurePlant(Scene scene)
        {
            var plant = All<Transform>(scene).FirstOrDefault(t => t.name == "Supplied_Plant");
            var result = new Plant();
            var bedRightMax = ContinuousTownLayout.HomeRig.Bed(3).x + ContinuousTownLayout.HomeRig.BedSize.x * 0.5f;
            var wallInner = ContinuousTownLayout.FindBlock("HomeRight").Min.x;
            var pcStandZ = ContinuousTownLayout.HomeRig.Pc.z + ContinuousTownLayout.HomeRig.PcSize.z * 0.5f + 0.8f;
            result.corridorMinX = R(bedRightMax);
            result.corridorMaxX = R(wallInner);
            result.corridorMinZ = R(pcStandZ - 0.43f);
            result.corridorMaxZ = R(ContinuousTownLayout.HomeRig.Door.z);
            if (plant == null) return result;
            var renderers = plant.GetComponentsInChildren<Renderer>(true);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            result.position = R(plant.position);
            result.boundsMin = R(b.min);
            result.boundsMax = R(b.max);
            result.hasCollider = plant.GetComponentsInChildren<Collider>(true).Length > 0;
            result.overlapsEastCorridor = b.max.x > result.corridorMinX && b.min.x < result.corridorMaxX &&
                                          b.max.z > result.corridorMinZ && b.min.z < result.corridorMaxZ;
            // Lifted 5 cm so the floor slab the pot stands on is not reported.
            result.overlapsFurniture = Physics.OverlapBox(b.center + Vector3.up * 0.05f, b.extents - Vector3.up * 0.05f, Quaternion.identity, ~0,
                    QueryTriggerInteraction.Ignore)
                .Where(c => !c.transform.IsChildOf(plant))
                .Select(c => c.name).Distinct().ToArray();
            return result;
        }

        // ---- helpers -----------------------------------------------------------------------------

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static GameObject Root(Scene scene, string name) =>
            All<Transform>(scene).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static float R(float v) => Mathf.Round(v * 1000f) / 1000f;
        private static Vector3 R(Vector3 v) => new Vector3(R(v.x), R(v.y), R(v.z));
    }
}
