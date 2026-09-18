using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    // P3-A equipment rules stay pure so a restored LoadoutState can be re-applied safely.
    // Current P3 economy definitions expose slot + level; tube levels map to +30 seconds each.
    public static class DiverEquipmentRules
    {
        public const float TubeOxygenSecondsPerLevel = 30f;

        public static float ResolveMaxOxygen(float baseMaxOxygen, PlayerId expectedPlayer,
            LoadoutState loadout, IReadOnlyList<EquipmentDefinition> equippedDefinitions)
        {
            var baseline = FinitePositive(baseMaxOxygen) ? baseMaxOxygen : 120f;
            if (!loadout.PlayerId.Equals(expectedPlayer) || equippedDefinitions == null)
                return baseline;

            var strongestTubeLevel = 0;
            for (var i = 0; i < equippedDefinitions.Count; i++)
            {
                var definition = equippedDefinitions[i];
                if (!ContainsEquipmentId(loadout.EquippedIds, definition.EquipmentId)) continue;
                if (!string.Equals(definition.Slot, "tube", StringComparison.OrdinalIgnoreCase)) continue;
                strongestTubeLevel = Math.Max(strongestTubeLevel, Math.Max(0, definition.Level));
            }

            return baseline + strongestTubeLevel * TubeOxygenSecondsPerLevel;
        }

        public static bool OwnsEquipmentSlot(PlayerId expectedPlayer, LoadoutState loadout,
            IReadOnlyList<EquipmentDefinition> equippedDefinitions, string slot)
        {
            if (!loadout.PlayerId.Equals(expectedPlayer) || equippedDefinitions == null ||
                string.IsNullOrWhiteSpace(slot))
                return false;

            for (var i = 0; i < equippedDefinitions.Count; i++)
            {
                var definition = equippedDefinitions[i];
                if (!string.Equals(definition.Slot, slot, StringComparison.OrdinalIgnoreCase)) continue;
                if (ContainsEquipmentId(loadout.EquippedIds, definition.EquipmentId)) return true;
            }

            return false;
        }

        private static bool ContainsEquipmentId(IReadOnlyList<string> equipmentIds, string equipmentId)
        {
            if (equipmentIds == null || string.IsNullOrWhiteSpace(equipmentId)) return false;
            for (var i = 0; i < equipmentIds.Count; i++)
                if (string.Equals(equipmentIds[i], equipmentId, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool FinitePositive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
