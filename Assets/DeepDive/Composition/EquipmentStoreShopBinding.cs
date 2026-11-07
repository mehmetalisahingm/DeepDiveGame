using System;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.Composition
{
    // Mehmet / #250: connects the prefab's authored displays and counter to the
    // EXISTING host-authoritative selection/purchase seam. No inventory, price,
    // save data, netcode object, extra shop or world placement is created here.
    [DisallowMultipleComponent]
    public sealed class EquipmentStoreShopBinding : MonoBehaviour
    {
        private EquipmentStoreLayout layout;

        public EquipmentStoreLayout Layout => layout;

        public static EquipmentStoreShopBinding Bind(ServicePointAnchor shop, EquipmentStoreLayout store)
        {
            if (shop == null || store == null ||
                shop.Definition.ServiceType != ServicePointType.EquipmentShop ||
                store.Register == null || store.CustomerPoint == null ||
                store.Displays == null) return null;

            var binding = shop.GetComponent<EquipmentStoreShopBinding>();
            if (binding != null && binding.layout != null && binding.layout != store) return null;
            if (binding == null) binding = shop.gameObject.AddComponent<EquipmentStoreShopBinding>();
            binding.layout = store;
            var area = shop.GetComponent<EquipmentStoreAreaAnchor>();
            if (area == null) area = shop.gameObject.AddComponent<EquipmentStoreAreaAnchor>();
            area.Configure(store.transform, store.CustomerPoint);

            foreach (var display in store.Displays)
            {
                if (display.Anchor == null || Array.IndexOf(ShopShelfAnchor.ItemIds, display.EquipmentId) < 0)
                    continue;
                foreach (Transform product in display.Anchor)
                {
                    if (!product.name.StartsWith("Product_", StringComparison.Ordinal)) continue;
                    var anchor = product.GetComponent<ShopShelfAnchor>();
                    if (anchor == null) anchor = product.gameObject.AddComponent<ShopShelfAnchor>();
                    anchor.Configure(shop, display.EquipmentId);
                    // Visuals keep their existing geometry; this trigger is only
                    // an F/raycast target, never a floor/character obstruction.
                    var hit = product.GetComponent<BoxCollider>();
                    if (hit == null) hit = product.gameObject.AddComponent<BoxCollider>();
                    hit.isTrigger = true;
                    hit.center = Vector3.zero;
                    hit.size = new Vector3(.55f, .55f, .55f);
                }
            }

            // Aim at the physical counter (its EXISTING solid collider), not at
            // a duplicate cashier or a new service ID. This works even when the
            // prefab is not a direct child of the ServicePointAnchor.
            var counter = store.transform.Find("Collision/Counter");
            if (counter != null && counter.GetComponent<Collider>() != null)
            {
                var cashier = counter.GetComponent<EquipmentStoreCashierTarget>();
                if (cashier == null) cashier = counter.gameObject.AddComponent<EquipmentStoreCashierTarget>();
                cashier.Configure(shop);
            }
            ShopShelfPresentation.BindStore(shop, store);
            return binding;
        }

        // Only the authored physical store requires that the owner's basket
        // actually holds the product being paid for. Existing legacy shops
        // keep their old direct-catalog purchase behaviour.
        public static bool MatchesCheckoutSelection(bool authoredStore, string selectedId, string requestedId)
            => !authoredStore || (!string.IsNullOrWhiteSpace(requestedId) &&
                string.Equals(selectedId, requestedId, StringComparison.Ordinal));

        public bool ContainsCustomer(Vector3 worldFeet)
        {
            var area = GetComponent<EquipmentStoreAreaAnchor>();
            return area != null && area.ContainsCustomer(worldFeet);
        }

        public bool AtCashier(Vector3 worldFeet, float reach)
        {
            var area = GetComponent<EquipmentStoreAreaAnchor>();
            return area != null && area.AtCashier(worldFeet, reach);
        }
    }
}
