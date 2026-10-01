using UnityEngine;

namespace DeepDive.World
{
    // What the water classifier needs to know about one diver, and nothing more - the same
    // narrowing IWaterBounds does for the fish swim rules. A value struct so a classification
    // can be tested against plain numbers instead of a live CharacterController, and so World
    // never has to reach into Mehmet's player.
    //
    // The probe describes a capsule. Its centre is NOT Position: worldCenter = Position +
    // CenterOffsetWorld, a world-axis offset, already rotated by the caller. P3.1 assumes yaw
    // only; if pitch/roll arrive, this seam is revised.
    public readonly struct WaterProbe
    {
        // Feet/transform origin of the diver, not the capsule centre.
        public readonly Vector3 Position;

        // World-axis offset from Position to the capsule centre. See the note above.
        public readonly Vector3 CenterOffsetWorld;

        public readonly float Radius;
        public readonly float Height;

        // Capsule axis. Up-vector rather than a rotation, because only the axis matters here.
        public readonly Vector3 Up;

        public WaterProbe(Vector3 position, Vector3 centerOffsetWorld, float radius, float height, Vector3 up)
        {
            Position = position;
            CenterOffsetWorld = centerOffsetWorld;
            Radius = radius;
            Height = height;
            Up = up;
        }
    }
}
