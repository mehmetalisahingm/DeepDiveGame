using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class RecordingLedgerTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);
        private static readonly PlayerId Cem = new PlayerId(2);

        private const string Dive = "dive-1";
        private const string Bass = "sea_bass";
        private const string Octopus = "octopus";

        // Stands in for Composition's bridge to Mert's economy. Counting the calls is how the
        // tests prove nobody is paid twice and nobody is paid before surfacing.
        private sealed class FakeEconomy : IRecordingSink
        {
            public PlayerActionResult Answer = PlayerActionResult.Accepted;
            public readonly List<RecordingResult> Claims = new List<RecordingResult>();

            public int Calls => Claims.Count;

            public PlayerActionResult TryClaim(RecordingResult result)
            {
                Claims.Add(result);
                return Answer;
            }
        }

        private static RecordingEvaluation Take(PlayerId player, string subject, int quality,
            float validSeconds = 5f, float score01 = 0.6f, string diveId = Dive) =>
            new RecordingEvaluation(diveId, player, subject, quality, validSeconds, score01);

        private static IReadOnlyCollection<PlayerId> Surfaced(params PlayerId[] players) => players;

        [Test]
        public void OnlyTheBestTakePerPlayerAndSubjectIsKept()
        {
            var ledger = new RecordingLedger();

            Assert.IsTrue(ledger.Register(Take(Alice, Bass, 1)));
            Assert.IsTrue(ledger.Register(Take(Alice, Bass, 3)));
            Assert.IsFalse(ledger.Register(Take(Alice, Bass, 2)), "a worse take must not replace a better one");

            Assert.AreEqual(1, ledger.Count);
            Assert.IsTrue(ledger.TryGetBest(Alice, Bass, out var best));
            Assert.AreEqual(3, best.Quality);
        }

        [Test]
        public void EachSubjectAndEachPlayerIsTrackedSeparately()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 2));
            ledger.Register(Take(Alice, Octopus, 1));
            ledger.Register(Take(Bob, Bass, 4));

            Assert.AreEqual(3, ledger.Count);
        }

        [Test]
        public void AShotWorthNothingIsNotStored()
        {
            var ledger = new RecordingLedger();

            Assert.IsFalse(ledger.Register(Take(Alice, Bass, RecordingQuality.NoPayout)));
            Assert.IsFalse(ledger.Register(Take(Alice, "", 3)), "a subjectless take can never be priced");
            Assert.IsFalse(ledger.Register(Take(Alice, Bass, 3, diveId: "")));
            Assert.AreEqual(0, ledger.Count);
        }

        [Test]
        public void SettlementPaysOncePerSubjectToTheBestDiverWhoSurfaced()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 2));
            ledger.Register(Take(Bob, Bass, 4));
            ledger.Register(Take(Alice, Octopus, 3));
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice, Bob));

            Assert.AreEqual(2, paid.Count, "two species filmed, two payments");
            Assert.AreEqual(2, economy.Calls);
            Assert.AreEqual(1, CountFor(paid, Bass));
            Assert.AreEqual(1, CountFor(paid, Octopus));
            Assert.AreEqual(Bob, FindFor(paid, Bass).PlayerId, "Bob had the better bass shot");
            Assert.AreEqual(4, FindFor(paid, Bass).Quality);
        }

        [Test]
        public void WhenTheBestCameramanDrownsTheNextBestSurvivorIsPaid()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Bob, Bass, 4));   // best, but Bob never makes it out
            ledger.Register(Take(Alice, Bass, 2)); // second best, surfaces
            ledger.Register(Take(Cem, Bass, 1));   // worst, also surfaces
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice, Cem));

            Assert.AreEqual(1, paid.Count, "still one payment for the species");
            Assert.AreEqual(Alice, paid[0].PlayerId);
            Assert.AreEqual(2, paid[0].Quality, "the drowned diver's grade does not carry over");
        }

        [Test]
        public void IfNobodyWhoFilmedItSurfacedNobodyIsPaid()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Bob, Bass, 4));
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice));

            Assert.AreEqual(0, paid.Count);
            Assert.AreEqual(0, economy.Calls, "the economy must not even be asked");
            Assert.IsTrue(ledger.IsSettled, "the dive is still over");
        }

        [Test]
        public void ADiveWhereNobodySurfacedPaysNothing()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 4));
            var economy = new FakeEconomy();

            Assert.AreEqual(0, ledger.Settle(economy, Dive, Surfaced()).Count);
            Assert.AreEqual(0, economy.Calls);
        }

        [Test]
        public void SettlingTwiceDoesNotPayTwice()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3));
            var economy = new FakeEconomy();

            Assert.AreEqual(1, ledger.Settle(economy, Dive, Surfaced(Alice)).Count);
            Assert.AreEqual(0, ledger.Settle(economy, Dive, Surfaced(Alice)).Count,
                "a retried DiveSummary must not pay a second time");
            Assert.AreEqual(1, economy.Calls);
        }

        [Test]
        public void RegisteringAfterSettlementIsRefused()
        {
            var ledger = new RecordingLedger();
            ledger.Settle(new FakeEconomy(), Dive, Surfaced(Alice));

            Assert.IsFalse(ledger.Register(Take(Alice, Bass, 4)));
        }

        [Test]
        public void AnUnboundEconomyPaysNothingAndLeavesTheLedgerOpen()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3));

            Assert.AreEqual(0, ledger.Settle(null, Dive, Surfaced(Alice)).Count);
            Assert.IsFalse(ledger.IsSettled, "an unwired economy must not burn the dive's recordings");

            var economy = new FakeEconomy();
            Assert.AreEqual(1, ledger.Settle(economy, Dive, Surfaced(Alice)).Count,
                "once the sink is bound the same dive can still be settled");
        }

        [Test]
        public void AMissingDiveIdPaysNothingAndLeavesTheLedgerOpen()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3));
            var economy = new FakeEconomy();

            Assert.AreEqual(0, ledger.Settle(economy, "", Surfaced(Alice)).Count);
            Assert.AreEqual(0, ledger.Settle(economy, null, Surfaced(Alice)).Count);
            Assert.IsFalse(ledger.IsSettled);
            Assert.AreEqual(0, economy.Calls);
        }

        [Test]
        public void TakesStampedWithAnotherDiveAreNotPaid()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 4, diveId: "dive-0"));
            ledger.Register(Take(Alice, Octopus, 2));
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice));

            Assert.AreEqual(1, paid.Count);
            Assert.AreEqual(Octopus, paid[0].SubjectId, "last dive's footage is not this dive's payout");
        }

        [Test]
        public void ARefusedClaimIsNotReportedAsPaidAndDoesNotFallToTheRunnerUp()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Bob, Bass, 4));
            ledger.Register(Take(Alice, Bass, 2));
            var economy = new FakeEconomy { Answer = PlayerActionResult.Rejected };

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice, Bob));

            Assert.AreEqual(0, paid.Count);
            Assert.AreEqual(1, economy.Calls, "a refusal is Mert's decision, not the runner-up's turn");
            Assert.AreEqual(Bob, economy.Claims[0].PlayerId);
        }

        [Test]
        public void ThePaidResultCarriesEveryFieldMertNeeds()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3, validSeconds: 7.5f));
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice));

            Assert.AreEqual(1, paid.Count);
            var result = paid[0];
            Assert.IsNotEmpty(result.RecordingId);
            Assert.AreEqual(Dive, result.DiveId);
            Assert.AreEqual(Alice, result.PlayerId);
            Assert.AreEqual(Bass, result.SubjectId);
            Assert.AreEqual(3, result.Quality);
            Assert.AreEqual(7.5f, result.ValidDurationSeconds, 0.0001f);
        }

        [Test]
        public void EveryPayoutGetsItsOwnRecordingId()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3));
            ledger.Register(Take(Alice, Octopus, 3));
            var economy = new FakeEconomy();

            var paid = ledger.Settle(economy, Dive, Surfaced(Alice));

            Assert.AreEqual(2, paid.Count);
            Assert.AreNotEqual(paid[0].RecordingId, paid[1].RecordingId);
        }

        [Test]
        public void TiesAreBrokenTheSameWayEveryTimeSoHostsAgree()
        {
            var identical = Take(Alice, Bass, 3, 5f, 0.6f);
            var sameAgain = Take(Bob, Bass, 3, 5f, 0.6f);

            // Lower player id wins a dead heat; the point is only that it is never arbitrary.
            Assert.Greater(RecordingLedger.Compare(identical, sameAgain), 0);
            Assert.Less(RecordingLedger.Compare(sameAgain, identical), 0);
            Assert.AreEqual(0, RecordingLedger.Compare(identical, identical));
        }

        [Test]
        public void BetterMeansHigherTierThenBetterLookingThenLonger()
        {
            Assert.Greater(RecordingLedger.Compare(Take(Alice, Bass, 4, 2f, 0.1f), Take(Alice, Bass, 3, 90f, 0.9f)),
                0, "tier beats everything");
            Assert.Greater(RecordingLedger.Compare(Take(Alice, Bass, 3, 5f, 0.9f), Take(Alice, Bass, 3, 5f, 0.5f)),
                0, "at the same tier the better-looking shot wins");
            Assert.Greater(RecordingLedger.Compare(Take(Alice, Bass, 3, 9f, 0.6f), Take(Alice, Bass, 3, 5f, 0.6f)),
                0, "then the longer one");
        }

        [Test]
        public void ResetClearsTheLedgerForTheNextDive()
        {
            var ledger = new RecordingLedger();
            ledger.Register(Take(Alice, Bass, 3));
            ledger.Settle(new FakeEconomy(), Dive, Surfaced(Alice));

            ledger.Reset();
            Assert.AreEqual(0, ledger.Count);
            Assert.IsFalse(ledger.IsSettled);
            Assert.IsTrue(ledger.Register(Take(Alice, Bass, 1, diveId: "dive-2")));
        }

        private static int CountFor(IReadOnlyList<RecordingResult> paid, string subjectId)
        {
            var count = 0;
            for (var i = 0; i < paid.Count; i++)
                if (paid[i].SubjectId == subjectId) count++;
            return count;
        }

        private static RecordingResult FindFor(IReadOnlyList<RecordingResult> paid, string subjectId)
        {
            for (var i = 0; i < paid.Count; i++)
                if (paid[i].SubjectId == subjectId) return paid[i];
            Assert.Fail($"no payment for {subjectId}");
            return default;
        }
    }
}
