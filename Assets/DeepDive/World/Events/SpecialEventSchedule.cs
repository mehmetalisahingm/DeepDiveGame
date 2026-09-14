namespace DeepDive.World
{
    // When the special event is allowed to start, counted from the beginning of the dive.
    //
    // Mehmet, 14 September 2026: "host dalis basindan 45 sn sonra baslatir ... dalis basina bir
    // kez". The number itself lives in RecordingEventDefinition; this class is the rule that
    // spends it.
    //
    // Separate from SpecialEventWindow on purpose. The window owns how long the event stays
    // open and deliberately knows nothing about what triggers it - see its own comment. Folding
    // the countdown in there would put trigger policy inside the window and force it to take a
    // dive context on every tick. Two small rules, each with one job, and the host-side shell
    // joins them in two lines:
    //
    //     if (schedule.Tick(dive, deltaTime)) window.TryBegin(dive);
    //     window.Tick(deltaTime);
    //
    // Pure rules object, no Unity type: the delay is measured from the host's own delta time,
    // the same discipline RecordingSession applies to a take.
    public sealed class SpecialEventSchedule
    {
        private float elapsedSeconds;
        private bool fired;

        public SpecialEventSchedule(float delaySeconds)
        {
            DelaySeconds = delaySeconds;
        }

        // How long after the dive starts the event may begin. Zero is legal and means "as soon
        // as the dive is running"; a negative or nonsensical value is a setup fault and never
        // fires, rather than being replaced by an invented delay.
        public float DelaySeconds { get; }

        public bool IsValid =>
            !float.IsNaN(DelaySeconds) && !float.IsInfinity(DelaySeconds) && DelaySeconds >= 0f;

        // The dive currently being counted in. Cleared whenever there is no live dive, which is
        // what makes the next dive start its own countdown instead of inheriting one.
        public string DiveId { get; private set; } = "";

        public float ElapsedSeconds => elapsedSeconds;

        public bool HasFired => fired;

        public float RemainingSeconds
        {
            get
            {
                if (fired || !IsValid) return 0f;
                var remaining = DelaySeconds - elapsedSeconds;
                return remaining > 0f ? remaining : 0f;
            }
        }

        // Called every host tick. True exactly once per dive: on the tick that spends the delay.
        // The caller turns that into SpecialEventWindow.TryBegin; nothing here opens anything.
        public bool Tick(IDiveContext dive, float deltaTime)
        {
            if (!IsValid) return false;

            // Between dives the countdown does not merely pause, it is dropped: "45 seconds"
            // is measured from the start of a dive, so a dive that ended halfway through leaves
            // nothing behind for the next one to inherit.
            var diveId = dive != null && dive.IsDiveActive ? dive.CurrentDiveId : "";
            if (string.IsNullOrWhiteSpace(diveId))
            {
                Reset();
                return false;
            }

            // A different dive id is a different dive, even if no tick ever saw the gap.
            if (diveId != DiveId)
            {
                DiveId = diveId;
                elapsedSeconds = 0f;
                fired = false;
            }

            if (fired) return false;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return false;

            elapsedSeconds += deltaTime;
            if (elapsedSeconds < DelaySeconds) return false;

            // Clamped so the reported wait is the delay that was agreed, not the host frame that
            // happened to overshoot it.
            elapsedSeconds = DelaySeconds;
            fired = true;
            return true;
        }

        // For reuse across dives, matching RecordingSession.Reset and SpecialEventWindow.Reset.
        public void Reset()
        {
            DiveId = "";
            elapsedSeconds = 0f;
            fired = false;
        }
    }
}
