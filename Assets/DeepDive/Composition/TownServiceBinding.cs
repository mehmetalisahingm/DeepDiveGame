using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // Host-side wiring for the P3.2 town: binds Mehmet's service-interaction seam, the shop purchase RPC and
    // the free-boat-part claim to the economy, and mirrors town progress/outcomes into each player's
    // EconomyPlayerSync. It owns no rules; those live in TownServiceHandler / EconomyManager.
    [DisallowMultipleComponent]
    public sealed class TownServiceBinding : MonoBehaviour
    {
        private const float ProgressInterval = 0.25f;
        private const float AnchorRefreshInterval = 1f;
        private const float ShopRangeSlack = 1.5f;

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private TownServiceHandler handler;
        private Func<PlayerId, ServicePointDefinition, ulong, PlayerActionResult> interactionDelegate;
        private Func<PlayerId, string, ulong, TransactionResult> purchaseDelegate;
        private Func<PlayerId, string, ulong, TransactionResult> foundPartDelegate;
        private readonly Dictionary<string, ServicePointAnchor> anchors = new Dictionary<string, ServicePointAnchor>();
        private double nextProgress;
        private double nextAnchorRefresh;

        public TownServiceHandler Handler => handler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<TownServiceBinding>() == null)
                session.gameObject.AddComponent<TownServiceBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private bool IsHost => economy != null && adapter != null && manager != null &&
            manager.IsListening && adapter.IsAuthority;

        private void Update()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (!IsHost)
            {
                Unbind();
                return;
            }

            EnsureHandler();
            Bind();
            var now = Time.unscaledTimeAsDouble;
            if (now >= nextAnchorRefresh)
            {
                nextAnchorRefresh = now + AnchorRefreshInterval;
                RefreshAnchors();
            }

            handler.Tick();
            if (now < nextProgress) return;
            nextProgress = now + ProgressInterval;
            PublishProgress();
        }

        private void EnsureHandler()
        {
            if (handler != null) return;
            handler = new TownServiceHandler(economy,
                townOpen: () => IsHost && adapter.Session.State.Phase != SessionPhase.Dive,
                canServe: player => adapter.Session.Roster.ContainsKey(player) &&
                                    manager.ConnectedClients.ContainsKey(player.Value),
                inRange: IsNearShop);
            handler.OutcomeReady += ForwardOutcome;
            handler.ShopClosed += player => SyncFor(player)?.SetActiveService(string.Empty);
            interactionDelegate = handler.HandleInteraction;
            purchaseDelegate = handler.HandlePurchase;
            foundPartDelegate = handler.HandleFoundPart;
        }

        private void Bind()
        {
            ServiceInteractionAuthority.Bind(interactionDelegate);
            EconomyPurchaseAuthority.Bind(purchaseDelegate);
            BoatPartClaim.Bind(foundPartDelegate);
        }

        private void Unbind()
        {
            if (handler == null) return;
            ServiceInteractionAuthority.Unbind(interactionDelegate);
            EconomyPurchaseAuthority.Unbind(purchaseDelegate);
            BoatPartClaim.Unbind(foundPartDelegate);
            handler.CloseAll();
            handler = null;
            interactionDelegate = null;
            purchaseDelegate = null;
            foundPartDelegate = null;
        }

        private void RefreshAnchors()
        {
            anchors.Clear();
            foreach (var anchor in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
            {
                var id = anchor.Definition.ServiceId;
                if (!string.IsNullOrEmpty(id)) anchors[id] = anchor;
            }
        }

        private bool IsNearShop(PlayerId player, ServicePointDefinition shop)
        {
            if (!anchors.TryGetValue(shop.ServiceId, out var anchor) || anchor == null) return false;
            if (!manager.ConnectedClients.TryGetValue(player.Value, out var client) || client.PlayerObject == null)
                return false;
            var reach = shop.InteractionDistance * ShopRangeSlack;
            return (client.PlayerObject.transform.position - anchor.WorldPosition).sqrMagnitude <= reach * reach;
        }

        private EconomyPlayerSync SyncFor(PlayerId player) =>
            manager != null && manager.ConnectedClients.TryGetValue(player.Value, out var client) &&
            client.PlayerObject != null
                ? client.PlayerObject.GetComponent<EconomyPlayerSync>()
                : null;

        private void ForwardOutcome(PlayerId player, TownServiceOutcome outcome) =>
            SyncFor(player)?.PublishServiceOutcome(outcome);

        private void PublishProgress()
        {
            var boatParts = economy.BoatRepair.CompletedPartIds.Count;
            var mask = 0;
            foreach (var part in economy.BoatRepair.CompletedPartIds)
                for (var i = 0; i < BoatRepairParts.All.Count; i++)
                    if (part == BoatRepairParts.All[i]) mask |= 1 << i;
            foreach (var pair in manager.ConnectedClients)
            {
                var player = new PlayerId(pair.Key);
                SyncFor(player)?.PublishTownProgress(economy.PendingCountFor(player, TurnInKind.Catch),
                    economy.PendingCountFor(player, TurnInKind.Recording), boatParts, mask);
            }
        }

        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
