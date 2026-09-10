using DeepDive.Network;
using UnityEngine;

namespace DeepDive.World
{
    // The only place World touches DeepDive.Network. SwimVolume is Mehmet's water query
    // (P1-A); the divers already use the same helper, so fish and divers agree on where the
    // water is. Read-only use: this calls the static query and owns no volume of its own.
    //
    // Isolated behind IWaterBounds so the dependency stays one file wide and the swim rules
    // stay testable without a scene.
    public sealed class SwimVolumeBounds : IWaterBounds
    {
        public static readonly SwimVolumeBounds Instance = new SwimVolumeBounds();

        public bool Contains(Vector3 point) => SwimVolume.Contains(point);
    }
}
