using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World
{
    public static class DeepEncounterWorld
    {
        public const string ArenaId = "arena-deep-1";
        public const string EncounterId = "encounter-deep-1";
        public const float TraceRadius = 1.6f;
        public const float ArenaRadius = 2.4f;

        public static readonly string[] TraceIds = { "deep-trace-1", "deep-trace-2", "deep-trace-3" };
        public static readonly Vector3[] TracePositions =
        {
            new Vector3(-7f, -16f, 50f),
            new Vector3( 7f, -17f, 54f),
            new Vector3(-4f, -18f, 60f)
        };

        public static readonly Vector3 ArenaPosition = new Vector3(5f, -18f, 59f);
        public static readonly Vector3 BossPosition = new Vector3(5f, -18f, 60.5f);
        public static string ExpectedCellId(Vector3 position, string regionId, DiveRegionBounds bounds)
        {
            var layout = new ExplorationCellLayout(regionId, bounds);
            if (!layout.TryCell(position, out var gx, out var gz)) return string.Empty;
            return ExplorationIds.CellId(regionId, gx, gz);
        }

        public static bool IsValidTraceContext(string traceId, string cellId, string depthBandId,
            string regionId, DiveRegionBounds bounds)
        {
            if (!string.Equals(depthBandId, DepthBandIds.Deep, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(cellId) || string.IsNullOrWhiteSpace(regionId))
                return false;

            for (var i = 0; i < TraceIds.Length; i++)
            {
                if (!string.Equals(traceId, TraceIds[i], StringComparison.Ordinal)) continue;
                return string.Equals(cellId, ExpectedCellId(TracePositions[i], regionId, bounds), StringComparison.Ordinal);
            }
            return false;
        }

        public static bool IsValidArenaContext(string arenaId, string cellId, string depthBandId,
            string regionId, DiveRegionBounds bounds) =>
            string.Equals(arenaId, ArenaId, StringComparison.Ordinal) &&
            string.Equals(depthBandId, DepthBandIds.Deep, StringComparison.Ordinal) &&
            string.Equals(cellId, ExpectedCellId(ArenaPosition, regionId, bounds), StringComparison.Ordinal);
    }

    // Real P4.4 world validator + host evidence feed. Progression remains Mert's authority and
    // encounter health remains Mehmet's authority; this class only owns where the evidence exists.
    [DisallowMultipleComponent]
    public sealed class DeepProgressionWorldRuntime : MonoBehaviour, IDeepProgressionWorld
    {
        private DiveRegionField region;
        private WaterField water;
        private ExplorationCellLayout layout;
        private readonly HashSet<string> submitted = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Renderer> traceRenderers = new List<Renderer>();
        private Renderer bossRenderer;
        private Collider bossCollider;
        private double nextProbe;
        private ulong requestSequence = 0xD44E000000000000UL;
        private bool hadPlayerInArena;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, "DiveTestArea", StringComparison.Ordinal)) return;
            var existing = FindFirstObjectByType<DeepProgressionWorldRuntime>();
            if (existing != null) return;
            var root = new GameObject("P44DeepWorld");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<DeepProgressionWorldRuntime>();
        }

        private void Awake()
        {
            EnsureWorld();
            BuildPresentation();
        }

        private void OnEnable()
        {
            EnsureWorld();
            DeepProgressionWorld.Bind(this);
        }

        private void OnDisable()
        {
            DeepProgressionWorld.Unbind(this);
            BossEncounterRuntime.Abort();
        }

        private void Update()
        {
            EnsureWorld();
            UpdatePresentation();

            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening || !manager.IsServer || region == null || water == null)
                return;

            var now = Time.unscaledTimeAsDouble;
            if (now < nextProbe) return;
            nextProbe = now + 0.12;

            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                .Where(p => p.IsSpawned && !p.Passive.Value).ToArray();
            var anyInArena = false;

            foreach (var player in players)
            {
                var position = player.transform.position;
                if (!TryContext(position, out var cellId, out var band)) continue;
                var playerId = new PlayerId(player.OwnerClientId);

                for (var i = 0; i < DeepEncounterWorld.TraceIds.Length; i++)
                {
                    if (Vector3.Distance(position, DeepEncounterWorld.TracePositions[i]) > DeepEncounterWorld.TraceRadius)
                        continue;

                    var key = playerId.Value + ":" + DeepEncounterWorld.TraceIds[i];
                    if (submitted.Contains(key)) continue;
                    var result = DeepProgressionEvidence.TrySubmitTrace(
                        playerId, DeepEncounterWorld.TraceIds[i], cellId, band, ++requestSequence);
                    if (result.Accepted || string.Equals(result.ReasonCode, "AlreadyProcessed", StringComparison.Ordinal))
                        submitted.Add(key);
                }

                if (Vector3.Distance(position, DeepEncounterWorld.ArenaPosition) <= DeepEncounterWorld.ArenaRadius)
                {
                    anyInArena = true;
                    var result = DeepProgressionEvidence.TrySubmitDiscovery(
                        playerId, DeepEncounterWorld.ArenaId, cellId, band, ++requestSequence);
                    if (result.Accepted || string.Equals(result.ReasonCode, "AlreadyProcessed", StringComparison.Ordinal))
                        BossEncounterRuntime.TryActivate(DeepEncounterWorld.EncounterId);
                }
            }

            if (hadPlayerInArena && !anyInArena && BossEncounterRuntime.Snapshot.Phase == BossEncounterPhase.Active)
                BossEncounterRuntime.Abort();
            hadPlayerInArena = anyInArena;
        }

        public bool IsValidTrace(string traceId, string cellId, string depthBandId) =>
            EnsureWorld() && DeepEncounterWorld.IsValidTraceContext(
                traceId, cellId, depthBandId, region.RegionId, region.Bounds);

        public bool IsDiscoveryArea(string arenaId, string cellId, string depthBandId) =>
            EnsureWorld() && DeepEncounterWorld.IsValidArenaContext(
                arenaId, cellId, depthBandId, region.RegionId, region.Bounds);

        private bool EnsureWorld()
        {
            if (region == null) region = FindFirstObjectByType<DiveRegionField>();
            if (water == null)
            {
                water = FindFirstObjectByType<WaterField>();
                if (water != null) water.RebuildVolumes();
            }
            if (region == null || water == null) return false;
            layout = new ExplorationCellLayout(region.RegionId, region.Bounds);
            return layout.IsValid;
        }

        private bool TryContext(Vector3 position, out string cellId, out string band)
        {
            cellId = string.Empty;
            band = DepthBandIds.Unclassified;
            if (!EnsureWorld() || !layout.TryCell(position, out var gx, out var gz)) return false;
            if (!WaterDepth.TryClassify(water.Bodies, position, out band)) return false;
            cellId = ExplorationIds.CellId(region.RegionId, gx, gz);
            return true;
        }

        private string CellIdFor(Vector3 position)
        {
            if (!layout.TryCell(position, out var gx, out var gz)) return string.Empty;
            return ExplorationIds.CellId(region.RegionId, gx, gz);
        }

        private void BuildPresentation()
        {
            if (traceRenderers.Count > 0) return;
            for (var i = 0; i < DeepEncounterWorld.TraceIds.Length; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "DeepTrace_" + (i + 1);
                marker.transform.SetParent(transform, false);
                marker.transform.position = DeepEncounterWorld.TracePositions[i];
                marker.transform.localScale = Vector3.one * 0.65f;
                var collider = marker.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                traceRenderers.Add(marker.GetComponent<Renderer>());
            }

            var boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            boss.name = "DeepBoss_Target";
            boss.transform.SetParent(transform, false);
            boss.transform.position = DeepEncounterWorld.BossPosition;
            boss.transform.localScale = new Vector3(1.7f, 2.4f, 1.7f);
            boss.AddComponent<DeepBossHarpoonTarget>();
            bossRenderer = boss.GetComponent<Renderer>();
            bossCollider = boss.GetComponent<Collider>();
        }

        private void UpdatePresentation()
        {
            var stage = BossProgression.State.Stage;
            var showTraces = stage >= DeepProgressionStage.Rumor && stage < DeepProgressionStage.Trace;
            for (var i = 0; i < traceRenderers.Count; i++)
                if (traceRenderers[i] != null) traceRenderers[i].enabled = showTraces;

            var boss = BossEncounterRuntime.Snapshot;
            if (bossRenderer != null) bossRenderer.enabled = boss.IsVisible;
            if (bossCollider != null) bossCollider.enabled = boss.Phase == BossEncounterPhase.Active;
        }
    }

    // The collider is only a host raycast target. It owns no health/network/save state.
    public sealed class DeepBossHarpoonTarget : MonoBehaviour, IHarpoonTarget
    {
        public PlayerActionResult TryApplyHarpoonHit(HarpoonHit hit) =>
            BossEncounterRuntime.TryHit(hit);
    }
}
