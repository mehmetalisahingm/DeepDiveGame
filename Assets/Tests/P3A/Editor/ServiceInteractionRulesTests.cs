using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class ServiceInteractionRulesTests
    {
        private static ServicePointDefinition Shop(float distance = 2.5f) =>
            new ServicePointDefinition("equipment-shop", ServicePointType.EquipmentShop,
                "town.equipment.anchor", distance, "equipment-basic");

        [Test]
        public void ValidVisibleServiceWithinRangeIsAccepted()
        {
            var result = ServiceInteractionRules.Validate(Shop(), Vector3.zero,
                new Vector3(0f, 0f, 2f), hasClearLineOfSight: true);

            Assert.That(result, Is.EqualTo(PlayerActionResult.Accepted));
        }

        [Test]
        public void OutOfRangeServiceIsRejectedByHostRule()
        {
            var result = ServiceInteractionRules.Validate(Shop(2f), Vector3.zero,
                new Vector3(0f, 0f, 2.01f), hasClearLineOfSight: true);

            Assert.That(result, Is.EqualTo(PlayerActionResult.InvalidTarget));
        }

        [Test]
        public void BlockedServiceIsRejectedEvenWhenClose()
        {
            var result = ServiceInteractionRules.Validate(Shop(), Vector3.zero,
                Vector3.forward, hasClearLineOfSight: false);

            Assert.That(result, Is.EqualTo(PlayerActionResult.InvalidTarget));
        }

        [Test]
        public void InvalidServiceDefinitionIsRejected()
        {
            var invalid = new ServicePointDefinition("", ServicePointType.None, "", 2.5f, "");
            Assert.That(ServiceInteractionRules.Validate(invalid, Vector3.zero, Vector3.zero, true),
                Is.EqualTo(PlayerActionResult.InvalidTarget));
        }
    }
}
