using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Economy
{
    // Composition owns the host/session checks and binds this handler. EconomyPlayerSync only
    // transports an owner's shop request; it never decides phase/authority by itself.
    public static class EconomyPurchaseAuthority
    {
        public static Func<PlayerId, string, ulong, TransactionResult> Handler { get; private set; }

        public static bool IsBound => Handler != null;

        public static void Bind(Func<PlayerId, string, ulong, TransactionResult> handler) => Handler = handler;
        public static void Unbind(Func<PlayerId, string, ulong, TransactionResult> handler)
        {
            if (Handler == handler) Handler = null;
        }

        public static TransactionResult TryPurchase(PlayerId player, string equipmentId, ulong requestId) =>
            Handler != null
                ? Handler(player, equipmentId, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);
    }
}