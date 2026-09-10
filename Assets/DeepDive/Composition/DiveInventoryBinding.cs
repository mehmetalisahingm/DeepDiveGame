using System;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Session;
using DeepDive.World;

namespace DeepDive.Composition
{
    // The only World -> Inventory bridge. Authority is supplied by the live session.
    public sealed class DiveInventoryBinding : IDiveContext, ICatchClaimSink, IDisposable
    {
        private readonly SessionManager session;
        private readonly InventoryManager inventory;
        private readonly Func<bool> isAuthority;
        private readonly Func<PlayerId, bool> canClaim;
        private bool disposed;

        public DiveInventoryBinding(SessionManager session, InventoryManager inventory,
            Func<bool> isAuthority, Func<PlayerId, bool> canClaim)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.isAuthority = isAuthority ?? throw new ArgumentNullException(nameof(isAuthority));
            this.canClaim = canClaim ?? throw new ArgumentNullException(nameof(canClaim));
            session.OnSessionStateChanged += StateChanged;
            Refresh();
        }

        public bool IsDiveActive => !disposed && session != null && inventory != null &&
            isAuthority() && session.State.Phase == SessionPhase.Dive &&
            !string.IsNullOrWhiteSpace(session.State.DiveId);
        public string CurrentDiveId => IsDiveActive ? session.State.DiveId : string.Empty;

        private void StateChanged(SessionState state) => Refresh();

        public void Refresh()
        {
            if (IsDiveActive)
            {
                DiveContext.Bind(this);
                CatchClaim.Bind(this);
            }
            else Release();
        }

        public PlayerActionResult TryClaim(PlayerId player, CaptureResult capture)
        {
            if (!IsDiveActive || !session.Roster.ContainsKey(player) || !canClaim(player))
                return PlayerActionResult.InvalidState;
            if (inventory.Bags.TryGetValue(player, out var bag) && bag.SafelyReturned)
                return PlayerActionResult.InvalidState;
            if (capture.DiveId != CurrentDiveId || string.IsNullOrWhiteSpace(capture.CaptureId) ||
                string.IsNullOrWhiteSpace(capture.SpeciesId) || capture.WeightGrams <= 0)
                return PlayerActionResult.InvalidTarget;
            // Bound before the inventory sum, so malformed weights cannot overflow it.
            if (capture.WeightGrams > InventoryManager.CapacityGrams)
                return PlayerActionResult.InventoryFull;
            return Map(inventory.TryAddCatch(player, capture));
        }

        public static PlayerActionResult Map(InventoryActionResult result)
        {
            switch (result)
            {
                case InventoryActionResult.Ok: return PlayerActionResult.Accepted;
                case InventoryActionResult.InventoryFull: return PlayerActionResult.InventoryFull;
                case InventoryActionResult.WrongPhase:
                case InventoryActionResult.PlayerInactive: return PlayerActionResult.InvalidState;
                case InventoryActionResult.InvalidTarget:
                case InventoryActionResult.AlreadyClaimed: return PlayerActionResult.InvalidTarget;
                default: return PlayerActionResult.Rejected;
            }
        }

        private void Release()
        {
            // Destruction of an older/duplicate session must not erase a newer binding.
            if (ReferenceEquals(DiveContext.Source, this)) DiveContext.Unbind();
            if (ReferenceEquals(CatchClaim.Sink, this)) CatchClaim.Unbind();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (session != null) session.OnSessionStateChanged -= StateChanged;
            Release();
        }
    }
}
