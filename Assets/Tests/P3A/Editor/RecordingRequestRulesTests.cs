using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public class RecordingRequestRulesTests
    {
        private sealed class FakeSink : IRecordingEvaluationSink
        {
            public RecordingCandidate Last;
            public int Calls;
            public PlayerActionResult Result = PlayerActionResult.Accepted;

            public PlayerActionResult TrySubmit(RecordingCandidate candidate)
            {
                Last = candidate;
                Calls++;
                return Result;
            }
        }

        private FakeSink bound;

        [TearDown]
        public void TearDown()
        {
            if (bound != null) RecordingEvaluation.Unbind(bound);
            bound = null;
        }

        [Test]
        public void ZeroOrStaleRequestIdIsRejectedAsDuplicate()
        {
            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                RecordingRequestRules.Validate(0, 0, RecordingCommand.Start, false, true));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                RecordingRequestRules.Validate(4, 4, RecordingCommand.Start, false, true));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                RecordingRequestRules.Validate(3, 4, RecordingCommand.Start, false, true));
        }

        [Test]
        public void StartRequiresATarget()
        {
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                RecordingRequestRules.Validate(1, 0, RecordingCommand.Start, false, false));
        }

        [Test]
        public void StartWhileAlreadyRecordingIsRejected()
        {
            Assert.AreEqual(PlayerActionResult.InvalidState,
                RecordingRequestRules.Validate(2, 1, RecordingCommand.Start, true, true));
        }

        [Test]
        public void StopWhileNotRecordingIsRejected()
        {
            Assert.AreEqual(PlayerActionResult.InvalidState,
                RecordingRequestRules.Validate(2, 1, RecordingCommand.Stop, false, true));
        }

        [Test]
        public void ValidStartAndStopTransitionsReachEvaluatorShape()
        {
            Assert.AreEqual(PlayerActionResult.Accepted,
                RecordingRequestRules.Validate(1, 0, RecordingCommand.Start, false, true));
            Assert.AreEqual(PlayerActionResult.Accepted,
                RecordingRequestRules.Validate(2, 1, RecordingCommand.Stop, true, true));
        }

        [Test]
        public void MissingEvaluatorBlocksRecordingInsteadOfInventingAResult()
        {
            var candidate = new RecordingCandidate(1, "dive-1", new PlayerId(7), true, 55,
                RecordingCommand.Start);
            Assert.AreEqual(PlayerActionResult.InvalidState, RecordingEvaluation.TrySubmit(candidate));
        }

        [Test]
        public void BoundEvaluatorReceivesExactCandidate()
        {
            bound = new FakeSink();
            RecordingEvaluation.Bind(bound);
            var candidate = new RecordingCandidate(9, "dive-42", new PlayerId(3), true, 1234,
                RecordingCommand.Start);

            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TrySubmit(candidate));
            Assert.AreEqual(1, bound.Calls);
            Assert.AreEqual((ulong)9, bound.Last.RequestId);
            Assert.AreEqual("dive-42", bound.Last.DiveId);
            Assert.AreEqual(new PlayerId(3), bound.Last.PlayerId);
            Assert.IsTrue(bound.Last.HasTarget);
            Assert.AreEqual((ulong)1234, bound.Last.TargetNetworkObjectId);
            Assert.AreEqual(RecordingCommand.Start, bound.Last.Command);
        }

        [Test]
        public void UnbindingADifferentSinkDoesNotEraseCurrentEvaluator()
        {
            bound = new FakeSink();
            var other = new FakeSink();
            RecordingEvaluation.Bind(bound);
            RecordingEvaluation.Unbind(other);

            var candidate = new RecordingCandidate(1, "dive-1", new PlayerId(1), true, 2,
                RecordingCommand.Start);
            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TrySubmit(candidate));
            Assert.AreEqual(1, bound.Calls);
        }
    }
}
