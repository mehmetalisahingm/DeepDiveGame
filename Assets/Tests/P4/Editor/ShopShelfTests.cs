using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public class ShopShelfTests
    {
        private GameObject root;
        private ServicePointAnchor shop;
        [SetUp] public void SetUp()
        {
            root = new GameObject("ShelfFixture");
            root.transform.position = new Vector3(500, 500, 500);
            shop = root.AddComponent<ServicePointAnchor>();
            shop.Configure(TownServiceCatalog.EquipmentShopId, ServicePointType.EquipmentShop, "test", 2.5f, "equipment");
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test] public void ADisplayCannotGrantEquipmentAndRejectsInvalidSelections()
        {
            var sample = root.AddComponent<ShopShelfAnchor>();
            sample.Configure(shop, EconomyManager.CameraBasicId);
            Assert.IsTrue(sample.CanSelect(root.transform.position, true));
            Assert.IsFalse(sample.CanSelect(root.transform.position + Vector3.right * 3f, true), "remote shelf selection");
            Assert.IsFalse(sample.CanSelect(root.transform.position, false), "busy/day-closing/dive selection");
            sample.Configure(shop, "boat-3");
            Assert.IsFalse(sample.CanSelect(root.transform.position, true), "not an equipment display product");
            sample.Configure(shop, EconomyManager.CameraBasicId);
            shop.Configure(TownServiceCatalog.FishBuyerId, ServicePointType.FishBuyer, "test", 2.5f, "fish");
            Assert.IsFalse(sample.CanSelect(root.transform.position, true), "wrong merchant");
        }

        [Test] public void RacksAreIdempotentAndNeverAddSolidWalkObstacles()
        {
            var view = ShopShelfPresentation.Build(shop, root.transform);
            Assert.AreSame(view, ShopShelfPresentation.Build(shop, root.transform));
            var samples = root.GetComponentsInChildren<ShopShelfAnchor>();
            CollectionAssert.AreEquivalent(ShopShelfAnchor.ItemIds, samples.Select(s => s.ItemId).ToArray());
            Assert.AreEqual(12, samples.Length);
            Assert.IsTrue(samples.All(s => s.Shop == shop && s.GetComponent<Collider>().isTrigger));
            Assert.IsFalse(root.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger));
            Assert.IsTrue(samples.All(s => !string.IsNullOrWhiteSpace(ShopShelfPresentation.Label(s.ItemId))));
        }

        [Test] public void ShelfAimStillRespectsAWallAndTheExactProductCollider()
        {
            var view = ShopShelfPresentation.Build(shop, root.transform);
            var sample = root.GetComponentsInChildren<ShopShelfAnchor>().Single(s => s.ItemId == EconomyManager.CameraBasicId);
            var eye = sample.transform.position + Vector3.forward * 1.2f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position = Vector3.Lerp(eye, sample.transform.position, .5f);
                wall.transform.localScale = Vector3.one * .2f;
                Physics.SyncTransforms();
                Assert.IsTrue(EyeRay.Raycast(eye, Vector3.back, 2f, QueryTriggerInteraction.Collide, null, out var hit));
                Assert.AreEqual(wall.GetComponent<Collider>(), hit.collider, "a display cannot be selected through a wall");
                wall.SetActive(false); Physics.SyncTransforms();
                Assert.IsTrue(EyeRay.Raycast(eye, Vector3.back, 2f, QueryTriggerInteraction.Collide, null, out hit));
                Assert.AreEqual(sample, hit.collider.GetComponent<ShopShelfAnchor>());
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
