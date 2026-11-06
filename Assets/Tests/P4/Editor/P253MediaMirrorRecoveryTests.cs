using DeepDive.Composition;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P253MediaMirrorRecoveryTests
    {
        [TestCase(true, true, false, true, false, 10.0, 0.0, true)]
        [TestCase(true, true, false, true, false, 10.0, 12.0, false)]
        [TestCase(true, true, false, true, true, 10.0, 0.0, false)]
        [TestCase(true, false, false, true, false, 10.0, 0.0, false)]
        [TestCase(true, true, false, false, false, 10.0, 0.0, false)]
        [TestCase(true, true, true, true, false, 10.0, 0.0, false)]
        [TestCase(false, true, false, true, false, 10.0, 0.0, false)]
        [TestCase(true, true, false, true, false, 12.0, 12.0, true)]
        public void OnlyConnectedClientsWithoutSnapshotsAskHostToResend(
            bool listening, bool registered, bool server, bool connected, bool hasMirror,
            double now, double nextAt, bool expected)
        {
            Assert.AreEqual(expected, MediaNetworkBinding.MirrorRequestDue(
                listening, registered, server, connected, hasMirror, now, nextAt));
        }

        [Test]
        public void MirrorRequestNeverFiresForNonFiniteTime()
        {
            Assert.IsFalse(MediaNetworkBinding.MirrorRequestDue(true, true, false, true, false,
                double.NaN, 0));
            Assert.IsFalse(MediaNetworkBinding.MirrorRequestDue(true, true, false, true, false,
                double.PositiveInfinity, 0));
        }

        [TestCase(4, 3, true, true)]
        [TestCase(3, 4, true, false)]
        [TestCase(4, 4, true, true)]
        [TestCase(0, -1, false, true)]
        [TestCase(-1, 4, true, false)]
        public void OutOfOrderOrInvalidMirrorCannotEraseNewerArchive(
            int incoming, int existing, bool hasMirror, bool expected)
        {
            Assert.AreEqual(expected,
                MediaNetworkBinding.AcceptMirrorRevision(incoming, existing, hasMirror));
        }
    }
}
