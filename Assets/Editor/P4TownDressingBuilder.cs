using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #171/#162: bakes the town's dressing (Resources/CityKit/TownDressing) in WORLD coordinates from the town layout's own constants: a wooden
    // fence on every edge block, cobble paving on the main street and in front of the stalls and the store, street lamps and bunting, palms, crates,
    // barrels, benches, planters. It is collider-free and standing only where the route evidence says nobody walks: every prop is checked against the
    // measured routes (docs/reports/evidence/p172-route/current-routes.json), the walk slabs, the counters/anchors and the store/house footprints at
    // bake time, and the same rules are asserted again by P4TownDressingTests. A candidate that fails is skipped and logged, never moved silently.
    // DeepDive > P4.10 > Build town dressing, or -executeMethod DeepDive.EditorTools.P4TownDressingBuilder.Build
    public static class P4TownDressingBuilder
    {
        public const string PrefabPath = P410EquipmentStoreBuilder.Folder + "/TownDressing.prefab";
        public const string EvidencePath = "docs/reports/evidence/p172-route/current-routes.json";
        public const float RouteClearance = 1.1f;      // a standing prop keeps this far from every route polyline
        public const float AnchorClearance = 1.2f;     // ... and from every service point / world anchor
        private const string ShoreFolder = "Assets/DeepDive/Art/Resources/ShoreKit/";
        private const float G = ContinuousTownLayout.WalkSurfaceY;

        // ---- evidence (a tiny JsonUtility mirror of the audit file) -----------------------------------------------------
        [Serializable] private class Pt { public float x, y, z; }
        [Serializable] private class RouteRow { public string id; public Pt[] waypoints; }
        [Serializable] private class BoundRow { public string name, kind; public Pt min, max; }
        [Serializable] private class AnchorRow { public string id; public Pt position; }
        [Serializable] private class Evidence { public RouteRow[] routes; public BoundRow[] bounds; public AnchorRow[] anchors; }

        private static Evidence evidence;
        private static readonly List<string> Rejected = new List<string>();
        private static readonly List<Light> Lamps = new List<Light>();
        private static readonly List<Renderer> Bulbs = new List<Renderer>();
        private static int placed;

        [MenuItem("DeepDive/P4.10/Build town dressing")]
        public static void Build()
        {
            evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(EvidencePath));
            Rejected.Clear(); Lamps.Clear(); Bulbs.Clear(); placed = 0;
            Directory.CreateDirectory(P410EquipmentStoreBuilder.Folder + "/Materials");
            AssetDatabase.Refresh();
            var root = new GameObject("TownDressing");
            try
            {
                Fences(Group(root.transform, "Fences"));
                Paving(Group(root.transform, "Paving"));
                var props = Group(root.transform, "Props");
                Lamp(props, "LampEastNear", new Vector2(3.7f, -18.2f), 180f);
                Lamp(props, "LampWestNear", new Vector2(-3.7f, -18.2f), 0f);
                Lamp(props, "LampEastMid", new Vector2(3.9f, -23.8f), 180f);
                Lamp(props, "LampWestMid", new Vector2(-3.9f, -23.8f), 0f);
                Lamp(props, "LampEastStall", new Vector2(7.2f, -27.0f), 180f);
                Lamp(props, "LampWestStall", new Vector2(-5.2f, -33.0f), 0f);
                Lamp(props, "LampEastFar", new Vector2(12.4f, -26.5f), 180f);
                Lamp(props, "LampApproachEast", new Vector2(3.2f, -40.0f), 180f);
                Lamp(props, "LampApproachWest", new Vector2(-3.0f, -39.2f), 0f);
                Bunting(props, new Vector3(-3.7f, G + 3.1f, -18.2f), new Vector3(3.7f, G + 3.1f, -18.2f));
                Bunting(props, new Vector3(-3.9f, G + 3.1f, -23.8f), new Vector3(3.9f, G + 3.1f, -23.8f));
                Bunting(props, new Vector3(-3.0f, G + 3.1f, -39.2f), new Vector3(3.2f, G + 3.1f, -40.0f));

                Palm(props, "shore-palm-1", new Vector2(12.7f, -17.0f), 20f);
                Palm(props, "shore-palm-2", new Vector2(-11.7f, -17.0f), 140f);
                Palm(props, "shore-palm-3", new Vector2(12.8f, -34.2f), 80f);
                Palm(props, "shore-palm-2", new Vector2(-12.5f, -34.2f), 200f);
                Palm(props, "shore-palm-1", new Vector2(13.0f, -24.8f), 60f);

                Cluster(props, "StallCrates", new Vector2(12.6f, -31.8f), 0f);
                Cluster(props, "EastBarrels", new Vector2(12.6f, -19.6f), 90f);
                Cluster(props, "MidCrates", new Vector2(6.8f, -34.4f), 15f);
                Cluster(props, "WestCrates", new Vector2(-6.6f, -34.4f), -20f);
                Shore(props, "shore-prop-anchor", new Vector2(8.4f, -17.6f), 120f, 0.9f);
                Shore(props, "shore-prop-chest", new Vector2(12.7f, -29.3f), -40f, 0.9f);
                Shore(props, "shore-prop-bucket-fishes", new Vector2(7.1f, -22.8f), 30f, 0.9f);
                Shore(props, "shore-prop-barrel", new Vector2(7.2f, -25.2f), 0f, 0.9f);
                Bench(props, "BenchEast", new Vector2(6.4f, -18.4f), 0f);
                Bench(props, "BenchWest", new Vector2(-6.4f, -18.4f), 0f);
                Bench(props, "BenchEastStall", new Vector2(8.0f, -23.2f), 90f);
                Planter(props, "PlanterEast", new Vector2(4.6f, -31.9f));
                Planter(props, "PlanterWest", new Vector2(-4.4f, -31.9f));
                Planter(props, "PlanterApproachEast", new Vector2(3.0f, -37.4f));

                var lighting = root.AddComponent<DressingLighting>();
                lighting.Lamps = Lamps.ToArray(); lighting.Bulbs = Bulbs.ToArray();
                lighting.BulbOn = P4SharedHouseBuilder.Mat("BulbOn", new Color(1f, 0.92f, 0.7f), 0.5f, new Color(2.2f, 1.75f, 1.1f));
                lighting.BulbOff = P4SharedHouseBuilder.Mat("BulbOff", new Color(0.9f, 0.86f, 0.72f), 0.5f);
                lighting.LampIntensity = 2.2f;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"P171_TOWN_DRESSING_BUILT {PrefabPath} placed={placed} rejected={Rejected.Count}");
                foreach (var why in Rejected) Debug.Log("P171_TOWN_DRESSING_REJECTED " + why);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        // ---- the placement rules (also used by the test through the same arithmetic) ----------------------------------------

        public static float DistanceToRoutes(Vector2 p)
        {
            var best = float.MaxValue;
            foreach (var route in evidence.routes)
                for (var i = 0; i + 1 < route.waypoints.Length; i++)
                    best = Mathf.Min(best, DistanceToSegment(p, new Vector2(route.waypoints[i].x, route.waypoints[i].z), new Vector2(route.waypoints[i + 1].x, route.waypoints[i + 1].z)));
            return best;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        private static bool OnWalkSlab(Vector2 p, float margin) =>
            evidence.bounds.Any(b => b.kind == "walk" && b.max.y > G - 0.05f && b.min.y < G && p.x > b.min.x + margin && p.x < b.max.x - margin && p.y > b.min.z + margin && p.y < b.max.z - margin
                                   && b.name != "Beach_Wade" && b.name != "Shore_Ramp");

        // The solid things a prop must not sit in: the store, the house plot's interior, the stall counters, the dock.
        private static bool InSolid(Vector2 p, float margin) =>
            evidence.bounds.Any(b => (b.kind == "solid" || b.kind == "structure" || b.kind.StartsWith("counter") || b.name == "Dock_Town")
                                     && b.max.y > G + 0.2f && p.x > b.min.x - margin && p.x < b.max.x + margin && p.y > b.min.z - margin && p.y < b.max.z + margin)
            || (p.x > -15.5f && p.x < -4.5f && p.y > -30.4f && p.y < -21.2f)       // the store's footprint, walls and all
            || (p.x > -7.9f && p.x < 7.9f && p.y > -55.9f && p.y < -41.5f);       // the house plot (the house has its own dressing)

        public static bool Allowed(Vector2 p, float radius, out string why)
        {
            why = null;
            if (!OnWalkSlab(p, radius)) { why = "not on a walk slab"; return false; }
            var route = DistanceToRoutes(p);
            if (route < RouteClearance + radius) { why = $"{route:0.00} m from a route"; return false; }
            foreach (var a in evidence.anchors)
                if (Vector2.Distance(p, new Vector2(a.position.x, a.position.z)) < AnchorClearance + radius) { why = "near anchor " + a.id; return false; }
            if (InSolid(p, radius)) { why = "inside a solid"; return false; }
            return true;
        }

        private static bool Place(string name, Vector2 p, float radius)
        {
            if (Allowed(p, radius, out var why)) { placed++; return true; }
            Rejected.Add($"{name} at ({p.x:0.0}, {p.y:0.0}): {why}");
            return false;
        }

        // ---- helpers -----------------------------------------------------------------------------------------------

        private static Transform Group(Transform parent, string name) => P410EquipmentStoreBuilder.Empty(parent, name, Vector3.zero);
        private static Material Kit(string name) => P410EquipmentStoreBuilder.Kit(name);
        private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 size, Material m, Vector3? euler = null) =>
            P410EquipmentStoreBuilder.Prim(parent, PrimitiveType.Cube, name, pos, size, m, euler);

        private static GameObject Shore(Transform parent, string id, Vector2 xz, float yaw, float scale)
        {
            if (!Place(id, xz, 0.5f * scale)) return null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ShoreFolder + id + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.position = new Vector3(xz.x, G, xz.y);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        private static void Palm(Transform parent, string id, Vector2 xz, float yaw)
        {
            var palm = Shore(parent, id, xz, yaw, 1.0f);
            if (palm == null) return;
        }

        private static void Cluster(Transform parent, string name, Vector2 xz, float yaw)
        {
            if (!Place(name, xz, 1.0f)) return;
            var group = Group(parent, name);
            group.position = new Vector3(xz.x, G, xz.y);
            group.rotation = Quaternion.Euler(0f, yaw, 0f);
            P410EquipmentStoreBuilder.Part(group, "Prop_Crate", new Vector3(0f, 0f, 0f), 10f, Vector3.one * 0.8f);
            P410EquipmentStoreBuilder.Part(group, "Prop_Crate", new Vector3(0.95f, 0f, 0.2f), -25f, Vector3.one * 0.7f);
            P410EquipmentStoreBuilder.Part(group, "Prop_Crate", new Vector3(0.1f, 0.78f, 0.0f), 40f, Vector3.one * 0.55f);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ShoreFolder + "shore-prop-barrel.prefab");
            var barrel = (GameObject)PrefabUtility.InstantiatePrefab(asset, group);
            barrel.transform.localPosition = new Vector3(-0.9f, 0f, 0.35f);
            barrel.transform.localScale = Vector3.one * 0.85f;
            foreach (var c in barrel.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        }

        private static void Bench(Transform parent, string name, Vector2 xz, float yaw)
        {
            if (!Place(name, xz, 1.0f)) return;
            var bench = Group(parent, name);
            bench.position = new Vector3(xz.x, G, xz.y);
            bench.rotation = Quaternion.Euler(0f, yaw, 0f);
            var plank = P4SharedHouseBuilder.Mat("Plank", new Color(0.55f, 0.38f, 0.22f), 0.2f);
            var leg = Kit("MI_WoodTrim");
            Cube(bench, "Seat", new Vector3(0f, 0.46f, 0f), new Vector3(1.7f, 0.07f, 0.5f), plank);
            Cube(bench, "Back", new Vector3(0f, 0.82f, -0.22f), new Vector3(1.7f, 0.4f, 0.05f), plank);
            foreach (var x in new[] { -0.75f, 0.75f })
            {
                Cube(bench, "Leg", new Vector3(x, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.42f), leg);
                Cube(bench, "BackPost", new Vector3(x, 0.7f, -0.22f), new Vector3(0.08f, 0.5f, 0.08f), leg);
            }
        }

        private static void Planter(Transform parent, string name, Vector2 xz)
        {
            if (!Place(name, xz, 0.7f)) return;
            var planter = Group(parent, name);
            planter.position = new Vector3(xz.x, G, xz.y);
            var wood = Kit("MI_WoodTrim");
            Cube(planter, "Box", new Vector3(0f, 0.28f, 0f), new Vector3(1.3f, 0.56f, 0.55f), wood);
            var leaf = P4SharedHouseBuilder.Mat("Leaf", new Color(0.22f, 0.5f, 0.24f), 0.15f);
            var colours = new[] { new Color(0.92f, 0.3f, 0.35f), new Color(0.98f, 0.82f, 0.3f), new Color(0.96f, 0.96f, 0.92f) };
            for (var i = 0; i < 6; i++)
            {
                var x = -0.5f + i * 0.2f;
                P410EquipmentStoreBuilder.Prim(planter, PrimitiveType.Sphere, "Leaf", new Vector3(x, 0.66f, 0f), new Vector3(0.3f, 0.3f, 0.3f), leaf);
                P410EquipmentStoreBuilder.Prim(planter, PrimitiveType.Sphere, "Bloom", new Vector3(x, 0.84f, 0.04f), new Vector3(0.12f, 0.12f, 0.12f), P4SharedHouseBuilder.Mat("Bloom" + (i % 3), colours[i % 3], 0.3f));
            }
        }

        // A street lamp: pole, a short arm towards the street (`yaw` turns it), a lantern with a point light that the night switches on.
        private static void Lamp(Transform parent, string name, Vector2 xz, float yaw)
        {
            if (!Place(name, xz, 0.35f)) return;
            var lamp = Group(parent, name);
            lamp.position = new Vector3(xz.x, G, xz.y);
            lamp.rotation = Quaternion.Euler(0f, yaw, 0f);
            var iron = Kit("MI_MetalOrnaments");
            var wood = Kit("MI_WoodTrim");
            Cube(lamp, "Base", new Vector3(0f, 0.18f, 0f), new Vector3(0.34f, 0.36f, 0.34f), Kit("MI_RockTrim"));
            P410EquipmentStoreBuilder.Prim(lamp, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.6f, 0f), new Vector3(0.1f, 1.45f, 0.1f), wood);
            Cube(lamp, "Arm", new Vector3(0.3f, 3.0f, 0f), new Vector3(0.62f, 0.07f, 0.07f), iron);
            Cube(lamp, "Brace", new Vector3(0.15f, 2.86f, 0f), new Vector3(0.35f, 0.05f, 0.05f), iron, new Vector3(0f, 0f, 40f));
            Cube(lamp, "Cap", new Vector3(0.58f, 2.82f, 0f), new Vector3(0.3f, 0.06f, 0.3f), iron);
            var bulb = P410EquipmentStoreBuilder.Prim(lamp, PrimitiveType.Sphere, "Bulb", new Vector3(0.58f, 2.62f, 0f), new Vector3(0.28f, 0.34f, 0.28f), P4SharedHouseBuilder.Mat("BulbOff", new Color(0.9f, 0.86f, 0.72f), 0.5f));
            Bulbs.Add(bulb.GetComponent<Renderer>());
            var light = new GameObject("LampLight").AddComponent<Light>();
            light.transform.SetParent(lamp, false);
            light.transform.localPosition = new Vector3(0.58f, 2.5f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.45f);
            light.range = 9f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            Lamps.Add(light);
        }

        // A sagging line of small flags between two lamp arms, above head height, never over anything that is walked on at head height.
        private static void Bunting(Transform parent, Vector3 from, Vector3 to)
        {
            if (!Allowed(new Vector2(from.x, from.z), 0.3f, out _) || !Allowed(new Vector2(to.x, to.z), 0.3f, out _))
            {
                Rejected.Add($"Bunting ({from.x:0.0}, {from.z:0.0}) -> ({to.x:0.0}, {to.z:0.0}): a lamp at an end was rejected");
                return;
            }
            var line = Group(parent, "Bunting");
            var colours = new[] { new Color(0.9f, 0.3f, 0.3f), new Color(0.98f, 0.82f, 0.3f), new Color(0.3f, 0.65f, 0.85f), new Color(0.4f, 0.75f, 0.45f), new Color(0.95f, 0.55f, 0.2f) };
            const int count = 17;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var sag = Mathf.Sin(t * Mathf.PI) * -0.45f;
                var position = Vector3.Lerp(from, to, t) + new Vector3(0f, sag, 0f);
                var along = (to - from).normalized;
                Cube(line, "Flag", position + new Vector3(0f, -0.14f, 0f), new Vector3(0.22f, 0.26f, 0.02f), P4SharedHouseBuilder.Mat("Pennant" + (i % colours.Length), colours[i % colours.Length], 0.05f), new Vector3(0f, Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg, 45f));
            }
        }

        // ---- fences ----------------------------------------------------------------------------------------------------

        private static void Fences(Transform parent)
        {
            var post = P4SharedHouseBuilder.Mat("FencePost", new Color(0.36f, 0.25f, 0.15f), 0.2f);
            var rail = P4SharedHouseBuilder.Mat("FencePlanks", new Color(0.52f, 0.37f, 0.22f), 0.2f);
            foreach (var block in ContinuousTownLayout.Edges)
            {
                var alongX = block.Size.x >= block.Size.z;
                var length = alongX ? block.Size.x : block.Size.z;
                if (length < 0.6f) continue;
                var count = Mathf.Max(2, Mathf.RoundToInt(length / 1.8f) + 1);
                for (var i = 0; i < count; i++)
                {
                    var t = count == 1 ? 0.5f : i / (float)(count - 1);
                    var a = alongX ? Mathf.Lerp(block.Min.x + 0.1f, block.Max.x - 0.1f, t) : Mathf.Lerp(block.Min.z + 0.1f, block.Max.z - 0.1f, t);
                    var pos = alongX ? new Vector3(a, G + 0.78f, block.Center.z) : new Vector3(block.Center.x, G + 0.78f, a);
                    Cube(parent, "Post", pos, new Vector3(0.2f, 1.56f, 0.2f), post);
                    Cube(parent, "PostCap", pos + new Vector3(0f, 0.82f, 0f), new Vector3(0.28f, 0.07f, 0.28f), post);
                }
                var railCentre = new Vector3(block.Center.x, G + 1.32f, block.Center.z);
                Cube(parent, "TopRail", railCentre, alongX ? new Vector3(length, 0.1f, 0.2f) : new Vector3(0.2f, 0.1f, length), rail);
            }
        }

        // ---- paving ----------------------------------------------------------------------------------------------------

        private static void Paving(Transform parent)
        {
            // 2 m cobble modules, top 3 cm above the walk surface: the main street axis and the way home, the apron in front of the store door and a
            // pad in front of each stall's customer mark. Nothing is paved off the walk slabs.
            var cells = new HashSet<Vector2>();
            for (var z = -35f; z <= -17f; z += 2f) foreach (var x in new[] { -2f, 0f, 2f }) cells.Add(new Vector2(x, z));
            for (var z = -41f; z <= -37f; z += 2f) foreach (var x in new[] { -2f, 0f, 2f }) cells.Add(new Vector2(x, z));   // up to the house front (z -42); the floor beyond is the house's own
            foreach (var x in new[] { -11f, -9f }) foreach (var z in new[] { -20f, -18f }) cells.Add(new Vector2(x, z));
            foreach (var shop in ContinuousTownLayout.StorefrontPositions.Skip(1))
                foreach (var dx in new[] { -1f, 1f }) foreach (var dz in new[] { 1f, 3f }) cells.Add(new Vector2(Mathf.Round(shop.x / 2f) * 2f + dx, shop.z + dz));
            var skipped = 0;
            foreach (var cell in cells.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                if (!OnWalkSlab(cell, 0.9f)) { skipped++; continue; }
                if (cell.x - 1f < -4.7f && cell.x + 1f > -15.4f && cell.y + 1f > -30.3f && cell.y - 1f < -21.3f) { skipped++; continue; }   // the store's own floor
                var tile = P410EquipmentStoreBuilder.Part(parent, "Floor_UnevenBrick", new Vector3(cell.x, G, cell.y));
                tile.name = "Cobble";
                var renderers = tile.GetComponentsInChildren<Renderer>();
                var top = renderers.Max(r => r.bounds.max.y);
                tile.transform.position += new Vector3(0f, G + 0.03f - top, 0f);
            }
            Debug.Log($"P171_TOWN_PAVING cells={cells.Count} skipped={skipped}");
        }
    }
}
