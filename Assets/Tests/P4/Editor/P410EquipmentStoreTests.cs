using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.P1.Lab;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #249: the equipment store prefab alone (isolated, not in any scene). Measured against the numbers Utku's 4 Kasım door/corridor audit
    // recommended (docs/reports/P172-CITY-CANDIDATE-20261104.md): door opening >= 1.8 m (comfortable 2.6 m), shelf/till corridors >= 1.8 m,
    // threshold <= 0.30 m. Collision is the prefab's own simple boxes; street and ground are Utku's.
    public sealed class P410EquipmentStoreTests
    {
        private const string PrefabResource = "CityKit/EquipmentStore";
        private static readonly string[] SoldIds =
            { "camera-basic", "camera-advanced", "camera-professional", "tube-1", "tube-2", "fins-1", "bag-1", "harpoon-1" };

        private GameObject store;

        [SetUp]
        public void SetUp()
        {
            var prefab = Resources.Load<GameObject>(PrefabResource);
            Assert.NotNull(prefab, "Resources/" + PrefabResource + " (build it with DeepDive > P4.10 > Build equipment store prefab)");
            store = (GameObject)Object.Instantiate(prefab);
            store.transform.position = Vector3.zero;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown() { if (store != null) Object.DestroyImmediate(store); }

        private EquipmentStoreLayout Layout() => store.GetComponent<EquipmentStoreLayout>();

        private static bool Free(Vector3 footOrigin, float radius, float height)
        {
            var bottom = footOrigin + Vector3.up * (radius + 0.02f);
            var top = footOrigin + Vector3.up * (height - radius);
            return !Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        }

        [Test]
        public void ItIsAnIsolatedPrefabWithNoNetworkObjectAndOnlyTheLayoutScript()
        {
            Assert.IsNull(store.GetComponentInChildren<NetworkObject>(true));
            var scripts = store.GetComponentsInChildren<MonoBehaviour>(true);
            // (plus the label that keeps the sign from showing through the walls: it only swaps the TextMesh's material)
            CollectionAssert.AreEqual(new[] { typeof(EquipmentStoreLayout) }, scripts.Select(s => s.GetType()).Where(t => t != typeof(DepthTestedText)).ToArray(), "data holder only");
            Assert.AreEqual(1, scripts.Count(s => s is DepthTestedText), "the one sign");
            foreach (var collider in store.GetComponentsInChildren<Collider>(true))
                Assert.AreEqual("Collision", collider.transform.parent.name, collider.name + ": only the simple collision boxes collide, visuals do not");
        }

        [Test]
        public void EveryAnchorIsSetAndEverySoldEquipmentIdHasAVisibleDisplay()
        {
            var layout = Layout();
            Assert.NotNull(layout);
            foreach (var anchor in new[] { layout.Entrance, layout.Threshold, layout.CustomerPoint, layout.Register, layout.ShopkeeperPoint, layout.ExitPoint })
                Assert.NotNull(anchor);
            var shown = layout.Displays.Where(d => d.Anchor != null && d.Items > 0 && d.Anchor.GetComponentsInChildren<Renderer>().Length >= d.Items).Select(d => d.EquipmentId).Distinct().ToArray();
            CollectionAssert.AreEquivalent(SoldIds, shown, "one visible display per catalog equipment id (3 cameras, 2 tubes, fins, bag, harpoon)");
            foreach (var display in layout.Displays)
                Assert.AreEqual(display.Items, display.Anchor.Cast<Transform>().Count(t => t.name.StartsWith("Product_")), display.EquipmentId + ": the item count matches the visible copies");
            Assert.GreaterOrEqual(layout.Displays.Sum(d => d.Items), 40, "full shelves, not a few props");
        }

        [Test]
        public void TheDoorOpeningIsAtLeastTheComfortableWidthAndFourPlayersFitAbreast()
        {
            var layout = Layout();
            Assert.GreaterOrEqual(layout.OpeningWidth, 2.6f, "Utku's comfortable value for four players (minimum 1.8 m)");
            Assert.GreaterOrEqual(layout.OpeningHeight, 2.4f);
            var z = layout.Threshold.position.z;
            // Four real-size capsules (radius 0.3, height 1.8) side by side through the wall plane: no collider in the way.
            foreach (var x in new[] { -0.9f, -0.3f, 0.3f, 0.9f }) Assert.IsTrue(Free(new Vector3(x, 0f, z), 0.3f, 1.8f), "x=" + x);
            // And the opening is closed beyond its width: a capsule just outside it collides with the wall.
            Assert.IsFalse(Free(new Vector3(layout.OpeningWidth * 0.5f + 0.3f, 0f, z), 0.3f, 1.8f), "the wall stands right next to the opening");
        }

        [Test]
        public void TheThresholdIsFlushWithTheStreetAndStepFree()
        {
            var layout = Layout();
            var z = layout.Threshold.position.z;
            Assert.IsTrue(Physics.Raycast(new Vector3(0f, 1f, z), Vector3.down, out var doorway, 3f, ~0, QueryTriggerInteraction.Ignore));
            Assert.LessOrEqual(Mathf.Abs(doorway.point.y), 0.01f, "floor top at the doorway is the street level (0.00 m), no step (the controller climbs 0.30 m)");
            Assert.IsTrue(Physics.Raycast(new Vector3(0f, 1f, 1.5f), Vector3.down, out var inside, 3f, ~0, QueryTriggerInteraction.Ignore));
            Assert.LessOrEqual(Mathf.Abs(inside.point.y - doorway.point.y), 0.01f, "the floor is one level from the door to the till");
        }

        [Test]
        public void ShelfAndTillCorridorsAreAtLeastOnePointEightMetresWide()
        {
            var layout = Layout();
            // A free circle of 1.8 m diameter touching the front face of every display and of the counter: the corridor in front is >= 1.8 m.
            const float backFront = 8f - 0.09f - 0.5f;   // front face of the back shelves (z)
            var fronts = new List<(string name, Vector3 point)>
            {
                ("counter", new Vector3(0f, 0f, 6.0f - 1.05f)),
                ("back shelf A", new Vector3(-3.4f, 0f, backFront - 0.95f)),
                ("back shelf B", new Vector3(3.4f, 0f, backFront - 0.95f)),
                ("left shelf 1", new Vector3(-4.67f + 0.25f + 0.95f, 0f, 2.0f)),
                ("left shelf 2", new Vector3(-4.67f + 0.25f + 0.95f, 0f, 4.5f)),
                ("right shelf 1", new Vector3(4.67f - 0.25f - 0.95f, 0f, 2.0f)),
                ("right shelf 2", new Vector3(4.67f - 0.25f - 0.95f, 0f, 4.5f)),
                ("table (door side)", new Vector3(0f, 0f, 3.0f - 0.5f - 0.95f)),
                ("table (counter side)", new Vector3(0f, 0f, 3.0f + 0.5f + 0.95f)),
            };
            // A sphere of radius 0.9 m at chest height (clear of the floor) stands in for the 1.8 m wide free circle.
            foreach (var (name, point) in fronts)
                Assert.IsFalse(Physics.CheckSphere(new Vector3(point.x, 1.0f, point.z), 0.9f, ~0, QueryTriggerInteraction.Ignore), name + ": a 1.8 m wide free circle in front");
            // The customer point itself is free for a standing player, and the shopkeeper stands 1.7 m behind it (the town's clerk-to-customer distance).
            Assert.IsTrue(Free(new Vector3(layout.CustomerPoint.position.x, 0f, layout.CustomerPoint.position.z), 0.3f, 1.8f));
            Assert.AreEqual(1.7f, Vector3.Distance(layout.CustomerPoint.position, layout.ShopkeeperPoint.position), 0.01f, "customer = clerk + 1.7 m, like every stall");
            // A genuinely blocked spot is detected (the check is not vacuous): inside the counter.
            Assert.IsFalse(Free(new Vector3(0f, 0f, 6.4f), 0.3f, 1.8f));
        }

        // A real CharacterController (the game's player size: radius 0.3, height 1.8, step 0.3) walks in from the street, past the table to the
        // customer point at the till, then to the back shelf; the walk is by waypoints and never leaves the floor or enters a collider.
        [Test]
        public void ARealCharacterControllerWalksFromTheStreetToTheTillAndTheShelves()
        {
            var layout = Layout();
            var street = GameObject.CreatePrimitive(PrimitiveType.Cube);   // the street belongs to the world (Utku); here just a flat slab
            street.name = "TestStreet";
            street.transform.position = new Vector3(0f, -0.1f, -8f);
            street.transform.localScale = new Vector3(30f, 0.2f, 15.4f);
            var player = new GameObject("walker");
            try
            {
                var controller = player.AddComponent<CharacterController>();
                controller.radius = 0.3f; controller.height = 1.8f; controller.center = new Vector3(0f, 0.9f, 0f); controller.stepOffset = 0.3f; controller.slopeLimit = 45f;
                player.transform.position = new Vector3(0.4f, 0.02f, -3f);
                Physics.SyncTransforms();

                bool WalkTo(Vector3 target)
                {
                    for (var i = 0; i < 600; i++)
                    {
                        var flat = new Vector3(target.x - player.transform.position.x, 0f, target.z - player.transform.position.z);
                        if (flat.magnitude < 0.15f) return true;
                        controller.Move(flat.normalized * Mathf.Min(0.1f, flat.magnitude) + Vector3.down * 0.02f);
                    }
                    return false;
                }

                var z = layout.Threshold.position.z;
                Assert.IsTrue(WalkTo(new Vector3(0f, 0f, -0.9f)), "to the door; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(0f, 0f, z)), "through the door; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(0f, 0f, 1.5f)), "inside; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(1.9f, 0f, 2.0f)), "round the table; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(1.9f, 0f, 4.6f)), "past the table; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(layout.CustomerPoint.position.x, 0f, layout.CustomerPoint.position.z)), "to the till; at " + player.transform.position);
                Assert.LessOrEqual(Vector3.Distance(player.transform.position, new Vector3(layout.CustomerPoint.position.x, player.transform.position.y, layout.CustomerPoint.position.z)), 0.3f);
                Assert.IsTrue(WalkTo(new Vector3(-2.2f, 0f, 5.2f)), "away from the counter; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(-3.4f, 0f, 6.3f)), "to the back shelves; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(-2.2f, 0f, 5.2f)), "back; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(-3.3f, 0f, 3.2f)), "to the left shelves; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(0f, 0f, 1.5f)), "to the door side; at " + player.transform.position);
                Assert.IsTrue(WalkTo(new Vector3(0f, 0f, -0.9f)), "and back out to the street; at " + player.transform.position);
                Assert.LessOrEqual(Mathf.Abs(player.transform.position.y), 0.12f, "the whole walk stayed on the floor level");
            }
            finally { Object.DestroyImmediate(player); Object.DestroyImmediate(street); }
        }

        [Test]
        public void TheMeshAndRendererBudgetIsReasonable()
        {
            var renderers = store.GetComponentsInChildren<Renderer>(true);
            var triangles = store.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3)
                            + store.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(r => r.sharedMesh != null ? r.sharedMesh.triangles.Length / 3 : 0);
            var lights = store.GetComponentsInChildren<Light>(true);
            TestContext.Out.WriteLine($"P410_STORE_BUDGET renderers={renderers.Length} triangles={triangles} lights={lights.Length}");
            Assert.Less(renderers.Length, 520);
            Assert.Less(triangles, 200000);
            Assert.LessOrEqual(lights.Length, 4, "four soft point lights without shadows");
            Assert.IsTrue(lights.All(l => l.shadows == LightShadows.None));
        }
    }
}
