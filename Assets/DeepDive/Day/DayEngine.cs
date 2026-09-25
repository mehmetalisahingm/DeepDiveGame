using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Day
{
    // Host-only campaign day authority (#90, docs/plan/CONTRACTS.md "Gun sonu ve uyku islemi").
    // Pure C#: no scene, no clock source of its own. The caller feeds it host time through Tick, so
    // "istemcinin bilgisayar saati odul veya gun ilerlemesi kaynagi degildir" holds by construction
    // and the whole close pipeline is testable without a Unity object.
    //
    // Who owns what: this class owns the day number, the clock, who is in bed, and the fixed close
    // order. Mehmet's layer validates that a bed request is real (player, bed, proximity) and calls
    // TryEnterBed/TryLeaveBed; the economy/inventory/save/placement work is reached only through
    // IDayCloseHooks, so nothing here reaches into another owner's state.
    public sealed class DayEngine : IDayPersistence
    {
        public const int MaxSummaryHistory = 30;
        private const byte OpEnterBed = 1;
        private const byte OpLeaveBed = 2;

        // Working default: a full 16 h day in 20 real minutes. WORLD_SYSTEMS says the ratio is tuned in
        // playtests, so it is a settable field, not a contract.
        public float GameMinutesPerRealSecond { get; set; } = (DayIds.DayEndMinute - DayIds.DayStartMinute) / (20f * 60f);

        public event Action OnStateChanged;
        public event Action<int> OnWarning;
        public event Action<DaySummary> OnSummaryReady;

        private readonly Dictionary<string, PlayerId> _beds = new Dictionary<string, PlayerId>();
        private readonly Dictionary<PlayerId, string> _bedOf = new Dictionary<PlayerId, string>();
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> _processed =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();
        private readonly HashSet<string> _closedCloseIds = new HashSet<string>();
        private readonly List<DaySummary> _history = new List<DaySummary>();
        private readonly HashSet<int> _warned = new HashSet<int>();
        private readonly HashSet<string> _ledgerEvents = new HashSet<string>();
        private readonly HashSet<string> _discovered = new HashSet<string>();
        private readonly HashSet<string> _progress = new HashSet<string>();

        private IDayRoster _roster;
        private IDayCloseHooks _hooks;
        private int _dayNumber = 1;
        private double _clock = DayIds.DayStartMinute;
        private DayPhase _phase = DayPhase.Running;
        private int _revision;
        private int _fishSold, _fishWeight, _income, _expenses;
        private DaySummary _pendingSummary;
        private DayCloseReason _pendingReason;
        private DayDiveClosure _pendingDives;
        private bool _divesClosed;
        private bool _actionsSettled;
        private int _campaignSeed;

        public int DayNumber => _dayNumber;
        public DayPhase Phase => _phase;
        public int ClockMinute => (int)_clock;
        public int Revision => _revision;
        public bool IsLocked => _phase != DayPhase.Running;
        public DaySummary LastSummary => _history.Count > 0 ? _history[_history.Count - 1] : null;
        public IReadOnlyList<DaySummary> History => _history;
        public bool HasPendingClose => _pendingSummary != null;

        // Stable per (campaign, day): the same day always rolls the same weather/event seed, on every
        // host restart. Not derived from wall-clock time.
        public int WeatherSeed => Mix(_campaignSeed, _dayNumber);

        public CampaignDayState State => new CampaignDayState(
            _dayNumber, DayIds.DayId(_dayNumber), ClockMinute, _phase, SnapshotSleepers(), SnapshotActive(),
            WeatherSeed, _revision);

        public void Configure(IDayRoster roster, IDayCloseHooks hooks, int campaignSeed = 0)
        {
            _roster = roster;
            _hooks = hooks;
            _campaignSeed = campaignSeed;
        }

        // ---- clock -----------------------------------------------------------------------------

        // Host time only. Nothing advances while no active player is connected ("Butun oyuncular
        // cikmisken gun ilerlemez"), nor once the day is already closing.
        public void Tick(float realSeconds)
        {
            if (_phase != DayPhase.Running || realSeconds <= 0f) return;
            if (ActiveNow().Count == 0) return;

            _clock += realSeconds * GameMinutesPerRealSecond;
            WarnIfDue();
            if (_clock >= DayIds.DayEndMinute)
            {
                _clock = DayIds.DayEndMinute;
                BeginClose(DayCloseReason.Midnight);
            }
        }

        private void WarnIfDue()
        {
            Warn(DayIds.FirstWarningMinute);
            Warn(DayIds.SecondWarningMinute);
        }

        private void Warn(int minute)
        {
            if (_clock < minute || !_warned.Add(minute)) return;
            OnWarning?.Invoke(minute);
            Changed();
        }

        // ---- sleep -----------------------------------------------------------------------------

        // Mehmet validated proximity/real bed; this is the state rule: phase, active, one bed per
        // player, one player per bed. Same (player, requestId) replays the first answer.
        public TransactionResult TryEnterBed(PlayerId player, string bedId, ulong requestId)
        {
            if (_processed.TryGetValue((player, requestId, OpEnterBed), out var replay)) return replay;
            var result = EnterBed(player, bedId, requestId);
            _processed[(player, requestId, OpEnterBed)] = result;
            return result;
        }

        private TransactionResult EnterBed(PlayerId player, string bedId, ulong requestId)
        {
            if (requestId == 0) return TransactionResult.Reject(requestId, "InvalidState", _revision);
            if (_phase != DayPhase.Running) return TransactionResult.Reject(requestId, "DayClosing", _revision);
            if (!DayIds.IsBed(bedId)) return TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            if (!IsActive(player)) return TransactionResult.Reject(requestId, "NotActive", _revision);
            if (_bedOf.TryGetValue(player, out var current))
                return string.Equals(current, bedId, StringComparison.Ordinal)
                    ? TransactionResult.Ok(requestId, _revision)
                    : TransactionResult.Reject(requestId, "AlreadyInBed", _revision);
            if (_beds.ContainsKey(bedId)) return TransactionResult.Reject(requestId, "BedTaken", _revision);

            _beds[bedId] = player;
            _bedOf[player] = bedId;
            Changed();
            EvaluateEarlyClose();
            return TransactionResult.Ok(requestId, _revision);
        }

        // Getting up before the close starts cancels readiness; once the day is closing the close is
        // already committed and nobody can back out of it.
        public TransactionResult TryLeaveBed(PlayerId player, ulong requestId)
        {
            if (_processed.TryGetValue((player, requestId, OpLeaveBed), out var replay)) return replay;
            TransactionResult result;
            if (requestId == 0) result = TransactionResult.Reject(requestId, "InvalidState", _revision);
            else if (_phase != DayPhase.Running) result = TransactionResult.Reject(requestId, "DayClosing", _revision);
            else if (!_bedOf.TryGetValue(player, out var bed)) result = TransactionResult.Reject(requestId, "NotInBed", _revision);
            else
            {
                _bedOf.Remove(player);
                _beds.Remove(bed);
                Changed();
                result = TransactionResult.Ok(requestId, _revision);
            }
            _processed[(player, requestId, OpLeaveBed)] = result;
            return result;
        }

        // Called by the roster owner whenever someone connects, disconnects or turns passive/active.
        // A departed sleeper frees the bed; a departed non-sleeper may be exactly what was holding
        // the gate shut.
        public void NotifyRosterChanged()
        {
            if (_phase != DayPhase.Running) return;
            var active = new HashSet<PlayerId>(ActiveNow());
            var gone = new List<PlayerId>();
            foreach (var pair in _bedOf)
                if (!active.Contains(pair.Key)) gone.Add(pair.Key);
            for (var i = 0; i < gone.Count; i++)
            {
                _beds.Remove(_bedOf[gone[i]]);
                _bedOf.Remove(gone[i]);
            }
            if (gone.Count > 0) Changed();
            EvaluateEarlyClose();
        }

        private void EvaluateEarlyClose()
        {
            if (_phase != DayPhase.Running) return;
            var active = ActiveNow();
            if (active.Count == 0) return;   // no one to sleep: a day is never produced for an empty room
            for (var i = 0; i < active.Count; i++)
                if (!_bedOf.ContainsKey(active[i])) return;
            BeginClose(DayCloseReason.EarlySleep);
        }

        // ---- ledger ----------------------------------------------------------------------------
        // What the summary reports. Every entry is keyed by the event's own id, so a replayed sale or
        // discovery cannot count twice. Only the running day's numbers; the close resets them.

        public bool RecordSale(string saleId, int fishCount, int weightGrams, int income)
        {
            if (string.IsNullOrEmpty(saleId) || !_ledgerEvents.Add("sale:" + saleId)) return false;
            _fishSold += Math.Max(0, fishCount);
            _fishWeight += Math.Max(0, weightGrams);
            _income += Math.Max(0, income);
            Changed();
            return true;
        }

        public bool RecordExpense(string expenseId, int amount)
        {
            if (string.IsNullOrEmpty(expenseId) || !_ledgerEvents.Add("expense:" + expenseId)) return false;
            _expenses += Math.Max(0, amount);
            Changed();
            return true;
        }

        public bool RecordDiscovery(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId) || !_discovered.Add(speciesId)) return false;
            Changed();
            return true;
        }

        public bool RecordProgress(string progressId)
        {
            if (string.IsNullOrEmpty(progressId) || !_progress.Add(progressId)) return false;
            Changed();
            return true;
        }

        // ---- close -----------------------------------------------------------------------------

        // The one entry to a close. A close that has started (or finished) for this day is never
        // started again: the second caller just gets the same close id back. A request that names a
        // close id other than TODAY's is a replay of an earlier day's close (a delayed message, a
        // duplicated trigger, a re-sent request after a reload) and does nothing at all - it must never
        // close the day that is running now.
        public string BeginClose(DayCloseReason reason, string requestedCloseId = null)
        {
            var closeId = DayIds.CloseId(_dayNumber);
            if (requestedCloseId != null && !string.Equals(requestedCloseId, closeId, StringComparison.Ordinal))
                return requestedCloseId;
            if (_phase != DayPhase.Running || _closedCloseIds.Contains(closeId)) return closeId;

            _phase = DayPhase.Closing;
            _pendingReason = reason;
            Changed();
            RunClose(closeId);
            return closeId;
        }

        private void RunClose(string closeId)
        {
            // 1. actions the host already accepted finish; new ones are refused (IsLocked / DayLock).
            if (!_actionsSettled)
            {
                _hooks?.SettleAcceptedActions(closeId);
                _actionsSettled = true;
            }

            // 2. open dives close exactly once.
            if (!_divesClosed)
            {
                _pendingDives = _hooks != null ? _hooks.CloseOpenDives(closeId) : DayDiveClosure.None;
                _divesClosed = true;
            }

            // 3. the summary is built once and then only ever replayed.
            if (_pendingSummary == null) _pendingSummary = BuildSummary(closeId);

            _phase = DayPhase.Summary;
            Changed();
            TryPersistAndAdvance(closeId);
        }

        // "Yazma basarisizsa ertesi sabah basari gibi acilmaz; ayni closeId ile tekrar denenir."
        // Safe to call repeatedly: it only re-attempts the write, it never re-runs steps 1-3.
        public bool RetryClose()
        {
            if (_phase != DayPhase.Summary || _pendingSummary == null) return false;
            return TryPersistAndAdvance(_pendingSummary.CloseId);
        }

        private bool TryPersistAndAdvance(string closeId)
        {
            // From here the exported save already names the NEXT day, the summary and this close id, so
            // the single write below is the atomic advance (see ExportDay).
            var written = _hooks == null || _hooks.Persist();
            if (!written) return false;

            _closedCloseIds.Add(closeId);
            _history.Add(_pendingSummary);
            while (_history.Count > MaxSummaryHistory) _history.RemoveAt(0);
            var summary = _pendingSummary;

            _dayNumber++;
            _clock = DayIds.DayStartMinute;
            _phase = DayPhase.Morning;
            _beds.Clear();
            _bedOf.Clear();
            _warned.Clear();
            ClearLedger();
            _pendingSummary = null;
            _divesClosed = false;
            _actionsSettled = false;
            _pendingDives = DayDiveClosure.None;
            Changed();

            OnSummaryReady?.Invoke(summary);
            _hooks?.BeginMorning(_dayNumber);
            return true;
        }

        // Morning is the moment players are placed at home; the day only starts running after that.
        public void CompleteMorning()
        {
            if (_phase != DayPhase.Morning) return;
            _phase = DayPhase.Running;
            Changed();
        }

        private DaySummary BuildSummary(string closeId)
        {
            var summary = new DaySummary
            {
                DayNumber = _dayNumber,
                DayId = DayIds.DayId(_dayNumber),
                CloseId = closeId,
                Reason = _pendingReason,
                FishSold = _fishSold,
                FishSoldWeightGrams = _fishWeight,
                Income = _income,
                Expenses = _expenses,
                LostDivers = _pendingDives.LostDivers
            };
            summary.DiscoveredSpeciesIds.AddRange(_discovered);
            summary.LostCaptureIds.AddRange(_pendingDives.LostCaptureIds);
            summary.ProgressIds.AddRange(_progress);
            return summary;
        }

        private void ClearLedger()
        {
            _fishSold = _fishWeight = _income = _expenses = 0;
            _ledgerEvents.Clear();
            _discovered.Clear();
            _progress.Clear();
        }

        // ---- persistence -----------------------------------------------------------------------

        // While a close is being written the export already describes the day AFTER it (next number,
        // 08:00, summary in history, close id closed, empty ledger). Any write that lands in that
        // window - the close's own or a coincident economy save - is therefore the atomic advance and
        // a reload can only ever see "before" or "after", never a day advanced twice or not at all.
        public DaySaveData ExportDay()
        {
            var data = new DaySaveData();
            var closing = _phase == DayPhase.Summary && _pendingSummary != null;
            if (closing)
            {
                data.DayNumber = _dayNumber + 1;
                data.ClockMinute = DayIds.DayStartMinute;
            }
            else
            {
                data.DayNumber = _dayNumber;
                data.ClockMinute = Math.Min(ClockMinute, DayIds.DayEndMinute - 1);
                data.Ledger.FishSold = _fishSold;
                data.Ledger.FishSoldWeightGrams = _fishWeight;
                data.Ledger.Income = _income;
                data.Ledger.Expenses = _expenses;
                data.Ledger.DiscoveredSpeciesIds.AddRange(_discovered);
                data.Ledger.ProgressIds.AddRange(_progress);
                data.Ledger.EventIds.AddRange(_ledgerEvents);
            }

            data.ClosedCloseIds.AddRange(_closedCloseIds);
            var history = new List<DaySummary>(_history);
            if (closing)
            {
                data.ClosedCloseIds.Add(_pendingSummary.CloseId);
                history.Add(_pendingSummary);
            }
            while (history.Count > MaxSummaryHistory) history.RemoveAt(0);
            data.SummaryHistory.AddRange(history);
            return data;
        }

        // A reloaded campaign is always a Running day at its saved checkpoint. A day that was closing
        // when the host died was never written, so it simply resumes at the last durable state.
        public bool RestoreDay(DaySaveData data)
        {
            if (data == null || data.DayNumber < 1 || data.ClockMinute < DayIds.DayStartMinute ||
                data.ClockMinute >= DayIds.DayEndMinute)
                return false;

            _dayNumber = data.DayNumber;
            _clock = data.ClockMinute;
            _phase = DayPhase.Running;
            _beds.Clear();
            _bedOf.Clear();
            _processed.Clear();
            _warned.Clear();
            _pendingSummary = null;
            _divesClosed = false;
            _actionsSettled = false;
            _pendingDives = DayDiveClosure.None;
            _closedCloseIds.Clear();
            _history.Clear();
            ClearLedger();

            if (data.ClosedCloseIds != null)
                foreach (var id in data.ClosedCloseIds)
                    if (!string.IsNullOrEmpty(id)) _closedCloseIds.Add(id);
            if (data.SummaryHistory != null)
                foreach (var summary in data.SummaryHistory)
                    if (summary != null) _history.Add(summary);
            var ledger = data.Ledger;
            if (ledger != null)
            {
                _fishSold = Math.Max(0, ledger.FishSold);
                _fishWeight = Math.Max(0, ledger.FishSoldWeightGrams);
                _income = Math.Max(0, ledger.Income);
                _expenses = Math.Max(0, ledger.Expenses);
                if (ledger.DiscoveredSpeciesIds != null)
                    foreach (var id in ledger.DiscoveredSpeciesIds) if (!string.IsNullOrEmpty(id)) _discovered.Add(id);
                if (ledger.ProgressIds != null)
                    foreach (var id in ledger.ProgressIds) if (!string.IsNullOrEmpty(id)) _progress.Add(id);
                if (ledger.EventIds != null)
                    foreach (var id in ledger.EventIds) if (!string.IsNullOrEmpty(id)) _ledgerEvents.Add(id);
            }

            // Warnings already passed at the saved minute are not re-announced after a reload.
            if (_clock >= DayIds.FirstWarningMinute) _warned.Add(DayIds.FirstWarningMinute);
            if (_clock >= DayIds.SecondWarningMinute) _warned.Add(DayIds.SecondWarningMinute);
            Changed();
            return true;
        }

        // ---- helpers ---------------------------------------------------------------------------

        private IReadOnlyList<PlayerId> ActiveNow() => _roster != null ? _roster.ActivePlayers() : Array.Empty<PlayerId>();

        private bool IsActive(PlayerId player)
        {
            var active = ActiveNow();
            for (var i = 0; i < active.Count; i++)
                if (active[i].Equals(player)) return true;
            return false;
        }

        private IReadOnlyList<PlayerId> SnapshotSleepers()
        {
            var list = new List<PlayerId>(_bedOf.Keys);
            list.Sort((a, b) => a.Value.CompareTo(b.Value));
            return list;
        }

        private IReadOnlyList<PlayerId> SnapshotActive() => new List<PlayerId>(ActiveNow());

        private void Changed()
        {
            _revision++;
            OnStateChanged?.Invoke();
        }

        private static int Mix(int a, int b)
        {
            unchecked
            {
                var h = (uint)a * 2654435761u;
                h ^= (uint)b + 0x9E3779B9u + (h << 6) + (h >> 2);
                return (int)h;
            }
        }
    }
}
