namespace DeepDive.World
{
    // Whether a subject may be filmed right now, for subjects that are only filmable some of
    // the time. A fish is always filmable and has no window; a special event has one.
    //
    // As narrow as IRecorderView and IRecordingSubject, and for the same reason: the recording
    // rules must not learn what an event is. All they ask is whether the shutter is allowed to
    // run, so RecordingSession can be tested against a one-line fake instead of a live event.
    //
    // The closing behaviour this seam feeds is Mehmet's 14 September 2026 decision, and it lives
    // in RecordingSession, not here: seconds already earned survive, no further seconds accrue,
    // and no new take may start. An implementation only has to answer the question.
    public interface IRecordingWindow
    {
        bool IsOpen { get; }
    }
}
