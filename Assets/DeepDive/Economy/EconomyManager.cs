using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    // P3-C economy authority: safe-catch sale, validated recording rewards, shared crew money,
    // shop/loadout progression and snapshot state. Network transport and disk orchestration stay
    // outside this class; Composition wires those pieces to this host-owned state.
    [RequireComponent(typeof(InventoryManager))]
    public class EconomyManager : MonoBehaviour
    {
        public event Action OnBalanceChanged;
        public event Action<PlayerId> OnLoadoutChanged;

        public int SharedBalance { get; private set; }
        public int Revision { get; private set; }
        public bool PurchasesEnabled { get; set; } = true;

        private InventoryManager _inventory;
        private readonly Dictionary<string, int> _priceBySpeciesId = new Dictionary<string, int>();
        private readonly Dictionary<int, int> _recordingPriceByQuality = new Dictionary<int, int>();
        private readonly Dictionary<string, EquipmentDefinition> _catalog = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<PlayerId, HashSet<string>> _loadout = new Dictionary<PlayerId, HashSet<string>>();
        private readonly HashSet<string> _soldCaptureIds = new HashSet<string>();
        private readonly HashSet<string> _paidRecordingIds = new HashSet<string>();
        private readonly Dictionary<(PlayerId, ulong), TransactionResult> _processedRequests =
            new Dictionary<(PlayerId, ulong), TransactionResult>();
        private bool _subscribed;

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

        // Small P3 tuning table. Composition calls this idempotently on the live session root.
        // Values intentionally stay here because P3-C owns economy numbers.
        public void ConfigureP3Defaults()
        {
            SetPrice("sea_bass", 120);
            SetRecordingPrice(1, 100); // Bronze
            SetRecordingPrice(2, 200); // Silver
            SetRecordingPrice(3, 350); // Gold
            SetRecordingPrice(4, 550); // Platinum

            AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 300));
            AddToCatalog(new EquipmentDefinition("tube-2", "tube", 2, 700));
            AddToCatalog(new EquipmentDefinition("tube-3", "tube", 3, 1200));
        }

        public void SetPrice(string speciesId, int pricePerCapture)
        {
            EnsureSubscribed();
            if (string.IsNullOrWhiteSpace(speciesId)) return;
            _priceBySpeciesId[speciesId] = Math.Max(0, pricePerCapture);
        }

        public void SetRecordingPrice(int quality, int credits)
        {
            if (quality < 1 || quality > 4) return;
            _recordingPriceByQuality[quality] = Math.Max(0, credits);
        }

        public int RecordingPriceForQuality(int quality) =>
            _recordingPriceByQuality.TryGetValue(quality, out var credits) ? credits : 0;

        public void AddToCatalog(EquipmentDefinition definition)
        {
            EnsureSubscribed();
            if (string.IsNullOrWhiteSpace(definition.EquipmentId)) return;
            _catalog[definition.EquipmentId] = definition;
        }

        public bool TryGetEquipmentDefinition(string equipmentId, out EquipmentDefinition definition) =>
            _catalog.TryGetValue(equipmentId, out definition);

        public IReadOnlyList<string> LoadoutFor(PlayerId player) =>
            _loadout.TryGetValue(player, out var set) ? new List<string>(set) : Array.Empty<string>();

        public LoadoutState LoadoutStateFor(PlayerId player) =>
            new LoadoutState(player, LoadoutFor(player), Revision);

        public bool TryGetNextTubeUpgrade(PlayerId player, out EquipmentDefinition next)
        {
            var currentLevel = CurrentTubeLevel(player);
            var found = false;
            next = default;

            foreach (var definition in _catalog.Values)
            {
                if (!string.Equals(definition.Slot, "tube", StringComparison.OrdinalIgnoreCase) ||
                    definition.Level <= currentLevel)
                    continue;

                if (!found || definition.Level < next.Level ||
                    (definition.Level == next.Level &&
                     string.CompareOrdinal(definition.EquipmentId, next.EquipmentId) < 0))
                {
                    next = definition;
                    found = true;
                }
            }

            return found;
        }

        private int CurrentTubeLevel(PlayerId player)
        {
            var strongest = 0;
            if (!_loadout.TryGetValue(player, out var owned)) return strongest;

            foreach (var equipmentId in owned)
                if (_catalog.TryGetValue(equipmentId, out var definition) &&
                    string.Equals(definition.Slot, "tube", StringComparison.OrdinalIgnoreCase))
                    strongest = Math.Max(strongest, definition.Level);

            return strongest;
        }

        private void HandleDiveSummary(DiveSummary summary) => SellPreservedCatches(summary);

        public int SellPreservedCatches(DiveSummary summary)
        {
            var earned = 0;
            foreach (var captureId in summary.PreservedCaptureIds)
            {
                if (string.IsNullOrWhiteSpace(captureId) || _soldCaptureIds.Contains(captureId)) continue;
                if (!Inventory.TryGetCapture(captureId, out var capture)) continue;

                var price = PriceFor(capture);
                if (price <= 0) continue; // an unpriced catch is not burned; it can be priced later.
                _soldCaptureIds.Add(captureId);
                earned += price;
            }

            if (earned <= 0) return 0;
            SharedBalance += earned;
            Revision++;
            OnBalanceChanged?.Invoke();
            return earned;
        }

        private int PriceFor(CaptureResult capture) =>
            _priceBySpeciesId.TryGetValue(capture.SpeciesId, out var price) ? price : 0;

        // Called only after World/Composition selected a payable recording from a safely
        // returned player. recordingId is a persistent payment id and can never mint twice.
        public PlayerActionResult TryPayRecording(RecordingResult result)
        {
            if (string.IsNullOrWhiteSpace(result.RecordingId) ||
                string.IsNullOrWhiteSpace(result.DiveId) ||
                string.IsNullOrWhiteSpace(result.SubjectId) ||
                result.Quality < 1 || result.Quality > 4 ||
                result.ValidDurationSeconds <= 0f)
                return PlayerActionResult.InvalidTarget;

            if (_paidRecordingIds.Contains(result.RecordingId))
                return PlayerActionResult.DuplicateRequest;

            var reward = RecordingPriceForQuality(result.Quality);
            if (reward <= 0)
                return PlayerActionResult.InvalidState;

            _paidRecordingIds.Add(result.RecordingId);
            SharedBalance += reward;
            Revision++;
            OnBalanceChanged?.Invoke();
            return PlayerActionResult.Accepted;
        }

        public TransactionResult TryPurchase(PlayerId player, string equipmentId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;

            TransactionResult result;
            if (!PurchasesEnabled)
                result = TransactionResult.Reject(requestId, "InvalidState", Revision);
            else if (requestId == 0 || string.IsNullOrWhiteSpace(equipmentId) ||
                     !_catalog.TryGetValue(equipmentId, out var definition))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_loadout.TryGetValue(player, out var owned) && owned.Contains(equipmentId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (string.Equals(definition.Slot, "tube", StringComparison.OrdinalIgnoreCase) &&
                     definition.Level != CurrentTubeLevel(player) + 1)
                result = TransactionResult.Reject(requestId, "InvalidState", Revision);
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

        public EconomySaveData CreateSaveSnapshot(string campaignId, string checkpointId)
        {
            var host = new PlayerId(0);
            return new EconomySaveData
            {
                schemaVersion = EconomySaveData.CurrentSchemaVersion,
                campaignId = campaignId ?? string.Empty,
                checkpointId = checkpointId ?? string.Empty,
                sharedBalance = SharedBalance,
                revision = Revision,
                hostEquipmentIds = new List<string>(LoadoutFor(host)).ToArray(),
                soldCaptureIds = new List<string>(_soldCaptureIds).ToArray(),
                paidRecordingIds = new List<string>(_paidRecordingIds).ToArray()
            };
        }

        public bool TryRestoreSnapshot(EconomySaveData data)
        {
            if (!EconomySaveStore.IsValid(data)) return false;

            SharedBalance = data.sharedBalance;
            Revision = data.revision;
            _processedRequests.Clear();
            _soldCaptureIds.Clear();
            _paidRecordingIds.Clear();
            _loadout.Clear();

            AddNonBlank(_soldCaptureIds, data.soldCaptureIds);
            AddNonBlank(_paidRecordingIds, data.paidRecordingIds);

            var host = new PlayerId(0);
            var restored = new HashSet<string>();
            if (data.hostEquipmentIds != null)
            {
                foreach (var equipmentId in data.hostEquipmentIds)
                    if (!string.IsNullOrWhiteSpace(equipmentId) && _catalog.ContainsKey(equipmentId))
                        restored.Add(equipmentId);
            }
            if (restored.Count > 0) _loadout[host] = restored;

            OnBalanceChanged?.Invoke();
            OnLoadoutChanged?.Invoke(host);
            return true;
        }

        private static void AddNonBlank(HashSet<string> target, string[] values)
        {
            if (values == null) return;
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value)) target.Add(value);
        }
    }
}
