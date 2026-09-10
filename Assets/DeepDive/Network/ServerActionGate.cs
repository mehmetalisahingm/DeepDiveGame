using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    public enum PlayerActionKind : byte { Harpoon = 1, Pickup = 2 }

    // Host-side replay/rate guard shared by harpoon and pickup RPCs.
    public sealed class ServerActionGate
    {
        private ulong lastRequestId;
        private double nextHarpoonAt;
        private double nextPickupAt;

        public PlayerActionResult TryAccept(ulong requestId, PlayerActionKind kind, double now, double cooldownSeconds)
        {
            if (requestId == 0 || requestId <= lastRequestId) return PlayerActionResult.DuplicateRequest;
            lastRequestId = requestId;
            if (double.IsNaN(now) || double.IsInfinity(now)) return PlayerActionResult.InvalidState;

            var next = kind == PlayerActionKind.Harpoon ? nextHarpoonAt : nextPickupAt;
            if (now < next) return PlayerActionResult.TooFast;

            var cooldown = double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds)
                ? 0d : Math.Max(0d, cooldownSeconds);
            if (kind == PlayerActionKind.Harpoon) nextHarpoonAt = now + cooldown;
            else nextPickupAt = now + cooldown;
            return PlayerActionResult.Accepted;
        }

        public void Reset()
        {
            lastRequestId = 0;
            nextHarpoonAt = 0d;
            nextPickupAt = 0d;
        }
    }
}
