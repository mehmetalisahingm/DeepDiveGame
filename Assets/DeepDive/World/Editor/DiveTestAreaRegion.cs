using DeepDive.Core.Contracts;

namespace DeepDive.World.Editor
{
    // The one region of DiveTestArea and its extents (P4.3-B2 #108): the P3 arena x/z -15..15 plus the reef shelf
    // and deep basin north of it, all under the same region id (CONTRACTS P4.3: "ayni regionId altinda ...
    // Ikinci bolge acilma sistemi kurulmaz"). Both scene scripts that write DiveRegion read these numbers, so
    // re-running the P3.3 script after P4.3 cannot shrink the map back to the old arena.
    //
    // X is unchanged, so every existing cell keeps its id and its map u; only v and the row count grow.
    public static class DiveTestAreaRegion
    {
        public const string RegionId = ExplorationIds.NearRegionId;
        public const float MinX = -15f;
        public const float MaxX = 15f;
        public const float MinZ = -15f;
        public const float MaxZ = 65f;
    }
}
