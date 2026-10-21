using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // P4.8-B (#188, S1): the beach repair cradle authored by DiveTestAreaRepairCradleSetup and used by
    // BoatRepairNetworkBinding. EditMode geometry and real CharacterController walks on DiveTestArea with the town
    // built on top; not a 1/2/4-process or human acceptance.
    public sealed class P48RepairCradleTests
    {
        private const string DiverPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        private const float WalkY = 8.4f;
        private const float Dt = 0.02f;
        private const float WorkerSpacing = 1.06f;

        private static readonly Vector3 Cradle = new Vector3(3.6f, 8.4f, -12.6f);
        private const float CradleYaw = 90f;
        private static readonly Vector3 Drop = new Vector3(7.5f, 8.4f, -14.5f);
        private static readonly Vector3 TownGate = new Vector3(0f, 8.4f, -13.5f);

        // ---- the contract ------------------------------------------------------------------------

        [Test]
        public void RepairAnchorIdsAreStableKebabCaseAndApartFromTheTownAnchors()
        {
            CollectionAssert.AreEqual(
                new[] { "repair-boat-cradle", "repair-work-0", "repair-work-1", "repair-work-2", "repair-work-3", "repair-drop-0" },
                RepairAnchorIds.All.ToArray());
            var kebab = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");
            foreach (var id in RepairAnchorIds.All) StringAssert.IsMatch(kebab.ToString(), id);
            Assert.AreEqual("", RepairAnchorIds.Work(4));
            Assert.AreEqual("", RepairAnchorIds.Work(-1));
            CollectionAssert.IsEmpty(RepairAnchorIds.All.Intersect(WorldAnchorIds.All), "repair anchors must not alias town anchors");
        }

        // ---- the authored anchors ----------------------------------------------------------------

        [Test]
        public void CradleAnchorsAreAuthoredOnceBareAndWhereS1Decided()
        {
            ContinuousTownScene.With(scene =>
            {
                foreach (var id in RepairAnchorIds.All)
                {
                    Assert.IsTrue(WorldAnchors.TryFind(id, out var anchor), id + " missing or duplicated: run DeepDive/P4/Dive test area: repair cradle anchors (#188)");
                    Assert.AreEqual(scene, anchor.gameObject.scene, id + " scene");
                    Assert.AreEqual("P48_RepairAnchors", anchor.transform.parent.name, id + " parent");
                    Assert.AreEqual(2, anchor.GetComponents<Component>().Length, id + " must be a bare transform + WorldAnchor");
                    Assert.IsNull(anchor.GetComponentInParent<NetworkObject>(), id + " must not be networked");
                }
                WorldAnchors.TryGetPose(RepairAnchorIds.Cradle, out var cradle);
                Assert.That(Vector3.Distance(cradle.position, Cradle), Is.LessThan(1e-3f));
                Assert.That(Mathf.DeltaAngle(cradle.rotation.eulerAngles.y, CradleYaw), Is.EqualTo(0f).Within(0.01f));
                WorldAnchors.TryGetPose(RepairAnchorIds.Drop, out var drop);
                Assert.That(Vector3.Distance(drop.position, Drop), Is.LessThan(1e-3f));
            });
        }

        [Test]
        public void CradleHullLiesOnDryBeachClearOfOtherSolids()
        {
            WithScenery(scene =>
            {
                Assert.IsTrue(BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size));
                var pose = CradlePose();
                var swim = All<SwimVolume>(scene).Select(s => s.GetComponent<BoxCollider>().bounds).ToArray();
                for (var a = -size.Width * 0.5f; a <= size.Width * 0.5f + 1e-4f; a += 0.25f)
                for (var b = -size.Length * 0.5f; b <= size.Length * 0.5f + 1e-4f; b += 0.25f)
                {
                    var q = pose.position + pose.rotation * new Vector3(a, 0f, b);
                    Assert.IsTrue(Physics.Raycast(new Vector3(q.x, 12f, q.z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore));
                    Assert.AreEqual("Beach_Platform", hit.collider.name, "hull footprint at " + q);
                    Assert.AreEqual(WalkY, hit.point.y, 0.01f);
                    Assert.IsFalse(swim.Any(s => s.Contains(hit.point + Vector3.up * 0.05f)), "hull footprint in water at " + q);
                }
                var solids = Physics.OverlapBox(pose.position + Vector3.up * (BoatRepairNetworkBinding.CradleHullHeight * 0.5f + 0.05f),
                        new Vector3(size.Width * 0.5f, BoatRepairNetworkBinding.CradleHullHeight * 0.5f, size.Length * 0.5f), pose.rotation, ~0,
                        QueryTriggerInteraction.Ignore)
                    .Select(c => c.name).ToArray();
                Assert.IsEmpty(solids, "the hull box overlaps " + string.Join(", ", solids));
            }, furnitureProxies: true);
        }

        // ---- working at the cradle ---------------------------------------------------------------

        // Work i (0..2) reaches slot i, work 3 reaches slot 2: eye within the pickup range, and the real E/hammer ray
        // (RaycastAll + PickupRaycastRules) hits the slot trigger, not the low solid hull in front of it. Every work
        // point fits a diver with the hull in place and keeps 1.06 m from the others.
        [Test]
        public void EveryWorkPointReachesItsSlotPastTheLowHull()
        {
            var (eye, reach) = DiverEyeAndReach();
            WithScenery(scene =>
            {
                var site = BuildSite(scene);
                try
                {
                    var work = WorkPoints();
                    for (var i = 0; i < work.Length; i++)
                    {
                        Assert.IsTrue(Fits(work[i]), $"repair-work-{i} at {work[i]} does not fit a diver beside the hull");
                        for (var j = 0; j < i; j++)
                            Assert.GreaterOrEqual(Flat(work[i] - work[j]), WorkerSpacing - 1e-3f, $"repair-work-{j} and -{i} too close");
                        var slotIndex = Mathf.Min(i, 2);
                        var slot = site.transform.TransformPoint(BoatRepairNetworkBinding.SlotLocal(slotIndex));
                        var from = work[i] + Vector3.up * eye;
                        var distance = Vector3.Distance(from, slot);
                        Assert.LessOrEqual(distance, Mathf.Min(reach, 2.65f), $"repair-work-{i} eye is {distance:0.00} m from slot {slotIndex}");
                        var hits = Physics.RaycastAll(from, (slot - from).normalized, reach, ~0, QueryTriggerInteraction.Collide);
                        var first = PickupRaycastRules.SelectFirstPickupOrBlocker(hits);
                        Assert.IsNotNull(first, $"repair-work-{i}: the ray hit nothing");
                        var target = first.GetComponentInParent<BoatRepairSlotTarget>();
                        Assert.IsNotNull(target, $"repair-work-{i}: the ray stopped at {first.name}, not slot {slotIndex}");
                        Assert.AreEqual(slotIndex, target.Index);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(site);
                    Physics.SyncTransforms();
                }
            }, furnitureProxies: true);
        }

        // With the hull and four crew standing on the work points, the shore routes still walk with the real controller:
        // the pier (beach -> ledge -> dock-town-1), the town gate onto the beach, the beach strip from the hull part to
        // the fuel-tank part round the hull, the drop on to boarding, and each work point from the hull part start.
        [Test]
        public void HullAndFourCrewLeaveTheShoreRoutesWalkable()
        {
            WithScenery(scene =>
            {
                var site = BuildSite(scene);
                var work = WorkPoints();
                var crew = work.Select((w, i) => Worker(scene, w, i)).ToArray();
                try
                {
                    Physics.SyncTransforms();
                    var south = Cradle.z - (2.4f * 0.5f + BoatRepairNetworkBinding.RoutePad);
                    var westEnd = Cradle.x - (5f * 0.5f + BoatRepairNetworkBinding.RoutePad);
                    var eastEnd = Cradle.x + (5f * 0.5f + BoatRepairNetworkBinding.RoutePad);
                    var failures = new List<string>();
                    void Walk(string id, params Vector3[] line)
                    {
                        if (!WalkWithController(scene, line, out var why)) failures.Add(id + ": " + why);
                    }
                    // The walker really collides: straight along the strip runs into the hull.
                    Assert.IsFalse(WalkWithController(scene, new[] { P(-12f, -12.5f), P(12.5f, -12.5f) }, out var through),
                        "the cradle hull must block a straight walk through it");
                    StringAssert.Contains("stuck", through);
                    Walk("pier", P(9f, -13.6f), P(9f, -9f), P(9f, -3.9f));
                    Walk("town gate -> beach", P(0f, -18f), TownGate, P(-3f, -12.5f));
                    Walk("beach strip round the hull", P(-12f, -12.5f), P(westEnd, south), P(eastEnd, south), P(12.5f, -12.5f));
                    Walk("drop -> dock-town-1", Drop, P(9f, -13.6f), P(9f, -9f), P(9f, -3.75f));
                    for (var i = 0; i < crew.Length; i++)
                    {
                        crew[i].SetActive(false);
                        Physics.SyncTransforms();
                        var goal = work[i];
                        Walk($"hull part -> repair-work-{i}", i < 2
                            ? new[] { P(-12f, -12.5f), goal }
                            : new[] { P(-12f, -12.5f), P(westEnd, south), goal });
                        crew[i].SetActive(true);
                        Physics.SyncTransforms();
                    }
                    Assert.IsEmpty(failures, string.Join("\n", failures));
                }
                finally
                {
                    foreach (var c in crew) Object.DestroyImmediate(c);
                    Object.DestroyImmediate(site);
                    Physics.SyncTransforms();
                }
            }, furnitureProxies: true);
        }

        // ---- helpers -----------------------------------------------------------------------------

        private static Pose CradlePose()
        {
            Assert.IsTrue(WorldAnchors.TryGetPose(RepairAnchorIds.Cradle, out var pose), "repair-boat-cradle missing");
            return pose;
        }

        private static Vector3[] WorkPoints() => Enumerable.Range(0, RepairAnchorIds.WorkCount).Select(i =>
        {
            Assert.IsTrue(WorldAnchors.TryGetPose(RepairAnchorIds.Work(i), out var pose), RepairAnchorIds.Work(i) + " missing");
            return pose.position;
        }).ToArray();

        // The binding's worksite reduced to what the physics sees: the low solid hull and the three slot triggers.
        private static GameObject BuildSite(Scene scene)
        {
            BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var size);
            var pose = CradlePose();
            var site = new GameObject("Test_P48_Worksite");
            SceneManager.MoveGameObjectToScene(site, scene);
            site.transform.SetPositionAndRotation(pose.position, pose.rotation);
            var hull = site.AddComponent<BoxCollider>();
            hull.center = new Vector3(0f, BoatRepairNetworkBinding.CradleHullHeight * 0.5f, 0f);
            hull.size = new Vector3(size.Width, BoatRepairNetworkBinding.CradleHullHeight, size.Length);
            for (var i = 0; i < 3; i++)
            {
                var slot = new GameObject("RepairSlot-" + i);
                slot.transform.SetParent(site.transform, false);
                slot.transform.localPosition = BoatRepairNetworkBinding.SlotLocal(i);
                slot.AddComponent<BoatRepairSlotTarget>().Index = i;
                var trigger = slot.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(.59f, .8f, .15f);
            }
            Physics.SyncTransforms();
            return site;
        }

        private static GameObject Worker(Scene scene, Vector3 feet, int index)
        {
            var go = new GameObject("Test_P48_Crew_" + index);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = feet;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            return go;
        }

        // Walks the waypoints with a NetworkDiver-sized CharacterController at walk speed with gravity. Fails on a fall
        // off the walk height, on 1 s without progress, or after 30 s.
        private static bool WalkWithController(Scene scene, Vector3[] line, out string why)
        {
            var go = new GameObject("Test_P48_Walker");
            SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
                cc.slopeLimit = 45f; cc.stepOffset = 0.3f; cc.skinWidth = 0.08f;
                go.transform.position = line[0] + Vector3.up * 0.02f;
                Physics.SyncTransforms();
                var fall = 0f;
                var stuck = 0f;
                var t = 0f;
                for (var w = 1; w < line.Length; w++)
                {
                    while (Flat(line[w] - go.transform.position) > 0.12f)
                    {
                        var before = go.transform.position;
                        var dir = line[w] - before; dir.y = 0f;
                        var step = Mathf.Min(4f * Dt, dir.magnitude);
                        fall = cc.isGrounded ? -1f : fall - 9.81f * Dt;
                        cc.Move(dir.normalized * step + Vector3.up * fall * Dt);
                        t += Dt;
                        stuck = Flat(go.transform.position - before) < step * 0.25f ? stuck + Dt : 0f;
                        if (go.transform.position.y < WalkY - 0.3f) { why = $"fell to {go.transform.position} on leg {w}"; return false; }
                        if (stuck > 1f) { why = $"stuck at {go.transform.position} on leg {w} toward {line[w]}"; return false; }
                        if (t > 30f) { why = "timeout"; return false; }
                    }
                }
                why = "";
                return true;
            }
            finally
            {
                Object.DestroyImmediate(go);
                Physics.SyncTransforms();
            }
        }

        private static (float eye, float reach) DiverEyeAndReach()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPath);
            var cam = prefab.GetComponentInChildren<Camera>(true);
            var reach = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("pickupRange").floatValue;
            return (prefab.transform.InverseTransformPoint(cam.transform.position).y, reach);
        }

        private static bool Fits(Vector3 feet) =>
            !Physics.CheckCapsule(feet + Vector3.up * (0.35f + 0.08f + 0.3f), feet + Vector3.up * (1.8f - 0.35f), 0.35f, ~0, QueryTriggerInteraction.Ignore);

        // Fish and the special-event hull are NetworkObjects that move or appear at run time; they are off here.
        private static void WithScenery(Action<Scene> assertions, bool furnitureProxies)
        {
            ContinuousTownScene.With(scene =>
            {
                var dynamic = All<Collider>(scene).Where(c => c.enabled && c.GetComponentInParent<NetworkObject>() != null).ToArray();
                try
                {
                    foreach (var c in dynamic) c.enabled = false;
                    Physics.SyncTransforms();
                    assertions(scene);
                }
                finally
                {
                    foreach (var c in dynamic) if (c != null) c.enabled = true;
                    Physics.SyncTransforms();
                }
            }, furnitureProxies);
        }

        private static Vector3 P(float x, float z) => new Vector3(x, WalkY, z);
        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
    }
}
