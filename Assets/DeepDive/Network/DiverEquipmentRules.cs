using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    // P3/P4 equipment rules stay pure so a restored LoadoutState can be re-applied safely.
    // Economy/catalog ownership stays outside Network; this class only derives player capability.
    public static class DiverEquipmentRules
    {
        public const float TubeOxygenSecondsPerLevel = 30f;
        public const string CameraSlot = "camera";
        public const string FinsSlot = "fins";
        public const string BagSlot = "bag";
        public const string HarpoonSlot = "harpoon";

        public static float ResolveMaxOxygen(float baseMaxOxygen, PlayerId expectedPlayer,
            LoadoutState loadout, IReadOnlyList<EquipmentDefinition> equippedDefinitions)
        {
            var baseline = FinitePositive(baseMaxOxygen) ? baseMaxOxygen : 120f;
            if (!loadout.PlayerId.Equals(expectedPlayer) || equippedDefinitions == null)
                return baseline;

            var strongestTubeLevel = StrongestOwnedLevel(loadout, equippedDefinitions, "tube");
            return baseline + strongestTubeLevel * TubeOxygenSecondsPerLevel;
        }

        public static PlayerEquipmentCapabilities ResolveCapabilities(PlayerId expectedPlayer,
            LoadoutState loadout, IReadOnlyList<EquipmentDefinition> equippedDefinitions)
        {
            if (!loadout.PlayerId.Equals(expectedPlayer) || equippedDefinitions == null)
                return new PlayerEquipmentCapabilities(CameraTier.None, 0, 0, 0);

            var cameraLevel = StrongestOwnedLevel(loadout, equippedDefinitions, CameraSlot);
            var cameraTier = cameraLevel <= 0
                ? CameraTier.None
                : cameraLevel == 1
                    ? CameraTier.Basic
                    : cameraLevel == 2
                        ? CameraTier.Advanced
                        : CameraTier.Professional;

            return new PlayerEquipmentCapabilities(
                cameraTier,
                StrongestOwnedLevel(loadout, equippedDefinitions, FinsSlot),
                StrongestOwnedLevel(loadout, equippedDefinitions, BagSlot),
                StrongestOwnedLevel(loadout, equippedDefinitions, HarpoonSlot));
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

        private static int StrongestOwnedLevel(LoadoutState loadout,
            IReadOnlyList<EquipmentDefinition> equippedDefinitions, string slot)
        {
            if (equippedDefinitions == null || string.IsNullOrWhiteSpace(slot)) return 0;

            var strongestLevel = 0;
            for (var i = 0; i < equippedDefinitions.Count; i++)
            {
                var definition = equippedDefinitions[i];
                if (!string.Equals(definition.Slot, slot, StringComparison.OrdinalIgnoreCase)) continue;
                if (!ContainsEquipmentId(loadout.EquippedIds, definition.EquipmentId)) continue;
                strongestLevel = Math.Max(strongestLevel, Math.Max(0, definition.Level));
            }

            return strongestLevel;
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
