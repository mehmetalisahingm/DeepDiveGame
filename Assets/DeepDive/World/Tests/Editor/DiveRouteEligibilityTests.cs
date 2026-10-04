using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    // P4.3-B2 (#108): activeClass >= RequiredVehicleClass, fail-closed on None, undefined values and default.
    public class DiveRouteEligibilityTests
    {
        private static DiveRouteDefinition Route(string id, VehicleClass required) =>
            new DiveRouteDefinition(id, "dock-town-1", "anchor", 8f, 8f, required);

        [TestCase(VehicleClass.Rowboat, VehicleClass.None, false)]
        [TestCase(VehicleClass.Rowboat, VehicleClass.Rowboat, true)]
        [TestCase(VehicleClass.Rowboat, VehicleClass.Motorboat, true)]
        [TestCase(VehicleClass.Rowboat, VehicleClass.ResearchBoat, true)]
        [TestCase(VehicleClass.Motorboat, VehicleClass.None, false)]
        [TestCase(VehicleClass.Motorboat, VehicleClass.Rowboat, false)]
        [TestCase(VehicleClass.Motorboat, VehicleClass.Motorboat, true)]
        [TestCase(VehicleClass.Motorboat, VehicleClass.ResearchBoat, true)]
        [TestCase(VehicleClass.ResearchBoat, VehicleClass.None, false)]
        [TestCase(VehicleClass.ResearchBoat, VehicleClass.Rowboat, false)]
        [TestCase(VehicleClass.ResearchBoat, VehicleClass.Motorboat, false)]
        [TestCase(VehicleClass.ResearchBoat, VehicleClass.ResearchBoat, true)]
        public void EveryRouteAgainstEveryClass(VehicleClass required, VehicleClass active, bool expected)
        {
            var id = required == VehicleClass.Rowboat ? BoatTripIds.NearRouteId
                : required == VehicleClass.Motorboat ? BoatTripIds.ReefRouteId
                : BoatTripIds.DeepRouteId;

            Assert.That(DiveRouteEligibility.IsEligible(Route(id, required), active), Is.EqualTo(expected));
        }

        [Test]
        public void DefaultDefinitionIsRefusedForEveryVehicle()
        {
            foreach (var active in new[] { VehicleClass.None, VehicleClass.Rowboat, VehicleClass.Motorboat, VehicleClass.ResearchBoat })
                Assert.That(DiveRouteEligibility.IsEligible(default, active), Is.False, active.ToString());
        }

        [Test]
        public void TheLegacyFiveArgumentRouteIsARowboatRoute()
        {
            var legacy = new DiveRouteDefinition(BoatTripIds.NearRouteId, "dock-town-1", "anchor-near-1", 8f, 8f);
            Assert.That(DiveRouteEligibility.IsEligible(legacy, VehicleClass.Rowboat), Is.True);
            Assert.That(DiveRouteEligibility.IsEligible(legacy, VehicleClass.None), Is.False);
        }

        [Test]
        public void UndefinedActiveClassIsRefused()
        {
            // Numerically 9 >= every class; the rule must not read that as "can sail anything".
            Assert.That(DiveRouteEligibility.IsEligible(Route(BoatTripIds.NearRouteId, VehicleClass.Rowboat), (VehicleClass)9), Is.False);
        }

        [Test]
        public void UndefinedRequiredClassIsRefused()
        {
            Assert.That(DiveRouteEligibility.IsEligible(Route("r", (VehicleClass)9), VehicleClass.ResearchBoat), Is.False);
            Assert.That(DiveRouteEligibility.IsEligible(Route("r", VehicleClass.None), VehicleClass.ResearchBoat), Is.False);
        }

        [Test]
        public void EmptyRouteIdIsRefused()
        {
            Assert.That(DiveRouteEligibility.IsEligible(Route("", VehicleClass.Rowboat), VehicleClass.ResearchBoat), Is.False);
            Assert.That(DiveRouteEligibility.IsEligible(Route(null, VehicleClass.Rowboat), VehicleClass.ResearchBoat), Is.False);
        }
    }
}
