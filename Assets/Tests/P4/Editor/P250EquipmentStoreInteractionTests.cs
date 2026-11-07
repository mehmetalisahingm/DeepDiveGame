using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #250 tests the REAL Mert #274 prefab against the existing Mehmet
    // selection/sepet and host-controlled checkout, without world teleport.
    public sealed class P250EquipmentStoreInteractionTests
    {
        private GameObject fixture;
        private GameObject store;
        private ServicePointAnchor shop;
        private EquipmentStoreLayout layout;

        [SetUp]
        public void SetUp()
        {
            fixture = new GameObject("EquipmentShopService");
            fixture.transform.position = new Vector3(350f, 8.4f, -120f);
            shop = fixture.AddComponent<ServicePointAnchor>();
            shop.Configure(TownServiceCatalog.EquipmentShopId, ServicePointType.EquipmentShop,
                "equipment-shop", 2.5f, "equipment");
            var prefab = Resources.Load<GameObject>("CityKit/EquipmentStore");
            Assert.NotNull(prefab);
            store = Object.Instantiate(prefab, fixture.transform);
            store.transform.localPosition = Vector3.zero;
            layout = store.GetComponent<EquipmentStoreLayout>();
            Assert.NotNull(layout);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
        }

        [Test]
        public void RealPrefabDisplaysBecomeNonSolidAuthorizedSelectionTargets()
        {
            var binding = EquipmentStoreShopBinding.Bind(shop, layout);
            Assert.NotNull(binding);
            Assert.AreSame(binding, EquipmentStoreShopBinding.Bind(shop, layout));
            var items = store.GetComponentsInChildren<ShopShelfAnchor>();
            Assert.AreEqual(layout.Displays.Sum(d => d.Items), items.Length);
            CollectionAssert.AreEquivalent(layout.Displays.Select(d => d.EquipmentId).Distinct().ToArray(),
                items.Select(x => x.ItemId).Distinct().ToArray());
            Assert.IsTrue(items.All(x => x.Shop == shop && x.GetComponent<BoxCollider>() != null &&
                x.GetComponent<BoxCollider>().isTrigger));
            Assert.IsEmpty(store.GetComponentsInChildren<NetworkObject>(),
                "shop visuals never create a second gameplay authority");
            Assert.IsNull(store.transform.Find(ShopShelfPresentation.RootName),
                "no second duplicate placeholder rack is built over Mert's displays");
            Assert.NotNull(store.GetComponent<ShopShelfPresentation>(),
                "the original cashier and basket presentation are reused");
        }

        [Test]
        public void RealCounterTargetsTheSameEquipmentServiceAndPreservesHostReach()
        {
            Assert.NotNull(EquipmentStoreShopBinding.Bind(shop, layout));
            var counter = store.transform.Find("Collision/Counter");
            Assert.NotNull(counter);
            var target = counter.GetComponent<EquipmentStoreCashierTarget>();
            Assert.NotNull(target);
            Assert.AreSame(shop, target.Shop);
            Assert.IsFalse(counter.GetComponent<Collider>().isTrigger, "keep the real solid checkout counter");
            var binding = shop.GetComponent<EquipmentStoreShopBinding>();
            var customer = layout.CustomerPoint.position;
            Assert.IsTrue(binding.AtCashier(customer, 3.75f));
            Assert.IsFalse(binding.AtCashier(customer + Vector3.right * 8f, 3.75f),
                "cannot pay from outside the counter");
            Assert.AreEqual(PlayerActionResult.Accepted,
                ServiceInteractionRules.Validate(shop.Definition, customer, counter.position, true));
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                ServiceInteractionRules.Validate(shop.Definition, customer + Vector3.right * 8f, counter.position, true));
        }

        [Test]
        public void AuthoredCheckoutCannotBuyAnUnselectedItemOrBypassTheBasket()
        {
            Assert.IsFalse(EquipmentStoreShopBinding.MatchesCheckoutSelection(true, "", "camera-basic"));
            Assert.IsFalse(EquipmentStoreShopBinding.MatchesCheckoutSelection(true, "camera-basic", "camera-professional"));
            Assert.IsFalse(EquipmentStoreShopBinding.MatchesCheckoutSelection(true, "camera-basic", ""));
            Assert.IsTrue(EquipmentStoreShopBinding.MatchesCheckoutSelection(true, "camera-basic", "camera-basic"));
            Assert.IsTrue(EquipmentStoreShopBinding.MatchesCheckoutSelection(false, "", "camera-basic"),
                "old equipment shop keeps its direct-catalog purchase contract");
        }

        [Test]
        public void MissingAuthoredProductsKeepTheExistingHostValidatedCashierAlternative()
        {
            var binding = EquipmentStoreShopBinding.Bind(shop, layout);
            var catalogOnly = ShopShelfAnchor.ItemIds.Where(binding.IsCatalogOnlyItem).ToArray();
            CollectionAssert.AreEquivalent(
                new[] { "tube-3", BoatRepairParts.Hull, BoatRepairParts.Engine, BoatRepairParts.FuelTank },
                catalogOnly, "the authored eight-product prefab must not hide the other four catalog products");
            foreach (var id in layout.Displays.Select(x => x.EquipmentId))
                Assert.IsFalse(binding.IsCatalogOnlyItem(id), "authored shelf items cannot bypass the basket");
            Assert.IsFalse(binding.IsCatalogOnlyItem("nonexistent-item"), "unknown catalog entries are never eligible");
            Assert.IsTrue(EquipmentStoreShopBinding.MatchesCheckoutSelection(
                true, "", "tube-3", binding.IsCatalogOnlyItem("tube-3")),
                "the absent tank remains purchasable at the real host-validated cashier");
            Assert.IsTrue(EquipmentStoreShopBinding.MatchesCheckoutSelection(
                true, "", BoatRepairParts.Engine, binding.IsCatalogOnlyItem(BoatRepairParts.Engine)),
                "repair parts must remain purchasable as an alternative to finding them");
            Assert.IsFalse(EquipmentStoreShopBinding.MatchesCheckoutSelection(
                true, "", "camera-basic", binding.IsCatalogOnlyItem("camera-basic")),
                "present items must still be physically selected");
            Assert.IsFalse(EquipmentStoreShopBinding.MatchesCheckoutSelection(
                true, "", "nonexistent-item", binding.IsCatalogOnlyItem("nonexistent-item")),
                "unknown items cannot skip host basket verification");
        }

        [Test]
        public void ASelectionCanTravelFromBackShelvesToCashierButNotOutsideStore()
        {
            var binding = EquipmentStoreShopBinding.Bind(shop, layout);
            Assert.IsTrue(binding.ContainsCustomer(layout.CustomerPoint.position));
            var farShelf = layout.Displays.First(x => x.Anchor != null && x.Anchor.position.z > store.transform.position.z + 5f);
            var behindShelf = new Vector3(farShelf.Anchor.position.x, store.transform.position.y, farShelf.Anchor.position.z - 1f);
            Assert.IsTrue(binding.ContainsCustomer(behindShelf));
            Assert.IsFalse(binding.ContainsCustomer(layout.ExitPoint.position - store.transform.forward * 8f));
            Assert.IsFalse(binding.ContainsCustomer(store.transform.position + Vector3.up * 6f));
        }

        [Test]
        public void WallRayStillBlocksASelectionAndHighHarpoonsCanBeSelected()
        {
            EquipmentStoreShopBinding.Bind(shop, layout);
            var harpoon = store.GetComponentsInChildren<ShopShelfAnchor>().First(x => x.ItemId == "harpoon-1");
            var feet = harpoon.transform.position;
            feet.y = store.transform.position.y;
            feet.z -= 1.0f;
            Assert.IsTrue(harpoon.CanSelect(feet, true), "high rack products are in reach from floor");
            Assert.IsFalse(harpoon.CanSelect(feet + Vector3.right * 5f, true));
            Assert.IsFalse(harpoon.CanSelect(feet, false));
            Assert.IsFalse(ShopShelfAnchor.DisplayWithinReach(feet + Vector3.up * 7f, harpoon.transform.position));
        }
    }
}
