using System;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Progression;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.4-C (#123) host shell for the deep progression authority. World/Mehmet feed it through the Core seams; the campaign file
    // carries it in the SAME save as the day, the money and the boat (late-bound like the exploration and the media). It owns no
    // rule: those live in DeepProgressionAuthority. It only (a) binds the authority and the seams while this machine is the host,
    // (b) re-evaluates the derived stages from the exploration the host already counted, and (c) mirrors the stage to every player.
    [DisallowMultipleComponent]
    public sealed class DeepProgressionNetworkBinding : MonoBehaviour
    {
        private const float EvaluateInterval = 0.5f;
        private const float MirrorInterval = 0.25f;

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomySaveStore saveStore;
        private DeepProgressionAuthority authority;
        private Func<DeepProgressionState> stateProvider;
        private Func<PlayerId, string, string, string, ulong, TransactionResult> traceHandler;
        private Func<PlayerId, string, string, string, ulong, TransactionResult> discoveryHandler;
        private Func<string, string, ulong, TransactionResult> completeHandler;
        private double nextEvaluate;
        private double nextMirror;

        public DeepProgressionAuthority Authority => authority;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<DeepProgressionNetworkBinding>() == null)
                session.gameObject.AddComponent<DeepProgressionNetworkBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private bool IsHost => adapter != null && manager != null && manager.IsListening && adapter.IsAuthority && saveStore != null;

        private void Update()
        {
            // The campaign store is added by its own bootstrap, possibly after this component's Awake: resolve it lazily.
            if (saveStore == null) saveStore = GetComponent<EconomySaveStore>();
            if (!IsHost)
            {
                Release();
                return;
            }

            Ensure();
            var now = Time.unscaledTimeAsDouble;
            if (now >= nextEvaluate)
            {
                nextEvaluate = now + EvaluateInterval;
                // Derived stages read the exploration the host already counted (the live dive authority, or what the campaign
                // file loaded when no dive is running): never a position, never a client report.
                authority.EvaluateExploration(saveStore.GetExplorationSnapshot());
            }
            if (now >= nextMirror)
            {
                nextMirror = now + MirrorInterval;
                Mirror();
            }
        }

        private void Ensure()
        {
            if (authority != null) return;
            authority = new DeepProgressionAuthority(saveStore.SaveNow);
            // Late binding hands the authority what the campaign file already held (an absent record = the closed default).
            saveStore.Progression = authority;
            stateProvider = () => authority.State;
            traceHandler = authority.TrySubmitTrace;
            discoveryHandler = authority.TrySubmitDiscovery;
            completeHandler = authority.TryCompleteBoss;
            BossProgression.Bind(stateProvider);
            DeepProgressionEvidence.Bind(traceHandler, discoveryHandler, completeHandler);
        }

        private void Release()
        {
            if (authority == null) return;
            // Save before letting go so a town read after the dive stays current, then unbind everything this machine bound.
            saveStore.SaveNow();
            BossProgression.Unbind(stateProvider);
            DeepProgressionEvidence.Unbind(traceHandler, discoveryHandler, completeHandler);
            if (ReferenceEquals(saveStore.Progression, authority)) saveStore.Progression = null;
            authority = null;
            stateProvider = null;
            traceHandler = null;
            discoveryHandler = null;
            completeHandler = null;
        }

        private void Mirror()
        {
            var state = authority.State;
            foreach (var pair in manager.ConnectedClients)
            {
                var sync = pair.Value.PlayerObject != null ? pair.Value.PlayerObject.GetComponent<EconomyPlayerSync>() : null;
                if (sync != null) sync.PublishProgression(state);
            }
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
