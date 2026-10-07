using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Trip;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P3.3 runtime composition root. It owns no trip, repair, route or movement rules; it only
    // connects Mert's BoatTripManager, Utku's authored DiveRoutePath and Mehmet's host mover.
    [DisallowMultipleComponent]
    public sealed class BoatTripNetworkBinding : MonoBehaviour, BoatTripManager.ISessionRoster,
        IBoatRoutePathSource, IBoatTripRouteCatalog
    {
        private const float MirrorInterval = 0.1f;

        private SessionNetworkAdapter adapter;
        private NetworkManager networkManager;
        private EconomyManager economy;
        private BoatTripManager tripManager;
        private System.Func<string> activeVehicleProvider;
        private NetworkBoatController boatController;
        private bool bound;
        private double nextMirror;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session == null) return;

            var binding = session.GetComponent<BoatTripNetworkBinding>();
            if (binding == null)
                session.gameObject.AddComponent<BoatTripNetworkBinding>();
            else
                binding.RefreshWorldRoute();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            networkManager = GetComponent<NetworkManager>();
            if (adapter == null || networkManager == null) enabled = false;
        }

        private bool IsHost => adapter != null && networkManager != null &&
                               networkManager.IsListening && adapter.IsAuthority;

        private void Update()
        {
            if (!IsHost)
            {
                Unbind();
                return;
            }

            EnsureRuntimeObjects();
            if (economy == null || tripManager == null || boatController == null) return;
            Bind();
            if (!bound) return;

            tripManager.TryAutoRecallIfEmpty();
            boatController.ApplyTripState(tripManager.State);

            var now = Time.unscaledTimeAsDouble;
            if (now < nextMirror) return;
            nextMirror = now + MirrorInterval;
            PublishStateAndPose();
        }

        private void EnsureRuntimeObjects()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (tripManager == null)
            {
                tripManager = GetComponent<BoatTripManager>();
                if (tripManager == null) tripManager = gameObject.AddComponent<BoatTripManager>();
            }

            if (boatController != null) return;
            var authorityObject = new GameObject("P3BoatAuthority");
            authorityObject.transform.SetParent(transform, false);
            var hullPresentation = authorityObject.AddComponent<BoatHullPresentation>();
            hullPresentation.Initialize(null);
            boatController = authorityObject.AddComponent<NetworkBoatController>();
            boatController.Initialize(networkManager, this);
        }

        // P4.3 seam consumed by Mert's authoritative active-vehicle selection. This method owns no
        // unlock/ownership decision; it only applies the already-approved physical profile to
        // Mehmet's host mover. The authoritative trip state must already carry the same boatId so
        // clients never observe old-trip state paired with a newly selected hull.
        public bool ConfigureActiveVehiclePhysicalProfile(string boatId, BoatHullKind hullKind, string dockRouteId)
        {
            if (!IsHost) return false;
            EnsureRuntimeObjects();
            if (tripManager == null || tripManager.State.BoatId != boatId) return false;
            return boatController != null && boatController.ConfigureActiveVehicle(boatId, hullKind, dockRouteId);
        }

        private void RefreshWorldRoute()
        {
            if (!IsHost || boatController == null) return;
            boatController.SetRoutePathSource(this);
            ApplyActiveVehiclePhysicalProfile();
            PublishStateAndPose();
        }

        private void Bind()
        {
            if (bound || economy == null || tripManager == null || boatController == null) return;

            activeVehicleProvider = () => economy.ActiveVehicleId;
            tripManager.Configure(() => economy.BoatRepair.Status, this, activeVehicleProvider, this);
            ActiveVehicle.Bind(activeVehicleProvider);
            tripManager.OnTripChanged += PublishStateAndPose;
            economy.OnFleetChanged += HandleFleetChanged;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            BoatBoardingPhysicalInteraction.Bind(TryBoardNearest, TryDisembark);
            boatController.SetRoutePathSource(this);
            bound = true;
            ApplyActiveVehiclePhysicalProfile();
            PublishStateAndPose();
        }

        private void Unbind()
        {
            if (!bound && boatController == null) return;

            if (bound)
            {
                if (tripManager != null)
                {
                    tripManager.OnTripChanged -= PublishStateAndPose;
                    tripManager.Shutdown();
                }
                if (economy != null) economy.OnFleetChanged -= HandleFleetChanged;
                if (networkManager != null)
                    networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                BoatBoardingPhysicalInteraction.Unbind(TryBoardNearest, TryDisembark);
                if (activeVehicleProvider != null) { ActiveVehicle.Unbind(activeVehicleProvider); activeVehicleProvider = null; }
            }

            bound = false;
            if (boatController != null)
            {
                boatController.Shutdown();
                Destroy(boatController.gameObject);
                boatController = null;
            }
        }

        private void HandleFleetChanged()
        {
            if (!bound) return;
            ApplyActiveVehiclePhysicalProfile();
            PublishStateAndPose();
        }

        private bool ApplyActiveVehiclePhysicalProfile()
        {
            if (!IsHost || economy == null || tripManager == null || boatController == null) return false;
            var boatId = economy.ActiveVehicleId;
            if (string.IsNullOrWhiteSpace(boatId)) return false;
            var vehicleClass = VehicleCatalog.ClassOf(boatId);
            if (!BoatHullSeatRules.TryFromVehicleClass(vehicleClass, out var hullKind)) return false;

            // Every route departs from the same canonical town berth. Physical hull spacing is applied
            // by NetworkBoatController along near WP_0 -> WP_1, never by mutating World route data.
            return ConfigureActiveVehiclePhysicalProfile(boatId, hullKind, BoatTripIds.NearRouteId);
        }

        private TransactionResult TryBoardNearest(PlayerId player, ulong requestId)
        {
            if (!bound || economy == null || boatController == null)
                return TransactionResult.Reject(requestId, "InvalidState", tripManager != null ? tripManager.State.Revision : 0);
            if (economy.BoatRepair.Status != BoatRepairStatus.Repaired)
                return TransactionResult.Reject(requestId, "BoatNotRepaired", tripManager.State.Revision);
            return boatController.TryBoardNearest(player, requestId);
        }

        private TransactionResult TryDisembark(PlayerId player, ulong requestId)
        {
            if (!bound || boatController == null)
                return TransactionResult.Reject(requestId, "InvalidState", tripManager != null ? tripManager.State.Revision : 0);

            // Inbound finishes with the hull facing shore. Before a docked player steps out, reset
            // the boat to the route's canonical outbound berth heading so the local stern offset
            // resolves exactly to Utku's Dock_Town_Anchor rather than to the seaward side.
            if (tripManager != null && tripManager.State.Phase == BoatTripPhase.Docked)
                boatController.SetRoutePathSource(this);

            return boatController.TryDisembark(player, requestId);
        }

        private void PublishStateAndPose()
        {
            if (!bound || tripManager == null || boatController == null || networkManager == null || !networkManager.IsServer)
                return;

            var state = tripManager.State;
            boatController.ApplyTripState(state);
            var visible = economy != null && economy.BoatRepair.Status == BoatRepairStatus.Repaired;
            var position = boatController.transform.position;
            var rotation = boatController.transform.rotation;

            var syncs = FindObjectsByType<BoatTripPlayerSync>(FindObjectsSortMode.None);
            for (var i = 0; i < syncs.Length; i++)
            {
                var sync = syncs[i];
                if (!sync.IsSpawned || !sync.IsServer) continue;
                sync.PublishTripState(state, new PlayerId(sync.OwnerClientId));
                sync.PublishBoatPose(position, rotation, visible, boatController.HullKind);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!bound || tripManager == null) return;
            tripManager.HandlePlayerDisconnected(new PlayerId(clientId));
            PublishStateAndPose();
        }

        public bool IsConnected(PlayerId player) =>
            networkManager != null && networkManager.IsServer && networkManager.ConnectedClients.ContainsKey(player.Value);

        public bool TryGetDefinition(string routeId, out DiveRouteDefinition definition) =>
            DiveRoutePath.TryGetDefinition(routeId, out definition);

        public bool TryGetRoute(string routeId, List<Vector3> points, out float outboundSeconds, out float inboundSeconds)
        {
            outboundSeconds = 0f;
            inboundSeconds = 0f;
            if (points == null) return false;
            points.Clear();

            if (!DiveRoutePath.TryFind(routeId, out var route) || route == null || !route.IsUsable) return false;
            route.Refresh();
            var definition = route.ToDefinition();
            for (var i = 0; i < route.Waypoints.Count; i++) points.Add(route.Waypoints[i]);
            outboundSeconds = definition.OutboundSeconds;
            inboundSeconds = definition.InboundSeconds;
            return points.Count >= 2;
        }

        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
