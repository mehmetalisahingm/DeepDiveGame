using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    // Player validates owner intent, range and line-of-sight first. Progression/economy may bind
    // this hook to execute the concrete service without making Network depend on that module.
    public static class ServiceInteractionAuthority
    {
        public static Func<PlayerId, ServicePointDefinition, ulong, PlayerActionResult> Handler { get; private set; }

        public static bool IsBound => Handler != null;

        public static void Bind(Func<PlayerId, ServicePointDefinition, ulong, PlayerActionResult> handler) =>
            Handler = handler;

        public static void Unbind(Func<PlayerId, ServicePointDefinition, ulong, PlayerActionResult> handler)
        {
            if (Handler == handler) Handler = null;
        }

        public static PlayerActionResult TryInteract(PlayerId player, ServicePointDefinition service, ulong requestId) =>
            Handler != null ? Handler(player, service, requestId) : PlayerActionResult.Accepted;
    }
}
