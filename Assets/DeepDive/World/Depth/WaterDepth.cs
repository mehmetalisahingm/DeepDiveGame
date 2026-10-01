using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // World position -> depth -> DepthBandIds, for the exploration fog's "kesfedilmis derinlik
    // cizgileri" (P4.1-B #89).
    //
    // NOT a second water authority. The water line is read from the WaterBody list WaterField
    // already builds (WaterField.Bodies) - the same bodies the locomotion tracker classifies
    // against - so there is no surface number of its own to drift. It never produces an
    // EnvironmentLocomotion and nothing in the Land/Surface/Underwater chain reads it.
    //
    // Pure and static: no scene, no allocation, no memory between calls.
    public static class WaterDepth
    {
        // Depth below the water line at this XZ: positive under the surface, negative above it.
        // False when no body's footprint holds the point (dry land, outside the sea) or the input
        // is not finite - "not over water" is refused, never guessed as zero.
        //
        // Footprint is inclusive on its edges, like DiveRegionBounds. Where bodies overlap the
        // highest surface wins (the largest depth), so the answer does not depend on the order
        // WaterField happened to list its volumes in.
        public static bool TryDepthAt(IReadOnlyList<WaterBody> bodies, Vector3 world, out float depth)
        {
            depth = 0f;
            if (bodies == null || !IsFinite(world)) return false;

            var found = false;
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body.SignedInset(world.x, world.z) < 0f) continue;

                var d = body.SurfaceY - world.y;
                if (found && d <= depth) continue;
                depth = d;
                found = true;
            }

            return found;
        }

        // The band id for a world position, one of DepthBandIds; the metre thresholds are
        // DiveDepthBands' (DepthBand.cs), not repeated here. False above the surface, off the
        // water, or deeper than any authored band - an empty id, never a nearest-band guess.
        public static bool TryClassify(IReadOnlyList<WaterBody> bodies, Vector3 world, out string depthBandId)
        {
            depthBandId = string.Empty;
            if (!TryDepthAt(bodies, world, out var depth)) return false;
            if (!DiveDepthBands.TryFind(depth, out var band)) return false;

            depthBandId = band.Id;
            return true;
        }

        private static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
