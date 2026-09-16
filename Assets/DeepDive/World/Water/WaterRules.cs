using System.Collections.Generic;
using DeepDive.Network;

namespace DeepDive.World
{
    // The raw water classification: given a diver's capsule and the water around it, say
    // which environment they are in. Pure and stateless - it reads its arguments, returns an
    // answer, and remembers nothing.
    //
    // Raw means no hysteresis. A capsule sitting exactly on a threshold will flip between two
    // answers as it jitters; the deadband that stops that is WaterTracker's job and wraps
    // this, so the geometry stays separately testable from the smoothing.
    //
    // Two points decide everything: the capsule's lowest and highest points, and the XZ of
    // its lower end. Feet decide whether you are over water at all - a diver leaning out over
    // the sea from a rock is still on land, and one leaning inland from the shallows is still
    // in the water. The measurements themselves live in WaterGeometry, shared with the
    // tracker so there is only ever one copy of them.
    public static class WaterRules
    {
        public static EnvironmentLocomotion ClassifyRaw(IReadOnlyList<WaterBody> bodies, in WaterProbe probe) =>
            WaterGeometry.Classify(bodies, probe, WaterThresholds.Exact);
    }
}
