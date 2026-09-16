using System.Collections.Generic;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.World
{
    // The raw water classification: given a diver's capsule and the water around it, say
    // which environment they are in. Pure and stateless - it reads its arguments, returns an
    // answer, and remembers nothing.
    //
    // Raw means no hysteresis. A capsule sitting exactly on a threshold will flip between two
    // answers as it jitters; the deadband that stops that is Step 2's job and wraps this, so
    // that the geometry stays separately testable from the smoothing.
    //
    // Two points decide everything: the capsule's lowest and highest points, and the XZ of
    // its lower end. Feet decide whether you are over water at all - a diver leaning out over
    // the sea from a rock is still on land, and one leaning inland from the shallows is still
    // in the water.
    public static class WaterRules
    {
        // Below this an up-vector has no usable direction; it matches the magnitude at which
        // Vector3.normalized gives up and returns zero.
        private const float MinUpSqrMagnitude = 1e-10f;

        public static EnvironmentLocomotion ClassifyRaw(IReadOnlyList<WaterBody> bodies, in WaterProbe probe)
        {
            if (bodies == null || bodies.Count == 0) return EnvironmentLocomotion.Land;
            if (!IsUsable(probe)) return EnvironmentLocomotion.Land;

            var center = probe.Position + probe.CenterOffsetWorld;
            var up = probe.Up.normalized;

            // Distance from the capsule's centre to either sphere centre. A capsule shorter
            // than its own diameter has no straight section left, so it is a sphere.
            var half = Mathf.Max(probe.Height, 2f * probe.Radius) * 0.5f - probe.Radius;
            var a = center - up * half;
            var b = center + up * half;

            var baseY = Mathf.Min(a.y, b.y) - probe.Radius;
            var topY = Mathf.Max(a.y, b.y) + probe.Radius;

            // The foot is the lower sphere centre. A capsule lying flat has no lower end, so
            // the tie resolves to the centre rather than to whichever end the maths named
            // first - otherwise a level diver's footprint would shift with their heading.
            Vector3 foot;
            if (Mathf.Approximately(a.y, b.y)) foot = center;
            else foot = a.y < b.y ? a : b;

            var wettest = EnvironmentLocomotion.Land;
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];

                // Edge inclusive: standing exactly on the shore line counts as over water.
                if (body.SignedInset(foot.x, foot.z) < 0f) continue;

                // Base exactly at the surface is dry - you are standing on the water line,
                // not in it.
                if (body.SurfaceY - baseY <= 0f) continue;

                // Top exactly at the surface is submerged, so the head being level with the
                // water still counts as under it.
                var result = body.SurfaceY - topY >= 0f
                    ? EnvironmentLocomotion.Underwater
                    : EnvironmentLocomotion.Surface;

                if (Wetness(result) <= Wetness(wettest)) continue;
                wettest = result;

                // Nothing is wetter than underwater, so the remaining bodies cannot change
                // the answer. This is also why the result does not depend on list order.
                if (wettest == EnvironmentLocomotion.Underwater) return wettest;
            }

            return wettest;
        }

        // Spelled out rather than leaning on the enum's numeric order, which belongs to
        // DeepDive.Network and is not ours to depend on.
        private static int Wetness(EnvironmentLocomotion environment) => environment switch
        {
            EnvironmentLocomotion.Underwater => 2,
            EnvironmentLocomotion.Surface => 1,
            _ => 0
        };

        // A probe we cannot reason about classifies as Land: the diver keeps walking rather
        // than being dropped into a swim state by a bad number.
        private static bool IsUsable(in WaterProbe probe)
        {
            if (!IsFinite(probe.Position) || !IsFinite(probe.CenterOffsetWorld) || !IsFinite(probe.Up))
                return false;
            if (!IsFinite(probe.Radius) || !IsFinite(probe.Height)) return false;
            if (probe.Radius < 0f || probe.Height < 0f) return false;
            return probe.Up.sqrMagnitude >= MinUpSqrMagnitude;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        private static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }
}
