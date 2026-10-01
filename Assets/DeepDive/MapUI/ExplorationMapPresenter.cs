using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.World;

namespace DeepDive.MapUI
{
    // One fog tile of the shared exploration map, already in the map's 0..1 square.
    public readonly struct FogTile
    {
        public readonly string CellId;
        public readonly float CenterU;
        public readonly float CenterV;
        public readonly float Width;
        public readonly float Height;
        public readonly bool Discovered;
        public readonly string DepthBandId;

        public FogTile(string cellId, float centerU, float centerV, float width, float height, bool discovered, string depthBandId)
        {
            CellId = cellId;
            CenterU = centerU;
            CenterV = centerV;
            Width = width;
            Height = height;
            Discovered = discovered;
            DepthBandId = depthBandId ?? string.Empty;
        }
    }

    // #90 map fog. Pure: turns Utku's snapshot into tiles. Where a cell sits is ExplorationMapProjection's
    // answer (derived from the grid), never computed here, so the UI holds no second world->map transform.
    // Only cells and their bands go in and come out - no player, fish or boss position exists in the input.
    public static class ExplorationMapPresenter
    {
        public static IReadOnlyList<FogTile> BuildFog(in ExplorationSnapshot snapshot)
        {
            var grid = snapshot.Grid;
            var tiles = new List<FogTile>(snapshot.Cells.Count);
            if (!grid.IsValid) return tiles;

            var width = 1f / grid.Columns;
            var height = 1f / grid.Rows;
            for (var i = 0; i < snapshot.Cells.Count; i++)
            {
                var cell = snapshot.Cells[i];
                // A cell the projection refuses (outside the grid) is not drawn, never clamped to an edge.
                if (!ExplorationMapProjection.TryCellCenter(grid, cell, out var u, out var v)) continue;
                tiles.Add(new FogTile(cell.CellId, u, v, width, height, cell.Discovered, cell.DepthBandId));
            }
            return tiles;
        }

        public static int DiscoveredCount(IReadOnlyList<FogTile> tiles)
        {
            var n = 0;
            for (var i = 0; i < tiles.Count; i++)
                if (tiles[i].Discovered) n++;
            return n;
        }
    }
}
