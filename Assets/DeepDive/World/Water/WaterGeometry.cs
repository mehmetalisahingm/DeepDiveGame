using System.Collections.Generic;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.World
{
    // How far past each line a capsule has to reach before it counts. Zero everywhere is the
    // raw rule; a tracker widens them to give the last answer a band to hold on to.
    //
    // Sign convention matches the measurements below: positive means "further into the
    // water", so a negative threshold lets a state hold slightly past its own line.
    internal readonly struct WaterThresholds
    {
        public static readonly WaterThresholds Exact = new WaterThresholds(0f, 0f, 0f);

        // Inset must be >= Inset, Base depth must be > Base, Top depth must be >= Top. The
        // base is strict and the other two are not, which is what makes a base exactly at the
        // surface dry while a top exactly at it is submerged.
        public readonly float Inset;
        public readonly float Base;
        public readonly float Top;

        public WaterThresholds(float inset, float baseLine, float top)
        {
            Inset = inset;
            Base = baseLine;
            Top = top;
        }
    }

    // The capsule reduced to the three numbers the rules actually read: its lowest point, its
    // highest point, and where its lower end stands in XZ. Measured once per classification
    // and reused for every body, so the geometry lives in exactly one place.
    internal readonly struct WaterCapsule
    {
        // Below this an up-vector has no usable direction; it matches the magnitude at which
        // Vector3.normalized gives up and returns zero.
        private const float MinUpSqrMagnitude = 1e-10f;

        public readonly bool IsUsable;
        public readonly float BaseY;
        public readonly float TopY;
        public readonly float FootX;
        public readonly float FootZ;

        private WaterCapsule(bool isUsable, float baseY, float topY, float footX, float footZ)
        {
            IsUsable = isUsable;
            BaseY = baseY;
            TopY = topY;
            FootX = footX;
            FootZ = footZ;
        }

        public static WaterCapsule Measure(in WaterProbe probe)
        {
            if (!IsUsable_(probe)) return new WaterCapsule(false, 0f, 0f, 0f, 0f);

            var center = probe.Position + probe.CenterOffsetWorld;
            var up = probe.Up.normalized;

            // Distance from the capsule's centre to either sphere centre. A capsule shorter
            // than its own diameter has no straight section left, so it is a sphere.
            var half = Mathf.Max(probe.Height, 2f * probe.Radius) * 0.5f - probe.Radius;
            var a = center - up * half;
            var b = center + up * half;

            // The foot is the lower sphere centre. A capsule lying flat has no lower end, so
            // the tie resolves to the centre rather than to whichever end the maths named
            // first - otherwise a level diver's footprint would shift with their heading.
            Vector3 foot;
            if (Mathf.Approximately(a.y, b.y)) foot = center;
            else foot = a.y < b.y ? a : b;

            return new WaterCapsule(
                true,
                Mathf.Min(a.y, b.y) - probe.Radius,
                Mathf.Max(a.y, b.y) + probe.Radius,
                foot.x,
                foot.z);
        }

        // A probe we cannot reason about reads as unusable, and the rules answer Land: the
        // diver keeps walking rather than being dropped into a swim state by a bad number.
        private static bool IsUsable_(in WaterProbe probe)
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

    // The one classification loop. WaterRules calls it with exact thresholds, WaterTracker
    // with widened ones; neither owns a second copy of the geometry.
    internal static class WaterGeometry
    {
        public static EnvironmentLocomotion Classify(
            IReadOnlyList<WaterBody> bodies, in WaterProbe probe, in WaterThresholds thresholds)
        {
            if (bodies == null || bodies.Count == 0) return EnvironmentLocomotion.Land;

            var capsule = WaterCapsule.Measure(probe);
            if (!capsule.IsUsable) return EnvironmentLocomotion.Land;

            var wettest = EnvironmentLocomotion.Land;
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];

                if (body.SignedInset(capsule.FootX, capsule.FootZ) < thresholds.Inset) continue;
                if (body.SurfaceY - capsule.BaseY <= thresholds.Base) continue;

                var result = body.SurfaceY - capsule.TopY >= thresholds.Top
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
    }
}
