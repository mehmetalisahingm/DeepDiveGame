using System;

namespace DeepDive.Core.Contracts
{
    public enum CameraTier
    {
        None = 0,
        Basic = 1,
        Advanced = 2,
        Professional = 3
    }

    // Player-facing capability snapshot derived from the authoritative LoadoutState + catalog.
    // It contains no prices or world validation rules: Mert owns catalog/ownership, Utku owns
    // capture/world validation, and Mehmet consumes this result in player/network presentation.
    public readonly struct PlayerEquipmentCapabilities
    {
        public readonly CameraTier CameraTier;
        public readonly int FinsLevel;
        public readonly int BagLevel;
        public readonly int HarpoonLevel;

        public PlayerEquipmentCapabilities(CameraTier cameraTier, int finsLevel, int bagLevel, int harpoonLevel)
        {
            CameraTier = cameraTier;
            FinsLevel = NonNegative(finsLevel);
            BagLevel = NonNegative(bagLevel);
            HarpoonLevel = NonNegative(harpoonLevel);
        }

        public bool HasCamera => CameraTier != CameraTier.None;

        private static int NonNegative(int value) => value < 0 ? 0 : value;
    }

    // P4.3 tuning seam. These functions always recompute from the serialized base value and the
    // strongest owned level, so reconnect/loadout replay cannot stack effects. The numbers are
    // deliberately small first-pass gameplay values and can be retuned without changing ownership.
    public static class P4EquipmentEffectRules
    {
        public const float FinsSpeedBonusPerLevel = 0.15f;
        public const int BagCapacityBonusGramsPerLevel = 5000;
        public const float HarpoonRangeBonusPerLevel = 3f;
        public const float HarpoonDamageBonusPerLevel = 0.25f;
        public const float HarpoonCooldownReductionPerLevel = 0.08f;
        public const float MinimumHarpoonCooldownMultiplier = 0.60f;

        public static float ResolveSwimSpeed(float baseSpeed, int finsLevel)
        {
            var safeBase = Math.Max(0f, baseSpeed);
            return safeBase * (1f + NonNegative(finsLevel) * FinsSpeedBonusPerLevel);
        }

        public static int ResolveBagCapacityGrams(int baseCapacityGrams, int bagLevel)
        {
            var safeBase = Math.Max(0, baseCapacityGrams);
            return safeBase + NonNegative(bagLevel) * BagCapacityBonusGramsPerLevel;
        }

        public static float ResolveHarpoonRange(float baseRange, int harpoonLevel) =>
            Math.Max(0f, baseRange) + NonNegative(harpoonLevel) * HarpoonRangeBonusPerLevel;

        public static float ResolveHarpoonDamage(float baseDamage, int harpoonLevel) =>
            Math.Max(0f, baseDamage) + NonNegative(harpoonLevel) * HarpoonDamageBonusPerLevel;

        public static float ResolveHarpoonCooldown(float baseCooldown, int harpoonLevel)
        {
            var multiplier = Math.Max(MinimumHarpoonCooldownMultiplier,
                1f - NonNegative(harpoonLevel) * HarpoonCooldownReductionPerLevel);
            return Math.Max(0f, baseCooldown) * multiplier;
        }

        private static int NonNegative(int value) => value < 0 ? 0 : value;
    }
}
