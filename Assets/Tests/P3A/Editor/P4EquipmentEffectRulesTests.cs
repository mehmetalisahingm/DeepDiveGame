using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public class P4EquipmentEffectRulesTests
    {
        [Test]
        public void FinsRecomputeFromBaseWithoutStacking()
        {
            var first = P4EquipmentEffectRules.ResolveSwimSpeed(3f, 1);
            var replay = P4EquipmentEffectRules.ResolveSwimSpeed(3f, 1);

            Assert.AreEqual(3.45f, first, 0.0001f);
            Assert.AreEqual(first, replay, 0.0001f);
        }

        [Test]
        public void BagLevelAddsDeterministicCapacity()
        {
            Assert.AreEqual(20000, P4EquipmentEffectRules.ResolveBagCapacityGrams(20000, 0));
            Assert.AreEqual(25000, P4EquipmentEffectRules.ResolveBagCapacityGrams(20000, 1));
            Assert.AreEqual(30000, P4EquipmentEffectRules.ResolveBagCapacityGrams(20000, 2));
        }

        [Test]
        public void HarpoonUpgradeImprovesRangeDamageAndCooldownFromBase()
        {
            Assert.AreEqual(25f, P4EquipmentEffectRules.ResolveHarpoonRange(22f, 1), 0.0001f);
            Assert.AreEqual(1.25f, P4EquipmentEffectRules.ResolveHarpoonDamage(1f, 1), 0.0001f);
            Assert.AreEqual(0.598f, P4EquipmentEffectRules.ResolveHarpoonCooldown(0.65f, 1), 0.0001f);
        }

        [Test]
        public void NegativeLevelsNeverReduceBaseCapability()
        {
            Assert.AreEqual(3f, P4EquipmentEffectRules.ResolveSwimSpeed(3f, -4), 0.0001f);
            Assert.AreEqual(20000, P4EquipmentEffectRules.ResolveBagCapacityGrams(20000, -4));
            Assert.AreEqual(22f, P4EquipmentEffectRules.ResolveHarpoonRange(22f, -4), 0.0001f);
            Assert.AreEqual(1f, P4EquipmentEffectRules.ResolveHarpoonDamage(1f, -4), 0.0001f);
            Assert.AreEqual(0.65f, P4EquipmentEffectRules.ResolveHarpoonCooldown(0.65f, -4), 0.0001f);
        }
    }
}
