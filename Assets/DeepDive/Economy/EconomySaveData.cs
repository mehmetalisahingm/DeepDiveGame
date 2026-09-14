using System;
using System.Collections.Generic;

namespace DeepDive.Economy
{
    [Serializable]
    public sealed class EconomySaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public string CampaignId = "";
        public string CheckpointId = "";
        public int SharedBalance;
        public int Revision;
        public List<string> SoldCaptureIds = new List<string>();
        public List<string> PaidRecordingIds = new List<string>();
        public List<EconomyLoadoutSave> Loadouts = new List<EconomyLoadoutSave>();
    }

    [Serializable]
    public sealed class EconomyLoadoutSave
    {
        public ulong PlayerId;
        public List<string> EquipmentIds = new List<string>();
    }
}