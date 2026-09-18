using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    public enum PlayerActionKind : byte
    {
        Harpoon = 1,
        Pickup = 2,
        ServiceInteraction = 3
    }

    // Host-side replay/rate guard shared by player-originated actions.
    public sealed class ServerActionGate
    {
        private ulong lastRequestId;
        private double nextHarpoonAt;
        private double nextPickupAt;
        private double nextServiceInteractionAt;

        public PlayerActionResult TryAccept(ulong requestId, PlayerActionKind kind, double now, double cooldownSeconds)
        {
            if (requestId == 0 || requestId <= lastRequestId) return PlayerActionResult.DuplicateRequest;
            if (kind != PlayerActionKind.Harpoon && kind != PlayerActionKind.Pickup &&
                kind != PlayerActionKind.ServiceInteraction)
                return PlayerActionResult.InvalidState;

            lastRequestId = requestId;
            if (double.IsNaN(now) || double.IsInfinity(now)) return PlayerActionResult.InvalidState;

            var next = kind switch
            {
                PlayerActionKind.Harpoon => nextHarpoonAt,
                PlayerActionKind.Pickup => nextPickupAt,
                PlayerActionKind.ServiceInteraction => nextServiceInteractionAt,
                _ => 0d
            };
            if (now < next) return PlayerActionResult.TooFast;

            var cooldown = double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds)
                ? 0d : Math.Max(0d, cooldownSeconds);
            switch (kind)
            {
                case PlayerActionKind.Harpoon:
                    nextHarpoonAt = now + cooldown;
                    break;
                case PlayerActionKind.Pickup:
                    nextPickupAt = now + cooldown;
                    break;
                case PlayerActionKind.ServiceInteraction:
                    nextServiceInteractionAt = now + cooldown;
                    break;
            }
            return PlayerActionResult.Accepted;
        }

        public void Reset()
        {
            lastRequestId = 0;
            nextHarpoonAt = 0d;
            nextPickupAt = 0d;
            nextServiceInteractionAt = 0d;
        }
    }
}
