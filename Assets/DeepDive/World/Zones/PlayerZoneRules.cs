using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.World
{
    // An axis-aligned zone volume in world space. Faces are inclusive, so two boxes that share a
    // face both contain it and the zone priority alone decides who wins there.
    public readonly struct ZoneBox
    {
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        private ZoneBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        // Non-finite, inverted or zero-thickness boxes are refused rather than kept as a volume
        // that silently contains nothing.
        public static bool TryCreate(Vector3 min, Vector3 max, out ZoneBox box)
        {
            box = default;
            if (!IsFinite(min) || !IsFinite(max)) return false;
            if (max.x <= min.x || max.y <= min.y || max.z <= min.z) return false;
            box = new ZoneBox(min, max);
            return true;
        }

        public static bool TryFromCenterSize(Vector3 center, Vector3 size, out ZoneBox box) =>
            TryCreate(center - size * 0.5f, center + size * 0.5f, out box);

        public bool Contains(Vector3 p) =>
            p.x >= Min.x && p.x <= Max.x &&
            p.y >= Min.y && p.y <= Max.y &&
            p.z >= Min.z && p.z <= Max.z;

        internal static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    // The capsule the Sea test measures: NetworkDiver's CharacterController (r 0.35, h 1.8,
    // centre 0.9 above the feet). The same shape the water tracker is fed for a walking diver.
    public static class ZoneProbeCapsule
    {
        public const float DiverRadius = 0.35f;
        public const float DiverHeight = 1.8f;
        public const float DiverCenterHeight = 0.9f;

        public static WaterProbe Diver(Vector3 feetPosition) =>
            new WaterProbe(feetPosition, Vector3.up * DiverCenterHeight, DiverRadius, DiverHeight, Vector3.up);
    }

    public readonly struct ZoneVolumeEntry
    {
        public readonly PlayerZone Zone;
        public readonly ZoneBox Box;

        public ZoneVolumeEntry(PlayerZone zone, ZoneBox box)
        {
            Zone = zone;
            Box = box;
        }
    }

    // Feet position -> zone. Pure and stateless, per player: it remembers nothing between calls, so
    // four divers queried in any order get the same four answers.
    //
    // Sea comes first and comes only from the water rules (WaterRules.ClassifyRaw != Land); a zone
    // box never says Sea. Then boxes by fixed priority Home > Dock > Town > Coast, independent of
    // the order they were listed in.
    public static class PlayerZoneRules
    {
        public static bool IsBoxZone(PlayerZone zone) => Priority(zone) > 0;

        public static PlayerZone Resolve(Vector3 feetPosition, IReadOnlyList<WaterBody> water,
            IReadOnlyList<ZoneVolumeEntry> volumes)
        {
            if (!ZoneBox.IsFinite(feetPosition)) return PlayerZone.Unknown;

            if (water != null && water.Count > 0 &&
                WaterRules.ClassifyRaw(water, ZoneProbeCapsule.Diver(feetPosition)) != EnvironmentLocomotion.Land)
                return PlayerZone.Sea;

            var best = PlayerZone.Unknown;
            if (volumes == null) return best;
            for (var i = 0; i < volumes.Count; i++)
            {
                var entry = volumes[i];
                if (Priority(entry.Zone) <= Priority(best)) continue;
                if (entry.Box.Contains(feetPosition)) best = entry.Zone;
            }
            return best;
        }

        // Spelled out so the enum's byte values never become the priority by accident.
        private static int Priority(PlayerZone zone) => zone switch
        {
            PlayerZone.Home => 4,
            PlayerZone.Dock => 3,
            PlayerZone.Town => 2,
            PlayerZone.Coast => 1,
            _ => 0
        };
    }
}
