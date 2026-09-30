using System;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    // #101 processed-state ledger: the first result per recording id is kept (never overwritten), Empty is a
    // valid terminal result, a blank id or a disallowed band is refused, and reading allocates nothing.
    public class RecordingWorldContextLedgerTests
    {
        private static readonly RecordingWorldContext Shallow = new RecordingWorldContext(RecordingSubjectKind.Species,
            ExplorationIds.NearRegionId, ExplorationIds.CellId(ExplorationIds.NearRegionId, 0, 0), DepthBandIds.Shallow, true);

        private static readonly RecordingWorldContext EventPlace = new RecordingWorldContext(RecordingSubjectKind.Event,
            ExplorationIds.NearRegionId, ExplorationIds.CellId(ExplorationIds.NearRegionId, 1, 0), DepthBandIds.Shallow, false);

        private static void AssertSame(in RecordingWorldContext actual, in RecordingWorldContext expected)
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.RegionId, Is.EqualTo(expected.RegionId));
            Assert.That(actual.CellId, Is.EqualTo(expected.CellId));
            Assert.That(actual.DepthBandId, Is.EqualTo(expected.DepthBandId));
            Assert.That(actual.FirstRecordingOfSubject, Is.EqualTo(expected.FirstRecordingOfSubject));
        }

        [Test]
        public void Complete_FirstTime_IsCompleted_AndResolves()
        {
            var ledger = new RecordingWorldContextLedger();
            Assert.That(ledger.Complete("rec-1", Shallow), Is.EqualTo(RecordingContextCompleteOutcome.Completed));
            Assert.That(ledger.TryResolve("rec-1", out var c), Is.True);
            AssertSame(c, Shallow);
            Assert.That(ledger.Count, Is.EqualTo(1));
        }

        [Test]
        public void Complete_SameIdTwice_IsAlreadyCompleted()
        {
            var ledger = new RecordingWorldContextLedger();
            ledger.Complete("rec-1", Shallow);
            Assert.That(ledger.Complete("rec-1", Shallow), Is.EqualTo(RecordingContextCompleteOutcome.AlreadyCompleted));
            Assert.That(ledger.Count, Is.EqualTo(1));
        }

        [Test]
        public void Complete_DifferentContext_DoesNotOverwrite()
        {
            var ledger = new RecordingWorldContextLedger();
            ledger.Complete("rec-1", Shallow);
            Assert.That(ledger.Complete("rec-1", EventPlace), Is.EqualTo(RecordingContextCompleteOutcome.AlreadyCompleted));
            Assert.That(ledger.TryResolve("rec-1", out var c), Is.True);
            AssertSame(c, Shallow);
        }

        [Test]
        public void Complete_Empty_IsTerminal()
        {
            var ledger = new RecordingWorldContextLedger();
            Assert.That(ledger.Complete("rec-1", RecordingWorldContext.Empty), Is.EqualTo(RecordingContextCompleteOutcome.Completed));
            Assert.That(ledger.TryResolve("rec-1", out var c), Is.True, "processed, even though the result is Empty");
            AssertSame(c, RecordingWorldContext.Empty);

            Assert.That(ledger.Complete("rec-1", Shallow), Is.EqualTo(RecordingContextCompleteOutcome.AlreadyCompleted));
            ledger.TryResolve("rec-1", out var still);
            AssertSame(still, RecordingWorldContext.Empty);
        }

        [Test]
        public void Complete_BlankId_IsInvalid()
        {
            var ledger = new RecordingWorldContextLedger();
            foreach (var id in new[] { null, "", " " })
                Assert.That(ledger.Complete(id, Shallow), Is.EqualTo(RecordingContextCompleteOutcome.Invalid), id ?? "null");
            Assert.That(ledger.Count, Is.EqualTo(0));
        }

        [Test]
        public void Complete_InvalidBand_IsInvalid()
        {
            var ledger = new RecordingWorldContextLedger();
            var blank = new RecordingWorldContext(RecordingSubjectKind.Species, "r", "c", " ", false);
            var unknown = new RecordingWorldContext(RecordingSubjectKind.Event, "r", "c", "abyss", false);

            Assert.That(ledger.Complete("rec-1", blank), Is.EqualTo(RecordingContextCompleteOutcome.Invalid));
            Assert.That(ledger.Complete("rec-1", unknown), Is.EqualTo(RecordingContextCompleteOutcome.Invalid));
            Assert.That(ledger.TryResolve("rec-1", out _), Is.False, "nothing stored");

            Assert.That(ledger.Complete("rec-1", Shallow), Is.EqualTo(RecordingContextCompleteOutcome.Completed));
        }

        [Test]
        public void TryResolve_Unprocessed_IsFalse_AndDoesNotAllocate()
        {
            var ledger = new RecordingWorldContextLedger();
            ledger.Complete("rec-1", Shallow);

            Assert.That(ledger.TryResolve("rec-2", out var missing), Is.False);
            AssertSame(missing, RecordingWorldContext.Empty);
            Assert.That(ledger.TryResolve(null, out _), Is.False);

            ledger.TryResolve("rec-1", out _); // warm up (JIT)
            var before = GC.GetAllocatedBytesForCurrentThread();
            var hits = 0;
            for (var i = 0; i < 10000; i++)
            {
                if (ledger.TryResolve("rec-1", out _)) hits++;
                ledger.TryResolve("rec-2", out _);
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes (hits {hits})");
        }
    }
}
