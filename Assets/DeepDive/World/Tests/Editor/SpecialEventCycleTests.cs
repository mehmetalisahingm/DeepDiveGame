using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class SpecialEventCycleTests
    {
        // The agreed numbers (Mehmet, 14 September 2026), written out here rather than read from
        // the asset so a retune of the asset cannot quietly change what these tests claim.
        private const float Delay = 45f;
        private const float Window = 20f;

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        private static FakeDive ActiveDive(string diveId = "dive-1") =>
            new FakeDive { IsDiveActive = true, CurrentDiveId = diveId };

        private static SpecialEventCycle Cycle(float delaySeconds = Delay, float windowSeconds = Window) =>
            new SpecialEventCycle(new SpecialEventSchedule(delaySeconds), new SpecialEventWindow(windowSeconds));

        // Runs the host clock for the given seconds, one fixed tick at a time.
        private static void Run(SpecialEventCycle cycle, IDiveContext dive, float seconds, float tick = 0.5f)
        {
            for (var elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += tick)
                cycle.Tick(dive, tick);
        }

        [Test]
        public void NothingIsOnScreenBeforeTheDelayIsSpent()
        {
            var cycle = Cycle();

            Run(cycle, ActiveDive(), Delay - 1f);

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Pending, cycle.State);
        }

        [Test]
        public void TheEventAppearsOnceTheDelayIsSpent()
        {
            var cycle = Cycle();
            var dive = ActiveDive("dive-7");

            Run(cycle, dive, Delay + 1f);

            Assert.IsTrue(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Active, cycle.State);
            Assert.AreEqual("dive-7", cycle.DiveId);
        }

        [Test]
        public void TheEventFadesAfterItsWindow()
        {
            var cycle = Cycle();
            var dive = ActiveDive();

            Run(cycle, dive, Delay + Window - 1f);
            Assert.IsTrue(cycle.IsOpen, "still on screen just before the window is up");

            Run(cycle, dive, 2f);
            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Finished, cycle.State);
        }

        // "Dalis basina bir kez": the host keeps ticking for the rest of the dive and the event
        // must not come back.
        [Test]
        public void ItAppearsOnlyOncePerDive()
        {
            var cycle = Cycle();
            var dive = ActiveDive();
            Run(cycle, dive, Delay + Window + 1f);

            Run(cycle, dive, 300f);

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Finished, cycle.State);
        }

        [Test]
        public void ANewDiveGetsItsOwnAppearance()
        {
            var cycle = Cycle();
            Run(cycle, ActiveDive("dive-1"), Delay + Window + 1f);

            var nextDive = ActiveDive("dive-2");
            Run(cycle, nextDive, Delay - 1f);
            Assert.IsFalse(cycle.IsOpen, "the new dive waits its own 45 seconds");

            Run(cycle, nextDive, 2f);
            Assert.IsTrue(cycle.IsOpen);
            Assert.AreEqual("dive-2", cycle.DiveId);
        }

        // The dive ended while the plankton were on screen. Nothing may be carried over: the
        // next dive must not open the event early or instantly.
        [Test]
        public void ADiveThatEndedMidAppearanceLeavesNothingBehind()
        {
            var cycle = Cycle();
            var dive = ActiveDive("dive-1");
            Run(cycle, dive, Delay + 5f);
            Assert.IsTrue(cycle.IsOpen);

            dive.IsDiveActive = false;
            cycle.Tick(dive, 0.5f);

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Pending, cycle.State);
            Assert.AreEqual("", cycle.DiveId);

            var nextDive = ActiveDive("dive-2");
            Run(cycle, nextDive, Delay - 1f);
            Assert.IsFalse(cycle.IsOpen, "the second dive starts its wait from scratch");
        }

        [Test]
        public void WithoutALiveDiveNothingRuns()
        {
            var cycle = Cycle();

            Run(cycle, null, 300f);
            Run(cycle, new FakeDive { IsDiveActive = false }, 300f);
            Run(cycle, new FakeDive { IsDiveActive = true, CurrentDiveId = "  " }, 300f);

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Pending, cycle.State);
        }

        // --- The retire guard (Mehmet: the target is not destroyed while a take is open) -----

        [Test]
        public void NothingMayBeRetiredWhileTheEventIsStillWaiting()
        {
            var cycle = Cycle();

            Assert.IsFalse(cycle.CanRetire(0), "it has not even appeared yet");
        }

        [Test]
        public void NothingMayBeRetiredWhileTheEventIsOnScreen()
        {
            var cycle = Cycle();
            Run(cycle, ActiveDive(), Delay + 1f);

            Assert.IsTrue(cycle.IsOpen);
            Assert.IsFalse(cycle.CanRetire(0));
        }

        // The one that matters: the window is over, but somebody is still holding a recording
        // open. Taking the object away here would run RecordingSubject.AbortAll and burn the
        // seconds they earned inside the window.
        [Test]
        public void NothingMayBeRetiredWhileATakeIsStillOpen()
        {
            var cycle = Cycle();
            Run(cycle, ActiveDive(), Delay + Window + 1f);
            Assert.AreEqual(SpecialEventState.Finished, cycle.State);

            Assert.IsFalse(cycle.CanRetire(1), "a diver is still filming; their seconds are not ours to burn");
            Assert.IsFalse(cycle.CanRetire(3));
        }

        [Test]
        public void RetiringIsOnlySafeOnceTheEventIsOverAndNobodyIsFilming()
        {
            var cycle = Cycle();
            Run(cycle, ActiveDive(), Delay + Window + 1f);

            Assert.IsTrue(cycle.CanRetire(0));
        }

        // --- Teardown and reuse --------------------------------------------------------------

        [Test]
        public void EndClosesARunningAppearanceAndItDoesNotComeBackThisDive()
        {
            var cycle = Cycle();
            var dive = ActiveDive();
            Run(cycle, dive, Delay + 5f);

            cycle.End();

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Finished, cycle.State);

            Run(cycle, dive, 300f);
            Assert.IsFalse(cycle.IsOpen, "the event has had its turn this dive");
        }

        [Test]
        public void EndOnAnEventThatNeverAppearedLeavesItWaiting()
        {
            var cycle = Cycle();
            var dive = ActiveDive();
            Run(cycle, dive, 10f);

            cycle.End();

            Assert.AreEqual(SpecialEventState.Pending, cycle.State);
            Run(cycle, dive, Delay + 1f);
            Assert.IsTrue(cycle.IsOpen, "it never had its turn, so it still gets one");
        }

        [Test]
        public void ResetPutsTheWholeCycleBackToWaiting()
        {
            var cycle = Cycle();
            var dive = ActiveDive();
            Run(cycle, dive, Delay + 5f);

            cycle.Reset();

            Assert.IsFalse(cycle.IsOpen);
            Assert.AreEqual(SpecialEventState.Pending, cycle.State);
            Assert.AreEqual("", cycle.DiveId);
            Assert.AreEqual(Delay, cycle.WaitRemainingSeconds, 0.0001f, "the wait starts over too");
        }

        // A definition nobody finished filling in must produce an event that never appears,
        // not one that appears on invented numbers.
        [Test]
        public void AMisconfiguredCycleNeverOpens()
        {
            var noWindow = Cycle(Delay, 0f);
            var noSchedule = Cycle(-1f, Window);
            var nothing = new SpecialEventCycle(null, null);

            foreach (var cycle in new[] { noWindow, noSchedule, nothing })
            {
                Assert.IsFalse(cycle.IsValid);
                Run(cycle, ActiveDive(), 300f);
                Assert.IsFalse(cycle.IsOpen);
                Assert.AreEqual(SpecialEventState.Pending, cycle.State);
            }
        }

        [Test]
        public void UnusableTicksAreIgnoredInsteadOfMovingTheEventAlong()
        {
            var cycle = Cycle(1f, Window);
            var dive = ActiveDive();

            cycle.Tick(dive, float.NaN);
            cycle.Tick(dive, float.PositiveInfinity);
            cycle.Tick(dive, 0f);
            cycle.Tick(dive, -5f);

            Assert.IsFalse(cycle.IsOpen, "nonsense never becomes waiting time");
            Assert.AreEqual(1f, cycle.WaitRemainingSeconds, 0.0001f);
        }
    }
}
