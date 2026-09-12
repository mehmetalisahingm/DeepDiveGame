using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    // Owns shared currency, capture sale, the shop and equipment loadout (docs/plan/
    // CONTRACTS.md: "Ekonomi ve ilerleme | Mert" and "Kalici kayit | Mert"). Currency is a
    // single crew-wide balance ("ortak para sistemi"), not per-player.
    //
    // Host-authoritative, same shape as InventoryManager: a pure state authority meant to be
    // driven by real network requests once Mehmet's shop-request transport (P3) exists.
    // Capture pricing hooks off InventoryManager.OnDiveSummaryReady directly (both modules are
    // expected on the same session root, mirroring InventoryManager -> SessionManager).
    //
    // Recording payment (docs/plan/CONTRACTS.md RecordingResult, P3-B/#35) is intentionally not
    // wired yet: Utku's real recording/target validation does not exist yet, so there is
    // nothing genuine to price. Add it once #35 has a real RecordingResult producer.
    [RequireComponent(typeof(InventoryManager))]
    public class EconomyManager : MonoBehaviour
    {
        public event Action OnBalanceChanged;
        public event Action<PlayerId> OnLoadoutChanged;

        public int SharedBalance { get; private set; }
        public int Revision { get; private set; }

        private InventoryManager _inventory;
        private readonly Dictionary<string, int> _priceBySpeciesId = new Dictionary<string, int>();
        private readonly Dictionary<string, EquipmentDefinition> _catalog = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<PlayerId, HashSet<string>> _loadout = new Dictionary<PlayerId, HashSet<string>>();
        private readonly HashSet<string> _soldCaptureIds = new HashSet<string>();
        // Keyed by (player, requestId), not requestId alone: requestId is generated per-player,
        // so two different players can legitimately produce the same value (Mehmet's review on
        // #37) - keying on requestId alone would let one player's purchase replay another's
        // stale result instead of being processed.
        private readonly Dictionary<(PlayerId, ulong), TransactionResult> _processedRequests =
            new Dictionary<(PlayerId, ulong), TransactionResult>();
        private bool _subscribed;

        // Resolved lazily instead of in Awake(): AddComponent does not guarantee Awake has run
        // by the time a caller (e.g. an EditMode test right after AddComponent) uses this -
        // same reasoning as InventoryManager.Session.
        private InventoryManager Inventory
        {
            get
            {
                if (_inventory == null) _inventory = GetComponent<InventoryManager>();
                EnsureSubscribed();
                return _inventory;
            }
        }

        private void Awake() => EnsureSubscribed();
        private void OnEnable() => EnsureSubscribed();

        private void OnDisable()
        {
            if (!_subscribed) return;
            _inventory.OnDiveSummaryReady -= HandleDiveSummary;
            _subscribed = false;
        }

        private void EnsureSubscribed()
        {
            if (_subscribed) return;
            if (_inventory == null) _inventory = GetComponent<InventoryManager>();
            if (_inventory == null) return;
            _inventory.OnDiveSummaryReady += HandleDiveSummary;
            _subscribed = true;
        }

        // Static price/catalog setup (e.g. from composition at startup, mirroring how
        // SpeciesDefinition/ItemDefinition assets would be read). Zero clamps negative input.
        // Also ensures subscription to InventoryManager as early as possible, since this is
        // typically the first call made against a freshly added EconomyManager.
        public void SetPrice(string speciesId, int pricePerCapture)
        {
            EnsureSubscribed();
            _priceBySpeciesId[speciesId] = Math.Max(0, pricePerCapture);
        }

        public void AddToCatalog(EquipmentDefinition definition)
        {
            EnsureSubscribed();
            _catalog[definition.EquipmentId] = definition;
        }

        // Read-only catalog access for Composition. Equipment ownership/economy remains here;
        // the diver module consumes definitions only to recompute player stats.
        public bool TryGetEquipmentDefinition(string equipmentId, out EquipmentDefinition definition) =>
            _catalog.TryGetValue(equipmentId, out definition);

        public IReadOnlyList<string> LoadoutFor(PlayerId player) =>
            _loadout.TryGetValue(player, out var set) ? new List<string>(set) : Array.Empty<string>();

        public LoadoutState LoadoutStateFor(PlayerId player) =>
            new LoadoutState(player, LoadoutFor(player), Revision);

        private void HandleDiveSummary(DiveSummary summary) => SellPreservedCatches(summary);

        // Pays for every preserved capture not already sold. Safe to call more than once for
        // the same DiveSummary (e.g. a retried event delivery): already-sold ids are skipped,
        // so a dive's proceeds are never paid twice (CONTRACTS: "ayni av ... iki kez para
        // uretmemeli").
        public int SellPreservedCatches(DiveSummary summary)
        {
            var earned = 0;
            foreach (var captureId in summary.PreservedCaptureIds)
            {
                if (_soldCaptureIds.Contains(captureId)) continue;
                if (!Inventory.TryGetCapture(captureId, out var capture)) continue;
                _soldCaptureIds.Add(captureId);
                earned += PriceFor(capture);
            }

            if (earned <= 0) return 0;
            SharedBalance += earned;
            Revision++;
            OnBalanceChanged?.Invoke();
            return earned;
        }

        private int PriceFor(CaptureResult capture) =>
            _priceBySpeciesId.TryGetValue(capture.SpeciesId, out var price) ? price : 0;

        // Host-side shop purchase. Balance debit and loadout grant are one logical step: either
        // both happen or neither does (CONTRACTS: "Para dusme ve ekipman olusturma tek islem").
        // requestId makes a retried/duplicate request return the original result instead of
        // charging twice (CONTRACTS: "Ayni bildirimin tekrari bonusu tekrar eklemez").
        public TransactionResult TryPurchase(PlayerId player, string equipmentId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;

            TransactionResult result;
            if (!_catalog.TryGetValue(equipmentId, out var definition))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_loadout.TryGetValue(player, out var owned) && owned.Contains(equipmentId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (SharedBalance < definition.Price)
                result = TransactionResult.Reject(requestId, "InsufficientFunds", Revision);
            else
            {
                SharedBalance -= definition.Price;
                if (!_loadout.TryGetValue(player, out var set))
                {
                    set = new HashSet<string>();
                    _loadout[player] = set;
                }
                set.Add(equipmentId);
                Revision++;
                result = TransactionResult.Ok(requestId, Revision);
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted)
            {
                OnBalanceChanged?.Invoke();
                OnLoadoutChanged?.Invoke(player);
            }
            return result;
        }
    }
}
