using System;

namespace DeepDive.Core.Contracts
{
    // Input/UI only asks to board or disembark. The host-side physical layer chooses the
    // actual authored/approved seat and validates proximity before forwarding to BoatBoarding.
    // This keeps client input from becoming a second seat authority.
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
