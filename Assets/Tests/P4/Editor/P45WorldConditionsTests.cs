using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Trip;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class P45WorldConditionsTests
    {
        private sealed class Roster : BoatTripManager.ISessionRoster
        {
            public bool IsConnected(PlayerId player) => true;
        }

        private sealed class Routes : IBoatTripRouteCatalog
        {
            public bool TryGetDefinition(string routeId, out DiveRouteDefinition route)
            {
                if (routeId == BoatTripIds.DeepRouteId)
                {
                    route = new DiveRouteDefinition(routeId, "dock", "deep", 10f, 10f, VehicleClass.ResearchBoat);
                    return true;
                }
                route = default;
                return false;
            }
        }

        [Test]
        public void SamePersistedSeedAndDayAlwaysProduceSameWeather()
        {
            for (var day = 1; day <= 35; day++)
            for (var seed = -20; seed <= 20; seed++)
                Assert.AreEqual(P45WorldRules.Weather(day, seed), P45WorldRules.Weather(day, seed));

            var calm = 0;
            var windy = 0;
            for (var i = 1; i <= 30; i++)
            {
                if (P45WorldRules.Weather(i, 17) == P45WeatherKind.Windy) windy++;
                else calm++;
            }
            Assert.Greater(calm, 0);
            Assert.Greater(windy, 0);
        }

        [Test]
        public void WindOnlyBlocksNewDeepDepartures_NeverNearOrSafeReturn()
        {
            Assert.IsFalse(P45WorldRules.CanDepart(BoatTripIds.DeepRouteId, P45WeatherKind.Windy));
            Assert.IsTrue(P45WorldRules.CanDepart(BoatTripIds.DeepRouteId, P45WeatherKind.Calm));
            Assert.IsTrue(P45WorldRules.CanDepart(BoatTripIds.NearRouteId, P45WeatherKind.Windy));
            Assert.IsTrue(P45WorldRules.CanDepart(BoatTripIds.ReefRouteId, P45WeatherKind.Windy));

            var advisory = P45WorldRules.Advisory(P45WeatherKind.Windy);
            Assert.IsTrue(advisory.ReturnAllowed);
            Assert.AreEqual(CrewWarningKind.Windy, advisory.Warning);
        }

        [Test]
        public void HostTripWeatherGateRefusesDepartureWithoutChangingTrip()
        {
            var go = new GameObject("weather-trip-test");
            var trip = go.AddComponent<BoatTripManager>();
            var host = new PlayerId(0);
            var number = 1UL;
            try
            {
                trip.Configure(() => BoatRepairStatus.Repaired, new Roster(),
                    () => VehicleIds.ResearchBoat, new Routes(), _ => false);
                Assert.IsTrue(BoatBoarding.TryBoard(host, VehicleIds.ResearchBoat, BoatTripIds.Seat0, number++).Accepted);
                var denied = trip.TryStartRoute(host, BoatTripIds.DeepRouteId, number++);
                Assert.IsFalse(denied.Accepted);
                Assert.AreEqual("WeatherRestricted", denied.ReasonCode);
                Assert.AreEqual(BoatTripPhase.Docked, trip.State.Phase);

                // Reconfigure from the same saved day after conditions change in a separate
                // test scenario; a fresh request is allowed without inventing a new route.
                trip.Configure(() => BoatRepairStatus.Repaired, new Roster(),
                    () => VehicleIds.ResearchBoat, new Routes(), _ => true);
                var accepted = trip.TryStartRoute(host, BoatTripIds.DeepRouteId, number++);
                Assert.IsTrue(accepted.Accepted);
                Assert.AreEqual(BoatTripPhase.Outbound, trip.State.Phase);
            }
            finally
            {
                trip.Shutdown();
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CurrentUsesTheAuthoredRegionAndNeverLeaksToDryLand()
        {
            var bounds = new DiveRegionBounds(-15f, 15f, -15f, 15f);
            Assert.IsTrue(bounds.TryMapToWorld(P45WorldRules.CurrentMapPosition, out var xz));
            var center = new Vector3(xz.x, 2f, xz.y);
            Assert.IsTrue(P45WorldRules.TryCurrent(bounds, center, true, P45WeatherKind.Calm, out var calm));
            Assert.IsTrue(calm.WarnPlayer);
            Assert.Less(calm.DriftMetresPerSecond.magnitude, CrewEnvironmentRules.MaximumCurrentSpeedMetresPerSecond);
            Assert.IsTrue(P45WorldRules.TryCurrent(bounds, center, true, P45WeatherKind.Windy, out var wind));
            Assert.Greater(wind.DriftMetresPerSecond.magnitude, calm.DriftMetresPerSecond.magnitude);
            Assert.IsFalse(P45WorldRules.TryCurrent(bounds, center, false, P45WeatherKind.Windy, out _));
            Assert.IsFalse(P45WorldRules.TryCurrent(bounds, new Vector3(-14f, 2f, -14f), true,
                P45WeatherKind.Windy, out _));
        }

        [Test]
        public void ExistingSeaBassIsMoreActiveAndVisibleOnlyAtNight()
        {
            Assert.IsFalse(P45WorldRules.IsNight(12 * 60, DayPhase.Running));
            Assert.IsTrue(P45WorldRules.IsNight(22 * 60, DayPhase.Running));
            Assert.AreEqual(1f, P45WorldRules.SpeciesActivity("sea_bass", 12 * 60, DayPhase.Running));
            Assert.Greater(P45WorldRules.SpeciesActivity("sea_bass", 22 * 60, DayPhase.Running), 1f);
            Assert.AreEqual(1f, P45WorldRules.SpeciesActivity("other_species", 22 * 60, DayPhase.Running));
        }

        [Test]
        public void DailyObjectivesRejectUnknownOrUnreachableTargetsWithoutFishLocations()
        {
            var species = new HashSet<string> { "sea_bass" };
            var shallow = new P45ObjectiveTarget("sea_bass", DepthBandIds.Shallow, BoatTripIds.NearRouteId, false);
            var deep = new P45ObjectiveTarget("sea_bass", DepthBandIds.Deep, BoatTripIds.DeepRouteId, true);
            var bogus = new P45ObjectiveTarget("unseen_fish", DepthBandIds.Shallow, BoatTripIds.NearRouteId, false);

            Assert.IsTrue(P45WorldObjectiveEligibility.CanOffer(shallow, species, VehicleClass.Rowboat,
                false, P45WeatherKind.Windy));
            Assert.IsFalse(P45WorldObjectiveEligibility.CanOffer(deep, species, VehicleClass.Rowboat,
                true, P45WeatherKind.Calm));
            Assert.IsFalse(P45WorldObjectiveEligibility.CanOffer(deep, species, VehicleClass.ResearchBoat,
                false, P45WeatherKind.Calm));
            Assert.IsFalse(P45WorldObjectiveEligibility.CanOffer(deep, species, VehicleClass.ResearchBoat,
                true, P45WeatherKind.Windy));
            Assert.IsTrue(P45WorldObjectiveEligibility.CanOffer(deep, species, VehicleClass.ResearchBoat,
                true, P45WeatherKind.Calm));
            Assert.IsFalse(P45WorldObjectiveEligibility.CanOffer(bogus, species, VehicleClass.ResearchBoat,
                true, P45WeatherKind.Calm));
        }
    }
}
