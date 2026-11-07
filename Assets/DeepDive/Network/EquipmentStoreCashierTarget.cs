using UnityEngine;

namespace DeepDive.Network
{
    // The existing physical counter's hit target. Player interaction checks
    // the shop definition/range on the host; no payment or save code lives here.
    public sealed class EquipmentStoreCashierTarget : MonoBehaviour
    {
        public ServicePointAnchor Shop { get; private set; }
        public void Configure(ServicePointAnchor shop) => Shop = shop;
    }
}
