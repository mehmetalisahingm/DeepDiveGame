using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Guards the P4.3-B2 (#108) half of DiveTestArea: the reef shelf and deep basin north of the P3 arena, the reef
    // and deep anchorages and routes, the grown water and walls, the north surface lid - all produced by
    // DiveTestAreaReefDeepSetup.Apply. If this fails, running DeepDive/P4.3/Dive test area: reef and deep is the
    // repair, not hand-editing the scene.
    //
    // Expectations are written out rather than read from the setup script (this assembly does not reference
    // DeepDive.World.Editor), like every other DiveTestArea scene test. EditMode, nothing here writes the scene.
    public class DiveTestAreaReefDeepSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        private const float SurfaceY = 8f;
        private const float RegionMinX = -15f, RegionMaxX = 15f, RegionMinZ = -15f, RegionMaxZ = 65f;

        private const float ReefShelfTopY = -8f;      // 16 m
        private const float DeepBasinTopY = -26f;     // 34 m
        private const float WaterBottomY = -27f;      // 35 m

        private static readonly Vector3 ReefAnchor = new Vector3(3f, 8f, 30f);
        private static readonly Vector3 DeepAnchor = new Vector3(0f, 8f, 56f);
        private const float BoardingOffsetX = 3f;

        private static readonly Vector3[] NearWaypoints =
        {
            new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 3.5f), new Vector3(9f, 8f, 8.5f)
        };

        private static readonly Vector3[] ReefWaypoints =
        {
            new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 3.5f), new Vector3(6f, 8f, 14f), new Vector3(3f, 8f, 30f)
        };

        private static readonly Vector3[] DeepWaypoints =
        {
            new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 3.5f), new Vector3(6f, 8f, 14f),
            new Vector3(3f, 8f, 36f), new Vector3(0f, 8f, 56f)
        };

        // The research vessel (BoatHullSeatRules, 4.4 x 10) plus the 0.25 m margin P3.3 froze, and the frozen draft.
        private const float EnvelopeMargin = 0.25f;
        private const float HullDraft = 0.5f;
        private const float WallInnerX = 14.75f, WallInnerMinZ = -14.75f, WallInnerMaxZ = 64.75f, WallMargin = 1f;

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

        // --- Helpers -----------------------------------------------------------------------------------------

        private GameObject Require(string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            Assert.Fail("DiveTestArea is missing " + name + "; run DeepDive/P4.3/Dive test area: reef and deep");
            return null;
        }

        private List<T> FindAll<T>() where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        private WaterField Field()
        {
            var fields = FindAll<WaterField>();
            Assert.AreEqual(1, fields.Count);
            fields[0].RebuildVolumes();
            return fields[0];
        }

        private DiveRoutePath Route(string routeId)
        {
            var paths = FindAll<DiveRoutePath>().Where(p => p.RouteId == routeId).ToList();
            Assert.AreEqual(1, paths.Count, "exactly one path must carry " + routeId);
            paths[0].Refresh();
            return paths[0];
        }

        private RouteAnchor Anchor(string anchorId)
        {
            var anchors = FindAll<RouteAnchor>().Where(a => a.AnchorId == anchorId).ToList();
            Assert.AreEqual(1, anchors.Count, "exactly one anchor must carry " + anchorId);
            return anchors[0];
        }

        private static Bounds BoxOf(GameObject piece)
        {
            var box = piece.GetComponent<BoxCollider>();
            return new Bounds(box.transform.TransformPoint(box.center), Vector3.Scale(box.size, box.transform.lossyScale));
        }

        // The top face of a (possibly tilted) unit-box slab: its two edge midpoints along local z.
        private static (Vector3 Near, Vector3 Far) TopEdge(GameObject slab)
        {
            var t = slab.transform;
            return (t.TransformPoint(new Vector3(0f, 0.5f, -0.5f)), t.TransformPoint(new Vector3(0f, 0.5f, 0.5f)));
        }

        private static WaterProbe Diver(Vector3 feet) =>
            new WaterProbe(feet, new Vector3(0f, 0.9f, 0f), 0.35f, 1.8f, Vector3.up);

        private static void Research(out float halfWidth, out float halfLength)
        {
            Assert.IsTrue(BoatHullSeatRules.TryGetDimensions(BoatHullKind.ResearchVessel, out var hull));
            halfWidth = hull.Width * 0.5f + EnvelopeMargin;
            halfLength = hull.Length * 0.5f + EnvelopeMargin;
        }

        // --- Region and grid -----------------------------------------------------------------------------------

        [Test]
        public void TheRegionIsTheP3ArenaGrownNorthUnderTheSameId()
        {
            var regions = FindAll<DiveRegionField>();
            Assert.AreEqual(1, regions.Count, "one region, never a second one for reef or deep");
            Assert.AreEqual(ExplorationIds.NearRegionId, regions[0].RegionId);
            var b = regions[0].Bounds;
            Assert.AreEqual(RegionMinX, b.MinX, 1e-4f);
            Assert.AreEqual(RegionMaxX, b.MaxX, 1e-4f);
            Assert.AreEqual(RegionMinZ, b.MinZ, 1e-4f);
            Assert.AreEqual(RegionMaxZ, b.MaxZ, 1e-4f);
        }

        [Test]
        public void TheGridIs6By16AndEveryP41CellIdIsStillInIt()
        {
            var region = FindAll<DiveRegionField>()[0];
            var layout = new ExplorationCellLayout(region.RegionId, region.Bounds);

            Assert.AreEqual(-3, layout.Grid.MinGridX);
            Assert.AreEqual(-3, layout.Grid.MinGridZ);
            Assert.AreEqual(6, layout.Grid.Columns);
            Assert.AreEqual(16, layout.Grid.Rows);
            Assert.AreEqual(96, layout.CellCount);

            // The 36 cells of the P4.1 arena keep their ids: a save written before P4.3 restores into this grid.
            var fog = new ExplorationCellAuthority(region.RegionId, region.Bounds, Field().Bodies);
            for (var gz = -3; gz <= 2; gz++)
            for (var gx = -3; gx <= 2; gx++)
            {
                var id = "region-near-1:gx" + gx + ":gz" + gz;
                Assert.AreEqual(CellRestoreOutcome.Restored, fog.TryRestore(id, gx, gz, DepthBandIds.Shallow), id);
            }

            Assert.AreEqual(36, fog.DiscoveredCount);
        }

        [Test]
        public void OldCellsKeepTheirMapUAndOnlyVIsRescaled()
        {
            var region = FindAll<DiveRegionField>()[0];
            var grid = new ExplorationCellLayout(region.RegionId, region.Bounds).Grid;

            Assert.IsTrue(ExplorationMapProjection.TryCellCenter(grid, 0, 0, out var u, out var v));
            Assert.AreEqual(3.5f / 6f, u, 1e-5f, "u is unchanged (x extent unchanged)");
            Assert.AreEqual(3.5f / 16f, v, 1e-5f, "v is rescaled to 16 rows");
        }

        // --- Sea bed and bands -------------------------------------------------------------------------------

        [Test]
        public void TheSeaBedFollowsTheDocumentedProfile()
        {
            Assert.AreEqual(ReefShelfTopY, BoxOf(Require("Reef_Shelf")).max.y, 1e-3f, "reef shelf top");
            Assert.AreEqual(DeepBasinTopY, BoxOf(Require("Deep_Basin")).max.y, 1e-3f, "deep basin top");

            var reefDrop = TopEdge(Require("Reef_DropOff"));
            Assert.AreEqual(new Vector3(0f, 0f, 15f).y, reefDrop.Near.y, 1e-3f);
            Assert.AreEqual(15f, reefDrop.Near.z, 1e-3f);
            Assert.AreEqual(ReefShelfTopY, reefDrop.Far.y, 1e-3f);
            Assert.AreEqual(19f, reefDrop.Far.z, 1e-3f);

            var deepDrop = TopEdge(Require("Deep_DropOff"));
            Assert.AreEqual(ReefShelfTopY, deepDrop.Near.y, 1e-3f);
            Assert.AreEqual(40f, deepDrop.Near.z, 1e-3f);
            Assert.AreEqual(DeepBasinTopY, deepDrop.Far.y, 1e-3f);
            Assert.AreEqual(46f, deepDrop.Far.z, 1e-3f);

            foreach (var name in new[] { "Reef_DropOff", "Reef_Shelf", "Deep_DropOff", "Deep_Basin", "Reef_Head_0", "Reef_Head_1", "Reef_Head_2" })
            {
                var piece = Require(name);
                Assert.IsFalse(piece.GetComponent<BoxCollider>().isTrigger, name + " must be solid");
                Assert.IsNull(piece.GetComponent<NetworkObject>(), name + " is scenery, not networked");
                Assert.AreEqual(0, piece.layer, name + " must be on Default, the occluder layer");
            }
        }

        [Test]
        public void TheReefShelfBottomIsReefAndTheBasinBottomIsDeep()
        {
            var water = Field().Bodies;
            Assert.IsTrue(WaterDepth.TryClassify(water, new Vector3(ReefAnchor.x, ReefShelfTopY + 0.1f, ReefAnchor.z), out var reef));
            Assert.AreEqual(DepthBandIds.Reef, reef);
            Assert.IsTrue(WaterDepth.TryClassify(water, new Vector3(DeepAnchor.x, DeepBasinTopY + 0.1f, DeepAnchor.z), out var deep));
            Assert.AreEqual(DepthBandIds.Deep, deep);

            foreach (var head in new[] { "Reef_Head_0", "Reef_Head_1", "Reef_Head_2" })
            {
                var top = BoxOf(Require(head)).max;
                Assert.IsTrue(WaterDepth.TryClassify(water, new Vector3(top.x - 0.1f, top.y + 0.1f, top.z - 0.1f), out var band));
                Assert.AreEqual(DepthBandIds.Reef, band, head + " top must still be reef water");
            }
        }

        [Test]
        public void AllThreeBandsAreMeasurableInOneDescentAtTheDeepAnchorage()
        {
            var water = Field().Bodies;
            var expected = new (float Y, string Band)[]
            {
                (4f, DepthBandIds.Shallow), (-6f, DepthBandIds.Reef), (-17f, DepthBandIds.Deep), (DeepBasinTopY, DepthBandIds.Deep)
            };

            foreach (var (y, band) in expected)
            {
                Assert.IsTrue(WaterDepth.TryClassify(water, new Vector3(DeepAnchor.x, y, DeepAnchor.z), out var found), "y=" + y);
                Assert.AreEqual(band, found, "y=" + y);
            }
        }

        [Test]
        public void NoReachableWaterIsDeeperThan35Metres()
        {
            Assert.AreEqual(WaterBottomY, BoxOf(Require("SwimVolume")).min.y, 1e-3f, "the water's floor is the 35 m edge");
            foreach (var name in new[] { "Reef_Shelf", "Deep_Basin" })
                Assert.GreaterOrEqual(BoxOf(Require(name)).max.y, SurfaceY - DiveDepthBands.DeepMaxDepth, name);
            Assert.LessOrEqual(SurfaceY - DeepBasinTopY, DiveDepthBands.DeepMaxDepth);
        }

        // --- Water authority, walls, surface ------------------------------------------------------------------

        [Test]
        public void SwimVolumeIsStillTheOneWaterAuthorityGrownInPlace()
        {
            Assert.AreEqual(1, FindAll<SwimVolume>().Count);
            var field = Field();
            Assert.AreEqual(1, field.VolumeCount);
            Assert.AreEqual(1, field.Bodies.Count);
            var body = field.Bodies[0];
            Assert.AreEqual(SurfaceY, body.SurfaceY, 1e-4f, "the water line did not move");
            Assert.AreEqual(RegionMinX, body.MinX, 1e-4f);
            Assert.AreEqual(RegionMaxX, body.MaxX, 1e-4f);
            Assert.AreEqual(RegionMinZ, body.MinZ, 1e-4f);
            Assert.AreEqual(RegionMaxZ, body.MaxZ, 1e-4f);

            var bands = FindAll<DiveDepthBandSet>();
            Assert.AreEqual(1, bands.Count);
            Assert.AreEqual(SurfaceY, bands[0].SurfaceY, 1e-4f, "the P3.2 band set still agrees with the water line");
        }

        [Test]
        public void TheWallsEncloseTheGrownWater()
        {
            Assert.AreEqual(new Vector3(0f, -10f, 65f), Require("Wall_N").transform.position);
            Assert.AreEqual(new Vector3(30f, 34f, 0.5f), Require("Wall_N").transform.localScale);
            Assert.AreEqual(new Vector3(15f, -10f, 25f), Require("Wall_E").transform.position);
            Assert.AreEqual(new Vector3(-15f, -10f, 25f), Require("Wall_W").transform.position);
            Assert.AreEqual(new Vector3(0.5f, 34f, 80f), Require("Wall_E").transform.localScale);
            Assert.AreEqual(new Vector3(0.5f, 34f, 80f), Require("Wall_W").transform.localScale);

            // Wall_S is P1's and untouched.
            Assert.AreEqual(new Vector3(0f, 3f, -15f), Require("Wall_S").transform.position);
            Assert.AreEqual(new Vector3(30f, 8f, 0.5f), Require("Wall_S").transform.localScale);

            foreach (var name in new[] { "Wall_N", "Wall_E", "Wall_W" })
            {
                var box = BoxOf(Require(name));
                Assert.AreEqual(7f, box.max.y, 1e-3f, name + " top stays 1 m under the surface");
                Assert.AreEqual(WaterBottomY, box.min.y, 1e-3f, name + " reaches the water's floor");
            }
        }

        [Test]
        public void TheNorthSurfaceLidCoversTheAnchoragesWithoutACollider()
        {
            var lid = Require("WaterSurface_North");
            Assert.IsNull(lid.GetComponent<Collider>(), "the surface must never block a diver");
            var renderer = lid.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreSame(Require("WaterSurface").GetComponent<MeshRenderer>().sharedMaterial, renderer.sharedMaterial);
            Assert.AreEqual(Require("WaterSurface").transform.position.y, lid.transform.position.y, 1e-4f);

            var bounds = renderer.bounds;
            foreach (var anchor in new[] { ReefAnchor, DeepAnchor })
                Assert.IsTrue(anchor.x >= bounds.min.x && anchor.x <= bounds.max.x && anchor.z >= bounds.min.z && anchor.z <= bounds.max.z,
                    anchor + " has no surface over it");
            Assert.GreaterOrEqual(bounds.max.z, RegionMaxZ, "the lid reaches the north wall");
        }

        // --- Routes and anchorages ---------------------------------------------------------------------------

        [TestCase("route-reef-1")]
        [TestCase("route-deep-1")]
        public void TheRouteHasItsDocumentedWaypointsAndSharesTheHarbourExitWithNear(string routeId)
        {
            var route = Route(routeId);
            var expected = routeId == BoatTripIds.ReefRouteId ? ReefWaypoints : DeepWaypoints;

            Assert.AreEqual(expected.Length, route.Waypoints.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], route.Waypoints[i], routeId + " WP_" + i);
                Assert.AreEqual("WP_" + i, route.transform.GetChild(i).name);
            }

            var near = Route(BoatTripIds.NearRouteId);
            CollectionAssert.AreEqual(NearWaypoints, near.Waypoints.ToArray(), "the near route is frozen");
            Assert.AreEqual(near.Waypoints[0], route.Waypoints[0], "one berth");
            Assert.AreEqual(near.Waypoints[1], route.Waypoints[1], "one harbour gate");
            Assert.IsNull(route.GetComponent<NetworkObject>());
        }

        [TestCase("route-near-1", "anchor-near-1", VehicleClass.Rowboat, 8f)]
        [TestCase("route-reef-1", "anchor-reef-1", VehicleClass.Motorboat, 28f)]
        [TestCase("route-deep-1", "anchor-deep-1", VehicleClass.ResearchBoat, 52f)]
        public void TheSceneDefinitionIsReadableByIdAndEndsAtItsAnchor(string routeId, string anchorId,
            VehicleClass required, float seconds)
        {
            Assert.IsTrue(DiveRoutePath.TryGetDefinition(routeId, out var definition), routeId);
            Assert.AreEqual(routeId, definition.RouteId);
            Assert.AreEqual(DiveRouteAnchors.Dock, definition.DepartureDockAnchor);
            Assert.AreEqual(anchorId, definition.AnchorPointAnchor);
            Assert.AreEqual(required, definition.RequiredVehicleClass);
            Assert.AreEqual(seconds, definition.OutboundSeconds);
            Assert.AreEqual(seconds, definition.InboundSeconds);

            var route = Route(routeId);
            Assert.AreEqual(Anchor(anchorId).WorldPosition, route.Waypoints[route.Waypoints.Count - 1], "the route ends at its anchor");
        }

        [TestCase("anchor-reef-1", -8f)]
        [TestCase("anchor-deep-1", -26f)]
        public void TheAnchorageIsOpenWaterWithNothingSolidInTheLargestHullsColumn(string anchorId, float seaBedY)
        {
            var anchor = Anchor(anchorId);
            var at = anchor.WorldPosition;
            var tracker = Field().CreateTracker();

            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(new Vector3(at.x, SurfaceY - 1f, at.z))));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, tracker.Classify(Diver(new Vector3(at.x, (SurfaceY + seaBedY) * 0.5f, at.z))));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, tracker.Classify(Diver(new Vector3(at.x, seaBedY + 0.1f, at.z))));

            Research(out var halfWidth, out var halfLength);
            var column = new Bounds(new Vector3(at.x, 0f, at.z), new Vector3(halfWidth * 2f, 1f, halfLength * 2f));
            foreach (var root in scene.GetRootGameObjects())
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                var b = BoxOrBounds(collider);
                if (b.max.y <= seaBedY + 0.01f) continue;
                var overlaps = column.min.x < b.max.x && b.min.x < column.max.x && column.min.z < b.max.z && b.min.z < column.max.z;
                Assert.IsFalse(overlaps, collider.name + " stands in " + anchorId + "'s water column");
            }
        }

        [TestCase("anchor-reef-1")]
        [TestCase("anchor-deep-1")]
        public void BoardingIsOpenWaterBesideTheWidestHull(string anchorId)
        {
            var anchor = Anchor(anchorId);
            Research(out var halfWidth, out _);

            Assert.AreEqual(anchor.WorldPosition + new Vector3(BoardingOffsetX, 0f, 0f), anchor.BoardingPosition);
            Assert.Greater(BoardingOffsetX, halfWidth - EnvelopeMargin, "boarding must be outside the research hull");
            var tracker = Field().CreateTracker();
            Assert.AreEqual(EnvironmentLocomotion.Surface,
                tracker.Classify(Diver(new Vector3(anchor.BoardingPosition.x, SurfaceY - 1f, anchor.BoardingPosition.z))));
        }

        [TestCase("route-reef-1")]
        [TestCase("route-deep-1")]
        public void TheLargestHullClearsTheWorldPastTheHarbourGate(string routeId)
        {
            // Leg 0 (berth -> gate) is the shared, hull-dependent berth leg: #107's measured constraint, not this one.
            var route = Route(routeId);
            Research(out var hw, out var hl);
            var turn = Mathf.Sqrt(hw * hw + hl * hl);

            var obstacles = new List<(string Name, Bounds Box)>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger) continue;
                var b = BoxOrBounds(collider);
                if (b.max.y > SurfaceY - HullDraft) obstacles.Add((collider.name, b));
            }

            for (var leg = 1; leg + 1 < route.Waypoints.Count; leg++)
            {
                var from = route.Waypoints[leg];
                var to = route.Waypoints[leg + 1];
                var forward = new Vector3(to.x - from.x, 0f, to.z - from.z).normalized;
                var right = new Vector3(forward.z, 0f, -forward.x);
                var centre = (from + to) * 0.5f;
                var reach = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z)) * 0.5f + hl;
                var corners = new[]
                {
                    centre - forward * reach - right * hw, centre - forward * reach + right * hw,
                    centre + forward * reach + right * hw, centre + forward * reach - right * hw
                };

                foreach (var c in corners)
                {
                    Assert.LessOrEqual(Mathf.Abs(c.x), WallInnerX - WallMargin, routeId + " leg " + leg + " reaches a side wall");
                    Assert.GreaterOrEqual(c.z, WallInnerMinZ + WallMargin, routeId + " leg " + leg);
                    Assert.LessOrEqual(c.z, WallInnerMaxZ - WallMargin, routeId + " leg " + leg + " reaches the north wall");
                }

                foreach (var (name, box) in obstacles)
                {
                    // Conservative: the swept box's axis-aligned hull against the obstacle.
                    var minX = corners.Min(c => c.x); var maxX = corners.Max(c => c.x);
                    var minZ = corners.Min(c => c.z); var maxZ = corners.Max(c => c.z);
                    var hit = minX < box.max.x && box.min.x < maxX && minZ < box.max.z && box.min.z < maxZ;
                    Assert.IsFalse(hit, routeId + " leg " + leg + " sweeps through " + name);
                }
            }

            for (var i = 1; i + 1 < route.Waypoints.Count; i++)
                foreach (var (name, box) in obstacles)
                {
                    var p = route.Waypoints[i];
                    var dx = Mathf.Max(box.min.x - p.x, 0f, p.x - box.max.x);
                    var dz = Mathf.Max(box.min.z - p.z, 0f, p.z - box.max.z);
                    Assert.GreaterOrEqual(Mathf.Sqrt(dx * dx + dz * dz), turn, routeId + " turns into " + name + " at WP_" + i);
                }
        }

        private static Bounds BoxOrBounds(Collider collider) =>
            collider is BoxCollider box
                ? new Bounds(box.transform.TransformPoint(box.center), RotatedExtents(box))
                : collider.bounds;

        // World-axis bounds of a possibly rotated box, from its eight corners (collider.bounds is not refreshed in
        // EditMode for a freshly opened scene).
        private static Vector3 RotatedExtents(BoxCollider box)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (var i = 0; i < 8; i++)
            {
                var local = box.center + Vector3.Scale(box.size * 0.5f,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var world = box.transform.TransformPoint(local);
                min = Vector3.Min(min, world);
                max = Vector3.Max(max, world);
            }

            return max - min;
        }
    }
}
