using UnityEngine;

namespace DeepDive.Network
{
    // Network-side read-only selection geometry. The composition layer owns
    // the actual prefab binding; the diver never depends on its assembly.
    [DisallowMultipleComponent]
    public sealed class EquipmentStoreAreaAnchor : MonoBehaviour
    {
        private const float HalfWidthWithMargin = 5.75f;
        private const float FrontWithMargin = -2.1f;
        private const float BackWithMargin = 8.9f;

        public Transform StoreRoot { get; private set; }
        public Transform CustomerPoint { get; private set; }

        public void Configure(Transform storeRoot, Transform customerPoint)
        {
            StoreRoot = storeRoot;
            CustomerPoint = customerPoint;
        }

        public bool ContainsCustomer(Vector3 worldFeet)
        {
            if (StoreRoot == null) return false;
            var pos = StoreRoot.InverseTransformPoint(worldFeet);
            return Mathf.Abs(pos.x) <= HalfWidthWithMargin &&
                pos.z >= FrontWithMargin && pos.z <= BackWithMargin &&
                pos.y >= -1.0f && pos.y <= 2.8f;
        }

        public bool AtCashier(Vector3 worldFeet, float reach)
        {
            if (CustomerPoint == null || !ContainsCustomer(worldFeet)) return false;
            return (worldFeet - CustomerPoint.position).sqrMagnitude <= reach * reach;
        }
    }

}
