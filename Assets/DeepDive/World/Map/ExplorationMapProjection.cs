using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // The one shared place a cell's normalized map position comes from (P4.1-B #89). Mert's map
    // consumes this instead of computing its own, and the position is never stored in Core or the
    // save: it is DERIVED from the canonical grid state (ExplorationGrid + a cell's GridX/GridZ),
    //
    //     u = (GridX - MinGridX + 0.5) / Columns
    //     v = (GridZ - MinGridZ + 0.5) / Rows
    //
    // so it is not a second world->map algorithm - no world coordinate goes in. The grid itself is
    // DiveRegionBounds cut on the fixed 5 m world lines (ExplorationCellLayout). For a region on those
    // lines (the -15..15 arena) this is exactly the cell centre on DiveRegionBounds' 0..1 square.
    //
    // Pure and static: no allocation, no state.
    public static class ExplorationMapProjection
    {
        // The cell's centre, 0..1 across the grid. False for an invalid grid or a cell outside it,
        // with u/v left at 0 - refused, never clamped onto the edge.
        public static bool TryCellCenter(in ExplorationGrid grid, int gridX, int gridZ, out float u, out float v)
        {
            u = 0f;
            v = 0f;
            if (!ExplorationIds.IsCellInGrid(gridX, gridZ, grid)) return false;

            u = (gridX - grid.MinGridX + 0.5f) / grid.Columns;
            v = (gridZ - grid.MinGridZ + 0.5f) / grid.Rows;
            return true;
        }

        public static bool TryCellCenter(in ExplorationGrid grid, in ExplorationCellState cell, out float u, out float v) =>
            TryCellCenter(grid, cell.GridX, cell.GridZ, out u, out v);
    }
}
