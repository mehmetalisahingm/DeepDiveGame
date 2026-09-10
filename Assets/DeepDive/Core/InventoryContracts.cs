using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // Placeholder pending Utku's real species/catch definitions (P2-B, issue #21). Shape
    // matches docs/plan/CONTRACTS.md ortak veri sozlugu: "CaptureResult | captureId, diveId,
    // speciesId, agirlik, varsa kalite, av nesnesi kimligi | Utku -> Mert | P2".
    public readonly struct CaptureResult
    {
        public readonly string CaptureId;
        public readonly string DiveId;
        public readonly string SpeciesId;
        public readonly int WeightGrams;
        public readonly int? Quality;
        public readonly ulong CatchObjectId;

        public CaptureResult(string captureId, string diveId, string speciesId, int weightGrams,
            ulong catchObjectId, int? quality = null)
        {
            CaptureId = captureId;
            DiveId = diveId;
            SpeciesId = speciesId;
            WeightGrams = weightGrams;
            CatchObjectId = catchObjectId;
            Quality = quality;
        }
    }

    // docs/plan/CONTRACTS.md: "DiveSummary | Guvenli donenler, korunan av/cekim kimlikleri,
    // kayiplar ve kontrol noktasi kimligi | Mert -> Mehmet/Utku/UI | P2". Parasal alanlar P3.
    public readonly struct DiveSummary
    {
        public readonly string DiveId;
        public readonly IReadOnlyList<PlayerId> SafelyReturned;
        public readonly IReadOnlyList<string> PreservedCaptureIds;
        public readonly IReadOnlyList<string> LostCaptureIds;
        public readonly string CheckpointId;

        public DiveSummary(string diveId, IReadOnlyList<PlayerId> safelyReturned,
            IReadOnlyList<string> preservedCaptureIds, IReadOnlyList<string> lostCaptureIds, string checkpointId)
        {
            DiveId = diveId;
            SafelyReturned = safelyReturned;
            PreservedCaptureIds = preservedCaptureIds;
            LostCaptureIds = lostCaptureIds;
            CheckpointId = checkpointId;
        }
    }
}
