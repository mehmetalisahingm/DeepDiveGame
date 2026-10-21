using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #188: locks the repair-area evidence written by DeepDive.Editor.P48RepairAreaAudit against the live coast. The
    // parts, the berth and the measured points are read from the evidence file, not copied here; the dry carry paths
    // are walked again with a real CharacterController. A part/berth/coast change fails this test: re-run the audit
    // and update docs/reports/P48-REPAIR-AREA-188.md. EditMode geometry baseline only, not 1/2/4-process proof.
    public sealed class P48RepairAreaBaselineTests
    {
        private const string EvidencePath = "docs/reports/evidence/p188-repair/current-repair-area.json";
        private const float Radius = 0.35f, Height = 1.8f, Skin = 0.08f, Step = 0.3f, WaterSurfaceY = 8f;

        [Serializable] private sealed class Evidence { public Part[] parts; public Boat boat; public Site[] sites; public Carry[] carries; }
        [Serializable] private sealed class Part { public string id; public Vector3 position; public bool onDryWalkSurface; }
        [Serializable] private sealed class Boat { public Vector3 berth, boardingPoint; public float yaw; }
        [Serializable] private sealed class Point { public string id; public Vector3 position; }
        [Serializable] private sealed class Site { public string id; public Point[] workers; public Point drop; public Carry[] carries, dropCarries; }
        [Serializable] private sealed class Carry { public string id; public Vector3[] path; public Vector3 end; public float swimLength; public bool reached, fell, stuck; public string[] failures; }

        private static Evidence Load()
        {
            Assert.IsTrue(File.Exists(EvidencePath), EvidencePath + " missing: run DeepDive.Editor.P48RepairAreaAudit.Run");
            var evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(EvidencePath));
            Assert.IsNotNull(evidence.parts);
            Assert.IsNotNull(evidence.sites);
            return evidence;
        }

        private static IEnumerable<Carry> Carries(Site site) => site.carries.Concat(site.dropCarries ?? new Carry[0]);

        private static IEnumerable<T> All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));

        [Test]
        public void RecordedPartsAndBerthMatchTheLiveScene()
        {
            var evidence = Load();
            ContinuousTownScene.With(scene =>
            {
                var anchors = All<BoatPartAnchor>(scene).ToArray();
                CollectionAssert.AreEquivalent(BoatRepairParts.All, anchors.Select(a => a.PartId));
                foreach (var anchor in anchors)
                {
                    var recorded = evidence.parts.SingleOrDefault(p => p.id == anchor.PartId);
                    Assert.IsNotNull(recorded, anchor.PartId + " not in the evidence");
                    Assert.Less(Vector3.Distance(recorded.position, anchor.WorldPosition), 0.01f, anchor.PartId + " moved since the #188 audit");
                }
                var path = All<DiveRoutePath>(scene).Single(p => p.RouteId == BoatTripIds.NearRouteId);
                var forward = Vector3.ProjectOnPlane(path.Waypoints[1] - path.Waypoints[0], Vector3.up).normalized;
                var berth = path.Waypoints[0] + forward * BoatHullSeatRules.DockOffsetMeters(BoatHullKind.Rowboat);
                Assert.Less(Vector3.Distance(evidence.boat.berth, berth), 0.01f, "the rowboat berth moved since the #188 audit");
            }, furnitureProxies: true);
        }

        [Test]
        public void RecordedCarriesAreCleanAndReachTheirGoal()
        {
            var evidence = Load();
            var carries = evidence.sites.SelectMany(Carries).ToArray();
            Assert.IsNotEmpty(carries);
            foreach (var carry in carries)
            {
                Assert.IsTrue(carry.reached, carry.id + " did not reach its goal: " + string.Join("; ", carry.failures));
                Assert.IsFalse(carry.fell, carry.id + " fell");
                Assert.IsFalse(carry.stuck, carry.id + " stuck");
            }
        }

        [Test]
        public void RecordedWorkAndDropPointsStillStandOnDryFreeGround()
        {
            var evidence = Load();
            ContinuousTownScene.With(scene =>
            {
                var swim = All<SwimVolume>(scene).Select(s => s.GetComponent<BoxCollider>().bounds).ToArray();
                var points = evidence.sites.SelectMany(s => s.workers.Concat(s.drop != null && !string.IsNullOrEmpty(s.drop.id) ? new[] { s.drop } : new Point[0])).ToArray();
                Assert.IsNotEmpty(points);
                foreach (var point in points)
                {
                    var p = point.position;
                    Assert.IsTrue(Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore), point.id + " has no ground");
                    Assert.GreaterOrEqual(hit.point.y, WaterSurfaceY + 0.01f, point.id + " is not above the water surface");
                    Assert.IsFalse(swim.Any(b => b.Contains(hit.point + Vector3.up * 0.05f)), point.id + " is inside the SwimVolume");
                    var r = Radius + Skin;
                    var blockers = Physics.OverlapCapsule(hit.point + Vector3.up * (r + Step), hit.point + Vector3.up * (Height - r), r, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c != hit.collider).Select(c => c.name).ToArray();
                    Assert.IsEmpty(blockers, point.id + " overlaps " + string.Join(", ", blockers));
                }
            }, furnitureProxies: true);
        }

        // The proposed repair-drop-0 (S1): every work point and the cradle rim walk to it, and it walks on to the pier
        // boarding point, with the real controller.
        [Test]
        public void ProposedDropIsWalkedFromEveryWorkPointTheCradleAndOnToBoarding()
        {
            var evidence = Load();
            var s1 = evidence.sites.Single(s => s.id == "S1");
            Assert.IsNotNull(s1.drop);
            Assert.IsNotNull(s1.dropCarries);
            var ids = s1.workers.Select((w, i) => "S1:work-" + i + ":drop").Concat(new[] { "S1:cradle:drop", "S1:drop:boarding" }).ToArray();
            CollectionAssert.AreEquivalent(ids, s1.dropCarries.Select(c => c.id));
            foreach (var carry in s1.dropCarries)
            {
                Assert.IsTrue(carry.reached && !carry.fell && !carry.stuck, carry.id + ": " + string.Join("; ", carry.failures));
                Assert.AreEqual(0f, carry.swimLength, carry.id + " left dry land");
                var goal = carry.id.EndsWith(":boarding", StringComparison.Ordinal) ? evidence.boat.boardingPoint : s1.drop.position;
                Assert.Less(Vector2.Distance(new Vector2(carry.end.x, carry.end.z), new Vector2(goal.x, goal.z)), 0.3f, carry.id + " ended away from its goal");
            }
        }

        // Dry legs only (swimLength 0): the walk part of the evidence, replayed on the real controller with gravity.
        [Test]
        public void RecordedDryCarryPathsStillWalkWithTheRealController()
        {
            var evidence = Load();
            var dry = evidence.sites.SelectMany(Carries).Where(c => c.swimLength <= 0f && c.path.Length >= 2).ToArray();
            Assert.IsNotEmpty(dry);
            ContinuousTownScene.With(scene =>
            {
                foreach (var carry in dry)
                {
                    var diver = new GameObject("Test_P188_" + carry.id);
                    SceneManager.MoveGameObjectToScene(diver, scene);
                    try
                    {
                        var cc = diver.AddComponent<CharacterController>();
                        cc.height = Height; cc.radius = Radius; cc.center = new Vector3(0f, Height * 0.5f, 0f);
                        cc.slopeLimit = 45f; cc.stepOffset = Step; cc.skinWidth = Skin;
                        diver.transform.position = carry.path[0] + Vector3.up * Skin;
                        Physics.SyncTransforms();
                        var vy = 0f;
                        for (var w = 1; w < carry.path.Length; w++)
                        {
                            var target = new Vector2(carry.path[w].x, carry.path[w].z);
                            for (var guard = 0; ; guard++)
                            {
                                var here = new Vector2(diver.transform.position.x, diver.transform.position.z);
                                var to = target - here;
                                if (to.magnitude <= 0.05f) break;
                                Assert.Less(guard, 3000, carry.id + $" stuck at ({here.x:0.##}, {here.y:0.##})");
                                var step = Vector2.ClampMagnitude(to, 4f * 0.02f);
                                vy = cc.isGrounded ? -1f : vy - 9.81f * 0.02f;
                                cc.Move(new Vector3(step.x, vy * 0.02f, step.y));
                                Assert.Greater(diver.transform.position.y, WaterSurfaceY, carry.id + " dropped into the water");
                            }
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(diver);
                    }
                }
            }, furnitureProxies: true);
        }
    }
}
