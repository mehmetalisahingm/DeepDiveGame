using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class SpecialEventScheduleTests
    {
        private const float Delay = 45f;

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        private static FakeDive ActiveDive(string diveId = "dive-1") =>
            new FakeDive { IsDiveActive = true, CurrentDiveId = diveId };

        private static SpecialEventSchedule Schedule(float delaySeconds = Delay) =>
            new SpecialEventSchedule(delaySeconds);

        // Runs the host clock for the given seconds and reports whether the delay was spent
        // somewhere in there.
        private static bool Run(SpecialEventSchedule schedule, IDiveContext dive, float seconds,
            float tick = 0.5f)
        {
            var fired = false;
            for (var elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += tick)
                fired |= schedule.Tick(dive, tick);
            return fired;
        }

        [Test]
        public void ANewScheduleHasNotStartedCounting()
        {
            var schedule = Schedule();

            Assert.AreEqual(0f, schedule.ElapsedSeconds);
            Assert.AreEqual("", schedule.DiveId);
            Assert.IsFalse(schedule.HasFired);
            Assert.AreEqual(Delay, schedule.RemainingSeconds, 0.0001f);
        }

        // Same discipline as every other duration in the recording slice: the host's own delta
        // is what accrues, and no caller can declare that the delay is up.
        [Test]
        public void TheDelayIsWhateverTheHostTickedAndNothingTheCallerCouldClaim()
        {
            var schedule = Schedule();
            var dive = ActiveDive();

            for (var i = 0; i < 12; i++) schedule.Tick(dive, 0.25f);

            Assert.AreEqual(3f, schedule.ElapsedSeconds, 0.0001f, "12 host ticks of 0.25s is 3 seconds");
            Assert.AreEqual(Delay - 3f, schedule.RemainingSeconds, 0.0001f);
            Assert.IsFalse(schedule.HasFired);
        }

        [Test]
        public void ItFiresOnTheTickThatSpendsTheDelay()
        {
            var schedule = Schedule(1f);
            var dive = ActiveDive();

            Assert.IsFalse(schedule.Tick(dive, 0.5f), "not yet");
            Assert.IsTrue(schedule.Tick(dive, 0.5f), "this tick spent the delay");
            Assert.IsTrue(schedule.HasFired);
            Assert.AreEqual(0f, schedule.RemainingSeconds);
        }

        // "Dalis basina bir kez": the shell keeps ticking for the rest of the dive and must not
        // be told to start the event again.
        [Test]
        public void ItFiresExactlyOncePerDive()
        {
            var schedule = Schedule(1f);
            var dive = ActiveDive();
            Assert.IsTrue(Run(schedule, dive, 2f));

            Assert.IsFalse(Run(schedule, dive, 120f), "one plankton bloom per dive");
            Assert.AreEqual(1f, schedule.ElapsedSeconds, 0.0001f, "no time accrues after it fired");
        }

        [Test]
        public void WithoutALiveDiveNothingIsCounted()
        {
            var schedule = Schedule();

            Assert.IsFalse(Run(schedule, null, 60f), "no dive context at all");
            Assert.IsFalse(Run(schedule, new FakeDive { IsDiveActive = false }, 60f), "no dive running");
            Assert.IsFalse(Run(schedule, new FakeDive { IsDiveActive = true, CurrentDiveId = "  " }, 60f),
                "a dive with no id to count against");

            Assert.AreEqual(0f, schedule.ElapsedSeconds);
        }

        // The delay is measured from the start of a dive, so a dive that ended halfway through
        // leaves nothing for the next one to inherit - otherwise the second dive's event would
        // appear early, or instantly.
        [Test]
        public void ADiveThatEndedPartWayThroughLeavesNothingBehind()
        {
            var schedule = Schedule();
            var dive = ActiveDive();
            Run(schedule, dive, 30f);
            Assert.AreEqual(30f, schedule.ElapsedSeconds, 0.0001f);

            dive.IsDiveActive = false;
            schedule.Tick(dive, 0.5f);

            Assert.AreEqual(0f, schedule.ElapsedSeconds, "the countdown is dropped, not paused");
            Assert.AreEqual("", schedule.DiveId);
        }

        [Test]
        public void ANewDiveStartsTheCountdownOver()
        {
            var schedule = Schedule(1f);
            Assert.IsTrue(Run(schedule, ActiveDive("dive-1"), 2f));

            var nextDive = ActiveDive("dive-2");
            Assert.IsFalse(schedule.Tick(nextDive, 0.5f), "the new dive's delay has not been spent");
            Assert.AreEqual("dive-2", schedule.DiveId);
            Assert.IsFalse(schedule.HasFired);

            Assert.IsTrue(schedule.Tick(nextDive, 0.5f), "and now it has");
        }

        [Test]
        public void UnusableTicksAreIgnoredInsteadOfFiringEarly()
        {
            var schedule = Schedule(1f);
            var dive = ActiveDive();

            Assert.IsFalse(schedule.Tick(dive, float.NaN));
            Assert.IsFalse(schedule.Tick(dive, float.PositiveInfinity));
            Assert.IsFalse(schedule.Tick(dive, 0f));
            Assert.IsFalse(schedule.Tick(dive, -5f));

            Assert.AreEqual(0f, schedule.ElapsedSeconds, "nonsense never becomes waiting time");
            Assert.IsFalse(schedule.HasFired);
        }

        [Test]
        public void ElapsedNeverRunsPastTheDelay()
        {
            var schedule = Schedule(2f);

            Assert.IsTrue(schedule.Tick(ActiveDive(), 30f), "one huge host frame still fires once");
            Assert.AreEqual(2f, schedule.ElapsedSeconds, 0.0001f,
                "the overshoot is the host's frame, not a longer wait");
        }

        // A setup fault, not a schedule: refused rather than replaced with an invented delay,
        // the same way SpecialEventWindow refuses a duration it was never given.
        [Test]
        public void AnInvalidDelayNeverFires()
        {
            foreach (var delaySeconds in new[] { -1f, float.NaN, float.PositiveInfinity })
            {
                var schedule = Schedule(delaySeconds);
                Assert.IsFalse(schedule.IsValid, "delay " + delaySeconds + " is not a schedule");
                Assert.IsFalse(Run(schedule, ActiveDive(), 120f), "delay " + delaySeconds + " must never fire");
            }
        }

        // Zero is a legal delay and means "as soon as the dive is running". It is not the agreed
        // number - that is 45 and lives in the event definition - but the rule must handle it.
        [Test]
        public void AZeroDelayFiresOnTheFirstTickOfTheDive()
        {
            var schedule = Schedule(0f);

            Assert.IsTrue(schedule.IsValid);
            Assert.IsTrue(schedule.Tick(ActiveDive(), 0.02f));
        }

        [Test]
        public void ResetPutsItBackToTheStartOfTheCountdown()
        {
            var schedule = Schedule(1f);
            var dive = ActiveDive("dive-1");
            Run(schedule, dive, 2f);

            schedule.Reset();

            Assert.IsFalse(schedule.HasFired);
            Assert.AreEqual(0f, schedule.ElapsedSeconds);
            Assert.AreEqual("", schedule.DiveId);
            Assert.IsTrue(Run(schedule, dive, 2f), "it may fire again in a fresh countdown");
        }
    }
}
