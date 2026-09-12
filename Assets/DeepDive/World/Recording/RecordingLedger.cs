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

        public bool TryGetBest(PlayerId player, string subjectId, out RecordingTake take) =>
            best.TryGetValue((player.Value, subjectId ?? ""), out take);

        // Called once the dive is over and Mert's DiveSummary says who surfaced. Returns the
        // recordings that were actually paid, so the caller can log or show them.
        //
        // The sink is passed in rather than read from RecordingClaim, matching
        // CatchState.TryClaim: the rules stay pure and the tests stay honest about who paid.
        public IReadOnlyList<RecordingResult> Settle(IRecordingSink sink, string diveId,
            IReadOnlyCollection<PlayerId> safelyReturned)
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
            foreach (var subject in SubjectsInOrder(diveId, out var bySubject))
            {
                var candidates = bySubject[subject];
                candidates.Sort((left, right) => Compare(right, left)); // best first

                for (var i = 0; i < candidates.Count; i++)
                {
                    var candidate = candidates[i];
                    // Skipping past a drowned owner is the whole "sonraki en iyi guvenli kayit"
                    // rule; their take is simply lost with the rest of their dive.
                    if (!safe.Contains(candidate.PlayerId.Value)) continue;

                    var result = new RecordingResult(NewRecordingId(), diveId, candidate.PlayerId,
                        candidate.SubjectId, candidate.Quality, candidate.ValidSeconds);
                    if (sink.TryClaim(result) == PlayerActionResult.Accepted) paid.Add(result);

                    // One payment per subject per dive, whether the economy took it or refused
                    // it. A refusal is Mert's decision about this dive, not a reason to hand the
                    // species to the runner-up.
                    break;
                }
            }

            IsSettled = true;
            return paid;
        }

        // For reuse across dives, matching CatchState.Reset: nothing from the finished dive may
        // survive into the next one.
        public void Reset()
        {
            best.Clear();
            IsSettled = false;
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
