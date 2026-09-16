using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // The scene's water, read once into the shape the rules want.
    //
    // This is the shell around the pure layer: it is the only part that knows about colliders,
    // transforms and scene lifetimes. WaterBody, WaterRules and WaterTracker never see a
    // GameObject, which is what keeps them testable without a scene.
    //
    // It deliberately does NOT implement IWaterField. Hysteresis is per diver - two divers on
    // opposite sides of the same shore line hold different states - so the field hands out a
    // tracker per diver through CreateTracker instead of classifying on their behalf.
    [DisallowMultipleComponent]
    public sealed class WaterField : MonoBehaviour
    {
        [SerializeField] private BoxCollider[] volumes;
        [SerializeField] private float deadband = WaterTracker.DefaultDeadband;

        // Rebuilt in place rather than replaced, so trackers holding this list by reference
        // keep seeing the current water instead of a detached snapshot.
        private readonly List<WaterBody> bodies = new List<WaterBody>();
        private bool built;
        private bool reportedRotation;

        // Anything beyond this counts as a rotated volume. Degrees, not a dot product, so the
        // number in the log reads the same way as the inspector's.
        private const float RotationToleranceDegrees = 0.1f;

        public float Deadband => deadband;

        public int VolumeCount => volumes == null ? 0 : volumes.Length;

        // Built on first read rather than in Awake: an editor test opens the scene without
        // ever entering play mode, so Awake never runs and a field built there would be empty.
        public IReadOnlyList<WaterBody> Bodies
        {
            get
            {
                EnsureBuilt();
                return bodies;
            }
        }

        // One per diver. The tracker keeps this field's list by reference, so water that moves
        // or is rebuilt reaches every live tracker without recreating them.
        //
        // What does NOT follow automatically is the tracker's memory: after RebuildVolumes the
        // last answer was decided against water that no longer exists, so Composition resets
        // or replaces the trackers it owns rather than letting a stale state hold.
        public IWaterField CreateTracker()
        {
            EnsureBuilt();
            return new WaterTracker(bodies, deadband);
        }

        // For a shell that adds or removes water at runtime, and for tests that move a volume
        // after the first read.
        public void RebuildVolumes()
        {
            built = false;
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (built) return;
            built = true;
            bodies.Clear();
            if (volumes == null) return;

            foreach (var volume in volumes)
            {
                if (volume == null) continue;

                var rotation = volume.transform.rotation;
                if (Quaternion.Angle(rotation, Quaternion.identity) > RotationToleranceDegrees)
                {
                    // The pure layer measures an axis-aligned footprint, so a rotated box would
                    // be silently read as its unrotated self - water where there is none. The
                    // shell is the only place that can see the rotation, so refusing it is its
                    // job. Reported once per field: a rotated volume stays rotated, and a line
                    // per rebuild would bury the rest of the log.
                    if (!reportedRotation)
                    {
                        reportedRotation = true;
                        Debug.LogError(
                            $"P3_WATER_VOLUME_ROTATED field={name} volume={volume.name} " +
                            $"euler={rotation.eulerAngles} reason=water volumes must be axis aligned",
                            volume);
                    }
                    continue;
                }

                var worldCenter = volume.transform.TransformPoint(volume.center);
                var worldSize = Vector3.Scale(volume.size, volume.transform.lossyScale);
                if (WaterBody.TryFromBox(worldCenter, worldSize, out var body)) bodies.Add(body);
            }
        }
    }
}
