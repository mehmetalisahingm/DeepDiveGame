using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;

namespace DeepDive.P3A.Tests
{
    public sealed class P4VehiclePhysicalProfileTests
    {
        [TestCase(VehicleClass.Rowboat, BoatHullKind.Rowboat, 0f)]
        [TestCase(VehicleClass.Motorboat, BoatHullKind.Motorboat, 1f)]
        [TestCase(VehicleClass.ResearchBoat, BoatHullKind.ResearchVessel, 2.5f)]
        public void SemanticVehicleMapsToOnePhysicalHullAndCanonicalDockOffset(
            VehicleClass vehicleClass, BoatHullKind expectedHull, float expectedOffset)
        {
            Assert.IsTrue(BoatHullSeatRules.TryFromVehicleClass(vehicleClass, out var hull));
            Assert.AreEqual(expectedHull, hull);
            Assert.AreEqual(expectedOffset, BoatHullSeatRules.DockOffsetMeters(hull), 0.0001f);
        }

        [Test]
        public void NoneNeverInventsAPhysicalHull()
        {
            Assert.IsFalse(BoatHullSeatRules.TryFromVehicleClass(VehicleClass.None, out _));
        }
    }
}
