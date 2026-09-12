using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class RecordingSessionTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        private const string Subject = "sea_bass";

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        private static FakeDive ActiveDive(string diveId = "dive-1") =>
            new FakeDive { IsDiveActive = true, CurrentDiveId = diveId };

        private static readonly QualityTier[] Ladder =
        {
            new QualityTier("Bronze", 0.25f, 2f),
            new QualityTier("Silver", 0.5f, 4f),
            new QualityTier("Gold", 0.7f, 6f)
        };

        private static RecordingSession Session() => new RecordingSession(Subject, Ladder);

        // A frame the framing rules accepted, at the given score.
        private static RecordingSample Good(float score01) =>
            RecordingSample.Valid(score01, 5f, 0f, 0.3f);

        private static RecordingSample Blocked() =>
            RecordingSample.Reject(RecordingSampleRejection.Occluded, 5f);

        // Films for the given seconds at a steady score, one host tick at a time.
        private static void Film(RecordingSession session, PlayerId player, float seconds, float score01,
            float tick = 0.5f)
        {
            for (var elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += tick)
                session.Tick(player, tick, Good(score01));
        }

        [Test]
        public void StartOpensATakeAndStopClosesIt()
        {
            var session = Session();
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Assert.IsTrue(session.IsRecording(Alice));

            Film(session, Alice, 5f, 0.8f);
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.IsFalse(session.IsRecording(Alice));
            Assert.AreEqual(Subject, evaluation.SubjectId);
            Assert.AreEqual("dive-1", evaluation.DiveId);
            Assert.AreEqual(Alice, evaluation.PlayerId);
        }

        // The contract this whole two-call split exists for: the caller says "start" and "stop",
        // never "I recorded for N seconds". There is no parameter that could carry a claim.
        [Test]
        public void TheDurationIsWhateverTheHostTickedAndNothingTheCallerCouldClaim()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            for (var i = 0; i < 12; i++) session.Tick(Alice, 0.25f, Good(0.8f));

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.AreEqual(3f, evaluation.ValidSeconds, 0.0001f, "12 host ticks of 0.25s is 3 seconds");
        }

        [Test]
        public void StoppingImmediatelyAfterStartingEarnsNothing()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.AreEqual(0f, evaluation.ValidSeconds);
            Assert.AreEqual(RecordingQuality.NoPayout, evaluation.Quality);
            Assert.IsFalse(evaluation.IsPayable, "a tap of the record button is not a recording");
        }

        [Test]
        public void FramesTheRulesRejectedBankNoTime()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            for (var i = 0; i < 20; i++) session.Tick(Alice, 0.5f, Blocked());

            session.TryStop(dive, Alice, 2, out var evaluation);
            Assert.AreEqual(0f, evaluation.ValidSeconds, "ten seconds behind a rock is not ten seconds of footage");
            Assert.IsFalse(evaluation.IsPayable);
        }

        [Test]
        public void OnlyTheValidStretchOfALongTakeCounts()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            for (var i = 0; i < 8; i++) session.Tick(Alice, 0.5f, Good(0.8f)); // 4s filmed
            for (var i = 0; i < 8; i++) session.Tick(Alice, 0.5f, Blocked());  // 4s wasted

            session.TryStop(dive, Alice, 2, out var evaluation);
            Assert.AreEqual(4f, evaluation.ValidSeconds, 0.0001f);
        }

        [Test]
        public void TheScoreIsWeightedByHowLongTheShotHeldUp()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            session.Tick(Alice, 1f, Good(1f));
            session.Tick(Alice, 3f, Good(0f));

            session.TryStop(dive, Alice, 2, out var evaluation);
            Assert.AreEqual(4f, evaluation.ValidSeconds, 0.0001f);
            Assert.AreEqual(0.25f, evaluation.Score01, 0.0001f, "one good second in four is a quarter");
        }

        [Test]
        public void TheGradeComesFromTheTierLadder()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 4f, 0.8f);

            session.TryStop(dive, Alice, 2, out var evaluation);
            // 0.8 clears Gold's score but 4s does not clear its 6s, so Silver it is.
            Assert.AreEqual(2, evaluation.Quality);
            Assert.IsTrue(evaluation.IsPayable);
        }

        [Test]
        public void TickIgnoresNonsenseDeltasInsteadOfCorruptingTheDuration()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            session.Tick(Alice, float.NaN, Good(1f));
            session.Tick(Alice, float.PositiveInfinity, Good(1f));
            session.Tick(Alice, -5f, Good(1f));
            session.Tick(Alice, 0f, Good(1f));

            session.TryStop(dive, Alice, 2, out var evaluation);
            Assert.AreEqual(0f, evaluation.ValidSeconds);
        }

        [Test]
        public void TickingAPlayerWhoIsNotRecordingDoesNothing()
        {
            var session = Session();
            session.Tick(Alice, 1f, Good(1f));

            Assert.IsFalse(session.IsRecording(Alice));
            Assert.AreEqual(0f, session.ValidSecondsFor(Alice));
        }

        [Test]
        public void RecordingWithoutALiveDiveIsRefusedAndNotRemembered()
        {
            var session = Session();
            var dive = new FakeDive { IsDiveActive = false, CurrentDiveId = "dive-1" };

            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStart(dive, Alice, 1));
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStart(null, Alice, 2));
            Assert.AreEqual(PlayerActionResult.InvalidState,
                session.TryStart(new FakeDive { IsDiveActive = true, CurrentDiveId = "" }, Alice, 3));
            Assert.IsFalse(session.IsRecording(Alice));

            // Not remembered: once a dive is running the same request may still succeed.
            dive.IsDiveActive = true;
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
        }

        [Test]
        public void ASubjectWithoutAnIdCanNeverBeRecorded()
        {
            var session = new RecordingSession("", Ladder);

            Assert.AreEqual(PlayerActionResult.InvalidTarget, session.TryStart(ActiveDive(), Alice, 1));
            Assert.IsFalse(session.IsRecording(Alice));
        }

        [Test]
        public void StartingTwiceDoesNotOpenASecondTake()
        {
            var session = Session();
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStart(dive, Alice, 2));
            Assert.AreEqual(1, session.OpenTakeCount);
        }

        [Test]
        public void AReplayedStartReturnsTheEarlierResultWithoutRestartingTheTake()
        {
            var session = Session();
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Film(session, Alice, 3f, 0.8f);
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));

            Assert.AreEqual(3f, session.ValidSecondsFor(Alice), 0.0001f,
                "a replayed start must not reset the footage already banked");
        }

        [Test]
        public void AReplayedStopReturnsTheEarlierResultWithoutProducingASecondEvaluation()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 4f, 0.8f);

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var first));
            Assert.AreEqual(2, first.Quality);

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var second));
            Assert.AreEqual(RecordingQuality.NoPayout, second.Quality);
            Assert.IsFalse(second.IsPayable, "the replay must not mint a second payable evaluation");
        }

        [Test]
        public void StoppingWithoutStartingIsRefused()
        {
            var session = Session();

            Assert.AreEqual(PlayerActionResult.InvalidState,
                session.TryStop(ActiveDive(), Alice, 1, out var evaluation));
            Assert.IsFalse(evaluation.IsPayable);
        }

        [Test]
        public void ATakeThatOutlivesItsDiveIsVoid()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 8f, 0.9f);

            dive.CurrentDiveId = "dive-2";
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.IsFalse(evaluation.IsPayable, "last dive's footage must not be cashed in on this one");
            Assert.IsFalse(session.IsRecording(Alice), "the void take is dropped, not left open");
        }

        [Test]
        public void ATakeStoppedAfterTheDiveEndedIsVoid()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 8f, 0.9f);

            dive.IsDiveActive = false;
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.IsFalse(evaluation.IsPayable);
        }

        [Test]
        public void AbortDropsTheTakeWithoutProducingAnything()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 8f, 0.9f);

            session.Abort(Alice);
            Assert.IsFalse(session.IsRecording(Alice));
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var evaluation));
            Assert.IsFalse(evaluation.IsPayable, "a fish that died mid-shot pays nothing");
        }

        [Test]
        public void TwoDiversFilmTheSameSubjectIndependently()
        {
            var session = Session();
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Bob, 1));
            Assert.AreEqual(2, session.OpenTakeCount);

            Film(session, Alice, 6f, 0.9f);
            Film(session, Bob, 2f, 0.3f);

            session.TryStop(dive, Alice, 2, out var aliceTake);
            session.TryStop(dive, Bob, 2, out var bobTake);

            Assert.AreEqual(6f, aliceTake.ValidSeconds, 0.0001f);
            Assert.AreEqual(2f, bobTake.ValidSeconds, 0.0001f);
            Assert.Greater(aliceTake.Quality, bobTake.Quality);
        }

        [Test]
        public void AbortAllClearsEveryOpenTake()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            session.TryStart(dive, Bob, 1);

            session.AbortAll();
            Assert.AreEqual(0, session.OpenTakeCount);
        }

        [Test]
        public void ResetClearsOpenTakesAndHandledRequestsForTheNextDive()
        {
            var session = Session();
            var firstDive = ActiveDive();
            session.TryStart(firstDive, Alice, 1);
            Film(session, Alice, 4f, 0.8f);

            session.Reset();
            Assert.AreEqual(0, session.OpenTakeCount);

            var secondDive = ActiveDive("dive-2");
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(secondDive, Alice, 1),
                "the old dive's request id must not block the new dive");
            Assert.AreEqual(0f, session.ValidSecondsFor(Alice), "no footage may survive the reset");
        }
    }
}
