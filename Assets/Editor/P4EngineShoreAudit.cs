using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using DeepDive.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #188/#156 (Mehmet's #186 decision): the free engine part leaves the wade slope (under water) for dry shore. This picks the
    // spot by measurement, the #172/#188 way: DiveTestArea is opened additively, the town (layout, home rig, harbor marks,
    // storefront counters) is built on top, the proposed S1 cradle stands in as a solid box, every candidate cell of the coast is
    // filtered (dry walk ground, no collider, clear of the cradle/work/drop points and the walking corridors, apart from the other
    // two parts), and the survivors are scored on sight lines; the best few are then walked to with a real CharacterController
    // from the home door, the beach gate and the wade top, and picked up with the real 2.5 m eye ray. The engine object is moved
    // onto each candidate while it is measured and the scene is closed unsaved: no scene, layout or anchor change happens here.
    // Coordinates: Unity world space, metres, y up, +z toward the sea (north), +x east.
    public static class P4EngineShoreAudit
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        public const string RoutesPath = "docs/reports/evidence/p172-route/current-routes.json";
        public const string OutputPath = "docs/reports/evidence/p4-engine-shore/engine-shore.json";

        private const float Dt = 0.02f;
        private const float Cell = 0.25f;
        private const float DryY = 8.01f;
        private const float PickupRange = 2.5f;
        private const float PartHalf = 0.3f;
        private const float MinPartSpacing = 6f;
        private const float CorridorClearance = 1.0f;   // part half-diagonal 0.42 + padded capsule 0.43, rounded up
        private const float WorkClearance = 1.06f;      // #188 worker spacing
        private const float DropClearance = 1.31f;      // #188 drop-to-corridor clearance
        private const float WaitBand = 4f;              // #188 waiting band: 1.5..4 m from the cradle edge

        // #188 S1 proposal (Mehmet accepted S1 in #186): cradle 2.4 x 5 m at (3.6, 8.4, -12.6), yaw 90; four work points; one drop.
        public static readonly Vector3 CradleCenter = new Vector3(3.6f, 8.4f, -12.6f);
        public static readonly Vector3 CradleSize = new Vector3(5f, 1f, 2.4f);   // yaw 90: the 5 m length runs along x
        public static readonly Vector2[] WorkPoints = { new Vector2(0.5f, -11.5f), new Vector2(1.0f, -14.25f), new Vector2(6.25f, -14.25f), new Vector2(6.5f, -11.0f) };
        public static readonly Vector2 DropPoint = new Vector2(7.5f, -14.5f);

        private static readonly string[] SceneWalkSurfaces = { "Beach_Platform", "Beach_Wade", "Shore_Ledge", "Shore_Ramp", "Dock_Town" };

        [Serializable] public sealed class Rect2 { public string name; public Vector2 min, max; }
        [Serializable] public sealed class Segment { public string name; public Vector2 a, b; }

        [Serializable] public sealed class Sight
        {
            public string from;
            public Vector3 eye;
            public float headingYaw, distance, horizontalAngle, projectedHeightPx1080;
            public bool lineOfSight, inViewAtHeading;
            public string blocker;
        }

        [Serializable] public sealed class Walk
        {
            public string from;
            public Vector2[] waypoints;
            public float drivenLength, simSeconds;
            public bool reached, fell, stuck, enteredSea;
            public Vector2 endPosition;
            public string[] failures;
        }

        [Serializable] public sealed class Candidate
        {
            public string id;
            public Vector3 position;
            public string ground, zone;
            public bool inMap;
            public float toHull, toFuelTank, balance, corridorMargin, toCradle, softMargin;
            public string nearestCorridor, nearestSoft;
            public Sight[] sights;
            public int visibleInView;
            public float score;
            public Vector2 stand;
            public float pickupDistance;
            public bool pickupHitsPart;
            public string pickupBlocker;
            public Walk[] walks;
            public bool walkable;
            public string[] hints;
        }

        [Serializable] public sealed class Rejections { public int cells, noDryGround, notCoast, insideCollider, exitZone, cradleOrPoints, corridor, nearPart, noStand; }

        [Serializable] public sealed class Audit
        {
            public string branchBase = "codex/p4-integration @ 6816fba";
            public string unity = Application.unityVersion;
            public string scene = ScenePath, routes = RoutesPath;
            public string coordinates = "Unity world, metres, y up, +z north (sea), +x east";
            public float radius, height, skin, stepOffset, slopeLimit, walkSpeed, eyeHeight, verticalFov, horizontalFov16x9;
            public Vector3 engineBefore, hull, fuelTank;
            public string engineBeforeZone;
            public bool engineBeforeInSwimVolume;
            public Vector3 cradleCenter = CradleCenter, cradleSize = CradleSize;
            public Vector2[] workPoints = WorkPoints;
            public Vector2 dropPoint = DropPoint;
            public Rect2[] corridorRects;
            public Segment[] corridorSegments, softSegments;
            public Rejections rejections = new Rejections();
            public int survivors;
            public Candidate[] ranked;
            public string chosen;
            public string[] alternatives;
            public string[] rules, notMeasured;
        }

        private sealed class Ctx
        {
            public Scene Scene;
            public IPlayerZoneSource Zones;
            public DiveRegionField Region;
            public Collider Swim, Exit;
            public Transform Engine;
            public Collider EngineCollider;
            public float Radius, Height, Skin, Step, Slope, Speed, Eye, Fov, HFov;
            public HashSet<string> Walk;
            public Rect2[] Rects;
            public Segment[] Segments, Soft;
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            var cc = prefab.GetComponent<CharacterController>();
            var cam = prefab.GetComponentInChildren<Camera>(true);
            var ctx = new Ctx
            {
                Radius = cc.radius, Height = cc.height, Skin = cc.skinWidth, Step = cc.stepOffset, Slope = cc.slopeLimit,
                Speed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue,
                Eye = prefab.transform.InverseTransformPoint(cam.transform.position).y, Fov = cam.fieldOfView,
                Walk = new HashSet<string>(SceneWalkSurfaces.Concat(ContinuousTownLayout.Slabs.Select(b => b.Name))
                    .Concat(ContinuousTownLayout.Fills.Select(b => b.Name)))
            };
            ctx.HFov = 2f * Mathf.Atan(Mathf.Tan(ctx.Fov * 0.5f * Mathf.Deg2Rad) * 16f / 9f) * Mathf.Rad2Deg;
            var audit = new Audit
            {
                radius = ctx.Radius, height = ctx.Height, skin = ctx.Skin, stepOffset = ctx.Step, slopeLimit = ctx.Slope,
                walkSpeed = ctx.Speed, eyeHeight = ctx.Eye, verticalFov = ctx.Fov, horizontalFov16x9 = R(ctx.HFov)
            };

            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            ctx.Scene = scene;
            GameObject cradle = null;
            try
            {
                ContinuousTownLayout.Build(scene);
                HomeSceneSetup.EnsureRig(scene);
                CoastalHarborLink.BuildMarkers(scene);
                foreach (var service in All<ServicePointAnchor>(scene)) TownStorefront.Build(service);
                ctx.Zones = All<WorldZoneField>(scene).Single();
                ctx.Region = All<DiveRegionField>(scene).Single();
                ctx.Swim = All<SwimVolume>(scene).Single().GetComponent<Collider>();
                ctx.Exit = All<Collider>(scene).Single(c => c.name == "DiveExit" && c.isTrigger);
                var parts = All<BoatPartAnchor>(scene).ToDictionary(p => p.PartId);
                ctx.Engine = parts[BoatRepairParts.Engine].transform;
                ctx.EngineCollider = ctx.Engine.GetComponent<Collider>();
                audit.engineBefore = R(ctx.Engine.position);
                audit.engineBeforeZone = ctx.Zones.ResolveZone(ctx.Engine.position).ToString();
                audit.engineBeforeInSwimVolume = ctx.Swim.bounds.Contains(ctx.Engine.position) &&
                                                 (ctx.Swim.ClosestPoint(ctx.Engine.position) - ctx.Engine.position).sqrMagnitude < 1e-6f;
                audit.hull = R(parts[BoatRepairParts.Hull].WorldPosition);
                audit.fuelTank = R(parts[BoatRepairParts.FuelTank].WorldPosition);

                // The S1 cradle does not exist yet; a solid stand-in keeps every walk and sight line honest about it.
                cradle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cradle.name = "Audit_S1_Cradle";
                SceneManager.MoveGameObjectToScene(cradle, scene);
                cradle.transform.SetPositionAndRotation(CradleCenter + Vector3.up * CradleSize.y * 0.5f, Quaternion.identity);
                cradle.transform.localScale = CradleSize;
                Physics.SyncTransforms();

                BuildCorridors(ctx, audit);
                audit.corridorRects = ctx.Rects;
                audit.corridorSegments = ctx.Segments;
                audit.softSegments = ctx.Soft;

                var survivors = Filter(ctx, audit, XZ(audit.hull), XZ(audit.fuelTank));
                audit.survivors = survivors.Count;
                foreach (var c in survivors) Score(ctx, c);
                var ordered = survivors.OrderByDescending(c => c.score).ThenBy(c => c.id, StringComparer.Ordinal).ToList();

                // Real walks for the best spread-out candidates (>= 1.5 m apart), so the table compares places, not neighbouring cells.
                var spread = new List<Candidate>();
                foreach (var c in ordered)
                {
                    if (spread.Count >= 12) break;
                    if (spread.Any(s => Vector2.Distance(XZ(s.position), XZ(c.position)) < 1.5f)) continue;
                    spread.Add(c);
                }
                foreach (var c in spread) Prove(ctx, c);
                audit.ranked = spread.ToArray();

                var proven = spread.Where(c => c.walkable && c.pickupHitsPart).ToList();
                audit.chosen = proven.Select(c => c.id).FirstOrDefault();
                var picked = new List<Candidate>();
                foreach (var c in proven)
                {
                    if (picked.Count >= 3) break;
                    if (picked.Any(p => Vector2.Distance(XZ(p.position), XZ(c.position)) < 1.5f)) continue;
                    picked.Add(c);
                }
                audit.alternatives = picked.Skip(1).Select(c => c.id).ToArray();

                audit.rules = new[]
                {
                    "cell: 0.25 m grid over x -14.5..14.5, z -20..-11; the part sits on the first collider straight down",
                    "dry: that collider is a walk surface, ground y >= 8.01, the point is outside the SwimVolume and the zone is Coast",
                    "free: a 0.3 m sphere 0.45 m above the ground touches nothing but the ground (BoatPartReachabilityTests rule); not in DiveExit",
                    "clear (hard): >= 1.0 m from pier, Shore_Ledge/DiveExit pad, Shore_Ramp, Beach_Wade, the wade and ledge landings, the gate line, the S1 cradle and the other parts' carry lines; >= 1.06 m from a work point, >= 1.31 m from the drop",
                    "clear (soft): #172 route lines - the part is a trigger and blocks no walk; < 1.0 m costs 1 point",
                    "apart: >= 6 m from the hull and the fuel tank parts",
                    "stand: a capsule-free dry cell 0.75..1.5 m from the part, whose 2.5 m eye ray hits the part's own trigger first",
                    "score: beach entry 3 and town square 3 (line of sight inside the 16:9 frame), beach gate 2 (line of sight), home door 1, wade top 0.5, + min(toHull, toFuelTank)/4, + min(corridorMargin, 3)/4, - 1 if soft margin < 1.0, - 0.5 per metre inside the S1 waiting band (< 4 m from the cradle)",
                    "walks: real CharacterController (prefab radius/height/skin/step/slope, walkSpeed, gravity, dt 0.02) along a 0.25 m grid path"
                };
                audit.notMeasured = new[]
                {
                    "fog, light, night and contrast: sight lines are geometric only",
                    "collider-less DevelopmentVisuals / supplied town art props (they cannot block a walk; they can block a view)",
                    "real 1/2/4-process runs and network latency",
                    "PR #190 rack/slot offsets (not on this branch)"
                };

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log($"P4_ENGINE_SHORE_AUDIT_WRITTEN {OutputPath} survivors={audit.survivors} chosen={audit.chosen}");
            }
            finally
            {
                if (cradle != null) Object.DestroyImmediate(cradle);
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- corridors ---------------------------------------------------------------------------

        [Serializable] private sealed class RouteFile { public RouteEntry[] routes; }
        [Serializable] private sealed class RouteEntry { public string id; public Vector2[] waypoints; }

        private static void BuildCorridors(Ctx ctx, Audit audit)
        {
            Rect2 Box(string name, Bounds b) => new Rect2 { name = name, min = new Vector2(R(b.min.x), R(b.min.z)), max = new Vector2(R(b.max.x), R(b.max.z)) };
            Bounds Of(string name) => All<Collider>(ctx.Scene).First(c => c.name == name && !c.isTrigger).bounds;
            var wade = Of("Beach_Wade");
            var ledge = Of("Shore_Ledge");
            var rects = new List<Rect2>
            {
                Box("Dock_Town (pier)", Of("Dock_Town")),
                Box("Shore_Ledge (DiveExit pad)", ledge),
                Box("Shore_Ramp", Of("Shore_Ramp")),
                Box("Beach_Wade", wade),
                // Where a swimmer steps off the wade onto the sand, and where the ledge meets the beach (#188 corridors).
                new Rect2 { name = "wade landing", min = new Vector2(R(wade.min.x), -12.5f), max = new Vector2(R(wade.max.x), -11f) },
                new Rect2 { name = "ledge landing", min = new Vector2(R(ledge.min.x), -13.5f), max = new Vector2(R(ledge.max.x), -11f) },
            };
            ctx.Rects = rects.ToArray();

            var soft = new List<Segment>();
            var file = JsonUtility.FromJson<RouteFile>(File.ReadAllText(RoutesPath));
            foreach (var route in file.routes)
                for (var i = 1; i < route.waypoints.Length; i++)
                {
                    var a = route.waypoints[i - 1]; var b = route.waypoints[i];
                    if (a.y < -21f && b.y < -21f) continue;   // town legs far from the coast
                    soft.Add(new Segment { name = "#172 route " + route.id, a = a, b = b });
                }
            var gate = XZ(Root(ctx.Scene, DiveTestAreaBeachSetup.TownGateName).transform.position);
            ctx.Soft = soft.ToArray();
            var segments = new List<Segment>();
            // #188 "Kapı (cadde -> plaj)" corridor: where the street comes down onto the sand.
            segments.Add(new Segment { name = "gate line (street -> beach)", a = new Vector2(gate.x, -16f), b = new Vector2(gate.x, -11f) });
            // #188 carry lines of the other two parts to S1 (part -> first work point / drop).
            segments.Add(new Segment { name = "hull carry -> repair-work-0", a = XZ(audit.hull), b = WorkPoints[0] });
            segments.Add(new Segment { name = "hull carry -> repair-work-1", a = XZ(audit.hull), b = WorkPoints[1] });
            segments.Add(new Segment { name = "fuel-tank carry -> repair-work-3", a = XZ(audit.fuelTank), b = WorkPoints[3] });
            segments.Add(new Segment { name = "fuel-tank carry -> repair-drop-0", a = XZ(audit.fuelTank), b = DropPoint });
            segments.Add(new Segment { name = "repair-drop-0 -> dock-town-1", a = DropPoint, b = new Vector2(9f, -12.5f) });
            ctx.Segments = segments.ToArray();
        }

        private static float RectDistance(Rect2 r, Vector2 p)
        {
            var dx = Mathf.Max(r.min.x - p.x, 0f, p.x - r.max.x);
            var dz = Mathf.Max(r.min.y - p.y, 0f, p.y - r.max.y);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float SegmentDistance(Segment s, Vector2 p)
        {
            var ab = s.b - s.a;
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, s.a + ab * t);
        }

        private static float CorridorMargin(Ctx ctx, Vector2 p, out string nearest)
        {
            var best = float.MaxValue; nearest = null;
            foreach (var r in ctx.Rects) { var d = RectDistance(r, p); if (d < best) { best = d; nearest = r.name; } }
            foreach (var s in ctx.Segments) { var d = SegmentDistance(s, p); if (d < best) { best = d; nearest = s.name; } }
            return best;
        }

        private static float SoftMargin(Ctx ctx, Vector2 p, out string nearest)
        {
            var best = float.MaxValue; nearest = null;
            foreach (var s in ctx.Soft) { var d = SegmentDistance(s, p); if (d < best) { best = d; nearest = s.name; } }
            return best;
        }

        private static float CradleDistance(Vector2 p) => RectDistance(new Rect2
        {
            min = new Vector2(CradleCenter.x - CradleSize.x * 0.5f, CradleCenter.z - CradleSize.z * 0.5f),
            max = new Vector2(CradleCenter.x + CradleSize.x * 0.5f, CradleCenter.z + CradleSize.z * 0.5f)
        }, p);

        // ---- filter ------------------------------------------------------------------------------

        private static List<Candidate> Filter(Ctx ctx, Audit audit, Vector2 hull, Vector2 fuel)
        {
            var list = new List<Candidate>();
            var rej = audit.rejections;
            for (var x = -14.5f; x <= 14.5f + 1e-4f; x += Cell)
            for (var z = -20f; z <= -11f + 1e-4f; z += Cell)
            {
                rej.cells++;
                var p = new Vector2(R(x), R(z));
                if (!Ground(ctx, p, out var y, out var groundName) || y < DryY) { rej.noDryGround++; continue; }
                var at = new Vector3(p.x, y, p.y);
                if (InSwim(ctx, at) || ctx.Zones.ResolveZone(at) != PlayerZone.Coast) { rej.notCoast++; continue; }
                if (Physics.OverlapSphere(at + Vector3.up * 0.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore).Length > 0) { rej.insideCollider++; continue; }
                if (ctx.Exit.bounds.Contains(at + Vector3.up * 0.9f)) { rej.exitZone++; continue; }
                if (CradleDistance(p) < CorridorClearance || WorkPoints.Any(w => Vector2.Distance(w, p) < WorkClearance) ||
                    Vector2.Distance(DropPoint, p) < DropClearance) { rej.cradleOrPoints++; continue; }
                var margin = CorridorMargin(ctx, p, out var nearest);
                if (margin < CorridorClearance) { rej.corridor++; continue; }
                var toHull = Vector2.Distance(p, hull); var toFuel = Vector2.Distance(p, fuel);
                if (toHull < MinPartSpacing || toFuel < MinPartSpacing) { rej.nearPart++; continue; }
                list.Add(new Candidate
                {
                    id = string.Format(CultureInfo.InvariantCulture, "c({0:0.##}, {1:0.##})", p.x, p.y),
                    position = R(at), ground = groundName, zone = PlayerZone.Coast.ToString(),
                    inMap = ctx.Region.TryWorldToMap(at, out _),
                    toHull = R(toHull), toFuelTank = R(toFuel), balance = R(Mathf.Min(toHull, toFuel)),
                    corridorMargin = R(margin), nearestCorridor = nearest, toCradle = R(CradleDistance(p)),
                    softMargin = R(SoftMargin(ctx, p, out var soft)), nearestSoft = soft
                });
            }
            // A part nobody can stand next to is not a candidate either.
            var kept = new List<Candidate>();
            foreach (var c in list)
            {
                if (!Stand(ctx, c)) { rej.noStand++; continue; }
                kept.Add(c);
            }
            return kept;
        }

        private static bool InSwim(Ctx ctx, Vector3 p) => (ctx.Swim.ClosestPoint(p) - p).sqrMagnitude < 1e-6f;

        // The nearest dry, capsule-free cell 0.75..1.5 m away whose eye ray reaches the part first; the gate side wins ties.
        private static bool Stand(Ctx ctx, Candidate c)
        {
            MoveEngine(ctx, c.position);
            var part = XZ(c.position);
            var gate = XZ(Root(ctx.Scene, DiveTestAreaBeachSetup.TownGateName).transform.position);
            var options = new List<(Vector2 p, float d, float g, bool hit, string blocker)>();
            for (var dx = -1.5f; dx <= 1.5f + 1e-4f; dx += Cell)
            for (var dz = -1.5f; dz <= 1.5f + 1e-4f; dz += Cell)
            {
                var q = part + new Vector2(dx, dz);
                var d = Vector2.Distance(q, part);
                if (d < 0.75f || d > 1.5f) continue;
                if (!Ground(ctx, q, out var y, out _) || y < DryY) continue;
                var feet = new Vector3(q.x, y, q.y);
                if (!CapsuleFree(ctx, feet)) continue;
                var hit = PickupRay(ctx, feet + Vector3.up * ctx.Eye, c.position, out var blocker, out var dist);
                options.Add((q, dist, Vector2.Distance(q, gate), hit, blocker));
            }
            if (options.Count == 0) return false;
            var best = options.OrderByDescending(o => o.hit).ThenBy(o => o.g).First();
            c.stand = new Vector2(R(best.p.x), R(best.p.y));
            c.pickupHitsPart = best.hit;
            c.pickupDistance = R(best.d);
            c.pickupBlocker = best.blocker;
            return true;
        }

        // NetworkPlayer's E ray: triggers included, the first thing hit within 2.5 m must be the part.
        private static bool PickupRay(Ctx ctx, Vector3 eye, Vector3 part, out string blocker, out float distance)
        {
            var to = part - eye;
            distance = to.magnitude;
            blocker = null;
            if (distance > PickupRange) { blocker = "out of range"; return false; }
            var first = Physics.RaycastAll(eye, to.normalized, PickupRange, ~0, QueryTriggerInteraction.Collide)
                .Where(h => h.collider.GetComponent<SwimVolume>() == null).OrderBy(h => h.distance).FirstOrDefault();
            if (first.collider == ctx.EngineCollider) return true;
            blocker = first.collider != null ? Named(first.collider.transform) : "nothing";
            return false;
        }

        private static bool CapsuleFree(Ctx ctx, Vector3 feet)
        {
            var r = ctx.Radius + ctx.Skin;
            return !Physics.CheckCapsule(feet + Vector3.up * (r + ctx.Step), feet + Vector3.up * (ctx.Height - r), r, ~0, QueryTriggerInteraction.Ignore);
        }

        private static void MoveEngine(Ctx ctx, Vector3 at)
        {
            ctx.Engine.position = at;
            Physics.SyncTransforms();
        }

        // ---- score -------------------------------------------------------------------------------

        private static (string name, Vector3 feet, float yaw)[] Views(Ctx ctx) => new[]
        {
            ("beach entry (CoastWalk, arriving from the street, facing the sea)", new Vector3(0f, ContinuousTownLayout.WalkSurfaceY, -17f), 0f),
            ("town square (facing the sea)", ContinuousTownLayout.TownSquareCenter, 0f),
            ("beach gate (Beach_TownGate, line of sight, any heading)", Root(ctx.Scene, DiveTestAreaBeachSetup.TownGateName).transform.position, 0f),
            ("home door (facing the sea)", ContinuousTownLayout.HomeRig.Door + new Vector3(0f, 0f, 0.3f), 0f),
            ("wade top (coming ashore, facing the beach)", new Vector3(-6.5f, ContinuousTownLayout.WalkSurfaceY, -11.2f), 180f)
        };

        private static void Score(Ctx ctx, Candidate c)
        {
            MoveEngine(ctx, c.position);
            var bounds = new Bounds(c.position, Vector3.one * PartHalf * 2f);
            c.sights = Views(ctx).Select(v => See(ctx, v.name, v.feet, v.yaw, bounds)).ToArray();
            c.visibleInView = c.sights.Count(s => s.lineOfSight && s.inViewAtHeading);
            // Weights: entry 3, square 3 (in frame); gate 2 (line of sight - the player turns there); home 1; wade top 0.5.
            var weights = new[] { 3f, 3f, 2f, 1f, 0.5f };
            var score = 0f;
            for (var i = 0; i < c.sights.Length; i++)
                if (c.sights[i].lineOfSight && (i == 2 || c.sights[i].inViewAtHeading)) score += weights[i];
            score += c.balance / 4f + Mathf.Min(c.corridorMargin, 3f) / 4f;
            if (c.softMargin < CorridorClearance) score -= 1f;
            // #188 keeps 1.5..4 m around the cradle free for the waiting crew; a pickup inside that band crowds it.
            score -= Mathf.Max(0f, WaitBand - c.toCradle) * 0.5f;
            if (!c.pickupHitsPart) score -= 10f;
            c.score = R(score);
        }

        private static Sight See(Ctx ctx, string from, Vector3 feet, float yaw, Bounds target)
        {
            var eye = feet + Vector3.up * ctx.Eye;
            var heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            string blocker = null;
            var visible = false;
            foreach (var point in new[] { target.center, target.center + Vector3.up * target.extents.y * 0.9f })
            {
                var to = point - eye;
                var hit = Physics.RaycastAll(eye, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    .OrderBy(h => h.distance).Select(h => Named(h.collider.transform)).FirstOrDefault();
                if (hit == null) { visible = true; break; }
                blocker ??= hit;
            }
            var toCenter = target.center - eye;
            var flat = Vector3.ProjectOnPlane(toCenter, Vector3.up);
            var h = Vector3.SignedAngle(heading, flat, Vector3.up);
            var v = Mathf.Asin(Mathf.Clamp(toCenter.y / toCenter.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
            return new Sight
            {
                from = from, eye = R(eye), headingYaw = yaw, distance = R(flat.magnitude), horizontalAngle = R(h),
                projectedHeightPx1080 = R(540f * (target.size.y / Mathf.Max(0.01f, toCenter.magnitude)) / Mathf.Tan(ctx.Fov * 0.5f * Mathf.Deg2Rad)),
                lineOfSight = visible, blocker = visible ? null : blocker,
                inViewAtHeading = Mathf.Abs(h) <= ctx.HFov * 0.5f && Mathf.Abs(v) <= ctx.Fov * 0.5f
            };
        }

        // ---- real walks ------------------------------------------------------------------------------

        private static void Prove(Ctx ctx, Candidate c)
        {
            MoveEngine(ctx, c.position);
            var gate = XZ(Root(ctx.Scene, DiveTestAreaBeachSetup.TownGateName).transform.position);
            var routes = JsonUtility.FromJson<RouteFile>(File.ReadAllText(RoutesPath)).routes;
            // #172 route a0 leaves home spawn 0 through the door and the square to the street (0, -16); take it up to there.
            var a0 = routes.Single(r => r.id == "a0").waypoints;
            var street = Array.FindIndex(a0, w => Vector2.Distance(w, new Vector2(0f, -16f)) < 0.01f);
            var home = a0.Take(street + 1).ToList();
            var walks = new List<Walk>();
            home.AddRange(GridPath(ctx, home[home.Count - 1], c.stand).Skip(1));
            walks.Add(Drive(ctx, "home spawn 0 (#172 a0 to the street, then grid path)", home));
            walks.Add(Drive(ctx, "beach gate", GridPath(ctx, gate, c.stand)));
            walks.Add(Drive(ctx, "wade top (ashore from the sea)", GridPath(ctx, new Vector2(-6.5f, -11.2f), c.stand)));
            c.walks = walks.ToArray();
            c.walkable = walks.All(w => w.reached && !w.fell && !w.stuck && !w.enteredSea);

            var fromGate = BoatRepairGuide.Line(BoatRepairParts.Engine, gate.x, gate.y, c.position.x, c.position.z);
            var fromSquare = BoatRepairGuide.Line(BoatRepairParts.Engine, ContinuousTownLayout.TownSquareCenter.x, ContinuousTownLayout.TownSquareCenter.z, c.position.x, c.position.z);
            var fromWade = BoatRepairGuide.Line(BoatRepairParts.Engine, -6.5f, -11.2f, c.position.x, c.position.z);
            c.hints = new[] { "gate: " + fromGate, "square: " + fromSquare, "wade top: " + fromWade };
        }

        // 8-connected grid search over dry, capsule-free cells (step <= stepOffset), then shortened where a capsule sweep is clear.
        private static List<Vector2> GridPath(Ctx ctx, Vector2 from, Vector2 to)
        {
            Vector2Int K(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.y / Cell));
            Vector2 P(Vector2Int k) => new Vector2(k.x * Cell, k.y * Cell);
            var start = K(from); var goal = K(to);
            var heights = new Dictionary<Vector2Int, float>();
            bool Ok(Vector2Int k, out float y)
            {
                if (heights.TryGetValue(k, out y)) return !float.IsNaN(y);
                var p = P(k);
                var ok = Ground(ctx, p, out y, out _) && y >= DryY && CapsuleFree(ctx, new Vector3(p.x, y, p.y));
                heights[k] = ok ? y : float.NaN;
                return ok;
            }
            var came = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            Ok(start, out _);
            while (queue.Count > 0 && !came.ContainsKey(goal))
            {
                var k = queue.Dequeue();
                var hy = heights.TryGetValue(k, out var hh) && !float.IsNaN(hh) ? hh : ContinuousTownLayout.WalkSurfaceY;
                for (var dx = -1; dx <= 1; dx++)
                for (var dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    var n = new Vector2Int(k.x + dx, k.y + dz);
                    if (came.ContainsKey(n) || n.x * Cell < -15f || n.x * Cell > 15f || n.y * Cell < -60f || n.y * Cell > -3f) continue;
                    if (!Ok(n, out var ny) || Mathf.Abs(ny - hy) > ctx.Step) continue;
                    came[n] = k; queue.Enqueue(n);
                }
            }
            if (!came.ContainsKey(goal)) return new List<Vector2> { from, to };
            var cells = new List<Vector2>();
            for (var k = goal; ; k = came[k]) { cells.Add(P(k)); if (k == start) break; }
            cells.Reverse();
            cells[0] = from; cells[cells.Count - 1] = to;
            var path = new List<Vector2> { cells[0] };
            var i = 0;
            while (i < cells.Count - 1)
            {
                var j = cells.Count - 1;
                while (j > i + 1 && !Clear(ctx, cells[i], cells[j])) j--;
                path.Add(cells[j]); i = j;
            }
            return path;
        }

        private static bool Clear(Ctx ctx, Vector2 a, Vector2 b)
        {
            var r = ctx.Radius + ctx.Skin;
            var y = ContinuousTownLayout.WalkSurfaceY;
            var start = new Vector3(a.x, y, a.y);
            var dir = new Vector3(b.x - a.x, 0f, b.y - a.y);
            if (Physics.CapsuleCast(start + Vector3.up * (r + ctx.Step), start + Vector3.up * (ctx.Height - r), r, dir.normalized, dir.magnitude, ~0,
                    QueryTriggerInteraction.Ignore)) return false;
            for (var s = 0f; s <= dir.magnitude; s += Cell)
            {
                var q = Vector2.Lerp(a, b, dir.magnitude < 1e-4f ? 0f : s / dir.magnitude);
                if (!Ground(ctx, q, out var gy, out _) || gy < DryY || Mathf.Abs(gy - y) > ctx.Step) return false;
            }
            return true;
        }

        private static Walk Drive(Ctx ctx, string from, List<Vector2> waypoints)
        {
            var walk = new Walk { from = from, waypoints = waypoints.Select(w => new Vector2(R(w.x), R(w.y))).ToArray() };
            var failures = new List<string>();
            var go = new GameObject("Audit_EngineShore_Diver");
            SceneManager.MoveGameObjectToScene(go, ctx.Scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = ctx.Height; cc.radius = ctx.Radius; cc.center = new Vector3(0f, ctx.Height * 0.5f, 0f);
                cc.slopeLimit = ctx.Slope; cc.stepOffset = ctx.Step; cc.skinWidth = ctx.Skin;
                var t = go.transform;
                var y0 = Ground(ctx, waypoints[0], out var gy, out _) ? gy : ContinuousTownLayout.WalkSurfaceY;
                t.position = new Vector3(waypoints[0].x, y0 + ctx.Skin, waypoints[0].y);
                Physics.SyncTransforms();
                var vy = 0f;
                var frames = 0;
                for (var w = 1; w < waypoints.Count && !walk.stuck; w++)
                {
                    var target = waypoints[w];
                    var guard = Mathf.CeilToInt(Vector2.Distance(waypoints[w - 1], target) / (ctx.Speed * Dt)) * 3 + 100;
                    while (true)
                    {
                        var here = XZ(t.position);
                        var toTarget = target - here;
                        if (toTarget.magnitude <= 0.05f) break;
                        if (--guard < 0) { walk.stuck = true; failures.Add($"stuck at ({here.x:0.##}, {here.y:0.##}) heading to {Fmt(target)}"); break; }
                        var step = Vector2.ClampMagnitude(toTarget, ctx.Speed * Dt);
                        vy = cc.isGrounded ? -1f : vy - 9.81f * Dt;
                        cc.Move(new Vector3(step.x, vy * Dt, step.y));
                        frames++;
                        walk.drivenLength += Vector2.Distance(here, XZ(t.position));
                        var feet = t.position;
                        if (ctx.Zones.ResolveZone(feet) == PlayerZone.Sea) { walk.enteredSea = true; failures.Add($"entered sea at ({feet.x:0.##}, {feet.z:0.##})"); }
                        if (!Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                        { walk.fell = true; failures.Add($"over no ground at ({feet.x:0.##}, {feet.z:0.##})"); }
                        else if (ground.collider.name != "Beach_Wade" && feet.y < ContinuousTownLayout.WalkSurfaceY - 0.05f)
                        { walk.fell = true; failures.Add($"dropped to y={feet.y:0.###} at ({feet.x:0.##}, {feet.z:0.##}) over {ground.collider.name}"); }
                    }
                }
                walk.endPosition = new Vector2(R(t.position.x), R(t.position.z));
                walk.reached = !walk.stuck && Vector2.Distance(XZ(t.position), waypoints[waypoints.Count - 1]) <= 0.2f;
                walk.simSeconds = R(frames * Dt);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            walk.drivenLength = R(walk.drivenLength);
            walk.failures = failures.Distinct().Take(20).ToArray();
            return walk;
        }

        // ---- helpers -----------------------------------------------------------------------------

        private static bool Ground(Ctx ctx, Vector2 p, out float y, out string name)
        {
            y = 0f; name = null;
            if (!Physics.Raycast(new Vector3(p.x, 14f, p.y), Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore)) return false;
            name = Named(hit.collider.transform);
            if (!ctx.Walk.Contains(hit.collider.name)) return false;
            y = hit.point.y;
            return true;
        }

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static GameObject Root(Scene scene, string name) =>
            All<Transform>(scene).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static string Named(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;
        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
        private static float R(float v) => Mathf.Round(v * 1000f) / 1000f;
        private static Vector3 R(Vector3 v) => new Vector3(R(v.x), R(v.y), R(v.z));
        private static string Fmt(Vector2 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##})", v.x, v.y);
    }
}
