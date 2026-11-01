using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #219 (P4.11-B): read-only measurement of the current sea bed. What is the floor made of, where does it step,
    // can a real CharacterController swim shore -> reef -> deep -> surface -> shore, where can the fish go, and how
    // high may a new floor rise anywhere without closing a fish, boss, route or diver volume. Same method as
    // P4WadeUndersideAudit: the scene is opened additively, the deterministic town is built on top, real physics
    // queries and a real CharacterController do the measuring, and the scene is closed unsaved.
    // Coordinates: Unity world space, metres, y up, +z north (sea), +x east.
    public static class P411SeabedAudit
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        public const string OutputPath = "docs/reports/evidence/p411-seabed/seabed.json";
        public const string CandidatePath = "docs/reports/evidence/p411-seabed/seabed-candidate.json";

        private const float Dt = 0.02f;
        private const float LandColumn = 99f;      // the bed grid's marker for "dry/solid at the water line"
        private const float CliffDegrees = 45f;    // steeper than this reads as a wall rather than a sand slope
        private const float ProfileStep = 0.25f;
        private const float BedClearance = 1.5f;   // feet above the bed for the contour lanes
        private const float EscapeSeconds = 90f;
        private const float FishWanderSeconds = 300f;
        private const float FishFleeSeconds = 20f;
        private const float FishChaseSeconds = 40f;
        private const float FishClearance = 0.2f;  // P4WadeUndersideAudit's scenery margin
        private const float MinWaterColumn = 2.2f; // a diver's capsule + skin under the line
        private const float HullFootprintMargin = 1f;

        public static readonly float[] LaneXs = { -10f, -5f, 0f, 5f, 10f };

        [Serializable] public sealed class Piece
        {
            public string name, collider, material;
            public Vector3 position, euler, scale, boundsMin, boundsMax;
            public float topY, tiltDegrees;
            public int triangles;
            public bool hasRenderer;
        }

        [Serializable] public sealed class BedRow { public float z; public float[] bedY; public int[] piece; }
        [Serializable] public sealed class CeilingRow { public float z; public float[] maxFloorY; public int[] limiter; }

        [Serializable] public sealed class Segment
        {
            public float from, to, yFrom, yTo, slopeDegrees;
            public string piece;
        }

        [Serializable] public sealed class Transition
        {
            public string kind, axis, fromPiece, toPiece;
            public float line, from, to, yFrom, yTo, drop, slopeDegrees;
        }

        [Serializable] public sealed class Profile
        {
            public string axis;
            public float line;
            public Segment[] segments;
            public Transition[] transitions;
            public float firstWaterZ, shallowToReefZ, reefToDeepZ;
        }

        [Serializable] public sealed class BandArea { public string band, piece; public float areaM2, minBedY, maxBedY; }

        [Serializable] public sealed class FreeWater
        {
            public int lattice, free, reachable, blockedAscent, unreachableAboveBed;
            public Vector3[] unreachableSample;
            public string[] blockedAscentBy;
        }

        [Serializable] public sealed class Block { public Vector3 at; public int leg; public float seconds; public string touching; }

        [Serializable] public sealed class Drive
        {
            public string id;
            public int waypoints;
            public float plannedLength, drivenLength, seconds, oxygenSeconds, minEyeClearance, maxDepth;
            public bool reachedEnd;
            public Vector3 end;
            public string endEnvironment;
            public Block[] blocks;
            public string[] bandsVisited;
        }

        [Serializable] public sealed class EscapeSummary
        {
            public string strategy;
            public int starts, reachedSurface, trapped;
            public float worstSeconds;
            public Vector3[] trappedFrom;
            public string[] trappedTouching;
        }

        [Serializable] public sealed class Sight
        {
            public string target;
            public Vector3 point;
            public int viewpoints, clear;
            public float bedBelow, surfaceAbove;
            public string[] blockers;
        }

        [Serializable] public sealed class Run_
        {
            public string scenario;
            public int frames, centreInside, bodyTouch, belowBed, stalled;
            public float maxDistanceFromHome;
            public Vector3 min, max, end;
            public bool endsInside;
            public string[] entered;
        }

        [Serializable] public sealed class FishRow
        {
            public string name, species, homeBand;
            public Vector3 home;
            public float wanderRadius, verticalRadius, bodyRadius, envelopeMinClearance, bedAtHome;
            public string[] envelopeHits;
            public Run_ wander;
            public Run_[] flee;
            public Run_ habitatWander;
            public Run_[] habitatFlee;
        }

        [Serializable] public sealed class SpeciesVolume
        {
            public string species;
            public int count;
            public Vector3 homeMin, homeMax, wanderMin, wanderMax, fleeMin, fleeMax;
            public float swimSpeed, fleeSpeed, fleeRadius, wanderRadius, fleeFractionInside, wanderFractionInside, fleeFractionBelowBed, fleeMaxDistance;
            public Vector3 habitatFleeMin, habitatFleeMax;
            public float habitatFractionInside, habitatFractionBelowBed, habitatFractionStalled, habitatMaxDistance;
        }

        [Serializable] public sealed class Protected { public string id, source, rule; public Vector3 min, max; public float bedBelow; }

        [Serializable] public sealed class RouteRow
        {
            public string id, anchor, requiredClass;
            public Vector3[] waypoints;
            public float minBedClearanceUnderKeel, bedAtAnchor;
            public string bandAtAnchorBed;
        }

        [Serializable] public sealed class Audit
        {
            public string branchBase = "codex/p4-integration @ a7adc55";
            public string unity = Application.unityVersion;
            public string scene = ScenePath, prefab = DiverPath;
            public string coordinates = "Unity world, metres, y up, +z north (sea), +x east";
            public float radius, height, skin, stepOffset, slopeLimit, swimSpeed, walkSpeed, maxOxygen, eyeHeight, nearClip, verticalFov, surfaceY;
            public float harpoonRange, pickupRange, hullDraft, dt = Dt;
            public Vector3 swimMin, swimMax;
            public float regionMinX, regionMaxX, regionMinZ, regionMaxZ;
            public string[] depthBands;
            public Piece[] pieces;
            public string[] pieceTable;
            public float[] gridXs;
            public BedRow[] bedGrid;
            public Profile[] profilesAlongZ, profilesAlongX;
            public BandArea[] bandAreas;
            public FreeWater freeWater;
            public Drive[] lanes;
            public Drive[] loops;
            public Drive[] surveys;
            public EscapeSummary[] escapes;
            public FishRow[] fish;
            public SpeciesVolume[] species;
            public Sight[] sights;
            public Protected[] protectedVolumes;
            public RouteRow[] routes;
            public float floorLowerBoundY;
            public string[] limiterTable;
            public CeilingRow[] ceilingGrid;
            public int sceneRenderersInWater, sceneTrianglesInWater;
            public Candidate candidate;
            public string[] notMeasured;
        }

        [Serializable] public sealed class Rock { public string id, role; public Vector3 min, max; }

        [Serializable] public sealed class Candidate
        {
            public string note;
            public string[] disabledPieces;
            public Vector3[] seaBassHomes;
            public Rock[] solids;
            public int terrainTriangles, floorOverCeiling;
            public float worstFloorOverCeiling;
            public string[] floorOverCeilingSample;
            public float steepestDegrees;
            public Vector2[] centreProfile;
            public Decor[] decor;
            public string[] habitatLeaks;
        }

        [Serializable] public sealed class Decor { public string id, role; public float x, z, baseY, topY, ceilingY; public string ceilingBy; public bool overCeiling; }

        private sealed class Ctx
        {
            public bool IsCandidate;
            public Scene Scene;
            public float Radius, Height, Skin, Step, Slope, SwimSpeed, WalkSpeed, SurfaceY, Eye, Near;
            public Vector3 Center;
            public WaterField Field;
            public BoxCollider Swim;
        }

        public static void Run() => Measure(false, OutputPath);

        // The same measurement with the plan's floor swapped in, in memory only: this is evidence for a proposal.
        public static void RunCandidate() => Measure(true, CandidatePath);

        private static void Measure(bool isCandidate, string outputPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            var cc = prefab.GetComponent<CharacterController>();
            var cam = prefab.GetComponentInChildren<Camera>(true);
            var player = new SerializedObject(prefab.GetComponent<NetworkPlayer>());
            var ctx = new Ctx
            {
                IsCandidate = isCandidate,
                Radius = cc.radius, Height = cc.height, Skin = cc.skinWidth, Step = cc.stepOffset, Slope = cc.slopeLimit,
                Center = cc.center, SwimSpeed = player.FindProperty("swimSpeed").floatValue,
                WalkSpeed = player.FindProperty("walkSpeed").floatValue,
                Eye = prefab.transform.InverseTransformPoint(cam.transform.position).y, Near = cam.nearClipPlane
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
                var candidate = isCandidate ? ApplyCandidate(ctx) : null;
                var region = All<DiveRegionField>(scene).Single();
                var swimBounds = ctx.Swim.bounds;

                var audit = new Audit
                {
                    radius = ctx.Radius, height = ctx.Height, skin = ctx.Skin, stepOffset = ctx.Step, slopeLimit = ctx.Slope,
                    swimSpeed = ctx.SwimSpeed, walkSpeed = ctx.WalkSpeed, eyeHeight = R(ctx.Eye), nearClip = ctx.Near,
                    verticalFov = cam.fieldOfView, surfaceY = ctx.SurfaceY,
                    maxOxygen = player.FindProperty("maxOxygen").floatValue,
                    harpoonRange = player.FindProperty("harpoonRange").floatValue,
                    pickupRange = player.FindProperty("pickupRange").floatValue,
                    swimMin = R(swimBounds.min), swimMax = R(swimBounds.max),
                    regionMinX = region.Bounds.MinX, regionMaxX = region.Bounds.MaxX,
                    regionMinZ = region.Bounds.MinZ, regionMaxZ = region.Bounds.MaxZ,
                    hullDraft = 0.5f,
                    depthBands = DiveDepthBands.All.Select(b =>
                        $"{b.Id}: {b.MinDepth}..{b.MaxDepth} m = y {ctx.SurfaceY - b.MinDepth}..{ctx.SurfaceY - b.MaxDepth}").ToArray()
                };

                audit.pieces = MeasurePieces(ctx).ToArray();
                MeasureBedGrid(ctx, audit);
                audit.profilesAlongZ = LaneXs.Select(x => MeasureProfile(ctx, "z", x)).ToArray();
                audit.profilesAlongX = new[] { -10f, 5f, 12f, 22f, 24f, 30f, 35f, 43f, 52f, 57.5f }.Select(z => MeasureProfile(ctx, "x", z)).ToArray();
                audit.bandAreas = MeasureBandAreas(ctx, audit).ToArray();
                audit.freeWater = MeasureFreeWater(ctx);
                audit.lanes = MeasureLanes(ctx).ToArray();
                audit.loops = MeasureLoops(ctx).ToArray();
                audit.escapes = MeasureEscapes(ctx).ToArray();
                audit.fish = All<FishActor>(scene).OrderBy(f => f.name, StringComparer.Ordinal).Select(f => MeasureFish(ctx, f)).ToArray();
                audit.species = SummariseSpecies(audit.fish, All<FishActor>(scene).ToArray());
                audit.sights = MeasureSights(ctx, audit.fish).ToArray();
                audit.protectedVolumes = MeasureProtected(ctx).ToArray();
                audit.routes = MeasureRoutes(ctx, All<DiveRoutePath>(scene).ToArray()).ToArray();
                MeasureCeiling(ctx, audit, All<DiveRoutePath>(scene).ToArray());
                MeasureRenderers(ctx, audit);
                audit.surveys = MeasureSurveys(ctx).ToArray();
                if (isCandidate) audit.surveys = audit.surveys.Concat(MeasureGate(ctx)).ToArray();
                if (candidate != null)
                {
                    CheckCandidateAgainstCeiling(candidate, audit);
                    audit.candidate = candidate;
                }
                audit.notMeasured = new[]
                {
                    "network latency, 1/2/4 process timing, build frame time: this is the host FixedUpdate rule replayed in EditMode",
                    "CurrentDrift: zero current here (CrewEnvironmentRules drift adds to swim velocity in game)",
                    "player steering: drives hold a straight line to the next waypoint; a blocked leg is reported, a person would go round",
                    "fog/absorption readability: sights are geometric line of sight only",
                    "fish visuals: body radius is the conservative renderer half-extent, not the mesh hull",
                    "boss/trace presentation is built at run time, so those volumes come from DeepEncounterWorld's constants",
                    "the anchored hull itself: routes are measured against the bed, not against a spawned boat"
                };

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllText(outputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P411_SEABED_AUDIT_WRITTEN " + outputPath);
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- 1. what the floor is ------------------------------------------------------------------

        private static IEnumerable<Piece> MeasurePieces(Ctx ctx)
        {
            var swim = ctx.Swim.bounds;
            foreach (var c in All<Collider>(ctx.Scene).Where(c => c.enabled && !c.isTrigger).OrderBy(c => c.name, StringComparer.Ordinal))
            {
                var b = c.bounds;
                if (b.min.y >= ctx.SurfaceY || b.max.x <= swim.min.x || b.min.x >= swim.max.x || b.max.z <= swim.min.z || b.min.z >= swim.max.z)
                    continue;
                var t = c.transform;
                var renderer = c.GetComponent<MeshRenderer>();
                var mesh = c.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : (c as MeshCollider)?.sharedMesh;
                yield return new Piece
                {
                    name = c.name, collider = c.GetType().Name, position = R(t.position), euler = R(t.eulerAngles), scale = R(t.lossyScale),
                    boundsMin = R(b.min), boundsMax = R(b.max), topY = R(b.max.y), tiltDegrees = R(Vector3.Angle(t.up, Vector3.up)),
                    hasRenderer = renderer != null, material = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "",
                    triangles = mesh != null ? (int)(mesh.GetIndexCount(0) / 3) : 0
                };
            }
        }

        // The first solid thing straight down from just under the water line. LandColumn when that point is already
        // inside something (the beach, the wade's upper end): there is no water column there at all.
        private static float BedAt(Ctx ctx, float x, float z, out string piece)
        {
            piece = "";
            var origin = new Vector3(x, ctx.SurfaceY - 0.05f, z);
            if (Physics.CheckSphere(origin, 0.01f, ~0, QueryTriggerInteraction.Ignore))
            {
                piece = "(land)";
                return LandColumn;
            }
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                piece = "(open)";
                return ctx.Swim.bounds.min.y;
            }
            piece = hit.collider.name;
            return hit.point.y;
        }

        private static void MeasureBedGrid(Ctx ctx, Audit audit)
        {
            var table = new List<string>();
            var xs = new List<float>();
            for (var x = -14.5f; x <= 14.5f; x += 1f) xs.Add(x);
            var rows = new List<BedRow>();
            for (var z = -14.5f; z <= 64.5f; z += 1f)
            {
                var row = new BedRow { z = z, bedY = new float[xs.Count], piece = new int[xs.Count] };
                for (var i = 0; i < xs.Count; i++)
                {
                    row.bedY[i] = R(BedAt(ctx, xs[i], z, out var name));
                    var index = table.IndexOf(name);
                    if (index < 0) { index = table.Count; table.Add(name); }
                    row.piece[i] = index;
                }
                rows.Add(row);
            }
            audit.gridXs = xs.ToArray();
            audit.bedGrid = rows.ToArray();
            audit.pieceTable = table.ToArray();
        }

        // One line through the world: the floor as runs of one piece, and every place the floor jumps or goes steeper
        // than CliffDegrees. Axis "z" walks north at a fixed x; axis "x" walks east at a fixed z.
        private static Profile MeasureProfile(Ctx ctx, string axis, float line)
        {
            var from = axis == "z" ? -14.5f : -14.5f;
            var to = axis == "z" ? 64.5f : 14.5f;
            var segments = new List<Segment>();
            var transitions = new List<Transition>();
            var profile = new Profile { axis = axis, line = line, firstWaterZ = float.NaN, shallowToReefZ = float.NaN, reefToDeepZ = float.NaN };
            float? previous = null;
            var previousPiece = "";
            var previousAt = from;
            Segment run = null;
            Transition open = null;
            for (var at = from; at <= to + 1e-3f; at += ProfileStep)
            {
                var bed = axis == "z" ? BedAt(ctx, line, at, out var piece) : BedAt(ctx, at, line, out piece);
                if (axis == "z")
                {
                    if (bed < LandColumn && float.IsNaN(profile.firstWaterZ)) profile.firstWaterZ = at;
                    if (previous.HasValue && previous.Value >= 0f && bed < 0f && float.IsNaN(profile.shallowToReefZ)) profile.shallowToReefZ = at;
                    if (previous.HasValue && previous.Value >= -12f && bed < -12f && float.IsNaN(profile.reefToDeepZ)) profile.reefToDeepZ = at;
                }
                if (previous.HasValue)
                {
                    var drop = previous.Value - bed;
                    var degrees = Mathf.Atan2(Mathf.Abs(drop), ProfileStep) * Mathf.Rad2Deg;
                    var landEdge = previous.Value >= LandColumn != bed >= LandColumn;
                    var kink = landEdge || degrees > CliffDegrees;
                    if (kink)
                    {
                        if (open == null)
                            open = new Transition
                            {
                                kind = landEdge ? "land-edge" : "cliff", axis = axis, line = line, from = previousAt, yFrom = R(previous.Value),
                                fromPiece = previousPiece
                            };
                        open.to = at;
                        open.yTo = R(bed);
                        open.toPiece = piece;
                    }
                    else if (open != null)
                    {
                        Finish(open, transitions);
                        open = null;
                    }
                }
                if (run == null || run.piece != piece)
                {
                    if (run != null) FinishRun(run, segments);
                    run = new Segment { from = at, yFrom = R(bed), piece = piece };
                }
                run.to = at;
                run.yTo = R(bed);
                previous = bed;
                previousPiece = piece;
                previousAt = at;
            }
            if (open != null) Finish(open, transitions);
            if (run != null) FinishRun(run, segments);
            profile.segments = segments.ToArray();
            profile.transitions = transitions.ToArray();
            profile.firstWaterZ = Clean(profile.firstWaterZ);
            profile.shallowToReefZ = Clean(profile.shallowToReefZ);
            profile.reefToDeepZ = Clean(profile.reefToDeepZ);
            return profile;
        }

        private static float Clean(float v) => float.IsNaN(v) ? -999f : v;

        private static void Finish(Transition t, List<Transition> list)
        {
            t.drop = R(t.yFrom - t.yTo);
            t.slopeDegrees = R(Mathf.Atan2(Mathf.Abs(t.drop), Mathf.Max(0.001f, t.to - t.from)) * Mathf.Rad2Deg);
            list.Add(t);
        }

        private static void FinishRun(Segment s, List<Segment> list)
        {
            s.slopeDegrees = s.yFrom >= LandColumn || s.yTo >= LandColumn ? 0f
                : R(Mathf.Atan2(Mathf.Abs(s.yFrom - s.yTo), Mathf.Max(0.001f, s.to - s.from)) * Mathf.Rad2Deg);
            list.Add(s);
        }

        private static IEnumerable<BandArea> MeasureBandAreas(Ctx ctx, Audit audit)
        {
            var areas = new Dictionary<(string, string), BandArea>();
            foreach (var row in audit.bedGrid)
                for (var i = 0; i < row.bedY.Length; i++)
                {
                    var bed = row.bedY[i];
                    var band = bed >= LandColumn ? "(land)"
                        : DiveDepthBands.TryFind(ctx.SurfaceY - bed, out var found) ? found.Id : "(above-water)";
                    var key = (band, audit.pieceTable[row.piece[i]]);
                    if (!areas.TryGetValue(key, out var area))
                        areas[key] = area = new BandArea { band = key.Item1, piece = key.Item2, minBedY = bed, maxBedY = bed };
                    area.areaM2 += 1f;
                    area.minBedY = Mathf.Min(area.minBedY, bed);
                    area.maxBedY = Mathf.Max(area.maxBedY, bed);
                }
            return areas.Values.OrderBy(a => a.band, StringComparer.Ordinal).ThenByDescending(a => a.areaM2);
        }

        // ---- 2. the water a diver can use -----------------------------------------------------------

        // 1 m lattice of feet positions. Reachable = connected through free lattice cells to the top layer, which is
        // the question "is there water you can swim into but not back out of".
        private static FreeWater MeasureFreeWater(Ctx ctx)
        {
            const int nx = 29, nz = 79;
            var yMin = -26;
            var ny = 33;
            var free = new bool[nx, ny, nz];
            var seen = new bool[nx, ny, nz];
            var queue = new Queue<(int, int, int)>();
            var result = new FreeWater();
            for (var ix = 0; ix < nx; ix++)
            for (var iy = 0; iy < ny; iy++)
            for (var iz = 0; iz < nz; iz++)
            {
                var feet = new Vector3(ix - 14f, yMin + iy + 0.5f, iz - 14f);
                result.lattice++;
                if (!Fits(ctx, feet, out _)) continue;
                free[ix, iy, iz] = true;
                result.free++;
                if (iy == ny - 1) { seen[ix, iy, iz] = true; queue.Enqueue((ix, iy, iz)); }
            }
            var steps = new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) };
            while (queue.Count > 0)
            {
                var (x, y, z) = queue.Dequeue();
                result.reachable++;
                foreach (var (dx, dy, dz) in steps)
                {
                    int a = x + dx, b = y + dy, c = z + dz;
                    if (a < 0 || b < 0 || c < 0 || a >= nx || b >= ny || c >= nz || !free[a, b, c] || seen[a, b, c]) continue;
                    seen[a, b, c] = true;
                    queue.Enqueue((a, b, c));
                }
            }
            var unreachable = new List<Vector3>();
            var overhead = new Dictionary<string, int>();
            for (var ix = 0; ix < nx; ix++)
            for (var iy = 0; iy < ny; iy++)
            for (var iz = 0; iz < nz; iz++)
            {
                if (!free[ix, iy, iz]) continue;
                var feet = new Vector3(ix - 14f, yMin + iy + 0.5f, iz - 14f);
                if (!seen[ix, iy, iz])
                {
                    // Under the floor is sealed void, not a trap. Only water above the bed can catch a diver.
                    var bed = BedAt(ctx, feet.x, feet.z, out _);
                    if (bed >= LandColumn || feet.y < bed) continue;
                    result.unreachableAboveBed++;
                    if (unreachable.Count < 40) unreachable.Add(feet);
                    continue;
                }
                if (!Physics.Raycast(feet + Vector3.up * ctx.Height, Vector3.up, out var hit, ctx.SurfaceY + 1f - feet.y, ~0, QueryTriggerInteraction.Ignore)) continue;
                result.blockedAscent++;
                overhead[hit.collider.name] = overhead.TryGetValue(hit.collider.name, out var n) ? n + 1 : 1;
            }
            result.unreachableSample = unreachable.ToArray();
            result.blockedAscentBy = overhead.OrderByDescending(k => k.Value).Select(k => k.Key + "=" + k.Value).ToArray();
            return result;
        }

        // Contour lanes: north at a fixed x, always BedClearance above whatever the bed is, then back the same way.
        private static IEnumerable<Drive> MeasureLanes(Ctx ctx)
        {
            foreach (var x in LaneXs)
            {
                var points = new List<Vector3>();
                for (var z = -14f; z <= 63.5f; z += 1f)
                {
                    var bed = BedAt(ctx, x, z, out _);
                    if (bed >= LandColumn) continue;
                    // A swimmer sees a rock coming: the highest bed within a metre ahead or behind sets the height.
                    var ahead = BedAt(ctx, x, z + 1f, out _);
                    var behind = BedAt(ctx, x, z - 1f, out _);
                    var top = Mathf.Max(bed, ahead < LandColumn ? ahead : bed, behind < LandColumn ? behind : bed);
                    var y = Mathf.Min(top + BedClearance, ctx.SurfaceY - ctx.Height - 0.5f);
                    var feet = new Vector3(x, y, z);
                    if (points.Count > 0 || Fits(ctx, feet, out _)) points.Add(feet);
                }
                if (points.Count < 2) continue;
                yield return DriveCc(ctx, $"lane-x{x}-north", points[0], points.Skip(1).ToArray());
                var back = Enumerable.Reverse(points).ToArray();
                yield return DriveCc(ctx, $"lane-x{x}-south", back[0], back.Skip(1).ToArray());
            }
        }

        // The whole trip, from the sand: walk down the wade, swim to the reef and the basin, visit the boss arena and
        // its traces, go straight up at the deep anchorage, swim home on the surface and walk out.
        private static IEnumerable<Drive> MeasureLoops(Ctx ctx)
        {
            var wade = Root(ctx.Scene, "Beach_Wade").GetComponent<Collider>();
            var wadeBounds = wade.bounds;
            foreach (var x in new[] { -7.6f, -6.5f, -4f })
            {
                var top = Raycast(ctx, new Vector3(x, wadeBounds.max.y + 2f, wadeBounds.min.z + 0.5f));
                var foot = Raycast(ctx, new Vector3(x, wadeBounds.max.y + 2f, wadeBounds.max.z - 0.5f));
                var start = top + Vector3.up * (ctx.Skin + 0.01f);
                var ashore = foot + Vector3.up * (ctx.Skin + 0.01f);
                var outbound = new[]
                {
                    ashore,
                    new Vector3(x, 3f, wadeBounds.max.z + 6f),
                    new Vector3(x * 0.5f, 1.5f, 13f),
                    new Vector3(0f, -5f, 21f),
                    new Vector3(0f, -6f, 32f),
                    new Vector3(-3f, -9f, 41f),
                    new Vector3(0f, Mathf.Max(-22f, BedAt(ctx, 0f, 50f, out _) + 2f), 50f),
                    DeepEncounterWorld.TracePositions[0],
                    DeepEncounterWorld.TracePositions[2],
                    DeepEncounterWorld.ArenaPosition,
                    DeepEncounterWorld.TracePositions[1],
                    new Vector3(0f, -20f, 56f),
                    new Vector3(0f, ctx.SurfaceY - 0.5f, 56f),
                    new Vector3(x, ctx.SurfaceY - 0.5f, 20f),
                    new Vector3(x, ctx.SurfaceY - 0.5f, wadeBounds.max.z + 1f),
                    start
                };
                yield return DriveCc(ctx, $"loop-wade-x{x}", start, outbound);
            }
        }

        // ProductDaySmokeDriver.OceanSurvey's underwater part for each slot: the -OceanVideo route, which the before
        // and after videos replay on the same build. It has to stay swimmable as written.
        private static IEnumerable<Drive> MeasureSurveys(Ctx ctx)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                var x = -11f + slot * 1.3f;
                var stand = new Vector3(-6.5f + slot, 6.3f, -5f);
                yield return DriveCc(ctx, $"ocean-survey-slot{slot}", stand, new[]
                {
                    new Vector3(x, 2f, 9f), new Vector3(x, -4f, 30f), new Vector3(x, -16f, 53f),
                    new Vector3(x, -4f, 30f), new Vector3(x, 2f, 9f), stand
                });
            }
        }

        // Straight through the arch at floor height, then over its top, both ways.
        private static IEnumerable<Drive> MeasureGate(Ctx ctx)
        {
            var low = FloorHeight(-7f, 30f) + 0.3f;
            yield return DriveCc(ctx, "reef-gate-under-north", new Vector3(-7f, -4.5f, 26f), new[] { new Vector3(-7f, low, 28f), new Vector3(-7f, low, 32f), new Vector3(-7f, -4.5f, 33.5f) });
            yield return DriveCc(ctx, "reef-gate-under-south", new Vector3(-7f, -4.5f, 33.5f), new[] { new Vector3(-7f, low, 32f), new Vector3(-7f, low, 28f), new Vector3(-7f, -4.5f, 26f) });
            yield return DriveCc(ctx, "reef-gate-over-north", new Vector3(-7f, -3.5f, 26f), new[] { new Vector3(-7f, -3.5f, 33.5f) });
        }

        private static Vector3 Raycast(Ctx ctx, Vector3 from)
        {
            if (!Physics.Raycast(from, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("P411_NO_FLOOR at=" + from);
            return hit.point;
        }

        private static CharacterController MakeDiver(Ctx ctx, Vector3 feet)
        {
            var go = new GameObject("Audit_P411_Diver");
            SceneManager.MoveGameObjectToScene(go, ctx.Scene);
            var cc = go.AddComponent<CharacterController>();
            cc.height = ctx.Height; cc.radius = ctx.Radius; cc.center = ctx.Center;
            cc.slopeLimit = ctx.Slope; cc.stepOffset = ctx.Step; cc.skinWidth = ctx.Skin;
            go.transform.position = feet;
            Physics.SyncTransforms();
            return cc;
        }

        // NetworkPlayer.FixedUpdate + PlayerPresentationRules.FilterMoveInput, steering straight at each waypoint.
        // A waypoint is passed within 0.45 m; 1.5 s of no progress reports a block and moves on to the next one.
        private static Drive DriveCc(Ctx ctx, string id, Vector3 start, Vector3[] waypoints)
        {
            var drive = new Drive { id = id, waypoints = waypoints.Length, minEyeClearance = float.MaxValue };
            var blocks = new List<Block>();
            var bands = new HashSet<string>();
            var cc = MakeDiver(ctx, start);
            try
            {
                var t = cc.transform;
                var tracker = ctx.Field.CreateTracker();
                var last = start;
                var previous = start;
                foreach (var w in waypoints) { drive.plannedLength += Vector3.Distance(previous, w); previous = w; }
                var gravity = 0f;
                var seconds = 0f;
                var frame = 0;
                var env = EnvironmentLocomotion.Land;
                for (var leg = 0; leg < waypoints.Length; leg++)
                {
                    var target = waypoints[leg];
                    var lastMove = t.position;
                    var lastMoveAt = seconds;
                    var legLimit = seconds + Vector3.Distance(t.position, target) / Mathf.Min(ctx.SwimSpeed, ctx.WalkSpeed) * 3f + 10f;
                    while (seconds < legLimit)
                    {
                        env = tracker.Classify(new WaterProbe(t.position, t.TransformVector(cc.center), ctx.Radius, ctx.Height, Vector3.up));
                        var offset = target - t.position;
                        if (env != EnvironmentLocomotion.Underwater) offset.y = target.y < ctx.SurfaceY - 2f ? offset.y : 0f;
                        if (offset.magnitude <= 0.45f) break;

                        var mode = env == EnvironmentLocomotion.Land ? LocomotionMode.Land
                            : env == EnvironmentLocomotion.Surface ? LocomotionMode.Surface : LocomotionMode.Underwater;
                        var move = PlayerPresentationRules.FilterMoveInput(mode, offset.normalized);
                        var swimming = PlayerPresentationRules.IsSwimming(mode);
                        var velocity = move * (swimming ? ctx.SwimSpeed : ctx.WalkSpeed);
                        if (swimming) gravity = 0f;
                        else
                        {
                            gravity = cc.isGrounded ? -2f : Mathf.Max(gravity - 9.81f * Dt, -20f);
                            velocity.y = gravity;
                        }
                        var before = t.position;
                        cc.Move(velocity * Dt);
                        drive.drivenLength += Vector3.Distance(before, t.position);
                        seconds += Dt;
                        frame++;
                        if (PlayerPresentationRules.DrainsOxygen(mode)) drive.oxygenSeconds += Dt;
                        drive.maxDepth = Mathf.Max(drive.maxDepth, ctx.SurfaceY - t.position.y);
                        if (WaterDepth.TryClassify(ctx.Field.Bodies, t.position, out var band)) bands.Add(band);
                        if (frame % 25 == 0) drive.minEyeClearance = Mathf.Min(drive.minEyeClearance, Clearance(t.position + Vector3.up * ctx.Eye, 2f));

                        if (Vector3.Distance(t.position, lastMove) > 0.05f) { lastMove = t.position; lastMoveAt = seconds; }
                        else if (seconds - lastMoveAt > 1.5f)
                        {
                            blocks.Add(new Block { at = R(t.position), leg = leg, seconds = R(seconds), touching = Touching(ctx, cc) });
                            break;
                        }
                    }
                    if (seconds >= legLimit) blocks.Add(new Block { at = R(t.position), leg = leg, seconds = R(seconds), touching = "timeout:" + Touching(ctx, cc) });
                }
                var endOffset = waypoints[waypoints.Length - 1] - t.position;
                drive.reachedEnd = blocks.Count == 0 && endOffset.magnitude < 1.5f;
                drive.end = R(t.position);
                drive.endEnvironment = env.ToString();
                drive.seconds = R(seconds);
                drive.oxygenSeconds = R(drive.oxygenSeconds);
                drive.maxDepth = R(drive.maxDepth);
                drive.plannedLength = R(drive.plannedLength);
                drive.drivenLength = R(drive.drivenLength);
                drive.minEyeClearance = R(drive.minEyeClearance);
                drive.blocks = blocks.ToArray();
                drive.bandsVisited = bands.OrderBy(b => b, StringComparer.Ordinal).ToArray();
                return drive;
            }
            finally
            {
                Object.DestroyImmediate(cc.gameObject);
                Physics.SyncTransforms();
            }
        }

        private static string Touching(Ctx ctx, CharacterController cc)
        {
            var feet = cc.transform.position;
            return string.Join(",", Physics.OverlapCapsule(feet + Vector3.up * ctx.Radius, feet + Vector3.up * (ctx.Height - ctx.Radius),
                    ctx.Radius + ctx.Skin + 0.05f, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => c != cc).Select(c => c.name).Distinct());
        }

        // From 1.5 m above the bed on a 5 m grid: hold "up", or "up + toward the shore", until the surface.
        private static IEnumerable<EscapeSummary> MeasureEscapes(Ctx ctx)
        {
            foreach (var (strategy, input) in new[]
                     {
                         ("up", Vector3.up),
                         ("up+south", Vector3.ClampMagnitude(new Vector3(0f, 1f, -1f), 1f))
                     })
            {
                var summary = new EscapeSummary { strategy = strategy };
                var trapped = new List<Vector3>();
                var touching = new HashSet<string>();
                for (var x = -12.5f; x <= 12.5f; x += 5f)
                for (var z = -2.5f; z <= 62.5f; z += 5f)
                {
                    var bed = BedAt(ctx, x, z, out _);
                    if (bed >= LandColumn) continue;
                    var feet = new Vector3(x, Mathf.Min(bed + BedClearance, ctx.SurfaceY - ctx.Height - 0.5f), z);
                    if (!Fits(ctx, feet, out _)) continue;
                    summary.starts++;
                    var cc = MakeDiver(ctx, feet);
                    try
                    {
                        var tracker = ctx.Field.CreateTracker();
                        var t = cc.transform;
                        var seconds = 0f;
                        var lastMove = feet;
                        var lastMoveAt = 0f;
                        var reached = false;
                        while (seconds < EscapeSeconds)
                        {
                            if (tracker.Classify(new WaterProbe(t.position, t.TransformVector(cc.center), ctx.Radius, ctx.Height, Vector3.up)) != EnvironmentLocomotion.Underwater)
                            {
                                reached = true;
                                break;
                            }
                            cc.Move(input * ctx.SwimSpeed * Dt);
                            seconds += Dt;
                            if (Vector3.Distance(t.position, lastMove) > 0.05f) { lastMove = t.position; lastMoveAt = seconds; }
                            else if (seconds - lastMoveAt > 3f) break;
                        }
                        if (reached)
                        {
                            summary.reachedSurface++;
                            summary.worstSeconds = Mathf.Max(summary.worstSeconds, R(seconds));
                        }
                        else
                        {
                            summary.trapped++;
                            trapped.Add(R(feet));
                            foreach (var name in Touching(ctx, cc).Split(',')) if (name.Length > 0) touching.Add(name);
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(cc.gameObject);
                        Physics.SyncTransforms();
                    }
                }
                summary.trappedFrom = trapped.ToArray();
                summary.trappedTouching = touching.OrderBy(n => n, StringComparer.Ordinal).ToArray();
                yield return summary;
            }
        }

        // ---- 3. the fish ----------------------------------------------------------------------------

        private static FishRow MeasureFish(Ctx ctx, FishActor actor)
        {
            var tuning = actor.Species.Swim;
            var home = actor.transform.position;
            var renderers = actor.GetComponentsInChildren<Renderer>(true);
            var body = renderers.Length == 0 ? 0.3f : renderers.Max(r => Mathf.Max(r.bounds.extents.x, r.bounds.extents.y, r.bounds.extents.z));
            var row = new FishRow
            {
                name = actor.name, species = actor.Species.SpeciesId, home = R(home), wanderRadius = tuning.WanderRadius,
                verticalRadius = tuning.WanderRadius * 0.5f, bodyRadius = R(body), bedAtHome = R(BedAt(ctx, home.x, home.z, out _)),
                homeBand = WaterDepth.TryClassify(ctx.Field.Bodies, home, out var band) ? band : ""
            };

            // The wander envelope: every candidate PickWanderTarget can accept (FishMotion.RandomOffset: |offset| <= 1,
            // |offset.y| <= 0.5, times WanderRadius, inside the water box).
            var hits = new Dictionary<string, int>();
            var total = 0;
            var r = tuning.WanderRadius;
            var clearance = 3f;
            for (var x = -r; x <= r + 1e-3f; x += 0.5f)
            for (var y = -r * 0.5f; y <= r * 0.5f + 1e-3f; y += 0.5f)
            for (var z = -r; z <= r + 1e-3f; z += 0.5f)
            {
                if (x * x + y * y + z * z > r * r + 1e-3f) continue;
                var p = home + new Vector3(x, y, z);
                if (!InSwimBox(ctx, p)) continue;
                total++;
                clearance = Mathf.Min(clearance, Clearance(p, clearance));
                foreach (var name in Physics.OverlapSphere(p, FishClearance, ~0, QueryTriggerInteraction.Ignore).Select(c => c.name).Distinct())
                    hits[name] = hits.TryGetValue(name, out var n) ? n + 1 : 1;
            }
            row.envelopeMinClearance = R(clearance);
            row.envelopeHits = hits.OrderByDescending(k => k.Value).Select(k => $"{k.Key}={R((float)k.Value / total)}").ToArray();

            var seed = StableSeed(actor.name);
            row.wander = RunFish(ctx, "wander", tuning, home, body, seed, FishWanderSeconds, _ => null);
            var flee = new List<Run_>();
            foreach (var (name, offset) in new[]
                     {
                         ("flee-from-north", Vector3.forward), ("flee-from-south", Vector3.back), ("flee-from-east", Vector3.right),
                         ("flee-from-west", Vector3.left), ("flee-from-above", Vector3.up), ("flee-from-below", Vector3.down)
                     })
            {
                var threat = home + offset * 2f;
                flee.Add(RunFish(ctx, name, tuning, home, body, seed, FishFleeSeconds, _ => threat));
            }
            foreach (var (name, offset) in new[]
                     {
                         ("chased-from-south", new Vector3(0f, 0f, -3f)), ("chased-from-above", new Vector3(0f, 3f, 0f)),
                         ("chased-from-shore-above", new Vector3(0f, 2f, -2f))
                     })
            {
                // A diver holding a straight line at the fish, at swim speed, ignoring obstacles: the worst push.
                var chaser = home + offset;
                var speed = ctx.SwimSpeed;
                flee.Add(RunFish(ctx, name, tuning, home, body, seed, FishChaseSeconds, fish =>
                {
                    chaser = Vector3.MoveTowards(chaser, fish, speed * Dt);
                    return chaser;
                }));
            }
            row.flee = flee.ToArray();
            if (ctx.IsCandidate)
            {
                // The same pushes, with the bounds the plan gives FishActor instead of the bare water box.
                row.habitatWander = RunFish(ctx, "wander", tuning, home, body, seed, FishWanderSeconds, _ => null, true);
                var bounded = new List<Run_>();
                foreach (var (name, offset) in new[]
                         {
                             ("flee-from-north", Vector3.forward), ("flee-from-south", Vector3.back), ("flee-from-east", Vector3.right),
                             ("flee-from-west", Vector3.left), ("flee-from-above", Vector3.up), ("flee-from-below", Vector3.down)
                         })
                {
                    var threat = home + offset * 2f;
                    bounded.Add(RunFish(ctx, name, tuning, home, body, seed, FishFleeSeconds, _ => threat, true));
                }
                foreach (var (name, offset) in new[]
                         {
                             ("chased-from-south", new Vector3(0f, 0f, -3f)), ("chased-from-above", new Vector3(0f, 3f, 0f)),
                             ("chased-from-shore-above", new Vector3(0f, 2f, -2f))
                         })
                {
                    var chaser = home + offset;
                    var speed = ctx.SwimSpeed;
                    bounded.Add(RunFish(ctx, name, tuning, home, body, seed, FishChaseSeconds, fish =>
                    {
                        chaser = Vector3.MoveTowards(chaser, fish, speed * Dt);
                        return chaser;
                    }, true));
                }
                row.habitatFlee = bounded.ToArray();
            }
            return row;
        }

        private static Run_ RunFish(Ctx ctx, string scenario, SwimTuning tuning, Vector3 home, float body, int seed, float seconds, Func<Vector3, Vector3?> threatAt, bool habitat = false)
        {
            IWaterBounds water = habitat ? new HabitatWaterBounds(new BoxWater(ctx), home) : (IWaterBounds)new BoxWater(ctx);
            var motion = new FishMotion(tuning, home, new System.Random(seed));
            var run = new Run_ { scenario = scenario, min = home, max = home };
            var entered = new HashSet<string>();
            var pos = home;
            var threats = new List<Vector3>();
            var frames = Mathf.RoundToInt(seconds / Dt);
            var inside = false;
            for (var f = 0; f < frames; f++)
            {
                threats.Clear();
                var threat = threatAt(pos);
                if (threat.HasValue) threats.Add(threat.Value);
                var was = pos;
                pos = motion.Step(pos, threats, Dt, water);
                if (pos == was) run.stalled++;
                run.frames++;
                var bedHere = BedAt(ctx, pos.x, pos.z, out _);
                if (bedHere < LandColumn && pos.y < bedHere) run.belowBed++;
                run.min = Vector3.Min(run.min, pos);
                run.max = Vector3.Max(run.max, pos);
                run.maxDistanceFromHome = Mathf.Max(run.maxDistanceFromHome, Vector3.Distance(pos, home));
                var centre = Physics.OverlapSphere(pos, 0.01f, ~0, QueryTriggerInteraction.Ignore);
                inside = centre.Length > 0;
                if (inside)
                {
                    run.centreInside++;
                    foreach (var c in centre) entered.Add(c.name);
                }
                if (Physics.CheckSphere(pos, body, ~0, QueryTriggerInteraction.Ignore)) run.bodyTouch++;
            }
            run.min = R(run.min);
            run.max = R(run.max);
            run.end = R(pos);
            run.endsInside = inside;
            run.maxDistanceFromHome = R(run.maxDistanceFromHome);
            run.entered = entered.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            return run;
        }

        private static SpeciesVolume[] SummariseSpecies(FishRow[] rows, FishActor[] actors) =>
            rows.GroupBy(f => f.species).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g =>
            {
                var tuning = actors.First(a => a.Species.SpeciesId == g.Key).Species.Swim;
                var flees = g.SelectMany(f => f.flee).ToArray();
                var wanders = g.Select(f => f.wander).ToArray();
                return new SpeciesVolume
                {
                    species = g.Key, count = g.Count(), swimSpeed = tuning.SwimSpeed, fleeSpeed = tuning.FleeSpeed, fleeRadius = tuning.FleeRadius,
                    wanderRadius = tuning.WanderRadius,
                    homeMin = g.Select(f => f.home).Aggregate(Vector3.Min), homeMax = g.Select(f => f.home).Aggregate(Vector3.Max),
                    wanderMin = wanders.Select(w => w.min).Aggregate(Vector3.Min), wanderMax = wanders.Select(w => w.max).Aggregate(Vector3.Max),
                    fleeMin = flees.Select(w => w.min).Aggregate(Vector3.Min), fleeMax = flees.Select(w => w.max).Aggregate(Vector3.Max),
                    wanderFractionInside = R((float)wanders.Sum(w => w.centreInside) / Mathf.Max(1, wanders.Sum(w => w.frames))),
                    fleeFractionInside = R((float)flees.Sum(w => w.centreInside) / Mathf.Max(1, flees.Sum(w => w.frames))),
                    fleeFractionBelowBed = R((float)flees.Sum(w => w.belowBed) / Mathf.Max(1, flees.Sum(w => w.frames))),
                    fleeMaxDistance = flees.Max(w => w.maxDistanceFromHome),
                    habitatFleeMin = HabitatRuns(g).Select(w => w.min).DefaultIfEmpty().Aggregate(Vector3.Min),
                    habitatFleeMax = HabitatRuns(g).Select(w => w.max).DefaultIfEmpty().Aggregate(Vector3.Max),
                    habitatFractionInside = R((float)HabitatRuns(g).Sum(w => w.centreInside) / Mathf.Max(1, HabitatRuns(g).Sum(w => w.frames))),
                    habitatFractionBelowBed = R((float)HabitatRuns(g).Sum(w => w.belowBed) / Mathf.Max(1, HabitatRuns(g).Sum(w => w.frames))),
                    habitatFractionStalled = R((float)HabitatRuns(g).Sum(w => w.stalled) / Mathf.Max(1, HabitatRuns(g).Sum(w => w.frames))),
                    habitatMaxDistance = HabitatRuns(g).Select(w => w.maxDistanceFromHome).DefaultIfEmpty().Max()
                };
            }).ToArray();

        private static Run_[] HabitatRuns(IEnumerable<FishRow> rows) =>
            rows.Where(f => f.habitatFlee != null).SelectMany(f => f.habitatFlee).ToArray();

        // ---- 4. what must stay open -----------------------------------------------------------------

        // A diver's eye 6 and 12 m away on eight headings, wherever the capsule fits: is the line to the target clear?
        private static IEnumerable<Sight> MeasureSights(Ctx ctx, FishRow[] fish)
        {
            var targets = fish.Select(f => (f.name, f.home)).ToList();
            targets.Add(("boss", DeepEncounterWorld.BossPosition));
            targets.Add(("arena", DeepEncounterWorld.ArenaPosition));
            for (var i = 0; i < DeepEncounterWorld.TraceIds.Length; i++)
                targets.Add((DeepEncounterWorld.TraceIds[i], DeepEncounterWorld.TracePositions[i]));
            foreach (var (name, point) in targets)
            {
                var sight = new Sight { target = name, point = R(point), bedBelow = R(BedAt(ctx, point.x, point.z, out _)), surfaceAbove = R(ctx.SurfaceY - point.y) };
                var blockers = new HashSet<string>();
                foreach (var d in new[] { 6f, 12f })
                    for (var i = 0; i < 8; i++)
                    {
                        var eye = point + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * d;
                        var feet = eye - Vector3.up * ctx.Eye;
                        if (!InSwimBox(ctx, feet) || !Fits(ctx, feet, out _)) continue;
                        sight.viewpoints++;
                        if (Physics.Linecast(eye, point, out var hit, ~0, QueryTriggerInteraction.Ignore)) blockers.Add(hit.collider.name);
                        else sight.clear++;
                    }
                sight.blockers = blockers.OrderBy(n => n, StringComparer.Ordinal).ToArray();
                yield return sight;
            }
        }

        private static IEnumerable<Protected> MeasureProtected(Ctx ctx)
        {
            Protected Sphere(string id, string source, Vector3 c, float r, string rule) => new Protected
            {
                id = id, source = source, rule = rule, min = R(c - Vector3.one * r), max = R(c + Vector3.one * r), bedBelow = R(BedAt(ctx, c.x, c.z, out _))
            };
            yield return Sphere("boss-arena", "DeepEncounterWorld.ArenaPosition/ArenaRadius", DeepEncounterWorld.ArenaPosition, DeepEncounterWorld.ArenaRadius,
                "a diver's transform (feet) must be able to enter this sphere from open water; deep band; same cell");
            yield return new Protected
            {
                id = "boss-target", source = "DeepEncounterWorld.BossPosition, capsule scale 1.7/2.4/1.7", rule = "harpoon line must reach the capsule",
                min = R(DeepEncounterWorld.BossPosition - new Vector3(0.85f, 2.4f, 0.85f)), max = R(DeepEncounterWorld.BossPosition + new Vector3(0.85f, 2.4f, 0.85f)),
                bedBelow = R(BedAt(ctx, DeepEncounterWorld.BossPosition.x, DeepEncounterWorld.BossPosition.z, out _))
            };
            for (var i = 0; i < DeepEncounterWorld.TraceIds.Length; i++)
                yield return Sphere(DeepEncounterWorld.TraceIds[i], "DeepEncounterWorld.TracePositions/TraceRadius", DeepEncounterWorld.TracePositions[i],
                    DeepEncounterWorld.TraceRadius, "reachable; deep band at the trace itself; visible marker not buried");
        }

        private static IEnumerable<RouteRow> MeasureRoutes(Ctx ctx, DiveRoutePath[] routes)
        {
            foreach (var route in routes.OrderBy(r => r.RouteId, StringComparer.Ordinal))
            {
                route.Refresh();
                var definition = route.ToDefinition();
                var points = route.Waypoints.Select(R).ToArray();
                var row = new RouteRow
                {
                    id = route.RouteId, anchor = definition.AnchorPointAnchor, requiredClass = definition.RequiredVehicleClass.ToString(), waypoints = points,
                    minBedClearanceUnderKeel = float.MaxValue
                };
                for (var leg = 1; leg + 1 < points.Length; leg++)
                    for (var t = 0f; t <= 1f; t += 0.05f)
                    {
                        var p = Vector3.Lerp(points[leg], points[leg + 1], t);
                        var bed = BedAt(ctx, p.x, p.z, out _);
                        if (bed >= LandColumn) continue;
                        row.minBedClearanceUnderKeel = Mathf.Min(row.minBedClearanceUnderKeel, ctx.SurfaceY - 0.5f - bed);
                    }
                row.minBedClearanceUnderKeel = R(row.minBedClearanceUnderKeel);
                var end = points[points.Length - 1];
                row.bedAtAnchor = R(BedAt(ctx, end.x, end.z, out _));
                row.bandAtAnchorBed = WaterDepth.TryClassify(ctx.Field.Bodies, new Vector3(end.x, row.bedAtAnchor + 0.1f, end.z), out var band) ? band : "";
                yield return row;
            }
        }

        // For every 1 m column: the highest a new floor may rise and what limits it. The fish envelope is the
        // wander set, not the flee reach: fleeing is unbounded by FishMotion and is reported separately above.
        private static void MeasureCeiling(Ctx ctx, Audit audit, DiveRoutePath[] routes)
        {
            audit.floorLowerBoundY = ctx.SurfaceY - DiveDepthBands.DeepMaxDepth + 1f;
            var limiters = new List<string>();
            var keel = ctx.SurfaceY - audit.hullDraft - HullFootprintMargin;
            var column = ctx.SurfaceY - MinWaterColumn;
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.ResearchVessel, out var hull);
            var halfWidth = hull.Width * 0.5f + 0.25f;
            var halfLength = hull.Length * 0.5f + 0.25f;
            var rows = new List<CeilingRow>();
            var fish = audit.fish;
            var protectedVolumes = audit.protectedVolumes;
            var routeLegs = routes.SelectMany(r =>
            {
                var w = r.Waypoints;
                return Enumerable.Range(0, w.Count - 1).Select(i => (id: r.RouteId, a: w[i], b: w[i + 1]));
            }).ToArray();

            for (var z = -14.5f; z <= 64.5f; z += 1f)
            {
                var row = new CeilingRow { z = z, maxFloorY = new float[audit.gridXs.Length], limiter = new int[audit.gridXs.Length] };
                for (var i = 0; i < audit.gridXs.Length; i++)
                {
                    var x = audit.gridXs[i];
                    var best = column;
                    var why = "water-column";
                    foreach (var f in fish)
                    {
                        var rho = Mathf.Max(0f, new Vector2(x - f.home.x, z - f.home.z).magnitude - 0.71f);
                        var r = f.wanderRadius;
                        if (rho > r) continue;
                        var drop = Mathf.Min(r * 0.5f, Mathf.Sqrt(r * r - rho * rho));
                        var y = f.home.y - drop - f.bodyRadius - FishClearance;
                        if (y < best) { best = y; why = "fish-wander:" + f.species; }
                    }
                    foreach (var p in protectedVolumes)
                    {
                        var margin = 0.71f;
                        if (x < p.min.x - margin || x > p.max.x + margin || z < p.min.z - margin || z > p.max.z + margin) continue;
                        if (p.min.y < best) { best = p.min.y; why = p.id; }
                    }
                    foreach (var leg in routeLegs)
                    {
                        if (FlatDistanceToSegment(new Vector3(x, 0f, z), leg.a, leg.b) > Mathf.Max(halfWidth, halfLength) + 0.71f) continue;
                        if (keel < best) { best = keel; why = "route:" + leg.id; }
                    }
                    var index = limiters.IndexOf(why);
                    if (index < 0) { index = limiters.Count; limiters.Add(why); }
                    row.maxFloorY[i] = R(best);
                    row.limiter[i] = index;
                }
                rows.Add(row);
            }
            audit.ceilingGrid = rows.ToArray();
            audit.limiterTable = limiters.ToArray();
        }

        private static float FlatDistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = new Vector2(b.x - a.x, b.z - a.z);
            var ap = new Vector2(p.x - a.x, p.z - a.z);
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            return (ap - ab * t).magnitude;
        }

        private static void MeasureRenderers(Ctx ctx, Audit audit)
        {
            var swim = ctx.Swim.bounds;
            foreach (var r in All<MeshRenderer>(ctx.Scene))
            {
                var b = r.bounds;
                if (b.min.y >= ctx.SurfaceY || b.max.x <= swim.min.x || b.min.x >= swim.max.x || b.max.z <= swim.min.z || b.min.z >= swim.max.z) continue;
                var mesh = r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                audit.sceneRenderersInWater++;
                if (mesh == null) continue;
                for (var s = 0; s < mesh.subMeshCount; s++) audit.sceneTrianglesInWater += (int)(mesh.GetIndexCount(s) / 3);
            }
        }

        // ---- 5. the plan's floor, in memory -----------------------------------------------------------

        private static readonly string[] OldFloor =
        {
            "SeaFloor", "Reef_DropOff", "Reef_Shelf", "Reef_Head_0", "Reef_Head_1", "Reef_Head_2", "Deep_DropOff", "Deep_Basin"
        };

        private static float FloorHeight(float x, float z) => SeabedProfile.FloorHeight(x, z);

        // No collider: a diver and a fish may pass through these (id, role, x, z, height).
        private static (string id, string role, float x, float z, float height)[] CandidateDecor() => new[]
        {
            ("reef-plant-seagrass", "shore", 8.5f, 5f, 1.17f),
            ("reef-plant-seagrass", "shore", 12f, 10f, 1.17f),
            ("reef-plant-seagrass", "shore", -13.5f, 13.5f, 1.17f),
            ("reef-coral-fan", "reef A", -9.5f, 22.6f, 1.02f),
            ("reef-coral-mound", "reef A", -5.5f, 23f, 0.46f),
            ("reef-plant-seagrass", "reef A", -4f, 24.5f, 1.17f),
            ("reef-coral-fan", "reef A", -13f, 26.5f, 1.02f),
            ("reef-coral-mound", "reef A", -8f, 25.8f, 0.46f),
            ("reef-plant-seagrass", "reef gate", -8.6f, 32f, 1.17f),
            ("reef-plant-seagrass", "reef gate", -11.2f, 29f, 1.17f),
            ("reef-coral-mound", "reef gate", -2.9f, 30.8f, 0.46f),
            ("reef-coral-fan", "reef B", -5f, 32.2f, 1.02f),
            ("reef-coral-mound", "reef B", -6.5f, 34.5f, 0.46f),
            ("reef-plant-seagrass", "reef B", -2f, 36f, 1.17f),
            ("reef-coral-fan", "reef edge", -3.6f, 36.2f, 1.02f),
            ("reef-coral-fan", "reef edge", -4.2f, 37.6f, 1.02f),
            ("reef-coral-mound", "east shelf", 6f, 27f, 0.46f),
            ("reef-coral-mound", "east shelf", 9f, 33f, 0.46f),
            ("reef-coral-mound", "east shelf", 3f, 34f, 0.46f),
            ("reef-plant-seagrass", "east shelf", 13f, 27f, 1.17f),
            ("reef-coral-fan", "east head", 10f, 20.5f, 1.02f),
            ("reef-coral-mound", "east head", 9f, 23f, 0.46f)
        };

        private static Candidate ApplyCandidate(Ctx ctx)
        {
            foreach (var name in OldFloor) Root(ctx.Scene, name).SetActive(false);

            // Sea bass leave the toe: the apron needs the water their old row used. Same four fish, north of the sand slope.
            var homes = new List<Vector3>();
            for (var index = 1; index < 5; index++)
            {
                var fish = Root(ctx.Scene, $"Fish_SeaBass_{index + 1:00}");
                fish.transform.position = new Vector3(-8f + (index - 1) * 2.4f, 4f, 8f);
                homes.Add(fish.transform.position);
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int nx = 30, nz = 80;
            for (var iz = 0; iz <= nz; iz++)
            for (var ix = 0; ix <= nx; ix++)
                vertices.Add(new Vector3(ix - 15f, FloorHeight(ix - 15f, iz - 15f), iz - 15f));
            var steepest = 0f;
            for (var iz = 0; iz < nz; iz++)
            for (var ix = 0; ix < nx; ix++)
            {
                int a = iz * (nx + 1) + ix, b = a + 1, c = a + nx + 1, d = c + 1;
                triangles.AddRange(new[] { a, c, b, b, c, d });
                var n = Vector3.Cross(vertices[c] - vertices[a], vertices[b] - vertices[a]).normalized;
                steepest = Mathf.Max(steepest, Vector3.Angle(n, Vector3.up));
            }
            var mesh = new Mesh { name = "Seabed_Candidate", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var terrain = new GameObject("Seabed_Terrain");
            SceneManager.MoveGameObjectToScene(terrain, ctx.Scene);
            terrain.AddComponent<MeshCollider>().sharedMesh = mesh;

            var solids = new List<Rock>();
            foreach (var solid in SeabedProfile.Solids)
            {
                var box = solid.Box;
                var id = $"{solid.KitId}@{solid.Scale}";
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Solid_" + id;
                SceneManager.MoveGameObjectToScene(go, ctx.Scene);
                go.transform.position = box.center;
                go.transform.localScale = box.size;
                solids.Add(new Rock { id = id, role = "", min = R(box.min), max = R(box.max) });
            }
            Physics.SyncTransforms();

            return new Candidate
            {
                note = "in memory only: the scene is closed unsaved. Arch boxes are estimates, #222 is not merged here.",
                disabledPieces = OldFloor, seaBassHomes = homes.Select(R).ToArray(), solids = solids.ToArray(),
                terrainTriangles = triangles.Count / 3, steepestDegrees = R(steepest),
                centreProfile = Enumerable.Range(-15, 81).Select(z => new Vector2(z, R(FloorHeight(0f, z)))).ToArray()
            };
        }

        private static void CheckCandidateAgainstCeiling(Candidate candidate, Audit audit)
        {
            var over = new List<string>();
            foreach (var row in audit.ceilingGrid)
                for (var i = 0; i < audit.gridXs.Length; i++)
                {
                    var excess = FloorHeight(audit.gridXs[i], row.z) - row.maxFloorY[i];
                    if (excess <= 0f) continue;
                    candidate.floorOverCeiling++;
                    candidate.worstFloorOverCeiling = Mathf.Max(candidate.worstFloorOverCeiling, R(excess));
                    if (over.Count < 30) over.Add($"x={audit.gridXs[i]} z={row.z} over={R(excess)} by={audit.limiterTable[row.limiter[i]]}");
                }
            candidate.floorOverCeilingSample = over.ToArray();

            candidate.decor = CandidateDecor().Select(d =>
            {
                var ix = Mathf.Clamp(Mathf.RoundToInt(d.x + 14.5f), 0, audit.gridXs.Length - 1);
                var iz = Mathf.Clamp(Mathf.RoundToInt(d.z + 14.5f), 0, audit.ceilingGrid.Length - 1);
                var cell = audit.ceilingGrid[iz];
                var baseY = FloorHeight(d.x, d.z);
                var top = baseY + d.height;
                return new Decor
                {
                    id = d.id, role = d.role, x = d.x, z = d.z, baseY = R(baseY), topY = R(top), ceilingY = cell.maxFloorY[ix],
                    ceilingBy = audit.limiterTable[cell.limiter[ix]], overCeiling = top > cell.maxFloorY[ix]
                };
            }).ToArray();

            candidate.habitatLeaks = audit.fish.SelectMany(f => f.habitatFlee.Concat(new[] { f.habitatWander })
                    .Where(r => r.centreInside > 0 || r.belowBed > 0)
                    .Select(r => $"{f.name} {r.scenario} inside={r.centreInside} below={r.belowBed} of {r.frames} entered={string.Join("+", r.entered)} " +
                                 $"min={r.min} max={r.max}"))
                .ToArray();
        }

        // ---- helpers --------------------------------------------------------------------------------

        private static bool Fits(Ctx ctx, Vector3 feet, out string blocker)
        {
            var a = feet + Vector3.up * ctx.Radius;
            var c = feet + Vector3.up * (ctx.Height - ctx.Radius);
            var hits = Physics.OverlapCapsule(a, c, ctx.Radius, ~0, QueryTriggerInteraction.Ignore);
            blocker = hits.Length > 0 ? string.Join(",", hits.Select(h => h.name).Distinct()) : null;
            return hits.Length == 0;
        }

        // Free radius round a point, up to `limit` (bisected: ClosestPoint does not work on SeaFloor's non-convex mesh).
        private static float Clearance(Vector3 p, float limit)
        {
            if (!Physics.CheckSphere(p, limit, ~0, QueryTriggerInteraction.Ignore)) return limit;
            float lo = 0f, hi = limit;
            for (var i = 0; i < 8; i++)
            {
                var mid = (lo + hi) * 0.5f;
                if (Physics.CheckSphere(p, mid, ~0, QueryTriggerInteraction.Ignore)) hi = mid; else lo = mid;
            }
            return lo;
        }

        // FNV-1a over the name: the same seed in every process.
        private static int StableSeed(string name)
        {
            var h = 2166136261u;
            foreach (var ch in name) h = (h ^ ch) * 16777619u;
            return (int)(h & 0x7fffffff);
        }

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

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static GameObject Root(Scene scene, string name) =>
            All<Transform>(scene).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static float R(float v) => Mathf.Round(v * 1000f) / 1000f;
        private static Vector3 R(Vector3 v) => new Vector3(R(v.x), R(v.y), R(v.z));
    }
}
