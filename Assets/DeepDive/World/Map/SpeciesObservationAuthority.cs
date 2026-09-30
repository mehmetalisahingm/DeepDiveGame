using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    public enum SpeciesObservationOutcome : byte
    {
        // Missing id/species, or a field the contract does not allow (e.g. a whitespace band).
        Invalid = 0,

        // This ObservationId was counted before (a retry, replay, reload or the day close reading
        // it again), or a sighting of an already sighted species. Nothing changed.
        AlreadyCounted = 1,

        // A new observation was recorded, but the species already had this evidence (a second
        // catch of the same species): habitat may grow, no new encyclopedia step.
        Counted = 2,

        // A new observation that gave the species evidence it did not have yet.
        CountedNewEvidence = 3,

        // The observer is not in a valid cell of this region (or a stored observation names another
        // region / no cell). Not recorded. A missing depth band inside the region is NOT this.
        OutOfRegion = 4
    }

    // Host state of species observation (P4.1-B #89, CONTRACTS "SpeciesDiscoveryState"): which
    // species have been sighted / recorded / caught, and the one IExplorationReadModel Mert's map and
    // encyclopedia read (cells from ExplorationCellAuthority + species from here).
    //
    // ObservationId comes from the event, never from a tick, a timestamp or a position:
    //   - Caught:   the capture's own CaptureId.
    //   - Recorded: the recording's own RecordingId.
    //   - Sighted:  minted by the host on first acceptance as SightingId(speciesId). Only the first
    //               sighting of a species is ever an observation ("ayni turun baska baligini gormek
    //               ikinci kesif odulu yaratmaz"), so the id is fixed per species and a reload or
    //               replay re-derives nothing - it applies the stored observation through TryApply.
    // An id is counted once, whatever path brings it back.
    //
    // CellId and DepthBandId are the observer's exploration cell and that cell's band at acceptance
    // (ExplorationCellAuthority.TryGetCell) - coarse and stable. Flow: world position -> valid region
    // -> valid cell -> observation; no cell means no observation (OutOfRegion). The band may be
    // DepthBandIds.Unclassified (> 8 m, not seen wet yet) - that is not an invalid region. The
    // observer position is only used for that lookup and never stored; no creature position is an
    // input at all.
    //
    // Verification (range, line of sight, "is this really a first encounter") is the caller's: the
    // Composition binding decides it with the scene. This class takes only already-verified events and
    // has no Physics, Transform or raycast.
    //
    // Pure class: no MonoBehaviour, no scene, no UI, no save. The host shell feeds it the verified
    // events; Mert's save later stores Observations and replays them through TryApply.
    public sealed class SpeciesObservationAuthority : IExplorationReadModel
    {
        private const string SightingPrefix = "sighting:";

        private sealed class SpeciesRecord
        {
            public readonly string SpeciesId;
            public bool Sighted;
            public bool Recorded;
            public bool Caught;
            public readonly List<string> BandIds = new List<string>(3);
            public readonly List<string> CellIds = new List<string>(4);
            public readonly List<string> ObservationIds = new List<string>(4);

            // The first Recorded observation of this species in acceptance order (#101). Set once in
            // Record, so a replay or reload through TryApply in the same order sets the same id.
            public string FirstRecordedId;

            public SpeciesRecord(string speciesId) => SpeciesId = speciesId;
        }

        private readonly ExplorationCellAuthority cells;
        private readonly HashSet<string> countedIds = new HashSet<string>(StringComparer.Ordinal);

        // ObservationId -> index in observations, so a recording's context is read without a scan.
        private readonly Dictionary<string, int> observationIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, SpeciesRecord> species = new Dictionary<string, SpeciesRecord>(StringComparer.Ordinal);
        private readonly List<SpeciesObservation> observations = new List<SpeciesObservation>();

        private int revision;
        private SpeciesDiscoveryState[] cachedSpecies = Array.Empty<SpeciesDiscoveryState>();
        private int cachedSpeciesRevision;
        private ExplorationSnapshot cachedSnapshot;
        private int cachedRevision = -1;

        public SpeciesObservationAuthority(ExplorationCellAuthority cells)
        {
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }

        // Rises on every change to cells or species (both parts only ever go up).
        public int Revision => cells.Revision + revision;

        // Every counted observation in acceptance order - what a save stores and replays.
        public IReadOnlyList<SpeciesObservation> Observations => observations;

        public static string SightingId(string speciesId) => SightingPrefix + speciesId;

        public bool IsCounted(string observationId) => observationId != null && countedIds.Contains(observationId);

        public bool TryGetSpecies(string speciesId, out SpeciesDiscoveryState state)
        {
            state = default;
            if (speciesId == null || !species.TryGetValue(speciesId, out var record)) return false;
            state = ToState(record);
            return true;
        }

        // #101: the world context of a counted species recording. True only when a Recorded observation with
        // ObservationId == recordingId exists; region, cell and band are that observation's own (not the
        // cell's band now), and FirstRecordingOfSubject says whether it is the species' first Recorded
        // observation. Read only - it accepts nothing - and allocation-free: the strings are the stored ones.
        public bool TryGetRecordingContext(string recordingId, out RecordingWorldContext context)
        {
            context = default;
            if (recordingId == null || !observationIndex.TryGetValue(recordingId, out var index)) return false;

            var observation = observations[index];
            if (observation.Evidence != SpeciesEvidence.Recorded) return false;

            var first = species.TryGetValue(observation.SpeciesId, out var record) &&
                        string.Equals(record.FirstRecordedId, observation.ObservationId, StringComparison.Ordinal);
            context = new RecordingWorldContext(RecordingSubjectKind.Species, observation.RegionId, observation.CellId,
                observation.DepthBandId, first);
            return true;
        }

        // A verified capture (CaptureResult: its SpeciesId is the same species schema, e.g. "sea_bass").
        public SpeciesObservationOutcome AcceptCatch(in CaptureResult capture, Vector3 observerWorld, int dayNumber) =>
            Accept(capture.CaptureId, capture.SpeciesId, SpeciesEvidence.Caught, observerWorld, dayNumber);

        // A valid recording of a species. speciesId is passed explicitly: RecordingResult.SubjectId
        // can be an event id (event_bioluminescence), which is not a species and must not come here.
        public SpeciesObservationOutcome AcceptRecording(string recordingId, string speciesId, Vector3 observerWorld,
            int dayNumber) =>
            Accept(recordingId, speciesId, SpeciesEvidence.Recorded, observerWorld, dayNumber);

        // A host-verified first encounter. Checked before any id is built, so seeing the same species
        // again every tick neither mints nor allocates.
        public SpeciesObservationOutcome AcceptSighting(string speciesId, Vector3 observerWorld, int dayNumber)
        {
            if (IsBlank(speciesId)) return SpeciesObservationOutcome.Invalid;
            if (species.TryGetValue(speciesId, out var record) && record.Sighted)
                return SpeciesObservationOutcome.AlreadyCounted;
            return Accept(SightingId(speciesId), speciesId, SpeciesEvidence.Sighted, observerWorld, dayNumber);
        }

        // Re-applies a stored observation as it is (reload, replay, day close): its own id, cell and
        // band are kept, nothing is re-derived. Counted once per id.
        public SpeciesObservationOutcome TryApply(in SpeciesObservation observation)
        {
            if (!observation.IsValid || IsBlank(observation.ObservationId) || IsBlank(observation.SpeciesId))
                return SpeciesObservationOutcome.Invalid;
            if (!DepthBandIds.IsValidOrUnclassified(observation.DepthBandId)) return SpeciesObservationOutcome.Invalid;
            if (observation.Evidence > SpeciesEvidence.Caught) return SpeciesObservationOutcome.Invalid;
            if (countedIds.Contains(observation.ObservationId)) return SpeciesObservationOutcome.AlreadyCounted;
            if (IsBlank(observation.CellId) ||
                !string.Equals(observation.RegionId, cells.Grid.RegionId, StringComparison.Ordinal))
                return SpeciesObservationOutcome.OutOfRegion;
            return Record(observation);
        }

        private SpeciesObservationOutcome Accept(string observationId, string speciesId, SpeciesEvidence evidence,
            Vector3 observerWorld, int dayNumber)
        {
            if (IsBlank(observationId) || IsBlank(speciesId)) return SpeciesObservationOutcome.Invalid;
            if (countedIds.Contains(observationId)) return SpeciesObservationOutcome.AlreadyCounted;

            // No valid region/cell -> no observation. Checked before any id is stored.
            if (!cells.TryGetCell(observerWorld, out var cellId, out var bandId))
                return SpeciesObservationOutcome.OutOfRegion;
            return Record(new SpeciesObservation(observationId, speciesId, evidence, cells.Grid.RegionId,
                cellId, bandId, dayNumber));
        }

        private SpeciesObservationOutcome Record(in SpeciesObservation observation)
        {
            if (!species.TryGetValue(observation.SpeciesId, out var record))
            {
                record = new SpeciesRecord(observation.SpeciesId);
                species.Add(observation.SpeciesId, record);
            }

            var newEvidence = false;
            switch (observation.Evidence)
            {
                case SpeciesEvidence.Sighted: newEvidence = !record.Sighted; record.Sighted = true; break;
                case SpeciesEvidence.Recorded: newEvidence = !record.Recorded; record.Recorded = true; break;
                case SpeciesEvidence.Caught: newEvidence = !record.Caught; record.Caught = true; break;
            }

            if (observation.Evidence == SpeciesEvidence.Recorded && record.FirstRecordedId == null)
                record.FirstRecordedId = observation.ObservationId;

            AddDistinct(record.BandIds, observation.DepthBandId);
            AddDistinct(record.CellIds, observation.CellId);
            record.ObservationIds.Add(observation.ObservationId);
            countedIds.Add(observation.ObservationId);
            observationIndex.Add(observation.ObservationId, observations.Count);
            observations.Add(observation);
            revision++;

            return newEvidence ? SpeciesObservationOutcome.CountedNewEvidence : SpeciesObservationOutcome.Counted;
        }

        // Cells from ExplorationCellAuthority, species sorted by id (ordinal) so the order does not
        // depend on which species happened to be seen first. Rebuilt only when Revision moves.
        public ExplorationSnapshot Snapshot()
        {
            var current = Revision;
            if (cachedRevision == current) return cachedSnapshot;

            if (cachedSpeciesRevision != revision)
            {
                var list = new SpeciesDiscoveryState[species.Count];
                var i = 0;
                foreach (var record in species.Values) list[i++] = ToState(record);
                Array.Sort(list, (a, b) => string.CompareOrdinal(a.SpeciesId, b.SpeciesId));
                cachedSpecies = list;
                cachedSpeciesRevision = revision;
            }

            var cellSnapshot = cells.Snapshot();
            cachedSnapshot = new ExplorationSnapshot(cellSnapshot.Grid, cellSnapshot.Cells, cachedSpecies, current);
            cachedRevision = current;
            return cachedSnapshot;
        }

        // Copies, so a published state never changes under its reader.
        private static SpeciesDiscoveryState ToState(SpeciesRecord r) =>
            new SpeciesDiscoveryState(r.SpeciesId, r.Sighted, r.Recorded, r.Caught,
                r.BandIds.ToArray(), r.CellIds.ToArray(), r.ObservationIds.ToArray());

        // Unclassified / out-of-region ("") is "not known", not a habitat entry.
        private static void AddDistinct(List<string> into, string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            for (var i = 0; i < into.Count; i++)
                if (string.Equals(into[i], id, StringComparison.Ordinal)) return;
            into.Add(id);
        }

        private static bool IsBlank(string value) => string.IsNullOrWhiteSpace(value);
    }
}
