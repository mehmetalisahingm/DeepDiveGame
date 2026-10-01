using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public class DiverEquipmentRulesTests
    {
        private static readonly PlayerId Alice = new PlayerId(1);
        private static readonly PlayerId Bob = new PlayerId(2);

        [Test]
        public void EmptyLoadoutKeepsBaseOxygen()
        {
            var loadout = new LoadoutState(Alice, new string[0], 1);
            Assert.AreEqual(120f, DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout,
                new EquipmentDefinition[0]));
        }

        [Test]
        public void LevelOneTubeAddsThirtySeconds()
        {
            var loadout = new LoadoutState(Alice, new[] { "tube-1" }, 2);
            var definitions = new[] { new EquipmentDefinition("tube-1", "tube", 1, 30) };
            Assert.AreEqual(150f, DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout, definitions));
        }

        [Test]
        public void NonTubeEquipmentDoesNotChangeOxygen()
        {
            var loadout = new LoadoutState(Alice, new[] { "bag-2" }, 3);
            var definitions = new[] { new EquipmentDefinition("bag-2", "bag", 2, 30) };
            Assert.AreEqual(120f, DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout, definitions));
        }

        [Test]
        public void MultipleTubeDefinitionsUseStrongestLevelInsteadOfStacking()
        {
            var loadout = new LoadoutState(Alice, new[] { "tube-1", "tube-2" }, 4);
            var definitions = new[]
            {
                new EquipmentDefinition("tube-1", "tube", 1, 30),
                new EquipmentDefinition("tube-2", "tube", 2, 60)
            };
            Assert.AreEqual(180f, DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout, definitions));
        }

        [Test]
        public void AnotherPlayersLoadoutCannotChangeThisDiver()
        {
            var bobLoadout = new LoadoutState(Bob, new[] { "tube-2" }, 5);
            var definitions = new[] { new EquipmentDefinition("tube-2", "tube", 2, 60) };
            Assert.AreEqual(120f, DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, bobLoadout, definitions));
        }

        [Test]
        public void ReapplyingSameLoadoutIsIdempotent()
        {
            var loadout = new LoadoutState(Alice, new[] { "tube-1" }, 6);
            var definitions = new[] { new EquipmentDefinition("tube-1", "tube", 1, 30) };
            var first = DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout, definitions);
            var second = DiverEquipmentRules.ResolveMaxOxygen(120f, Alice, loadout, definitions);
            Assert.AreEqual(first, second);
            Assert.AreEqual(150f, second);
        }

        [Test]
        public void CameraSlotBelongsOnlyToMatchingPlayerAndOwnedDefinition()
        {
            var loadout = new LoadoutState(Alice, new[] { "camera-basic" }, 7);
            var camera = new EquipmentDefinition("camera-basic", "camera", 1, 150);

            Assert.IsTrue(DiverEquipmentRules.OwnsEquipmentSlot(Alice, loadout, new[] { camera }, "camera"));
            Assert.IsFalse(DiverEquipmentRules.OwnsEquipmentSlot(Bob, loadout, new[] { camera }, "camera"));

            var mismatchedDefinition = new EquipmentDefinition("camera-other", "camera", 1, 150);
            Assert.IsFalse(DiverEquipmentRules.OwnsEquipmentSlot(Alice, loadout,
                new[] { mismatchedDefinition }, "camera"),
                "A camera definition not present in this player's loadout must not grant camera ownership.");
        }

        [Test]
        public void CameraLevelsMapToThreeP4TiersWithoutStacking()
        {
            var loadout = new LoadoutState(Alice, new[] { "camera-basic", "camera-pro" }, 8);
            var definitions = new[]
            {
                new EquipmentDefinition("camera-basic", DiverEquipmentRules.CameraSlot, 1, 150),
                new EquipmentDefinition("camera-pro", DiverEquipmentRules.CameraSlot, 3, 900)
            };

            var capabilities = DiverEquipmentRules.ResolveCapabilities(Alice, loadout, definitions);

            Assert.AreEqual(CameraTier.Professional, capabilities.CameraTier);
            Assert.IsTrue(capabilities.HasCamera);
        }

        [Test]
        public void P4UpgradeSlotsResolveStrongestOwnedLevelIndependently()
        {
            var loadout = new LoadoutState(Alice,
                new[] { "fins-1", "fins-2", "bag-1", "harpoon-1" }, 9);
            var definitions = new[]
            {
                new EquipmentDefinition("fins-1", DiverEquipmentRules.FinsSlot, 1, 100),
                new EquipmentDefinition("fins-2", DiverEquipmentRules.FinsSlot, 2, 250),
                new EquipmentDefinition("bag-1", DiverEquipmentRules.BagSlot, 1, 150),
                new EquipmentDefinition("harpoon-1", DiverEquipmentRules.HarpoonSlot, 1, 200)
            };

            var capabilities = DiverEquipmentRules.ResolveCapabilities(Alice, loadout, definitions);

            Assert.AreEqual(2, capabilities.FinsLevel);
            Assert.AreEqual(1, capabilities.BagLevel);
            Assert.AreEqual(1, capabilities.HarpoonLevel);
        }

        [Test]
        public void ForeignLoadoutCannotGrantP4Capabilities()
        {
            var bobLoadout = new LoadoutState(Bob,
                new[] { "camera-pro", "fins-2", "bag-1", "harpoon-1" }, 10);
            var definitions = new[]
            {
                new EquipmentDefinition("camera-pro", DiverEquipmentRules.CameraSlot, 3, 900),
                new EquipmentDefinition("fins-2", DiverEquipmentRules.FinsSlot, 2, 250),
                new EquipmentDefinition("bag-1", DiverEquipmentRules.BagSlot, 1, 150),
                new EquipmentDefinition("harpoon-1", DiverEquipmentRules.HarpoonSlot, 1, 200)
            };

            var capabilities = DiverEquipmentRules.ResolveCapabilities(Alice, bobLoadout, definitions);

            Assert.AreEqual(CameraTier.None, capabilities.CameraTier);
            Assert.AreEqual(0, capabilities.FinsLevel);
            Assert.AreEqual(0, capabilities.BagLevel);
            Assert.AreEqual(0, capabilities.HarpoonLevel);
        }

        [Test]
        public void MissingCatalogDefinitionCannotGrantCapabilityFromRawLoadoutId()
        {
            var loadout = new LoadoutState(Alice,
                new[] { "camera-pro", "fins-2", "bag-1", "harpoon-1" }, 11);
            var definitions = new[]
            {
                new EquipmentDefinition("camera-basic", DiverEquipmentRules.CameraSlot, 1, 150)
            };

            var capabilities = DiverEquipmentRules.ResolveCapabilities(Alice, loadout, definitions);

            Assert.AreEqual(CameraTier.None, capabilities.CameraTier);
            Assert.AreEqual(0, capabilities.FinsLevel);
            Assert.AreEqual(0, capabilities.BagLevel);
            Assert.AreEqual(0, capabilities.HarpoonLevel);
        }

        [Test]
        public void VitalsCapacityUpgradeRefillsOnceAndPersistsAcrossDiveReset()
        {
            var vitals = new DiverVitalsState(120f, 100f, 1f, 0.2f);
            vitals.Tick(20f, true);
            Assert.AreEqual(100f, vitals.Oxygen);

            Assert.IsTrue(vitals.SetMaxOxygen(150f, refill: true));
            Assert.AreEqual(150f, vitals.MaxOxygen);
            Assert.AreEqual(150f, vitals.Oxygen);

            vitals.Tick(10f, true);
            Assert.IsFalse(vitals.SetMaxOxygen(150f, refill: true));
            Assert.AreEqual(140f, vitals.Oxygen, "Duplicate loadout replay must not refill oxygen.");

            vitals.Reset();
            Assert.AreEqual(150f, vitals.Oxygen);
        }
    }
}
