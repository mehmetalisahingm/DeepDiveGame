using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    [RequireComponent(typeof(InventoryManager))]
    public class EconomyManager : MonoBehaviour
    {
        public event Action OnBalanceChanged;
        public event Action<PlayerId> OnLoadoutChanged;

        public int SharedBalance { get; private set; }
        public int Revision { get; private set; }
        public string LastCheckpointId { get; private set; } = "";

        private InventoryManager _inventory;
        private readonly Dictionary<string, int> _priceBySpeciesId = new Dictionary<string, int>();
        private readonly Dictionary<int, int> _recordingRewardByQuality = new Dictionary<int, int>();
        private readonly Dictionary<string, EquipmentDefinition> _catalog = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<PlayerId, HashSet<string>> _loadout = new Dictionary<PlayerId, HashSet<string>>();
        private readonly HashSet<string> _soldCaptureIds = new HashSet<string>();
        private readonly HashSet<string> _paidRecordingIds = new HashSet<string>();
        private readonly Dictionary<(PlayerId, ulong), TransactionResult> _processedRequests =
            new Dictionary<(PlayerId, ulong), TransactionResult>();
        private Func<bool> _persist;
        private bool _subscribed;
        private bool _configured;
        private readonly Dictionary<(string Subject, int Quality), int> _subjectRewards = new Dictionary<(string, int), int>();

        private InventoryManager Inventory
        {
            get
            {
                if (_inventory == null) _inventory = GetComponent<InventoryManager>();
                EnsureSubscribed();
                return _inventory;
            }
        }

        private void Awake()
        {
            ConfigureDefaults();
            EnsureSubscribed();
        }

        private void OnEnable() => EnsureSubscribed();

        private void OnDisable()
        {
            if (!_subscribed) return;
            _inventory.OnDiveSummaryReady -= HandleDiveSummary;
            _subscribed = false;
        }

        private void ConfigureDefaults()
        {
            if (_configured) return;
            _configured = true;
            // P3 v1 values: Utku owns quality, Mert owns credits.
            _recordingRewardByQuality[1] = 25;   // Bronze
            _recordingRewardByQuality[2] = 50;   // Silver
            _recordingRewardByQuality[3] = 100;  // Gold
            _recordingRewardByQuality[4] = 200;  // Platinum
            _priceBySpeciesId["sea_bass"] = 120;
            foreach (var subject in new[] { "sea_bass", "event_bioluminescence" })
                foreach (var price in _recordingRewardByQuality) _subjectRewards[(subject, price.Key)] = price.Value;

            _catalog["tube-1"] = new EquipmentDefinition("tube-1", "tube", 1, 100);
            _catalog["tube-2"] = new EquipmentDefinition("tube-2", "tube", 2, 250);
        }

        private void EnsureSubscribed()
        {
            ConfigureDefaults();
            if (_subscribed) return;
            if (_inventory == null) _inventory = GetComponent<InventoryManager>();
            if (_inventory == null) return;
            _inventory.OnDiveSummaryReady += HandleDiveSummary;
            _subscribed = true;
        }

        public void SetPersistenceHandler(Func<bool> persist) => _persist = persist;
        public void ClearPersistenceHandler(Func<bool> persist)
        {
            if (_persist == persist) _persist = null;
        }
        private bool Persist() => _persist == null || _persist();

        public void SetPrice(string speciesId, int pricePerCapture)
        {
            EnsureSubscribed();
            if (string.IsNullOrWhiteSpace(speciesId)) return;
            _priceBySpeciesId[speciesId] = Math.Max(0, pricePerCapture);
        }

        public void SetRecordingReward(int quality, int credits)
        {
            ConfigureDefaults();
            if (quality < 1 || quality > 4) throw new ArgumentOutOfRangeException(nameof(quality));
            _recordingRewardByQuality[quality] = Math.Max(0, credits);
            foreach (var subject in new[] { "sea_bass", "event_bioluminescence" })
                _subjectRewards[(subject, quality)] = Math.Max(0, credits);
        }

        public int RecordingRewardFor(int quality)
        {
            ConfigureDefaults();
            return _recordingRewardByQuality.TryGetValue(quality, out var reward) ? reward : 0;
        }

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

        private void HandleDiveSummary(DiveSummary summary)
        {
            LastCheckpointId = summary.CheckpointId ?? "";
            SellPreservedCatches(summary);
        }

        public int SellPreservedCatches(DiveSummary summary)
        {
            var earned = 0;
            var addedIds = new List<string>();
            foreach (var captureId in summary.PreservedCaptureIds)
            {
                if (string.IsNullOrWhiteSpace(captureId) || _soldCaptureIds.Contains(captureId)) continue;
                if (!Inventory.TryGetCapture(captureId, out var capture)) continue;
                _soldCaptureIds.Add(captureId);
                addedIds.Add(captureId);
                earned += PriceFor(capture);
            }

            if (earned <= 0) return 0;
            var previousBalance = SharedBalance;
            var previousRevision = Revision;
            SharedBalance += earned;
            Revision++;

            if (!Persist())
            {
                SharedBalance = previousBalance;
                Revision = previousRevision;
                foreach (var id in addedIds) _soldCaptureIds.Remove(id);
                return 0;
            }

            OnBalanceChanged?.Invoke();
            return earned;
        }

        private int PriceFor(CaptureResult capture) =>
            _priceBySpeciesId.TryGetValue(capture.SpeciesId, out var price) ? price : 0;

        // Called by RecordingDiveBinding while settling the real safe-return summary.
        public PlayerActionResult TryRewardRecording(RecordingResult result)
        {
            ConfigureDefaults();
            if (string.IsNullOrWhiteSpace(result.RecordingId) || string.IsNullOrWhiteSpace(result.DiveId) ||
                string.IsNullOrWhiteSpace(result.SubjectId) || result.ValidDurationSeconds <= 0f ||
                result.Quality < 1 || result.Quality > 4)
                return PlayerActionResult.InvalidTarget;

            if (_paidRecordingIds.Contains(result.RecordingId)) return PlayerActionResult.DuplicateRequest;
            if (!_subjectRewards.TryGetValue((result.SubjectId, result.Quality), out var reward) || reward <= 0)
                return PlayerActionResult.Rejected;

            var previousBalance = SharedBalance;
            var previousRevision = Revision;
            _paidRecordingIds.Add(result.RecordingId);
            SharedBalance += reward;
            Revision++;

            if (!Persist())
            {
                _paidRecordingIds.Remove(result.RecordingId);
                SharedBalance = previousBalance;
                Revision = previousRevision;
                return PlayerActionResult.Rejected;
            }

            OnBalanceChanged?.Invoke();
            return PlayerActionResult.Accepted;
        }

        public TransactionResult TryPurchase(PlayerId player, string equipmentId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;

            TransactionResult result;
            if (string.IsNullOrWhiteSpace(equipmentId) || !_catalog.TryGetValue(equipmentId, out var definition))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_loadout.TryGetValue(player, out var owned) && owned.Contains(equipmentId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (SharedBalance < definition.Price)
                result = TransactionResult.Reject(requestId, "InsufficientFunds", Revision);
            else
            {
                var previousBalance = SharedBalance;
                var previousRevision = Revision;
                var createdSet = false;
                SharedBalance -= definition.Price;
                if (!_loadout.TryGetValue(player, out var set))
                {
                    set = new HashSet<string>();
                    _loadout[player] = set;
                    createdSet = true;
                }
                set.Add(equipmentId);
                Revision++;

                if (!Persist())
                {
                    set.Remove(equipmentId);
                    if (createdSet && set.Count == 0) _loadout.Remove(player);
                    SharedBalance = previousBalance;
                    Revision = previousRevision;
                    result = TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                else result = TransactionResult.Ok(requestId, Revision);
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted)
            {
                OnBalanceChanged?.Invoke();
                OnLoadoutChanged?.Invoke(player);
            }
            return result;
        }

        public EconomySaveData ExportSaveData(string campaignId, string checkpointId)
        {
            var data = new EconomySaveData
            {
                SchemaVersion = EconomySaveData.CurrentSchemaVersion,
                CampaignId = campaignId ?? string.Empty,
                CheckpointId = checkpointId ?? LastCheckpointId ?? string.Empty,
                SharedBalance = SharedBalance,
                Revision = Revision,
                SoldCaptureIds = new List<string>(_soldCaptureIds),
                PaidRecordingIds = new List<string>(_paidRecordingIds)
            };

            foreach (var pair in _loadout)
            {
                data.Loadouts.Add(new EconomyLoadoutSave
                {
                    PlayerId = pair.Key.Value,
                    EquipmentIds = new List<string>(pair.Value)
                });
            }
            return data;
        }

        public bool TryRestore(EconomySaveData data)
        {
            ConfigureDefaults();
            if (data == null || data.SchemaVersion != EconomySaveData.CurrentSchemaVersion || data.SharedBalance < 0)
                return false;

            SharedBalance = data.SharedBalance;
            Revision = Math.Max(0, data.Revision);
            LastCheckpointId = data.CheckpointId ?? "";
            _soldCaptureIds.Clear();
            _paidRecordingIds.Clear();
            _loadout.Clear();
            _processedRequests.Clear();

            if (data.SoldCaptureIds != null)
                foreach (var id in data.SoldCaptureIds)
                    if (!string.IsNullOrWhiteSpace(id)) _soldCaptureIds.Add(id);
            if (data.PaidRecordingIds != null)
                foreach (var id in data.PaidRecordingIds)
                    if (!string.IsNullOrWhiteSpace(id)) _paidRecordingIds.Add(id);
            if (data.Loadouts != null)
            {
                foreach (var saved in data.Loadouts)
                {
                    var player = new PlayerId(saved.PlayerId);
                    if (!_loadout.TryGetValue(player, out var set))
                    {
                        set = new HashSet<string>();
                        _loadout[player] = set;
                    }
                    if (saved.EquipmentIds == null) continue;
                    foreach (var equipmentId in saved.EquipmentIds)
                        if (!string.IsNullOrWhiteSpace(equipmentId) && _catalog.ContainsKey(equipmentId))
                            set.Add(equipmentId);
                }
            }

            OnBalanceChanged?.Invoke();
            foreach (var player in _loadout.Keys) OnLoadoutChanged?.Invoke(player);
            return true;
        }
    }
}
