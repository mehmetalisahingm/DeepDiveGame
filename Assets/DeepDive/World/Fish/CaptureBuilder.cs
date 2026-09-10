using System;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // Builds the CaptureResult Utku hands to Mert (docs/plan/CONTRACTS.md ortak veri sozlugu:
    // "CaptureResult | captureId, diveId, speciesId, agirlik, varsa kalite, av nesnesi kimligi
    // | Utku -> Mert | P2"). Kept separate from FishActor so it is testable without NGO.
    //
    // The struct carries ids only, never a scene object reference: the receiving side must be
    // able to hold it after the object is gone.
    public static class CaptureBuilder
    {
        // Returns false rather than emitting a half-valid capture. A capture stamped with an
        // empty or stale diveId would be rejected by InventoryManager.TryAddCatch anyway
        // ("capture.DiveId != Session.State.DiveId" -> InvalidTarget), so refusing here keeps
        // the failure at the place that can explain it.
        public static bool TryCreate(IDiveContext dive, string speciesId, int weightGrams,
            ulong catchObjectId, out CaptureResult capture)
        {
            capture = default;
            if (dive == null || !dive.IsDiveActive) return false;
            var diveId = dive.CurrentDiveId;
            if (string.IsNullOrWhiteSpace(diveId)) return false;
            if (string.IsNullOrWhiteSpace(speciesId)) return false;
            if (weightGrams <= 0) return false;

            // Quality stays null in P2: it belongs to P3 recording evaluation, and an invented
            // value here would become a payout input ("Utku kaliteyi, Mert krediyi belirler").
            capture = new CaptureResult(NewCaptureId(), diveId, speciesId, weightGrams, catchObjectId);
            return true;
        }

        // Per-instance unique id. The session-scoped network object id is not an identity on
        // its own ("Oturum ici ag nesnesi kimligi tek basina kalici esya kimligi degildir").
        public static string NewCaptureId() => Guid.NewGuid().ToString("N");
    }
}
