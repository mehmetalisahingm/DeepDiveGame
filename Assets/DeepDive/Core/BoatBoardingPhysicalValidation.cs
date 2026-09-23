using System;

namespace DeepDive.Core.Contracts
{
    // Validates an explicit seat id against host-authored world geometry. Kept separate from
    // BoatTripManager so trip occupancy/progression stays Mert's single authority.
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

    // Input/UI asks only to board or disembark. The host Network layer chooses the nearest authored
    // seat and validates repair/proximity before forwarding to BoatBoarding. This prevents a client
    // from authoritatively choosing an arbitrary seat id or boarding an unrepaired boat.
    public static class BoatBoardingPhysicalInteraction
    {
        public static Func<PlayerId, ulong, TransactionResult> BoardNearestHandler { get; private set; }
        public static Func<PlayerId, ulong, TransactionResult> DisembarkHandler { get; private set; }
        public static bool IsBound => BoardNearestHandler != null && DisembarkHandler != null;

        public static void Bind(
            Func<PlayerId, ulong, TransactionResult> boardNearestHandler,
            Func<PlayerId, ulong, TransactionResult> disembarkHandler)
        {
            BoardNearestHandler = boardNearestHandler;
            DisembarkHandler = disembarkHandler;
        }

        public static void Unbind(
            Func<PlayerId, ulong, TransactionResult> boardNearestHandler,
            Func<PlayerId, ulong, TransactionResult> disembarkHandler)
        {
            if (BoardNearestHandler == boardNearestHandler) BoardNearestHandler = null;
            if (DisembarkHandler == disembarkHandler) DisembarkHandler = null;
        }

        public static TransactionResult TryBoardNearest(PlayerId player, ulong requestId) =>
            BoardNearestHandler != null
                ? BoardNearestHandler(player, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TryDisembark(PlayerId player, ulong requestId) =>
            DisembarkHandler != null
                ? DisembarkHandler(player, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);
    }
}