using System;

namespace DeepDive.Core.Contracts
{
    // P4.1 (#88/#90) seam between Mehmet's physical home interaction and Mert's shared
    // storage authority. The physical layer validates the real player/locker/range first;
    // the storage owner binds the actual inventory transaction here. An unbound store is
    // deliberately closed so the physical interaction can never become a second inventory authority.
    public static class HomeStorageInteraction
    {
        public static Func<PlayerId, ulong, TransactionResult> OpenHandler { get; private set; }

        public static void Bind(Func<PlayerId, ulong, TransactionResult> open)
        {
            OpenHandler = open;
        }

        public static void Unbind(Func<PlayerId, ulong, TransactionResult> open)
        {
            if (OpenHandler == open) OpenHandler = null;
        }

        public static TransactionResult TryOpen(PlayerId player, ulong requestId) =>
            OpenHandler != null
                ? OpenHandler(player, requestId)
                : TransactionResult.Reject(requestId, "StorageUnavailable", 0);
    }
}
