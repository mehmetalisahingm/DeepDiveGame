using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class RecordingDirectorTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        private const string Subject = "sea_bass";
        private const string Dive = "dive-1";

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        // A filmable thing the director can reach without a spawned NetworkObject. Records what
        // it was asked so the tests can prove the candidate's player and requestId got through.
        private sealed class FakeSubject : IRecordingSubject
        {
            public string SubjectId => Subject;

            public PlayerActionResult StartResult = PlayerActionResult.Accepted;
            public PlayerActionResult StopResult = PlayerActionResult.Accepted;
            public RecordingTake StopTake;

            public int Starts;
            public int Stops;
            public PlayerId LastPlayer;
            public ulong LastRequestId;

            public PlayerActionResult TryStartTake(PlayerId player, ulong requestId)
            {
                Starts++;
                LastPlayer = player;
                LastRequestId = requestId;
                return StartResult;
            }

            public PlayerActionResult TryStopTake(PlayerId player, ulong requestId, out RecordingTake take)
            {
                Stops++;
                LastPlayer = player;
                LastRequestId = requestId;
                take = StopTake;
                return StopResult;
            }
        }

        // Something the bridge resolved that is a target but cannot judge a shot.
        private sealed class BareTarget : IRecordingTarget
        {
        }

        private sealed class FakeEconomy : IRecordingSink
        {
            public readonly List<RecordingResult> Claims = new List<RecordingResult>();
            public PlayerActionResult Result = PlayerActionResult.Accepted;

            public PlayerActionResult TryClaim(RecordingResult result)
            {
                Claims.Add(result);
                return Result;
            }
        }

        [SetUp]
        [TearDown]
        public void ClearBindings()
        {
            DiveContext.Unbind();
            RecordingClaim.Unbind();
        }

        private static void LiveDive(string diveId = Dive) =>
            DiveContext.Bind(new FakeDive { IsDiveActive = true, CurrentDiveId = diveId });

        private static RecordingCandidate Candidate(IRecordingTarget target, PlayerId player,
            ulong requestId = 1, string diveId = Dive) =>
            new RecordingCandidate(requestId, diveId, player, target);

        private static RecordingTake Take(PlayerId player, int quality, float score01 = 0.8f,
            float validSeconds = 5f, string diveId = Dive, string subjectId = Subject) =>
            new RecordingTake(diveId, player, subjectId, quality, validSeconds, score01);

        private static DiveSummary Summary(string diveId, params PlayerId[] safelyReturned) =>
            new DiveSummary(diveId, safelyReturned, null, null, "checkpoint-1");

        // --- resolving the candidate -------------------------------------------------------

        [Test]
        public void ANullTargetCannotBeFilmed()
        {
            LiveDive();
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                director.TryStart(Candidate(null, Alice)));
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                director.TryStop(Candidate(null, Alice)));
        }

        [Test]
        public void ATargetThatIsNotARecordingSubjectCannotBeFilmed()
        {
            LiveDive();
            var director = new RecordingDirector();

            // Core's IRecordingTarget is empty, so being a target is not enough to be graded.
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                director.TryStart(Candidate(new BareTarget(), Alice)));
        }

        [Test]
        public void WithoutALiveDiveNothingIsFilmed()
        {
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            // Unbound dive context.
            Assert.AreEqual(PlayerActionResult.InvalidState, director.TryStart(Candidate(subject, Alice)));

            DiveContext.Bind(new FakeDive { IsDiveActive = false, CurrentDiveId = Dive });
            Assert.AreEqual(PlayerActionResult.InvalidState, director.TryStart(Candidate(subject, Alice)));
            Assert.AreEqual(0, subject.Starts);
        }

        [Test]
        public void ACandidateStampedWithAnotherDiveIsRefused()
        {
            LiveDive("dive-2");
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            // The bridge is a dive behind; judging this against the live dive would be wrong.
            Assert.AreEqual(PlayerActionResult.InvalidState,
                director.TryStart(Candidate(subject, Alice, 1, "dive-1")));
            Assert.AreEqual(PlayerActionResult.InvalidState,
                director.TryStop(Candidate(subject, Alice, 2, "dive-1")));
            Assert.AreEqual(0, subject.Starts);
            Assert.AreEqual(0, subject.Stops);
        }

        [Test]
        public void AMatchingDiveIdReachesTheSubjectWithThePlayerAndRequestId()
        {
            LiveDive();
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStart(Candidate(subject, Bob, 7)));
            Assert.AreEqual(1, subject.Starts);
            Assert.AreEqual(Bob, subject.LastPlayer);
            Assert.AreEqual(7UL, subject.LastRequestId);
        }

        [Test]
        public void TheSubjectsOwnRefusalIsPassedBackUnchanged()
        {
            LiveDive();
            var subject = new FakeSubject { StartResult = PlayerActionResult.DuplicateRequest };
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                director.TryStart(Candidate(subject, Alice)));
        }

        // --- IsPayable, the double-payment guard -------------------------------------------

        [Test]
        public void APayableTakeBecomesAClaim()
        {
            LiveDive();
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();
            var registered = new List<RecordingTake>();
            director.TakeRegistered += registered.Add;

            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice)));
            Assert.AreEqual(1, director.ClaimCount);
            Assert.AreEqual(1, registered.Count);
            Assert.AreEqual(2, registered[0].Quality);
        }

        [Test]
        public void AQualityZeroTakeIsAcceptedButEarnsNothing()
        {
            LiveDive();
            var subject = new FakeSubject { StopTake = Take(Alice, RecordingQuality.NoPayout) };
            var director = new RecordingDirector();

            // The stop happened, so the player is told Accepted; there is just nothing to pay.
            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice)));
            Assert.AreEqual(0, director.ClaimCount);
        }

        [Test]
        public void AReplayedStopRegistersNothingASecondTime()
        {
            LiveDive();
            var subject = new FakeSubject { StopTake = Take(Alice, 3) };
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice, 2)));
            Assert.AreEqual(1, director.ClaimCount);

            // RecordingSession answers a replayed requestId with the remembered result and an
            // empty take. That empty take is what must not turn into a second claim.
            subject.StopTake = default;
            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice, 2)));
            Assert.AreEqual(1, director.ClaimCount);
        }

        [Test]
        public void ATakeStampedWithAnotherDiveNeverBecomesAClaim()
        {
            LiveDive();
            var subject = new FakeSubject { StopTake = Take(Alice, 3, diveId: "dive-9") };
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice)));
            Assert.AreEqual(1, director.ClaimCount);

            // It is held, but settlement drops anything belonging to another dive.
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            Assert.AreEqual(0, director.SettleDive(Summary(Dive, Alice)).Count);
            Assert.AreEqual(0, economy.Claims.Count);
        }

        [Test]
        public void ARefusedStopIsNeverRegisteredEvenWithAPayableTake()
        {
            LiveDive();
            var subject = new FakeSubject
            {
                StopResult = PlayerActionResult.InvalidState,
                StopTake = Take(Alice, 4)
            };
            var director = new RecordingDirector();

            Assert.AreEqual(PlayerActionResult.InvalidState, director.TryStop(Candidate(subject, Alice)));
            Assert.AreEqual(0, director.ClaimCount);
        }

        [Test]
        public void OnlyTheBestTakePerPlayerAndSubjectIsKept()
        {
            LiveDive();
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            subject.StopTake = Take(Alice, 1, 0.3f, 3f);
            director.TryStop(Candidate(subject, Alice, 1));
            subject.StopTake = Take(Alice, 3, 0.9f, 8f);
            director.TryStop(Candidate(subject, Alice, 2));
            subject.StopTake = Take(Alice, 2, 0.6f, 5f);
            director.TryStop(Candidate(subject, Alice, 3));

            Assert.AreEqual(1, director.ClaimCount);
            Assert.IsTrue(director.TryGetBestClaim(Alice, Subject, out var best));
            Assert.AreEqual(3, best.Quality);
        }

        // --- settlement --------------------------------------------------------------------

        [Test]
        public void NobodyIsPaidBeforeTheDiveSettles()
        {
            LiveDive();
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();

            director.TryStop(Candidate(subject, Alice));
            Assert.AreEqual(1, director.ClaimCount);
            Assert.AreEqual(0, economy.Claims.Count, "a claim mid-dive is not a payment");

            var paid = director.SettleDive(Summary(Dive, Alice));
            Assert.AreEqual(1, paid.Count);
            Assert.AreEqual(1, economy.Claims.Count);
            Assert.AreEqual(Subject, economy.Claims[0].SubjectId);
            Assert.AreEqual(2, economy.Claims[0].Quality);
            Assert.AreEqual(Dive, economy.Claims[0].DiveId);
        }

        [Test]
        public void ADrownedCameramanLosesTheSubjectToTheNextBestSafeRecording()
        {
            LiveDive();
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            subject.StopTake = Take(Alice, 4, 0.95f, 10f);
            director.TryStop(Candidate(subject, Alice, 1));
            subject.StopTake = Take(Bob, 2, 0.6f, 5f);
            director.TryStop(Candidate(subject, Bob, 2));

            // Alice filmed the better shot but did not surface.
            var paid = director.SettleDive(Summary(Dive, Bob));
            Assert.AreEqual(1, paid.Count);
            Assert.AreEqual(Bob, paid[0].PlayerId);
            Assert.AreEqual(1, economy.Claims.Count);
        }

        [Test]
        public void SettlingTwiceDoesNotPayTwice()
        {
            LiveDive();
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();
            director.TryStop(Candidate(subject, Alice));

            Assert.AreEqual(1, director.SettleDive(Summary(Dive, Alice)).Count);
            Assert.IsTrue(director.IsSettled);
            Assert.AreEqual(0, director.SettleDive(Summary(Dive, Alice)).Count);
            Assert.AreEqual(1, economy.Claims.Count);
        }

        [Test]
        public void WithoutABoundEconomyTheDiveStaysOpenForALaterSettlement()
        {
            LiveDive();
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();
            director.TryStop(Candidate(subject, Alice));

            Assert.AreEqual(0, director.SettleDive(Summary(Dive, Alice)).Count);
            Assert.IsFalse(director.IsSettled, "an unbound economy must not burn the dive's recordings");

            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            Assert.AreEqual(1, director.SettleDive(Summary(Dive, Alice)).Count);
            Assert.AreEqual(1, economy.Claims.Count);
        }

        [Test]
        public void AStopAfterSettlementCannotReopenTheDive()
        {
            LiveDive();
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();

            director.SettleDive(Summary(Dive, Alice));
            Assert.AreEqual(PlayerActionResult.Accepted, director.TryStop(Candidate(subject, Alice)));
            Assert.AreEqual(0, director.ClaimCount);
        }

        [Test]
        public void ResetClearsTheLedgerForTheNextDive()
        {
            LiveDive();
            var economy = new FakeEconomy();
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject { StopTake = Take(Alice, 2) };
            var director = new RecordingDirector();
            director.TryStop(Candidate(subject, Alice));
            director.SettleDive(Summary(Dive, Alice));

            director.Reset();
            Assert.IsFalse(director.IsSettled);
            Assert.AreEqual(0, director.ClaimCount);

            // The next dive is a clean sheet, with its own dive id.
            DiveContext.Unbind();
            LiveDive("dive-2");
            subject.StopTake = Take(Alice, 1, diveId: "dive-2");
            director.TryStop(Candidate(subject, Alice, 1, "dive-2"));
            Assert.AreEqual(1, director.SettleDive(Summary("dive-2", Alice)).Count);
            Assert.AreEqual(2, economy.Claims.Count);
        }

        [Test]
        public void AnEconomyRefusalStillClosesTheSubjectForThisDive()
        {
            LiveDive();
            var economy = new FakeEconomy { Result = PlayerActionResult.Rejected };
            RecordingClaim.Bind(economy);
            var subject = new FakeSubject();
            var director = new RecordingDirector();

            subject.StopTake = Take(Alice, 4, 0.95f, 10f);
            director.TryStop(Candidate(subject, Alice, 1));
            subject.StopTake = Take(Bob, 2, 0.6f, 5f);
            director.TryStop(Candidate(subject, Bob, 2));

            var paid = director.SettleDive(Summary(Dive, Alice, Bob));
            Assert.AreEqual(0, paid.Count);
            // Mert refusing is his decision about this dive, not a reason to try the runner-up.
            Assert.AreEqual(1, economy.Claims.Count);
            Assert.AreEqual(Alice, economy.Claims[0].PlayerId);
        }
    }
}
