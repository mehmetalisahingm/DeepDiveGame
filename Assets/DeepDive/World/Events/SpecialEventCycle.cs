namespace DeepDive.World
{
    // One dive's worth of special event: the wait, the appearance, and the dive boundaries
    // around both. Joins SpecialEventSchedule (when it may start) to SpecialEventWindow (how
    // long it stays) and owns the one thing neither of them can see on its own - that a dive
    // ended or was replaced.
    //
    // This exists so SpecialEventRunner can be a shell with no decisions in it. The ordering
    // below is the part that would break quietly if it lived in a FixedUpdate: nothing in this
    // repository's EditMode tests starts a NetworkManager or spawns a NetworkObject, so logic
    // inside a NetworkBehaviour is logic nobody can test until the game is running. Here it is
    // a plain object driven by a fake dive.
    //
    // Pure rules object: no Unity type, no NGO. The host's delta time comes in, nothing else.
    public sealed class SpecialEventCycle
    {
        private readonly SpecialEventSchedule schedule;
        private readonly SpecialEventWindow window;

        public SpecialEventCycle(SpecialEventSchedule schedule, SpecialEventWindow window)
        {
            this.schedule = schedule;
            this.window = window;
        }

        // A cycle built from a misconfigured definition never opens, rather than opening on an
        // invented schedule. Same direction RecordingSubject takes with a missing quality table.
        public bool IsValid => schedule != null && window != null && schedule.IsValid && window.IsValid;

        public SpecialEventState State => window == null ? SpecialEventState.Pending : window.State;

        // What RecordingSession consults through IRecordingWindow, via the runner.
        public bool IsOpen => window != null && window.IsOpen;

        // The dive the current appearance belongs to, for the host log and the staleness checks.
        public string DiveId => window == null ? "" : window.DiveId;

        public float RemainingSeconds => window == null ? 0f : window.RemainingSeconds;

        public float WaitRemainingSeconds => schedule == null ? 0f : schedule.RemainingSeconds;

        // Called every host tick. The whole ordering of the event lives here and nowhere else.
        public void Tick(IDiveContext dive, float deltaTime)
        {
            if (!IsValid) return;

            var liveDiveId = dive != null && dive.IsDiveActive ? dive.CurrentDiveId : "";

            // No live dive: nothing runs, and nothing from the dive that just ended may survive
            // into the next one. Resetting rather than pausing is what makes the next dive count
            // its own 45 seconds instead of inheriting a half-spent wait.
            if (string.IsNullOrWhiteSpace(liveDiveId))
            {
                Reset();
                return;
            }

            // A different dive id is a different dive, even if no tick ever saw the gap between
            // them. The new dive gets its own appearance; the old one's is dropped.
            if (!string.IsNullOrWhiteSpace(window.DiveId) && window.DiveId != liveDiveId) Reset();

            if (schedule.Tick(dive, deltaTime)) window.TryBegin(dive);
            window.Tick(deltaTime);
        }

        // Whether the event object could be taken away without destroying work somebody has
        // already done. Mehmet, 14 September 2026: "Gorsel kapanabilir AMA acik kayit
        // sonuclanmadan hedef nesnesi YOK EDILMEZ" - despawning a RecordingSubject runs its
        // AbortAll, which drops every open take and burns the seconds a diver has earned.
        //
        // Nothing calls this in the current slice, and that is deliberate: SpecialEventRunner
        // never despawns anything. The event object is scene-placed and lives out the dive with
        // its visual switched off, so there is nothing to burn. The rule is written and locked
        // by tests here so that any later slice that does want the object gone already has it.
        public bool CanRetire(int openTakeCount) =>
            State == SpecialEventState.Finished && openTakeCount <= 0;

        // Closes a running appearance early: the object is being despawned, or the dive is being
        // torn down. Leaves a cycle that never started alone, so an early teardown cannot burn
        // the event for a dive it was never seen in.
        public void End() => window?.End();

        public void Reset()
        {
            schedule?.Reset();
            window?.Reset();
        }
    }
}
