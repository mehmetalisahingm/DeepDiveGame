using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class SuppliedKitTests
    {
        [TestCase("Rowboat")]
        [TestCase("CoastalFish")]
        [TestCase("Tuna")]
        [TestCase("RedSnapper")]
        [TestCase("ShopBuilding")]
        [TestCase("TownBuilding")]
        [TestCase("Bed")]
        [TestCase("Storage")]
        [TestCase("Bookshelf")]
        [TestCase("Couch")]
        [TestCase("Plant")]
        [TestCase("DeskLight")]
        public void ShippedArtHasVisibleUrpMaterialsAndNoGameplayColliders(string key)
        {
            var prefab = Resources.Load<GameObject>("SuppliedKit/" + key);
            Assert.NotNull(prefab, key);
            Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length, key);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0, key);
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.NotNull(material, key);
                    Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, key);
                }
        }

        [TestCase("CoastalFish")]
        [TestCase("Tuna")]
        [TestCase("RedSnapper")]
        public void FishIncludesItsPlayableLoopAnimation(string key)
        {
            var animation = Resources.Load<GameObject>("SuppliedKit/" + key).GetComponentInChildren<Animation>(true);
            Assert.NotNull(animation, key);
            Assert.NotNull(animation.clip, key);
            Assert.Greater(animation.clip.length, 0f, key);
            Assert.AreEqual(WrapMode.Loop, animation.wrapMode, key);
        }
    }
}
