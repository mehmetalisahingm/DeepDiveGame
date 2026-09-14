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

        public string SavePath => string.IsNullOrWhiteSpace(pathOverride)
            ? Path.Combine(Application.persistentDataPath, fileName)
            : pathOverride;
        public string BackupPath => SavePath + ".bak";
        public string LastError { get; private set; } = "";

        private void Awake()
        {
            economy = GetComponent<EconomyManager>();
            inventory = GetComponent<InventoryManager>();
            LoadNow();
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (subscribed || economy == null || inventory == null) return;
            economy.OnBalanceChanged += Changed;
            economy.OnLoadoutChanged += LoadoutChanged;
            inventory.OnDiveSummaryReady += SummaryReady;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            economy.OnBalanceChanged -= Changed;
            economy.OnLoadoutChanged -= LoadoutChanged;
            inventory.OnDiveSummaryReady -= SummaryReady;
            subscribed = false;
        }

        private void Changed()
        {
            if (!restoring) SaveNow();
        }

        private void LoadoutChanged(PlayerId player) => Changed();

        private void SummaryReady(DiveSummary summary)
        {
            checkpointId = summary.CheckpointId ?? "";
            if (!restoring) SaveNow();
        }

        public bool SaveNow()
        {
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (economy == null) return Fail("EconomyManager missing");

            var path = SavePath;
            var temp = path + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var json = JsonUtility.ToJson(economy.ExportSaveData(campaignId, checkpointId), true);
                File.WriteAllText(temp, json);

                // Verify the temporary file before replacing the last completed checkpoint.
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

            // No save is a valid first launch; only report an error when a candidate existed.
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
                if (data == null || !economy.TryRestore(data)) return Fail("invalid or unsupported save");

                restoring = true;
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