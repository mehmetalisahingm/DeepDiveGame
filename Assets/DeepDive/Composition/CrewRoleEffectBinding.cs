using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.5-A (#130): effect consumer for Mert's role-selection/save authority. This component
    // owns no role UI or persistence. A validated role is applied to exactly one connected
    // player, then mirrored into Inventory's capacity authority from the same role value.
    [DisallowMultipleComponent]
    public sealed class CrewRoleEffectBinding : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private InventoryManager inventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<CrewRoleEffectBinding>() == null)
                session.gameObject.AddComponent<CrewRoleEffectBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            inventory = GetComponent<InventoryManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        // Mert calls this after validating a free town role change or restoring saved state.
        // No client-supplied player object/position/stat is accepted here.
        public bool TryApplyRoleServer(PlayerId player, CrewRole role)
        {
            if (adapter == null || manager == null || !adapter.IsAuthority ||
                adapter.Session.State.Phase == SessionPhase.Dive || !CrewRoleEffectRules.IsValid(role))
                return false;

            if (!manager.ConnectedClients.TryGetValue(player.Value, out var client) || client.PlayerObject == null)
                return false;
            var diver = client.PlayerObject.GetComponent<NetworkPlayer>();
            if (diver == null || !diver.IsSpawned || !diver.IsServer) return false;

            var changed = diver.ApplyCrewRoleServer(role);
            var inventoryChanged = inventory != null && inventory.SetCrewRole(player, diver.CurrentCrewRole);
            return changed || inventoryChanged;
        }

        private void Update()
        {
            if (adapter == null || manager == null || !adapter.IsAuthority || inventory == null) return;

            // Reconciliation is replacement-only. If Mert restores a role directly onto a player
            // before this binding ticks, Inventory catches up without adding another capacity bonus.
            foreach (var pair in manager.ConnectedClients)
            {
                if (pair.Value.PlayerObject == null) continue;
                var diver = pair.Value.PlayerObject.GetComponent<NetworkPlayer>();
                if (diver == null || !diver.IsSpawned || !diver.IsServer) continue;
                inventory.SetCrewRole(new PlayerId(pair.Key), diver.CurrentCrewRole);
            }
        }
    }
}
