using System;
using DeepDive.Core.Contracts;

namespace DeepDive.Economy
{
    // Composition owns the host/session checks and binds the handler (TownServiceHandler.HandleSelectVehicle, which
    // adds the harbor-vendor session and range gates on top of the fleet rules). EconomyPlayerSync only transports an
    // owner's "make this the active boat" request; it never decides authority by itself. Same shape as
    // EconomyPurchaseAuthority.
    public static class VehicleSelectionAuthority
    {
        public static Func<PlayerId, string, ulong, TransactionResult> Handler { get; private set; }

        public static bool IsBound => Handler != null;

        public static void Bind(Func<PlayerId, string, ulong, TransactionResult> handler) => Handler = handler;

        public static void Unbind(Func<PlayerId, string, ulong, TransactionResult> handler)
        {
            if (Handler == handler) Handler = null;
        }

        public static TransactionResult TrySelect(PlayerId player, string boatId, ulong requestId) =>
            Handler != null
                ? Handler(player, boatId, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);
    }
}
