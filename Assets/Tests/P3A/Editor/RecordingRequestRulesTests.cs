using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public class RecordingRequestRulesTests
    {
        private sealed class FakeTarget : IRecordingTarget
        {
        }

        private sealed class FakeSink : IRecordingEvaluationSink
        {
            public RecordingCandidate Last;
            public RecordingCommand LastCommand;
            public int Calls;
            public PlayerActionResult Result = PlayerActionResult.Accepted;

            public PlayerActionResult TryStart(RecordingCandidate candidate)
            {
                Last = candidate;
                LastCommand = RecordingCommand.Start;
                Calls++;
                return Result;
            }

            public PlayerActionResult TryStop(RecordingCandidate candidate)
            {
                Last = candidate;
                LastCommand = RecordingCommand.Stop;
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
            var candidate = new RecordingCandidate(1, "dive-1", new PlayerId(7), new FakeTarget());
            Assert.AreEqual(PlayerActionResult.InvalidState, RecordingEvaluation.TryStart(candidate));
            Assert.AreEqual(PlayerActionResult.InvalidState, RecordingEvaluation.TryStop(candidate));
        }

        [Test]
        public void BoundEvaluatorReceivesComponentTargetWithoutScalarTargetId()
        {
            bound = new FakeSink();
            RecordingEvaluation.Bind(bound);
            var target = new FakeTarget();
            var candidate = new RecordingCandidate(9, "dive-42", new PlayerId(3), target);

            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStart(candidate));
            Assert.AreEqual(1, bound.Calls);
            Assert.AreEqual(RecordingCommand.Start, bound.LastCommand);
            Assert.AreEqual((ulong)9, bound.Last.RequestId);
            Assert.AreEqual("dive-42", bound.Last.DiveId);
            Assert.AreEqual(new PlayerId(3), bound.Last.PlayerId);
            Assert.AreSame(target, bound.Last.Target);
        }

        [Test]
        public void StartAndStopAreSeparateCallsAndCanReuseTheSameLockedTarget()
        {
            bound = new FakeSink();
            RecordingEvaluation.Bind(bound);
            var target = new FakeTarget();

            var start = new RecordingCandidate(1, "dive-1", new PlayerId(4), target);
            var stop = new RecordingCandidate(2, "dive-1", new PlayerId(4), target);

            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStart(start));
            Assert.AreEqual(RecordingCommand.Start, bound.LastCommand);
            Assert.AreSame(target, bound.Last.Target);

            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStop(stop));
            Assert.AreEqual(RecordingCommand.Stop, bound.LastCommand);
            Assert.AreSame(target, bound.Last.Target);
            Assert.AreEqual(2, bound.Calls);
        }

        [Test]
        public void UnbindingADifferentSinkDoesNotEraseCurrentEvaluator()
        {
            bound = new FakeSink();
            var other = new FakeSink();
            RecordingEvaluation.Bind(bound);
            RecordingEvaluation.Unbind(other);

            var candidate = new RecordingCandidate(1, "dive-1", new PlayerId(1), new FakeTarget());
            Assert.AreEqual(PlayerActionResult.Accepted, RecordingEvaluation.TryStart(candidate));
            Assert.AreEqual(1, bound.Calls);
        }
    }
}
