using DeepDive.Core.Contracts;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class BoatTripRouteEligibilityTests
    {
        private sealed class Roster : BoatTripManager.ISessionRoster
        {
            public bool IsConnected(PlayerId player) => true;
        }

        private sealed class Routes : IBoatTripRouteCatalog
        {
            public bool TryGetDefinition(string routeId, out DiveRouteDefinition definition)
            {
                if (routeId == BoatTripIds.NearRouteId)
                {
                    definition = new DiveRouteDefinition(routeId, "dock", "near", 10f, 10f, VehicleClass.Rowboat);
                    return true;
                }
                if (routeId == BoatTripIds.ReefRouteId)
                {
                    definition = new DiveRouteDefinition(routeId, "dock", "reef", 28f, 28f, VehicleClass.Motorboat);
                    return true;
                }
                if (routeId == BoatTripIds.DeepRouteId)
                {
                    definition = new DiveRouteDefinition(routeId, "dock", "deep", 52f, 52f, VehicleClass.ResearchBoat);
                    return true;
                }
                definition = default;
                return false;
            }
        }

        private static readonly PlayerId Host = new PlayerId(0);
        private GameObject root;
        private BoatTripManager trip;
        private string activeBoat;
        private ulong request;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("route-eligibility");
            trip = root.AddComponent<BoatTripManager>();
            request = 1;
        }

        [TearDown]
        public void Cleanup()
        {
            if (trip != null) trip.Shutdown();
            Object.DestroyImmediate(root);
        }

        [TestCase(VehicleIds.Rowboat, BoatTripIds.ReefRouteId, false)]
        [TestCase(VehicleIds.Motorboat, BoatTripIds.ReefRouteId, true)]
        [TestCase(VehicleIds.Motorboat, BoatTripIds.DeepRouteId, false)]
        [TestCase(VehicleIds.ResearchBoat, BoatTripIds.DeepRouteId, true)]
        public void ActiveVehicleMustMeetTheWorldRoutesRequiredClass(string boatId, string routeId, bool accepted)
        {
            activeBoat = boatId;
            trip.Configure(() => BoatRepairStatus.Repaired, new Roster(), () => activeBoat, new Routes());
            Assert.IsTrue(BoatBoarding.TryBoard(Host, boatId, BoatTripIds.Seat0, request++).Accepted);

            var result = trip.TryStartRoute(Host, routeId, request++);
            Assert.AreEqual(accepted, result.Accepted);
            if (accepted)
            {
                Assert.AreEqual(routeId, trip.State.RouteId);
                Assert.AreEqual(BoatTripPhase.Outbound, trip.State.Phase);
            }
            else
            {
                Assert.AreEqual("RequirementMissing", result.ReasonCode);
                Assert.AreEqual(BoatTripPhase.Docked, trip.State.Phase);
            }
        }

        [Test]
        public void UnknownWorldRouteIsRejectedWithoutDuplicatingARouteTableInTrip()
        {
            activeBoat = VehicleIds.ResearchBoat;
            trip.Configure(() => BoatRepairStatus.Repaired, new Roster(), () => activeBoat, new Routes());
            Assert.IsTrue(BoatBoarding.TryBoard(Host, activeBoat, BoatTripIds.Seat0, request++).Accepted);

            var result = trip.TryStartRoute(Host, "route-does-not-exist", request++);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("InvalidTarget", result.ReasonCode);
        }
    }
}
