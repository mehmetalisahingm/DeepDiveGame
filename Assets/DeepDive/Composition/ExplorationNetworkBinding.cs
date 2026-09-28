using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.1 composition owner for the real exploration path. World keeps the rules pure; this shell
    // is the only place that turns authoritative network/player/fish state into those rules.
    [DisallowMultipleComponent]
    public sealed class ExplorationNetworkBinding : MonoBehaviour
    {
        private const float DefaultSightingRangeMetres = 18f;
        private const float SightingPollSeconds = 0.25f;

        private sealed class ApprovedPlayerPositions : IExplorerPositionSource
        {
            public void CollectPositions(List<ExplorerPosition> into)
            {
                into.Clear();
                var snapshot = P4MapPositionFeed.SnapshotPlayers();
                for (var i = 0; i < snapshot.Count; i++)
                    into.Add(new ExplorerPosition(snapshot[i].Player, snapshot[i].WorldPosition));
            }
        }

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private InventoryManager inventory;
        private EconomySaveStore saveStore;
        private DayNetworkBinding dayBinding;
        private RecordingDiveBinding recording;

        private ExplorationCellAuthority cells;
        private SpeciesObservationAuthority species;
        private ExplorationPersistenceAdapter persistence;
        private readonly ApprovedPlayerPositions playerPositions = new ApprovedPlayerPositions();
        private readonly HashSet<string> processedCaptureIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> knownSpeciesIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<(string DiveId, ulong Player, string Subject), Vector3> recordingPositions =
            new Dictionary<(string, ulong, string), Vector3>();
        private readonly RaycastHit[] sightHits = new RaycastHit[12];
        private float nextSightingPoll;

        public ExplorationCellAuthority Cells => cells;
        public SpeciesObservationAuthority Species => species;
        public bool IsBound => species != null && ReferenceEquals(ExplorationFeed.Current, species);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<ExplorationNetworkBinding>() == null)
                session.gameObject.AddComponent<ExplorationNetworkBinding>();
        }

        private void OnEnable()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            inventory = GetComponent<InventoryManager>();
            saveStore = GetComponent<EconomySaveStore>();
            dayBinding = GetComponent<DayNetworkBinding>();
            if (inventory != null) inventory.OnBagChanged += BagChanged;
        }

        private void Update()
        {
            if (!IsHostAuthority())
            {
                UnhookRecording();
                ReleaseAuthorities(true);
                return;
            }

            EnsureRecordingHook();
            if (!IsLiveDiveScene()) return;
            if (!EnsureAuthorities()) return;

            cells.Tick(playerPositions);
            ProcessExistingCatches();

            if (Time.unscaledTime >= nextSightingPoll)
            {
                nextSightingPoll = Time.unscaledTime + SightingPollSeconds;
                PollVerifiedSightings();
            }
        }

        private bool IsHostAuthority() =>
            adapter != null && manager != null && manager.IsListening && adapter.IsAuthority;

        private bool IsLiveDiveScene() =>
            adapter != null && !adapter.Connection.IsSceneLoading &&
            SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene;

        private bool EnsureAuthorities()
        {
            if (species != null)
            {
                if (!ReferenceEquals(ExplorationFeed.Current, species)) ExplorationFeed.Bind(species);
                return true;
            }

            if (!DiveRegionField.TryFind(out var region)) return false;
            var water = FindFirstObjectByType<WaterField>();
            cells = new ExplorationCellAuthority(region.RegionId, region.Bounds, water != null ? water.Bodies : null);
            species = new SpeciesObservationAuthority(cells);
            persistence = new ExplorationPersistenceAdapter(cells, species);
            ExplorationFeed.Bind(species);

            if (saveStore == null) saveStore = GetComponent<EconomySaveStore>();
            if (saveStore != null) saveStore.Exploration = persistence;

            CacheKnownSpecies();
            ProcessExistingCatches();
            Debug.Log($"P4_EXPLORATION_BOUND region={region.RegionId} cells={cells.Grid.Columns * cells.Grid.Rows}");
            return true;
        }

        private void EnsureRecordingHook()
        {
            var world = GetComponent<RecordingWorldBinding>();
            var next = world != null ? world.Binding : null;
            if (ReferenceEquals(recording, next)) return;

            UnhookRecording();
            recording = next;
            if (recording == null) return;
            recording.TakeRegistered += RecordingTakeRegistered;
            recording.RecordingQueued += RecordingQueued;
        }

        private void UnhookRecording()
        {
            if (recording == null) return;
            recording.TakeRegistered -= RecordingTakeRegistered;
            recording.RecordingQueued -= RecordingQueued;
            recording = null;
        }

        private void RecordingTakeRegistered(RecordingTake take)
        {
            if (!IsHostAuthority() || species == null || !IsSpeciesId(take.SubjectId)) return;
            if (!P4MapPositionFeed.TryGetPlayerWorldPosition(take.PlayerId, out var observerWorld)) return;
            recordingPositions[(take.DiveId ?? string.Empty, take.PlayerId.Value, take.SubjectId ?? string.Empty)] = observerWorld;
        }

        private void RecordingQueued(RecordingResult result)
        {
            if (!IsHostAuthority() || species == null || !IsSpeciesId(result.SubjectId)) return;
            var key = (result.DiveId ?? string.Empty, result.PlayerId.Value, result.SubjectId ?? string.Empty);
            if (!recordingPositions.TryGetValue(key, out var observerWorld) &&
                !P4MapPositionFeed.TryGetPlayerWorldPosition(result.PlayerId, out observerWorld))
                return;

            var wasKnown = species.TryGetSpecies(result.SubjectId, out _);
            var outcome = species.AcceptRecording(result.RecordingId, result.SubjectId, observerWorld, CurrentDayNumber());
            RecordFirstDiscovery(result.SubjectId, wasKnown, outcome);
        }

        private void BagChanged(PlayerId player)
        {
            if (!IsHostAuthority() || species == null || inventory == null) return;
            ProcessBag(player);
        }

        private void ProcessExistingCatches()
        {
            if (inventory == null) inventory = GetComponent<InventoryManager>();
            if (inventory == null || species == null) return;
            foreach (var pair in inventory.Bags) ProcessBag(pair.Key);
        }

        private void ProcessBag(PlayerId player)
        {
            if (!inventory.Bags.TryGetValue(player, out var bag)) return;
            if (!P4MapPositionFeed.TryGetPlayerWorldPosition(player, out var observerWorld)) return;

            for (var i = 0; i < bag.Items.Count; i++)
            {
                var capture = bag.Items[i];
                if (string.IsNullOrWhiteSpace(capture.CaptureId) || !processedCaptureIds.Add(capture.CaptureId)) continue;
                var wasKnown = species.TryGetSpecies(capture.SpeciesId, out _);
                var outcome = species.AcceptCatch(capture, observerWorld, CurrentDayNumber());
                RecordFirstDiscovery(capture.SpeciesId, wasKnown, outcome);
            }
        }

        // Sightings are never client claims. The host uses its own replicated player pose, its own
        // fish transforms, camera direction, range and physics line-of-sight before World sees one.
        private void PollVerifiedSightings()
        {
            if (species == null) return;
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None);
            var maxDistanceSq = DefaultSightingRangeMetres * DefaultSightingRangeMetres;

            for (var f = 0; f < fish.Length; f++)
            {
                var target = fish[f];
                if (target == null || !target.IsSpawned || !target.IsServer || target.IsDead || target.Species == null) continue;
                var speciesId = target.Species.SpeciesId;
                if (string.IsNullOrWhiteSpace(speciesId)) continue;
                knownSpeciesIds.Add(speciesId);
                if (species.TryGetSpecies(speciesId, out var known) && known.Sighted) continue;

                for (var p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    if (player == null || !player.IsSpawned || !player.IsServer) continue;
                    var eye = player.RecordingEyePosition;
                    var toTarget = target.transform.position - eye;
                    if (toTarget.sqrMagnitude <= 0.0001f || toTarget.sqrMagnitude > maxDistanceSq) continue;

                    var direction = toTarget.normalized;
                    var forward = player.RecordingForwardServer;
                    if (forward.sqrMagnitude <= 0.0001f) continue;
                    var halfFov = Mathf.Clamp(player.RecordingFieldOfView * 0.5f, 10f, 80f);
                    if (Vector3.Dot(forward.normalized, direction) < Mathf.Cos(halfFov * Mathf.Deg2Rad)) continue;
                    if (!HasLineOfSight(player.transform, target.transform, eye, direction, Mathf.Sqrt(toTarget.sqrMagnitude))) continue;

                    var wasKnown = species.TryGetSpecies(speciesId, out _);
                    var outcome = species.AcceptSighting(speciesId, player.transform.position, CurrentDayNumber());
                    RecordFirstDiscovery(speciesId, wasKnown, outcome);
                    if (outcome == SpeciesObservationOutcome.CountedNewEvidence ||
                        outcome == SpeciesObservationOutcome.AlreadyCounted) break;
                }
            }
        }

        private bool HasLineOfSight(Transform playerRoot, Transform targetRoot, Vector3 origin, Vector3 direction, float distance)
        {
            var count = Physics.RaycastNonAlloc(origin, direction, sightHits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = sightHits[i].collider;
                if (collider == null) continue;
                var hit = collider.transform;
                if (hit == playerRoot || hit.IsChildOf(playerRoot)) continue;
                if (hit == targetRoot || hit.IsChildOf(targetRoot)) continue;
                return false;
            }
            return true;
        }

        private void CacheKnownSpecies()
        {
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None);
            for (var i = 0; i < fish.Length; i++)
            {
                var definition = fish[i] != null ? fish[i].Species : null;
                if (definition != null && !string.IsNullOrWhiteSpace(definition.SpeciesId))
                    knownSpeciesIds.Add(definition.SpeciesId);
            }
        }

        private bool IsSpeciesId(string subjectId)
        {
            if (string.IsNullOrWhiteSpace(subjectId)) return false;
            if (knownSpeciesIds.Contains(subjectId)) return true;
            CacheKnownSpecies();
            return knownSpeciesIds.Contains(subjectId);
        }

        private int CurrentDayNumber()
        {
            if (dayBinding == null) dayBinding = GetComponent<DayNetworkBinding>();
            return dayBinding != null && dayBinding.Engine != null ? dayBinding.Engine.DayNumber : 1;
        }

        private void RecordFirstDiscovery(string speciesId, bool wasKnown, SpeciesObservationOutcome outcome)
        {
            if (wasKnown || outcome != SpeciesObservationOutcome.CountedNewEvidence) return;
            if (dayBinding == null) dayBinding = GetComponent<DayNetworkBinding>();
            dayBinding?.Engine?.RecordDiscovery(speciesId);
        }

        private void ReleaseAuthorities(bool persist)
        {
            if (species == null) return;
            if (persist && saveStore != null && ReferenceEquals(saveStore.Exploration, persistence)) saveStore.SaveNow();
            if (ReferenceEquals(ExplorationFeed.Current, species)) ExplorationFeed.Unbind(species);
            if (saveStore != null && ReferenceEquals(saveStore.Exploration, persistence)) saveStore.Exploration = null;
            cells = null;
            species = null;
            persistence = null;
            processedCaptureIds.Clear();
            knownSpeciesIds.Clear();
            recordingPositions.Clear();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnBagChanged -= BagChanged;
            UnhookRecording();
            ReleaseAuthorities(true);
        }
    }
}
