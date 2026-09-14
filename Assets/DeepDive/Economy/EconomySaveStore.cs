using System;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EconomyManager), typeof(InventoryManager))]
    public sealed class EconomySaveStore : MonoBehaviour
    {
        [SerializeField] private string fileName = "deepdive-campaign.json";
        [SerializeField] private string campaignId = "local-host";

        private EconomyManager economy;
        private InventoryManager inventory;
        private string checkpointId = "";
        private string pathOverride;
        private bool subscribed;
        private bool restoring;
        public Func<bool> CanWrite { get; set; }

        public string SavePath => string.IsNullOrWhiteSpace(pathOverride)
            ? Path.Combine(Application.persistentDataPath, fileName)
            : pathOverride;
        public string BackupPath => SavePath + ".bak";
        public string LastError { get; private set; } = "";

        private void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-p1-report") pathOverride = args[i + 1] + ".campaign.json";
            economy = GetComponent<EconomyManager>();
            inventory = GetComponent<InventoryManager>();
            LoadNow();
            economy?.SetPersistenceHandler(SaveNow);
            Subscribe();
        }

        private void OnEnable()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            economy?.SetPersistenceHandler(SaveNow);
            Subscribe();
        }

        private void OnDisable()
        {
            economy?.ClearPersistenceHandler(SaveNow);
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed || inventory == null) return;
            inventory.OnDiveSummaryReady += SummaryReady;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inventory == null) return;
            inventory.OnDiveSummaryReady -= SummaryReady;
            subscribed = false;
        }

        private void SummaryReady(DiveSummary summary)
        {
            if (CanWrite != null && !CanWrite()) return;
            checkpointId = summary.CheckpointId ?? "";
            // EconomyManager may already have persisted the sale atomically. Write once more so
            // a zero-value dive still advances the last completed checkpoint.
            if (!restoring) SaveNow();
        }

        public bool SaveNow()
        {
            if (CanWrite != null && !CanWrite()) return false;
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (economy == null) return Fail("EconomyManager missing");

            var path = SavePath;
            var temp = path + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var effectiveCheckpoint = string.IsNullOrWhiteSpace(economy.LastCheckpointId)
                    ? checkpointId
                    : economy.LastCheckpointId;
                var snapshot = economy.ExportSaveData(campaignId, effectiveCheckpoint);
                // Session client IDs are not persistent identities. D06 saves the host's loadout only.
                snapshot.Loadouts.RemoveAll(x => x.PlayerId != 0);
                var json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(temp, json);

                var verified = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(temp));
                if (verified == null || verified.SchemaVersion != EconomySaveData.CurrentSchemaVersion)
                    throw new InvalidDataException("save verification failed");

                if (File.Exists(path))
                {
                    try { File.Replace(temp, path, BackupPath, true); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(path, BackupPath, true);
                        File.Delete(path);
                        File.Move(temp, path);
                    }
                }
                else File.Move(temp, path);

                checkpointId = effectiveCheckpoint ?? "";
                LastError = "";
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                return Fail(ex.Message);
            }
        }

        public bool LoadNow()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (economy == null) return Fail("EconomyManager missing");

            if (TryLoadPath(SavePath)) return true;
            if (File.Exists(BackupPath) && TryLoadPath(BackupPath)) return true;

            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
            {
                LastError = "";
                return true;
            }
            return false;
        }

        private bool TryLoadPath(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                var data = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
                if (data == null) return Fail("invalid save json");
                data.Loadouts?.RemoveAll(x => x == null || x.PlayerId != 0);

                restoring = true;
                if (!economy.TryRestore(data))
                {
                    restoring = false;
                    return Fail("invalid or unsupported save");
                }
                campaignId = string.IsNullOrWhiteSpace(data.CampaignId) ? campaignId : data.CampaignId;
                checkpointId = data.CheckpointId ?? "";
                restoring = false;
                LastError = "";
                return true;
            }
            catch (Exception ex)
            {
                restoring = false;
                return Fail(ex.Message);
            }
        }

        public void SetPathForTests(string path) => pathOverride = path;

        private bool Fail(string message)
        {
            LastError = message ?? "SaveFailed";
            Debug.LogWarning($"P3_SAVE_FAILED {LastError}");
            return false;
        }
    }
}
