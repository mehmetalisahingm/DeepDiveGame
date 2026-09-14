namespace DeepDive.World
{
    // What the scene should do about a special event this frame. Never a decision about the
    // event itself - only about what the players see and hear of it.
    public enum SpecialEventCue : byte
    {
        None = 0,

        // First sight of an event that is not on: wipe anything a playOnAwake left running.
        HideAtOnce = 1,

        // First sight of an event that is already under way - a diver who joined late. The glow
        // is there, but the start sound would announce a beginning that happened seconds ago.
        ShowSilently = 2,

        // The event began while this machine was watching.
        ShowWithStartSound = 3,

        // The event ended while this machine was watching: no new glow, the old glow thins out.
        Fade = 4
    }

    // Turns the one bool SpecialEventRunner mirrors - Active - into cues, one per real change.
    //
    // The distinction that needs a rules object is first sight versus a change. A client that
    // joins while the event is on receives Active = true as its initial sync, and to this
    // machine that looks exactly like the event starting; only the start sound differs, and
    // hearing "it began" twelve seconds in would be wrong. So the first value observed is shown
    // as it is, and only a change seen afterwards counts as the event beginning or ending.
    //
    // Pure rules object: no Unity type, no NGO. SpecialEventPresenter feeds it every frame and
    // applies what comes back.
    public sealed class SpecialEventPresentation
    {
        private enum Seen : byte
        {
            Nothing = 0,
            Hidden = 1,
            Shown = 2
        }

        private Seen seen = Seen.Nothing;

        public bool IsShown => seen == Seen.Shown;

        // Called with the runner's Active every frame while the runner is spawned. Repeating the
        // same value is the normal case and yields None.
        public SpecialEventCue Observe(bool active)
        {
            switch (seen)
            {
                case Seen.Nothing:
                    seen = active ? Seen.Shown : Seen.Hidden;
                    return active ? SpecialEventCue.ShowSilently : SpecialEventCue.HideAtOnce;

                case Seen.Hidden:
                    if (!active) return SpecialEventCue.None;
                    seen = Seen.Shown;
                    return SpecialEventCue.ShowWithStartSound;

                default:
                    if (active) return SpecialEventCue.None;
                    seen = Seen.Hidden;
                    return SpecialEventCue.Fade;
            }
        }

        // Called while the runner is not spawned: before the first sync, after a despawn, or when
        // the session is gone. There is no Active to believe any more, so a glow still on screen
        // goes at once, and whatever is observed next is first sight again - a diver who
        // reconnects mid-event gets the silent glow, not a second start sound.
        public SpecialEventCue Forget()
        {
            var wasShown = seen == Seen.Shown;
            seen = Seen.Nothing;
            return wasShown ? SpecialEventCue.HideAtOnce : SpecialEventCue.None;
        }
    }
}
