using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // Placeholder pending Utku's real recording/target validation (P3-B, issue #35). Shape
    // matches docs/plan/CONTRACTS.md ortak veri sozlugu: "RecordingResult | recordingId, diveId,
    // playerId, tur/olay kimligi, kalite, gecerli sure | Utku -> Mert/Mehmet | P3". Not yet
    // consumed for payment (see EconomyManager) until P3-B produces real recordings.
    public readonly struct RecordingResult
    {
        // #254: which real animal the host saw in this take. Default (None) for an event or an older caller.
        public readonly RecordingSpecimenProof Specimen;

        public readonly string RecordingId;
        public readonly string DiveId;
        public readonly PlayerId PlayerId;
        public readonly string SubjectId;
        public readonly int Quality;
        public readonly float ValidDurationSeconds;

        public RecordingResult(string recordingId, string diveId, PlayerId playerId, string subjectId,
            int quality, float validDurationSeconds, in RecordingSpecimenProof specimen = default)
        {
            Specimen = specimen;
            RecordingId = recordingId;
            DiveId = diveId;
            PlayerId = playerId;
            SubjectId = subjectId;
            Quality = quality;
            ValidDurationSeconds = validDurationSeconds;
        }
    }

    // #254. Explicit values: a save may store the byte, so a later kind is added, never renumbered.
    public enum SpecimenProofKind : byte
    {
        None = 0,          // no animal behind this recording: an event, or a result without a specimen
        FilmOnly = 1,      // this animal was filmed; no catch of it came home in the same dive
        FilmAndCatch = 2   // this animal was filmed and its catch came home in the same dive
    }

    // #254: what the host knows about the animal a recording shows. A close-up cannot change it: the weight is the
    // one rolled when this animal spawned, and SpecimenId names that one life, not the species and not the network
    // object (the same scene fish comes back as a new specimen next dive). CaptureId is set only for FilmAndCatch.
    //
    // Members are properties so default(RecordingSpecimenProof) reads as None with "" ids.
    public readonly struct RecordingSpecimenProof
    {
        private readonly string specimenId;
        private readonly string captureId;

        public SpecimenProofKind Kind { get; }
        public string SpecimenId => specimenId ?? string.Empty;
        public int WeightGrams { get; }
        public string CaptureId => captureId ?? string.Empty;

        public static RecordingSpecimenProof Filmed(string specimenId, int weightGrams) =>
            new RecordingSpecimenProof(specimenId, weightGrams, null);

        public static RecordingSpecimenProof FilmedAndCaught(string specimenId, int weightGrams, string captureId) =>
            new RecordingSpecimenProof(specimenId, weightGrams, captureId);

        // An animal with no id or no real weight proves nothing, so it folds to None rather than half a proof.
        private RecordingSpecimenProof(string specimenId, int weightGrams, string captureId)
        {
            var valid = !string.IsNullOrWhiteSpace(specimenId) && weightGrams > 0;
            var caught = valid && !string.IsNullOrWhiteSpace(captureId);
            Kind = !valid ? SpecimenProofKind.None : caught ? SpecimenProofKind.FilmAndCatch : SpecimenProofKind.FilmOnly;
            this.specimenId = valid ? specimenId : string.Empty;
            WeightGrams = valid ? weightGrams : 0;
            this.captureId = caught ? captureId : string.Empty;
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
