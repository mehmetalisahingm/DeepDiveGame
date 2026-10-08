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
        // v4 (P4.1-C) adds exploration (discovered cells + species observations). Older files load with none.
        // v5 (P4.2-C) adds the clip archive, channel publications and the single-right/settle ids.
        // v6 (P4.3-C) adds the vehicle fleet (purchased boats + active boat). Older files load as HasFleet=false: only the repaired rowboat, if any.
        // v7 (P4.4-C) adds the deep progression chain (encyclopedia -> rumor -> trace -> discovery -> boss unlock). Older files load as
        // HasProgression=false, which means the closed default: Locked, nothing counted.
        public const int CurrentSchemaVersion = 8;
        public const int OldestSupportedSchemaVersion = 1;

        // v8 (P4.5-C): daily goals, claim ids, free host role and visible development.
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
        public bool HasExploration;
        public DeepDive.Core.Contracts.ExplorationSaveData Exploration = new DeepDive.Core.Contracts.ExplorationSaveData();
        public List<string> ChannelRightIds = new List<string>();
        public List<string> ChannelSettleIds = new List<string>();
        public bool HasMedia;
        public bool HasFleet;
        public bool HasProgression;
        public DeepDive.Core.Contracts.DeepProgressionSaveData Progression = new DeepDive.Core.Contracts.DeepProgressionSaveData();
        // Only PURCHASED boats are stored: the rowboat's ownership is the P3 repair state (BoatPartIds), a single source.
        public List<string> FleetPurchasedBoatIds = new List<string>();
        public string FleetActiveBoatId = "";
        public DeepDive.Core.Contracts.MediaSaveData Media = new DeepDive.Core.Contracts.MediaSaveData();
        public bool HasLiving;
        public LivingWorldSaveData Living = new LivingWorldSaveData();
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
