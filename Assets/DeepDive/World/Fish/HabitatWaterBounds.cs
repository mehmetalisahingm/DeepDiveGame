using UnityEngine;

namespace DeepDive.World
{
    // Where one fish may swim (#219): the water, off the walls, above the sea bed, clear of the solid kit pieces and
    // on a leash round its home, so a chased fish neither runs through the floor nor out of its habitat. FishMotion
    // already turns for home when its next point is refused, so the swim rules stay as they are.
    //
    // No physics query on purpose: heights and boxes come from SeabedProfile, and the host asks this every step for
    // every fish. Like SwimVolumeBounds it owns no volume and decides nothing but "is this point allowed".
    public sealed class HabitatWaterBounds : IWaterBounds
    {
        public const float LeashFlat = 10f;
        public const float LeashVertical = 4.5f;
        public const float FloorMargin = 0.6f;
        public const float SolidMargin = 0.3f;

        // The inner faces of Wall_E/W/S/N are at x +-14.75, z -14.75 and z 64.75; the water box runs past them.
        public const float MaxAbsX = 14.25f;
        public const float MinZ = -14.25f;
        public const float MaxZ = 64f;

        private readonly IWaterBounds water;
        private readonly Vector3 home;

        public HabitatWaterBounds(IWaterBounds water, Vector3 home)
        {
            this.water = water;
            this.home = home;
        }

        public bool Contains(Vector3 point)
        {
            if (water != null && !water.Contains(point)) return false;
            if (Mathf.Abs(point.x) > MaxAbsX || point.z < MinZ || point.z > MaxZ) return false;
            if (Mathf.Abs(point.y - home.y) > LeashVertical) return false;
            var flat = new Vector2(point.x - home.x, point.z - home.z);
            if (flat.sqrMagnitude > LeashFlat * LeashFlat) return false;
            if (point.y < SeabedProfile.FloorHeight(point.x, point.z) + FloorMargin) return false;

            var solids = SeabedProfile.Solids;
            for (var i = 0; i < solids.Count; i++)
            {
                var box = solids[i].Box;
                box.Expand(SolidMargin * 2f);
                if (box.Contains(point)) return false;
            }

            return true;
        }
    }
}
