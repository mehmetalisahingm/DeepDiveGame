using System;
using System.IO;
using DeepDive.Economy;
using DeepDive.Session;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P3-C runtime composition: creates/configures the economy authority, attaches the real
    // recording payment handler, gates shop purchases to town phases and persists the host's
    // last completed economic/progression state on disk.
    [DisallowMultipleComponent]
    public sealed class EconomyRuntimeBinding : MonoBehaviour
    {
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private RecordingWorldBinding recording;
        private bool subscribed;
        private bool loadedForHost;
        private bool paymentBound;
        private bool dirty;
        private bool restoring;
        private string campaignId;
        private string checkpointId;
        private string savePath;

        public EconomyManager Economy => economy;
        public string SavePath => savePath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<EconomyRuntimeBinding>() == null)
                session.gameObject.AddComponent<EconomyRuntimeBinding>();
        }

        private void Awake() => Resolve();
        private void OnEnable() => Resolve();

        private void Resolve()
        {
            if (adapter == null) adapter = GetComponent<SessionNetworkAdapter>();
            if (manager == null) manager = GetComponent<NetworkManager>();
            if (economy == null) economy = GetComponent<EconomyManager>() ?? gameObject.AddComponent<EconomyManager>();
            if (recording == null) recording = GetComponent<RecordingWorldBinding>() ?? gameObject.AddComponent<RecordingWorldBinding>();
            if (string.IsNullOrEmpty(savePath))
                savePath = Path.Combine(Application.persistentDataPath, "DeepDive", "campaign-v1.json");

            economy.ConfigureP3Defaults();
            Subscribe();
        }

        private void Subscribe()
        {
            if (subscribed || economy == null) return;
            economy.OnBalanceChanged += EconomyChanged;
            economy.OnLoadoutChanged += LoadoutChanged;
            subscribed = true;
        }

        private void EconomyChanged()
        {
            if (!restoring) dirty = true;
        }

        private void LoadoutChanged(DeepDive.Core.Contracts.PlayerId player)
        {
            if (!restoring) dirty = true;
        }

        private void Update()
        {
            Resolve();
            if (adapter == null || manager == null || economy == null) return;

            var authority = adapter.IsAuthority && manager.IsListening;
            economy.PurchasesEnabled = authority && adapter.Session.State.Phase != SessionPhase.Dive;

            if (!authority)
            {
                if (paymentBound && recording != null)
                {
                    recording.SetPaymentHandler(null);
                    paymentBound = false;
                }
                loadedForHost = false;
                return;
            }

            if (!loadedForHost) LoadForHost();

            if (!paymentBound && recording != null)
            {
                recording.SetPaymentHandler(economy.TryPayRecording);
                paymentBound = true;
            }

            if (dirty) SaveNow();
        }

        private void LoadForHost()
        {
            restoring = true;
            try
            {
                if (EconomySaveStore.TryLoad(savePath, out var data, out var usedBackup) &&
                    economy.TryRestoreSnapshot(data))
                {
                    campaignId = data.campaignId;
                    checkpointId = data.checkpointId;
                    if (usedBackup) Debug.LogWarning("P3_SAVE_RECOVERED_FROM_BACKUP");
                    Debug.Log($"P3_SAVE_LOADED campaign={campaignId} checkpoint={checkpointId} balance={data.sharedBalance}");
                }
                else
                {
                    campaignId = Guid.NewGuid().ToString("N");
                    checkpointId = "";
                    Debug.Log($"P3_SAVE_NEW_CAMPAIGN campaign={campaignId}");
                }
            }
            finally
            {
                restoring = false;
                dirty = false;
                loadedForHost = true;
            }
        }

        public bool SaveNow()
        {
            if (!loadedForHost || adapter == null || !adapter.IsAuthority || economy == null) return false;
            checkpointId = Guid.NewGuid().ToString("N");
            var snapshot = economy.CreateSaveSnapshot(campaignId, checkpointId);
            if (!EconomySaveStore.TryWriteAtomic(savePath, snapshot, out var error))
            {
                Debug.LogError($"P3_SAVE_FAILED reason={error}");
                dirty = true;
                return false;
            }

            dirty = false;
            Debug.Log($"P3_SAVE_OK campaign={campaignId} checkpoint={checkpointId} balance={snapshot.sharedBalance}");
            return true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || economy == null) return;
            economy.OnBalanceChanged -= EconomyChanged;
            economy.OnLoadoutChanged -= LoadoutChanged;
            subscribed = false;
        }

        private void OnDisable()
        {
            if (dirty) SaveNow();
            if (paymentBound && recording != null) recording.SetPaymentHandler(null);
            paymentBound = false;
            Unsubscribe();
        }

        private void OnDestroy() => Unsubscribe();
    }
}
