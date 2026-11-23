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
            EnsureDisplayLabels(store);
            return binding;
        }

        // #249: make the ACTUAL prefab shelves legible in-world, using the existing
        // catalog names instead of inventing prices. Cosmetic objects are created
        // locally when the authored store is bound. No colliders, RPCs, stock,
        // shop identifiers or networked objects are introduced.
        private static void EnsureDisplayLabels(EquipmentStoreLayout store)
        {
            foreach (var display in store.Displays)
            {
                if (display.Anchor == null || display.Items <= 0 || display.Anchor.Find("DisplayLabel") != null)
                    continue;
                var title = ShopShelfPresentation.Label(display.EquipmentId);
                if (string.IsNullOrEmpty(title)) continue;

                var label = new GameObject("DisplayLabel");
                label.transform.SetParent(display.Anchor, false);
                // The centre table faces the entrance; wall shelves inherit their
                // existing shelf rotation and face the shopping aisle.
                var isCameraTable = display.Anchor.parent != null &&
                    display.Anchor.parent.name == "Table_Center";
                label.transform.localPosition = isCameraTable
                    ? new Vector3(0f, 0.16f, -0.38f)
                    : new Vector3(0f, -0.08f, -0.31f);
                label.transform.localRotation = Quaternion.identity;
                var mesh = label.AddComponent<TextMesh>();
                mesh.text = title;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.fontSize = 100;
                mesh.characterSize = isCameraTable ? 0.028f : 0.038f;
                mesh.color = new Color(0.98f, 0.91f, 0.70f);
                label.AddComponent<DepthTestedText>();
            }
        }

        // Authored displays may not yet cover the full existing equipment catalog.
        // Only truly absent catalog items use the original cashier purchase path;
        // represented shelf items still require the host-approved basket selection.
        public bool IsCatalogOnlyItem(string itemId)
        {
            if (layout == null || layout.Displays == null || string.IsNullOrWhiteSpace(itemId) ||
                Array.IndexOf(ShopShelfAnchor.ItemIds, itemId) < 0) return false;
            foreach (var display in layout.Displays)
                if (display.Anchor != null && display.Items > 0 &&
                    string.Equals(display.EquipmentId, itemId, StringComparison.Ordinal))
                    return false;
            return true;
        }

        // The shared purchase RPC also carries motor/research boat orders.
        // Only items from this equipment/repair catalog participate in shelf
        // selection; vessel purchases retain their vendor-specific authority.
        public static bool RequiresEquipmentBasket(string itemId)
            => !string.IsNullOrWhiteSpace(itemId) &&
               Array.IndexOf(ShopShelfAnchor.ItemIds, itemId) >= 0;

        public static bool MatchesCheckoutSelection(bool authoredStore, string selectedId, string requestedId,
            bool catalogOnlyAtCashier = false)
            => !authoredStore || (!string.IsNullOrWhiteSpace(requestedId) &&
                (string.Equals(selectedId, requestedId, StringComparison.Ordinal) || catalogOnlyAtCashier));

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
