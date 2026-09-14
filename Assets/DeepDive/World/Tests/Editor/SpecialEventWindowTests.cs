using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class SpecialEventWindowTests
    {
        private const float Duration = 10f;

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        private static FakeDive ActiveDive(string diveId = "dive-1") =>
            new FakeDive { IsDiveActive = true, CurrentDiveId = diveId };

        private static SpecialEventWindow Window(float durationSeconds = Duration) =>
            new SpecialEventWindow(durationSeconds);

        // Runs the host clock for the given seconds, one tick at a time, and reports whether the
        // window closed somewhere in there.
        private static bool Run(SpecialEventWindow window, float seconds, float tick = 0.5f)
        {
            var closed = false;
            for (var elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += tick)
                closed |= window.Tick(tick);
            return closed;
        }

        [Test]
        public void ANewWindowIsWaitingAndNothingIsHappeningYet()
        {
            var window = Window();

            Assert.AreEqual(SpecialEventState.Pending, window.State);
            Assert.IsFalse(window.IsActive);
            Assert.AreEqual("", window.DiveId);
            Assert.AreEqual(0f, window.ElapsedSeconds);
            Assert.AreEqual(0f, window.RemainingSeconds, "a window that never opened has nothing left to run");
        }

        [Test]
        public void BeginningStampsTheLiveDiveAndOpensTheWindow()
        {
            var window = Window();

            Assert.IsTrue(window.TryBegin(ActiveDive("dive-7")));
            Assert.AreEqual(SpecialEventState.Active, window.State);
            Assert.IsTrue(window.IsActive);
            Assert.AreEqual("dive-7", window.DiveId);
            Assert.AreEqual(Duration, window.RemainingSeconds, 0.0001f);
        }

        // Same refusal RecordingSession.TryStart makes, for the same reason: with no live dive
        // there is no dive id to stamp, and an event nobody can attribute to a dive could never
        // produce a payable recording.
        [Test]
        public void WithoutALiveDiveNothingBegins()
        {
            Assert.IsFalse(Window().TryBegin(null), "no dive context at all");
            Assert.IsFalse(Window().TryBegin(new FakeDive { IsDiveActive = false, CurrentDiveId = "dive-1" }),
                "the dive is over");
            Assert.IsFalse(Window().TryBegin(new FakeDive { IsDiveActive = true, CurrentDiveId = "  " }),
                "an active dive with no id to stamp");
        }

        // Not remembered as a refusal: a trigger that fired too early must still be able to open
        // the window once a dive is actually running.
        [Test]
        public void ATriggerThatFiredTooEarlyStillWorksOnceTheDiveIsLive()
        {
            var window = Window();
            Assert.IsFalse(window.TryBegin(new FakeDive { IsDiveActive = false }));

            Assert.IsTrue(window.TryBegin(ActiveDive()));
            Assert.AreEqual(SpecialEventState.Active, window.State);
        }

        // The contract the whole trigger/tick split exists for: the caller says "begin", and the
        // host's own clock says how long that lasted. There is no parameter that could carry a
        // claim about duration.
        [Test]
        public void TheWindowLastsAsLongAsTheHostTickedAndNothingTheCallerCouldClaim()
        {
            var window = Window();
            window.TryBegin(ActiveDive());

            for (var i = 0; i < 12; i++) window.Tick(0.25f);

            Assert.AreEqual(3f, window.ElapsedSeconds, 0.0001f, "12 host ticks of 0.25s is 3 seconds");
            Assert.AreEqual(Duration - 3f, window.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void TheWindowStaysOpenUntilItsDurationIsSpent()
        {
            var window = Window();
            window.TryBegin(ActiveDive());

            Assert.IsFalse(Run(window, Duration - 1f), "the window is not up yet");
            Assert.IsTrue(window.IsActive);

            Assert.IsTrue(Run(window, 2f), "the window closes once its time is spent");
            Assert.AreEqual(SpecialEventState.Finished, window.State);
            Assert.IsFalse(window.IsActive);
        }

        // The edge the host-side shell hangs off: reported once, by the tick that closed the
        // window, so the shell never has to diff the state itself. What it does with that edge -
        // to an open take above all - is a separate decision and not this class's business.
        [Test]
        public void OnlyTheTickThatClosesTheWindowReportsTheEdge()
        {
            var window = Window(1f);
            window.TryBegin(ActiveDive());

            Assert.IsFalse(window.Tick(0.5f), "still running");
            Assert.IsTrue(window.Tick(0.5f), "this tick spent the last of it");
            Assert.IsFalse(window.Tick(0.5f), "the window is already closed; the edge is not reported twice");
        }

        [Test]
        public void TicksBeforeTheTriggerChangeNothing()
        {
            var window = Window();

            Assert.IsFalse(window.Tick(5f));
            Assert.AreEqual(SpecialEventState.Pending, window.State);
            Assert.AreEqual(0f, window.ElapsedSeconds, "a pending window banks no time");
        }

        // The once-per-dive rule, from both sides: while it is running and after it is spent.
        [Test]
        public void ItCannotBeTriggeredASecondTimeInTheSameDive()
        {
            var window = Window();
            var dive = ActiveDive();
            Assert.IsTrue(window.TryBegin(dive));

            Assert.IsFalse(window.TryBegin(dive), "it is already running");

            Run(window, Duration + 1f);
            Assert.AreEqual(SpecialEventState.Finished, window.State);
            Assert.IsFalse(window.TryBegin(dive), "it already had its turn this dive");
        }

        [Test]
        public void AFinishedWindowStaysFinishedHoweverLongItIsTicked()
        {
            var window = Window(1f);
            window.TryBegin(ActiveDive());
            Run(window, 2f);

            Assert.IsFalse(Run(window, 60f));
            Assert.AreEqual(SpecialEventState.Finished, window.State);
            Assert.AreEqual(1f, window.ElapsedSeconds, 0.0001f, "no time accrues after the window closed");
            Assert.AreEqual(0f, window.RemainingSeconds);
        }

        // The dive ended, or the thing the event lives on went away. The event has had its turn
        // either way: the once-per-dive rule does not care why the window closed.
        [Test]
        public void EndingEarlyClosesTheWindowForTheRestOfTheDive()
        {
            var window = Window();
            var dive = ActiveDive();
            window.TryBegin(dive);
            Run(window, 2f);

            window.End();

            Assert.AreEqual(SpecialEventState.Finished, window.State);
            Assert.IsFalse(window.TryBegin(dive));
            Assert.AreEqual(2f, window.ElapsedSeconds, 0.0001f, "the seconds it did run are left as they were");
        }

        // An early teardown must not silently burn the event for a dive it never appeared in.
        [Test]
        public void EndingAWindowThatNeverBeganLeavesItWaiting()
        {
            var window = Window();

            window.End();

            Assert.AreEqual(SpecialEventState.Pending, window.State);
            Assert.IsTrue(window.TryBegin(ActiveDive()), "it never had its turn, so it still gets one");
        }

        [Test]
        public void ResetOpensTheEventAgainForTheNextDive()
        {
            var window = Window(1f);
            window.TryBegin(ActiveDive("dive-1"));
            Run(window, 2f);

            window.Reset();

            Assert.AreEqual(SpecialEventState.Pending, window.State);
            Assert.AreEqual("", window.DiveId, "no stamp from the old dive may survive into the next one");
            Assert.AreEqual(0f, window.ElapsedSeconds);
            Assert.IsTrue(window.TryBegin(ActiveDive("dive-2")));
            Assert.AreEqual("dive-2", window.DiveId);
        }

        // What lets the host-side shell notice a window that outlived its dive, the same way
        // RecordingSession voids a take stamped with a dive that is no longer the live one.
        [Test]
        public void TheStampedDiveIdIsWhatShowsAWindowOutlivedItsDive()
        {
            var window = Window();
            window.TryBegin(ActiveDive("dive-1"));

            var nextDive = ActiveDive("dive-2");

            Assert.AreNotEqual(nextDive.CurrentDiveId, window.DiveId);
            Assert.IsTrue(window.IsActive, "the window itself cannot see the dive change; the shell ends it");
        }

        [Test]
        public void UnusableTicksAreIgnoredInsteadOfClosingTheWindowEarly()
        {
            var window = Window();
            window.TryBegin(ActiveDive());

            Assert.IsFalse(window.Tick(float.NaN));
            Assert.IsFalse(window.Tick(float.PositiveInfinity));
            Assert.IsFalse(window.Tick(0f));
            Assert.IsFalse(window.Tick(-5f));

            Assert.AreEqual(0f, window.ElapsedSeconds, "nonsense never becomes event time");
            Assert.AreEqual(SpecialEventState.Active, window.State);
            Assert.AreEqual(Duration, window.RemainingSeconds, 0.0001f);
        }

        [Test]
        public void ElapsedNeverRunsPastTheDurationSoNothingGoesNegative()
        {
            var window = Window(2f);
            window.TryBegin(ActiveDive());

            Assert.IsTrue(window.Tick(30f), "one huge host frame still only spends the window once");
            Assert.AreEqual(2f, window.ElapsedSeconds, 0.0001f, "the overshoot is the host's frame, not event time");
            Assert.AreEqual(0f, window.RemainingSeconds);
        }

        // A window that could never be open is a setup fault, not a decision about a trigger.
        // Refused rather than quietly replaced: any fallback duration here would be a design
        // number nobody agreed on.
        [Test]
        public void AWindowWithNoRealDurationCanNeverBeTriggered()
        {
            foreach (var durationSeconds in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                var window = Window(durationSeconds);
                Assert.IsFalse(window.IsValid, "duration " + durationSeconds + " is not a window");
                Assert.IsFalse(window.TryBegin(ActiveDive()), "duration " + durationSeconds + " must not open");
                Assert.AreEqual(SpecialEventState.Pending, window.State);
            }
        }

        // Guards the placeholder without freezing it as a decision: how long the event stays
        // open is Mehmet's call, and when it lands the number moves to the event definition
        // asset. All this asserts is that the stand-in is constructible, not that it is right.
        [Test]
        public void ThePlaceholderDurationIsUsableButIsNotTheAgreedNumberYet()
        {
            var window = Window(SpecialEventWindow.PlaceholderDurationSeconds);

            Assert.IsTrue(window.IsValid);
            Assert.IsTrue(window.TryBegin(ActiveDive()));
        }
    }
}
