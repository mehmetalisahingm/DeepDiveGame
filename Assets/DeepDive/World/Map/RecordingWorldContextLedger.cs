using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    public enum RecordingContextCompleteOutcome : byte
    {
        // Blank recording id, or a context the contract does not allow (a kind outside the enum, a band that
        // is neither a known DepthBandIds id nor Unclassified). Nothing stored.
        Invalid = 0,

        // The first result for this recording id; stored.
        Completed = 1,

        // This id already has a result. The first one is kept as it is - never overwritten, even by a
        // different context - and nothing else happens.
        AlreadyCompleted = 2
    }

    // #101: which recordings have had their world context decided, and what it was. A processed-state record,
    // not an authority: it decides nothing (RecordingWorldContextRules.Process does) and only keeps the first
    // result per recording id. Empty is a valid terminal result. Not saved; its lifetime is the Composition
    // owner's - a new instance starts over.
    public sealed class RecordingWorldContextLedger
    {
        private readonly Dictionary<string, RecordingWorldContext> results =
            new Dictionary<string, RecordingWorldContext>(StringComparer.Ordinal);

        public int Count => results.Count;

        public RecordingContextCompleteOutcome Complete(string recordingId, in RecordingWorldContext context)
        {
            if (string.IsNullOrWhiteSpace(recordingId)) return RecordingContextCompleteOutcome.Invalid;
            if (context.Kind > RecordingSubjectKind.Event) return RecordingContextCompleteOutcome.Invalid;
            if (!DepthBandIds.IsValidOrUnclassified(context.DepthBandId)) return RecordingContextCompleteOutcome.Invalid;
            if (results.ContainsKey(recordingId)) return RecordingContextCompleteOutcome.AlreadyCompleted;

            results.Add(recordingId, context);
            return RecordingContextCompleteOutcome.Completed;
        }

        // false = not processed yet (context Empty); true = processed, Empty included. No allocation.
        public bool TryResolve(string recordingId, out RecordingWorldContext context)
        {
            context = default;
            return recordingId != null && results.TryGetValue(recordingId, out context);
        }
    }
}
