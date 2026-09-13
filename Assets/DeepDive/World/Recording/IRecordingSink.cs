using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // The money side of the recording chain, seen from World - the exact counterpart of
    // ICatchClaimSink. Composition implements it against Mert's economy and maps his result
    // onto the shared PlayerActionResult; World never references DeepDive.Economy itself.
    //
    // docs/plan/CONTRACTS.md: "RecordingResult | recordingId, diveId, playerId, tur/olay
    // kimligi, kalite, gecerli sure | Utku -> Mert/Mehmet | P3". World decides the quality,
    // Mert decides what that quality is worth; no credit value is ever computed on this side.
    //
    // Only PlayerActionResult.Accepted means the recording was actually paid. Every other value
    // leaves the recording unpaid, and the ledger reports it as such rather than retrying.
    public interface IRecordingSink
    {
        PlayerActionResult TryClaim(RecordingResult result);
    }

    // Host-side binding point, same shape as DiveContext and CatchClaim. Composition binds it;
    // World reads it. Left unbound nobody can be paid, which is the safe direction: the ledger
    // refuses to settle at all rather than discarding a dive's recordings unpaid.
    public static class RecordingClaim
    {
        public static IRecordingSink Sink { get; private set; }

        public static bool IsBound => Sink != null;

        public static void Bind(IRecordingSink sink) => Sink = sink;

        // Composition must call this when the session ends, so a sink belonging to a finished
        // session cannot be handed the next one's payouts.
        public static void Unbind() => Sink = null;
    }
}
