using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // Host state of the shared exploration fog (P4.1-B #89, CONTRACTS "Harita ve kalici kesif").
    // A cell opens because an approved player position (IExplorerPositionSource) reached it; it
    // opens once. A revisit is not a second discovery: the flag is already set, nothing is
    // re-set, no second result is produced and the revision does not move.
    //
    // Pure class, no MonoBehaviour and no scene lookup: the host shell (Composition, later) owns
    // one and hands it the real feed and WaterField.Bodies. Mert reads it through
    // SpeciesObservationAuthority, the one IExplorationReadModel, which adds the species; this
    // class's own Snapshot() carries the cells with an empty Species list. No world position is
    // stored or published - only cell ids and band ids - and no creature position ever enters,
    // since the only input is the player feed.
    public sealed class ExplorationCellAuthority
    {
        private readonly ExplorationCellLayout layout;
        private readonly IReadOnlyList<WaterBody> water;

        // Minted once here so a tick never builds a string.
        private readonly string[] cellIds;
        private readonly bool[] discovered;
        private readonly string[] depthBandIds;
        private readonly List<ExplorerPosition> positions = new List<ExplorerPosition>(8);

        private int revision;
        private ExplorationSnapshot cachedSnapshot;
        private int cachedRevision = -1;

        // water is WaterField.Bodies (live list, read by reference); null means depth is unknown
        // and cells open with an empty band. Invalid bounds or grid is an authoring mistake and
        // throws here, once, rather than silently opening nothing every tick. The cell range is
        // derived from the bounds (ExplorationCellLayout); there is no column count to pass.
        public ExplorationCellAuthority(string regionId, DiveRegionBounds bounds, IReadOnlyList<WaterBody> water)
        {
            layout = new ExplorationCellLayout(regionId, bounds);
            if (!layout.IsValid)
                throw new ArgumentException(
                    $"P4_EXPLORATION_LAYOUT_INVALID region={regionId} " +
                    $"bounds=x[{bounds.MinX},{bounds.MaxX}] z[{bounds.MinZ},{bounds.MaxZ}]");

            this.water = water;
            var grid = layout.Grid;
            var count = layout.CellCount;
            cellIds = new string[count];
            discovered = new bool[count];
            depthBandIds = new string[count];
            for (var gz = grid.MinGridZ; gz < grid.MinGridZ + grid.Rows; gz++)
            for (var gx = grid.MinGridX; gx < grid.MinGridX + grid.Columns; gx++)
            {
                var i = layout.IndexOf(gx, gz);
                cellIds[i] = ExplorationIds.CellId(grid.RegionId, gx, gz);
                depthBandIds[i] = DepthBandIds.Unclassified;
            }
        }

        public ExplorationGrid Grid => layout.Grid;

        public int Revision => revision;

        public int DiscoveredCount
        {
            get
            {
                var n = 0;
                for (var i = 0; i < discovered.Length; i++)
                    if (discovered[i]) n++;
                return n;
            }
        }

        public bool IsDiscovered(int gridX, int gridZ) =>
            ExplorationIds.IsCellInGrid(gridX, gridZ, layout.Grid) && discovered[layout.IndexOf(gridX, gridZ)];

        public bool IsDiscovered(string cellId)
        {
            for (var i = 0; i < cellIds.Length; i++)
                if (string.Equals(cellIds[i], cellId, StringComparison.Ordinal)) return discovered[i];
            return false;
        }

        // The id a world position's cell has, whether discovered or not; false out of region.
        public bool TryGetCellId(Vector3 world, out string cellId)
        {
            cellId = string.Empty;
            if (!layout.TryCell(world, out var gx, out var gz)) return false;
            cellId = cellIds[layout.IndexOf(gx, gz)];
            return true;
        }

        // The cell a world position is in and the band that cell holds right now (may be
        // DepthBandIds.Unclassified), discovered or not; false out of region. Pre-built strings, no
        // allocation. An observation stamps these, never the position itself.
        public bool TryGetCell(Vector3 world, out string cellId, out string depthBandId)
        {
            cellId = string.Empty;
            depthBandId = DepthBandIds.Unclassified;
            if (!layout.TryCell(world, out var gx, out var gz)) return false;
            var index = layout.IndexOf(gx, gz);
            cellId = cellIds[index];
            depthBandId = depthBandIds[index];
            return true;
        }

        // Polls the approved positions once and returns how many cells opened this tick. When
        // newlyDiscoveredCellIds is given, each newly opened cell's id is appended exactly once -
        // the discovery event. Ids are the pre-built strings, so a tick does not allocate.
        public int Tick(IExplorerPositionSource source, List<string> newlyDiscoveredCellIds = null)
        {
            if (source == null) return 0;

            source.CollectPositions(positions);
            var opened = 0;
            for (var i = 0; i < positions.Count; i++)
            {
                if (!TryObserve(positions[i].WorldPosition, out var index)) continue;
                opened++;
                newlyDiscoveredCellIds?.Add(cellIds[index]);
            }

            return opened;
        }

        // One approved position. True only the first time its cell opens; out of region, a
        // non-finite position or an already discovered cell is false.
        //
        // The band is recorded once, the first time the host sees one for the cell (a cell first
        // entered from the dry beach learns its band on the first wet visit). That fill moves the
        // revision but is not a discovery. It is never overwritten: which band a cell "is" once
        // P4.4 authors the deeper cuts is decided then, not guessed here.
        public bool TryObserve(Vector3 world, out int cellIndex)
        {
            cellIndex = -1;
            if (!layout.TryCell(world, out var gx, out var gz)) return false;

            var index = layout.IndexOf(gx, gz);
            var changed = false;
            var isNew = !discovered[index];
            if (isNew)
            {
                discovered[index] = true;
                changed = true;
            }

            if (depthBandIds[index].Length == 0 && WaterDepth.TryClassify(water, world, out var bandId))
            {
                depthBandIds[index] = bandId;
                changed = true;
            }

            if (changed) revision++;
            if (!isNew) return false;

            cellIndex = index;
            return true;
        }

        // Every cell of the region, discovered or not, ordered by gz then gx. Rebuilt only when the revision has
        // moved, so a reader polling each frame shares one array between changes.
        public ExplorationSnapshot Snapshot()
        {
            if (cachedRevision == revision) return cachedSnapshot;

            var grid = layout.Grid;
            var cells = new ExplorationCellState[cellIds.Length];
            for (var gz = grid.MinGridZ; gz < grid.MinGridZ + grid.Rows; gz++)
            for (var gx = grid.MinGridX; gx < grid.MinGridX + grid.Columns; gx++)
            {
                var i = layout.IndexOf(gx, gz);
                cells[i] = new ExplorationCellState(cellIds[i], gx, gz, discovered[i], depthBandIds[i]);
            }

            cachedSnapshot = new ExplorationSnapshot(layout.Grid, cells, Array.Empty<SpeciesDiscoveryState>(), revision);
            cachedRevision = revision;
            return cachedSnapshot;
        }
    }
}
