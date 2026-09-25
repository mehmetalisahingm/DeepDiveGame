using System;
using System.Collections.Generic;

namespace DeepDive.Economy
{
    [Serializable]
    public sealed class EconomySaveData
    {
        // v2 (P3.2-C) adds pending turn-ins and boat repair progress. v1 saves stay loadable: the new
        // lists are simply absent/empty, which means "nothing pending, boat still broken".
        // v3 (P4.1-C) adds the campaign day. v1/v2 files load as HasDay=false, which means "day 1, 08:00".
        public const int CurrentSchemaVersion = 3;
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
        // v3: safe catches parked in the shared home storage. Always shared (D06: no persistent non-host ids).
        public List<PendingTurnInSave> StoredItems = new List<PendingTurnInSave>();
        public bool HasDay;
        public DeepDive.Core.Contracts.DaySaveData Day = new DeepDive.Core.Contracts.DaySaveData();
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
