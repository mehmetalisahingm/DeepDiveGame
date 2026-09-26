using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.1 (#90): shared campaign day. docs/plan/CONTRACTS.md "CampaignDayState" / "DaySummary" and
    // "Gun sonu ve uyku islemi". UnityEngine-free like every other Core contract, so Mehmet's bed
    // interaction, Utku's discovery data and the save layer can all name these shapes without
    // depending on the day authority (DeepDive.Day.DayEngine).
    public enum DayPhase
    {
        Running,
        Closing,
        Summary,
        Morning
    }

    public enum DayCloseReason
    {
        EarlySleep,
        Midnight
    }

    public static class DayIds
    {
        // Working values from WORLD_SYSTEMS "Ortak saat, uyku ve 00:00": the day starts 08:00 and
        // closes 00:00. Minutes are minutes-since-midnight; 1440 is the 00:00 that ends the day.
        public const int DayStartMinute = 8 * 60;
        public const int DayEndMinute = 24 * 60;
        public const int FirstWarningMinute = 22 * 60;
        public const int SecondWarningMinute = 23 * 60;

        public const string Bed0 = "bed-0";
        public const string Bed1 = "bed-1";
        public const string Bed2 = "bed-2";
        public const string Bed3 = "bed-3";
        public const int BedCount = 4;

        public static readonly IReadOnlyList<string> Beds = new[] { Bed0, Bed1, Bed2, Bed3 };

        public static bool IsBed(string bedId)
        {
            for (var i = 0; i < Beds.Count; i++)
                if (string.Equals(Beds[i], bedId, StringComparison.Ordinal)) return true;
            return false;
        }

        public static string DayId(int dayNumber) => "day-" + dayNumber;

        // One close per day, so the id is a pure function of the day: a retried or replayed close for
        // the same day always names the same id and can never be mistaken for a second close.
        public static string CloseId(int dayNumber) => "close-day-" + dayNumber;
    }

    // Read-only view every client renders ("istemciler host saatini gosterir").
    public readonly struct CampaignDayState
    {
        public readonly int DayNumber;
        public readonly string DayId;
        public readonly int ClockMinute;
        public readonly DayPhase Phase;
        public readonly IReadOnlyList<PlayerId> SleepingPlayers;
        public readonly IReadOnlyList<PlayerId> ActivePlayers;
        public readonly int WeatherSeed;
        public readonly int Revision;

        public CampaignDayState(int dayNumber, string dayId, int clockMinute, DayPhase phase,
            IReadOnlyList<PlayerId> sleepingPlayers, IReadOnlyList<PlayerId> activePlayers, int weatherSeed, int revision)
        {
            DayNumber = dayNumber;
            DayId = dayId ?? string.Empty;
            ClockMinute = clockMinute;
            Phase = phase;
            SleepingPlayers = sleepingPlayers ?? Array.Empty<PlayerId>();
            ActivePlayers = activePlayers ?? Array.Empty<PlayerId>();
            WeatherSeed = weatherSeed;
            Revision = revision;
        }
    }

    // The one document a day close produces. Once written it is never rebuilt: a replay of the same
    // close returns this exact record.
    [Serializable]
    public sealed class DaySummary
    {
        public int DayNumber;
        public string DayId = "";
        public string CloseId = "";
        public DayCloseReason Reason;
        public int FishSold;
        public int FishSoldWeightGrams;
        public int Income;
        public int Expenses;
        public List<string> DiscoveredSpeciesIds = new List<string>();
        public int LostDivers;
        public List<string> LostCaptureIds = new List<string>();
        public List<string> ProgressIds = new List<string>();

        public int Net => Income - Expenses;
    }

    // What "dalis sonuclarini kapat" returns to the day authority. The loss rule itself is D07 and
    // the existing inventory/recording rules; the day only records the outcome.
    public readonly struct DayDiveClosure
    {
        public static readonly DayDiveClosure None = new DayDiveClosure(0, Array.Empty<string>());

        public readonly int LostDivers;
        public readonly IReadOnlyList<string> LostCaptureIds;

        public DayDiveClosure(int lostDivers, IReadOnlyList<string> lostCaptureIds)
        {
            LostDivers = lostDivers;
            LostCaptureIds = lostCaptureIds ?? Array.Empty<string>();
        }
    }

    // Steps 1, 2, 5 and 6 of the fixed close order (WORLD_SYSTEMS): accepted actions -> dive results
    // -> [summary, built by the day] -> next-day channel results (P4.2, none yet) -> atomic write ->
    // morning. The implementation lives in Composition, where the economy/inventory/save/player
    // authorities are reachable; the day authority itself stays free of all of them.
    public interface IDayCloseHooks
    {
        // Let trade/travel that the host already accepted finish. New ones are refused meanwhile.
        void SettleAcceptedActions(string closeId);

        // Close every still-open dive exactly once (D07): safe catches kept, the rest lost.
        DayDiveClosure CloseOpenDives(string closeId);

        // One atomic write of the next day, the summary and the close id. False = nothing was
        // written; the day then stays in Summary and retries with the SAME close id.
        bool Persist();

        // Players wake at home, the active boat is back at the dock (Mehmet's placement).
        void BeginMorning(int dayNumber);
    }

    // Who counts for the sleep gate: connected AND active. Dead/passive-in-dive and disconnected
    // players are simply not in this list, which is how "kopan/pasif oyuncu kilitlemez" is enforced.
    public interface IDayRoster
    {
        IReadOnlyList<PlayerId> ActivePlayers();
    }

    // Host-side read of "may a new trade/publish/travel request start". Bound by the day authority
    // while it is the host; unbound (no day authority, e.g. older tests) means open.
    public static class DayLock
    {
        public static Func<bool> IsLockedProvider { get; private set; }

        public static bool IsLocked => IsLockedProvider != null && IsLockedProvider();

        // The current day and last summary, readable by anything that spawns late (a respawned player's
        // mirror must start at today's values, not at the defaults). Null while there is no day authority.
        public static Func<CampaignDayState> StateProvider { get; private set; }
        public static Func<DaySummary> SummaryProvider { get; private set; }

        public static void BindState(Func<CampaignDayState> state, Func<DaySummary> summary)
        {
            StateProvider = state;
            SummaryProvider = summary;
        }

        public static void UnbindState(Func<CampaignDayState> state)
        {
            if (StateProvider != state) return;
            StateProvider = null;
            SummaryProvider = null;
        }

        public static void Bind(Func<bool> provider) => IsLockedProvider = provider;

        public static void Unbind(Func<bool> provider)
        {
            if (IsLockedProvider == provider) IsLockedProvider = null;
        }
    }

    // Mehmet's physical bed interaction (#88) validates the real player/bed/proximity/active state and then
    // calls this; the day authority binds the handlers while it is the host. Unbound means "no day authority
    // here" and every request is refused - nothing can put a player to sleep without the host day.
    public static class HomeBedInteraction
    {
        public static Func<PlayerId, string, ulong, TransactionResult> EnterHandler { get; private set; }
        public static Func<PlayerId, ulong, TransactionResult> LeaveHandler { get; private set; }

        public static void Bind(Func<PlayerId, string, ulong, TransactionResult> enter, Func<PlayerId, ulong, TransactionResult> leave)
        {
            EnterHandler = enter;
            LeaveHandler = leave;
        }

        public static void Unbind(Func<PlayerId, string, ulong, TransactionResult> enter, Func<PlayerId, ulong, TransactionResult> leave)
        {
            if (EnterHandler == enter) EnterHandler = null;
            if (LeaveHandler == leave) LeaveHandler = null;
        }

        public static TransactionResult TryEnterBed(PlayerId player, string bedId, ulong requestId) =>
            EnterHandler != null ? EnterHandler(player, bedId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TryLeaveBed(PlayerId player, ulong requestId) =>
            LeaveHandler != null ? LeaveHandler(player, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    // The item moves of the shared home storage (store a safe catch / take one back out). Mehmet's physical
    // layer (HomeStorageInteraction.TryOpen, P4HomeInteractionContracts) decides whether a player may use the
    // storage at all; these are the transactions once the host has verified the player is at it. The
    // composition root binds them to EconomyManager (the one authority over pending items and the storage)
    // while it is the host. Unbound means "no storage authority here": every request is refused.
    public static class HomeStorageItems
    {
        public static Func<PlayerId, string, ulong, TransactionResult> StoreHandler { get; private set; }
        public static Func<PlayerId, string, ulong, TransactionResult> RetrieveHandler { get; private set; }

        public static void Bind(Func<PlayerId, string, ulong, TransactionResult> store, Func<PlayerId, string, ulong, TransactionResult> retrieve)
        {
            StoreHandler = store;
            RetrieveHandler = retrieve;
        }

        public static void Unbind(Func<PlayerId, string, ulong, TransactionResult> store, Func<PlayerId, string, ulong, TransactionResult> retrieve)
        {
            if (StoreHandler == store) StoreHandler = null;
            if (RetrieveHandler == retrieve) RetrieveHandler = null;
        }

        public static TransactionResult TryStore(PlayerId player, string itemId, ulong requestId) =>
            StoreHandler != null ? StoreHandler(player, itemId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TryRetrieve(PlayerId player, string itemId, ulong requestId) =>
            RetrieveHandler != null ? RetrieveHandler(player, itemId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    // ---- save shapes (plain Serializable so JsonUtility writes them; nothing here is a scene type) ----

    [Serializable]
    public sealed class DayLedgerSave
    {
        public int FishSold;
        public int FishSoldWeightGrams;
        public int Income;
        public int Expenses;
        public List<string> DiscoveredSpeciesIds = new List<string>();
        public List<string> ProgressIds = new List<string>();
        // Ids of the sales/expenses already counted today, so a replay after a reload cannot count twice.
        public List<string> EventIds = new List<string>();
    }

    [Serializable]
    public sealed class DaySaveData
    {
        public int DayNumber = 1;
        public int ClockMinute = DayIds.DayStartMinute;
        public List<string> ClosedCloseIds = new List<string>();
        public List<DaySummary> SummaryHistory = new List<DaySummary>();
        public DayLedgerSave Ledger = new DayLedgerSave();
    }

    // Implemented by the day authority; the campaign save store composes it into its one file so the
    // day, the money and the boat are written atomically together (nothing ever advances alone).
    public interface IDayPersistence
    {
        DaySaveData ExportDay();
        bool RestoreDay(DaySaveData data);
    }
}
