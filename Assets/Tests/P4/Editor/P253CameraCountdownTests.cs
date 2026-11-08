using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P253CameraCountdownTests
    {
        [TestCase(CameraTier.Basic)]
        [TestCase(CameraTier.Advanced)]
        [TestCase(CameraTier.Professional)]
        public void EveryCameraTierUsesItsRealServerRecordingLimit(CameraTier tier)
        {
            var limit = CameraRecordingLimits.MaxSeconds(tier);
            var standby = RecordingCountdownRules.Evaluate(tier, false, 999f);
            var started = RecordingCountdownRules.Evaluate(tier, true, 0f);
            Assert.AreEqual((int)limit, standby.LimitSeconds);
            Assert.AreEqual((int)limit, standby.RemainingSeconds);
            Assert.AreEqual(1f, standby.RemainingRatio);
            Assert.IsFalse(standby.WarnLastSeconds);
            Assert.IsFalse(standby.ReachedLimit);
            Assert.AreEqual((int)limit, started.RemainingSeconds);
            Assert.AreEqual(1f, started.RemainingRatio);
            Assert.IsFalse(started.ReachedLimit);
        }

        [Test]
        public void OnlyLastFiveSecondsTriggerWarningAndLimitClampsToZero()
        {
            var limit = CameraRecordingLimits.MaxSeconds(CameraTier.Basic);
            var early = RecordingCountdownRules.Evaluate(CameraTier.Basic, true, limit - 6f);
            Assert.IsFalse(early.WarnLastSeconds);
            Assert.Greater(early.RemainingRatio, 0f);

            var warning = RecordingCountdownRules.Evaluate(CameraTier.Basic, true, limit - 4.1f);
            Assert.AreEqual(5, warning.RemainingSeconds);
            Assert.IsTrue(warning.WarnLastSeconds);
            Assert.IsFalse(warning.ReachedLimit);

            var nearEnd = RecordingCountdownRules.Evaluate(CameraTier.Basic, true, limit - .1f);
            Assert.AreEqual(1, nearEnd.RemainingSeconds);
            Assert.IsTrue(nearEnd.WarnLastSeconds);

            var ended = RecordingCountdownRules.Evaluate(CameraTier.Basic, true, limit + 80f);
            Assert.AreEqual(0, ended.RemainingSeconds);
            Assert.AreEqual(0f, ended.RemainingRatio);
            Assert.IsFalse(ended.WarnLastSeconds);
            Assert.IsTrue(ended.ReachedLimit);
        }

        [Test]
        public void BadOrEarlyElapsedTimeDoesNotCreateFakeExpiredRecording()
        {
            var tier = CameraTier.Advanced;
            foreach (var invalid in new[] { -100f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
            {
                var view = RecordingCountdownRules.Evaluate(tier, true, invalid);
                Assert.AreEqual((int)CameraRecordingLimits.MaxSeconds(tier), view.RemainingSeconds);
                Assert.AreEqual(1f, view.RemainingRatio);
                Assert.IsFalse(view.ReachedLimit);
            }
        }
    }
}
