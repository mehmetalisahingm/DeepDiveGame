using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // Placeholder pending Utku's real recording/target validation (P3-B, issue #35). Shape
    // matches docs/plan/CONTRACTS.md ortak veri sozlugu: "RecordingResult | recordingId, diveId,
    // playerId, tur/olay kimligi, kalite, gecerli sure | Utku -> Mert/Mehmet | P3". Not yet
    // consumed for payment (see EconomyManager) until P3-B produces real recordings.
    public readonly struct RecordingResult
    {
        public readonly string RecordingId;
        public readonly string DiveId;
        public readonly PlayerId PlayerId;
        public readonly string SubjectId;
        public readonly int Quality;
        public readonly float ValidDurationSeconds;

        public RecordingResult(string recordingId, string diveId, PlayerId playerId, string subjectId,
            int quality, float validDurationSeconds)
        {
            RecordingId = recordingId;
            DiveId = diveId;
            PlayerId = playerId;
            SubjectId = subjectId;
            Quality = quality;
            ValidDurationSeconds = validDurationSeconds;
        }
    }

    // docs/plan/CONTRACTS.md: "EquipmentDefinition | equipmentId, yuva, seviye, etkiler; para/alis
    // fiyatinin sahibi Mert | Mert -> Mehmet | P3". v1 treats each definition as a per-player SKU
    // (buy your own copy), not a shared physical instance transferable between divers.
    public readonly struct EquipmentDefinition
    {
        public readonly string EquipmentId;
        public readonly string Slot;
        public readonly int Level;
        public readonly int Price;

        public EquipmentDefinition(string equipmentId, string slot, int level, int price)
        {
            EquipmentId = equipmentId;
            Slot = slot;
            Level = level;
            Price = price;
        }
    }

    // docs/plan/CONTRACTS.md: "LoadoutState | playerId, takili equipmentInstanceId degerleri ve
    // revision | Mert -> Mehmet | P3". Mehmet recomputes diver stats (e.g. tube -> maxOxygen)
    // from this whenever it changes; Mert never writes Mehmet's stat fields directly.
    public readonly struct LoadoutState
    {
        public readonly PlayerId PlayerId;
        public readonly IReadOnlyList<string> EquippedIds;
        public readonly int Revision;

        public LoadoutState(PlayerId playerId, IReadOnlyList<string> equippedIds, int revision)
        {
            PlayerId = playerId;
            EquippedIds = equippedIds;
            Revision = revision;
        }
    }

    // docs/plan/CONTRACTS.md: "TransactionResult | requestId, kabul/red, reasonCode, etkilenen
    // revision | Islemin sahibi -> istegi yapan/UI | P2". ReasonCode is empty when Accepted, else
    // one of the shared vocabulary (InsufficientFunds, InvalidTarget, AlreadyProcessed, ...).
    public readonly struct TransactionResult
    {
        public readonly ulong RequestId;
        public readonly bool Accepted;
        public readonly string ReasonCode;
        public readonly int Revision;

        public TransactionResult(ulong requestId, bool accepted, string reasonCode, int revision)
        {
            RequestId = requestId;
            Accepted = accepted;
            ReasonCode = reasonCode ?? string.Empty;
            Revision = revision;
        }

        public static TransactionResult Ok(ulong requestId, int revision) =>
            new TransactionResult(requestId, true, string.Empty, revision);

        public static TransactionResult Reject(ulong requestId, string reasonCode, int revision) =>
            new TransactionResult(requestId, false, reasonCode, revision);
    }
}
