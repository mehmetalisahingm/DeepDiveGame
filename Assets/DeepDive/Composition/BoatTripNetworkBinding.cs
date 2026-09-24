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
    public sealed class BoatTripNetworkBinding : MonoBehaviour, BoatTripManager.ISessionRoster, IBoatRoutePathSource
    {
        private const float MirrorInterval = 0.1f;

        private SessionNetworkAdapter adapter;
        private NetworkManager networkManager;
        private EconomyManager economy;
        private BoatTripManager tripManager;
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
            if (session != null && session.GetComponent<BoatTripNetworkBinding>() == null)
                session.gameObject.AddComponent<BoatTripNetworkBinding>();
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
            boatController = authorityObject.AddComponent<NetworkBoatController>();
            boatController.Initialize(networkManager, this);
        }

        private void Bind()
        {
            if (bound || economy == null || tripManager == null || boatController == null) return;

            tripManager.Configure(() => economy.BoatRepair.Status, this);
            tripManager.OnTripChanged += PublishStateAndPose;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            BoatBoardingPhysicalInteraction.Bind(TryBoardNearest, TryDisembark);
            boatController.SetRoutePathSource(this);
            bound = true;
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
                if (networkManager != null)
                    networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                BoatBoardingPhysicalInteraction.Unbind(TryBoardNearest, TryDisembark);
            }

            bound = false;
            if (boatController != null)
            {
                boatController.Shutdown();
                Destroy(boatController.gameObject);
                boatController = null;
            }
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
                sync.PublishBoatPose(position, rotation, visible);
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
