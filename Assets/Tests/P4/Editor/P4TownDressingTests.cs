using System;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #171/#162: the town's baked dressing (Resources/CityKit/TownDressing). A pure look: no collider or network part, nothing that reaches the
    // floor stands within reach of a measured route (docs/reports/evidence/p172-route/current-routes.json, the same file the route baseline uses),
    // inside the store, the house plot or off the walk slabs, the fence only recolours the edge blocks, and the lamps switch with the night.
    public sealed class P4TownDressingTests
    {
        private const string EvidencePath = "docs/reports/evidence/p172-route/current-routes.json";
        private const float G = ContinuousTownLayout.WalkSurfaceY;

        [Serializable] private class Pt { public float x, y, z; }
        [Serializable] private class RouteRow { public string id; public Pt[] waypoints; }
        [Serializable] private class BoundRow { public string name, kind; public Pt min, max; }
        [Serializable] private class AnchorRow { public string id; public Pt position; }
        [Serializable] private class Evidence { public RouteRow[] routes; public BoundRow[] bounds; public AnchorRow[] anchors; }

        private static Evidence Load() => JsonUtility.FromJson<Evidence>(File.ReadAllText(EvidencePath));

        private static GameObject Prefab()
        {
            var prefab = Resources.Load<GameObject>(TownDressing.Resource);
            Assert.IsNotNull(prefab, "Resources/" + TownDressing.Resource + " (run DeepDive > P4.10 > Build town dressing)");
            return prefab;
        }

        private static float Distance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // Distance from a route polyline to an axis-aligned rectangle (0 when they touch or cross).
        private static float RectDistance(Rect rect, Vector2 a, Vector2 b)
        {
            const int steps = 40;
            var best = float.MaxValue;
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                var dx = Mathf.Max(rect.xMin - p.x, 0f, p.x - rect.xMax);
                var dy = Mathf.Max(rect.yMin - p.y, 0f, p.y - rect.yMax);
                best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
            }
            return best;
        }

        [Test]
        public void IsOnlyLookNoColliderBodyOrNetworkPart()
        {
            var dressing = Object.Instantiate(Prefab());
            try
            {
                Assert.IsEmpty(dressing.GetComponentsInChildren<Collider>(true), "colliders");
                Assert.IsEmpty(dressing.GetComponentsInChildren<Rigidbody>(true), "bodies");
                Assert.IsEmpty(dressing.GetComponentsInChildren<NetworkObject>(true), "network objects");
                Assert.IsEmpty(dressing.GetComponentsInChildren<ServicePointAnchor>(true), "service points");
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void IsDressedButWithinABudget()
        {
            var dressing = Object.Instantiate(Prefab());
            try
            {
                var renderers = dressing.GetComponentsInChildren<Renderer>(true).Length;
                Assert.GreaterOrEqual(renderers, 300, "fences, paving, lamps, bunting, palms, crates, benches, planters");
                Assert.LessOrEqual(renderers, 1800, "renderer budget");
                Assert.LessOrEqual(dressing.GetComponentsInChildren<Light>(true).Length, 14, "light budget");
                Assert.GreaterOrEqual(dressing.GetComponentsInChildren<Light>(true).Length, 6, "a street's worth of lamps");
                Assert.GreaterOrEqual(dressing.transform.Find("Paving").childCount, 40, "paved main street");
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void NothingThatReachesTheFloorStandsNearARoute()
        {
            var evidence = Load();
            var dressing = Object.Instantiate(Prefab());
            try
            {
                var props = dressing.transform.Find("Props");
                Assert.Greater(props.childCount, 20, "props");
                foreach (Transform prop in props)
                {
                    var renderers = prop.GetComponentsInChildren<Renderer>().Where(r => r.bounds.min.y < G + 1.9f && r.bounds.max.y > G + 0.1f).ToArray();
                    if (renderers.Length == 0) continue;     // bunting: above head height
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    var rect = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                    foreach (var route in evidence.routes)
                        for (var i = 0; i + 1 < route.waypoints.Length; i++)
                        {
                            var d = RectDistance(rect, new Vector2(route.waypoints[i].x, route.waypoints[i].z), new Vector2(route.waypoints[i + 1].x, route.waypoints[i + 1].z));
                            Assert.GreaterOrEqual(d, 0.6f, $"{prop.name} (x {bounds.min.x:0.0}..{bounds.max.x:0.0}, z {bounds.min.z:0.0}..{bounds.max.z:0.0}) is {d:0.00} m from route {route.id}");
                        }
                }
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void PropsStandOnWalkSlabsAndNotInTheStoreOrTheHousePlot()
        {
            var evidence = Load();
            var dressing = Object.Instantiate(Prefab());
            try
            {
                foreach (Transform prop in dressing.transform.Find("Props"))
                {
                    var p = new Vector2(prop.position.x, prop.position.z);
                    if (prop.GetComponentsInChildren<Renderer>().All(r => r.bounds.min.y > G + 1.9f)) continue;   // bunting
                    Assert.IsTrue(evidence.bounds.Any(b => b.kind == "walk" && b.name != "Beach_Wade" && b.name != "Shore_Ramp" && p.x > b.min.x && p.x < b.max.x && p.y > b.min.z && p.y < b.max.z),
                        $"{prop.name} at ({p.x:0.0}, {p.y:0.0}) stands on a walk slab");
                    Assert.IsFalse(p.x > -15.5f && p.x < -4.5f && p.y > -30.4f && p.y < -21.2f, $"{prop.name} is inside the equipment store");
                    Assert.IsFalse(p.x > -7.9f && p.x < 7.9f && p.y > -55.9f && p.y < -41.5f, $"{prop.name} is inside the house plot");
                }
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void NothingIsInsideTheStoreOrTheHouse()
        {
            var dressing = Object.Instantiate(Prefab());
            try
            {
                var store = new Bounds(new Vector3(-10f, G + 1.5f, -25.8f), new Vector3(10.1f, 3f, 8.5f));   // inside the store's walls (x -15..-5, z -30..-21.6)
                var house = new Bounds(new Vector3(0f, G + 1.5f, -48f), new Vector3(11.4f, 3f, 11.4f));       // inside the house's walls
                foreach (var r in dressing.GetComponentsInChildren<Renderer>())
                {
                    Assert.IsFalse(r.bounds.Intersects(store), r.transform.parent.name + "/" + r.name + " stands inside the store");
                    Assert.IsFalse(r.bounds.Intersects(house), r.transform.parent.name + "/" + r.name + " stands inside the house");
                }
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void LampsAndLanternsSwitchWithTheNight()
        {
            var dressing = Object.Instantiate(Prefab());
            try
            {
                var l = dressing.GetComponent<DressingLighting>();
                Assert.IsNotNull(l);
                Assert.GreaterOrEqual(l.Lamps.Length, 6);
                Assert.AreEqual(l.Lamps.Length, l.Bulbs.Length, "one lantern per lamp");
                l.Apply(0f);
                Assert.IsTrue(l.Lamps.All(x => !x.enabled), "off by day");
                l.Apply(1f);
                Assert.IsTrue(l.Lamps.All(x => x.enabled && x.intensity > 1f), "on at night");
                Assert.IsTrue(l.Bulbs.All(b => b.sharedMaterial == l.BulbOn));
            }
            finally { Object.DestroyImmediate(dressing); }
        }

        [Test]
        public void TheTownPlacesItOnceAndOnlyRecoloursTheEdgeBlocks()
        {
            ContinuousTownScene.With(scene =>
            {
                var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName).transform;
                Assert.AreEqual(1, root.Cast<Transform>().Count(t => t.name == TownDressing.Name), "one dressing under the town root");
                Assert.AreSame(root.Find(TownDressing.Name).gameObject, TownDressing.Place(root), "idempotent");
                Assert.IsEmpty(root.Find(TownDressing.Name).GetComponentsInChildren<Collider>(true), "no collider");
                var planks = Resources.Load<Material>(TownDressing.FenceMaterial);
                Assert.IsNotNull(planks);
                var edges = root.Cast<Transform>().Where(t => t.name.StartsWith("Edge_")).ToArray();
                Assert.AreEqual(ContinuousTownLayout.Edges.Count, edges.Length, "every edge block is still there");
                foreach (var edge in edges)
                {
                    Assert.AreSame(planks, edge.GetComponent<Renderer>().sharedMaterial, edge.name + " is planks now");
                    Assert.IsNotNull(edge.GetComponent<Collider>(), edge.name + " keeps its collider");
                }
                // same sizes as the data says (nothing was resized to fit the fence)
                foreach (var block in ContinuousTownLayout.Edges)
                    Assert.AreEqual(block.Size.y, root.Find(block.Name).localScale.y, 1e-4f, block.Name);
            });
        }
    }
}
