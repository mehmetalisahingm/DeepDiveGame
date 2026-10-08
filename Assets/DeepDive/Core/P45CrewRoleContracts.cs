using System;
using UnityEngine;

namespace DeepDive.Core.Contracts
{
    public enum CrewRole : byte
    {
        None = 0,
        CameraOperator = 1,
        Hunter = 2,
        Explorer = 3,
        Carrier = 4
    }

    public enum CrewPingKind : byte
    {
        None = 0,
        Return = 1,
        Interest = 2,
        Danger = 3
    }

    public enum CrewWarningKind : byte
    {
        None = 0,
        LocalCurrent = 1,
        Windy = 2,
        ReturnRecommended = 3
    }

    // P4.5-A (#130): small role bonuses only. Every resolver starts from the supplied base
    // value, so role changes, reconnects and save restores cannot accumulate modifiers.
    public static class CrewRoleEffectRules
    {
        public const float CameraFramingToleranceDegrees = 3f;
        public const float HunterHarpoonCooldownMultiplier = 0.90f;
        public const float HunterHarpoonRangeBonusMetres = 1.5f;
        public const float HunterHarpoonDamageMultiplier = 1.10f;
        public const float ExplorerOxygenDrainMultiplier = 0.90f;
        public const float ExplorerPingLifetimeMultiplier = 1.35f;
        public const int CarrierCapacityBonusGrams = 5000;

        public static bool IsValid(CrewRole role) =>
            role == CrewRole.None || role == CrewRole.CameraOperator || role == CrewRole.Hunter ||
            role == CrewRole.Explorer || role == CrewRole.Carrier;

        public static float ResolveCameraFramingTolerance(CrewRole role) =>
            role == CrewRole.CameraOperator ? CameraFramingToleranceDegrees : 0f;

        public static float ResolveHarpoonCooldown(float baseCooldown, CrewRole role) =>
            Math.Max(0f, baseCooldown) * (role == CrewRole.Hunter ? HunterHarpoonCooldownMultiplier : 1f);

        public static float ResolveHarpoonRange(float baseRange, CrewRole role) =>
            Math.Max(0f, baseRange) + (role == CrewRole.Hunter ? HunterHarpoonRangeBonusMetres : 0f);

        public static float ResolveHarpoonDamage(float baseDamage, CrewRole role) =>
            Math.Max(0f, baseDamage) * (role == CrewRole.Hunter ? HunterHarpoonDamageMultiplier : 1f);

        public static float ResolveOxygenDrainMultiplier(CrewRole role) =>
            role == CrewRole.Explorer ? ExplorerOxygenDrainMultiplier : 1f;

        public static int ResolveBagCapacityGrams(int baseCapacityGrams, CrewRole role) =>
            Math.Max(0, baseCapacityGrams) + (role == CrewRole.Carrier ? CarrierCapacityBonusGrams : 0);

        public static float ResolvePingLifetime(float baseSeconds, CrewRole role) =>
            Math.Max(0f, baseSeconds) * (role == CrewRole.Explorer ? ExplorerPingLifetimeMultiplier : 1f);
    }

    public readonly struct CrewCurrentSample
    {
        public readonly Vector3 DriftMetresPerSecond;
        public readonly bool WarnPlayer;

        public CrewCurrentSample(Vector3 driftMetresPerSecond, bool warnPlayer)
        {
            DriftMetresPerSecond = driftMetresPerSecond;
            WarnPlayer = warnPlayer;
        }
    }

    public readonly struct CrewEnvironmentAdvisory
    {
        public readonly CrewWarningKind Warning;
        public readonly bool OutboundAllowed;
        public readonly bool ReturnAllowed;

        public CrewEnvironmentAdvisory(CrewWarningKind warning, bool outboundAllowed, bool returnAllowed)
        {
            Warning = warning;
            OutboundAllowed = outboundAllowed;
            ReturnAllowed = returnAllowed;
        }

        public static CrewEnvironmentAdvisory Clear => new CrewEnvironmentAdvisory(CrewWarningKind.None, true, true);
    }

    // Utku's P4.5 world layer implements these read-only seams. Mehmet's composition layer samples
    // them on the host; clients never submit a current vector or a weather verdict.
    public interface ICrewCurrentSource
    {
        bool TrySampleCurrent(Vector3 worldPosition, out CrewCurrentSample sample);
    }

    public interface ICrewWeatherAdvisorySource
    {
        CrewEnvironmentAdvisory CurrentAdvisory { get; }
    }

    public static class CrewEnvironmentRules
    {
        public const float MaximumCurrentSpeedMetresPerSecond = 2f;

        public static Vector3 SanitizeCurrent(Vector3 value)
        {
            if (!Finite(value.x) || !Finite(value.y) || !Finite(value.z)) return Vector3.zero;
            return Vector3.ClampMagnitude(value, MaximumCurrentSpeedMetresPerSecond);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
