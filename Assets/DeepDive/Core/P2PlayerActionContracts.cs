namespace DeepDive.Core.Contracts
{
    public enum PlayerActionResult
    {
        Accepted = 0,
        InvalidState = 1,
        InvalidTarget = 2,
        TooFast = 3,
        DuplicateRequest = 4,
        Rejected = 5,
        InventoryFull = 6
    }

    public readonly struct HarpoonHit
    {
        public readonly PlayerId PlayerId;
        public readonly ulong RequestId;
        public readonly float Damage;

        public HarpoonHit(PlayerId playerId, ulong requestId, float damage)
        {
            PlayerId = playerId;
            RequestId = requestId;
            Damage = damage;
        }
    }

    // P2-B implements the real fish/catch authority. P2-A only transports the
    // owner intent to the host and invokes this hook after host-side aim/range checks.
    public interface IHarpoonTarget
    {
        PlayerActionResult TryApplyHarpoonHit(HarpoonHit hit);
    }

    // The implementation must not consume/remove the catch until Utku's catch
    // validation and Mert's inventory capacity/add operation both succeed.
    public interface ICatchPickupTarget
    {
        PlayerActionResult TryPickup(PlayerId playerId, ulong requestId);
    }
}
