using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // One zone box as scene data (#138). No collider: a zone is read by WorldZoneField, never by
    // physics, so it can neither block a diver nor raise a trigger. The box is the transform's
    // position plus an authored world size; rotation and scale are ignored on purpose.
    [DisallowMultipleComponent]
    public sealed class WorldZoneVolume : MonoBehaviour
    {
        [SerializeField] private PlayerZone zone = PlayerZone.Unknown;
        [SerializeField] private Vector3 size = Vector3.zero;

        public PlayerZone Zone => zone;
        public Vector3 Size => size;

        public void Configure(PlayerZone volumeZone, Vector3 worldSize)
        {
            zone = volumeZone;
            size = worldSize;
        }

        // Sea/Unknown are not box zones and a degenerate box contains nothing; both are refused.
        public bool TryGetEntry(out ZoneVolumeEntry entry)
        {
            entry = default;
            if (!PlayerZoneRules.IsBoxZone(zone)) return false;
            if (!ZoneBox.TryFromCenterSize(transform.position, size, out var box)) return false;
            entry = new ZoneVolumeEntry(zone, box);
            return true;
        }
    }
}
