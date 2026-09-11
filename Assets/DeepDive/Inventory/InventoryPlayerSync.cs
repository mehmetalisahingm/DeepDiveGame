using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Inventory
{
    // InventoryManager.Bags is host-only server state (docs/plan/CONTRACTS.md "Canta/depo ve
    // ekipman sahipligi | Mert"); nothing replicated it, so a guest client could never see or
    // rely on its own bag. This mirrors one player's own bag onto NetworkVariables (same
    // approach as NetworkPlayer.Oxygen/Health) so the owner sees an accurate, live readout
    // regardless of whether it is host or guest.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class InventoryPlayerSync : NetworkBehaviour
    {
        public readonly NetworkVariable<int> BagWeightGrams = new NetworkVariable<int>();
        public readonly NetworkVariable<int> BagItemCount = new NetworkVariable<int>();
        public readonly NetworkVariable<bool> SafelyReturned = new NetworkVariable<bool>();

        private InventoryManager inventory;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            inventory = FindFirstObjectByType<InventoryManager>();
            if (inventory == null) return;
            inventory.OnBagChanged += HandleBagChanged;
            Refresh();
        }

        public override void OnNetworkDespawn()
        {
            if (inventory != null) inventory.OnBagChanged -= HandleBagChanged;
        }

        private void HandleBagChanged(PlayerId player)
        {
            if (player.Value != OwnerClientId) return;
            Refresh();
        }

        private void Refresh()
        {
            var snapshot = inventory.SnapshotFor(new PlayerId(OwnerClientId));
            BagWeightGrams.Value = snapshot.WeightGrams;
            BagItemCount.Value = snapshot.ItemCount;
            SafelyReturned.Value = snapshot.SafelyReturned;
        }

        // Own on-screen readout, separate from NetworkPlayer's O2/health/action boxes so this
        // module does not need to edit that file to show bag state.
        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            var ratio = Mathf.Clamp01(BagWeightGrams.Value / (float)InventoryManager.CapacityGrams);
            GUI.Box(new Rect(20, 110, 230, 24),
                $"BAG {BagWeightGrams.Value}/{InventoryManager.CapacityGrams}g x{BagItemCount.Value} ({ratio * 100f:0}%)");
            if (SafelyReturned.Value)
                GUI.Box(new Rect(20, 138, 230, 24), "SAFE - CATCH SECURED FOR RETURN");
        }
    }
}
