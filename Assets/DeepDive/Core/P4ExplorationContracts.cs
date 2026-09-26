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

        // A cell is a column/row of the region's 0..1 map square, so its id is a pure function of the
        // two indices: the same cell always gets the same id, across hosts, reloads and replays.
        public static string CellId(int column, int row) => "cell-" + column + "-" + row;

        public static bool IsCellInGrid(int column, int row, ExplorationGrid grid) =>
            grid.IsValid && column >= 0 && row >= 0 && column < grid.Columns && row < grid.Rows;
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

        public static bool IsKnown(string depthBandId)
        {
            for (var i = 0; i < All.Count; i++)
                if (string.Equals(All[i], depthBandId, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    // How the region's map square is cut into cells. Ints only: the map side already knows the
    // region's 0..1 square, it needs just the counts to draw the fog over it.
    public readonly struct ExplorationGrid
    {
        public readonly string RegionId;
        public readonly int Columns;
        public readonly int Rows;

        public ExplorationGrid(string regionId, int columns, int rows)
        {
            RegionId = regionId ?? string.Empty;
            Columns = columns;
            Rows = rows;
        }

        public bool IsValid => !string.IsNullOrEmpty(RegionId) && Columns > 0 && Rows > 0;
    }

    // One cell of the shared exploration fog. Discovered only ever goes false -> true; the host opens
    // a cell because an approved position really reached it, never because a client asked.
    public readonly struct ExplorationCellState
    {
        public readonly string CellId;
        public readonly int Column;
        public readonly int Row;
        public readonly bool Discovered;

        // The depth band the host saw this cell reach ("kesfedilmis derinlik cizgileri"), one of
        // DepthBandIds. Empty = not known yet. An id, never a metre value or display text, so the
        // P4.4 cuts can grow without changing this shape.
        public readonly string DepthBandId;

        public ExplorationCellState(string cellId, int column, int row, bool discovered, string depthBandId)
        {
            CellId = cellId ?? string.Empty;
            Column = column;
            Row = row;
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
