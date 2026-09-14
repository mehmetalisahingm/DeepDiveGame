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

        // A subject that is only filmable some of the time, like the special event. The fish
        // subjects above pass no window at all and must stay unaffected by any of this.
        private sealed class FakeWindow : IRecordingWindow
        {
            public bool IsOpen { get; set; } = true;
        }

        private static RecordingSession Session(IRecordingWindow window) =>
            new RecordingSession(Subject, Ladder, window);

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
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var take));
            Assert.IsFalse(session.IsRecording(Alice));
            Assert.AreEqual(Subject, take.SubjectId);
            Assert.AreEqual("dive-1", take.DiveId);
            Assert.AreEqual(Alice, take.PlayerId);
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

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var take));
            Assert.AreEqual(3f, take.ValidSeconds, 0.0001f, "12 host ticks of 0.25s is 3 seconds");
        }

        [Test]
        public void StoppingImmediatelyAfterStartingEarnsNothing()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var take));
            Assert.AreEqual(0f, take.ValidSeconds);
            Assert.AreEqual(RecordingQuality.NoPayout, take.Quality);
            Assert.IsFalse(take.IsPayable, "a tap of the record button is not a recording");
        }

        [Test]
        public void FramesTheRulesRejectedBankNoTime()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            for (var i = 0; i < 20; i++) session.Tick(Alice, 0.5f, Blocked());

            session.TryStop(dive, Alice, 2, out var take);
            Assert.AreEqual(0f, take.ValidSeconds, "ten seconds behind a rock is not ten seconds of footage");
            Assert.IsFalse(take.IsPayable);
        }

        [Test]
        public void OnlyTheValidStretchOfALongTakeCounts()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            for (var i = 0; i < 8; i++) session.Tick(Alice, 0.5f, Good(0.8f)); // 4s filmed
            for (var i = 0; i < 8; i++) session.Tick(Alice, 0.5f, Blocked());  // 4s wasted

            session.TryStop(dive, Alice, 2, out var take);
            Assert.AreEqual(4f, take.ValidSeconds, 0.0001f);
        }

        [Test]
        public void TheScoreIsWeightedByHowLongTheShotHeldUp()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);

            session.Tick(Alice, 1f, Good(1f));
            session.Tick(Alice, 3f, Good(0f));

            session.TryStop(dive, Alice, 2, out var take);
            Assert.AreEqual(4f, take.ValidSeconds, 0.0001f);
            Assert.AreEqual(0.25f, take.Score01, 0.0001f, "one good second in four is a quarter");
        }

        [Test]
        public void TheGradeComesFromTheTierLadder()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 4f, 0.8f);

            session.TryStop(dive, Alice, 2, out var take);
            // 0.8 clears Gold's score but 4s does not clear its 6s, so Silver it is.
            Assert.AreEqual(2, take.Quality);
            Assert.IsTrue(take.IsPayable);
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

            session.TryStop(dive, Alice, 2, out var take);
            Assert.AreEqual(0f, take.ValidSeconds);
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
                session.TryStop(ActiveDive(), Alice, 1, out var take));
            Assert.IsFalse(take.IsPayable);
        }

        [Test]
        public void ATakeThatOutlivesItsDiveIsVoid()
        {
            var session = Session();
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 8f, 0.9f);

            dive.CurrentDiveId = "dive-2";
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var take));
            Assert.IsFalse(take.IsPayable, "last dive's footage must not be cashed in on this one");
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
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var take));
            Assert.IsFalse(take.IsPayable);
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
            Assert.AreEqual(PlayerActionResult.InvalidState, session.TryStop(dive, Alice, 2, out var take));
            Assert.IsFalse(take.IsPayable, "a fish that died mid-shot pays nothing");
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

        // --- The event window (Mehmet, 14 September 2026) ---------------------------------

        [Test]
        public void AClosedWindowRefusesToStartARecording()
        {
            var window = new FakeWindow { IsOpen = false };
            var session = Session(window);

            Assert.AreEqual(PlayerActionResult.InvalidTarget, session.TryStart(ActiveDive(), Alice, 1),
                "the plankton are not there to film");
            Assert.IsFalse(session.IsRecording(Alice));
            Assert.AreEqual(0, session.OpenTakeCount);
        }

        // The condition is temporary, so the refusal must not be remembered: a diver who pressed
        // record a second too early has to succeed the moment the event appears.
        [Test]
        public void TheClosedWindowRefusalIsNotRememberedSoTheSameDiverCanFilmWhenItOpens()
        {
            var window = new FakeWindow { IsOpen = false };
            var session = Session(window);
            var dive = ActiveDive();
            Assert.AreEqual(PlayerActionResult.InvalidTarget, session.TryStart(dive, Alice, 1));

            window.IsOpen = true;

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1),
                "the same request must work once the window is open");
            Assert.IsTrue(session.IsRecording(Alice));
        }

        [Test]
        public void AnOpenWindowBehavesExactlyLikeASubjectWithNoWindow()
        {
            var session = Session(new FakeWindow { IsOpen = true });
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Film(session, Alice, 5f, 0.8f);
            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var take));

            Assert.AreEqual(5f, take.ValidSeconds, 0.0001f);
            Assert.IsTrue(take.IsPayable);
        }

        // Mehmet's decision, and the reason this seam exists at all: closing the window freezes
        // a running take, it does not throw it away. Eight good seconds stay eight good seconds.
        [Test]
        public void WhenTheWindowClosesMidTakeTheEarnedSecondsSurvive()
        {
            var window = new FakeWindow { IsOpen = true };
            var session = Session(window);
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 6f, 0.8f);

            window.IsOpen = false;

            Assert.IsTrue(session.IsRecording(Alice), "the take is frozen, not dropped");
            Assert.AreEqual(6f, session.ValidSecondsFor(Alice), 0.0001f,
                "every second earned inside the window is kept");
        }

        [Test]
        public void NoTimeIsBankedWhileTheWindowIsShutHoweverLongItIsTicked()
        {
            var window = new FakeWindow { IsOpen = true };
            var session = Session(window);
            session.TryStart(ActiveDive(), Alice, 1);
            Film(session, Alice, 6f, 0.8f);

            window.IsOpen = false;
            Film(session, Alice, 30f, 1f);

            Assert.AreEqual(6f, session.ValidSecondsFor(Alice), 0.0001f,
                "a diver cannot keep filming an event that is over");
        }

        [Test]
        public void ATakeStoppedAfterTheWindowClosedIsStillGradedAndPayable()
        {
            var window = new FakeWindow { IsOpen = true };
            var session = Session(window);
            var dive = ActiveDive();
            session.TryStart(dive, Alice, 1);
            Film(session, Alice, 6f, 0.8f);
            window.IsOpen = false;

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStop(dive, Alice, 2, out var take));
            Assert.AreEqual(6f, take.ValidSeconds, 0.0001f);
            Assert.AreEqual(3, take.Quality, "0.8 held for 6s is Gold on this ladder");
            Assert.IsTrue(take.IsPayable, "the shot was earned while the event was on screen");
        }

        [Test]
        public void ASubjectWithNoWindowIsNeverGatedByOne()
        {
            var session = Session();
            var dive = ActiveDive();

            Assert.AreEqual(PlayerActionResult.Accepted, session.TryStart(dive, Alice, 1));
            Film(session, Alice, 5f, 0.8f);

            Assert.AreEqual(5f, session.ValidSecondsFor(Alice), 0.0001f,
                "a fish is filmable whenever the diver can see it");
        }
    }
}
