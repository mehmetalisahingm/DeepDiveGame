using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // What Process did: the observation outcome and the recording's world context.
    public readonly struct RecordingContextProcessResult
    {
        // null = AcceptRecording was NOT called ("does not apply"): an event, an unknown kind, no authority
        // or no observer. A value is the real AcceptRecording result of this call.
        public SpeciesObservationOutcome? Outcome { get; }
        public RecordingWorldContext Context { get; }

        public bool ObservationAttempted => Outcome.HasValue;

        public RecordingContextProcessResult(SpeciesObservationOutcome? outcome, in RecordingWorldContext context)
        {
            Outcome = outcome;
            Context = context;
        }
    }

    // #101: the one place a recording's world context is decided. Accepting the species recording and reading
    // its context happen in ONE call and in that order, so a caller can never decide before the observation
    // is counted. Rows, first match wins:
    //   1. Unknown (or out-of-range) kind         -> no accept, Empty
    //   2. no species or cells authority          -> no accept, (kind, "", "", "", false)
    //   3. observer position unknown              -> no accept, (kind, "", "", "", false)
    //   4. Event                                  -> no accept, place (cells.TryGetPlaceContext)
    //   5. Species, AcceptRecording then counted  -> context from the stored observation (first flag included)
    //   6. Species, not counted (OutOfRegion,
    //      Invalid, id counted as other evidence) -> place, kind Species, never first
    // AcceptRecording is idempotent: a second Process of the same recordingId is AlreadyCounted with the same
    // context, read from the stored observation (a moved observer or another subjectId changes nothing).
    //
    // The observer position is only an input to AcceptRecording / the cell lookup; the context never carries
    // it. Which kind a subject is (species vs event id) is the caller's: an event id must come as Event.
    // Pure: no MonoBehaviour, no scene, no save.
    public static class RecordingWorldContextRules
    {
        public static RecordingContextProcessResult Process(SpeciesObservationAuthority species,
            ExplorationCellAuthority cells, string recordingId, string subjectId, RecordingSubjectKind kind,
            bool observerKnown, Vector3 observerWorld, int dayNumber)
        {
            if (kind != RecordingSubjectKind.Species && kind != RecordingSubjectKind.Event)
                return new RecordingContextProcessResult(null, RecordingWorldContext.Empty);

            var kindOnly = new RecordingWorldContext(kind, string.Empty, string.Empty, string.Empty, false);
            if (species == null || cells == null || !observerKnown)
                return new RecordingContextProcessResult(null, kindOnly);

            RecordingWorldContext context;
            if (kind == RecordingSubjectKind.Event)
            {
                cells.TryGetPlaceContext(RecordingSubjectKind.Event, observerWorld, out context);
                return new RecordingContextProcessResult(null, context);
            }

            var outcome = species.AcceptRecording(recordingId, subjectId, observerWorld, dayNumber);
            if (!species.TryGetRecordingContext(recordingId, out context))
                cells.TryGetPlaceContext(RecordingSubjectKind.Species, observerWorld, out context);
            return new RecordingContextProcessResult(outcome, context);
        }
    }
}
