using System;

namespace DeepDive.Core.Contracts
{
    // Network transport still distinguishes start/stop, but RecordingCandidate itself is the
    // host-resolved recording context: it never carries client-owned duration or a raw target id.
    public enum RecordingCommand : byte
    {
        Start = 1,
        Stop = 2
    }

    // P3-B owns the concrete implementation (RecordingSubject). Composition resolves this
    // component from the authoritative target NetworkObject before the candidate reaches the
    // recording rule layer. Keeping the target as a component contract prevents target identity
    // from becoming a client-trusted scalar.
    public interface IRecordingTarget
    {
    }

    public readonly struct RecordingCandidate
    {
        public readonly ulong RequestId;
        public readonly string DiveId;
        public readonly PlayerId PlayerId;
        public readonly IRecordingTarget Target;

        public RecordingCandidate(ulong requestId, string diveId, PlayerId playerId, IRecordingTarget target)
        {
            RequestId = requestId;
            DiveId = diveId ?? string.Empty;
            PlayerId = playerId;
            Target = target;
        }
    }

    // Temporary Composition seam until P3-B's concrete RecordingSession is wired. Start/Stop are
    // deliberately separate: the host owns elapsed time, and Stop must reuse the target locked at
    // Start instead of performing a second raycast. When P3-B's richer stop result is connected,
    // Composition must inspect its IsPayable flag before forwarding anything to economy.
    public interface IRecordingEvaluationSink
    {
        PlayerActionResult TryStart(RecordingCandidate candidate);
        PlayerActionResult TryStop(RecordingCandidate candidate);
    }

    public static class RecordingEvaluation
    {
        public static IRecordingEvaluationSink Sink { get; private set; }

        public static void Bind(IRecordingEvaluationSink sink)
        {
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public static void Unbind(IRecordingEvaluationSink sink)
        {
            if (ReferenceEquals(Sink, sink)) Sink = null;
        }

        public static PlayerActionResult TryStart(RecordingCandidate candidate) =>
            Sink == null ? PlayerActionResult.InvalidState : Sink.TryStart(candidate);

        public static PlayerActionResult TryStop(RecordingCandidate candidate) =>
            Sink == null ? PlayerActionResult.InvalidState : Sink.TryStop(candidate);
    }

    // Pure transport gate. requestId is consumed per player by the bridge; this helper only
    // validates request ordering/state before the host resolves IRecordingTarget and calls P3-B.
    public static class RecordingRequestRules
    {
        public static PlayerActionResult Validate(ulong requestId, ulong lastRequestId,
            RecordingCommand command, bool isRecording, bool hasTarget)
        {
            if (requestId == 0 || requestId <= lastRequestId)
                return PlayerActionResult.DuplicateRequest;
            if (command != RecordingCommand.Start && command != RecordingCommand.Stop)
                return PlayerActionResult.Rejected;

            if (command == RecordingCommand.Start)
            {
                if (isRecording) return PlayerActionResult.InvalidState;
                if (!hasTarget) return PlayerActionResult.InvalidTarget;
            }
            else if (!isRecording)
            {
                return PlayerActionResult.InvalidState;
            }

            return PlayerActionResult.Accepted;
        }
    }
}
