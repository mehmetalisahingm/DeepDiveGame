using System;
using System.Collections.Generic;

namespace DeepDive.Economy
{
    [Serializable]
    public sealed class EconomySaveData
    {
        // v2 (P3.2-C) adds pending turn-ins and boat repair progress. v1 saves stay loadable: the new
        // lists are simply absent/empty, which means "nothing pending, boat still broken".
        public const int CurrentSchemaVersion = 2;
        public const int OldestSupportedSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string CampaignId = "";
        public string CheckpointId = "";
        public int SharedBalance;
        public int Revision;
        public List<string> SoldCaptureIds = new List<string>();
        public List<string> PaidRecordingIds = new List<string>();
        public List<EconomyLoadoutSave> Loadouts = new List<EconomyLoadoutSave>();
        public List<PendingTurnInSave> PendingTurnIns = new List<PendingTurnInSave>();
        public List<string> BoatPartIds = new List<string>();
    }

    [Serializable]
    public sealed class EconomyLoadoutSave
    {
        public ulong PlayerId;
        public List<string> EquipmentIds = new List<string>();
    }

    // Session client ids are not persistent identities (D06), so a saved pending item never names a
    // non-host carrier: it is written as shared escrow that any player may hand in.
    [Serializable]
    public sealed class PendingTurnInSave
    {
        public string ItemId = "";
        public byte Kind;
        public string SourceDiveId = "";
        public string SubjectId = "";
        public int WeightGrams;
        public int Quality;
        public float ValidDurationSeconds;
        public ulong CarrierPlayerId;
        public bool SharedEscrow;
        public int Revision;
    }
}
