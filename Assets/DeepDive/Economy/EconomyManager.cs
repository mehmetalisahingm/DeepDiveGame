using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    // Shared-money and progression authority (host only). P3.2: a safe return no longer pays. It
    // queues a PendingTurnIn; money is produced only by an NPC interaction (TrySellCatches /
    // TryTurnInRecordings), once per item id, atomically with the save.
    [RequireComponent(typeof(InventoryManager))]
    public class EconomyManager : MonoBehaviour
    {
        public const string CameraBasicId = "camera-basic";
        // P4.3-C tiers. The slot/level pairs are what Mehmet's DiverEquipmentRules resolves (strongest owned level, no stacking).
        public const string CameraAdvancedId = "camera-advanced";
        public const string CameraProId = "camera-pro";
        public const string FinsId = "fins-1";
        public const string BagId = "bag-1";
        public const string HarpoonId = "harpoon-1";

        private const byte OpEquipment = 1;
        private const byte OpSellCatches = 2;
        private const byte OpTurnInRecordings = 3;
        private const byte OpBoatPart = 4;
        private const byte OpStore = 5;
        private const byte OpRetrieve = 6;
        private const byte OpVehicleBuy = 7;
        private const byte OpVehicleSelect = 8;

        // Shared home storage (P4.1). Slots, not weight: what limits carrying is the bag rule on retrieval.
        public const int StorageCapacityItems = 40;

        public event Action OnBalanceChanged;
        public event Action<PlayerId> OnLoadoutChanged;
        public event Action OnPendingChanged;
        public event Action OnBoatRepairChanged;
        public event Action OnStorageChanged;
        public event Action OnFleetChanged;

        // P4.1 day ledger feeds. A settled hand-in: (deal id, item count, total grams, credits, was it catches).
        // The deal id is the joined ids of the handed-in items, so it is unique (each item leaves the queue once).
        public event Action<string, int, int, int, bool> OnSettled;
        // A spend that was committed and saved: (spend id, credits).
        public event Action<string, int> OnSpent;

        public int SharedBalance { get; private set; }
        public int Revision { get; private set; }
        public string LastCheckpointId { get; private set; } = "";
        public int BoatPartPrice { get; private set; } = 120;

        private sealed class PendingItem
        {
            public string ItemId;
            public TurnInKind Kind;
            public string DiveId;
            public string SubjectId;
            public int WeightGrams;
            public int Quality;
            public float ValidDurationSeconds;
            public PlayerId Carrier;
            public bool Shared;
            public int Revision;

            public PendingTurnInState ToState() => new PendingTurnInState(ItemId, Kind, DiveId, SubjectId,
                WeightGrams, Quality, ValidDurationSeconds, Carrier, Shared, Revision);
        }

        private InventoryManager _inventory;
        private readonly Dictionary<string, int> _priceBySpeciesId = new Dictionary<string, int>();
        private readonly Dictionary<int, int> _recordingRewardByQuality = new Dictionary<int, int>();
        private readonly Dictionary<string, EquipmentDefinition> _catalog = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<PlayerId, HashSet<string>> _loadout = new Dictionary<PlayerId, HashSet<string>>();
        private readonly HashSet<string> _soldCaptureIds = new HashSet<string>();
        private readonly HashSet<string> _paidRecordingIds = new HashSet<string>();
        // P4.2: recordings whose ONE commercial right went to the channel, and channel payouts already credited.
        private readonly HashSet<string> _channelRightIds = new HashSet<string>();
        private readonly HashSet<string> _channelSettleIds = new HashSet<string>();
        private readonly List<PendingItem> _pending = new List<PendingItem>();
        private readonly List<PendingItem> _stored = new List<PendingItem>();
        private readonly List<string> _boatParts = new List<string>();
        // P4.3-C fleet: only PURCHASED boats are stored here; the rowboat is owned when its repair is complete.
        private readonly List<string> _purchasedVehicles = new List<string>();
        private string _activeVehicleId = "";
        // Tier chains: an upgrade can only be bought on top of the previous tier (the same player's loadout).
        private readonly Dictionary<string, string> _equipmentRequires = new Dictionary<string, string>();
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> _processedRequests =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();
        private readonly Dictionary<(PlayerId, ulong, byte), TurnInResult> _processedTurnIns =
            new Dictionary<(PlayerId, ulong, byte), TurnInResult>();
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
            // The basic camera is a separately bought item, not a free default.
            _catalog[CameraBasicId] = new EquipmentDefinition(CameraBasicId, "camera", 1, 150);
            // P4.3 tiers. Prices are WORKING values for the P4.5 balance pass. The catalog owns id/level/price; what a tier DOES
            // (range, low light, speed, capacity, damage) is Mehmet's/Utku's rule, resolved from the owning player's loadout.
            _catalog[CameraAdvancedId] = new EquipmentDefinition(CameraAdvancedId, "camera", 2, 450);
            _catalog[CameraProId] = new EquipmentDefinition(CameraProId, "camera", 3, 1100);
            _catalog[FinsId] = new EquipmentDefinition(FinsId, "fins", 1, 220);
            _catalog[BagId] = new EquipmentDefinition(BagId, "bag", 1, 260);
            _catalog[HarpoonId] = new EquipmentDefinition(HarpoonId, "harpoon", 1, 300);
            _equipmentRequires[CameraAdvancedId] = CameraBasicId;
            _equipmentRequires[CameraProId] = CameraAdvancedId;
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

        public void SetBoatPartPrice(int price) => BoatPartPrice = Math.Max(0, price);

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

        // ---- Pending turn-ins -------------------------------------------------------------

        public IReadOnlyList<PendingTurnInState> PendingTurnIns()
        {
            var list = new List<PendingTurnInState>(_pending.Count);
            foreach (var item in _pending) list.Add(item.ToState());
            return list;
        }

        public int PendingCountFor(PlayerId player, TurnInKind kind)
        {
            var count = 0;
            foreach (var item in _pending)
                if (item.Kind == kind && CanHandIn(item, player)) count++;
            return count;
        }

        // ---- Shared home storage ---------------------------------------------------------------
        // A safe, unpaid catch can be parked at home and taken back out to be sold. An item is in exactly
        // one place - carried/pending OR stored - because the two moves below are single atomic steps
        // (remove from one list, add to the other, write the save; any failure restores both). A stored
        // item is not in _pending, so no NPC can pay it, and it cannot be sold twice or stored twice.

        public IReadOnlyList<PendingTurnInState> StoredItems()
        {
            var list = new List<PendingTurnInState>(_stored.Count);
            foreach (var item in _stored) list.Add(item.ToState());
            return list;
        }

        public int StoredCount => _stored.Count;

        // The unpaid catch ids this player could put into storage (shared escrow or personally carried).
        public IReadOnlyList<string> PendingCatchIdsFor(PlayerId player)
        {
            var ids = new List<string>();
            foreach (var item in _pending)
                if (item.Kind == TurnInKind.Catch && CanHandIn(item, player)) ids.Add(item.ItemId);
            return ids;
        }

        public IReadOnlyList<string> StoredCatchIds()
        {
            var ids = new List<string>(_stored.Count);
            foreach (var item in _stored) ids.Add(item.ItemId);
            return ids;
        }

        // Total weight of the unpaid catches this player is personally carrying (the bag rule for retrieval).
        private int CarriedGrams(PlayerId player)
        {
            var grams = 0;
            foreach (var item in _pending)
                if (item.Kind == TurnInKind.Catch && !item.Shared && item.Carrier.Equals(player)) grams += item.WeightGrams;
            return grams;
        }

        public TransactionResult TryStoreItem(PlayerId player, string itemId, ulong requestId)
        {
            EnsureSubscribed();
            var key = (player, requestId, OpStore);
            if (_processedRequests.TryGetValue(key, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            TransactionResult result;
            var item = FindPending(TurnInKind.Catch, itemId);
            if (requestId == 0 || string.IsNullOrWhiteSpace(itemId) || item == null || !CanHandIn(item, player))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_stored.Count >= StorageCapacityItems)
                result = TransactionResult.Reject(requestId, "StorageFull", Revision);
            else
            {
                var previousRevision = Revision;
                _pending.Remove(item);
                _stored.Add(item);
                Revision++;
                if (!Persist())
                {
                    _stored.Remove(item);
                    _pending.Add(item);
                    Revision = previousRevision;
                    // Not cached: the same request id may be retried once the disk recovers.
                    return TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                result = TransactionResult.Ok(requestId, Revision);
            }

            _processedRequests[key] = result;
            if (result.Accepted)
            {
                OnPendingChanged?.Invoke();
                OnStorageChanged?.Invoke();
            }
            return result;
        }

        // Taking an item out makes the caller its carrier again, so the bag capacity applies.
        public TransactionResult TryRetrieveItem(PlayerId player, string itemId, ulong requestId)
        {
            EnsureSubscribed();
            var key = (player, requestId, OpRetrieve);
            if (_processedRequests.TryGetValue(key, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            PendingItem item = null;
            foreach (var stored in _stored)
                if (string.Equals(stored.ItemId, itemId, StringComparison.Ordinal)) { item = stored; break; }

            TransactionResult result;
            if (requestId == 0 || item == null)
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (CarriedGrams(player) + item.WeightGrams > InventoryManager.CapacityGrams)
                result = TransactionResult.Reject(requestId, "InventoryFull", Revision);
            else
            {
                var previousRevision = Revision;
                var previousCarrier = item.Carrier;
                var previousShared = item.Shared;
                _stored.Remove(item);
                item.Carrier = player;
                item.Shared = false;
                _pending.Add(item);
                Revision++;
                if (!Persist())
                {
                    _pending.Remove(item);
                    item.Carrier = previousCarrier;
                    item.Shared = previousShared;
                    _stored.Add(item);
                    Revision = previousRevision;
                    return TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                result = TransactionResult.Ok(requestId, Revision);
            }

            _processedRequests[key] = result;
            if (result.Accepted)
            {
                OnPendingChanged?.Invoke();
                OnStorageChanged?.Invoke();
            }
            return result;
        }

        // ---- P4.2 single commercial right (CONTRACTS: "NPC veya Kanal secimi hak kullanimiyla ayni islemde") ----

        public bool IsChannelClaimed(string recordingId) => recordingId != null && _channelRightIds.Contains(recordingId);

        // Claims the right in memory; the caller (the channel publish) writes the campaign file once for the whole
        // publication and releases the claim if that write fails. A recording already paid at the NPC is refused.
        // A still-pending NPC candidate is withdrawn from the NPC queue in the same step.
        public bool TryClaimRecordingForChannel(string recordingId)
        {
            if (string.IsNullOrWhiteSpace(recordingId) || _paidRecordingIds.Contains(recordingId)) return false;
            if (!_channelRightIds.Add(recordingId)) return true;
            var pending = FindPending(TurnInKind.Recording, recordingId);
            if (pending != null)
            {
                _pending.Remove(pending);
                _withdrawnForChannel[recordingId] = pending;
                OnPendingChanged?.Invoke();
            }
            Revision++;
            return true;
        }

        private readonly Dictionary<string, PendingItem> _withdrawnForChannel = new Dictionary<string, PendingItem>();

        public void ReleaseChannelClaim(string recordingId)
        {
            if (recordingId == null || !_channelRightIds.Remove(recordingId)) return;
            if (_withdrawnForChannel.TryGetValue(recordingId, out var item))
            {
                _withdrawnForChannel.Remove(recordingId);
                _pending.Add(item);
                OnPendingChanged?.Invoke();
            }
        }

        // Paid inside the day-close transaction (no own write): idempotent by the settle id.
        public bool CreditChannelIncome(string settleId, int amount)
        {
            if (string.IsNullOrWhiteSpace(settleId) || amount < 0) return false;
            if (!_channelSettleIds.Add(settleId)) return true;
            SharedBalance += amount;
            Revision++;
            OnBalanceChanged?.Invoke();
            return true;
        }

        public bool PersistNow() => Persist();

        private static bool CanHandIn(PendingItem item, PlayerId player) =>
            item.Shared || item.Carrier.Equals(player);

        private PendingItem FindPending(TurnInKind kind, string itemId)
        {
            foreach (var item in _pending)
                if (item.Kind == kind && string.Equals(item.ItemId, itemId, StringComparison.Ordinal)) return item;
            return null;
        }

        private void HandleDiveSummary(DiveSummary summary)
        {
            LastCheckpointId = summary.CheckpointId ?? "";
            QueuePreservedCatches(summary);
        }

        // Turns safely returned catches into unpaid pending items. Never credits money. Queued items
        // stay in memory even if the disk write fails: a catch must not vanish because of an I/O error.
        public int QueuePreservedCatches(DiveSummary summary)
        {
            if (summary.PreservedCaptureIds == null) return 0;
            var queued = 0;
            foreach (var captureId in summary.PreservedCaptureIds)
            {
                if (string.IsNullOrWhiteSpace(captureId) || _soldCaptureIds.Contains(captureId) ||
                    FindPending(TurnInKind.Catch, captureId) != null) continue;
                if (!Inventory.TryGetCapture(captureId, out var capture)) continue;
                if (!_priceBySpeciesId.ContainsKey(capture.SpeciesId)) continue;

                var carrier = FindCarrier(captureId, out var found);
                _pending.Add(new PendingItem
                {
                    ItemId = captureId,
                    Kind = TurnInKind.Catch,
                    DiveId = string.IsNullOrEmpty(capture.DiveId) ? summary.DiveId : capture.DiveId,
                    SubjectId = capture.SpeciesId,
                    WeightGrams = capture.WeightGrams,
                    Quality = capture.Quality ?? 0,
                    Carrier = carrier,
                    Shared = !found,
                    Revision = Revision + 1
                });
                queued++;
            }

            if (queued == 0) return 0;
            Revision++;
            Persist();
            OnPendingChanged?.Invoke();
            return queued;
        }

        private PlayerId FindCarrier(string captureId, out bool found)
        {
            foreach (var pair in Inventory.Bags)
                foreach (var item in pair.Value.Items)
                    if (item.CaptureId == captureId)
                    {
                        found = true;
                        return pair.Key;
                    }
            found = false;
            return default;
        }

        // Called by RecordingDiveBinding while settling the real safe-return summary. It only creates
        // the pending commercial right; the recording NPC pays it.
        public PlayerActionResult TryQueueRecordingTurnIn(RecordingResult result)
        {
            ConfigureDefaults();
            if (string.IsNullOrWhiteSpace(result.RecordingId) || string.IsNullOrWhiteSpace(result.DiveId) ||
                string.IsNullOrWhiteSpace(result.SubjectId) || result.ValidDurationSeconds <= 0f ||
                result.Quality < 1 || result.Quality > 4)
                return PlayerActionResult.InvalidTarget;

            if (_paidRecordingIds.Contains(result.RecordingId) || _channelRightIds.Contains(result.RecordingId) ||
                FindPending(TurnInKind.Recording, result.RecordingId) != null)
                return PlayerActionResult.DuplicateRequest;
            if (!_subjectRewards.TryGetValue((result.SubjectId, result.Quality), out var reward) || reward <= 0)
                return PlayerActionResult.Rejected;

            _pending.Add(new PendingItem
            {
                ItemId = result.RecordingId,
                Kind = TurnInKind.Recording,
                DiveId = result.DiveId,
                SubjectId = result.SubjectId,
                Quality = result.Quality,
                ValidDurationSeconds = result.ValidDurationSeconds,
                Carrier = result.PlayerId,
                Shared = false,
                Revision = Revision + 1
            });
            Revision++;
            Persist();
            OnPendingChanged?.Invoke();
            return PlayerActionResult.Accepted;
        }

        private int CatchPrice(PendingItem item) =>
            _priceBySpeciesId.TryGetValue(item.SubjectId, out var price) ? price : 0;

        private int RecordingPrice(PendingItem item) =>
            _subjectRewards.TryGetValue((item.SubjectId, item.Quality), out var reward) ? reward : 0;

        public TurnInResult TrySellCatches(PlayerId player, ulong requestId) =>
            TrySettle(player, requestId, TurnInKind.Catch, OpSellCatches, CatchPrice, _soldCaptureIds);

        public TurnInResult TryTurnInRecordings(PlayerId player, ulong requestId) =>
            TrySettle(player, requestId, TurnInKind.Recording, OpTurnInRecordings, RecordingPrice, _paidRecordingIds);

        // One atomic unit: remove the handed-in items, mark their ids paid, credit the money, write the
        // save. Any failure restores every piece. A replayed request id returns the first result.
        private TurnInResult TrySettle(PlayerId player, ulong requestId, TurnInKind kind, byte op,
            Func<PendingItem, int> priceOf, HashSet<string> paidIds)
        {
            EnsureSubscribed();
            var key = (player, requestId, op);
            if (_processedTurnIns.TryGetValue(key, out var replayed)) return replayed;
            // Closing the day locks new trade; not cached, so the same request id can succeed tomorrow.
            if (DayLock.IsLocked) return TurnInResult.Reject(requestId, "DayClosing", Revision);

            var items = new List<PendingItem>();
            var earned = 0;
            foreach (var item in _pending)
            {
                if (item.Kind != kind || !CanHandIn(item, player)) continue;
                var price = priceOf(item);
                if (price <= 0) continue;
                items.Add(item);
                earned += price;
            }

            TurnInResult result;
            if (items.Count == 0) result = TurnInResult.Reject(requestId, "NothingToTurnIn", Revision);
            else
            {
                var previousBalance = SharedBalance;
                var previousRevision = Revision;
                var snapshot = new List<PendingItem>(_pending);
                foreach (var item in items)
                {
                    _pending.Remove(item);
                    paidIds.Add(item.ItemId);
                }
                SharedBalance += earned;
                Revision++;

                if (!Persist())
                {
                    _pending.Clear();
                    _pending.AddRange(snapshot);
                    foreach (var item in items) paidIds.Remove(item.ItemId);
                    SharedBalance = previousBalance;
                    Revision = previousRevision;
                    // Not cached: the same request id may be retried once the disk recovers.
                    return TurnInResult.Reject(requestId, "SaveFailed", Revision);
                }

                result = TurnInResult.Ok(requestId, earned, items.Count, Revision);
                var ids = new List<string>(items.Count);
                var grams = 0;
                foreach (var item in items) { ids.Add(item.ItemId); grams += item.WeightGrams; }
                OnSettled?.Invoke(string.Join("+", ids), items.Count, grams, earned, kind == TurnInKind.Catch);
            }

            _processedTurnIns[key] = result;
            if (result.Accepted)
            {
                OnBalanceChanged?.Invoke();
                OnPendingChanged?.Invoke();
            }
            return result;
        }

        // ---- Equipment ---------------------------------------------------------------------

        public TransactionResult TryPurchase(PlayerId player, string equipmentId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId, OpEquipment);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            TransactionResult result;
            if (string.IsNullOrWhiteSpace(equipmentId) || !_catalog.TryGetValue(equipmentId, out var definition))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_loadout.TryGetValue(player, out var owned) && owned.Contains(equipmentId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (_equipmentRequires.TryGetValue(equipmentId, out var requiredId) &&
                     !(_loadout.TryGetValue(player, out var ownedForTier) && ownedForTier.Contains(requiredId)))
                result = TransactionResult.Reject(requestId, "RequirementMissing", Revision);
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
                else
                {
                    result = TransactionResult.Ok(requestId, Revision);
                    OnSpent?.Invoke("equip-" + equipmentId + "-" + player.Value, definition.Price);
                }
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted)
            {
                OnBalanceChanged?.Invoke();
                OnLoadoutChanged?.Invoke(player);
            }
            return result;
        }

        // ---- Boat repair -------------------------------------------------------------------

        public BoatRepairState BoatRepair
        {
            get
            {
                var status = _boatParts.Count == 0 ? BoatRepairStatus.Broken
                    : _boatParts.Count >= BoatRepairParts.All.Count ? BoatRepairStatus.Repaired
                    : BoatRepairStatus.InProgress;
                return new BoatRepairState(BoatRepairParts.BoatId, BoatRepairParts.All,
                    new List<string>(_boatParts), status, Revision);
            }
        }

        // Each fixed part contributes at most once. Found parts (Utku's free world parts, verified by the
        // World/interaction layer) and bought parts advance the SAME progress; a contributed part is
        // consumed into the boat and can never be sold, so a free part cannot mint money.
        public TransactionResult TryContributeBoatPart(PlayerId player, string partId, BoatPartSource source,
            ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId, OpBoatPart);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            TransactionResult result;
            var cost = source == BoatPartSource.Purchased ? BoatPartPrice : 0;
            if (!BoatRepairParts.IsPart(partId))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (_boatParts.Contains(partId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (SharedBalance < cost)
                result = TransactionResult.Reject(requestId, "InsufficientFunds", Revision);
            else
            {
                var previousBalance = SharedBalance;
                var previousRevision = Revision;
                _boatParts.Add(partId);
                SharedBalance -= cost;
                Revision++;

                if (!Persist())
                {
                    _boatParts.Remove(partId);
                    SharedBalance = previousBalance;
                    Revision = previousRevision;
                    return TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                result = TransactionResult.Ok(requestId, Revision);
                if (cost > 0) OnSpent?.Invoke("boat-part-" + partId, cost);
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted)
            {
                if (cost > 0) OnBalanceChanged?.Invoke();
                OnBoatRepairChanged?.Invoke();
                OnFleetChanged?.Invoke();   // the last part makes the rowboat owned (and active when nothing else is)
            }
            return result;
        }

        // ---- Vehicle fleet (P4.3-C) --------------------------------------------------------
        // ONE source for "which vehicles do we own and which one is at sea". The shared balance pays (D06: ownership is
        // campaign-wide, never a player's personal inventory item), so a purchase can neither duplicate into a loadout nor be
        // bought twice. Mehmet's hull/seat/movement and Utku's routes only READ this (ActiveVehicle / Fleet).

        private bool RowboatOwned => _boatParts.Count >= BoatRepairParts.All.Count;

        private bool OwnsVehicle(string boatId) =>
            string.Equals(boatId, VehicleIds.Rowboat, StringComparison.Ordinal) ? RowboatOwned : _purchasedVehicles.Contains(boatId);

        // The stored selection when it is still owned, otherwise the rowboat (when repaired), otherwise nothing.
        public string ActiveVehicleId =>
            !string.IsNullOrEmpty(_activeVehicleId) && OwnsVehicle(_activeVehicleId) ? _activeVehicleId
            : RowboatOwned ? VehicleIds.Rowboat : string.Empty;

        public VehicleFleetState Fleet
        {
            get
            {
                var owned = new List<string>(3);
                for (var i = 0; i < VehicleIds.All.Count; i++)
                    if (OwnsVehicle(VehicleIds.All[i])) owned.Add(VehicleIds.All[i]);
                return new VehicleFleetState(owned, ActiveVehicleId, Revision);
            }
        }

        public TransactionResult TryPurchaseVehicle(PlayerId player, string boatId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId, OpVehicleBuy);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            TransactionResult result;
            if (!VehicleCatalog.TryGet(boatId, out var definition) || !definition.IsForSale)
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (OwnsVehicle(boatId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else if (definition.RequiresBoatId.Length > 0 && !OwnsVehicle(definition.RequiresBoatId))
                result = TransactionResult.Reject(requestId, "RequirementMissing", Revision);
            else if (SharedBalance < definition.Price)
                result = TransactionResult.Reject(requestId, "InsufficientFunds", Revision);
            else
            {
                var previousBalance = SharedBalance;
                var previousRevision = Revision;
                SharedBalance -= definition.Price;
                _purchasedVehicles.Add(boatId);
                Revision++;
                if (!Persist())
                {
                    _purchasedVehicles.Remove(boatId);
                    SharedBalance = previousBalance;
                    Revision = previousRevision;
                    return TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                result = TransactionResult.Ok(requestId, Revision);
                OnSpent?.Invoke("vehicle-" + boatId, definition.Price);
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted)
            {
                OnBalanceChanged?.Invoke();
                OnFleetChanged?.Invoke();
            }
            return result;
        }

        // Switching the active vehicle: only an owned one, never to the one already active, and only while the trip
        // authority says nothing is under way and nobody is seated (VehicleSwitchGate). The gate refusals can change on
        // their own (the boat docks), so - like DayClosing - they are not cached against the request id.
        public TransactionResult TrySelectVehicle(PlayerId player, string boatId, ulong requestId)
        {
            EnsureSubscribed();
            var requestKey = (player, requestId, OpVehicleSelect);
            if (_processedRequests.TryGetValue(requestKey, out var replayed)) return replayed;
            if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", Revision);

            TransactionResult result;
            if (!VehicleIds.IsVehicle(boatId))
                result = TransactionResult.Reject(requestId, "InvalidTarget", Revision);
            else if (!OwnsVehicle(boatId))
                result = TransactionResult.Reject(requestId, "NotOwned", Revision);
            else if (string.Equals(ActiveVehicleId, boatId, StringComparison.Ordinal))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", Revision);
            else
            {
                var blocked = VehicleSwitchGate.Check();
                if (blocked != null) return TransactionResult.Reject(requestId, blocked, Revision);

                var previousActive = _activeVehicleId;
                var previousRevision = Revision;
                _activeVehicleId = boatId;
                Revision++;
                if (!Persist())
                {
                    _activeVehicleId = previousActive;
                    Revision = previousRevision;
                    return TransactionResult.Reject(requestId, "SaveFailed", Revision);
                }
                result = TransactionResult.Ok(requestId, Revision);
            }

            _processedRequests[requestKey] = result;
            if (result.Accepted) OnFleetChanged?.Invoke();
            return result;
        }

        // A file can only restore what the rules would have allowed: known purchasable ids, once each, whose requirement
        // chain is owned (the rowboat through the repair parts restored just before). The active id must be an owned one.
        private void RestoreFleet(EconomySaveData data)
        {
            _purchasedVehicles.Clear();
            _activeVehicleId = "";
            if (!data.HasFleet) return;
            for (var i = 0; i < VehicleCatalog.All.Count; i++)
            {
                var definition = VehicleCatalog.All[i];
                if (!definition.IsForSale || data.FleetPurchasedBoatIds == null || !data.FleetPurchasedBoatIds.Contains(definition.BoatId)) continue;
                if (definition.RequiresBoatId.Length > 0 && !OwnsVehicle(definition.RequiresBoatId)) continue;
                _purchasedVehicles.Add(definition.BoatId);
            }
            if (!string.IsNullOrEmpty(data.FleetActiveBoatId) && OwnsVehicle(data.FleetActiveBoatId)) _activeVehicleId = data.FleetActiveBoatId;
        }

        // ---- Save / load -------------------------------------------------------------------

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
                PaidRecordingIds = new List<string>(_paidRecordingIds),
                BoatPartIds = new List<string>(_boatParts),
                ChannelRightIds = new List<string>(_channelRightIds),
                ChannelSettleIds = new List<string>(_channelSettleIds),
                HasFleet = true,
                FleetPurchasedBoatIds = new List<string>(_purchasedVehicles),
                FleetActiveBoatId = _activeVehicleId ?? string.Empty
            };

            foreach (var pair in _loadout)
            {
                data.Loadouts.Add(new EconomyLoadoutSave
                {
                    PlayerId = pair.Key.Value,
                    EquipmentIds = new List<string>(pair.Value)
                });
            }

            foreach (var item in _stored)
            {
                data.StoredItems.Add(new PendingTurnInSave
                {
                    ItemId = item.ItemId,
                    Kind = (byte)item.Kind,
                    SourceDiveId = item.DiveId,
                    SubjectId = item.SubjectId,
                    WeightGrams = item.WeightGrams,
                    Quality = item.Quality,
                    ValidDurationSeconds = item.ValidDurationSeconds,
                    CarrierPlayerId = 0,
                    SharedEscrow = true,
                    Revision = item.Revision
                });
            }

            foreach (var item in _pending)
            {
                // Only the host has a persistent id (D06); anyone else's item becomes shared escrow.
                var hostCarried = !item.Shared && item.Carrier.Value == 0;
                data.PendingTurnIns.Add(new PendingTurnInSave
                {
                    ItemId = item.ItemId,
                    Kind = (byte)item.Kind,
                    SourceDiveId = item.DiveId,
                    SubjectId = item.SubjectId,
                    WeightGrams = item.WeightGrams,
                    Quality = item.Quality,
                    ValidDurationSeconds = item.ValidDurationSeconds,
                    CarrierPlayerId = 0,
                    SharedEscrow = !hostCarried,
                    Revision = item.Revision
                });
            }
            return data;
        }

        public bool TryRestore(EconomySaveData data)
        {
            ConfigureDefaults();
            if (data == null || data.SchemaVersion < EconomySaveData.OldestSupportedSchemaVersion ||
                data.SchemaVersion > EconomySaveData.CurrentSchemaVersion || data.SharedBalance < 0)
                return false;

            SharedBalance = data.SharedBalance;
            Revision = Math.Max(0, data.Revision);
            LastCheckpointId = data.CheckpointId ?? "";
            _soldCaptureIds.Clear();
            _paidRecordingIds.Clear();
            _channelRightIds.Clear();
            _channelSettleIds.Clear();
            _withdrawnForChannel.Clear();
            _loadout.Clear();
            _pending.Clear();
            _stored.Clear();
            _boatParts.Clear();
            _processedRequests.Clear();
            _processedTurnIns.Clear();

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

            if (data.ChannelRightIds != null)
                foreach (var id in data.ChannelRightIds)
                    if (!string.IsNullOrWhiteSpace(id)) _channelRightIds.Add(id);
            if (data.ChannelSettleIds != null)
                foreach (var id in data.ChannelSettleIds)
                    if (!string.IsNullOrWhiteSpace(id)) _channelSettleIds.Add(id);
            RestorePending(data.PendingTurnIns);
            RestoreStored(data.StoredItems);
            if (data.BoatPartIds != null)
                foreach (var partId in data.BoatPartIds)
                    if (BoatRepairParts.IsPart(partId) && !_boatParts.Contains(partId)) _boatParts.Add(partId);
            RestoreFleet(data);

            OnBalanceChanged?.Invoke();
            OnPendingChanged?.Invoke();
            OnStorageChanged?.Invoke();
            OnBoatRepairChanged?.Invoke();
            OnFleetChanged?.Invoke();
            foreach (var player in _loadout.Keys) OnLoadoutChanged?.Invoke(player);
            return true;
        }

        // A stored catch that an edited/old file also lists as pending or already paid is dropped: an item is
        // never in two places, and a paid id never comes back.
        private void RestoreStored(List<PendingTurnInSave> saved)
        {
            if (saved == null) return;
            foreach (var entry in saved)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId) || (TurnInKind)entry.Kind != TurnInKind.Catch) continue;
                if (_soldCaptureIds.Contains(entry.ItemId) || FindPending(TurnInKind.Catch, entry.ItemId) != null) continue;
                var duplicate = false;
                foreach (var stored in _stored) if (stored.ItemId == entry.ItemId) duplicate = true;
                if (duplicate || _stored.Count >= StorageCapacityItems) continue;

                _stored.Add(new PendingItem
                {
                    ItemId = entry.ItemId,
                    Kind = TurnInKind.Catch,
                    DiveId = entry.SourceDiveId ?? "",
                    SubjectId = entry.SubjectId ?? "",
                    WeightGrams = Math.Max(0, entry.WeightGrams),
                    Quality = entry.Quality,
                    ValidDurationSeconds = entry.ValidDurationSeconds,
                    Carrier = default,
                    Shared = true,
                    Revision = entry.Revision
                });
            }
        }

        private void RestorePending(List<PendingTurnInSave> saved)
        {
            if (saved == null) return;
            foreach (var entry in saved)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId)) continue;
                var kind = (TurnInKind)entry.Kind;
                if (kind != TurnInKind.Catch && kind != TurnInKind.Recording) continue;
                // A paid id can never come back as pending, whatever an edited/old file says.
                var paid = kind == TurnInKind.Catch ? _soldCaptureIds : _paidRecordingIds;
                if (paid.Contains(entry.ItemId) || FindPending(kind, entry.ItemId) != null) continue;

                _pending.Add(new PendingItem
                {
                    ItemId = entry.ItemId,
                    Kind = kind,
                    DiveId = entry.SourceDiveId ?? "",
                    SubjectId = entry.SubjectId ?? "",
                    WeightGrams = Math.Max(0, entry.WeightGrams),
                    Quality = entry.Quality,
                    ValidDurationSeconds = entry.ValidDurationSeconds,
                    Carrier = new PlayerId(entry.CarrierPlayerId),
                    Shared = entry.SharedEscrow,
                    Revision = entry.Revision
                });
            }
        }
    }
}
