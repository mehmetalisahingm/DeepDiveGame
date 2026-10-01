using System;
using DeepDive.Core.Contracts;
using DeepDive.World;

namespace DeepDive.Composition
{
    // #90: writes Utku's exploration state into the campaign file and restores it through the
    // authorities' own idempotent load paths. The adapter owns no shadow exploration state:
    // cells are restored by ExplorationCellAuthority.TryRestore and observations by
    // SpeciesObservationAuthority.TryApply.
    public sealed class ExplorationPersistenceAdapter : IExplorationPersistence
    {
        private readonly ExplorationCellAuthority cells;
        private readonly SpeciesObservationAuthority species;

        public ExplorationPersistenceAdapter(ExplorationCellAuthority cells, SpeciesObservationAuthority species)
        {
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
            this.species = species ?? throw new ArgumentNullException(nameof(species));
        }

        public ExplorationSaveData ExportExploration()
        {
            var data = new ExplorationSaveData { RegionId = cells.Grid.RegionId };
            var snapshot = cells.Snapshot();
            for (var i = 0; i < snapshot.Cells.Count; i++)
            {
                var cell = snapshot.Cells[i];
                if (!cell.Discovered) continue;
                data.DiscoveredCells.Add(new ExplorationCellSave
                {
                    CellId = cell.CellId,
                    GridX = cell.GridX,
                    GridZ = cell.GridZ,
                    DepthBandId = cell.DepthBandId
                });
            }

            var observations = species.Observations;
            for (var i = 0; i < observations.Count; i++)
                data.Observations.Add(SpeciesObservationSave.From(observations[i]));
            return data;
        }

        // A file from another region is refused whole. Invalid/tampered cells and observations are
        // individually ignored by their owning authorities; valid data is idempotent on repeated loads.
        public bool RestoreExploration(ExplorationSaveData data)
        {
            if (data == null) return false;
            if (!string.IsNullOrEmpty(data.RegionId) &&
                !string.Equals(data.RegionId, cells.Grid.RegionId, StringComparison.Ordinal))
                return false;

            if (data.DiscoveredCells != null)
                foreach (var cell in data.DiscoveredCells)
                {
                    if (cell == null) continue;
                    cells.TryRestore(cell.CellId, cell.GridX, cell.GridZ, cell.DepthBandId ?? string.Empty);
                }

            if (data.Observations != null)
                foreach (var saved in data.Observations)
                    if (saved != null) species.TryApply(saved.ToObservation());
            return true;
        }
    }
}
