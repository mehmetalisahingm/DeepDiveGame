using System;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using UnityEngine;

namespace DeepDive.Economy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EconomyManager), typeof(InventoryManager))]
    public sealed class EconomySaveStore : MonoBehaviour, IExplorationProgressReadModel
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

        private IDayPersistence day;
        private DaySaveData loadedDay;
        private IExplorationPersistence exploration;
        private ExplorationSaveData loadedExploration;
        private IMediaPersistence media;
        private MediaSaveData loadedMedia;
        private IProgressionPersistence progression;
        private DeepProgressionSaveData loadedProgression;
        private LivingWorldSaveData loadedLiving;
        private LivingWorldAuthority living;

        public LivingWorldAuthority Living
        {
            get => living;
            set
            {
                living = value;
                if (living != null) living.Restore(loadedLiving);
            }
        }

        // Same late-binding rule as the day, exploration and media. A progression record that is absent from the file is the closed
        // default (the authority starts Locked); one that is present is restored through the authority's fail-closed validation.
        public IProgressionPersistence Progression
        {
            get => progression;
            set
            {
                progression = value;
                if (progression != null) progression.RestoreProgression(loadedProgression);
            }
        }

        // The exploration the host already counted: the live dive authority's export while it exists, otherwise what the campaign
        // file loaded. Read-only evidence for the progression chain (never a position, never a second exploration record).
        public ExplorationSaveData GetExplorationSnapshot() => exploration != null ? exploration.ExportExploration() : loadedExploration;

        // Same late-binding rule as the day and exploration.
        public IMediaPersistence Media
        {
            get => media;
            set
            {
                media = value;
                if (media != null && loadedMedia != null) media.RestoreMedia(loadedMedia);
            }
        }

        // Same late-binding rule as the day: the host shell that owns the exploration authorities binds after the
        // file was read, and is handed what was already on disk.
        public IExplorationPersistence Exploration
        {
            get => exploration;
            set
            {
                exploration = value;
                if (exploration != null && loadedExploration != null) exploration.RestoreExploration(loadedExploration);
            }
        }

        // Campaign-level progression read: while the dive authority is alive read its current snapshot;
        // in town or after restart fall back to the exploration snapshot loaded from the same campaign file.
        // This keeps Economy independent from the World authority and makes discovery gates scene-safe.
        public bool HasDiscoveredCellInBand(string depthBandId)
        {
            if (string.IsNullOrWhiteSpace(depthBandId)) return false;
            var data = GetExplorationSnapshot();
            var cells = data?.DiscoveredCells;
            if (cells == null) return false;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell != null && string.Equals(cell.DepthBandId, depthBandId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        // The day authority is bound after the file was first read (the store loads in Awake), so a
        // binding that arrives late is handed the day that was already on disk - and every later
        // write carries the day, the money and the boat in the SAME file.
        public IDayPersistence Day
        {
            get => day;
            set
            {
                day = value;
                if (day != null && loadedDay != null) day.RestoreDay(loadedDay);
            }
        }

        public string CampaignId => campaignId;

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
                if (day != null)
                {
                    snapshot.Day = day.ExportDay();
                    snapshot.HasDay = true;
                }
                if (exploration != null)
                {
                    snapshot.Exploration = exploration.ExportExploration();
                    snapshot.HasExploration = true;
                }
                else if (loadedExploration != null)
                {
                    // Nothing bound on this machine right now: carry the saved exploration forward untouched.
                    snapshot.Exploration = loadedExploration;
                    snapshot.HasExploration = true;
                }
                if (media != null)
                {
                    snapshot.Media = media.ExportMedia();
                    snapshot.HasMedia = true;
                }
                else if (loadedMedia != null)
                {
                    snapshot.Media = loadedMedia;
                    snapshot.HasMedia = true;
                }
                if (progression != null)
                {
                    snapshot.Progression = progression.ExportProgression();
                    snapshot.HasProgression = true;
                }
                else if (loadedProgression != null)
                {
                    snapshot.Progression = loadedProgression;
                    snapshot.HasProgression = true;
                }
                if (living != null)
                {
                    snapshot.Living = living.Export();
                    snapshot.HasLiving = true;
                }
                else if (loadedLiving != null)
                {
                    snapshot.Living = loadedLiving;
                    snapshot.HasLiving = true;
                }
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
                // A later re-bind must be handed what is on disk NOW, not what was there at the first load.
                if (snapshot.HasDay) loadedDay = snapshot.Day;
                if (snapshot.HasExploration) loadedExploration = snapshot.Exploration;
                if (snapshot.HasMedia) loadedMedia = snapshot.Media;
                if (snapshot.HasProgression) loadedProgression = snapshot.Progression;
                if (snapshot.HasLiving) loadedLiving = snapshot.Living;
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
                loadedDay = data.HasDay ? data.Day : null;
                loadedExploration = data.HasExploration ? data.Exploration : null;
                loadedMedia = data.HasMedia ? data.Media : null;
                loadedProgression = data.HasProgression ? data.Progression : null;
                loadedLiving = data.HasLiving ? data.Living : null;
                if (living != null && !living.Restore(loadedLiving))
                {
                    restoring = false;
                    return Fail("invalid living world state");
                }
                if (progression != null && !progression.RestoreProgression(loadedProgression))
                {
                    restoring = false;
                    return Fail("invalid progression state");
                }
                if (loadedMedia != null && media != null && !media.RestoreMedia(loadedMedia))
                {
                    restoring = false;
                    return Fail("invalid media state");
                }
                if (loadedExploration != null && exploration != null && !exploration.RestoreExploration(loadedExploration))
                {
                    restoring = false;
                    return Fail("invalid exploration state");
                }
                if (loadedDay != null && day != null && !day.RestoreDay(loadedDay))
                {
                    restoring = false;
                    return Fail("invalid day state");
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
