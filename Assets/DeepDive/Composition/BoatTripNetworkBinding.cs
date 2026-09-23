using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Trip;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Composition
{
    // Runtime seam owner for P3.3. Trip/progression remains in BoatTripManager, physical seats and
    // movement remain in NetworkBoatController, and EconomyManager remains the sole repair source.
    [DisallowMultipleComponent]
    public sealed class BoatTripNetworkBinding : MonoBehaviour, BoatTripManager.ISessionRoster
    {
        [SerializeField] private BoatTripManager tripManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private NetworkBoatController boatController;
        [Tooltip("Utku/#76 component implementing IBoatRoutePathSource.")]
        [SerializeField] private MonoBehaviour routePathSource;

        private NetworkManager _networkManager;
        private bool _bound;

        private void OnEnable() => TryBind();

        private void Update()
        {
            if (!_bound) TryBind();
            if (_bound && boatController != null && boatController.IsSpawned)
                boatController.ApplyTripState(tripManager.State);
        }

        private void OnDisable() => Unbind();

        public void Configure(BoatTripManager manager, EconomyManager economy, NetworkBoatController controller,
            MonoBehaviour pathSource = null)
        {
            Unbind();
            tripManager = manager;
            economyManager = economy;
            boatController = controller;
            routePathSource = pathSource;
            if (isActiveAndEnabled) TryBind();
        }

        private void TryBind()
        {
            if (_bound) return;
            if (tripManager == null) tripManager = FindFirstObjectByType<BoatTripManager>();
            if (economyManager == null) economyManager = FindFirstObjectByType<EconomyManager>();
            if (boatController == null) boatController = FindFirstObjectByType<NetworkBoatController>();
            _networkManager = NetworkManager.Singleton;
            if (tripManager == null || economyManager == null || boatController == null ||
                _networkManager == null || !_networkManager.IsServer) return;

            tripManager.Configure(() => economyManager.BoatRepair.Status, this);
            tripManager.OnTripChanged += PublishState;
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            BoatBoardingPhysicalInteraction.Bind(TryBoardNearest, TryDisembark);

            if (routePathSource is IBoatRoutePathSource pathSource)
                boatController.SetRoutePathSource(pathSource);

            _bound = true;
            PublishState();
        }

        private void Unbind()
        {
            if (!_bound) return;
            if (tripManager != null)
            {
                tripManager.OnTripChanged -= PublishState;
                tripManager.Shutdown();
            }
            if (_networkManager != null) _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            BoatBoardingPhysicalInteraction.Unbind(TryBoardNearest, TryDisembark);
            _bound = false;
        }

        private TransactionResult TryBoardNearest(PlayerId player, ulong requestId)
        {
            if (!_bound || economyManager == null || economyManager.BoatRepair.Status != BoatRepairStatus.Repaired)
                return TransactionResult.Reject(requestId, "BoatNotRepaired", tripManager != null ? tripManager.State.Revision : 0);
            return boatController.TryBoardNearest(player, requestId);
        }

        private TransactionResult TryDisembark(PlayerId player, ulong requestId)
        {
            if (!_bound) return TransactionResult.Reject(requestId, "InvalidState", 0);
            return boatController.TryDisembark(player, requestId);
        }

        private void PublishState()
        {
            if (!_bound && (_networkManager == null || !_networkManager.IsServer)) return;
            var state = tripManager.State;
            if (boatController != null && boatController.IsSpawned) boatController.ApplyTripState(state);

            var syncs = FindObjectsByType<BoatTripPlayerSync>(FindObjectsSortMode.None);
            for (var i = 0; i < syncs.Length; i++)
            {
                var sync = syncs[i];
                if (!sync.IsSpawned || !sync.IsServer) continue;
                sync.PublishTripState(state, new PlayerId(sync.OwnerClientId));
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!_bound) return;
            tripManager.HandlePlayerDisconnected(new PlayerId(clientId));
        }

        public bool IsConnected(PlayerId player) =>
            _networkManager != null && _networkManager.IsServer && _networkManager.ConnectedClients.ContainsKey(player.Value);
    }
}