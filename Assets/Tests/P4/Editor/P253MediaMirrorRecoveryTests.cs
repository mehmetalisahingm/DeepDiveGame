using DeepDive.Composition;
using DeepDive.Core.Contracts;
using System.Collections.Generic;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P253MediaMirrorRecoveryTests
    {
        [Test]
        public void ArchivePagesExposeEveryClipExactlyOnceNewestFirst()
        {
            Assert.AreEqual(1, HomePcView.ArchivePageCount(0));
            Assert.AreEqual(-1, HomePcView.ArchiveIndex(0, 0, 0));
            foreach (var count in new[] { 1, 9, 10, 18, 19, 128 })
            {
                var seen = new List<int>();
                for (var page = 0; page < HomePcView.ArchivePageCount(count); page++)
                    for (var row = 0; row < HomePcView.ArchivePageSize; row++)
                    {
                        var index = HomePcView.ArchiveIndex(count, page, row);
                        if (index >= 0) seen.Add(index);
                    }
                Assert.AreEqual(count, seen.Count);
                for (var i = 0; i < count; i++) Assert.AreEqual(count - 1 - i, seen[i]);
            }
            Assert.AreEqual(-1, HomePcView.ArchiveIndex(10, 2, 0));
            Assert.AreEqual(-1, HomePcView.ArchiveIndex(10, 0, -1));
        }

        [Test]
        public void ArchiveBudgetLabelUsesHostManifestSizesAndDoesNotClampLegacyUsage()
        {
            Assert.AreEqual("ARSIV 0/128 | 0.0/1024 MiB", HomePcView.ArchiveBudgetLabel(null));
            var clips = new List<ClipSave> { new ClipSave { SizeBytes = 1024L * 1024 + 512 * 1024 },
                new ClipSave { SizeBytes = 1024L * 1024 * 1024 } };
            Assert.AreEqual("ARSIV 2/128 | 1025.5/1024 MiB", HomePcView.ArchiveBudgetLabel(clips));
        }

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
