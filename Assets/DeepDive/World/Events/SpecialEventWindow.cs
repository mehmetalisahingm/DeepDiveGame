namespace DeepDive.World
{
    // Where a special event is in its one and only life, for one dive.
    //
    // Deliberately three states and not a bool: "never happened yet" and "already over" both
    // read as "not filmable right now", but only one of them may still be triggered, and the
    // whole point of the event is that it happens once per dive.
    public enum SpecialEventState : byte
    {
        Pending = 0,
        Active = 1,
        Finished = 2
    }

    // The time window a special event is open for (#35, PHASES.md:122 "basit ozel olay").
    //
    // Pure rules object like RecordingSession and CatchState: no Unity type, no NGO, no scene,
    // so the ordering that matters is testable on its own. The NetworkBehaviour that ticks this
    // on the host and mirrors the state to clients is a later slice and stays thin.
    //
    // Two things are deliberately NOT decided here:
    //
    //   1. WHAT triggers the event. TryBegin is called from outside - by a timer, a dive-start
    //      hook, a trigger volume, whatever the design lands on. This class only rules on
    //      whether a trigger is allowed to take effect right now, which is the part that has
    //      to hold no matter which trigger is chosen.
    //
    //   2. WHAT HAPPENS TO AN OPEN RECORDING when the window closes. Mehmet decided this on
    //      14 September 2026, and it is the freeze door, not the drop door: the seconds already
    //      earned survive, no further seconds accrue, no new take may start, and a take that was
    //      running is still graded when it is stopped. This class still owns no takes and says
    //      nothing about them - it only answers IRecordingWindow.IsOpen. The rule itself lives
    //      in RecordingSession.TryStart and RecordingSession.Tick.
    //
    // The duration is measured here and nowhere else, the same way RecordingSession measures a
    // take: Tick is fed the host's own delta time, and there is no path by which a caller can
    // declare how long the event ran - docs/plan/CONTRACTS.md, "Sonuc doguran ... degisikliklerini
    // ev sahibi dogrular".
    public sealed class SpecialEventWindow : IRecordingWindow
    {
        private float elapsedSeconds;

        public SpecialEventWindow(float durationSeconds)
        {
            DurationSeconds = durationSeconds;
        }

        // How long the window stays open once triggered. Kept exactly as given: a nonsensical
        // duration is refused at TryBegin rather than quietly replaced with an invented one,
        // because any fallback here would be a design number nobody agreed on.
        public float DurationSeconds { get; }

        // A window that could never be open is a setup fault, not a decision about a trigger -
        // same treatment RecordingSubject gives a missing quality table.
        public bool IsValid =>
            !float.IsNaN(DurationSeconds) && !float.IsInfinity(DurationSeconds) && DurationSeconds > 0f;

        public SpecialEventState State { get; private set; } = SpecialEventState.Pending;

        public bool IsActive => State == SpecialEventState.Active;

        // IRecordingWindow: the shutter may run exactly while the event is on screen. Pending
        // and Finished both read as shut, which is what stops a diver filming a plankton cluster
        // that has not appeared yet or has already faded.
        public bool IsOpen => IsActive;

        // The dive the event was triggered in, stamped at TryBegin and kept through Finished.
        // The host-side shell compares it against the live dive id to notice a window that
        // outlived its dive, which is the same staleness guard RecordingSession applies to a
        // take ("Bir sonraki dalista eski diveId'ye ait ... istek yeniden kullanilamaz").
        public string DiveId { get; private set; } = "";

        public float ElapsedSeconds => elapsedSeconds;

        public float RemainingSeconds
        {
            get
            {
                if (State != SpecialEventState.Active) return 0f;
                var remaining = DurationSeconds - elapsedSeconds;
                return remaining > 0f ? remaining : 0f;
            }
        }

        // Opens the window, if it may be opened at all. False is the normal answer, not an
        // error: the trigger fires against a window that is already running or already spent
        // every time the event has happened, and the caller is expected to keep asking.
        //
        // No live dive means no dive id to stamp, and an event that cannot be attributed to a
        // dive could never produce a payable recording - same refusal RecordingSession.TryStart
        // makes for the same reason.
        public bool TryBegin(IDiveContext dive)
        {
            if (!IsValid) return false;
            if (State != SpecialEventState.Pending) return false;
            if (dive == null || !dive.IsDiveActive) return false;

            var diveId = dive.CurrentDiveId;
            if (string.IsNullOrWhiteSpace(diveId)) return false;

            DiveId = diveId;
            elapsedSeconds = 0f;
            State = SpecialEventState.Active;
            return true;
        }

        // Called every host tick. Returns true only on the tick that closed the window, so the
        // shell can act on the edge once instead of comparing states itself. What it does on
        // that edge - to an open take, to the world, to the HUD - is not decided here.
        public bool Tick(float deltaTime)
        {
            if (State != SpecialEventState.Active) return false;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return false;

            elapsedSeconds += deltaTime;
            if (elapsedSeconds < DurationSeconds) return false;

            // Clamped so a long tick cannot report the event as having run longer than it was
            // ever open for. The overshoot is the host's frame, not extra event time.
            elapsedSeconds = DurationSeconds;
            State = SpecialEventState.Finished;
            return true;
        }

        // Closes a running window before its time is up: the dive ended, or the thing the event
        // lives on went away. Finished rather than back to Pending, because the event has had
        // its turn this dive - the once-per-dive rule does not care why it ended.
        //
        // A window that never started has nothing to close and stays Pending, so an early
        // teardown cannot silently burn the event for a dive it never appeared in.
        public void End()
        {
            if (State != SpecialEventState.Active) return;
            State = SpecialEventState.Finished;
        }

        // For reuse across dives, matching RecordingSession.Reset and CatchState.Reset: the
        // next dive gets its own event, and no stamp from the old one may survive into it.
        public void Reset()
        {
            State = SpecialEventState.Pending;
            DiveId = "";
            elapsedSeconds = 0f;
        }
    }
}
