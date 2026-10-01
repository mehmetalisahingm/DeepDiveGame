using System.Collections.Generic;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.World
{
    // The raw rule with a memory. One per diver.
    //
    // Raw geometry alone flip-flops: a diver bobbing a millimetre either side of the water
    // line would alternate Land and Surface every tick, and every tick would restate the
    // presentation and the oxygen drain. So the last answer gets a band to hold on to - a
    // state keeps itself until the capsule is a clear deadband past the line that would end
    // it, and has to reach a deadband past the line to claim a wetter one.
    //
    // This wraps WaterRules rather than replacing it: the geometry is shared through
    // WaterGeometry, so the smoothing has no second copy of the maths to drift from.
    //
    // Host-side only. The memory is local state, never serialised and never sent - the
    // clients read the environment Mehmet's player already replicates.
    public sealed class WaterTracker : IWaterField
    {
        public const float DefaultDeadband = 0.10f;

        private readonly IReadOnlyList<WaterBody> bodies;
        private readonly float deadband;

        private EnvironmentLocomotion previous;
        private bool hasPrevious;

        // The list is held by reference, so a shell that adds or removes water later does not
        // need to rebuild every diver's tracker.
        public WaterTracker(IReadOnlyList<WaterBody> bodies, float deadband = DefaultDeadband)
        {
            this.bodies = bodies;
            // A negative band would invert the comparisons and make every line sticky in the
            // wrong direction, so it is clamped rather than trusted.
            this.deadband = Mathf.Max(0f, deadband);
        }

        public float Deadband => deadband;

        public bool HasMemory => hasPrevious;

        public EnvironmentLocomotion Classify(in WaterProbe probe)
        {
            var result = WaterGeometry.Classify(bodies, probe, Thresholds());
            previous = result;
            hasPrevious = true;
            return result;
        }

        // Called on spawn, despawn, teleport and scene reset. After this the next call is
        // judged on raw geometry alone, so a diver who reappears somewhere else is not held
        // by where they used to be.
        public void Reset()
        {
            hasPrevious = false;
            previous = EnvironmentLocomotion.Land;
        }

        private WaterThresholds Thresholds()
        {
            // No memory yet: nothing to hold on to, so the raw lines decide.
            if (!hasPrevious) return WaterThresholds.Exact;

            switch (previous)
            {
                // Already fully under: keep both the water and the submersion until the
                // capsule is a clear band back out of each.
                case EnvironmentLocomotion.Underwater:
                    return new WaterThresholds(-deadband, -deadband, -deadband);

                // At the surface: hold the water past its lines, but going fully under is a
                // new claim and has to be earned by a band.
                case EnvironmentLocomotion.Surface:
                    return new WaterThresholds(-deadband, -deadband, deadband);

                // On land: entering the water at all is the new claim. The head line keeps
                // its raw threshold, so a diver who enters already fully submerged is called
                // underwater at once rather than spending a tick at the surface.
                default:
                    return new WaterThresholds(deadband, deadband, 0f);
            }
        }
    }
}
