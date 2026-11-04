using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // One dive's worth of recordings, and the rule for who gets paid for them. Pure rules object
    // so the payout ordering is testable without NGO, an economy or a scene.
    //
    // Two rules, both from the P3-B brief:
    //   - keep only the best take per (player, subject), so filming the same species ten times
    //     does not flood the ledger;
    //   - pay exactly once per subject per dive, to the best take whose owner actually surfaced.
    //     If the best cameraman drowned, the next best one who made it out is paid instead.
    //
    // Payment happens at settlement and never before, which is what "odeme guvenli donusten
    // sonra" means: a take registered mid-dive is a claim, not a credit. Register only ever sees
    // payable takes - the caller checks RecordingTake.IsPayable first.
    public sealed class RecordingLedger
    {
        private readonly Dictionary<(ulong Player, string Subject), RecordingTake> best =
            new Dictionary<(ulong, string), RecordingTake>();

        // A settled dive is closed. EconomyManager already guards against a DiveSummary being
        // delivered twice ("a retried event delivery"); this is the same guard on the side that
        // decides who to pay, so a repeat cannot pay a second time.
        public bool IsSettled { get; private set; }
        private readonly Dictionary<string, RecordingResult> pendingResults = new Dictionary<string, RecordingResult>();
        private readonly HashSet<string> paidSubjects = new HashSet<string>();
        // #254: specimen id -> the catch a bag accepted in this dive, and that catch's dive.
        private readonly Dictionary<string, CaptureResult> catches = new Dictionary<string, CaptureResult>(StringComparer.Ordinal);

        public int Count => best.Count;

        // Rejects rather than stores anything unpayable: a Quality 0 take is a real outcome but
        // there is nothing to award, and keeping it would only complicate the settlement sort.
        public bool Register(RecordingTake take)
        {
            if (IsSettled) return false;
            if (!take.IsPayable) return false;

            var key = (take.PlayerId.Value, take.SubjectId);
            if (best.TryGetValue(key, out var current) && Compare(take, current) <= 0) return false;

            best[key] = take;
            return true;
        }

        // #254: a bag accepted this catch. Nothing is proved yet: only a catch the dive summary preserves counts.
        public void NoteCatch(CaptureResult capture)
        {
            if (string.IsNullOrWhiteSpace(capture.SpecimenId) || string.IsNullOrWhiteSpace(capture.CaptureId)) return;
            catches[capture.SpecimenId] = capture;
        }

        // #254: preserved catches of this dive with no film of the same animal in any diver's kept take. They stay
        // ordinary catches; this only tells the host log, and later the channel, that they prove no recording.
        public IReadOnlyList<string> CatchesWithoutFilm(string diveId, IReadOnlyCollection<string> preservedCaptureIds)
        {
            var result = new List<string>();
            var filmed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var take in best.Values) if (take.DiveId == diveId) filmed.Add(take.SpecimenId);
            foreach (var entry in catches)
                if (entry.Value.DiveId == diveId && !filmed.Contains(entry.Key) && Contains(preservedCaptureIds, entry.Value.CaptureId))
                    result.Add(entry.Value.CaptureId);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public bool TryGetBest(PlayerId player, string subjectId, out RecordingTake take) =>
            best.TryGetValue((player.Value, subjectId ?? ""), out take);

        // Called once the dive is over and Mert's DiveSummary says who surfaced. Returns the
        // recordings that were actually paid, so the caller can log or show them.
        //
        // The sink is passed in rather than read from RecordingClaim, matching
        // CatchState.TryClaim: the rules stay pure and the tests stay honest about who paid.
        public IReadOnlyList<RecordingResult> Settle(IRecordingSink sink, string diveId,
            IReadOnlyCollection<PlayerId> safelyReturned, IReadOnlyCollection<string> preservedCaptureIds = null)
        {
            if (IsSettled) return Array.Empty<RecordingResult>();

            // No bound economy and no dive id are both missing infrastructure, not decisions.
            // The ledger stays open so a correct settlement can still happen once Composition
            // has wired things up - the same reasoning as CatchState's unbound sink, and the
            // safe direction here because closing would silently burn the dive's recordings.
            if (sink == null) return Array.Empty<RecordingResult>();
            if (string.IsNullOrWhiteSpace(diveId)) return Array.Empty<RecordingResult>();

            var safe = new HashSet<ulong>();
            if (safelyReturned != null)
            {
                foreach (var player in safelyReturned) safe.Add(player.Value);
            }

            var paid = new List<RecordingResult>();
            var retryRequired = false;
            foreach (var subject in SubjectsInOrder(diveId, out var bySubject))
            {
                if (paidSubjects.Contains(subject)) continue;
                var candidates = bySubject[subject];
                candidates.Sort((left, right) => Compare(right, left)); // best first

                for (var i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];
                    // Skipping past a drowned owner is the whole "sonraki en iyi guvenli kayit"
                    // rule; their take is simply lost with the rest of their dive.
                    if (!safe.Contains(candidate.PlayerId.Value)) continue;

                    if (!pendingResults.TryGetValue(subject, out var result))
                        pendingResults[subject] = result = new RecordingResult(NewRecordingId(), diveId, candidate.PlayerId,
                            candidate.SubjectId, candidate.Quality, candidate.ValidSeconds,
                            ProofFor(candidate, preservedCaptureIds));
                    var answer = sink.TryClaim(result);
                    if (answer == PlayerActionResult.Accepted || answer == PlayerActionResult.DuplicateRequest)
                    {
                        paidSubjects.Add(subject);
                        if (answer == PlayerActionResult.Accepted) paid.Add(result);
                    }
                    else retryRequired = true;

                    // One payment per subject per dive, whether the economy took it or refused
                    // it. A refusal is Mert's decision about this dive, not a reason to hand the
                    // species to the runner-up.
                    break;
                }
            }

            IsSettled = !retryRequired;
            return paid;
        }

        // For reuse across dives, matching CatchState.Reset: nothing from the finished dive may
        // survive into the next one.
        public void Reset()
        {
            best.Clear();
            pendingResults.Clear();
            catches.Clear();
            paidSubjects.Clear();
            IsSettled = false;
        }

        // #254: the catch counts only from the same dive and only if it came home; a catch lost with a drowned diver
        // leaves the film as film alone. Who shot and who filmed do not matter: one diver may film while another hunts.
        private RecordingSpecimenProof ProofFor(RecordingTake take, IReadOnlyCollection<string> preservedCaptureIds)
        {
            if (catches.TryGetValue(take.SpecimenId, out var capture) && capture.DiveId == take.DiveId &&
                Contains(preservedCaptureIds, capture.CaptureId))
                return RecordingSpecimenProof.FilmedAndCaught(take.SpecimenId, take.WeightGrams, capture.CaptureId);
            return RecordingSpecimenProof.Filmed(take.SpecimenId, take.WeightGrams);
        }

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            if (ids == null) return false;
            foreach (var entry in ids) if (entry == id) return true;
            return false;
        }

        // Better means: higher tier first, then the better-looking shot, then the longer one.
        // The player id breaks the remaining ties so two identical takes settle the same way on
        // every host instead of following dictionary order. Positive when left is better.
        public static int Compare(RecordingTake left, RecordingTake right)
        {
            if (left.Quality != right.Quality) return left.Quality.CompareTo(right.Quality);

            var score = left.Score01.CompareTo(right.Score01);
            if (score != 0) return score;

            var seconds = left.ValidSeconds.CompareTo(right.ValidSeconds);
            if (seconds != 0) return seconds;

            return right.PlayerId.Value.CompareTo(left.PlayerId.Value);
        }

        // Per-recording unique id, minted at settlement like CaptureBuilder.NewCaptureId: a
        // session-scoped network id is not a persistent identity on its own.
        public static string NewRecordingId() => Guid.NewGuid().ToString("N");

        // Groups this dive's entries by subject and returns the subject ids in a stable order.
        // Entries stamped with another dive id are dropped: a take that outlived its dive must
        // never be cashed in against the current one.
        private List<string> SubjectsInOrder(string diveId,
            out Dictionary<string, List<RecordingTake>> bySubject)
        {
            bySubject = new Dictionary<string, List<RecordingTake>>(StringComparer.Ordinal);
            foreach (var entry in best.Values)
            {
                if (!string.Equals(entry.DiveId, diveId, StringComparison.Ordinal)) continue;
                if (!bySubject.TryGetValue(entry.SubjectId, out var list))
                {
                    list = new List<RecordingTake>();
                    bySubject[entry.SubjectId] = list;
                }
                list.Add(entry);
            }

            var subjects = new List<string>(bySubject.Keys);
            subjects.Sort(StringComparer.Ordinal);
            return subjects;
        }
    }
}
