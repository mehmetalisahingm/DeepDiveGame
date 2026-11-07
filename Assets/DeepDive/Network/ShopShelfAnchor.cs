using System;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // A display sample, never an inventory item. F selects it; only the existing
    // cashier/economy transaction can transfer money and grant equipment.
    public sealed class ShopShelfAnchor : MonoBehaviour
    {
        public static readonly string[] ItemIds = {
            "tube-1", "tube-2", "tube-3", "camera-basic", "camera-advanced", "camera-professional",
            "fins-1", "bag-1", "harpoon-1", BoatRepairParts.Hull, BoatRepairParts.Engine, BoatRepairParts.FuelTank
        };
        public string ItemId { get; private set; }
        public ServicePointAnchor Shop { get; private set; }
        public void Configure(ServicePointAnchor shop, string itemId) { Shop = shop; ItemId = itemId; }
        public static bool DisplayWithinReach(Vector3 feet, Vector3 display)
        {
            // A high-mounted harpoon is not unreachable merely because the
            // customer's eye/feet are below it. The actual ray must still hit
            // the product without hitting a wall or another display first.
            var dx = feet.x - display.x;
            var dz = feet.z - display.z;
            return dx * dx + dz * dz <= 2.5f * 2.5f && Mathf.Abs(feet.y - display.y) <= 3.2f;
        }

        public bool CanSelect(Vector3 feet, bool available) => available && Shop != null &&
            Shop.Definition.ServiceType == ServicePointType.EquipmentShop &&
            Array.IndexOf(ItemIds, ItemId) >= 0 && DisplayWithinReach(feet, transform.position);
    }
}
