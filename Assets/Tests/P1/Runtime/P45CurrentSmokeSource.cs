using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Test-only provider for P4.5-A. It proves Mehmet's consumer seam against a host-owned
    // current source without implementing Utku's actual world/current content.
    public sealed class P45CurrentSmokeSource : MonoBehaviour, ICrewCurrentSource
    {
        public bool Active { get; set; }

        public bool TrySampleCurrent(Vector3 worldPosition, out CrewCurrentSample sample)
        {
            if (!Active)
            {
                sample = default;
                return false;
            }

            sample = new CrewCurrentSample(new Vector3(0.65f, 0f, 0f), true);
            return true;
        }
    }
}
