using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.4-C (#123): the deep-exploration progression chain  encyclopedia -> rumor -> trace -> discovery -> boss unlock.
    // ONE source of truth (the host's DeepProgressionAuthority); everything else reads it or feeds it evidence through the
    // host seams below. Nothing here spawns a boss, places a trace point or decides what the world accepts: the boss
    // lifecycle is Mehmet's (#121), the trace/arena world data and its validation are Utku's (#122).
    //
    // Stage meaning (a stage is REACHED when its evidence was accepted; the chain cannot skip a step):
    //   Encyclopedia  the exploration/encyclopedia system holds a real, host-counted species observation
    //   Rumor         ... and the reef band has been reached (a discovered Reef cell): the crew hears of something deeper
    //   Trace         ... and every required trace point of the deep world was found (DeepProgressionIds.RequiredTraceCount)
    //   Discovery     ... and the encounter area was reached from a valid deep context -> the boss is UNLOCKED (available)
    // Encyclopedia and Rumor are DERIVED from the existing exploration evidence (no second exploration authority). Trace and
    // Discovery are fed by the host-side world layer through DeepProgressionEvidence and re-checked by the world validator
    // (IDeepProgressionWorld). There is no client path at all: a guest cannot report a trace or a discovery.
    public enum DeepProgressionStage : byte
    {
        Locked = 0,
        Encyclopedia = 1,
        Rumor = 2,
        Trace = 3,
        Discovery = 4
    }

    public static class DeepProgressionIds
    {
        // The one P4.4 boss. Stable like BoatRepairParts.BoatId so a save and Mehmet's encounter can reference it.
        public const string BossId = "boss-deep-1";

        // WORKING VALUE: how many DISTINCT trace points must be found before the discovery step. Utku's #122 must author at least
        // this many trace points (any ids; the world validator accepts them one by one). Changing it is a save-visible rule change.
        public const int RequiredTraceCount = 3;

        public const int CurrentSaveVersion = 1;
    }

    // What every reader (map/encyclopedia UI, Mehmet's encounter layer) sees. No world position, no trace/arena coordinates:
    // only the stage and counts, so nothing hidden leaks before the unlock.
    public readonly struct DeepProgressionState
    {
        public readonly DeepProgressionStage Stage;
        public readonly int TracesFound;
        public readonly int TracesRequired;
        public readonly IReadOnlyList<string> CompletedBossIds;
        public readonly int Revision;

        public DeepProgressionState(DeepProgressionStage stage, int tracesFound, int tracesRequired,
            IReadOnlyList<string> completedBossIds, int revision)
        {
            Stage = stage;
            TracesFound = tracesFound;
            TracesRequired = tracesRequired;
            CompletedBossIds = completedBossIds ?? Array.Empty<string>();
            Revision = revision;
        }

        public bool BossUnlocked => Stage == DeepProgressionStage.Discovery;

        public bool IsCompleted(string bossId)
        {
            if (CompletedBossIds == null) return false;
            for (var i = 0; i < CompletedBossIds.Count; i++)
                if (string.Equals(CompletedBossIds[i], bossId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // The persistent side. Versioned, idempotent: the ids make a replay a no-op, and a restore NEVER trusts the stage number
    // alone (it must be backed by the evidence ids, otherwise it falls back to the highest consistent stage: fail-closed).
    // The transient boss encounter (active, health, who is inside) is deliberately NOT stored here (Mehmet's, not persisted).
    [Serializable]
    public sealed class DeepProgressionSaveData
    {
        public int Version = DeepProgressionIds.CurrentSaveVersion;
        public byte Stage;
        public string EncyclopediaEvidenceId = "";
        public string RumorEvidenceId = "";
        public List<string> TraceIds = new List<string>();
        public string DiscoveryEvidenceId = "";
        public List<string> CompletedBossIds = new List<string>();
    }

    public interface IProgressionPersistence
    {
        DeepProgressionSaveData ExportProgression();
        bool RestoreProgression(DeepProgressionSaveData data);
    }

    // Seam for Utku's world (#122): the host-side rule "is this trace/arena context real?". The trace points, the arena and the
    // deep band geometry stay his; progression only asks. UNBOUND = fail-closed: no trace or discovery is ever accepted.
    public interface IDeepProgressionWorld
    {
        // The host observed this player in this exploration cell/depth band when it reached the trace point.
        bool IsValidTrace(string traceId, string cellId, string depthBandId);

        // The same for the boss encounter area (arenaId is the stable arena id World gives Mehmet's encounter).
        bool IsDiscoveryArea(string arenaId, string cellId, string depthBandId);
    }

    public static class DeepProgressionWorld
    {
        public static IDeepProgressionWorld Current { get; private set; }

        public static void Bind(IDeepProgressionWorld world) => Current = world;

        public static void Unbind(IDeepProgressionWorld world)
        {
            if (ReferenceEquals(Current, world)) Current = null;
        }
    }

    // Host-side feed. Utku's trigger volumes / Mehmet's encounter layer call these on the HOST with what the host itself
    // observed (the player's real cell and band, never a client claim). Composition binds the handlers to the authority while it
    // is the host; unbound (client, no campaign) rejects.
    public static class DeepProgressionEvidence
    {
        public static Func<PlayerId, string, string, string, ulong, TransactionResult> TraceHandler { get; private set; }
        public static Func<PlayerId, string, string, string, ulong, TransactionResult> DiscoveryHandler { get; private set; }
        public static Func<string, string, ulong, TransactionResult> CompleteHandler { get; private set; }

        public static bool IsBound => TraceHandler != null;

        public static void Bind(Func<PlayerId, string, string, string, ulong, TransactionResult> trace,
            Func<PlayerId, string, string, string, ulong, TransactionResult> discovery,
            Func<string, string, ulong, TransactionResult> complete)
        {
            TraceHandler = trace;
            DiscoveryHandler = discovery;
            CompleteHandler = complete;
        }

        public static void Unbind(Func<PlayerId, string, string, string, ulong, TransactionResult> trace,
            Func<PlayerId, string, string, string, ulong, TransactionResult> discovery,
            Func<string, string, ulong, TransactionResult> complete)
        {
            if (TraceHandler == trace) TraceHandler = null;
            if (DiscoveryHandler == discovery) DiscoveryHandler = null;
            if (CompleteHandler == complete) CompleteHandler = null;
        }

        // Reasons: OutOfOrder (an earlier step is missing), InvalidContext (the world refused this cell/band),
        // WorldUnavailable (no validator bound: fail-closed), AlreadyProcessed (that evidence/step was counted), InvalidTarget,
        // InvalidState (unbound), SaveFailed.
        public static TransactionResult TrySubmitTrace(PlayerId player, string traceId, string cellId, string depthBandId, ulong requestId) =>
            TraceHandler != null ? TraceHandler(player, traceId, cellId, depthBandId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TrySubmitDiscovery(PlayerId player, string arenaId, string cellId, string depthBandId, ulong requestId) =>
            DiscoveryHandler != null ? DiscoveryHandler(player, arenaId, cellId, depthBandId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);

        // Mehmet's encounter layer reports a finished encounter once; it is stored once per boss.
        public static TransactionResult TryCompleteBoss(string bossId, string encounterId, ulong requestId) =>
            CompleteHandler != null ? CompleteHandler(bossId, encounterId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    // Read seam for Mehmet's encounter authority (and the UI): a boss may only be offered when this says it is available.
    // Unbound (no campaign) = Locked, so the default is closed.
    public static class BossProgression
    {
        private static Func<DeepProgressionState> provider;

        public static void Bind(Func<DeepProgressionState> state) => provider = state;

        public static void Unbind(Func<DeepProgressionState> state)
        {
            if (provider == state) provider = null;
        }

        public static DeepProgressionState State =>
            provider != null ? provider() : new DeepProgressionState(DeepProgressionStage.Locked, 0, DeepProgressionIds.RequiredTraceCount, null, 0);

        public static bool IsAvailable(string bossId) =>
            string.Equals(bossId, DeepProgressionIds.BossId, StringComparison.Ordinal) && State.BossUnlocked;

        public static bool IsCompleted(string bossId) => State.IsCompleted(bossId);
    }
}
