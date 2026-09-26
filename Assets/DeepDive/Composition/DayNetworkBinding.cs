using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.Session;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.1 composition root for the shared day (#90). It owns no day rule: DayEngine owns the clock,
    // the sleep gate and the close order. This only (a) says who counts as active, (b) connects the
    // engine's close hooks to the economy/inventory/save/session that already exist, (c) feeds the
    // day ledger from real sales and spends, (d) writes the day into the one campaign file, and
    // (e) mirrors the state to every player. Bed proximity/player state stay Mehmet's: his
    // interaction calls HomeBedInteraction, which this binds to the engine while it is the host.
    [DisallowMultipleComponent]
    public sealed class DayNetworkBinding : MonoBehaviour, IDayRoster, IDayCloseHooks
    {
        private const float PublishInterval = 0.1f;   // a freshly spawned player sync shows defaults until the first publish, so keep it short

        // Mehmet's morning placement (players at home, active boat at the dock) subscribes here.
        public static event Action<int> MorningBegan;

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private EconomySaveStore store;
        private InventoryManager inventory;
        private DayManager day;
        private bool bound;
        private double nextPublish;
        private string rosterSignature = "";
        private readonly HashSet<string> reportedParts = new HashSet<string>();
        private Func<PlayerId, string, ulong, TransactionResult> enterDelegate;
        private Func<PlayerId, ulong, TransactionResult> leaveDelegate;
        private Action<string, int, int, int, bool> settledDelegate;
        private Action<string, int> spentDelegate;
        private Action boatDelegate;
        private Action<DaySummary> summaryDelegate;
        private Action stateDelegate;
        private Action stateDelegateStorage;
        private bool dirty;
        private Func<PlayerId, string, ulong, TransactionResult> storeDelegate;
        private Func<PlayerId, string, ulong, TransactionResult> retrieveDelegate;
        private Func<PlayerId, ulong, TransactionResult> openDelegate;

        public DayEngine Engine => day != null ? day.Engine : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<DayNetworkBinding>() == null)
                session.gameObject.AddComponent<DayNetworkBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private bool IsHost => adapter != null && manager != null && manager.IsListening && adapter.IsAuthority;

        private void Update()
        {
            if (!IsHost)
            {
                Unbind();
                return;
            }

            EnsureObjects();
            if (economy == null || store == null || inventory == null || day == null) return;
            Bind();

            // The clock holds still in the lobby and while a scene loads; the sleep gate does not.
            day.Engine.ClockPaused = adapter.Session.State.Phase == SessionPhase.Lobby || adapter.Connection.IsSceneLoading;

            var signature = RosterSignature();
            if (signature != rosterSignature)
            {
                rosterSignature = signature;
                day.Engine.NotifyRosterChanged();
            }

            var now = Time.unscaledTimeAsDouble;
            if (!dirty && now < nextPublish) return;
            nextPublish = now + PublishInterval;
            dirty = false;
            Publish();
        }

        private void EnsureObjects()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (store == null) store = GetComponent<EconomySaveStore>();
            if (inventory == null) inventory = GetComponent<InventoryManager>();
            if (day == null)
            {
                day = GetComponent<DayManager>();
                if (day == null) day = gameObject.AddComponent<DayManager>();
            }
        }

        private void Bind()
        {
            if (bound) return;

            day.Configure(this, this, 17);
            store.Day = day.Engine;

            enterDelegate = day.Engine.TryEnterBed;
            leaveDelegate = day.Engine.TryLeaveBed;
            HomeBedInteraction.Bind(enterDelegate, leaveDelegate);
            // Mehmet's physical layer resolves what the player is looking at and calls Open; this answers "the storage
            // authority exists". The item moves re-check, on the host, that the player really is at the storage.
            openDelegate = (player, requestId) => TransactionResult.Ok(requestId, economy.Revision);
            storeDelegate = (player, itemId, requestId) => IsAtStorage(player)
                ? economy.TryStoreItem(player, itemId, requestId)
                : TransactionResult.Reject(requestId, "NotAtStorage", economy.Revision);
            retrieveDelegate = (player, itemId, requestId) => IsAtStorage(player)
                ? economy.TryRetrieveItem(player, itemId, requestId)
                : TransactionResult.Reject(requestId, "NotAtStorage", economy.Revision);
            HomeStorageInteraction.Bind(openDelegate);
            HomeStorageItems.Bind(storeDelegate, retrieveDelegate);

            settledDelegate = (id, count, grams, earned, isCatch) =>
                day.Engine.RecordSale(id, isCatch ? count : 0, isCatch ? grams : 0, earned);
            spentDelegate = (id, amount) => day.Engine.RecordExpense(id, amount);
            boatDelegate = ReportBoatProgress;
            summaryDelegate = OnSummary;
            stateDelegate = () => dirty = true;
            stateDelegateStorage = () => dirty = true;
            economy.OnStorageChanged += stateDelegateStorage;
            economy.OnSettled += settledDelegate;
            economy.OnSpent += spentDelegate;
            economy.OnBoatRepairChanged += boatDelegate;
            day.Engine.OnSummaryReady += summaryDelegate;
            day.Engine.OnStateChanged += stateDelegate;
            bound = true;
            ReportBoatProgress();
            dirty = true;
        }

        private void Unbind()
        {
            if (!bound) return;
            HomeBedInteraction.Unbind(enterDelegate, leaveDelegate);
            HomeStorageInteraction.Unbind(openDelegate);
            HomeStorageItems.Unbind(storeDelegate, retrieveDelegate);
            if (economy != null)
            {
                economy.OnSettled -= settledDelegate;
                economy.OnSpent -= spentDelegate;
                economy.OnBoatRepairChanged -= boatDelegate;
                economy.OnStorageChanged -= stateDelegateStorage;
            }
            if (day != null)
            {
                day.Engine.OnSummaryReady -= summaryDelegate;
                day.Engine.OnStateChanged -= stateDelegate;
                day.Shutdown();
            }
            if (store != null) store.Day = null;
            bound = false;
        }

        private void ReportBoatProgress()
        {
            if (economy == null || day == null) return;
            var repair = economy.BoatRepair;
            foreach (var part in repair.CompletedPartIds)
                if (reportedParts.Add(part)) day.Engine.RecordProgress("boat-part-" + part);
            if (repair.Status == BoatRepairStatus.Repaired && reportedParts.Add("boat-repaired"))
                day.Engine.RecordProgress("boat-repaired");
        }

        private void OnSummary(DaySummary summary)
        {
            reportedParts.Clear();   // a new day reports progress made from here on
            Publish();
        }

        private void Publish()
        {
            if (day == null) return;
            var state = day.Engine.State;
            var syncs = FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None);
            for (var i = 0; i < syncs.Length; i++)
            {
                if (!syncs[i].IsSpawned || !syncs[i].IsServer) continue;
                syncs[i].PublishDay(state);
                syncs[i].PublishSummary(day.Engine.LastSummary);
                syncs[i].PublishStorage(economy.StoredCount);
                syncs[i].PublishStorageLists(economy.PendingCatchIdsFor(new PlayerId(syncs[i].OwnerClientId)), economy.StoredCatchIds());
            }
        }

        // Host-side proximity: the player's own authoritative position against the physical storage target.
        private bool IsAtStorage(PlayerId player)
        {
            HomeInteractionAnchor storage = null;
            var anchors = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None);
            for (var i = 0; i < anchors.Length; i++)
                if (anchors[i].Kind == HomeInteractionKind.Storage) { storage = anchors[i]; break; }
            if (storage == null) return false;

            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
                if (players[i].IsSpawned && players[i].OwnerClientId == player.Value)
                    return Vector3.Distance(players[i].transform.position, storage.WorldPosition) <= HomePlayerInteractionBinding.InteractionRange + 0.5f;
            return false;
        }

        // ---- IDayRoster ------------------------------------------------------------------------

        // Connected AND not passive. Deliberately NOT phase-dependent: the home (beds) is the lobby room, so
        // players there are active sleepers; only the CLOCK is held still in the lobby (see Update).
        public IReadOnlyList<PlayerId> ActivePlayers()
        {
            var result = new List<PlayerId>();
            if (adapter == null || manager == null || adapter.Session == null) return result;

            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var id in adapter.Session.Roster.Keys)
            {
                if (!manager.ConnectedClients.ContainsKey(id.Value)) continue;
                var passive = false;
                for (var i = 0; i < players.Length; i++)
                    if (players[i].IsSpawned && players[i].OwnerClientId == id.Value) { passive = players[i].Passive.Value; break; }
                if (!passive) result.Add(id);
            }
            return result;
        }

        private string RosterSignature()
        {
            var active = ActivePlayers();
            var ids = new List<ulong>(active.Count);
            for (var i = 0; i < active.Count; i++) ids.Add(active[i].Value);
            ids.Sort();
            return string.Join(",", ids);
        }

        // ---- IDayCloseHooks --------------------------------------------------------------------

        // Trade is already locked by DayLock the moment the day starts closing; nothing else in this
        // build queues work that could still be "accepted but not finished".
        public void SettleAcceptedActions(string closeId) { }

        // D07 at day end: a dive still open is closed through the SAME session transition and
        // inventory finalisation a normal return uses, so catches of players who never got back to
        // safety are lost by the existing rule and the resulting DiveSummary names them.
        public DayDiveClosure CloseOpenDives(string closeId)
        {
            if (adapter == null || adapter.Session.State.Phase != SessionPhase.Dive || inventory == null)
                return DayDiveClosure.None;

            DiveSummary? summary = null;
            void Capture(DiveSummary s) => summary = s;
            inventory.OnDiveSummaryReady += Capture;
            try { adapter.AdvancePhase(); }
            finally { inventory.OnDiveSummaryReady -= Capture; }

            if (!summary.HasValue) return DayDiveClosure.None;
            var rosterCount = adapter.Session.Roster.Count;
            var lostDivers = Math.Max(0, rosterCount - summary.Value.SafelyReturned.Count);
            return new DayDiveClosure(lostDivers, new List<string>(summary.Value.LostCaptureIds));
        }

        public bool Persist() => store != null && store.SaveNow();

        public void BeginMorning(int dayNumber) => MorningBegan?.Invoke(dayNumber);

        private void OnDisable() => Unbind();
        private void OnDestroy() => Unbind();
    }
}
