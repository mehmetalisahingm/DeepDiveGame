using UnityEngine;

namespace DeepDive.World
{
    // Water as the classifier needs it, and nothing more: an XZ footprint and the height of
    // its surface. A value struct so a classification can be tested against plain numbers
    // instead of a scene, and so World never has to reach into a collider.
    //
    // There is deliberately no floor. Inside the footprint, anything below SurfaceY is wet,
    // however deep - a diver on the sea bed is underwater, not out of the volume. The box's
    // bottom face is not modelled because nothing in the rules needs it.
    //
    // Axis-aligned by assumption. A rotated box is not detected here; rejecting one is the
    // Step 3 shell's job, since a pure struct has no scene to inspect.
    public readonly struct WaterBody
    {
        // Footprint edges in world XZ. Kept as four floats rather than a Vector2 pair so no
        // reader has to remember that a Vector2's y would be holding a z.
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public readonly float SurfaceY;

        public WaterBody(float minX, float maxX, float minZ, float maxZ, float surfaceY)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            SurfaceY = surfaceY;
        }

        // A box whose size is zero or negative on any axis is not water, so it is refused
        // rather than turned into a degenerate body a caller would have to remember to check.
        public static bool TryFromBox(Vector3 worldCenter, Vector3 worldSize, out WaterBody body)
        {
            body = default;
            if (!IsFinite(worldCenter) || !IsFinite(worldSize)) return false;
            if (worldSize.x <= 0f || worldSize.y <= 0f || worldSize.z <= 0f) return false;

            var half = worldSize * 0.5f;
            body = new WaterBody(
                worldCenter.x - half.x, worldCenter.x + half.x,
                worldCenter.z - half.z, worldCenter.z + half.z,
                worldCenter.y + half.y);
            return true;
        }

        // Signed distance from an XZ point to the nearest footprint edge: positive inside,
        // zero exactly on the edge, negative outside. Only the sign is load-bearing, so the
        // outside value is the nearest-edge distance rather than a true corner distance.
        //
        // A malformed body (Min past Max) is negative everywhere, so it simply never reads
        // as wet - that is why the rules need no separate validity check.
        public float SignedInset(float x, float z) =>
            Mathf.Min(
                Mathf.Min(x - MinX, MaxX - x),
                Mathf.Min(z - MinZ, MaxZ - z));

        private static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
