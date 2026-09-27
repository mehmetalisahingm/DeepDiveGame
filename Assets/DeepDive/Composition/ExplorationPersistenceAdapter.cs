using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.World;

namespace DeepDive.Composition
{
    // #90: writes Utku's exploration state (#94) into the campaign file and puts it back. It decides
    // nothing: observations are replayed through SpeciesObservationAuthority.TryApply, the authority's own
    // idempotent path (a replay is AlreadyCounted, an observation from another region is refused).
    //
    // Cells: ExplorationCellAuthority has no "restore a discovered cell" entry yet - it only opens a cell
    // when an approved position reaches it, and faking such a position would put a guessed world point
    // into the authority. Until Utku adds that entry (asked on #89), restored cells are kept here as
    // saved truth and written back on every save, so a reload never loses exploration progress; they are
    // reported by UnrestoredCells rather than drawn as if the authority knew them.
    public sealed class ExplorationPersistenceAdapter : IExplorationPersistence
    {
        private readonly ExplorationCellAuthority cells;
        private readonly SpeciesObservationAuthority species;
        private readonly Dictionary<string, ExplorationCellSave> savedCells =
            new Dictionary<string, ExplorationCellSave>(StringComparer.Ordinal);

        public ExplorationPersistenceAdapter(ExplorationCellAuthority cells, SpeciesObservationAuthority species)
        {
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
            this.species = species ?? throw new ArgumentNullException(nameof(species));
        }

        // Saved discovered cells the live authority does not know as discovered (see class comment).
        public int UnrestoredCells
        {
            get
            {
                var n = 0;
                foreach (var cell in savedCells.Values)
                    if (!cells.IsDiscovered(cell.CellId)) n++;
                return n;
            }
        }

        public ExplorationSaveData ExportExploration()
        {
            var data = new ExplorationSaveData { RegionId = cells.Grid.RegionId };
            var written = new HashSet<string>(StringComparer.Ordinal);

            var snapshot = cells.Snapshot();
            for (var i = 0; i < snapshot.Cells.Count; i++)
            {
                var cell = snapshot.Cells[i];
                if (!cell.Discovered || !written.Add(cell.CellId)) continue;
                data.DiscoveredCells.Add(new ExplorationCellSave
                {
                    CellId = cell.CellId, GridX = cell.GridX, GridZ = cell.GridZ, DepthBandId = cell.DepthBandId
                });
            }

            // Saved truth the authority has not re-learned yet is written back unchanged.
            foreach (var cell in savedCells.Values)
                if (written.Add(cell.CellId)) data.DiscoveredCells.Add(Copy(cell));

            var observations = species.Observations;
            for (var i = 0; i < observations.Count; i++) data.Observations.Add(SpeciesObservationSave.From(observations[i]));
            return data;
        }

        // A file from another region is refused whole (it is not this campaign's map). Observations the
        // authority refuses (invalid, out of region) are dropped, never forced in.
        public bool RestoreExploration(ExplorationSaveData data)
        {
            if (data == null) return false;
            if (!string.IsNullOrEmpty(data.RegionId) && !string.Equals(data.RegionId, cells.Grid.RegionId, StringComparison.Ordinal))
                return false;

            savedCells.Clear();
            if (data.DiscoveredCells != null)
                foreach (var cell in data.DiscoveredCells)
                {
                    if (cell == null || string.IsNullOrWhiteSpace(cell.CellId)) continue;
                    if (!ExplorationIds.IsCellInGrid(cell.GridX, cell.GridZ, cells.Grid)) continue;
                    if (!string.Equals(cell.CellId, ExplorationIds.CellId(cells.Grid.RegionId, cell.GridX, cell.GridZ), StringComparison.Ordinal)) continue;
                    if (!DepthBandIds.IsValidOrUnclassified(cell.DepthBandId ?? string.Empty)) continue;
                    savedCells[cell.CellId] = Copy(cell);
                }

            if (data.Observations != null)
                foreach (var saved in data.Observations)
                    if (saved != null) species.TryApply(saved.ToObservation());
            return true;
        }

        private static ExplorationCellSave Copy(ExplorationCellSave c) => new ExplorationCellSave
        {
            CellId = c.CellId, GridX = c.GridX, GridZ = c.GridZ, DepthBandId = c.DepthBandId ?? string.Empty
        };
    }
}
