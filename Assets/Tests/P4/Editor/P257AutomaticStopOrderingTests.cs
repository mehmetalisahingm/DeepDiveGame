using DeepDive.Composition;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P257AutomaticStopOrderingTests
    {
        [TestCase(false, 0UL, 18UL, true)]
        [TestCase(false, 0UL, 0UL, true)]
        [TestCase(true, 18UL, 18UL, true)]
        [TestCase(true, 27UL, 18UL, false)]
        [TestCase(true, 18UL, 0UL, false)]
        [TestCase(true, 27UL, 0UL, false)]
        public void AutomaticStopNeverCancelsAnotherLiveCameraTake(
            bool recording, ulong currentTarget, ulong stoppedTarget, bool expected)
        {
            Assert.AreEqual(expected, RecordingNetworkBridge.ShouldApplyAutomaticStop(
                recording, currentTarget, stoppedTarget));
        }

        [Test]
        public void CollectedFishStopAndExpiredTimeStopUseTheSameTargetRule()
        {
            // Both host-generated Stop notices use the finished target ID.
            // The local owner can already have a newer StartResult for fish B.
            const ulong fishA = 731, fishB = 732;
            Assert.IsFalse(RecordingNetworkBridge.ShouldApplyAutomaticStop(true, fishB, fishA));
            Assert.IsTrue(RecordingNetworkBridge.ShouldApplyAutomaticStop(true, fishA, fishA));
            // An idle HUD may always accept a late terminal status; no capture
            // state or reward is written by presentation-only notifications.
            Assert.IsTrue(RecordingNetworkBridge.ShouldApplyAutomaticStop(false, 0, fishA));
        }
    }
}
