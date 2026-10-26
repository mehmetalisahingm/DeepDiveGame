using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #171 P4.7-C: pins what the imported Medieval Village MegaKit subset promises the scene builders: 1 unit = 1 m, the 2 m wall module,
    // a human-passable door, URP materials only (no pink/missing/Standard), and sample prefabs that keep the shop's service-point convention.
    public sealed class MegaKitKitTests
    {
        private const string Root = "Assets/DeepDive/Art/MegaKit";
        private static string[] ModelPaths => Directory.GetFiles(Root + "/Models", "*.fbx").Select(p => p.Replace('\\', '/')).ToArray();

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static Bounds ModelBounds(string name)
        {
            var instance = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx"));
            try { return BoundsOf(instance); } finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void EveryModelLoadsWithRenderersAndOnlyKitMaterials()
        {
            Assert.AreEqual(176, ModelPaths.Length, "free Standard tier has 176 models");
            foreach (var path in ModelPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(asset, path);
                var renderers = asset.GetComponentsInChildren<Renderer>();
                Assert.IsNotEmpty(renderers, path);
                foreach (var material in renderers.SelectMany(r => r.sharedMaterials))
                {
                    Assert.IsNotNull(material, path + " has an empty material slot");
                    StringAssert.StartsWith(Root + "/Materials/", AssetDatabase.GetAssetPath(material), path + " still uses an embedded/imported material");
                }
            }
        }

        [Test]
        public void MaterialsAreUrpAndTexturedOnesHaveTheirMaps()
        {
            var materials = AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.AreEqual(12, materials.Length);
            foreach (var material in materials)
            {
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, material.name);
                var flat = material.name == "MI_MetalOrnaments" || material.name == "MI_WindowGlass" || material.name == "Shop_Accent_Equipment";
                Assert.AreEqual(!flat, material.GetTexture("_BaseMap") != null, material.name + " base map");
            }
        }

        [Test]
        public void WallModuleIsTwoMetresAndDoorsFitAPlayer()
        {
            var wall = ModelBounds("Wall_Plaster_Straight");
            Assert.AreEqual(2.0f, wall.size.x, 0.05f);
            Assert.AreEqual(3.12f, wall.size.y, 0.05f);
            Assert.AreEqual(0f, wall.min.y, 0.05f, "wall base sits on y = 0");
            foreach (var door in new[] { "Door_1_Flat", "Door_1_Round" })
            {
                var size = ModelBounds(door).size;
                Assert.GreaterOrEqual(size.x, 1.1f, door + " width");   // #171 passage rule: >= 1.1 m wide, >= 2.1 m tall
                Assert.GreaterOrEqual(size.y, 2.09f, door + " height");
            }
        }

        [Test]
        public void HomeFacadeSampleIsStandingHouseSizedForPeople()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/HomeFacade_Sample.prefab");
            Assert.IsNotNull(prefab);
            var instance = (GameObject)Object.Instantiate(prefab);
            try
            {
                var b = BoundsOf(instance);
                Assert.Greater(b.size.x, 6f); Assert.Less(b.size.x, 9f);     // 6 m body + roof overhang
                Assert.Greater(b.size.y, 6f); Assert.Less(b.size.y, 9f);     // 3.12 m walls + steep tiled roof
                Assert.Greater(b.size.z, 5f); Assert.Less(b.size.z, 8f);
                Assert.AreEqual(0f, b.min.y, 0.3f, "stands on the ground (the stair foot sinks 0.2 m)");
                var door = instance.GetComponentsInChildren<Transform>().First(t => t.name == "Door_1_Round");
                Assert.AreEqual(0f, BoundsOf(door.gameObject).center.x, 0.1f, "door leaf is centred in the front opening");
                Assert.IsEmpty(instance.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Where(m => m == null));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void ShopFrontSampleKeepsTheServicePointConvention()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/ShopFront_Sample.prefab");
            Assert.IsNotNull(prefab);
            var instance = (GameObject)Object.Instantiate(prefab);
            try
            {
                var point = instance.transform.Find("ServicePoint");
                Assert.IsNotNull(point, "customer point child");
                Assert.AreEqual(-1.7f, point.localPosition.z, 0.001f);   // 1.7 m in front of the counter (TownStorefront placeholder convention)
                var counter = instance.transform.Find("Counter");
                Assert.IsNotNull(counter);
                Assert.AreEqual(1.0f, counter.localScale.y, 0.001f);     // 1.0 m counter height
                Assert.IsNotNull(counter.GetComponent<Collider>(), "counter blocks walking through");
                Assert.IsNull(instance.transform.Find("SignBoard").GetComponent<Collider>(), "decor carries no collider");
                Assert.IsNotNull(instance.transform.Find("SignAccent"));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void LicenseAndCreditsAreRecorded()
        {
            var license = File.ReadAllText(Root + "/LICENSE-Quaternius-MedievalVillage.txt");
            StringAssert.Contains("CC0 1.0", license);
            StringAssert.Contains("Medieval Village MegaKit", File.ReadAllText("docs/assets/CREDITS.md"));
        }
    }
}
