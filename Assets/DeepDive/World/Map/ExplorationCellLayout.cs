using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // How a region's footprint lies on the fixed exploration grid (P4.1-B #89). The grid is
    // ExplorationIds': CellSizeMetres squares anchored at world X = 0, Z = 0, so a cell's (gx, gz) -
    // and therefore its id - depends only on the world position, never on the region's size or
    // corner. The footprint only decides WHICH cells exist: the range below is derived from the
    // bounds (the 30 x 30 m arena at -15..15 gives gx/gz -3..2, i.e. 6 x 6), it is not a constant.
    //
    // Pure readonly struct, like DiveRegionBounds: plain numbers in, ints out, no allocation.
    public readonly struct ExplorationCellLayout
    {
        public readonly DiveRegionBounds Bounds;
        public readonly ExplorationGrid Grid;

        public ExplorationCellLayout(string regionId, DiveRegionBounds bounds)
        {
            Bounds = bounds;
            if (!bounds.IsValid || string.IsNullOrEmpty(regionId))
            {
                Grid = default;
                return;
            }

            // First cell = the one holding the min edge; last cell = the one whose interior reaches
            // the max edge (ceil - 1), so a footprint ending exactly on a grid line gets no empty
            // zero-width cell past it.
            var minX = FloorCell(bounds.MinX);
            var minZ = FloorCell(bounds.MinZ);
            var maxX = Mathf.CeilToInt(bounds.MaxX / ExplorationIds.CellSizeMetres) - 1;
            var maxZ = Mathf.CeilToInt(bounds.MaxZ / ExplorationIds.CellSizeMetres) - 1;
            Grid = new ExplorationGrid(regionId, minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
        }

        public bool IsValid => Bounds.IsValid && Grid.IsValid;

        public int CellCount => IsValid ? Grid.Columns * Grid.Rows : 0;

        // Storage slot only (row-major from the grid's min corner) - never an id, never saved.
        public int IndexOf(int gridX, int gridZ) => (gridZ - Grid.MinGridZ) * Grid.Columns + (gridX - Grid.MinGridX);

        // Out of region is refused, never clamped - DiveRegionBounds.Contains refuses it and this adds
        // no fallback. The one fold is the region's own max edge: the bounds are inclusive, so a point
        // exactly on a max edge that lies on a grid line is inside and belongs to the last cell rather
        // than to a cell the region does not have. (Only that exact line; the walls keep players off it.)
        public bool TryCell(Vector3 world, out int gridX, out int gridZ)
        {
            gridX = 0;
            gridZ = 0;
            if (!IsValid) return false;
            if (!Bounds.Contains(world.x, world.z)) return false;

            var gx = Mathf.Min(FloorCell(world.x), Grid.MinGridX + Grid.Columns - 1);
            var gz = Mathf.Min(FloorCell(world.z), Grid.MinGridZ + Grid.Rows - 1);
            if (!ExplorationIds.IsCellInGrid(gx, gz, Grid)) return false;

            gridX = gx;
            gridZ = gz;
            return true;
        }

        private static int FloorCell(float world) => Mathf.FloorToInt(world / ExplorationIds.CellSizeMetres);
    }
}
