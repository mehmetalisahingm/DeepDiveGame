using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    [DisallowMultipleComponent]
    public sealed class RecordingWorldBinding : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private InventoryManager inventory;
        private EconomyManager economy;
        private RecordingDiveBinding binding;
        private readonly Dictionary<PlayerId, NetworkRecorderView> views = new Dictionary<PlayerId, NetworkRecorderView>();
        public RecordingDiveBinding Binding => binding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<RecordingWorldBinding>() == null)
                session.gameObject.AddComponent<RecordingWorldBinding>();
        }

        private void OnEnable()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            inventory = GetComponent<InventoryManager>();
            economy = GetComponent<EconomyManager>();
            if (adapter == null || manager == null || inventory == null) return;
            binding = new RecordingDiveBinding(adapter.Session, inventory,
                () => isActiveAndEnabled && adapter.IsAuthority && manager.IsListening);
            BindPayment();
        }

        public void SetPaymentHandler(Func<RecordingResult, PlayerActionResult> handler) =>
            binding?.SetPaymentHandler(handler);

        private void BindPayment()
        {
            if (binding == null) return;
            if (economy == null) economy = GetComponent<EconomyManager>();
            binding.SetPaymentHandler(economy != null ? economy.TryQueueRecordingTurnIn : null);
        }

        private void Update()
        {
            if (binding == null) return;
            BindPayment();
            binding.Refresh();
            if (!binding.IsActive) { views.Clear(); return; }
            var live = new HashSet<PlayerId>();
            foreach (var client in manager.ConnectedClients)
            {
                var player = client.Value.PlayerObject != null ? client.Value.PlayerObject.GetComponent<NetworkPlayer>() : null;
                if (player == null || !player.IsSpawned) continue;
                var id = new PlayerId(client.Key);
                live.Add(id);
                if (!views.TryGetValue(id, out var view) || view.Player != player)
                    views[id] = view = new NetworkRecorderView(player, inventory, () => binding.IsActive && !adapter.Connection.IsSceneLoading);
                binding.BindView(id, view);
            }
            foreach (var id in new List<PlayerId>(views.Keys))
                if (!live.Contains(id)) { binding.RemoveView(id); views.Remove(id); }
        }

        private void OnDisable()
        {
            binding?.Dispose();
            binding = null;
            views.Clear();
        }
    }

    public sealed class NetworkRecorderView : IRecorderView
    {
        public NetworkPlayer Player { get; }
        private readonly InventoryManager inventory;
        private readonly Func<bool> activeDive;
        public NetworkRecorderView(NetworkPlayer player, InventoryManager inventory, Func<bool> activeDive)
        { Player = player; this.inventory = inventory; this.activeDive = activeDive; }
        public bool IsActive => Player != null && Player.IsSpawned && Player.IsServer && !Player.Passive.Value &&
            Player.Swimming.Value && activeDive() &&
            inventory.Bags.TryGetValue(new PlayerId(Player.OwnerClientId), out var bag) && !bag.SafelyReturned;
        public Vector3 EyePosition => Player.RecordingEyePosition;
        public Vector3 EyeForward => Player.RecordingForwardServer;
        public float VerticalFieldOfViewDegrees => Player.RecordingFieldOfView;
    }
}