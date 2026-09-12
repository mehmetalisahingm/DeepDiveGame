using System;

namespace DeepDive.Core.Contracts
{
    // P3 camera transport contract. Mehmet produces the intent on the owner, the host
    // reconstructs the authoritative session/player context, and Utku's evaluator decides
    // whether the recording is actually valid. No client-provided quality/duration is trusted.
    public enum RecordingCommand : byte
    {
        Start = 1,
        Stop = 2
    }

    public readonly struct RecordingCandidate
    {
        public readonly ulong RequestId;
        public readonly string DiveId;
        public readonly PlayerId PlayerId;
        public readonly bool HasTarget;
        public readonly ulong TargetNetworkObjectId;
        public readonly RecordingCommand Command;

        public RecordingCandidate(ulong requestId, string diveId, PlayerId playerId,
            bool hasTarget, ulong targetNetworkObjectId, RecordingCommand command)
        {
            RequestId = requestId;
            DiveId = diveId ?? string.Empty;
            PlayerId = playerId;
            HasTarget = hasTarget;
            TargetNetworkObjectId = targetNetworkObjectId;
            Command = command;
        }
    }

    // Implemented by P3-B. This boundary deliberately carries only intent + target identity;
    // visibility, distance, timing, framing and quality stay host-authoritative in Utku's area.
    public interface IRecordingEvaluationSink
    {
        PlayerActionResult TrySubmit(RecordingCandidate candidate);
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

        public static PlayerActionResult TrySubmit(RecordingCandidate candidate) =>
            Sink == null ? PlayerActionResult.InvalidState : Sink.TrySubmit(candidate);
    }

    // Pure gate used by the network bridge and EditMode tests. requestId is consumed per
    // player by the bridge; this helper only decides whether the requested state transition is
    // structurally valid before the P3-B evaluator sees it.
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
