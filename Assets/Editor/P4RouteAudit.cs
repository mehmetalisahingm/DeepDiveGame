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
    // #172 (P4.7-B): read-only route, anchor and sight measurement of the CURRENT town. The scene is opened
    // additively, the deterministic layout and runtime furniture (home rig, storefront counters, harbor marks)
    // are built on top, a real CharacterController walks the routes, and the scene is closed unsaved.
    // No layout, scene, prefab or service position changes: this is evidence for a proposal, not a design.
    // Coordinates: Unity world space, metres, y up, +z toward the sea (north), +x east.
    public static class P4RouteAudit
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        public const string OutputPath = "docs/reports/evidence/p172-route/current-routes.json";

        private const float Dt = 0.02f;
        private const float SampleSpacing = 0.25f;
        private const float WaterSurfaceY = 8.0f;
        private const float WalkY = ContinuousTownLayout.WalkSurfaceY;
        private const float MaxProbe = 15f;

        private static readonly string[] SceneWalkSurfaces = { "Beach_Platform", "Beach_Wade", "Shore_Ledge", "Shore_Ramp", "Dock_Town" };
        private static readonly string[] SlopeSurfaces = { "Beach_Wade", "Shore_Ramp" };

        [Serializable] public sealed class Box { public string name, kind; public Vector3 min, max; }
        [Serializable] public sealed class Point { public string id, kind, source, zone; public Vector3 position; public bool inMap; }
        [Serializable] public sealed class Pinch { public string at, between; public float width, margin; public Vector2 center; }

        [Serializable] public sealed class Route
        {
            public string id, label, start, end;
            public Vector2[] waypoints;
            public float plannedLength, drivenLength, simSeconds, secondsAtWalkSpeed;
            public bool expectsSea, reachedSea, fell, stuck;
            public int frames, blockedFrames;
            public float narrowestWidth, narrowestMargin, narrowestOutsideHomeWidth;
            public Vector2 narrowestAt, narrowestOutsideHomeAt;
            public string narrowestBetween, narrowestOutsideHomeBetween;
            public Vector2 endPosition;
            public string[] failures;
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
            public string branchBase = "codex/p4-integration @ c7b2449";
            public string unity = Application.unityVersion;
            public string scene = ScenePath, prefab = DiverPath;
            public string coordinates = "Unity world, metres, y up, +z north (sea), +x east";
            public float radius, height, skin, stepOffset, slopeLimit, walkSpeed, eyeHeight, verticalFov, horizontalFov16x9;
            public float dt = Dt;
            public Box[] bounds;
            public Point[] anchors;
            public int worldAnchorCount;
            public string brokenBoatNote;
            public Route[] routes;
            public Pinch[] pinches;
            public float[] spawnToNearestBedExit;
            public string[] spawnNearestBed;
            public Sight[] sightlines;
            public string[] notMeasured;
        }

        private sealed class Ctx
        {
            public Scene Scene;
            public IPlayerZoneSource Zones;
            public DiveRegionField Region;
            public float Radius, Height, Skin, Step, Slope, Speed, Eye, Fov;
            public HashSet<string> Walk;
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
            var audit = new Audit
            {
                radius = ctx.Radius, height = ctx.Height, skin = ctx.Skin, stepOffset = ctx.Step, slopeLimit = ctx.Slope,
                walkSpeed = ctx.Speed, eyeHeight = ctx.Eye, verticalFov = ctx.Fov,
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
                ctx.Region = All<DiveRegionField>(scene).Single();

                audit.bounds = CollectBounds(ctx).ToArray();
                audit.anchors = CollectAnchors(ctx, out audit.worldAnchorCount, out audit.brokenBoatNote).ToArray();
                audit.routes = WalkRoutes(ctx, audit.anchors).ToArray();
                audit.pinches = HomePinches(ctx).ToArray();
                SpawnBedExits(ctx, out audit.spawnToNearestBedExit, out audit.spawnNearestBed);
                audit.sightlines = Sightlines(ctx, audit.anchors, audit.horizontalFov16x9).ToArray();
                audit.notMeasured = new[]
                {
                    "swimming legs (sea part of the wade/ramp routes): swim speed and buoyancy are not the walk controller",
                    "real 1/2/4-process walks, network latency, build frame time",
                    "fog/day-night readability: sight lines are geometric only (no fog, light or contrast)",
                    "turning, stopping at counters and UI time: times are pure locomotion at walkSpeed",
                    "P4.5 DevelopmentVisuals props (collider-less by design) and the runtime boat hull itself"
                };

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P172_ROUTE_AUDIT_WRITTEN " + OutputPath);
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // ---- bounds ------------------------------------------------------------------------------

        private static IEnumerable<Box> CollectBounds(Ctx ctx)
        {
            foreach (var col in All<Collider>(ctx.Scene).OrderBy(c => c.name, StringComparer.Ordinal))
            {
                var b = col.bounds;
                if (col.GetComponent<SwimVolume>() != null) { yield return MakeBox(col.name, "water.SwimVolume", b); continue; }
                if (col.isTrigger)
                {
                    if (col.name == "DiveExit") yield return MakeBox(col.name, "trigger.DiveExit", b);
                    continue;
                }
                if (b.max.y < 4f) continue; // reef/seafloor geometry below the coast is not part of the walk map
                string kind;
                if (ctx.Walk.Contains(col.name)) kind = "walk";
                else if (col.name.StartsWith("Edge_", StringComparison.Ordinal)) kind = "edge";
                else if (col.name.StartsWith("Wall_", StringComparison.Ordinal)) kind = "arenaWall";
                else if (col.name.StartsWith("Home", StringComparison.Ordinal) || col.name.StartsWith("MainBuilding", StringComparison.Ordinal)) kind = "structure";
                else if (col.name == "Counter") kind = "counter:" + col.transform.parent.parent.name;
                else kind = "solid";
                yield return MakeBox(col.name, kind, b);
            }
            // Collider-less sea planes: what the player sees as water, which is not the swimmable SwimVolume.
            foreach (var renderer in All<Renderer>(ctx.Scene).Where(x => x.name.StartsWith("WaterSurface", StringComparison.Ordinal)))
                yield return MakeBox(renderer.name, "visual.water", renderer.bounds);
            var r = ctx.Region.Bounds;
            yield return new Box { name = "DiveRegionField:" + ctx.Region.RegionId, kind = "map", min = new Vector3(r.MinX, 0f, r.MinZ), max = new Vector3(r.MaxX, 0f, r.MaxZ) };
            foreach (var zone in All<WorldZoneVolume>(ctx.Scene))
                yield return new Box { name = zone.name, kind = "zone." + zone.Zone, min = R(zone.transform.position - zone.Size * 0.5f), max = R(zone.transform.position + zone.Size * 0.5f) };
        }

        private static Box MakeBox(string name, string kind, Bounds b) => new Box { name = name, kind = kind, min = R(b.min), max = R(b.max) };

        // ---- anchors -----------------------------------------------------------------------------

        private static IEnumerable<Point> CollectAnchors(Ctx ctx, out int worldAnchors, out string boatNote)
        {
            var list = new List<Point>();
            var world = All<WorldAnchor>(ctx.Scene).ToArray();
            worldAnchors = world.Length;
            foreach (var id in WorldAnchorIds.All)
            {
                var a = world.SingleOrDefault(w => w.Id == id);
                list.Add(P(ctx, id, "worldAnchor", "ContinuousTownLayout", a != null ? a.transform.position : new Vector3(float.NaN, float.NaN, float.NaN)));
            }
            foreach (var s in All<ServicePointAnchor>(ctx.Scene))
            {
                var id = s.Definition.ServiceId;
                list.Add(P(ctx, id, "service", "TownServices (moved by ContinuousTownLayout)", s.transform.position));
                var store = s.GetComponent<TownStorefront>();
                if (store != null && store.CustomerPoint != null)
                    list.Add(P(ctx, id + ":customer", "serviceCustomer", "TownStorefront.CustomerSpot", store.CustomerPoint.position));
            }
            foreach (var name in new[] { "Dock_Town_Anchor", "DiveExit", "Beach_TownGate" })
            {
                var go = Root(ctx.Scene, name);
                if (go != null) list.Add(P(ctx, name, "coast", "scene", go.transform.position));
            }
            foreach (var surface in new[] { "Beach_Wade", "Shore_Ramp" })
            {
                var top = SlopeEnd(ctx, surface, true);
                var bottom = SlopeEnd(ctx, surface, false);
                list.Add(P(ctx, surface + ":dryTop", "coast", "scene, raycast along centre line", top));
                list.Add(P(ctx, surface + ":waterline", "coast", "first sample whose zone is not Sea", bottom));
            }
            foreach (var part in All<BoatPartAnchor>(ctx.Scene))
                list.Add(P(ctx, part.PartId, "boatPart", "scene BoatPartAnchor " + part.name, part.WorldPosition));
            foreach (var anchor in All<RouteAnchor>(ctx.Scene))
            {
                list.Add(P(ctx, anchor.AnchorId, "routeAnchor", "scene " + anchor.name, anchor.WorldPosition));
                list.Add(P(ctx, anchor.AnchorId + ":boarding", "routeBoarding", "scene " + anchor.name, anchor.BoardingPosition));
            }
            foreach (var path in All<DiveRoutePath>(ctx.Scene))
            {
                if (path.Waypoints.Count == 0) continue;
                list.Add(P(ctx, path.RouteId + ":wp0", "routeStart", "scene " + path.name, path.Waypoints[0]));
                if (path.RouteId != BoatTripIds.NearRouteId || path.Waypoints.Count < 2) continue;
                // The rowboat has no scene object: NetworkBoatController.SnapToDockRouteStart puts it on the near
                // route's wp0, moved along wp0->wp1 by DockOffsetMeters, and BoatTripNetworkBinding hides it until repaired.
                var forward = Vector3.ProjectOnPlane(path.Waypoints[1] - path.Waypoints[0], Vector3.up).normalized;
                var p = path.Waypoints[0] + forward * BoatHullSeatRules.DockOffsetMeters(BoatHullKind.Rowboat);
                list.Add(P(ctx, "broken-rowboat-berth", "boat", "near route wp0 + Rowboat DockOffsetMeters along wp0->wp1", p));
            }
            boatNote = "No static boat in DiveTestArea. The runtime NetworkBoatController snaps to the near route's wp0 " +
                       "(+DockOffsetMeters, 0 m for the rowboat) and is hidden until BoatRepair is Repaired; its three parts are the BoatPartAnchors.";
            foreach (var spawn in All<PlayerSpawnPoint>(ctx.Scene))
                list.Add(P(ctx, spawn.name, "legacySpawn", "scene PlayerSpawnPoint", spawn.transform.position));
            foreach (var bed in All<HomeInteractionAnchor>(ctx.Scene).Where(b => b.Kind == HomeInteractionKind.Bed))
                list.Add(P(ctx, bed.BedId + ":exit", "bedExit", "HomeSceneSetup ExitPose", bed.ExitPose.position));
            return list;
        }

        private static Point P(Ctx ctx, string id, string kind, string source, Vector3 position) => new Point
        {
            id = id, kind = kind, source = source, position = R(position),
            zone = float.IsNaN(position.x) ? "missing" : ctx.Zones.ResolveZone(position).ToString(),
            inMap = !float.IsNaN(position.x) && ctx.Region.TryWorldToMap(position, out _)
        };

        // Walks the slope's centre line; top = highest dry sample, waterline = first non-Sea sample from the water end.
        private static Vector3 SlopeEnd(Ctx ctx, string name, bool top)
        {
            var b = Root(ctx.Scene, name).GetComponent<Collider>().bounds;
            var alongX = b.size.x > b.size.z;
            var samples = new List<Vector3>();
            var n = Mathf.CeilToInt((alongX ? b.size.x : b.size.z) / 0.05f);
            for (var i = 0; i <= n; i++)
            {
                var t = (float)i / n;
                var q = alongX ? new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), 0f, b.center.z) : new Vector3(b.center.x, 0f, Mathf.Lerp(b.min.z, b.max.z, t));
                if (!Physics.Raycast(new Vector3(q.x, b.max.y + 1f, q.z), Vector3.down, out var hit, b.size.y + 2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider.name != name) continue;
                samples.Add(hit.point);
            }
            if (top) return samples.OrderByDescending(s => s.y).First();
            return samples.Where(s => ctx.Zones.ResolveZone(s) != PlayerZone.Sea).OrderBy(s => s.y).First();
        }

        // ---- routes ------------------------------------------------------------------------------

        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

        private static IEnumerable<Route> WalkRoutes(Ctx ctx, Point[] anchors)
        {
            Vector2 A(string id) => XZ(anchors.First(a => a.id == id).position);
            var door = XZ(ContinuousTownLayout.HomeRig.Door);
            var origin = XZ(ContinuousTownLayout.HomeOrigin);
            var approach = XZ(ContinuousTownLayout.TownSquareCenter) + new Vector2(0f, -7f);
            var street = new Vector2(0f, -16f);
            var beach = new Vector2(0f, -12.5f);
            var ledgeTurn = new Vector2(9f, -12.5f);
            var exit = A("DiveExit");
            var dock = new Vector2(9f, -4f);
            var bedNorth = ContinuousTownLayout.HomeRig.Bed(0).z + ContinuousTownLayout.HomeRig.BedSize.z * 0.5f + 0.55f;
            Vector2 Corridor(float sign, float z) => new Vector2(origin.x + sign * 4.7f, z);
            Vector2 Customer(string id) => A(id + ":customer");
            var pcStand = XZ(ContinuousTownLayout.HomeRig.Pc) + new Vector2(0f, ContinuousTownLayout.HomeRig.PcSize.z * 0.5f + 0.8f);

            List<Vector2> Leave(Vector2 from)
            {
                var sign = from.x < origin.x ? -1f : 1f;
                return new List<Vector2> { from, Corridor(sign, from.y), Corridor(sign, bedNorth), door, approach };
            }

            // a) direct: each home spawn -> side corridor -> door -> approach -> street -> beach -> ledge -> dock
            for (var i = 0; i < WorldAnchorIds.HomeSpawnCount; i++)
            {
                var w = Leave(A(WorldAnchorIds.HomeSpawn(i)));
                w.AddRange(new[] { street, ledgeTurn, exit, dock });
                yield return Drive(ctx, "a" + i, "direct: home-spawn-" + i + " -> pier/boat", w, false);
            }

            // b) shopping: spawn 0 -> equipment counter -> pier
            var b = Leave(A(WorldAnchorIds.HomeSpawn(0)));
            b.AddRange(new[] { Customer("shop-equipment"), new Vector2(-6f, -16f), ledgeTurn, exit, dock });
            yield return Drive(ctx, "b", "shopping: home -> equipment -> pier", b, false);

            // c) selling: pier/dive exit -> fish buyer -> recording buyer -> home PC -> bed
            var bed3 = A(DayIds.Beds[3] + ":exit");
            var fishCustomer = Customer("buyer-fish");
            var recCustomer = Customer("buyer-recording");
            var c = new List<Vector2> { dock, exit, ledgeTurn, fishCustomer, recCustomer,
                new Vector2(0.5f, recCustomer.y), approach, door, Corridor(1f, bedNorth), Corridor(1f, pcStand.y), pcStand, bed3 };
            yield return Drive(ctx, "c", "selling: pier/DiveExit -> fish -> recording -> home PC -> bed", c, false);

            // d) water in and out
            var d1 = Leave(A(WorldAnchorIds.HomeSpawn(0)));
            d1.AddRange(new[] { street, beach, new Vector2(-6.5f, -11.2f), new Vector2(-6.5f, -5f) });
            yield return Drive(ctx, "d1", "water in: home -> wade -> sea", d1, true);
            var rampWater = A("Shore_Ramp:waterline");
            var d2 = new List<Vector2> { rampWater, exit, ledgeTurn, street, approach, door, Corridor(-1f, bedNorth),
                Corridor(-1f, A(WorldAnchorIds.HomeSpawn(0)).y), A(WorldAnchorIds.HomeSpawn(0)) };
            yield return Drive(ctx, "d2", "water out: ramp waterline -> DiveExit -> home", d2, false);

            // e) optional walk: the reachable town perimeter once, with the ledge/pier spur, back to the beach
            var e = new List<Vector2>
            {
                beach, new Vector2(-13.8f, -13.8f), new Vector2(-13.4f, -15.2f), new Vector2(-13.4f, -35.4f),
                new Vector2(-3.2f, -35.4f), new Vector2(-3.2f, -42.6f), Corridor(-1f, -43f), Corridor(-1f, -50.5f),
                new Vector2(-2.5f, -50.5f), new Vector2(-2.5f, -53f), new Vector2(2.5f, -53f), new Vector2(2.5f, -50.5f),
                Corridor(1f, -50.5f), Corridor(1f, -43f), new Vector2(3.2f, -42.6f), new Vector2(3.2f, -35.4f),
                new Vector2(13.4f, -35.4f), new Vector2(13.4f, -15.2f), new Vector2(13.8f, -13.8f), ledgeTurn, exit, dock,
                exit, ledgeTurn, beach
            };
            yield return Drive(ctx, "e", "optional walk: reachable town perimeter + pier spur", e, false);
        }

        private static Route Drive(Ctx ctx, string id, string label, List<Vector2> waypoints, bool expectsSea)
        {
            var route = new Route
            {
                id = id, label = label, expectsSea = expectsSea,
                waypoints = waypoints.Select(w => new Vector2(R(w.x), R(w.y))).ToArray(),
                start = Fmt(waypoints[0]), end = Fmt(waypoints[waypoints.Count - 1])
            };
            for (var i = 1; i < waypoints.Count; i++) route.plannedLength += Vector2.Distance(waypoints[i - 1], waypoints[i]);
            var failures = new List<string>();

            var go = new GameObject("Audit_P172_Diver");
            SceneManager.MoveGameObjectToScene(go, ctx.Scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = ctx.Height; cc.radius = ctx.Radius; cc.center = new Vector3(0f, ctx.Height * 0.5f, 0f);
                cc.slopeLimit = ctx.Slope; cc.stepOffset = ctx.Step; cc.skinWidth = ctx.Skin;
                var t = go.transform;
                var startY = Physics.Raycast(new Vector3(waypoints[0].x, 12f, waypoints[0].y), Vector3.down, out var g0, 8f, ~0, QueryTriggerInteraction.Ignore)
                    ? g0.point.y : WalkY;
                t.position = new Vector3(waypoints[0].x, startY + ctx.Skin, waypoints[0].y);
                Physics.SyncTransforms();
                var vy = 0f;
                var startedInSea = ctx.Zones.ResolveZone(t.position) == PlayerZone.Sea;
                for (var w = 1; w < waypoints.Count && !route.stuck && !route.reachedSea; w++)
                {
                    var target = waypoints[w];
                    var guard = Mathf.CeilToInt(Vector2.Distance(waypoints[w - 1], target) / (ctx.Speed * Dt)) * 3 + 100;
                    while (true)
                    {
                        var here = XZ(t.position);
                        var toTarget = target - here;
                        if (toTarget.magnitude <= 0.05f) break;
                        if (--guard < 0)
                        {
                            route.stuck = true;
                            failures.Add($"stuck at ({here.x:0.##}, {here.y:0.##}) heading to {Fmt(target)}");
                            break;
                        }
                        var step = Vector2.ClampMagnitude(toTarget, ctx.Speed * Dt);
                        vy = cc.isGrounded ? -1f : vy - 9.81f * Dt;
                        cc.Move(new Vector3(step.x, vy * Dt, step.y));
                        route.frames++;
                        var moved = Vector2.Distance(here, XZ(t.position));
                        route.drivenLength += moved;
                        if (moved < step.magnitude * 0.5f) route.blockedFrames++;

                        var feet = t.position;
                        if (ctx.Zones.ResolveZone(feet) == PlayerZone.Sea)
                        {
                            if (startedInSea) continue;
                            route.reachedSea = true;
                            if (!expectsSea) { route.fell = true; failures.Add($"entered sea at ({feet.x:0.##}, {feet.z:0.##})"); }
                            break;
                        }
                        startedInSea = false;
                        if (!Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            route.fell = true;
                            failures.Add($"over no ground at ({feet.x:0.##}, {feet.z:0.##}) y={feet.y:0.###}");
                        }
                        else
                        {
                            var slope = SlopeSurfaces.Contains(ground.collider.name);
                            if (feet.y < ground.point.y - 0.05f || (!slope && ground.point.y < WalkY - 0.05f))
                            {
                                route.fell = true;
                                failures.Add($"dropped to y={feet.y:0.###} at ({feet.x:0.##}, {feet.z:0.##}) over {ground.collider.name}");
                            }
                        }
                    }
                }
                route.endPosition = new Vector2(R(t.position.x), R(t.position.z));
                if (!route.stuck && !route.reachedSea && Vector2.Distance(XZ(t.position), waypoints[waypoints.Count - 1]) > 0.2f)
                    failures.Add("did not reach the last waypoint");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }

            route.simSeconds = R(route.frames * Dt);
            route.secondsAtWalkSpeed = R(route.drivenLength / ctx.Speed);
            route.plannedLength = R(route.plannedLength);
            route.drivenLength = R(route.drivenLength);
            Narrowest(ctx, waypoints, route);
            route.failures = failures.Distinct().Take(40).ToArray();
            return route;
        }

        // Clear width across the route, perpendicular to each leg, every 0.25m. Samples within 0.35m of a turn
        // are skipped (the perpendicular there is ambiguous) and so are samples in the sea.
        private static void Narrowest(Ctx ctx, List<Vector2> waypoints, Route route)
        {
            route.narrowestWidth = float.MaxValue;
            route.narrowestOutsideHomeWidth = float.MaxValue;
            for (var leg = 0; leg + 1 < waypoints.Count; leg++)
            {
                var a = waypoints[leg];
                var b = waypoints[leg + 1];
                var len = Vector2.Distance(a, b);
                if (len < 0.01f) continue;
                var dir = (b - a) / len;
                var normal = new Vector2(-dir.y, dir.x);
                var count = Mathf.CeilToInt(len / SampleSpacing);
                for (var i = 0; i <= count; i++)
                {
                    var along = len * i / count;
                    if ((leg > 0 && along < 0.35f) || (leg + 2 < waypoints.Count && len - along < 0.35f)) continue;
                    var p = a + dir * along;
                    if (!Ground(ctx, p, out var gy, out _) || gy < WaterSurfaceY) continue;
                    var left = Free(ctx, p, gy, normal, out var leftWhy);
                    var right = Free(ctx, p, gy, -normal, out var rightWhy);
                    var width = left + right;
                    if (width < route.narrowestOutsideHomeWidth && ctx.Zones.ResolveZone(new Vector3(p.x, gy, p.y)) != PlayerZone.Home)
                    {
                        route.narrowestOutsideHomeWidth = R(width);
                        route.narrowestOutsideHomeAt = new Vector2(R(p.x), R(p.y));
                        route.narrowestOutsideHomeBetween = leftWhy + " | " + rightWhy;
                    }
                    if (width >= route.narrowestWidth) continue;
                    route.narrowestWidth = R(width);
                    route.narrowestMargin = R(width - 2f * (ctx.Radius + ctx.Skin));
                    route.narrowestAt = new Vector2(R(p.x), R(p.y));
                    route.narrowestBetween = leftWhy + " | " + rightWhy;
                }
            }
        }

        // The first thing straight down must be a walk surface; otherwise `top` names what stands there.
        private static bool Ground(Ctx ctx, Vector2 p, out float y, out string top)
        {
            y = 0f;
            top = "ground ends";
            if (!Physics.Raycast(new Vector3(p.x, 12f, p.y), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (!ctx.Walk.Contains(hit.collider.name)) { top = Named(hit.collider.transform); return false; }
            y = hit.point.y;
            return true;
        }

        // Distance from p (feet at y) along dir until a solid at shin/hip/head height, or until the walk ground ends
        // (no ground, a step > stepOffset, or the sea).
        private static float Free(Ctx ctx, Vector2 p, float y, Vector2 dir, out string why)
        {
            var d3 = new Vector3(dir.x, 0f, dir.y);
            var obstacle = MaxProbe;
            why = "probe " + MaxProbe + "m";
            foreach (var h in new[] { 0.35f, 1.0f, 1.6f })
            {
                var origin = new Vector3(p.x, y + h, p.y);
                foreach (var hit in Physics.RaycastAll(origin, d3, MaxProbe, ~0, QueryTriggerInteraction.Ignore))
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

        // ---- the home bottleneck -----------------------------------------------------------------

        // Free spans across the house at the bed row and at the open facade, read with a thin probe at bed height.
        private static IEnumerable<Pinch> HomePinches(Ctx ctx)
        {
            var bedZ = ContinuousTownLayout.HomeRig.Bed(0).z;
            var bedY = ContinuousTownLayout.HomeRig.Bed(0).y;
            var minX = ContinuousTownLayout.FindBlock("HomeLeft").Min.x - 0.5f;
            var maxX = ContinuousTownLayout.FindBlock("HomeRight").Max.x + 0.5f;
            foreach (var (label, z, y) in new[]
                     {
                         ("bed row", bedZ, bedY),
                         ("door line (0.2m inside the facade)", ContinuousTownLayout.HomeRig.Door.z - 0.2f, WalkY + 1.0f)
                     })
            {
                string previous = null;
                float? spanStart = null;
                for (var x = minX; x <= maxX + 1e-4f; x += 0.002f)
                {
                    var blocker = Physics.OverlapBox(new Vector3(x, y, z), new Vector3(0.001f, 0.05f, 0.001f), Quaternion.identity, ~0,
                            QueryTriggerInteraction.Ignore)
                        .Where(c => !ctx.Walk.Contains(c.name)).Select(c => Named(c.transform)).FirstOrDefault();
                    if (blocker == null) { spanStart ??= x; continue; }
                    if (spanStart.HasValue && previous != null)
                    {
                        var width = x - spanStart.Value;
                        yield return new Pinch
                        {
                            at = label, width = R(width), margin = R(width - 2f * (ctx.Radius + ctx.Skin)),
                            center = new Vector2(R((spanStart.Value + x) * 0.5f), R(z)), between = previous + " | " + blocker
                        };
                    }
                    spanStart = null;
                    previous = blocker;
                }
            }
        }

        private static void SpawnBedExits(Ctx ctx, out float[] distances, out string[] beds)
        {
            var exits = All<HomeInteractionAnchor>(ctx.Scene).Where(b => b.Kind == HomeInteractionKind.Bed).ToArray();
            distances = new float[WorldAnchorIds.HomeSpawnCount];
            beds = new string[WorldAnchorIds.HomeSpawnCount];
            for (var i = 0; i < distances.Length; i++)
            {
                var spawn = XZ(ContinuousTownLayout.HomeRig.Spawn(i));
                var best = exits.OrderBy(e => Vector2.Distance(spawn, XZ(e.ExitPose.position))).First();
                distances[i] = R(Vector2.Distance(spawn, XZ(best.ExitPose.position)));
                beds[i] = best.BedId;
            }
        }

        // ---- sight lines -------------------------------------------------------------------------

        private static IEnumerable<Sight> Sightlines(Ctx ctx, Point[] anchors, float hFov)
        {
            Vector3 A(string id) => anchors.First(a => a.id == id).position;
            var views = new[]
            {
                ("home door", ContinuousTownLayout.HomeRig.Door + new Vector3(0f, 0f, 0.3f), 0f),
                ("town square", ContinuousTownLayout.TownSquareCenter, 0f),
                ("coast (beach middle)", new Vector3(0f, WalkY, -12.5f), 180f),
                ("pier", new Vector3(9f, WalkY, -4.5f), 180f)
            };
            var targets = new List<(string name, Bounds bounds, Collider[] own)>();
            foreach (var s in All<ServicePointAnchor>(ctx.Scene))
            {
                var renderers = s.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                targets.Add(("shop " + s.Definition.ServiceId, b, s.GetComponentsInChildren<Collider>(true)));
            }
            var dock = Root(ctx.Scene, "Dock_Town");
            targets.Add(("pier (Dock_Town)", dock.GetComponent<Renderer>().bounds, dock.GetComponentsInChildren<Collider>(true)));
            targets.Add(("broken-rowboat-berth", new Bounds(A("broken-rowboat-berth") + Vector3.up * 0.5f, new Vector3(1.4f, 1f, 3.5f)), new Collider[0]));
            foreach (var part in All<BoatPartAnchor>(ctx.Scene))
            {
                var r = part.GetComponentInChildren<Renderer>();
                var b = r != null ? r.bounds : new Bounds(part.WorldPosition, Vector3.one * 0.6f);
                targets.Add(("boat part " + part.PartId, b, part.GetComponentsInChildren<Collider>(true)));
            }
            targets.Add(("home door", new Bounds(ContinuousTownLayout.HomeRig.Door + Vector3.up * 1.5f, new Vector3(11.65f, 3f, 0.1f)), new Collider[0]));

            foreach (var (from, feet, yaw) in views)
            {
                var eye = feet + Vector3.up * ctx.Eye;
                var heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                foreach (var (name, bounds, own) in targets)
                {
                    if ((name == "home door" && from == "home door") || (name.StartsWith("pier") && from == "pier")) continue;
                    var candidates = new[] { bounds.center, bounds.center + Vector3.up * bounds.extents.y * 0.9f };
                    string blocker = null;
                    var visible = false;
                    foreach (var point in candidates)
                    {
                        var hitName = FirstBlocker(eye, point, own);
                        if (hitName == null) { visible = true; break; }
                        blocker ??= hitName;
                    }
                    var to = bounds.center - eye;
                    var flat = Vector3.ProjectOnPlane(to, Vector3.up);
                    var h = Vector3.SignedAngle(heading, flat, Vector3.up);
                    var v = Mathf.Asin(Mathf.Clamp(to.y / to.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
                    var px = 540f * (bounds.size.y / Mathf.Max(0.01f, to.magnitude)) / Mathf.Tan(ctx.Fov * 0.5f * Mathf.Deg2Rad);
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

        private static string FirstBlocker(Vector3 eye, Vector3 point, Collider[] own)
        {
            var to = point - eye;
            return Physics.RaycastAll(eye, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !own.Contains(h.collider)).OrderBy(h => h.distance).Select(h => Named(h.collider.transform)).FirstOrDefault();
        }

        // ---- helpers -----------------------------------------------------------------------------

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        private static GameObject Root(Scene scene, string name) =>
            All<Transform>(scene).Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        private static string Named(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        private static float R(float v) => Mathf.Round(v * 1000f) / 1000f;
        private static Vector3 R(Vector3 v) => new Vector3(R(v.x), R(v.y), R(v.z));
        private static string Fmt(Vector2 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##})", v.x, v.y);
    }
}
