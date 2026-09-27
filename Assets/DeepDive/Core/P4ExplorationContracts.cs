using System;
using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.1-B (#89): shared exploration and species observation. docs/plan/CONTRACTS.md "WorldMapState"
    // (kesif hucresi) and "SpeciesDiscoveryState", and "Harita ve kalici kesif": Utku supplies the
    // world->map transform and the discovery event, Mert the UI and the save. Pure data like every
    // other Core contract - no Unity type, no world position, no UI state, no save authority - so
    // Mert's map/encyclopedia can read it without depending on DeepDive.World.
    //
    // What is deliberately NOT here:
    //   - Any live creature position. An observation names the cell it happened in, never where the
    //     fish is now ("her baligin surekli canli konumu yoktur; bilinen habitat veya son gozlem").
    //   - Labels, silhouettes, colours or "which encyclopedia line is open". Those follow from the
    //     evidence below and are the UI's decision.
    //   - A save shape. Mert's save composes this later; nothing here writes a file.
    //   - The daylight state. That is World's own output for the atmosphere, not a shared contract.
    public static class ExplorationIds
    {
        // Same string DiveRegionField authors in DiveTestArea ("region-near-1"); one region in P4.1,
        // the kiyi/resif/derin cuts of P4.3 stay under this id (no second region system).
        public const string NearRegionId = "region-near-1";

        // Cells are fixed CellSizeMetres x CellSizeMetres squares of ONE world grid anchored at world
        // X = 0, Z = 0: cell (gx, gz) covers x in [gx*size, (gx+1)*size) and z in [gz*size, (gz+1)*size).
        // The id is regionId + those two grid coordinates and nothing else - never a running index,
        // a column count or a region corner - so the same world position keeps the same id when the
        // region grows; a larger footprint only adds cells around the old ones. Coordinates can be
        // negative (the arena runs -15..15). Changing the size or the origin renames every cell, so it
        // changes only together with a save migration.
        public const float CellSizeMetres = 5f;

        public static string CellId(string regionId, int gridX, int gridZ) =>
            regionId + ":gx" + gridX + ":gz" + gridZ;

        public static bool IsCellInGrid(int gridX, int gridZ, ExplorationGrid grid) =>
            grid.IsValid &&
            gridX >= grid.MinGridX && gridX < grid.MinGridX + grid.Columns &&
            gridZ >= grid.MinGridZ && gridZ < grid.MinGridZ + grid.Rows;
    }

    // The canonical depth cut ids (WORLD_SYSTEMS P4.3/P4.4: kiyi/resif/derin), the BoatTripIds pattern:
    // stable kebab-case ids a save, a cell and an observation can reference without a migration.
    // Ids, never localized text - the UI owns the words. Shallow is the same string
    // DiveDepthBands.ShallowId already carries; reef and deep are reserved here so the contract does not
    // change when P4.4 authors their metre ranges in World.
    public static class DepthBandIds
    {
        public const string Shallow = "shallow";
        public const string Reef = "reef";
        public const string Deep = "deep";

        public static readonly IReadOnlyList<string> All = new[] { Shallow, Reef, Deep };

        // "No band": the cell or observation has no classified depth. Not a band and not in All.
        public const string Unclassified = "";

        // What a cell/observation DepthBandId may hold: a known id or Unclassified. Null and
        // whitespace are invalid - the host never produces them and a save must not load them.
        public static bool IsValidOrUnclassified(string depthBandId) =>
            depthBandId != null && (depthBandId.Length == 0 || IsKnown(depthBandId));

        public static bool IsKnown(string depthBandId)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i], depthBandId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // Which grid cells the region's footprint touches: gx MinGridX .. MinGridX+Columns-1, likewise z.
    // Columns and Rows are derived from the footprint at runtime (the 30 x 30 m arena gives 6 x 6) and
    // are part of no id; a bigger region only widens the range.
    public readonly struct ExplorationGrid
    {
        public readonly string RegionId;
        public readonly int MinGridX;
        public readonly int MinGridZ;
        public readonly int Columns;
        public readonly int Rows;

        public ExplorationGrid(string regionId, int minGridX, int minGridZ, int columns, int rows)
        {
            RegionId = regionId ?? string.Empty;
            MinGridX = minGridX;
            MinGridZ = minGridZ;
            Columns = columns;
            Rows = rows;
        }

        public bool IsValid => !string.IsNullOrEmpty(RegionId) && Columns > 0 && Rows > 0;
    }

    // One cell of the shared exploration fog: the canonical grid state only (coordinates, discovered,
    // band). Where it is drawn on the map is derived by World's projection, not stored here, so the
    // save never holds a second copy of the world->map transform. Discovered only ever goes
    // false -> true; the host opens a cell because an approved position really reached it, never
    // because a client asked.
    public readonly struct ExplorationCellState
    {
        public readonly string CellId;
        public readonly int GridX;
        public readonly int GridZ;
        public readonly bool Discovered;

        // The depth band the host saw this cell reach ("kesfedilmis derinlik cizgileri"). FROZEN:
        // a known DepthBandIds id, or DepthBandIds.Unclassified ("") - not seen wet yet, or deeper than
        // any authored band (reef/deep are reserved ids with no metres until P4.4). Never null (the
        // constructor folds null to ""), never whitespace, never a metre value or display text. A save
        // stores "" as it is, and a consumer never guesses reef/deep from it.
        public readonly string DepthBandId;

        public ExplorationCellState(string cellId, int gridX, int gridZ, bool discovered, string depthBandId)
        {
            CellId = cellId ?? string.Empty;
            GridX = gridX;
            GridZ = gridZ;
            Discovered = discovered;
            DepthBandId = depthBandId ?? string.Empty;
        }
    }

    // The kinds of evidence the encyclopedia unlocks on (WORLD_SYSTEMS "Ansiklopedi"): first verified
    // encounter, a valid camera recording, a caught specimen. Research proof is P4.4 and is added then;
    // values are explicit so a later addition never renumbers these.
    public enum SpeciesEvidence : byte
    {
        Sighted = 0,
        Recorded = 1,
        Caught = 2
    }

    // One host-verified observation event. SpeciesId is the same stable id CaptureResult and
    // DaySummary.DiscoveredSpeciesIds already carry (SpeciesDefinition.SpeciesId, e.g. "sea_bass").
    //
    // ObservationId is minted ONCE, by the host, at the moment it first accepts the event, and is fixed
    // from then on; every later processing of that same accepted event (a retry, a replay, a reload,
    // the day close re-reading it) carries that stored id and is counted once. It is never re-derived
    // per tick from a timestamp, a frame or a world position - two ticks of one sighting must not
    // produce two ids. Caught and Recorded reuse the source event's own id (captureId, recordingId);
    // Sighted gets a host-issued id when the sighting is accepted.
    public readonly struct SpeciesObservation
    {
        public readonly string ObservationId;
        public readonly string SpeciesId;
        public readonly SpeciesEvidence Evidence;
        public readonly string RegionId;

        // NOT a live coordinate. The coarse, stable exploration cell (ExplorationIds.CellId) the
        // observer was in when the host accepted this observation, recorded only for an observation
        // that actually happened - habitat / "son gozlem". No Vector3, no exact position and no fish
        // position ever leaves through this; where the creature is now is never published.
        public readonly string CellId;

        // One of DepthBandIds, same rule: the band at acceptance, an id and never display text.
        public readonly string DepthBandId;

        public readonly int DayNumber;

        public SpeciesObservation(string observationId, string speciesId, SpeciesEvidence evidence,
            string regionId, string cellId, string depthBandId, int dayNumber)
        {
            ObservationId = observationId ?? string.Empty;
            SpeciesId = speciesId ?? string.Empty;
            Evidence = evidence;
            RegionId = regionId ?? string.Empty;
            CellId = cellId ?? string.Empty;
            DepthBandId = depthBandId ?? string.Empty;
            DayNumber = dayNumber;
        }

        public bool IsValid => !string.IsNullOrEmpty(ObservationId) && !string.IsNullOrEmpty(SpeciesId);
    }

    // docs/plan/CONTRACTS.md "SpeciesDiscoveryState": per species, which evidence exists and which
    // observation first proved it. Seeing another fish of the same species adds no new evidence
    // ("ayni turun baska baligini gormek ikinci kesif odulu yaratmaz"). Reward ids are Mert's
    // progress layer and join in P4.4 with the content; they are not decided here.
    public readonly struct SpeciesDiscoveryState
    {
        public readonly string SpeciesId;
        public readonly bool Sighted;
        public readonly bool Recorded;
        public readonly bool Caught;

        // Habitat the encyclopedia may show: bands and cells this species has been observed in.
        public readonly IReadOnlyList<string> ObservedDepthBandIds;
        public readonly IReadOnlyList<string> ObservedCellIds;

        // The observation ids already counted for this species, so a replay is recognisably a replay.
        public readonly IReadOnlyList<string> ObservationIds;

        public SpeciesDiscoveryState(string speciesId, bool sighted, bool recorded, bool caught,
            IReadOnlyList<string> observedDepthBandIds, IReadOnlyList<string> observedCellIds,
            IReadOnlyList<string> observationIds)
        {
            SpeciesId = speciesId ?? string.Empty;
            Sighted = sighted;
            Recorded = recorded;
            Caught = caught;
            ObservedDepthBandIds = observedDepthBandIds ?? Array.Empty<string>();
            ObservedCellIds = observedCellIds ?? Array.Empty<string>();
            ObservationIds = observationIds ?? Array.Empty<string>();
        }

        public bool Has(SpeciesEvidence evidence) => evidence switch
        {
            SpeciesEvidence.Sighted => Sighted,
            SpeciesEvidence.Recorded => Recorded,
            SpeciesEvidence.Caught => Caught,
            _ => false
        };
    }

    // The whole shared exploration picture at one revision - what Mert's map and encyclopedia render.
    // Cells lists every cell of the grid (discovered or not) so the UI never has to invent the gaps.
    public readonly struct ExplorationSnapshot
    {
        public readonly ExplorationGrid Grid;
        public readonly IReadOnlyList<ExplorationCellState> Cells;
        public readonly IReadOnlyList<SpeciesDiscoveryState> Species;
        public readonly int Revision;

        public ExplorationSnapshot(ExplorationGrid grid, IReadOnlyList<ExplorationCellState> cells,
            IReadOnlyList<SpeciesDiscoveryState> species, int revision)
        {
            Grid = grid;
            Cells = cells ?? Array.Empty<ExplorationCellState>();
            Species = species ?? Array.Empty<SpeciesDiscoveryState>();
            Revision = revision;
        }
    }

    // Read-only seam Mert's map/encyclopedia consume. Implemented by Utku's exploration authority;
    // there is no write path here - cells open and observations count only inside the host authority.
    // Revision rises on every change, so a reader can skip rebuilding when it has not moved.
    public interface IExplorationReadModel
    {
        int Revision { get; }
        ExplorationSnapshot Snapshot();
    }
}
