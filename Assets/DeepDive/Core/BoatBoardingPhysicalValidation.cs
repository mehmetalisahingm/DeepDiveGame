using System;

namespace DeepDive.Core.Contracts
{
    // Mehmet/#75 host-side physical gate. BoatTripManager remains the seat/trip authority; this
    // seam only proves that a requested authored seat is physically reachable from the requesting
    // player's authoritative world position. Mert's RPC checks it before forwarding a seat id.
    public static class BoatBoardingPhysicalValidation
    {
        public static Func<PlayerId, string, string, bool> Handler { get; private set; }
        public static bool IsBound => Handler != null;

        public static void Bind(Func<PlayerId, string, string, bool> handler) => Handler = handler;

        public static void Unbind(Func<PlayerId, string, string, bool> handler)
        {
            if (Handler == handler) Handler = null;
        }

        public static bool Validate(PlayerId player, string boatId, string seatId) =>
            Handler != null && Handler(player, boatId, seatId);
    }
}