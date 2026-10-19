using System;
using System.Collections.Generic;
using System.Globalization;
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
    // #188 (P4.8-B): read-only measurement of the free repair parts, the damaged rowboat's berth and the space a
    // four-player repair needs around it. Same method as P4RouteAudit (#172): the real DiveTestArea is opened
    // additively, the runtime layout/home rig/harbor marks/storefronts are built on top, a real CharacterController
    // (NetworkDiver.prefab values) walks and swims, and the scene is closed unsaved. Candidate repair sites, work
    // points and drop points are temporary measurement objects destroyed before the scene closes: no layout, scene,
    // prefab or anchor changes. Evidence for a proposal, not a design.
    // Coordinates: Unity world space, metres, y up, +z toward the sea (north), +x east.
    public static class P48RepairAreaAudit
    {
        public const string ScenePath = P4RouteAudit.ScenePath;
        public const string DiverPath = P4RouteAudit.DiverPath;
        public const string OutputPath = "docs/reports/evidence/p188-repair/current-repair-area.json";

        private const float Dt = 0.02f;
        private const float WaterSurfaceY = 8.0f;
        private const float WalkY = ContinuousTownLayout.WalkSurfaceY;
        private const float Cell = 0.25f;
        private const float GridMinX = -16f, GridMaxX = 16f, GridMinZ = -24f, GridMaxZ = 0f;
        // Work band: from this far off the hull rim a standing diver reaches the rim with the 2.5 m pickup ray
        // (eye 1.55 m) and has a hand's room to swing; the waiting band is the next ring out.
        private const float WorkMin = 0.45f, WorkMax = 1.5f, WaitMax = 4f;
        // "kapsül 0.86 + pay": two padded capsules side by side plus 0.2 m elbow room.
        private const float WorkerSpacing = 1.06f;
        private const float PassWidth = 0.86f;

        private static readonly string[] SceneWalkSurfaces = { "Beach_Platform", "Beach_Wade", "Shore_Ledge", "Shore_Ramp", "Dock_Town" };
        private static readonly string[] SlopeSurfaces = { "Beach_Wade", "Shore_Ramp" };

        // PR #190 (Mehmet, codex/p48-mehmet-repair-interaction, unmerged): BoatRepairNetworkBinding builds its temporary
        // worksite at the runtime boat pose with these LOCAL offsets. Copied as numbers to show where they land in this
        // world; nothing here depends on that code.
        private static readonly Vector3[] Pr190Slots = { new Vector3(-0.65f, 0.75f, -2.15f), new Vector3(0f, 0.75f, -2.15f), new Vector3(0.65f, 0.75f, -2.15f) };
        private static readonly Vector3[] Pr190Racks = { new Vector3(-0.65f, 0.65f, -4.1f), new Vector3(-0.65f, 0.65f, -4.9f), new Vector3(-0.65f, 0.65f, -5.7f) };
        private static readonly Vector3[] Pr190Stands = { new Vector3(-0.65f, 0f, -3.8f), new Vector3(0f, 0f, -4.2f), new Vector3(0.65f, 0f, -3.8f) };

        [Serializable] public sealed class Box { public string name, kind; public Vector3 min, max; }

        [Serializable] public sealed class Part
        {
            public string id, anchorObject, zone, groundCollider, waterAtPart;
            public Vector3 position, rendererSize, triggerSize;
            public float groundY, depthBelowSurface, nearestDryStandDistance;
            public bool onDryWalkSurface, insideSwimVolume, pickupFromDryLand;
            public Vector3 nearestDryStand;
            public string[] overlapping;
        }

        [Serializable] public sealed class Seat { public string id, zone; public Vector3 position; public bool overSwimVolume; }

        [Serializable] public sealed class Boat
        {
            public string source, zone, note;
            public Vector3 berth;
            public float yaw, width, length, seabedY, waterDepth, gapSternToPierEnd;
            public bool staticBoatInScene, overSwimVolume;
            public Vector2[] footprint;
            public Seat[] seats;
            public Vector3 boardingPoint;
        }

        [Serializable] public sealed class Marker
        {
            public string id, zone, groundCollider, note;
            public Vector3 position;
            public bool onDryWalkSurface, insideSwimVolume, embedded;
            public float distanceToBoarding;
        }

        [Serializable] public sealed class Carry
        {
            public string id, part, label, goal;
            public Vector3 start, end;
            public Vector3[] path;
            public float drivenLength, landLength, swimLength, simSeconds, landSeconds, swimSeconds, straightLine;
            public bool reached, fell, stuck;
            public int frames, blockedFrames, waterTransitions;
            public string modes;
            public string[] failures;
        }

        [Serializable] public sealed class WorkPoint
        {
            public string id, zone;
            public Vector3 position;
            public float distanceToRim, distanceToNearestCorridor, distanceToBoarding;
            public string nearestCorridor;
        }

        [Serializable] public sealed class Corridor
        {
            public string id;
            public Vector2[] line;
            public float widthBefore, widthWithSite, widthWithSiteAndWorkers;
            public Vector2 narrowestAt;
            public string narrowestBetween;
            public bool stillPassable, endpointsFreeWithWorkers;
            public float pathBefore, pathWithSiteAndWorkers;
        }

        [Serializable] public sealed class Site
        {
            public string id, label, surface;
            public Vector3 center;
            public float yaw, dryFraction, waterFraction;
            public Vector2[] footprint;
            public string[] footprintObstacles;
            public int workCells, workCellsOffCorridor, waitCells, waitCellsOffCorridor;
            public float waitAreaOffCorridorM2;
            public WorkPoint[] workers;
            public bool fourWorkersFit, fourWorkersFitOffCorridor;
            public Marker drop;
            public Corridor[] corridors;
            public Carry[] carries, dropCarries;
            public int fishWithin8m;
            public string[] notes;
        }

        [Serializable] public sealed class Sight
        {
            public string from, target;
            public Vector3 eye, targetPoint;
            public float headingYaw, distance, horizontalAngle, verticalAngle, projectedHeightPx1080;
            public bool lineOfSight, inViewAtHeading;
            public string blocker;
        }

        [Serializable] public sealed class Audit
        {
            public string branchBase = "codex/p4-integration @ 6816fba (#185 merged)";
            public string unity = Application.unityVersion;
            public string scene = ScenePath, prefab = DiverPath;
            public string coordinates = "Unity world, metres, y up, +z north (sea), +x east";
            public float radius, height, skin, stepOffset, slopeLimit, walkSpeed, swimSpeed, eyeHeight, verticalFov, horizontalFov16x9, pickupRange;
            public Vector3 capsuleCenter;
            public float dt = Dt, gridCell = Cell, workBandMin = WorkMin, workBandMax = WorkMax, waitBandMax = WaitMax, workerSpacing = WorkerSpacing, passWidth = PassWidth;
            public Box[] bounds;
            public Part[] parts;
            public string[] pickupTargets;
            public Boat boat;
            public Marker[] pr190Worksite;
            public Carry[] carries;
            public Site[] sites;
            public Sight[] sightlines;
            public string[] notMeasured;
        }

        private sealed class Ctx
        {
            public Scene Scene;
            public IPlayerZoneSource Zones;
            public WaterField Water;
            public float Radius, Height, Skin, Step, Slope, Speed, SwimSpeed, Eye, Fov, Pickup;
            public Vector3 Center;
            public HashSet<string> Walk;
            public Bounds[] SwimBoxes;
            public Vector3 Boarding;
            public Vector3 Berth;
            public float BerthYaw;
        }

        private sealed class SiteSpec
        {
            public string Id, Label;
            public Vector3 Center;
            public float Yaw;
            public bool Solid;
        }

        public static void Run()
        {
            cachedMap = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            var cc = prefab.GetComponent<CharacterController>();
            var cam = prefab.GetComponentInChildren<Camera>(true);
            var player = new SerializedObject(prefab.GetComponent<NetworkPlayer>());
            var ctx = new Ctx
            {
                Radius = cc.radius, Height = cc.height, Skin = cc.skinWidth, Step = cc.stepOffset, Slope = cc.slopeLimit, Center = cc.center,
                Speed = player.FindProperty("walkSpeed").floatValue, SwimSpeed = player.FindProperty("swimSpeed").floatValue,
                Pickup = player.FindProperty("pickupRange").floatValue,
                Eye = prefab.transform.InverseTransformPoint(cam.transform.position).y, Fov = cam.fieldOfView,
                Walk = new HashSet<string>(SceneWalkSurfaces.Concat(ContinuousTownLayout.Slabs.Select(b => b.Name))
                    .Concat(ContinuousTownLayout.Fills.Select(b => b.Name)))
            };
            var audit = new Audit
            {
                radius = ctx.Radius, height = ctx.Height, skin = ctx.Skin, stepOffset = ctx.Step, slopeLimit = ctx.Slope,
                walkSpeed = ctx.Speed, swimSpeed = ctx.SwimSpeed, eyeHeight = ctx.Eye, verticalFov = ctx.Fov, pickupRange = ctx.Pickup,
                capsuleCenter = ctx.Center,
                horizontalFov16x9 = 2f * Mathf.Atan(Mathf.Tan(ctx.Fov * 0.5f * Mathf.Deg2Rad) * 16f / 9f) * Mathf.Rad2Deg
            };

            var prior = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            ctx.Scene = scene;
            try
            {
                ContinuousTownLayout.Build(scene);
                HomeSceneSetup.EnsureRig(scene);
                CoastalHarborLink.BuildMarkers(scene);
                foreach (var service in All<ServicePointAnchor>(scene)) TownStorefront.Build(service);
                Physics.SyncTransforms();
                ctx.Zones = All<WorldZoneField>(scene).Single();
                ctx.Water = All<WaterField>(scene).Single();
                ctx.SwimBoxes = All<SwimVolume>(scene).Select(s => s.GetComponent<BoxCollider>().bounds).ToArray();
                ctx.Boarding = All<RouteAnchor>(scene).Single(a => a.AnchorId == DiveRouteAnchors.Dock).BoardingPosition;

                audit.bounds = CollectBounds(ctx).ToArray();
                audit.boat = MeasureBoat(ctx);
                audit.parts = All<BoatPartAnchor>(scene).OrderBy(p => BoatRepairParts.All.ToList().IndexOf(p.PartId)).Select(p => MeasurePart(ctx, p)).ToArray();
                audit.pickupTargets = All<MonoBehaviour>(scene).Where(m => m is IBoatPartPickupTarget)
                    .Select(m => Named(m.transform) + " (" + m.GetType().Name + ")").OrderBy(s => s, StringComparer.Ordinal).ToArray();
                audit.pr190Worksite = Pr190(ctx).ToArray();

                var sites = new[]
                {
                    new SiteSpec { Id = "S0", Label = "current runtime berth: route-near-1 wp0 (rowboat in the water at the pier end)", Center = ctx.Berth, Yaw = ctx.BerthYaw, Solid = false },
                    new SiteSpec { Id = "S1", Label = "beach east, between the gate line and the ramp, hull parallel to the shore", Center = new Vector3(3.6f, WalkY, -12.6f), Yaw = 90f, Solid = true },
                    new SiteSpec { Id = "S2", Label = "beach west, between the hull part and the wade, hull parallel to the shore", Center = new Vector3(-2.2f, WalkY, -12.6f), Yaw = 90f, Solid = true },
                    new SiteSpec { Id = "S3", Label = "street plaza east of the gate line (dry, off the beach strip)", Center = new Vector3(5.5f, WalkY, -19f), Yaw = 90f, Solid = true }
                };
                audit.sites = sites.Select(s => MeasureSite(ctx, s, audit.parts)).ToArray();
                audit.carries = audit.sites.First(s => s.id == "S0").carries.Concat(SwimToBerth(ctx, audit.parts)).ToArray();
                audit.sightlines = Sightlines(ctx, audit, audit.horizontalFov16x9).ToArray();
                audit.notMeasured = new[]
                {
                    "real 1/2/4-process walks, same-part races, late join, network latency, build frame time (issue #188 item 7)",
                    "carry speed: no carry slowdown exists in integration or PR #190, so carrying = walkSpeed/swimSpeed",
                    "swim legs use WaterField's own tracker (deadband) with swimSpeed and fins level 0; current drift is not applied",
                    "fog/day-night readability: sight lines are geometric only (no fog, light or contrast)",
                    "candidate sites S1-S3 are temporary 2.4 x 0.9 x 5 m solid boxes (conservative: PR #190's worksite is collider-less)",
                    "workers are temporary capsules of the real radius at fixed points; real crowding/animation not simulated"
                };

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P188_REPAIR_AUDIT_WRITTEN " + OutputPath);
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- bounds near the coast ---------------------------------------------------------------

        private static IEnumerable<Box> CollectBounds(Ctx ctx)
        {
            foreach (var col in All<Collider>(ctx.Scene).OrderBy(c => c.name, StringComparer.Ordinal))
            {
                var b = col.bounds;
                if (b.max.z < -26f || b.min.z > 4f || b.max.y < 4f) continue;
                if (col.GetComponent<SwimVolume>() != null) { yield return MakeBox(col.name, "water.SwimVolume", b); continue; }
                if (col.isTrigger)
                {
                    if (col.name == "DiveExit") yield return MakeBox(col.name, "trigger.DiveExit", b);
                    else if (col.GetComponentInParent<BoatPartAnchor>() != null) yield return MakeBox(Named(col.transform), "trigger.BoatPart", b);
                    continue;
                }
                string kind;
                if (ctx.Walk.Contains(col.name)) kind = "walk";
                else if (col.name.StartsWith("Edge_", StringComparison.Ordinal)) kind = "edge";
                else kind = "solid";
                yield return MakeBox(Named(col.transform), kind, b);
            }
        }

        private static Box MakeBox(string name, string kind, Bounds b) => new Box { name = name, kind = kind, min = R(b.min), max = R(b.max) };

        // ---- boat --------------------------------------------------------------------------------

        private static Boat MeasureBoat(Ctx ctx)
        {
            var path = All<DiveRoutePath>(ctx.Scene).Single(p => p.RouteId == BoatTripIds.NearRouteId);
            var forward = Vector3.ProjectOnPlane(path.Waypoints[1] - path.Waypoints[0], Vector3.up).normalized;
            ctx.Berth = path.Waypoints[0] + forward * BoatHullSeatRules.DockOffsetMeters(BoatHullKind.Rowboat);
            ctx.BerthYaw = Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var rotation = Quaternion.Euler(0f, ctx.BerthYaw, 0f);
            var seats = new List<Seat>();
            foreach (var id in new[] { BoatTripIds.Seat0, BoatTripIds.Seat1, BoatTripIds.Seat2, BoatTripIds.Seat3 })
            {
                BoatHullSeatRules.TryResolveLocalOffset(BoatHullKind.Rowboat, id, out var local);
                var p = ctx.Berth + rotation * local;
                seats.Add(new Seat { id = id, position = R(p), zone = ctx.Zones.ResolveZone(p).ToString(), overSwimVolume = InSwim(ctx, new Vector3(p.x, WaterSurfaceY - 0.05f, p.z)) });
            }
            var dock = Root(ctx.Scene, "Dock_Town").GetComponent<Collider>().bounds;
            var stern = ctx.Berth - forward * size.Length * 0.5f;
            Physics.Raycast(new Vector3(ctx.Berth.x, WaterSurfaceY - 0.1f, ctx.Berth.z), Vector3.down, out var bed, 60f, ~0, QueryTriggerInteraction.Ignore);
            return new Boat
            {
                source = "route-near-1 wp0 + BoatHullSeatRules.DockOffsetMeters(Rowboat) along wp0->wp1 (NetworkBoatController.SnapToDockRouteStart)",
                berth = R(ctx.Berth), yaw = R(ctx.BerthYaw), width = size.Width, length = size.Length,
                zone = ctx.Zones.ResolveZone(ctx.Berth).ToString(),
                overSwimVolume = InSwim(ctx, new Vector3(ctx.Berth.x, WaterSurfaceY - 0.05f, ctx.Berth.z)),
                seabedY = R(bed.point.y), waterDepth = R(WaterSurfaceY - bed.point.y),
                gapSternToPierEnd = R(stern.z - dock.max.z),
                footprint = Footprint(ctx.Berth, ctx.BerthYaw, size.Width, size.Length).Select(v => new Vector2(R(v.x), R(v.y))).ToArray(),
                seats = seats.ToArray(), boardingPoint = R(ctx.Boarding),
                staticBoatInScene = All<Transform>(ctx.Scene).Any(t => t.GetComponent("NetworkBoatController") != null || t.GetComponent<BoatHullPresentation>() != null),
                note = "No damaged boat exists in the scene; BoatTripNetworkBinding hides the runtime hull until BoatRepair is Repaired. PR #190 draws a temporary broken-hull worksite at this pose."
            };
        }

        private static IEnumerable<Marker> Pr190(Ctx ctx)
        {
            var rotation = Quaternion.Euler(0f, ctx.BerthYaw, 0f);
            for (var i = 0; i < 3; i++) yield return Mark(ctx, "pr190-slot-" + i, ctx.Berth + rotation * Pr190Slots[i], "repair slot trigger (0.59 x 0.8 x 0.15)", false);
            for (var i = 0; i < 3; i++) yield return Mark(ctx, "pr190-rack-" + i, ctx.Berth + rotation * Pr190Racks[i], "recovered material on the shared rack (0.6 x 0.14 x 0.45)", true);
            for (var i = 0; i < 3; i++) yield return Mark(ctx, "pr190-stand-" + i, ctx.Berth + rotation * Pr190Stands[i], "StandPosition(i) (smoke driver stand)", true);
        }

        private static Marker Mark(Ctx ctx, string id, Vector3 p, string note, bool groundIt)
        {
            var m = new Marker { id = id, note = note, distanceToBoarding = R(Vector2.Distance(XZ(p), XZ(ctx.Boarding))) };
            if (Physics.Raycast(new Vector3(p.x, 12f, p.z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                m.groundCollider = Named(hit.collider.transform);
                m.onDryWalkSurface = ctx.Walk.Contains(hit.collider.name) && hit.point.y >= WaterSurfaceY + 0.01f;
                if (groundIt && m.onDryWalkSurface) p.y = Mathf.Max(p.y, hit.point.y);
            }
            else m.groundCollider = "none";
            m.position = R(p);
            m.zone = ctx.Zones.ResolveZone(p).ToString();
            m.insideSwimVolume = InSwim(ctx, p + Vector3.up * 0.05f);
            m.embedded = Physics.OverlapBox(p + Vector3.up * 0.3f, new Vector3(0.3f, 0.2f, 0.3f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => !ctx.Walk.Contains(c.name));
            return m;
        }

        // ---- parts -------------------------------------------------------------------------------

        private static Part MeasurePart(Ctx ctx, BoatPartAnchor anchor)
        {
            var p = anchor.WorldPosition;
            var part = new Part { id = anchor.PartId, anchorObject = Named(anchor.transform), position = R(p), zone = ctx.Zones.ResolveZone(p).ToString() };
            var r = anchor.GetComponentInChildren<Renderer>();
            part.rendererSize = r != null ? R(r.bounds.size) : Vector3.zero;
            var t = anchor.GetComponentInChildren<Collider>();
            part.triggerSize = t != null ? R(t.bounds.size) : Vector3.zero;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var floor, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                part.groundCollider = Named(floor.collider.transform);
                part.groundY = R(floor.point.y);
                part.onDryWalkSurface = ctx.Walk.Contains(floor.collider.name) && floor.point.y >= WaterSurfaceY + 0.01f;
            }
            part.insideSwimVolume = InSwim(ctx, p);
            part.depthBelowSurface = R(Mathf.Max(0f, WaterSurfaceY - p.y));
            var probe = new WaterProbe(new Vector3(p.x, floor.point.y, p.z), ctx.Center, ctx.Radius, ctx.Height, Vector3.up);
            part.waterAtPart = WaterRules.ClassifyRaw(ctx.Water.Bodies, probe) + " (raw class of a diver standing on the ground at the part)";
            part.overlapping = Physics.OverlapSphere(p + Vector3.up * 0.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h != floor.collider && !h.transform.IsChildOf(anchor.transform)).Select(h => h.name).ToArray();

            // Nearest dry stand from which the 2.5 m pickup ray reaches the part with a clear line of sight.
            var map = StandMap(ctx);
            var best = float.MaxValue;
            foreach (var kv in map)
            {
                var feet = kv.Value;
                var eye = feet + Vector3.up * ctx.Eye;
                var d = Vector3.Distance(eye, r != null ? r.bounds.center : p);
                if (d > ctx.Pickup) continue;
                var flat = Vector2.Distance(XZ(feet), XZ(p));
                if (flat >= best) continue;
                var aim = r != null ? r.bounds.center : p + Vector3.up * 0.3f;
                var to = aim - eye;
                var blocked = Physics.RaycastAll(eye, to.normalized, Mathf.Max(0f, to.magnitude - 0.3f), ~0, QueryTriggerInteraction.Ignore).Any(h => !h.transform.IsChildOf(anchor.transform));
                if (blocked) continue;
                best = flat;
                part.nearestDryStand = R(feet);
            }
            part.pickupFromDryLand = best < float.MaxValue;
            part.nearestDryStandDistance = part.pickupFromDryLand ? R(best) : -1f;
            return part;
        }

        // ---- the standable grid ------------------------------------------------------------------

        private static Dictionary<Vector2Int, Vector3> cachedMap;

        private static Dictionary<Vector2Int, Vector3> StandMap(Ctx ctx)
        {
            if (cachedMap == null) cachedMap = BuildStandMap(ctx);
            return cachedMap;
        }

        // Every 0.25 m cell where a padded capsule can stand on DRY walk ground (above the water surface, outside
        // the SwimVolume), clear of every non-walk collider.
        private static Dictionary<Vector2Int, Vector3> BuildStandMap(Ctx ctx)
        {
            var map = new Dictionary<Vector2Int, Vector3>();
            for (var x = GridMinX; x <= GridMaxX + 1e-4f; x += Cell)
                for (var z = GridMinZ; z <= GridMaxZ + 1e-4f; z += Cell)
                {
                    if (!Stand(ctx, new Vector2(x, z), out var feet)) continue;
                    map[Key(x, z)] = feet;
                }
            return map;
        }

        private static bool Stand(Ctx ctx, Vector2 p, out Vector3 feet)
        {
            feet = default;
            if (!Physics.Raycast(new Vector3(p.x, 12f, p.y), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (!ctx.Walk.Contains(hit.collider.name) || hit.point.y < WaterSurfaceY + 0.01f) return false;
            feet = hit.point;
            if (InSwim(ctx, feet + Vector3.up * 0.05f)) return false;
            foreach (var o in new[] { new Vector2(0.2f, 0f), new Vector2(-0.2f, 0f), new Vector2(0f, 0.2f), new Vector2(0f, -0.2f) })
            {
                if (!Physics.Raycast(new Vector3(p.x + o.x, 12f, p.y + o.y), Vector3.down, out var rim, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
                if (!ctx.Walk.Contains(rim.collider.name) || Mathf.Abs(rim.point.y - feet.y) > ctx.Step) return false;
            }
            var r = ctx.Radius + ctx.Skin;
            return !Physics.OverlapCapsule(feet + Vector3.up * (r + ctx.Step), feet + Vector3.up * (ctx.Height - r), r, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => !ctx.Walk.Contains(c.name));
        }

        private static Vector2Int Key(float x, float z) => new Vector2Int(Mathf.RoundToInt(x / Cell), Mathf.RoundToInt(z / Cell));

        // Breadth-first path over standable cells (8-connected, no corner cutting, step <= stepOffset), string-pulled.
        private static List<Vector3> FindPath(Ctx ctx, Dictionary<Vector2Int, Vector3> map, Vector3 from, Vector3 to)
        {
            var start = Nearest(map, from);
            var goal = Nearest(map, to);
            if (!start.HasValue || !goal.HasValue) return null;
            var came = new Dictionary<Vector2Int, Vector2Int> { [start.Value] = start.Value };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start.Value);
            while (queue.Count > 0 && !came.ContainsKey(goal.Value))
            {
                var c = queue.Dequeue();
                for (var dx = -1; dx <= 1; dx++)
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dz);
                        if (came.ContainsKey(n) || !map.TryGetValue(n, out var ny)) continue;
                        if (dx != 0 && dz != 0 && (!map.ContainsKey(new Vector2Int(c.x + dx, c.y)) || !map.ContainsKey(new Vector2Int(c.x, c.y + dz)))) continue;
                        if (Mathf.Abs(ny.y - map[c].y) > ctx.Step) continue;
                        came[n] = c;
                        queue.Enqueue(n);
                    }
            }
            if (!came.ContainsKey(goal.Value)) return null;
            var cells = new List<Vector2Int>();
            for (var c = goal.Value; ; c = came[c]) { cells.Add(c); if (c == start.Value) break; }
            cells.Reverse();
            // String pulling: keep a cell only when the straight run from the last kept one leaves the standable set.
            var kept = new List<Vector3> { map[cells[0]] };
            var anchor = 0;
            for (var i = 2; i < cells.Count; i++)
                if (!StraightClear(ctx, map[cells[anchor]], map[cells[i]])) { kept.Add(map[cells[i - 1]]); anchor = i - 1; }
            kept.Add(map[cells[cells.Count - 1]]);
            return kept;
        }

        private static bool StraightClear(Ctx ctx, Vector3 a, Vector3 b)
        {
            var n = Mathf.CeilToInt(Vector2.Distance(XZ(a), XZ(b)) / (Cell * 0.5f));
            for (var i = 0; i <= n; i++)
            {
                var q = Vector3.Lerp(a, b, (float)i / Mathf.Max(1, n));
                if (!Stand(ctx, XZ(q), out var g) || Mathf.Abs(g.y - q.y) > ctx.Step + 0.2f) return false;
            }
            return true;
        }

        private static Vector2Int? Nearest(Dictionary<Vector2Int, Vector3> map, Vector3 p)
        {
            Vector2Int? best = null;
            var bestD = float.MaxValue;
            foreach (var kv in map)
            {
                var d = Vector2.SqrMagnitude(XZ(kv.Value) - XZ(p));
                if (d < bestD) { bestD = d; best = kv.Key; }
            }
            return best;
        }

        // ---- carrying (walk + swim, real CharacterController) ------------------------------------

        // Mixed driver: each frame WaterField's own per-diver tracker classifies the capsule; Land walks at walkSpeed
        // with gravity toward the target's XZ, Surface/Underwater swim at swimSpeed toward the 3D target (Surface cannot
        // rise, like PlayerPresentationRules.FilterMoveInput).
        private static Carry Drive(Ctx ctx, string id, string partId, string label, string goal, List<Vector3> waypoints)
        {
            var carry = new Carry
            {
                id = id, part = partId, label = label, goal = goal, path = waypoints.Select(R).ToArray(),
                start = R(waypoints[0]), straightLine = R(Vector2.Distance(XZ(waypoints[0]), XZ(waypoints[waypoints.Count - 1])))
            };
            var failures = new List<string>();
            var modes = new List<string>();
            var go = new GameObject("Audit_P188_Diver");
            SceneManager.MoveGameObjectToScene(go, ctx.Scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = ctx.Height; cc.radius = ctx.Radius; cc.center = ctx.Center;
                cc.slopeLimit = ctx.Slope; cc.stepOffset = ctx.Step; cc.skinWidth = ctx.Skin;
                var t = go.transform;
                t.position = waypoints[0] + Vector3.up * ctx.Skin;
                Physics.SyncTransforms();
                var tracker = ctx.Water.CreateTracker();
                var vy = 0f;
                var last = "";
                for (var w = 1; w < waypoints.Count && !carry.stuck; w++)
                {
                    var target = waypoints[w];
                    var guard = Mathf.CeilToInt(Vector3.Distance(waypoints[w - 1], target) / (Mathf.Min(ctx.Speed, ctx.SwimSpeed) * Dt)) * 3 + 150;
                    while (true)
                    {
                        var env = tracker.Classify(new WaterProbe(t.position, ctx.Center, ctx.Radius, ctx.Height, Vector3.up));
                        var land = env == EnvironmentLocomotion.Land;
                        var mode = land ? "walk" : env == EnvironmentLocomotion.Surface ? "surface" : "underwater";
                        if (mode != last) { if (last != "") carry.waterTransitions++; modes.Add(mode); last = mode; }
                        var here = t.position;
                        var flat = XZ(target) - XZ(here);
                        var arrived = flat.magnitude <= (land ? 0.05f : 0.15f);
                        if (arrived) break;
                        if (--guard < 0)
                        {
                            carry.stuck = true;
                            failures.Add($"stuck at ({here.x:0.##}, {here.y:0.##}, {here.z:0.##}) {mode} heading to {Fmt(target)}");
                            break;
                        }
                        Vector3 move;
                        if (land)
                        {
                            var step = Vector2.ClampMagnitude(flat, ctx.Speed * Dt);
                            vy = cc.isGrounded ? -1f : vy - 9.81f * Dt;
                            move = new Vector3(step.x, vy * Dt, step.y);
                        }
                        else
                        {
                            vy = 0f;
                            var dir = target.y >= WaterSurfaceY ? new Vector3(flat.x, 0f, flat.y) : target - here;
                            if (env == EnvironmentLocomotion.Surface && dir.y > 0f) dir.y = 0f;
                            move = Vector3.ClampMagnitude(dir, ctx.SwimSpeed * Dt);
                        }
                        cc.Move(move);
                        carry.frames++;
                        var moved = Vector3.Distance(here, t.position);
                        var movedFlat = Vector2.Distance(XZ(here), XZ(t.position));
                        carry.drivenLength += land ? movedFlat : moved;
                        if (land) { carry.landLength += movedFlat; carry.landSeconds += Dt; }
                        else { carry.swimLength += moved; carry.swimSeconds += Dt; }
                        if (moved < new Vector3(move.x, land ? 0f : move.y, move.z).magnitude * 0.5f) carry.blockedFrames++;

                        if (!land) continue;
                        var feet = t.position;
                        if (!Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            carry.fell = true;
                            failures.Add($"over no ground at ({feet.x:0.##}, {feet.z:0.##}) y={feet.y:0.###}");
                        }
                        else if (feet.y < ground.point.y - 0.05f || (!SlopeSurfaces.Contains(ground.collider.name) && ground.point.y < WalkY - 0.05f))
                        {
                            carry.fell = true;
                            failures.Add($"dropped to y={feet.y:0.###} at ({feet.x:0.##}, {feet.z:0.##}) over {ground.collider.name}");
                        }
                    }
                }
                carry.end = R(t.position);
                carry.reached = !carry.stuck && Vector2.Distance(XZ(t.position), XZ(waypoints[waypoints.Count - 1])) <= 0.2f;
                if (!carry.reached && !carry.stuck) failures.Add("did not reach the goal");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
            carry.simSeconds = R(carry.frames * Dt);
            carry.landSeconds = R(carry.landSeconds);
            carry.swimSeconds = R(carry.swimSeconds);
            carry.drivenLength = R(carry.drivenLength);
            carry.landLength = R(carry.landLength);
            carry.swimLength = R(carry.swimLength);
            carry.modes = string.Join(" > ", modes);
            carry.failures = failures.Distinct().Take(30).ToArray();
            return carry;
        }

        private static Vector3 GroundAt(Vector3 p)
        {
            return Physics.Raycast(new Vector3(p.x, p.y + 2f, p.z), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : p;
        }

        // Every free part to the site's first work point. Dry parts walk the land grid; the submerged part swims to the
        // nearest dry ground first (via the wade, and via the ramp) and then walks.
        private static IEnumerable<Carry> Carries(Ctx ctx, string siteId, Vector3 goal, Part[] parts)
        {
            var map = BuildStandMap(ctx);
            foreach (var part in parts)
            {
                if (part.onDryWalkSurface)
                {
                    var start = GroundAt(part.position);
                    var path = FindPath(ctx, map, start, goal);
                    if (path == null) { yield return Unreachable(siteId + ":" + part.id, part.id, start, goal); continue; }
                    path[0] = start;
                    yield return Drive(ctx, siteId + ":" + part.id, part.id, "walk " + part.id + " -> " + siteId + " work point 0", Fmt(goal), path);
                    continue;
                }
                var seabed = GroundAt(part.position);
                foreach (var (via, waterline, dry) in new[]
                         {
                             ("wade", new Vector3(-6.5f, WaterSurfaceY - 0.25f, -10.0f), new Vector3(-6.5f, WalkY, -11.4f)),
                             ("ramp", new Vector3(5.6f, WaterSurfaceY - 0.25f, -9f), new Vector3(7.2f, WalkY, -9f))
                         })
                {
                    var land = FindPath(ctx, map, dry, goal);
                    if (land == null) { yield return Unreachable(siteId + ":" + part.id + ":" + via, part.id, seabed, goal); continue; }
                    var all = new List<Vector3> { seabed, waterline };
                    all.AddRange(land);
                    yield return Drive(ctx, siteId + ":" + part.id + ":" + via, part.id,
                        "swim " + part.id + " ashore via the " + via + ", walk -> " + siteId + " work point 0", Fmt(goal), all);
                }
            }
        }

        private static IEnumerable<Carry> SwimToBerth(Ctx ctx, Part[] parts)
        {
            var engine = parts.First(p => p.id == BoatRepairParts.Engine);
            var stern = ctx.Berth + Quaternion.Euler(0f, ctx.BerthYaw, 0f) * new Vector3(0f, 0f, -3.1f);
            var wps = new List<Vector3> { GroundAt(engine.position), new Vector3(-6.5f, WaterSurfaceY - 0.6f, -6f), new Vector3(stern.x - 1.6f, WaterSurfaceY - 0.6f, stern.z + 1f) };
            yield return Drive(ctx, "swim:" + engine.id + ":berth", engine.id,
                "swim the submerged part straight to the boat's side in the water (placement from water is rejected in PR #190)", Fmt(wps[2]), wps);
        }

        private static Carry Unreachable(string id, string part, Vector3 from, Vector3 to) => new Carry
        {
            id = id, part = part, label = "no standable path", goal = Fmt(to), start = R(from), end = R(from), stuck = true,
            failures = new[] { "no path on the standable grid" }, path = new Vector3[0], modes = ""
        };

        // ---- sites -------------------------------------------------------------------------------

        private static readonly (string id, Vector2[] line)[] Corridors =
        {
            ("pier: ledge -> DiveExit -> dock-town-1", new[] { new Vector2(9f, -12.5f), new Vector2(9f, -9f), new Vector2(9f, -3.9f) }),
            ("ramp: waterline -> DiveExit", new[] { new Vector2(6.3f, -9f), new Vector2(9f, -9f) }),
            ("wade: waterline -> beach", new[] { new Vector2(-6.5f, -10.3f), new Vector2(-6.5f, -12.5f) }),
            ("gate: street -> beach", new[] { new Vector2(0f, -18f), new Vector2(0f, -12.5f) }),
            ("beach strip: hull part <-> fuel-tank part", new[] { new Vector2(-12f, -12.5f), new Vector2(12.5f, -12.5f) })
        };

        private static Site MeasureSite(Ctx ctx, SiteSpec spec, Part[] parts)
        {
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var site = new Site { id = spec.Id, label = spec.Label, center = R(spec.Center), yaw = R(spec.Yaw) };
            var corners = Footprint(spec.Center, spec.Yaw, size.Width, size.Length);
            site.footprint = corners.Select(v => new Vector2(R(v.x), R(v.y))).ToArray();

            // What the hull would lie on.
            int dry = 0, wet = 0, total = 0;
            var rotation = Quaternion.Euler(0f, spec.Yaw, 0f);
            var surfaces = new HashSet<string>();
            for (var a = -size.Width * 0.5f; a <= size.Width * 0.5f + 1e-4f; a += Cell)
                for (var b = -size.Length * 0.5f; b <= size.Length * 0.5f + 1e-4f; b += Cell)
                {
                    var q = spec.Center + rotation * new Vector3(a, 0f, b);
                    total++;
                    if (!Physics.Raycast(new Vector3(q.x, 12f, q.z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore)) { wet++; continue; }
                    surfaces.Add(hit.collider.name);
                    if (ctx.Walk.Contains(hit.collider.name) && hit.point.y >= WaterSurfaceY + 0.01f && !InSwim(ctx, hit.point + Vector3.up * 0.05f)) dry++;
                    else wet++;
                }
            site.dryFraction = R((float)dry / total);
            site.waterFraction = R((float)wet / total);
            site.surface = string.Join(", ", surfaces.OrderBy(s => s, StringComparer.Ordinal));
            var halfBox = new Vector3(size.Width * 0.5f, 0.45f, size.Length * 0.5f);
            var baseY = spec.Solid ? GroundAt(spec.Center).y : WaterSurfaceY;
            site.footprintObstacles = Physics.OverlapBox(new Vector3(spec.Center.x, baseY + 0.55f, spec.Center.z), halfBox, rotation, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => !ctx.Walk.Contains(c.name)).Select(c => Named(c.transform)).Distinct().ToArray();

            var temporary = new List<GameObject>();
            try
            {
                var before = Corridors.Select(c => Narrowest(ctx, c.line)).ToArray();
                var pathsBefore = Corridors.Select(c => PathLength(ctx, c.line, out _)).ToArray();
                if (spec.Solid)
                {
                    var wreck = new GameObject("Audit_P188_Wreck_" + spec.Id);
                    SceneManager.MoveGameObjectToScene(wreck, ctx.Scene);
                    wreck.transform.SetPositionAndRotation(new Vector3(spec.Center.x, baseY + 0.45f, spec.Center.z), rotation);
                    wreck.AddComponent<BoxCollider>().size = halfBox * 2f;
                    temporary.Add(wreck);
                    Physics.SyncTransforms();
                }
                var withSite = Corridors.Select(c => Narrowest(ctx, c.line)).ToArray();

                // Work and waiting cells around the rim.
                var map = BuildStandMap(ctx);
                var work = new List<(Vector3 feet, float rim, float corridor, string name)>();
                foreach (var feet in map.Values)
                {
                    var rim = RimDistance(spec.Center, spec.Yaw, size.Width, size.Length, XZ(feet));
                    if (rim > WaitMax) continue;
                    var (corridorDistance, corridorName) = NearestCorridor(XZ(feet));
                    if (rim >= WorkMin && rim <= WorkMax) work.Add((feet, rim, corridorDistance, corridorName));
                    else if (rim > WorkMax)
                    {
                        site.waitCells++;
                        if (corridorDistance >= PassWidth) site.waitCellsOffCorridor++;
                    }
                }
                site.workCells = work.Count;
                site.workCellsOffCorridor = work.Count(w => w.corridor >= PassWidth);
                site.waitAreaOffCorridorM2 = R(site.waitCellsOffCorridor * Cell * Cell);

                // Four workers: off-corridor cells first, nearest the rim first, then spread round the hull.
                var chosen = new List<(Vector3 feet, float rim, float corridor, string name)>();
                foreach (var w in work.OrderBy(w => w.corridor >= PassWidth ? 0 : 1).ThenBy(w => Mathf.Round(w.rim * 4f)).ThenBy(w => w.feet.x).ThenBy(w => w.feet.z))
                {
                    if (chosen.Count == 4) break;
                    if (chosen.Any(c => Vector2.Distance(XZ(c.feet), XZ(w.feet)) < WorkerSpacing)) continue;
                    chosen.Add(w);
                }
                site.workers = chosen.Select((c, i) => new WorkPoint
                {
                    id = spec.Id + "-work-" + i, position = R(c.feet), zone = ctx.Zones.ResolveZone(c.feet).ToString(), distanceToRim = R(c.rim),
                    distanceToNearestCorridor = R(c.corridor), nearestCorridor = c.name, distanceToBoarding = R(Vector2.Distance(XZ(c.feet), XZ(ctx.Boarding)))
                }).ToArray();
                site.fourWorkersFit = chosen.Count == 4;
                site.fourWorkersFitOffCorridor = chosen.Count == 4 && chosen.All(c => c.corridor >= PassWidth);

                // Safe drop: a dry free cell outside the work band, off every corridor, nearest the hull.
                var drop = map.Values
                    .Select(f => (feet: f, rim: RimDistance(spec.Center, spec.Yaw, size.Width, size.Length, XZ(f)), corridor: NearestCorridor(XZ(f)).distance))
                    .Where(c => c.rim > WorkMax && c.rim <= 10f && c.corridor >= PassWidth + 0.45f && chosen.All(w => Vector2.Distance(XZ(w.feet), XZ(c.feet)) >= WorkerSpacing))
                    .OrderBy(c => c.rim).ThenBy(c => c.feet.x).ThenBy(c => c.feet.z).Select(c => (Vector3?)c.feet).FirstOrDefault();
                if (drop.HasValue) site.drop = Mark(ctx, spec.Id + "-drop", drop.Value, "nearest dry free cell beyond the work band (<= 10 m), >= 1.31 m off every corridor; rim " + R(RimDistance(spec.Center, spec.Yaw, size.Width, size.Length, XZ(drop.Value))) + " m", true);

                site.carries = chosen.Count > 0 ? Carries(ctx, spec.Id, chosen[0].feet, parts).ToArray() : new Carry[0];

                // Workers stand as real-radius capsules; re-measure the corridors.
                for (var i = 0; i < chosen.Count; i++)
                {
                    var worker = new GameObject("Audit_P188_Worker_" + spec.Id + "_" + i);
                    SceneManager.MoveGameObjectToScene(worker, ctx.Scene);
                    worker.transform.position = chosen[i].feet;
                    var capsule = worker.AddComponent<CapsuleCollider>();
                    capsule.radius = ctx.Radius; capsule.height = ctx.Height; capsule.center = new Vector3(0f, ctx.Height * 0.5f, 0f);
                    temporary.Add(worker);
                }
                Physics.SyncTransforms();
                var withWorkers = Corridors.Select(c => Narrowest(ctx, c.line)).ToArray();
                var endsFree = new bool[Corridors.Length];
                var pathsAfter = Corridors.Select((c, i) => PathLength(ctx, c.line, out endsFree[i])).ToArray();
                site.corridors = Corridors.Select((c, i) => new Corridor
                {
                    id = c.id, line = c.line, widthBefore = before[i].width, widthWithSite = withSite[i].width, widthWithSiteAndWorkers = withWorkers[i].width,
                    narrowestAt = withWorkers[i].at, narrowestBetween = withWorkers[i].between, pathBefore = pathsBefore[i], pathWithSiteAndWorkers = pathsAfter[i], endpointsFreeWithWorkers = endsFree[i],
                    stillPassable = endsFree[i] && pathsAfter[i] >= 0f
                }).ToArray();

                site.dropCarries = spec.Id == "S1" && drop.HasValue
                    ? DropCarries(ctx, spec, chosen.Select(c => c.feet).ToList(), temporary.Where(g => g.name.StartsWith("Audit_P188_Worker_", StringComparison.Ordinal)).ToList(), drop.Value).ToArray()
                    : new Carry[0];
            }
            finally
            {
                foreach (var go in temporary) Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }

            site.fishWithin8m = All<FishActor>(ctx.Scene).Count(f => RimDistance(spec.Center, spec.Yaw, size.Width, size.Length, XZ(f.transform.position)) <= 8f);
            var notes = new List<string>();
            if (!spec.Solid) notes.Add("hull floats in the SwimVolume; work cells can only be on dry ground near the pier end");
            if (site.footprintObstacles.Length > 0) notes.Add("footprint overlaps solids: " + string.Join(", ", site.footprintObstacles));
            site.notes = notes.ToArray();
            return site;
        }

        // The proposed drop (repair-drop-0) walked with the real controller, wreck in place: each work point -> drop with the
        // other three workers standing, the cradle rim (nearest standable cell to the cradle centre, no workers) -> drop,
        // and drop -> pier boarding point with workers 1-3 standing (worker 0 is the one who went to the drop).
        private static List<Carry> DropCarries(Ctx ctx, SiteSpec spec, List<Vector3> work, List<GameObject> workers, Vector3 drop)
        {
            var carries = new List<Carry>();
            try
            {
                for (var i = 0; i < work.Count; i++)
                {
                    Standing(workers, w => w != i);
                    carries.Add(DryDrive(ctx, spec.Id + ":work-" + i + ":drop", "walk " + spec.Id + " work point " + i + " -> drop (other workers standing)", work[i], drop));
                }
                Standing(workers, w => false);
                var rim = Nearest(BuildStandMap(ctx), spec.Center);
                if (rim.HasValue)
                    carries.Add(DryDrive(ctx, spec.Id + ":cradle:drop", "walk " + spec.Id + " cradle rim -> drop (no workers)", BuildStandMap(ctx)[rim.Value], drop));
                Standing(workers, w => w != 0);
                carries.Add(DryDrive(ctx, spec.Id + ":drop:boarding", "walk " + spec.Id + " drop -> dock-town-1 boarding (workers 1-3 standing)", drop, ctx.Boarding));
            }
            finally
            {
                Standing(workers, w => true);
            }
            return carries;
        }

        private static void Standing(List<GameObject> workers, Func<int, bool> active)
        {
            for (var i = 0; i < workers.Count; i++) workers[i].SetActive(active(i));
            Physics.SyncTransforms();
        }

        private static Carry DryDrive(Ctx ctx, string id, string label, Vector3 from, Vector3 to)
        {
            var path = FindPath(ctx, BuildStandMap(ctx), from, to);
            if (path == null) return Unreachable(id, "", from, to);
            path[0] = from;
            return Drive(ctx, id, "", label, Fmt(to), path);
        }

        // Walkable length between a corridor's two ends on the standable grid (workers and wreck included); -1 = no path.
        private static float PathLength(Ctx ctx, Vector2[] line, out bool endpointsFree)
        {
            var map = BuildStandMap(ctx);
            var a = new Vector3(line[0].x, WalkY, line[0].y);
            var b = new Vector3(line[line.Length - 1].x, WalkY, line[line.Length - 1].y);
            var na = Nearest(map, a); var nb = Nearest(map, b);
            endpointsFree = na.HasValue && nb.HasValue && Vector2.Distance(XZ(map[na.Value]), line[0]) <= 0.5f && Vector2.Distance(XZ(map[nb.Value]), line[line.Length - 1]) <= 0.5f;
            var path = FindPath(ctx, map, a, b);
            if (path == null) return -1f;
            var length = 0f;
            for (var i = 1; i < path.Count; i++) length += Vector2.Distance(XZ(path[i - 1]), XZ(path[i]));
            return R(length);
        }

        private static (float distance, string name) NearestCorridor(Vector2 p)
        {
            var best = float.MaxValue;
            var name = "";
            foreach (var (id, line) in Corridors)
                for (var i = 0; i + 1 < line.Length; i++)
                {
                    var d = SegmentDistance(p, line[i], line[i + 1]);
                    if (d < best) { best = d; name = id; }
                }
            return (best, name);
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        // Clear width across a corridor every 0.25 m (P4RouteAudit.Narrowest's probes). A sample whose first ground hit
        // is a solid (a hull lying across it) counts as width 0.
        private static (float width, Vector2 at, string between) Narrowest(Ctx ctx, Vector2[] line)
        {
            var best = (width: float.MaxValue, at: Vector2.zero, between: "");
            for (var leg = 0; leg + 1 < line.Length; leg++)
            {
                var a = line[leg];
                var b = line[leg + 1];
                var len = Vector2.Distance(a, b);
                var dir = (b - a) / len;
                var normal = new Vector2(-dir.y, dir.x);
                var count = Mathf.CeilToInt(len / Cell);
                for (var i = 0; i <= count; i++)
                {
                    var along = len * i / count;
                    if ((leg > 0 && along < 0.35f) || (leg + 2 < line.Length && len - along < 0.35f)) continue;
                    var p = a + dir * along;
                    if (!Ground(ctx, p, out var gy, out var top))
                    {
                        if (top != "ground ends" && 0f < best.width) best = (0f, new Vector2(R(p.x), R(p.y)), "blocked by " + top);
                        continue;
                    }
                    if (gy < WaterSurfaceY) continue;
                    var left = Free(ctx, p, gy, normal, out var leftWhy);
                    var right = Free(ctx, p, gy, -normal, out var rightWhy);
                    var width = left + right;
                    if (width < best.width) best = (R(width), new Vector2(R(p.x), R(p.y)), leftWhy + " | " + rightWhy);
                }
            }
            return best;
        }

        private static bool Ground(Ctx ctx, Vector2 p, out float y, out string top)
        {
            y = 0f;
            top = "ground ends";
            if (!Physics.Raycast(new Vector3(p.x, 12f, p.y), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (!ctx.Walk.Contains(hit.collider.name)) { top = Named(hit.collider.transform); return false; }
            y = hit.point.y;
            return true;
        }

        private static float Free(Ctx ctx, Vector2 p, float y, Vector2 dir, out string why)
        {
            const float maxProbe = 15f;
            var d3 = new Vector3(dir.x, 0f, dir.y);
            var obstacle = maxProbe;
            why = "probe " + maxProbe + "m";
            foreach (var h in new[] { 0.35f, 1.0f, 1.6f })
            {
                var origin = new Vector3(p.x, y + h, p.y);
                foreach (var hit in Physics.RaycastAll(origin, d3, maxProbe, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (ctx.Walk.Contains(hit.collider.name) || hit.distance >= obstacle) continue;
                    obstacle = hit.distance;
                    why = Named(hit.collider.transform);
                }
            }
            var last = y;
            for (var s = 0.05f; s < obstacle; s += 0.05f)
            {
                var q = p + dir * s;
                if (!Ground(ctx, q, out var gy, out var top)) { why = top; return s - 0.05f; }
                if (Mathf.Abs(gy - last) > ctx.Step) { why = "step"; return s - 0.05f; }
                if (gy < WaterSurfaceY && ctx.Zones.ResolveZone(new Vector3(q.x, gy, q.y)) == PlayerZone.Sea) { why = "sea"; return s - 0.05f; }
                last = gy;
            }
            return obstacle;
        }

        // ---- sight lines -------------------------------------------------------------------------

        private static IEnumerable<Sight> Sightlines(Ctx ctx, Audit audit, float hFov)
        {
            var views = new[]
            {
                ("home door", ContinuousTownLayout.HomeRig.Door + new Vector3(0f, 0f, 0.3f), 0f),
                ("town square", ContinuousTownLayout.TownSquareCenter, 0f),
                ("beach (middle)", new Vector3(0f, WalkY, -12.5f), 0f),
                ("beach (middle) facing east", new Vector3(0f, WalkY, -12.5f), 90f),
                ("pier", new Vector3(9f, WalkY, -4.5f), 180f)
            };
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var targets = new List<(string name, Bounds bounds, Collider[] own)>();
            foreach (var part in All<BoatPartAnchor>(ctx.Scene))
            {
                var r = part.GetComponentInChildren<Renderer>();
                targets.Add(("part " + part.PartId, r != null ? r.bounds : new Bounds(part.WorldPosition, Vector3.one * 0.6f), part.GetComponentsInChildren<Collider>(true)));
            }
            foreach (var site in audit.sites)
            {
                var y = site.id == "S0" ? WaterSurfaceY : GroundAt(site.center).y;
                var b = new Bounds(new Vector3(site.center.x, y + 0.45f, site.center.z), Vector3.zero);
                foreach (var c in site.footprint) b.Encapsulate(new Vector3(c.x, y + 0.9f, c.y));
                b.Encapsulate(new Vector3(site.center.x, y, site.center.z));
                targets.Add(("site " + site.id + " hull", b, new Collider[0]));
                if (site.drop != null) targets.Add(("site " + site.id + " drop", new Bounds(site.drop.position + Vector3.up * 0.4f, new Vector3(0.8f, 0.8f, 0.6f)), new Collider[0]));
            }
            foreach (var (from, feet, yaw) in views)
            {
                var eye = feet + Vector3.up * ctx.Eye;
                var heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                foreach (var (name, bounds, own) in targets)
                {
                    var candidates = new[] { bounds.center, bounds.center + Vector3.up * bounds.extents.y * 0.9f };
                    string blocker = null;
                    var visible = false;
                    foreach (var point in candidates)
                    {
                        var to0 = point - eye;
                        var hitName = Physics.RaycastAll(eye, to0.normalized, to0.magnitude, ~0, QueryTriggerInteraction.Ignore)
                            .Where(h => !own.Contains(h.collider)).OrderBy(h => h.distance).Select(h => Named(h.collider.transform)).FirstOrDefault();
                        if (hitName == null) { visible = true; break; }
                        blocker ??= hitName;
                    }
                    var to = bounds.center - eye;
                    var flat = Vector3.ProjectOnPlane(to, Vector3.up);
                    var h = Vector3.SignedAngle(heading, flat, Vector3.up);
                    var v = Mathf.Asin(Mathf.Clamp(to.y / to.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
                    var px = 540f * (Mathf.Max(bounds.size.y, 0.1f) / Mathf.Max(0.01f, to.magnitude)) / Mathf.Tan(ctx.Fov * 0.5f * Mathf.Deg2Rad);
                    yield return new Sight
                    {
                        from = from, target = name, eye = R(eye), targetPoint = R(bounds.center), headingYaw = yaw,
                        distance = R(flat.magnitude), horizontalAngle = R(h), verticalAngle = R(v), projectedHeightPx1080 = R(px),
                        lineOfSight = visible, blocker = visible ? null : blocker,
                        inViewAtHeading = Mathf.Abs(h) <= hFov * 0.5f && Mathf.Abs(v) <= ctx.Fov * 0.5f
                    };
                }
            }
        }

        // ---- helpers -----------------------------------------------------------------------------

        private static Vector2[] Footprint(Vector3 center, float yaw, float width, float length)
        {
            var r = Quaternion.Euler(0f, yaw, 0f);
            return new[] { new Vector3(-width, 0f, -length), new Vector3(width, 0f, -length), new Vector3(width, 0f, length), new Vector3(-width, 0f, length) }
                .Select(c => XZ(center + r * (c * 0.5f))).ToArray();
        }

        // Horizontal distance from p to the hull rectangle (0 inside).
        private static float RimDistance(Vector3 center, float yaw, float width, float length, Vector2 p)
        {
            var local = Quaternion.Euler(0f, -yaw, 0f) * new Vector3(p.x - center.x, 0f, p.y - center.z);
            var dx = Mathf.Max(0f, Mathf.Abs(local.x) - width * 0.5f);
            var dz = Mathf.Max(0f, Mathf.Abs(local.z) - length * 0.5f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool InSwim(Ctx ctx, Vector3 p) => ctx.SwimBoxes.Any(b => b.Contains(p));

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static GameObject Root(Scene scene, string name) =>
            All<Transform>(scene).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static string Named(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
        private static float R(float v) => Mathf.Round(v * 1000f) / 1000f;
        private static Vector3 R(Vector3 v) => new Vector3(R(v.x), R(v.y), R(v.z));
        private static string Fmt(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", v.x, v.y, v.z);
    }
}
