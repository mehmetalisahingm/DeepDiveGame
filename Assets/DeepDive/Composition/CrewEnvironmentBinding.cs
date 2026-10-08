using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.5-A (#130): host-only consumer of Utku's current/weather seams. World decides what the
    // current/weather is; this layer only samples it at authoritative player transforms, clamps
    // current velocity, replicates the warning and applies drift while swimming.
    [DisallowMultipleComponent]
    public sealed class CrewEnvironmentBinding : MonoBehaviour
    {
        private const float RefreshSeconds = 0.20f;
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private float nextRefresh;
        private readonly List<ICrewCurrentSource> currentSources = new List<ICrewCurrentSource>();
        private readonly List<ICrewWeatherAdvisorySource> weatherSources = new List<ICrewWeatherAdvisorySource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<CrewEnvironmentBinding>() == null)
                session.gameObject.AddComponent<CrewEnvironmentBinding>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private void Update()
        {
            if (adapter == null || manager == null || !manager.IsListening || !adapter.IsAuthority) return;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;

            DiscoverSources();
            var advisory = ResolveAdvisory();

            foreach (var pair in manager.ConnectedClients)
            {
                if (pair.Value.PlayerObject == null) continue;
                var player = pair.Value.PlayerObject.GetComponent<NetworkPlayer>();
                if (player == null || !player.IsSpawned || !player.IsServer) continue;

                var drift = Vector3.zero;
                var warning = advisory.Warning;
                for (var i = 0; i < currentSources.Count; i++)
                {
                    var source = currentSources[i];
                    if (source == null || !source.TrySampleCurrent(player.transform.position, out var sample)) continue;
                    drift = CrewEnvironmentRules.SanitizeCurrent(sample.DriftMetresPerSecond);
                    if (sample.WarnPlayer) warning = CrewWarningKind.LocalCurrent;
                    break;
                }

                if (warning == CrewWarningKind.None && !advisory.OutboundAllowed && advisory.ReturnAllowed)
                    warning = CrewWarningKind.ReturnRecommended;

                player.SetCrewEnvironmentServer(drift, warning);
            }
        }

        private void DiscoverSources()
        {
            currentSources.Clear();
            weatherSources.Clear();
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour == this) continue;
                if (behaviour is ICrewCurrentSource current) currentSources.Add(current);
                if (behaviour is ICrewWeatherAdvisorySource weather) weatherSources.Add(weather);
            }
        }

        private CrewEnvironmentAdvisory ResolveAdvisory()
        {
            for (var i = 0; i < weatherSources.Count; i++)
            {
                var source = weatherSources[i];
                if (source == null) continue;
                var advisory = source.CurrentAdvisory;
                // Safety invariant from P4.5: bad weather may block departure, never the team's return.
                if (!advisory.ReturnAllowed)
                    return new CrewEnvironmentAdvisory(advisory.Warning, advisory.OutboundAllowed, true);
                return advisory;
            }
            return CrewEnvironmentAdvisory.Clear;
        }
    }
}
