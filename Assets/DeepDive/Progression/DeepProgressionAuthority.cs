using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Progression
{
    // P4.4-C (#123): the host's single progression authority for  encyclopedia -> rumor -> trace -> discovery -> boss unlock.
    // Pure C# (no Unity types) so every rule is testable with the real save store and file, like DayEngine/ChannelAuthority.
    //
    // Rules (see P4ProgressionContracts.cs for the stage meaning):
    //  - The chain is strictly ordered; a later step can never be accepted before the earlier one.
    //  - Encyclopedia/Rumor are DERIVED from the exploration save shape the host already counted (EvaluateExploration); the
    //    authority never reads a position and never creates a second exploration record.
    //  - Trace/Discovery arrive only from the host-side world layer and must pass the world validator; with no validator bound
    //    they are refused (fail-closed). Evidence is idempotent by its own id (a trace id counts once; discovery once).
    //  - Every accepted step is persisted before it is acknowledged; a failed write rolls the step back and it can be retried.
    //  - The transient boss encounter is not stored here. A completed boss is recorded once.
    public sealed class DeepProgressionAuthority : IProgressionPersistence
    {
        private const byte OpTrace = 1;
        private const byte OpDiscovery = 2;
        private const byte OpComplete = 3;

        private readonly Func<bool> persist;
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> processed =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();

        private DeepProgressionStage stage = DeepProgressionStage.Locked;
        private string encyclopediaEvidenceId = "";
        private string rumorEvidenceId = "";
        private readonly List<string> traceIds = new List<string>();
        private string discoveryEvidenceId = "";
        private readonly List<string> completedBossIds = new List<string>();
        private int revision;

        public event Action OnChanged;

        // persist: writes the campaign file (EconomySaveStore.SaveNow); null = in-memory only (tests).
        public DeepProgressionAuthority(Func<bool> persist = null) => this.persist = persist;

        public DeepProgressionState State =>
            new DeepProgressionState(stage, traceIds.Count, DeepProgressionIds.RequiredTraceCount, new List<string>(completedBossIds), revision);

        private bool Persist() => persist == null || persist();

        // ---- derived stages: encyclopedia and rumor ------------------------------------------------------------

        // Called by the host shell whenever the exploration read model may have grown. Idempotent: a stage already reached is
        // never produced again, and the evidence ids make a restored campaign re-evaluate to the very same state.
        // Returns true when at least one stage advanced (and was persisted).
        public bool EvaluateExploration(ExplorationSaveData exploration)
        {
            if (exploration == null || stage >= DeepProgressionStage.Rumor) return false;

            var snapshot = Snapshot(); var snapshotRevision = revision;
            var advanced = false;
            if (stage == DeepProgressionStage.Locked)
            {
                var observation = FirstObservationId(exploration);
                if (observation != null)
                {
                    stage = DeepProgressionStage.Encyclopedia;
                    encyclopediaEvidenceId = "enc:" + observation;
                    advanced = true;
                }
            }
            if (stage == DeepProgressionStage.Encyclopedia)
            {
                var reefCell = FirstDiscoveredCell(exploration, DepthBandIds.Reef);
                if (reefCell != null)
                {
                    stage = DeepProgressionStage.Rumor;
                    rumorEvidenceId = "rumor:" + reefCell;
                    advanced = true;
                }
            }
            if (!advanced) return false;

            revision++;
            if (!Persist())
            {
                Rollback(snapshot, snapshotRevision);
                return false;   // not acknowledged: the next evaluation retries
            }
            OnChanged?.Invoke();
            return true;
        }

        private static string FirstObservationId(ExplorationSaveData data)
        {
            var list = data.Observations;
            if (list == null) return null;
            for (var i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (o != null && !string.IsNullOrWhiteSpace(o.ObservationId) && !string.IsNullOrWhiteSpace(o.SpeciesId)) return o.ObservationId;
            }
            return null;
        }

        private static string FirstDiscoveredCell(ExplorationSaveData data, string depthBandId)
        {
            var list = data.DiscoveredCells;
            if (list == null) return null;
            for (var i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (c != null && !string.IsNullOrWhiteSpace(c.CellId) && string.Equals(c.DepthBandId, depthBandId, StringComparison.Ordinal)) return c.CellId;
            }
            return null;
        }

        // ---- host-fed stages: trace and discovery -------------------------------------------------------------

        public TransactionResult TrySubmitTrace(PlayerId player, string traceId, string cellId, string depthBandId, ulong requestId)
        {
            var key = (player, requestId, OpTrace);
            if (processed.TryGetValue(key, out var replayed)) return replayed;

            if (string.IsNullOrWhiteSpace(traceId)) return Cache(key, TransactionResult.Reject(requestId, "InvalidTarget", revision));
            if (stage >= DeepProgressionStage.Trace) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);
            if (stage < DeepProgressionStage.Rumor) return TransactionResult.Reject(requestId, "OutOfOrder", revision);   // not cached: changes on its own
            if (traceIds.Contains(traceId)) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);

            var world = DeepProgressionWorld.Current;
            if (world == null) return TransactionResult.Reject(requestId, "WorldUnavailable", revision);
            if (!world.IsValidTrace(traceId, cellId ?? string.Empty, depthBandId ?? string.Empty))
                return TransactionResult.Reject(requestId, "InvalidContext", revision);

            var snapshot = Snapshot(); var snapshotRevision = revision;
            traceIds.Add(traceId);
            if (traceIds.Count >= DeepProgressionIds.RequiredTraceCount) stage = DeepProgressionStage.Trace;
            revision++;
            if (!Persist())
            {
                Rollback(snapshot, snapshotRevision);
                return TransactionResult.Reject(requestId, "SaveFailed", revision);
            }
            var result = Cache(key, TransactionResult.Ok(requestId, revision));
            OnChanged?.Invoke();
            return result;
        }

        public TransactionResult TrySubmitDiscovery(PlayerId player, string arenaId, string cellId, string depthBandId, ulong requestId)
        {
            var key = (player, requestId, OpDiscovery);
            if (processed.TryGetValue(key, out var replayed)) return replayed;

            if (string.IsNullOrWhiteSpace(arenaId)) return Cache(key, TransactionResult.Reject(requestId, "InvalidTarget", revision));
            if (stage >= DeepProgressionStage.Discovery) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);
            if (stage < DeepProgressionStage.Trace) return TransactionResult.Reject(requestId, "OutOfOrder", revision);

            var world = DeepProgressionWorld.Current;
            if (world == null) return TransactionResult.Reject(requestId, "WorldUnavailable", revision);
            if (!world.IsDiscoveryArea(arenaId, cellId ?? string.Empty, depthBandId ?? string.Empty))
                return TransactionResult.Reject(requestId, "InvalidContext", revision);

            var snapshot = Snapshot(); var snapshotRevision = revision;
            stage = DeepProgressionStage.Discovery;
            discoveryEvidenceId = "discovery:" + arenaId;
            revision++;
            if (!Persist())
            {
                Rollback(snapshot, snapshotRevision);
                return TransactionResult.Reject(requestId, "SaveFailed", revision);
            }
            var result = Cache(key, TransactionResult.Ok(requestId, revision));
            OnChanged?.Invoke();
            return result;
        }

        // The encounter layer reports a finished encounter. Needs the boss to have been unlocked; recorded once per boss.
        public TransactionResult TryCompleteBoss(string bossId, string encounterId, ulong requestId)
        {
            var key = (default(PlayerId), requestId, OpComplete);
            if (processed.TryGetValue(key, out var replayed)) return replayed;

            if (!string.Equals(bossId, DeepProgressionIds.BossId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(encounterId))
                return Cache(key, TransactionResult.Reject(requestId, "InvalidTarget", revision));
            if (stage < DeepProgressionStage.Discovery) return TransactionResult.Reject(requestId, "OutOfOrder", revision);
            if (completedBossIds.Contains(bossId)) return TransactionResult.Reject(requestId, "AlreadyProcessed", revision);

            var snapshot = Snapshot(); var snapshotRevision = revision;
            completedBossIds.Add(bossId);
            revision++;
            if (!Persist())
            {
                Rollback(snapshot, snapshotRevision);
                return TransactionResult.Reject(requestId, "SaveFailed", revision);
            }
            var result = Cache(key, TransactionResult.Ok(requestId, revision));
            OnChanged?.Invoke();
            return result;
        }

        private TransactionResult Cache((PlayerId, ulong, byte) key, TransactionResult result)
        {
            processed[key] = result;
            return result;
        }

        // ---- persistence ------------------------------------------------------------------------------------------

        public DeepProgressionSaveData ExportProgression() => Snapshot();

        // Fail-closed: the stage number alone never counts. Each stage must be backed by its evidence; anything unbacked (a
        // hand-edited stage, missing ids, too few distinct traces, a completion without an unlock) is dropped back to the highest
        // consistent stage. A null/old (absent) record is the default: Locked, nothing counted.
        public bool RestoreProgression(DeepProgressionSaveData data)
        {
            processed.Clear();
            // An absent record (an older campaign file) or one this build does not understand is the closed default.
            if (data == null || data.Version < 1 || data.Version > DeepProgressionIds.CurrentSaveVersion)
            {
                ResetToDefault();
                return true;
            }

            var requested = data.Stage > (byte)DeepProgressionStage.Discovery ? DeepProgressionStage.Discovery : (DeepProgressionStage)data.Stage;
            var consistent = DeepProgressionStage.Locked;

            var enc = Valid(data.EncyclopediaEvidenceId) ? data.EncyclopediaEvidenceId : "";
            var rumor = Valid(data.RumorEvidenceId) ? data.RumorEvidenceId : "";
            var discovery = Valid(data.DiscoveryEvidenceId) ? data.DiscoveryEvidenceId : "";
            var traces = new List<string>();
            if (data.TraceIds != null)
                foreach (var id in data.TraceIds)
                    if (Valid(id) && !traces.Contains(id)) traces.Add(id);

            if (requested >= DeepProgressionStage.Encyclopedia && enc.Length > 0) consistent = DeepProgressionStage.Encyclopedia;
            if (consistent == DeepProgressionStage.Encyclopedia && requested >= DeepProgressionStage.Rumor && rumor.Length > 0) consistent = DeepProgressionStage.Rumor;
            if (consistent == DeepProgressionStage.Rumor && requested >= DeepProgressionStage.Trace && traces.Count >= DeepProgressionIds.RequiredTraceCount) consistent = DeepProgressionStage.Trace;
            if (consistent == DeepProgressionStage.Trace && requested >= DeepProgressionStage.Discovery && discovery.Length > 0) consistent = DeepProgressionStage.Discovery;

            stage = consistent;
            encyclopediaEvidenceId = consistent >= DeepProgressionStage.Encyclopedia ? enc : "";
            rumorEvidenceId = consistent >= DeepProgressionStage.Rumor ? rumor : "";
            discoveryEvidenceId = consistent >= DeepProgressionStage.Discovery ? discovery : "";
            traceIds.Clear();
            // Below the Rumor step no trace may exist; at or above it the saved traces are kept (the chain was backed up to here).
            if (consistent >= DeepProgressionStage.Rumor) traceIds.AddRange(traces);
            completedBossIds.Clear();
            if (consistent == DeepProgressionStage.Discovery && data.CompletedBossIds != null &&
                data.CompletedBossIds.Contains(DeepProgressionIds.BossId))
                completedBossIds.Add(DeepProgressionIds.BossId);
            revision++;
            OnChanged?.Invoke();
            return true;
        }

        private static bool Valid(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 96;

        private void ResetToDefault()
        {
            stage = DeepProgressionStage.Locked;
            encyclopediaEvidenceId = "";
            rumorEvidenceId = "";
            discoveryEvidenceId = "";
            traceIds.Clear();
            completedBossIds.Clear();
            revision++;
            OnChanged?.Invoke();
        }

        private DeepProgressionSaveData Snapshot() => new DeepProgressionSaveData
        {
            Version = DeepProgressionIds.CurrentSaveVersion,
            Stage = (byte)stage,
            EncyclopediaEvidenceId = encyclopediaEvidenceId,
            RumorEvidenceId = rumorEvidenceId,
            TraceIds = new List<string>(traceIds),
            DiscoveryEvidenceId = discoveryEvidenceId,
            CompletedBossIds = new List<string>(completedBossIds)
        };

        // Roll back to an exported snapshot without raising events (a failed write).
        private void Rollback(DeepProgressionSaveData snapshot, int snapshotRevision)
        {
            stage = (DeepProgressionStage)snapshot.Stage;
            encyclopediaEvidenceId = snapshot.EncyclopediaEvidenceId;
            rumorEvidenceId = snapshot.RumorEvidenceId;
            discoveryEvidenceId = snapshot.DiscoveryEvidenceId;
            traceIds.Clear(); traceIds.AddRange(snapshot.TraceIds);
            completedBossIds.Clear(); completedBossIds.AddRange(snapshot.CompletedBossIds);
            revision = snapshotRevision;
        }
    }
}
