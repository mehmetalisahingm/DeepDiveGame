using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class PlayerPresentationRulesTests
    {
        [Test]
        public void PassiveOverridesSeatedAndEnvironment()
        {
            Assert.That(PlayerPresentationRules.ResolveLocomotion(EnvironmentLocomotion.Underwater, true, true),
                Is.EqualTo(LocomotionMode.Passive));
        }

        [Test]
        public void SeatedOverridesWaterWhenPlayerIsActive()
        {
            Assert.That(PlayerPresentationRules.ResolveLocomotion(EnvironmentLocomotion.Underwater, true, false),
                Is.EqualTo(LocomotionMode.Seated));
        }

        [TestCase(EnvironmentLocomotion.Land, LocomotionMode.Land)]
        [TestCase(EnvironmentLocomotion.Surface, LocomotionMode.Surface)]
        [TestCase(EnvironmentLocomotion.Underwater, LocomotionMode.Underwater)]
        public void ActiveLocomotionMapsEnvironment(EnvironmentLocomotion environment, LocomotionMode expected)
        {
            Assert.That(PlayerPresentationRules.ResolveLocomotion(environment, false, false), Is.EqualTo(expected));
        }

        [Test]
        public void SurfaceBlocksUpwardInputButStillAllowsDiveInput()
        {
            var up = PlayerPresentationRules.FilterMoveInput(LocomotionMode.Surface, new Vector3(0f, 1f, 0f));
            var down = PlayerPresentationRules.FilterMoveInput(LocomotionMode.Surface, new Vector3(0f, -1f, 0f));

            Assert.That(up.y, Is.EqualTo(0f));
            Assert.That(down.y, Is.LessThan(0f));
        }

        [Test]
        public void UnderwaterAllowsVerticalInputAndLandDoesNot()
        {
            var underwater = PlayerPresentationRules.FilterMoveInput(LocomotionMode.Underwater, new Vector3(0f, 0.6f, 0f));
            var land = PlayerPresentationRules.FilterMoveInput(LocomotionMode.Land, new Vector3(0f, 0.6f, 0f));

            Assert.That(underwater.y, Is.GreaterThan(0f));
            Assert.That(land.y, Is.EqualTo(0f));
        }

        [Test]
        public void OnlyUnderwaterDrainsOxygen()
        {
            Assert.That(PlayerPresentationRules.DrainsOxygen(LocomotionMode.Underwater), Is.True);
            Assert.That(PlayerPresentationRules.DrainsOxygen(LocomotionMode.Surface), Is.False);
            Assert.That(PlayerPresentationRules.DrainsOxygen(LocomotionMode.Land), Is.False);
        }

        [Test]
        public void RecordingForcesCameraAndPassiveHidesEquipment()
        {
            Assert.That(PlayerPresentationRules.ResolveHeldEquipment(true, false, HeldEquipmentMode.Harpoon, true),
                Is.EqualTo(HeldEquipmentMode.Camera));
            Assert.That(PlayerPresentationRules.ResolveHeldEquipment(true, true, HeldEquipmentMode.Camera, true),
                Is.EqualTo(HeldEquipmentMode.None));
        }

        [Test]
        public void HarpoonCannotFireWhileCameraIsHeld()
        {
            Assert.That(PlayerPresentationRules.CanUseHarpoon(true, false, LocomotionMode.Underwater,
                HeldEquipmentMode.Camera), Is.False);
            Assert.That(PlayerPresentationRules.CanUseHarpoon(true, false, LocomotionMode.Underwater,
                HeldEquipmentMode.Harpoon), Is.True);
        }
    }
}
