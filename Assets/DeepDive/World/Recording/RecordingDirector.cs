using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // The host-side entry point for P3-A's recording transport, and the guard between a finished
    // take and the economy. Implements Core's IRecordingEvaluationSink, so the chain reads:
    //
    //   RecordingNetworkBridge (Mehmet)  ->  RecordingEvaluation.Sink  ->  this
    //     -> RecordingSubject/RecordingSession (the shot)  ->  RecordingLedger (who gets paid)
    //
    // Composition owns the wiring: it calls RecordingEvaluation.Bind(director) and hands the
    // dive's DiveSummary to SettleDive. World never binds itself - the bridge and the binding
    // both live on the other side of the Core contract.
    //
    // A plain class, not a MonoBehaviour: every rule here is orderings and id checks, so it
    // stays testable without NGO, an economy or a scene. The per-tick physics is the subject's.
    //
    // Payability is this class's core responsibility. RecordingSubject hands back a
    // RecordingTake for every processed stop, including ones worth nothing (Quality 0, no dive
    // id, a replayed request that returns an empty take). Core's own contract comment asks for
    // exactly this check: "Composition must inspect its IsPayable flag before forwarding
    // anything to economy". Only a payable take reaches the ledger, and even then the ledger
    // holds it as a claim until the dive settles - nobody is paid mid-dive.
    public sealed class RecordingDirector : IRecordingEvaluationSink
    {
        private readonly RecordingLedger ledger = new RecordingLedger();

        // Host-side notification for logging and HUD feedback later. Raised only for takes that
        // were payable and actually became the best claim for their (player, subject).
        public event Action<RecordingTake> TakeRegistered;

        // Read-only views for the host log and the tests; the ledger itself stays private so
        // nothing outside can register a take that skipped the IsPayable check below.
        public int ClaimCount => ledger.Count;

        public bool IsSettled => ledger.IsSettled;

        public bool TryGetBestClaim(PlayerId player, string subjectId, out RecordingTake take) =>
            ledger.TryGetBest(player, subjectId, out take);

        public PlayerActionResult TryStart(RecordingCandidate candidate)
        {
            if (!TryResolve(candidate, out var subject, out var refusal)) return refusal;
            return subject.TryStartTake(candidate.PlayerId, candidate.RequestId);
        }

        public PlayerActionResult TryStop(RecordingCandidate candidate)
        {
            if (!TryResolve(candidate, out var subject, out var refusal)) return refusal;

            var result = subject.TryStopTake(candidate.PlayerId, candidate.RequestId, out var take);
            if (result != PlayerActionResult.Accepted) return result;

            // Accepted means the stop was processed, not that money was earned. Three different
            // nothings arrive here as Accepted and must all be dropped:
            //   - Quality 0: the shot was taken but was too poor or too short to pay for;
            //   - an empty take from a replayed requestId, which is the second half of the
            //     double-payment guard: the first stop already registered its claim, and this
            //     repeat must not register a second one;
            //   - a take whose dive id or subject id went missing, which could never settle.
            // The result still goes back to the player unchanged - their stop did happen.
            if (!take.IsPayable) return result;

            if (ledger.Register(take)) TakeRegistered?.Invoke(take);
            return result;
        }

        // Called by Composition when Mert's DiveSummary lands, which is the only moment anybody
        // is paid: "odeme guvenli donusten sonra". The summary decides who surfaced, so a
        // drowned cameraman's claim is skipped in favour of the next best one who made it out.
        //
        // Reads RecordingClaim.Sink at call time, the same way CatchObject reads CatchClaim.Sink:
        // left unbound nothing is paid and the ledger stays open, so a late binding can still
        // settle the dive instead of the recordings being silently burned.
        public IReadOnlyList<RecordingResult> SettleDive(DiveSummary summary) =>
            ledger.Settle(RecordingClaim.Sink, summary.DiveId, summary.SafelyReturned);

        // For reuse across dives, matching CatchState.Reset and RecordingSession.Reset: no claim
        // and no settled flag from the finished dive may survive into the next one.
        public void Reset() => ledger.Reset();

        // Turns a transport-level candidate into the World object that can judge the shot, or
        // says why it cannot. Two things are checked here and nowhere else:
        //
        //   - Core's IRecordingTarget is deliberately empty, so the target carries no subject
        //     id. Only an IRecordingSubject can produce a take; anything else the bridge
        //     happened to resolve is not a filmable thing.
        //   - RecordingCandidate.DiveId comes from the bridge's own view of the session, while
        //     RecordingSession stamps takes with the dive id it reads from IDiveContext. If the
        //     two disagree the bridge is a dive behind, and accepting would let a request be
        //     judged against the wrong dive. Refused as InvalidState, which is how the session
        //     already treats a take that outlived its dive.
        private static bool TryResolve(RecordingCandidate candidate, out IRecordingSubject subject,
            out PlayerActionResult refusal)
        {
            subject = candidate.Target as IRecordingSubject;
            if (subject == null)
            {
                refusal = PlayerActionResult.InvalidTarget;
                return false;
            }

            var liveDiveId = DiveContext.CurrentDiveId;
            if (string.IsNullOrWhiteSpace(liveDiveId) ||
                !string.Equals(candidate.DiveId, liveDiveId, StringComparison.Ordinal))
            {
                subject = null;
                refusal = PlayerActionResult.InvalidState;
                return false;
            }

            refusal = PlayerActionResult.Accepted;
            return true;
        }
    }
}
