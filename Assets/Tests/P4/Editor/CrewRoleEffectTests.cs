using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class CrewRoleEffectTests
    {
        [Test]
        public void RoleResolvers_RecomputeFromBase_AndDoNotStack()
        {
            const float baseCooldown = 1f;
            const float baseRange = 20f;
            const float baseDamage = 2f;
            const int baseCapacity = 20000;

            var hunterCooldown = CrewRoleEffectRules.ResolveHarpoonCooldown(baseCooldown, CrewRole.Hunter);
            var hunterRange = CrewRoleEffectRules.ResolveHarpoonRange(baseRange, CrewRole.Hunter);
            var hunterDamage = CrewRoleEffectRules.ResolveHarpoonDamage(baseDamage, CrewRole.Hunter);
            var carrierCapacity = CrewRoleEffectRules.ResolveBagCapacityGrams(baseCapacity, CrewRole.Carrier);

            Assert.That(hunterCooldown, Is.EqualTo(CrewRoleEffectRules.ResolveHarpoonCooldown(baseCooldown, CrewRole.Hunter)).Within(0.0001f));
            Assert.That(hunterRange, Is.EqualTo(CrewRoleEffectRules.ResolveHarpoonRange(baseRange, CrewRole.Hunter)).Within(0.0001f));
            Assert.That(hunterDamage, Is.EqualTo(CrewRoleEffectRules.ResolveHarpoonDamage(baseDamage, CrewRole.Hunter)).Within(0.0001f));
            Assert.That(carrierCapacity, Is.EqualTo(CrewRoleEffectRules.ResolveBagCapacityGrams(baseCapacity, CrewRole.Carrier)));

            Assert.That(CrewRoleEffectRules.ResolveHarpoonCooldown(baseCooldown, CrewRole.None), Is.EqualTo(baseCooldown));
            Assert.That(CrewRoleEffectRules.ResolveBagCapacityGrams(baseCapacity, CrewRole.None), Is.EqualTo(baseCapacity));
        }

        [Test]
        public void Roles_OnlyAffectTheirOwnSmallCapability()
        {
            Assert.That(CrewRoleEffectRules.ResolveCameraFramingTolerance(CrewRole.CameraOperator),
                Is.EqualTo(CrewRoleEffectRules.CameraFramingToleranceDegrees));
            Assert.That(CrewRoleEffectRules.ResolveCameraFramingTolerance(CrewRole.Hunter), Is.Zero);

            Assert.That(CrewRoleEffectRules.ResolveOxygenDrainMultiplier(CrewRole.Explorer),
                Is.EqualTo(CrewRoleEffectRules.ExplorerOxygenDrainMultiplier));
            Assert.That(CrewRoleEffectRules.ResolveOxygenDrainMultiplier(CrewRole.Carrier), Is.EqualTo(1f));

            Assert.That(CrewRoleEffectRules.ResolvePingLifetime(20f, CrewRole.Explorer),
                Is.GreaterThan(CrewRoleEffectRules.ResolvePingLifetime(20f, CrewRole.None)));
        }

        [Test]
        public void CurrentSanitizer_RejectsNonFinite_AndClampsSpeed()
        {
            Assert.That(CrewEnvironmentRules.SanitizeCurrent(new Vector3(float.NaN, 0f, 0f)), Is.EqualTo(Vector3.zero));

            var clamped = CrewEnvironmentRules.SanitizeCurrent(new Vector3(100f, 0f, 0f));
            Assert.That(clamped.magnitude, Is.EqualTo(CrewEnvironmentRules.MaximumCurrentSpeedMetresPerSecond).Within(0.001f));
        }

        [Test]
        public void WeatherAdvisory_Default_KeepsSafeReturnOpen()
        {
            var clear = CrewEnvironmentAdvisory.Clear;
            Assert.That(clear.Warning, Is.EqualTo(CrewWarningKind.None));
            Assert.That(clear.OutboundAllowed, Is.True);
            Assert.That(clear.ReturnAllowed, Is.True);
        }
    }
}
