using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    public enum TurnInKind : byte
    {
        None = 0,
        Catch = 1,
        Recording = 2
    }

    // docs/plan/CONTRACTS.md "PendingTurnInState": a safely returned catch/recording that has NOT been
    // paid yet. Money is produced only by the matching NPC. The entry snapshots what the price needs
    // (subject/quality/weight) because the inventory forgets a dive's captures when the next dive starts.
    public readonly struct PendingTurnInState
    {
        public readonly string ItemId;
        public readonly TurnInKind Kind;
        public readonly string SourceDiveId;
        public readonly string SubjectId;
        public readonly int WeightGrams;
        public readonly int Quality;
        public readonly float ValidDurationSeconds;
        public readonly PlayerId Carrier;
        public readonly bool IsSharedEscrow;
        public readonly int Revision;

        public PendingTurnInState(string itemId, TurnInKind kind, string sourceDiveId, string subjectId,
            int weightGrams, int quality, float validDurationSeconds, PlayerId carrier, bool isSharedEscrow,
            int revision)
        {
            ItemId = itemId ?? string.Empty;
            Kind = kind;
            SourceDiveId = sourceDiveId ?? string.Empty;
            SubjectId = subjectId ?? string.Empty;
            WeightGrams = weightGrams;
            Quality = quality;
            ValidDurationSeconds = validDurationSeconds;
            Carrier = carrier;
            IsSharedEscrow = isSharedEscrow;
            Revision = revision;
        }
    }

    // Result of an NPC turn-in/sale. Money is only ever produced when Accepted && Earned > 0.
    public readonly struct TurnInResult
    {
        public readonly ulong RequestId;
        public readonly bool Accepted;
        public readonly string ReasonCode;
        public readonly int Earned;
        public readonly int ItemCount;
        public readonly int Revision;

        public TurnInResult(ulong requestId, bool accepted, string reasonCode, int earned, int itemCount, int revision)
        {
            RequestId = requestId;
            Accepted = accepted;
            ReasonCode = reasonCode ?? string.Empty;
            Earned = earned;
            ItemCount = itemCount;
            Revision = revision;
        }

        public static TurnInResult Ok(ulong requestId, int earned, int itemCount, int revision) =>
            new TurnInResult(requestId, true, string.Empty, earned, itemCount, revision);

        public static TurnInResult Reject(ulong requestId, string reasonCode, int revision) =>
            new TurnInResult(requestId, false, reasonCode, 0, 0, revision);
    }

    public enum BoatRepairStatus : byte
    {
        Broken = 0,
        InProgress = 1,
        Repaired = 2
    }

    public enum BoatPartSource : byte
    {
        Found = 1,
        Purchased = 2
    }

    // Fixed campaign boat and its three repair parts. Utku's world anchors must use exactly these ids
    // (docs/plan/CONTRACTS.md "BoatRepairState"); ids are stable so a save can reference them.
    public static class BoatRepairParts
    {
        public const string BoatId = "boat-1";
        public const string Hull = "boat-part-hull";
        public const string Engine = "boat-part-engine";
        public const string FuelTank = "boat-part-fuel-tank";

        public static readonly IReadOnlyList<string> All = new[] { Hull, Engine, FuelTank };

        public static bool IsPart(string partId)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i], partId, System.StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // docs/plan/CONTRACTS.md "BoatRepairState": each fixed part contributes at most once; the last
    // contribution makes the boat Repaired. Travel is a separate P3.3 state and not modelled here.
    public readonly struct BoatRepairState
    {
        public readonly string BoatId;
        public readonly IReadOnlyList<string> RequiredPartIds;
        public readonly IReadOnlyList<string> CompletedPartIds;
        public readonly BoatRepairStatus Status;
        public readonly int Revision;

        public BoatRepairState(string boatId, IReadOnlyList<string> requiredPartIds,
            IReadOnlyList<string> completedPartIds, BoatRepairStatus status, int revision)
        {
            BoatId = boatId ?? string.Empty;
            RequiredPartIds = requiredPartIds;
            CompletedPartIds = completedPartIds;
            Status = status;
            Revision = revision;
        }
    }

    // Seam for Utku's free world parts: the World/interaction layer validates that the part exists, is in
    // reach and unclaimed, then calls TryClaimFound. Composition binds the handler to the economy while it
    // is the host; unbound (client, no economy) means the part cannot be claimed.
    public interface IBoatPartPickup
    {
        string PartId { get; }
    }

    public static class BoatPartClaim
    {
        public static System.Func<PlayerId, string, ulong, TransactionResult> Handler { get; private set; }

        public static bool IsBound => Handler != null;

        public static void Bind(System.Func<PlayerId, string, ulong, TransactionResult> handler) => Handler = handler;

        public static void Unbind(System.Func<PlayerId, string, ulong, TransactionResult> handler)
        {
            if (Handler == handler) Handler = null;
        }

        public static TransactionResult TryClaimFound(PlayerId player, string partId, ulong requestId) =>
            Handler != null ? Handler(player, partId, requestId) : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    // Outcome of one NPC interaction as shown to the acting player. Carries no authority; it is a
    // host-published notification of a decision the economy already made.
    public readonly struct TownServiceOutcome
    {
        public readonly string ServiceId;
        public readonly ServicePointType ServiceType;
        public readonly ulong RequestId;
        public readonly bool Accepted;
        public readonly string ReasonCode;
        public readonly int Amount;
        public readonly int ItemCount;

        public TownServiceOutcome(string serviceId, ServicePointType serviceType, ulong requestId, bool accepted,
            string reasonCode, int amount, int itemCount)
        {
            ServiceId = serviceId ?? string.Empty;
            ServiceType = serviceType;
            RequestId = requestId;
            Accepted = accepted;
            ReasonCode = reasonCode ?? string.Empty;
            Amount = amount;
            ItemCount = itemCount;
        }
    }

    // The three physical P3.2 services. Anchor ids are what the scene's ServicePointAnchor components
    // must use; the interaction layer (Mehmet) consumes ServicePointDefinition built from them.
    public static class TownServiceCatalog
    {
        public const string EquipmentShopId = "shop-equipment";
        public const string FishBuyerId = "buyer-fish";
        public const string RecordingBuyerId = "buyer-recording";
        public const float DefaultInteractionDistance = 3f;

        public static readonly IReadOnlyList<ServicePointDefinition> All = new[]
        {
            new ServicePointDefinition(EquipmentShopId, ServicePointType.EquipmentShop,
                "anchor-shop-equipment", DefaultInteractionDistance, "catalog-equipment"),
            new ServicePointDefinition(FishBuyerId, ServicePointType.FishBuyer,
                "anchor-buyer-fish", DefaultInteractionDistance, "catalog-fish"),
            new ServicePointDefinition(RecordingBuyerId, ServicePointType.RecordingBuyer,
                "anchor-buyer-recording", DefaultInteractionDistance, "catalog-recording")
        };

        // True only for a scene-provided definition that matches the catalog id AND type, so a
        // misconfigured or duplicated anchor can never act as a different service.
        public static bool Matches(ServicePointDefinition definition)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i].ServiceId, definition.ServiceId, System.StringComparison.Ordinal))
                    return All[i].ServiceType == definition.ServiceType;
            return false;
        }
    }
}
