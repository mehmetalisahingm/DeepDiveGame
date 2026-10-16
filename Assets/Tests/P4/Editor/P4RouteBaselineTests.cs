using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #172: locks the route evidence written by DeepDive.Editor.P4RouteAudit against the live layout. The routes
    // are read from the evidence file, not copied here, and walked again with the real CharacterController
    // (ContinuousTownWalkTests.Drive). A layout/anchor change fails this test: re-run the audit and update
    // docs/reports/P4-ROUTE-172.md. This is an EditMode geometry baseline, not 1/2/4-process or playtest proof.
    public sealed class P4RouteBaselineTests
    {
        private const string EvidencePath = "docs/reports/evidence/p172-route/current-routes.json";
        private const float Radius = 0.35f;
        private const float Height = 1.8f;
        private const float Skin = 0.08f;

        [Serializable] private sealed class Evidence
        {
            public Route[] routes;
            public Point[] anchors;
            public float[] spawnToNearestBedExit;
        }

        [Serializable] private sealed class Route
        {
            public string id;
            public Vector2[] waypoints;
            public float plannedLength;
            public bool expectsSea, reachedSea, fell, stuck;
            public string[] failures;
        }

        [Serializable] private sealed class Point
        {
            public string id;
            public Vector3 position;
        }

        private static Evidence Load()
        {
            Assert.IsTrue(File.Exists(EvidencePath), EvidencePath + " missing: run DeepDive.Editor.P4RouteAudit.Run");
            var evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(EvidencePath));
            Assert.IsNotNull(evidence.routes);
            Assert.IsNotNull(evidence.anchors);
            return evidence;
        }

        private static Route RouteOf(Evidence evidence, string id)
        {
            var route = evidence.routes.SingleOrDefault(r => r.id == id);
            Assert.IsNotNull(route, "route " + id + " not in the evidence");
            return route;
        }

        [Test]
        public void RecordedRoutesAreCleanAndTheirLengthsMatchTheirWaypoints()
        {
            var evidence = Load();
            CollectionAssert.AreEquivalent(new[] { "a0", "a1", "a2", "a3", "b", "c", "d1", "d2", "e" }, evidence.routes.Select(r => r.id));
            foreach (var route in evidence.routes)
            {
                Assert.IsFalse(route.fell, route.id + " fell");
                Assert.IsFalse(route.stuck, route.id + " stuck");
                Assert.IsEmpty(route.failures, route.id + ": " + string.Join("\n", route.failures));
                Assert.AreEqual(route.expectsSea, route.reachedSea, route.id + " sea outcome");
                var length = 0f;
                for (var i = 1; i < route.waypoints.Length; i++) length += Vector2.Distance(route.waypoints[i - 1], route.waypoints[i]);
                Assert.AreEqual(route.plannedLength, length, 0.01f, route.id + " planned length");
            }
        }

        [Test]
        public void RecordedAnchorsAndTheSpawnBedFindingMatchTheLiveLayout()
        {
            var evidence = Load();
            ContinuousTownScene.With(scene =>
            {
                foreach (var id in WorldAnchorIds.All)
                {
                    Assert.IsTrue(WorldAnchors.TryGetPose(id, out var pose), id);
                    var recorded = evidence.anchors.SingleOrDefault(a => a.id == id);
                    Assert.IsNotNull(recorded, id + " not in the evidence");
                    Assert.Less(Vector3.Distance(recorded.position, pose.position), 0.01f, id + " moved since the #172 audit");
                }
                // Each a{i} route starts on its own home spawn; c ends on bed-3's exit pose.
                for (var i = 0; i < WorldAnchorIds.HomeSpawnCount; i++)
                    Assert.Less(Vector2.Distance(RouteOf(evidence, "a" + i).waypoints[0], XZ(ContinuousTownLayout.HomeRig.Spawn(i))), 0.01f, "a" + i);

                var exits = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HomeInteractionAnchor>(true))
                    .Where(b => b.Kind == HomeInteractionKind.Bed).Select(b => XZ(b.ExitPose.position)).ToArray();
                Assert.AreEqual(4, exits.Length);
                var c = RouteOf(evidence, "c");
                Assert.IsTrue(exits.Any(e => Vector2.Distance(e, c.waypoints[c.waypoints.Length - 1]) < 0.01f), "c must end on a bed exit");
                Assert.AreEqual(WorldAnchorIds.HomeSpawnCount, evidence.spawnToNearestBedExit.Length);
                for (var i = 0; i < WorldAnchorIds.HomeSpawnCount; i++)
                {
                    var spawn = XZ(ContinuousTownLayout.HomeRig.Spawn(i));
                    var nearest = exits.Min(e => Vector2.Distance(spawn, e));
                    Assert.AreEqual(evidence.spawnToNearestBedExit[i], nearest, 0.01f, "spawn " + i + " to nearest bed exit");
                    // Locks the known #173 finding: wider than the bare capsule, narrower than two skins.
                    Assert.GreaterOrEqual(nearest, 2f * Radius, "spawn " + i + " overlaps a bed exit");
                }
            }, furnitureProxies: true);
        }

        // d2's first leg climbs Shore_Ramp out of the water; the shared driver treats a below-8.35 slope other than
        // the wade as a drop, so the replay starts on DiveExit (its second waypoint). The audit drove the full leg.
        [TestCase("a0")]
        [TestCase("a1")]
        [TestCase("a2")]
        [TestCase("a3")]
        [TestCase("b")]
        [TestCase("c")]
        [TestCase("d1")]
        [TestCase("d2")]
        [TestCase("e")]
        public void RecordedRouteStillWalksWithTheRealController(string id)
        {
            var route = RouteOf(Load(), id);
            var waypoints = id == "d2" ? route.waypoints.Skip(1).ToArray() : route.waypoints;
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldZoneField.TryFind(out var zones));
                var diver = new GameObject("Test_P172_" + id);
                SceneManager.MoveGameObjectToScene(diver, scene);
                try
                {
                    var controller = diver.AddComponent<CharacterController>();
                    controller.height = Height;
                    controller.radius = Radius;
                    controller.center = new Vector3(0f, Height * 0.5f, 0f);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.3f;
                    controller.skinWidth = Skin;
                    Physics.SyncTransforms();

                    var failures = new List<string>();
                    ContinuousTownWalkTests.Drive(controller, zones, waypoints, failures, out var reachedSea);
                    Assert.IsEmpty(failures, id + ": " + string.Join("\n", failures.Take(20)));
                    Assert.AreEqual(route.expectsSea, reachedSea, id + " sea outcome");
                    if (!route.expectsSea)
                        Assert.Less(Vector2.Distance(XZ(diver.transform.position), waypoints[waypoints.Length - 1]), 0.2f, id + " did not arrive");
                }
                finally
                {
                    Object.DestroyImmediate(diver);
                }
            }, furnitureProxies: true);
        }

        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
    }
}
