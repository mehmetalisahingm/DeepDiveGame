using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public class P4RecordingCameraTierTests
    {
        private sealed class Target : IRecordingTarget
        {
        }

        [Test]
        public void RecordingCandidateCarriesHostResolvedCameraTier()
        {
            var target = new Target();
            var candidate = new RecordingCandidate(7, "dive-1", new PlayerId(3), target, CameraTier.Professional);

            Assert.AreEqual(CameraTier.Professional, candidate.CameraTier);
            Assert.AreSame(target, candidate.Target);
            Assert.AreEqual((ulong)3, candidate.PlayerId.Value);
        }

        [Test]
        public void LegacyCandidateConstructionDefaultsTierToNone()
        {
            var candidate = new RecordingCandidate(8, "dive-2", new PlayerId(4), new Target());
            Assert.AreEqual(CameraTier.None, candidate.CameraTier);
        }
    }
}
