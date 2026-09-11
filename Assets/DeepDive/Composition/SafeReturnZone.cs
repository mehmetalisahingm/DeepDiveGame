using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.Composition
{
    // Marks any active diver standing in the exit zone as safely returned while a dive is
    // active (docs/plan/CONTRACTS.md "Guvenli donus | Mert"). Separate from SessionPortal's
    // E-press phase advance: presence alone is enough here, so a guest client's own player
    // counts too, not only the host's. Host-only, since InventoryManager is host-authoritative.
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SafeReturnZone : MonoBehaviour
    {
        // Pure and testable without a live NGO session, mirroring ActionFeedbackRules.Resolve.
        public static bool Contains(BoxCollider box, Vector3 worldPoint)
        {
            var point = box.transform.InverseTransformPoint(worldPoint) - box.center;
            var half = box.size * 0.5f;
            return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.y) <= half.y && Mathf.Abs(point.z) <= half.z;
        }

        private void Update()
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session == null || !session.IsAuthority || session.Session.State.Phase != SessionPhase.Dive) return;
            var inventory = session.GetComponent<InventoryManager>();
            if (inventory == null) return;

            var box = GetComponent<BoxCollider>();
            foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if (!player.IsSpawned || player.Passive.Value) continue;
                if (Contains(box, player.transform.position + Vector3.up * 0.9f))
                    inventory.TryMarkSafeReturn(new PlayerId(player.OwnerClientId));
            }
        }
    }
}
