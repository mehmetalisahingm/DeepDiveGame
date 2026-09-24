using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Guards the P3.3 half of DiveTestArea: the jetty a diver walks out on, the berth the boat
    // waits at, the near route it sails, the offshore anchorage it stops at and the region the
    // map's 0..1 square covers. Everything asserted here is produced by
    // DiveTestAreaRouteSetup.Apply; if this fails, running that menu item again is the repair,
    // not hand-editing the scene.
    //
    // Expectations are written out rather than read from the setup script, the same way the P2,
    // P3.1, P3.2 and P3-B scene tests do it - and for the same reason: a guard that reads its
    // expectations from the script it is guarding would follow that script anywhere, including
    // into a mistake. DeepDive.World.Tests does not reference DeepDive.World.Editor and is not
    // given that reference here, so the geometry below is measured a second time from the numbers
    // the documents quote.
    //
    // Still EditMode: no rig, no play mode, no netcode. Nothing here writes the scene.
    public class DiveTestAreaRouteSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        private const string ShoreMaterialPath = "Assets/DeepDive/World/Art/ShoreMaterial.mat";

        private const string DockName = "Dock_Town";
        private const string DockAnchorName = "Dock_Town_Anchor";
        private const string AnchorName = "Anchor_Near";
        private const string BoardingName = "Boarding";
        private const string RegionName = "DiveRegion";

        // --- The jetty and the route, as authored ---------------------------------------------

        private static readonly Vector3 DockCenter = new Vector3(9f, 8.2f, -5.25f);
        private static readonly Vector3 DockSize = new Vector3(2f, 0.4f, 3.5f);
        private const float DockTopY = 8.4f;
        private const float DockShorewardZ = -7f;

        private static readonly Vector3 DockAnchorPosition = new Vector3(9f, 8.4f, -3.7f);

        private static readonly Vector3[] Waypoints =
        {
            new Vector3(9f, 8f, -0.4f),
            new Vector3(9f, 8f, 3.5f),
            new Vector3(9f, 8f, 8.5f)
        };

        private static readonly Vector3 AnchorPosition = new Vector3(9f, 8f, 8.5f);
        private static readonly Vector3 BoardingPosition = new Vector3(11f, 8f, 8.5f);

        private const string RegionId = "region-near-1";
        private const float RegionExtent = 15f;

        // --- The hull the route has to fit ----------------------------------------------------

        // The starting boat, frozen with Mehmet: hull 2.4 x 5.0, clearance envelope 2.90 x 5.50.
        private const float HullHalfWidth = 1.2f;
        private const float HullHalfLength = 2.5f;
        private const float EnvelopeHalfWidth = 1.45f;
        private const float EnvelopeHalfLength = 2.75f;
        private const float HullDraft = 0.5f;

        private const float MinBerthGap = 0.5f;
        private const float AuthoredBerthGap = 0.6f;
        private const float WallMargin = 1f;
        private const float ArenaInnerExtent = 14.75f;

        // A hull that pivots at a waypoint sweeps its circumscribed circle rather than its box.
        private static float TurnClearance =>
            Mathf.Sqrt(EnvelopeHalfWidth * EnvelopeHalfWidth + EnvelopeHalfLength * EnvelopeHalfLength);

        // --- The arena it has to live with ----------------------------------------------------

        private const float SurfaceY = 8f;
        private const float SeaBedY = 0f;

        private static readonly Vector3 ExitCenter = new Vector3(9f, 9.3f, -9f);
        private static readonly Vector3 ExitSize = new Vector3(3.5f, 2f, 3.5f);
        private const float ExitZoneProbeHeight = 0.9f;

        private static readonly Vector3 LedgeCenter = new Vector3(9f, 7.9f, -9f);
        private static readonly Vector3 LedgeSize = new Vector3(4f, 1f, 4f);
        private const float LedgeTopY = 8.4f;
        private const float LedgeSeawardZ = -7f;

        private static readonly Vector3 SwimCenter = new Vector3(0f, 4f, 0f);
        private static readonly Vector3 SwimSize = new Vector3(30f, 8f, 30f);

        private static readonly Vector3 PlatformCenter = new Vector3(0f, 4.2f, -12.875f);
        private static readonly Vector3 PlatformSize = new Vector3(29.5f, 8.4f, 3.75f);
        private static readonly Vector3 WadeCenter = new Vector3(-6.5f, 6.618738f, -7.653313f);
        private static readonly Vector3 WadeSize = new Vector3(6f, 0.4f, 7.5718446f);
        private const float WadeAngleDegrees = 25f;

        private static readonly Vector3 HullPartPosition = new Vector3(-12f, 8.4f, -12.5f);
        private static readonly Vector3 EnginePosition = new Vector3(-6.5f, 7.0010767f, -8f);
        private static readonly Vector3 FuelTankPosition = new Vector3(12.5f, 8.4f, -12.5f);

        // The spawn ring, the event and the fish wander box the route has to stay clear of.
        private const float SpawnRingExtent = 3f;
        private static readonly Vector3 EventCenter = new Vector3(-9f, 3.5f, -2f);
        private const float EventRadius = 1.5f;
        private static readonly Vector3 FishHome = new Vector3(0f, 4f, 8f);
        private const float FishWanderRadius = 6f;
        private const float FishVerticalWanderFactor = 0.5f;

        private Scene scene;
        private Scene previousActive;
        private bool opened;

        [SetUp]
        public void OpenDiveScene()
        {
            previousActive = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            opened = !already.IsValid() || !already.isLoaded;
            scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            SceneManager.SetActiveScene(scene);
        }

        [TearDown]
        public void CloseDiveScene()
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        // --- Helpers ---------------------------------------------------------------------------

        private GameObject Require(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            Assert.Fail("DiveTestArea is missing " + name + "; run DeepDive/P3.3/Dive test area");
            return null;
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        private WaterField Field()
        {
            var fields = FindAll<WaterField>(scene);
            Assert.AreEqual(1, fields.Count, "DiveTestArea must hold exactly one WaterField");
            return fields[0];
        }

        private DiveRoutePath Route()
        {
            var paths = FindAll<DiveRoutePath>(scene);
            Assert.AreEqual(1, paths.Count, "DiveTestArea must hold exactly one route path");
            paths[0].Refresh();
            return paths[0];
        }

        private RouteAnchor Anchor(string name)
        {
            var anchor = Require(name).GetComponent<RouteAnchor>();
            Assert.IsNotNull(anchor, name + " has no RouteAnchor");
            return anchor;
        }

        private DiveRegionField Region()
        {
            var regions = FindAll<DiveRegionField>(scene);
            Assert.AreEqual(1, regions.Count, "DiveTestArea must hold exactly one region field");
            return regions[0];
        }

        private static CharacterController DiverController()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, "Missing diver prefab");
            var controller = prefab.GetComponent<CharacterController>();
            Assert.IsNotNull(controller, "Diver prefab has no CharacterController");
            return controller;
        }

        // A probe shaped like the real diver, placed by the feet.
        private static WaterProbe Diver(Vector3 feet)
        {
            var controller = DiverController();
            return new WaterProbe(feet, controller.center, controller.radius, controller.height, Vector3.up);
        }

        private EnvironmentLocomotion ClassifyAt(Vector3 feet) => Field().CreateTracker().Classify(Diver(feet));

        // The authored box of a scaled unit cube, which is how every piece of this scenery is built.
        private static Bounds BoxOf(GameObject piece) =>
            new Bounds(piece.transform.position, piece.transform.localScale);

        private static bool FlatIntersects(Bounds a, Bounds b) =>
            a.min.x < b.max.x && b.min.x < a.max.x && a.min.z < b.max.z && b.min.z < a.max.z;

        private static bool FlatContains(Bounds bounds, Vector3 point) =>
            point.x >= bounds.min.x && point.x <= bounds.max.x &&
            point.z >= bounds.min.z && point.z <= bounds.max.z;

        private static float FlatDistance(Vector3 a, Vector3 b) =>
            Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));

        private static float FlatDistanceToBounds(Vector3 point, Bounds bounds)
        {
            var dx = Mathf.Max(bounds.min.x - point.x, 0f, point.x - bounds.max.x);
            var dz = Mathf.Max(bounds.min.z - point.z, 0f, point.z - bounds.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool InsideExitZone(Vector3 stand)
        {
            var probe = stand + Vector3.up * ExitZoneProbeHeight;
            var local = probe - ExitCenter;
            var half = ExitSize * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                   && Mathf.Abs(local.y) <= half.y
                   && Mathf.Abs(local.z) <= half.z;
        }

        // The swept envelope of a hull travelling one leg: the leg's own box, widened by the
        // envelope's half width and extended by its half length past both ends, since the bow and
        // the stern reach beyond the point the boat steers to.
        private static Vector3[] SweptEnvelope(Vector3 from, Vector3 to)
        {
            var direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
            var length = direction.magnitude;
            Assert.Greater(length, 0.001f, "two waypoints share a place");

            var forward = direction / length;
            var right = new Vector3(forward.z, 0f, -forward.x);
            var centre = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);
            var halfLength = length * 0.5f + EnvelopeHalfLength;

            return new[]
            {
                centre - forward * halfLength - right * EnvelopeHalfWidth,
                centre - forward * halfLength + right * EnvelopeHalfWidth,
                centre + forward * halfLength + right * EnvelopeHalfWidth,
                centre + forward * halfLength - right * EnvelopeHalfWidth
            };
        }

        // Separating-axis test in XZ between the swept box and a world-axis bounds. Four axes are
        // enough for two rectangles: both of the box's, and both world axes.
        private static bool Overlaps(Vector3[] corners, Bounds bounds)
        {
            var axes = new[]
            {
                (corners[1] - corners[0]).normalized,
                (corners[3] - corners[0]).normalized,
                Vector3.right,
                Vector3.forward
            };

            var boxCorners = new[]
            {
                new Vector3(bounds.min.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.max.z),
                new Vector3(bounds.min.x, 0f, bounds.max.z)
            };

            foreach (var axis in axes)
            {
                Project(corners, axis, out var minA, out var maxA);
                Project(boxCorners, axis, out var minB, out var maxB);
                if (maxA < minB || maxB < minA) return false;
            }

            return true;
        }

        private static void Project(Vector3[] points, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var point in points)
            {
                var value = point.x * axis.x + point.z * axis.z;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        private List<Collider> SolidColliders() => FindAll<Collider>(scene).Where(c => !c.isTrigger).ToList();

        // Every solid collider that reaches the hull's depth. The sea bed (top y = 0) and the
        // arena walls (top y = 7) fall out here: neither can touch a hull that floats between 7.5
        // and the surface, and the walls are covered by the arena inset rule instead.
        private List<Collider> HullDepthObstacles() =>
            SolidColliders().Where(c => c.bounds.max.y > SurfaceY - HullDraft).ToList();

        // --- The jetty -----------------------------------------------------------------------------

        // 18
        [Test]
        public void TheJettySitsAtTheDocumentedTransformAndIsSolidScenery()
        {
            var dock = Require(DockName);

            Assert.AreEqual(DockCenter, dock.transform.position, "the jetty moved");
            Assert.AreEqual(DockSize, dock.transform.localScale, "the jetty was resized");
            Assert.AreEqual(Quaternion.identity, dock.transform.rotation, "the jetty must stay axis aligned");
            Assert.AreEqual(0, dock.layer, "the jetty must stay on the default layer so a diver collides with it");

            var box = dock.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "the jetty has no collider; nobody could walk on it");
            Assert.IsFalse(box.isTrigger, "a trigger jetty would let a diver fall through the deck");
            Assert.AreEqual(Vector3.one, box.size, "the collider must match the scaled cube");
            Assert.AreEqual(Vector3.zero, box.center, "the collider must sit on the piece's own centre");

            var renderer = dock.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, "the jetty is invisible");
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Material>(ShoreMaterialPath), renderer.sharedMaterial,
                "the jetty must reuse P3.1's shore material rather than introduce a second one");

            // Authored scenery is identical on every machine, so there is nothing to replicate.
            Assert.IsNull(dock.GetComponent<NetworkObject>(), "the jetty must not be a network object");
            Assert.IsEmpty(dock.GetComponentsInChildren<NetworkBehaviour>(true), "the jetty must carry no netcode");
        }

        // 19
        [Test]
        public void TheJettyMeetsTheLedgeFlushWithNoStepBetweenThem()
        {
            var dock = BoxOf(Require(DockName));
            var ledge = BoxOf(Require("Shore_Ledge"));

            Assert.AreEqual(LedgeTopY, ledge.max.y, 0.001f, "Shore_Ledge's top moved");
            Assert.AreEqual(DockTopY, dock.max.y, 0.001f, "the jetty's deck moved");
            Assert.AreEqual(0f, dock.max.y - ledge.max.y, 0.001f,
                "a diver walking between the ledge and the deck must not have to step up or down");

            Assert.AreEqual(LedgeSeawardZ, ledge.max.z, 0.001f, "Shore_Ledge's seaward face moved");
            Assert.AreEqual(DockShorewardZ, dock.min.z, 0.001f,
                "the jetty must start exactly at the ledge's seaward face, with no gap to fall through");

            // 2 m of deck inside the ledge's 4, on the pad's own centre line.
            Assert.GreaterOrEqual(dock.min.x, ledge.min.x, "the jetty must stay within the ledge's width");
            Assert.LessOrEqual(dock.max.x, ledge.max.x, "the jetty must stay within the ledge's width");
            Assert.AreEqual(ExitCenter.x, dock.center.x, 0.001f, "the jetty must stay on the pad's centre line");
        }

        // 22
        [Test]
        public void TheJettyCannotBeClimbedOutOfTheWater()
        {
            var dock = BoxOf(Require(DockName));
            var controller = DiverController();

            // The one way up is the flush join with the ledge. From the water the deck stands
            // higher than a diver's step, and no face of it is a ramp.
            var climb = dock.max.y - SurfaceY;
            Assert.AreEqual(0.4f, climb, 0.001f, "the deck's height over the waterline moved");
            Assert.Greater(climb, controller.stepOffset,
                "a diver at the surface could step straight onto the deck; the jetty would become a water exit");
            Assert.AreEqual(0f, Vector3.Angle(Require(DockName).transform.up, Vector3.up), 0.001f,
                "a tilted jetty face would be a ramp out of the water");

            // And the water beside it really is water, so this is a climb that would matter.
            var beside = new Vector3(dock.max.x + controller.radius + 0.15f, SurfaceY - 1f, dock.center.z);
            Assert.AreEqual(EnvironmentLocomotion.Surface, ClassifyAt(beside),
                "the water beside the jetty must stay swimmable");
        }

        // --- The safe return pad --------------------------------------------------------------------

        // 20
        [Test]
        public void SteppingOffTheJettyOntoTheLedgeLandsInsideTheReturnPad()
        {
            var dock = BoxOf(Require(DockName));
            var padNorthZ = ExitCenter.z + ExitSize.z * 0.5f;

            Assert.AreEqual(0.25f, dock.min.z - padNorthZ, 0.001f,
                "the jetty must stop 0.25 m short of the pad, so walking ashore is a step the player takes");

            // One pace shorewards off the deck, on the ledge top, is inside the pad.
            var ashore = new Vector3(dock.center.x, LedgeTopY, padNorthZ - 0.25f);
            Assert.IsTrue(InsideExitZone(ashore),
                "a diver who walks off the jetty must reach the safe return pad at " + ashore);

            // The pad is dry land on the ledge, so the catch is banked by walking, not by floating.
            Assert.AreEqual(EnvironmentLocomotion.Land, ClassifyAt(ashore), "the return pad must stay dry land");
        }

        // 21
        [Test]
        public void NoJettyOrRouteSurfaceSitsInsideTheReturnPad()
        {
            var pad = new Bounds(ExitCenter, ExitSize);
            var dock = BoxOf(Require(DockName));

            Assert.IsFalse(FlatIntersects(dock, pad),
                "the jetty reaches into the return pad; it would become a second safe return");

            // Standing anywhere on the deck must not bank a catch.
            foreach (var z in new[] { dock.min.z + 0.01f, dock.center.z, dock.max.z - 0.01f })
                Assert.IsFalse(InsideExitZone(new Vector3(dock.center.x, dock.max.y, z)),
                    "standing on the jetty at z=" + z + " banks the catch without walking ashore");

            // Nor may the boat: a crew that parked in the pad would never have to land at all.
            var berth = Waypoints[0];
            var hull = new Bounds(berth, new Vector3(HullHalfWidth * 2f, 1f, HullHalfLength * 2f));
            Assert.IsFalse(FlatIntersects(hull, pad), "the berthed hull sits in the return pad");

            foreach (var waypoint in Route().Waypoints)
                Assert.IsFalse(FlatContains(pad, waypoint), "waypoint " + waypoint + " sits in the return pad");
        }

        // --- The anchorage is real water --------------------------------------------------------------

        // 23
        [Test]
        public void TheAnchorageIsOpenWaterAndNotAWadeShelf()
        {
            // Measured three ways: wet just under the surface, underwater at mid depth, and still
            // underwater at the sea bed. A shelf out there would turn the voyage into a paddle.
            Assert.AreEqual(EnvironmentLocomotion.Surface,
                ClassifyAt(new Vector3(AnchorPosition.x, SurfaceY - 1f, AnchorPosition.z)),
                "a diver floating at the anchorage must be at the surface");

            foreach (var feet in new[] { SurfaceY * 0.5f, SeaBedY + 0.1f })
                Assert.AreEqual(EnvironmentLocomotion.Underwater,
                    ClassifyAt(new Vector3(AnchorPosition.x, feet, AnchorPosition.z)),
                    "a diver at y=" + feet + " under the anchorage must be underwater");

            // And it is nowhere near the shelf a diver can stand on.
            var wade = Require("Beach_Wade").GetComponent<Renderer>().bounds;
            Assert.Greater(FlatDistanceToBounds(AnchorPosition, wade), 10f,
                "the anchorage drifted towards the wade shelf");
        }

        // 24
        [Test]
        public void NothingWalkableStandsInTheAnchoragesWaterColumn()
        {
            // The hull's own footprint, from the sea bed up. Only the sea bed itself may be down
            // there; anything else would be a floor under the boat.
            var column = new Bounds(
                new Vector3(AnchorPosition.x, 0f, AnchorPosition.z),
                new Vector3(HullHalfWidth * 2f, 1f, HullHalfLength * 2f));

            foreach (var collider in SolidColliders())
            {
                var bounds = collider.bounds;
                if (bounds.max.y <= SeaBedY + 0.01f) continue;
                Assert.IsFalse(FlatIntersects(column, bounds),
                    collider.name + " stands in the anchorage's water column " + bounds +
                    "; the anchorage must be open water, not a shelf");
            }
        }

        // 25
        [Test]
        public void TheBoardingSpotIsOpenWaterBesideTheHullNotInsideIt()
        {
            var sea = Anchor(AnchorName);
            var boarding = Require(AnchorName).transform.Find(BoardingName);
            Assert.IsNotNull(boarding, AnchorName + " has no " + BoardingName + " child");
            Assert.AreEqual(BoardingPosition, boarding.position, "the boarding spot moved");
            Assert.AreEqual(BoardingPosition, sea.BoardingPosition,
                "the anchor must hand out the boarding child, not its own position");

            Assert.AreEqual(EnvironmentLocomotion.Surface,
                ClassifyAt(new Vector3(BoardingPosition.x, SurfaceY - 1f, BoardingPosition.z)),
                "a diver climbing aboard floats here; it has to be water");

            // Beside the hull, not in it: the hull's east side is at x = 10.2.
            var clearance = BoardingPosition.x - (AnchorPosition.x + HullHalfWidth);
            Assert.AreEqual(0.8f, clearance, 0.001f, "the boarding clearance moved");
            Assert.Greater(clearance, DiverController().radius,
                "a diver would surface inside the hull rather than beside it");
        }

        // --- The route -------------------------------------------------------------------------------

        // 26
        [Test]
        public void TheRouteRunsDockToAnchorOnOneStableWaterLine()
        {
            var route = Route();
            var points = route.Waypoints;

            Assert.AreEqual(Waypoints.Length, points.Count, "the route's waypoint count changed");
            Assert.AreEqual(Waypoints.Length, route.transform.childCount, "a stray child joined the route");

            for (var i = 0; i < Waypoints.Length; i++)
            {
                // Sibling order is route order, and the names say which is which.
                Assert.AreEqual("WP_" + i, route.transform.GetChild(i).name, "waypoint " + i + " was reordered");
                Assert.AreEqual(Waypoints[i], points[i], "waypoint " + i + " moved");
            }

            // One water line, and it is the water's own rather than a number typed twice.
            var surface = Field().Bodies[0].SurfaceY;
            Assert.AreEqual(SurfaceY, surface, 0.001f, "the waterline moved");
            foreach (var point in points)
                Assert.AreEqual(surface, point.y, 0.001f,
                    "a step in the route would make the hull hop as it passed " + point);

            // Dock to anchor, in that order, without doubling back.
            for (var i = 0; i + 1 < points.Count; i++)
                Assert.Less(points[i].z, points[i + 1].z,
                    "leg " + i + " runs the wrong way; the route must lead away from the jetty");
        }

        // 27
        [Test]
        public void TheRouteEndsAtTheRealDockAndAnchor()
        {
            var route = Route();
            Assert.AreEqual(BoatTripIds.NearRouteId, route.RouteId, "the route id moved off the contract");
            Assert.IsTrue(route.IsContractRoute, "the route must carry Mert's contract id");
            Assert.IsTrue(route.IsUsable, "a route with fewer than two points is half-authored");

            var dock = Anchor(DockAnchorName);
            var sea = Anchor(AnchorName);
            Assert.AreEqual(DiveRouteAnchors.Dock, dock.AnchorId, "the dock anchor id moved");
            Assert.AreEqual(DiveRouteAnchors.AnchorPoint, sea.AnchorId, "the sea anchor id moved");
            Assert.IsTrue(dock.IsContractAnchor, "the dock anchor must be one of DiveRouteAnchors.All");
            Assert.IsTrue(sea.IsContractAnchor, "the sea anchor must be one of DiveRouteAnchors.All");
            Assert.AreEqual(DockAnchorPosition, dock.WorldPosition, "the dock anchor moved off the deck");

            // The anchorage is the last waypoint exactly - not a place near it - so the boat that
            // arrives and the icon Mert draws are in the same spot.
            var last = route.Waypoints[route.Waypoints.Count - 1];
            Assert.AreEqual(sea.WorldPosition, last, "the route must end at the anchor, not beside it");
            Assert.AreEqual(AnchorPosition, last, "the anchorage moved");

            // The berth has to be boardable from the deck spot the dock anchor marks.
            var reach = FlatDistance(route.Waypoints[0], dock.BoardingPosition);
            Assert.LessOrEqual(reach, HullHalfLength + MinBerthGap + HullHalfLength,
                "nobody could board from the dock anchor; the berth is " + reach + " m away");
            Assert.AreEqual(dock.WorldPosition, dock.BoardingPosition,
                "the dock anchor needs no boarding child; the deck spot it sits on is where a player stands");
        }

        // --- Clearance --------------------------------------------------------------------------------

        // 28
        [Test]
        public void NoHullSweepAlongTheRouteTouchesAWorldCollider()
        {
            var route = Route();
            var obstacles = HullDepthObstacles();
            Assert.IsNotEmpty(obstacles, "no collider reaches the hull's depth; this test would pass vacuously");

            for (var leg = 0; leg + 1 < route.Waypoints.Count; leg++)
            {
                var corners = SweptEnvelope(route.Waypoints[leg], route.Waypoints[leg + 1]);
                foreach (var obstacle in obstacles)
                    Assert.IsFalse(Overlaps(corners, obstacle.bounds),
                        "leg " + leg + " sweeps the hull through " + obstacle.name + " " + obstacle.bounds);
            }

            // Interior waypoints only: a hull that pivots there sweeps a circle, not a box. WP_0 is
            // the berth and is exempt by design - the test below is the one that governs it.
            for (var i = 1; i + 1 < route.Waypoints.Count; i++)
                foreach (var obstacle in obstacles)
                    Assert.GreaterOrEqual(FlatDistanceToBounds(route.Waypoints[i], obstacle.bounds), TurnClearance,
                        "a hull turning at waypoint " + i + " would sweep into " + obstacle.name);
        }

        // 28, the exemption written out rather than left implicit
        [Test]
        public void TheBerthIsExemptFromTheTurnCircleAndHeldToTheMeasuredSternGapInstead()
        {
            // The boat does not pivot at the berth: it lies alongside and leaves on a fixed
            // heading, so the circumscribed circle does not describe it. Held to that circle it
            // would fail by a centimetre - which is exactly why the exemption is written down here
            // instead of hiding in a loop bound.
            var jetty = BoxOf(Require(DockName));
            var berth = Route().Waypoints[0];

            var asCircle = FlatDistanceToBounds(berth, jetty);
            Assert.AreEqual(3.1f, asCircle, 0.001f, "the berth's distance to the jetty moved");
            Assert.AreEqual(3.109f, TurnClearance, 0.001f, "the turn clearance moved");
            Assert.Less(asCircle, TurnClearance,
                "the berth now clears the turn circle, so this exemption is no longer the reason it is skipped");

            // What actually governs the berth: the real hull's stern against the real jetty end,
            // box to box rather than centre to box.
            var stern = berth.z - HullHalfLength;
            var gap = stern - jetty.max.z;
            Assert.AreEqual(AuthoredBerthGap, gap, 0.001f, "the authored stern gap moved");
            Assert.GreaterOrEqual(gap, MinBerthGap,
                "the berthed boat would grind against the deck rather than lie alongside it");
        }

        // 29
        [Test]
        public void TheBerthedHullClearsTheJettyAndTheLedge()
        {
            var berth = Route().Waypoints[0];
            var hull = new Bounds(berth, new Vector3(HullHalfWidth * 2f, 1f, HullHalfLength * 2f));

            var jetty = BoxOf(Require(DockName));
            Assert.IsFalse(FlatIntersects(hull, jetty), "the berthed hull overlaps the jetty");
            Assert.GreaterOrEqual(hull.min.z - jetty.max.z, MinBerthGap, "the berth is too close to the jetty");

            var ledge = BoxOf(Require("Shore_Ledge"));
            Assert.IsFalse(FlatIntersects(hull, ledge), "the berthed hull overlaps the shore ledge");
            Assert.Greater(hull.min.z - ledge.max.z, 0f, "the berthed hull reaches the shore");

            // Close enough to board, far enough to read as boat rather than deck.
            Assert.Less(hull.min.z - jetty.max.z, HullHalfLength,
                "the berth drifted out of boarding range of the deck");
        }

        // 30
        [Test]
        public void EveryWaypointStaysInsideTheArenaWalls()
        {
            var limit = ArenaInnerExtent - EnvelopeHalfLength - WallMargin;
            Assert.AreEqual(11f, limit, 0.001f, "the arena inset moved");

            foreach (var waypoint in Route().Waypoints)
            {
                Assert.LessOrEqual(Mathf.Abs(waypoint.x), limit, "waypoint " + waypoint + " reaches the arena wall");
                Assert.LessOrEqual(Mathf.Abs(waypoint.z), limit, "waypoint " + waypoint + " reaches the arena wall");
            }
        }

        // 31
        [Test]
        public void TheRouteStaysOutOfTheSpawnRingTheFishBoxAndTheEvent()
        {
            var route = Route();
            var reach = new Vector3(FishWanderRadius, FishWanderRadius * FishVerticalWanderFactor, FishWanderRadius);
            var fish = new Bounds(FishHome, reach * 2f);
            var special = new Bounds(EventCenter, Vector3.one * (EventRadius * 2f));
            var spawns = FindAll<PlayerSpawnPoint>(scene);
            Assert.AreEqual(4, spawns.Count, "DiveTestArea must hold four spawn points");

            for (var leg = 0; leg + 1 < route.Waypoints.Count; leg++)
            {
                var corners = SweptEnvelope(route.Waypoints[leg], route.Waypoints[leg + 1]);
                Assert.IsFalse(Overlaps(corners, fish),
                    "leg " + leg + " sweeps the fish wander box; the fish would be pinned against the hull");
                Assert.IsFalse(Overlaps(corners, special),
                    "leg " + leg + " sweeps the special event volume");

                foreach (var spawn in spawns)
                    Assert.IsFalse(Overlaps(corners, new Bounds(spawn.transform.position, Vector3.one * 2f)),
                        "leg " + leg + " sweeps over " + spawn.name + "; a diver would spawn under the hull");
            }

            foreach (var spawn in spawns)
                Assert.AreEqual(SpawnRingExtent, Mathf.Abs(spawn.transform.position.x), 0.001f,
                    spawn.name + " left the spawn ring");
        }

        // --- The region -------------------------------------------------------------------------------

        // 32
        [Test]
        public void TheRegionCoversTheWholeWaterFootprint()
        {
            var region = Region();
            Assert.AreEqual(RegionName, region.name, "the region object was renamed");
            Assert.AreEqual(RegionId, region.RegionId, "the region id moved");
            Assert.IsTrue(region.Bounds.IsValid, "the region's extents are degenerate");
            Assert.AreEqual(-RegionExtent, region.Bounds.MinX, 0.001f, "region minX");
            Assert.AreEqual(RegionExtent, region.Bounds.MaxX, 0.001f, "region maxX");
            Assert.AreEqual(-RegionExtent, region.Bounds.MinZ, 0.001f, "region minZ");
            Assert.AreEqual(RegionExtent, region.Bounds.MaxZ, 0.001f, "region maxZ");

            // Every corner of the water, so no reachable spot is off the map.
            var water = BoxOf(Require("SwimVolume"));
            foreach (var corner in new[]
                     {
                         new Vector3(water.min.x, 0f, water.min.z),
                         new Vector3(water.max.x, 0f, water.min.z),
                         new Vector3(water.min.x, 0f, water.max.z),
                         new Vector3(water.max.x, 0f, water.max.z)
                     })
                Assert.IsTrue(region.Bounds.Contains(corner.x, corner.z),
                    "water at " + corner + " has no place on the map");

            // And every point this phase authored converts.
            foreach (var point in Route().Waypoints)
                Assert.IsTrue(region.TryWorldToMap(point, out _), point + " is outside the region");

            foreach (var anchor in new[] { Anchor(DockAnchorName), Anchor(AnchorName) })
            {
                Assert.IsTrue(region.TryWorldToMap(anchor.WorldPosition, out _),
                    anchor.name + " is outside the region");
                Assert.IsTrue(region.TryWorldToMap(anchor.BoardingPosition, out _),
                    anchor.name + "'s boarding spot is outside the region");
            }

            // The anchorage's own map coordinate, so a drifted region fails here rather than only
            // in Mert's UI: 9 of 15 is 0.8 across, 8.5 of 15 is 0.7833 up.
            Assert.IsTrue(region.TryWorldToMap(AnchorPosition, out var map));
            Assert.AreEqual(0.8f, map.x, 1e-4f, "the anchorage's mapX");
            Assert.AreEqual(0.7833f, map.y, 1e-3f, "the anchorage's mapZ");
        }

        // 33
        [Test]
        public void APointOutsideTheArenaIsRefusedRatherThanClamped()
        {
            var region = Region();

            Assert.IsFalse(region.TryWorldToMap(new Vector3(RegionExtent + 0.01f, SurfaceY, 0f), out var map),
                "a point past the arena wall was accepted");
            Assert.AreEqual(Vector2.zero, map, "a refused point came back as a coordinate");

            Assert.IsFalse(region.TryMapToWorld(new Vector2(1.01f, 0.5f), out var world),
                "a map coordinate past the square's edge was accepted");
            Assert.AreEqual(Vector2.zero, world, "a refused map coordinate came back as a place");

            // The square's own edge still converts, so this is a boundary and not a wall.
            Assert.IsTrue(region.TryMapToWorld(new Vector2(1f, 1f), out var corner));
            Assert.AreEqual(RegionExtent, corner.x, 0.001f, "the region's far corner moved");
            Assert.AreEqual(RegionExtent, corner.y, 0.001f, "the region's far corner moved");
        }

        // --- Regression --------------------------------------------------------------------------------

        // 34
        [Test]
        public void TheP32BeachAndItsBoatPartsAreUnchanged()
        {
            var platform = Require("Beach_Platform");
            Assert.AreEqual(PlatformCenter, platform.transform.position, "Beach_Platform moved");
            Assert.AreEqual(PlatformSize, platform.transform.localScale, "Beach_Platform resized");

            var wade = Require("Beach_Wade");
            Assert.AreEqual(WadeCenter.x, wade.transform.position.x, 0.001f, "Beach_Wade x");
            Assert.AreEqual(WadeCenter.y, wade.transform.position.y, 0.001f, "Beach_Wade y");
            Assert.AreEqual(WadeCenter.z, wade.transform.position.z, 0.001f, "Beach_Wade z");
            Assert.AreEqual(WadeSize.z, wade.transform.localScale.z, 0.001f, "Beach_Wade slope length");
            Assert.AreEqual(WadeAngleDegrees, Vector3.Angle(wade.transform.up, Vector3.up), 0.01f,
                "Beach_Wade slope moved");

            var parts = FindAll<BoatPartAnchor>(scene);
            Assert.AreEqual(3, parts.Count, "the beach must still hold exactly three boat part anchors");
            CollectionAssert.AreEquivalent(BoatRepairParts.All, parts.Select(p => p.PartId).ToArray(),
                "the boat part ids changed");

            Assert.AreEqual(HullPartPosition, Require("BoatPart_Hull").transform.position, "BoatPart_Hull moved");
            Assert.AreEqual(FuelTankPosition, Require("BoatPart_FuelTank").transform.position,
                "BoatPart_FuelTank moved");
            var engine = Require("BoatPart_Engine").transform.position;
            Assert.AreEqual(EnginePosition.x, engine.x, 0.001f, "BoatPart_Engine x");
            Assert.AreEqual(EnginePosition.y, engine.y, 0.001f, "BoatPart_Engine y");
            Assert.AreEqual(EnginePosition.z, engine.z, 0.001f, "BoatPart_Engine z");
        }

        // 35
        [Test]
        public void TheP31ShoreTheWaterAuthorityAndTheReturnPadAreUnchanged()
        {
            var ledge = Require("Shore_Ledge");
            Assert.AreEqual(LedgeCenter, ledge.transform.position, "Shore_Ledge moved");
            Assert.AreEqual(LedgeSize, ledge.transform.localScale, "Shore_Ledge resized");

            var ramp = Require("Shore_Ramp");
            Assert.AreEqual(25f, Vector3.Angle(ramp.transform.up, Vector3.up), 0.01f, "Shore_Ramp slope moved");

            // The jetty reads the water; it never opens it. One volume, one body, one waterline.
            var field = Field();
            Assert.AreEqual(1, field.VolumeCount, "WaterField must still read exactly one SwimVolume");
            Assert.AreEqual(1, field.Bodies.Count, "WaterField must still carry exactly one body");
            Assert.AreEqual(SurfaceY, field.Bodies[0].SurfaceY, 0.001f, "the waterline moved");

            var swim = Require("SwimVolume");
            Assert.AreEqual(SwimCenter, swim.transform.position, "SwimVolume moved");
            Assert.AreEqual(SwimSize, swim.transform.localScale, "SwimVolume resized");

            var exit = Require("DiveExit");
            Assert.AreEqual(ExitCenter, exit.transform.position, "DiveExit moved");
            var exitBox = exit.GetComponent<BoxCollider>();
            Assert.AreEqual(ExitSize, exitBox.size, "DiveExit resized");
            Assert.IsTrue(exitBox.isTrigger, "DiveExit must stay a trigger");
        }
    }
}
