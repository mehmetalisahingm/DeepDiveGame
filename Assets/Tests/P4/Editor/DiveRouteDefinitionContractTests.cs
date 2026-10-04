using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    // P4.3-B2 (#108): the Core half of the route contract. DiveRouteDefinition now names the least vehicle class a
    // route needs; the P3 five-argument shape must keep meaning "the rowboat's route" and default must never mean
    // "any vehicle". Eligibility itself is World's rule and is tested there.
    public sealed class DiveRouteDefinitionContractTests
    {
        [Test]
        public void LegacyFiveArgCtor_RequiresRowboat()
        {
            var route = new DiveRouteDefinition(BoatTripIds.NearRouteId, "dock-town-1", "anchor-near-1", 8f, 8f);

            Assert.That(route.RequiredVehicleClass, Is.EqualTo(VehicleClass.Rowboat));
            Assert.That(route.RequiredVehicleClass, Is.Not.EqualTo(VehicleClass.None), "the legacy path must not produce None");
        }

        [Test]
        public void LegacyFiveArgCtor_KeepsEveryExistingFieldIdentical()
        {
            var route = new DiveRouteDefinition(BoatTripIds.NearRouteId, "dock-town-1", "anchor-near-1", 8f, 7.5f);

            Assert.That(route.RouteId, Is.EqualTo("route-near-1"));
            Assert.That(route.DepartureDockAnchor, Is.EqualTo("dock-town-1"));
            Assert.That(route.AnchorPointAnchor, Is.EqualTo("anchor-near-1"));
            Assert.That(route.OutboundSeconds, Is.EqualTo(8f));
            Assert.That(route.InboundSeconds, Is.EqualTo(7.5f));
        }

        [TestCase(VehicleClass.Rowboat)]
        [TestCase(VehicleClass.Motorboat)]
        [TestCase(VehicleClass.ResearchBoat)]
        public void SixArgCtor_CarriesRequiredClass(VehicleClass required)
        {
            var route = new DiveRouteDefinition("r", "d", "a", 28f, 28f, required);

            Assert.That(route.RequiredVehicleClass, Is.EqualTo(required));
            Assert.That(route.OutboundSeconds, Is.EqualTo(28f));
            Assert.That(route.InboundSeconds, Is.EqualTo(28f));
        }

        [Test]
        public void SixArgCtor_StoresNoneAsGiven_ValidationIsNotCore()
        {
            var route = new DiveRouteDefinition("r", "d", "a", 1f, 1f, VehicleClass.None);

            Assert.That(route.RequiredVehicleClass, Is.EqualTo(VehicleClass.None));
        }

        [Test]
        public void Ctor_NullStrings_FoldToEmpty()
        {
            var legacy = new DiveRouteDefinition(null, null, null, 0f, 0f);
            var full = new DiveRouteDefinition(null, null, null, 0f, 0f, VehicleClass.Motorboat);

            foreach (var route in new[] { legacy, full })
            {
                Assert.That(route.RouteId, Is.Empty);
                Assert.That(route.DepartureDockAnchor, Is.Empty);
                Assert.That(route.AnchorPointAnchor, Is.Empty);
            }
        }

        [Test]
        public void Default_HasNoneClass_NullIds_ZeroSeconds()
        {
            var route = default(DiveRouteDefinition);

            Assert.That(route.RequiredVehicleClass, Is.EqualTo(VehicleClass.None));
            Assert.That(route.RouteId, Is.Null, "default skips the constructor, so nothing folds null to empty");
            Assert.That(route.DepartureDockAnchor, Is.Null);
            Assert.That(route.AnchorPointAnchor, Is.Null);
            Assert.That(route.OutboundSeconds, Is.EqualTo(0f));
            Assert.That(route.InboundSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void RouteIds_AreStableKebabCase_AndDistinct()
        {
            Assert.That(BoatTripIds.NearRouteId, Is.EqualTo("route-near-1"));
            Assert.That(BoatTripIds.ReefRouteId, Is.EqualTo("route-reef-1"));
            Assert.That(BoatTripIds.DeepRouteId, Is.EqualTo("route-deep-1"));
            Assert.That(new[] { BoatTripIds.NearRouteId, BoatTripIds.ReefRouteId, BoatTripIds.DeepRouteId }, Is.Unique);
        }
    }
}
