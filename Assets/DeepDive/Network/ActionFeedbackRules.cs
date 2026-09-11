using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    public enum PlayerFeedbackCue : byte
    {
        None = 0,
        HarpoonHit = 1,
        HarpoonMiss = 2,
        PickupAccepted = 3,
        BagFull = 4,
        InvalidState = 5,
        Cooldown = 6,
        Duplicate = 7,
        Rejected = 8
    }

    public readonly struct ActionFeedback
    {
        public readonly PlayerFeedbackCue Cue;
        public readonly string Message;
        public readonly float DurationSeconds;

        public ActionFeedback(PlayerFeedbackCue cue, string message, float durationSeconds)
        {
            Cue = cue;
            Message = message ?? string.Empty;
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
        }
    }

    // Keeps owner-facing feedback deterministic and testable without a live NGO session.
    // Harpoon Accepted means the host raycast found an IHarpoonTarget and that target accepted
    // damage. Pickup Accepted means the host-side catch -> inventory transaction completed.
    public static class ActionFeedbackRules
    {
        public static ActionFeedback Resolve(PlayerActionKind kind, PlayerActionResult result)
        {
            if (result == PlayerActionResult.TooFast)
                return new ActionFeedback(PlayerFeedbackCue.Cooldown, "COOLDOWN", 0.35f);
            if (result == PlayerActionResult.DuplicateRequest)
                return new ActionFeedback(PlayerFeedbackCue.Duplicate, "DUPLICATE BLOCKED", 0.45f);
            if (result == PlayerActionResult.InvalidState)
                return new ActionFeedback(PlayerFeedbackCue.InvalidState, "ACTION BLOCKED", 0.55f);
            if (result == PlayerActionResult.InventoryFull)
                return new ActionFeedback(PlayerFeedbackCue.BagFull, "BAG FULL", 0.8f);
            if (result == PlayerActionResult.Rejected)
                return new ActionFeedback(PlayerFeedbackCue.Rejected, "REJECTED", 0.55f);

            if (kind == PlayerActionKind.Harpoon)
            {
                if (result == PlayerActionResult.Accepted)
                    return new ActionFeedback(PlayerFeedbackCue.HarpoonHit, "HIT", 0.35f);
                if (result == PlayerActionResult.InvalidTarget)
                    return new ActionFeedback(PlayerFeedbackCue.HarpoonMiss, "MISS", 0.3f);
            }
            else if (kind == PlayerActionKind.Pickup)
            {
                if (result == PlayerActionResult.Accepted)
                    return new ActionFeedback(PlayerFeedbackCue.PickupAccepted, "CATCH SECURED", 0.8f);
                if (result == PlayerActionResult.InvalidTarget)
                    return new ActionFeedback(PlayerFeedbackCue.Rejected, "NO CATCH", 0.45f);
            }

            return new ActionFeedback(PlayerFeedbackCue.None, string.Empty, 0f);
        }
    }
}
