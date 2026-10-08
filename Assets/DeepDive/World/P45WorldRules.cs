using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    public enum P45WeatherKind : byte { Calm = 0, Windy = 1 }

    // Pure daily rules: the campaign's already-persisted WeatherSeed/DayNumber is the ONLY
    // random input. No client roll, no DateTime, and no second save field.
    public static class P45WorldRules
    {
        public const string NightActiveSpeciesId = "sea_bass";
        public const float NightSwimMultiplier = 1.45f;
        public const float CurrentRadius01 = 0.13f;
        public static readonly Vector2 CurrentMapPosition = new Vector2(0.72f, 0.55f);

        public static P45WeatherKind Weather(int dayNumber, int weatherSeed)
        {
            unchecked
            {
                var value = (uint)weatherSeed ^ ((uint)dayNumber * 0x9e3779b9u);
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return (value & 1u) == 0 ? P45WeatherKind.Calm : P45WeatherKind.Windy;
            }
        }

        // Only the deep/outlying departure is restricted. Inbound travel and any return
        // request do not consult this function, including when the weather turns windy.
        public static bool CanDepart(string routeId, P45WeatherKind weather) =>
            !string.Equals(routeId, BoatTripIds.DeepRouteId, StringComparison.Ordinal) ||
            weather != P45WeatherKind.Windy;

        public static CrewEnvironmentAdvisory Advisory(P45WeatherKind weather) =>
            weather == P45WeatherKind.Windy
                ? new CrewEnvironmentAdvisory(CrewWarningKind.Windy, true, true)
                : CrewEnvironmentAdvisory.Clear;

        public static bool IsNight(int clockMinute, DayPhase phase) =>
            DaylightModel.Evaluate(clockMinute, phase, 0).NightFactor >= 0.80f;

        public static float SpeciesActivity(string speciesId, int clockMinute, DayPhase phase) =>
            string.Equals(speciesId, NightActiveSpeciesId, StringComparison.Ordinal) && IsNight(clockMinute, phase)
                ? NightSwimMultiplier : 1f;

        // The local current is a *marked area* inside the same authored map region.
        // Only X/Z affect inclusion; the caller checks actual water depth, never land.
        public static bool TryCurrent(DiveRegionBounds bounds, Vector3 world, bool underwater,
            P45WeatherKind weather, out CrewCurrentSample sample)
        {
            sample = default;
            if (!underwater || !bounds.TryWorldToMap(world, out var map)) return false;
            var offset = map - CurrentMapPosition;
            if (offset.sqrMagnitude > CurrentRadius01 * CurrentRadius01) return false;
            var speed = weather == P45WeatherKind.Windy ? 0.75f : 0.42f;
            sample = new CrewCurrentSample(new Vector3(speed, 0f, speed * 0.25f), true);
            return true;
        }
    }

    // Mert's daily objective generator consumes a World-validated reachability verdict,
    // never a fish transform or a hidden cell location. A target must be an authored
    // species and its required band/route must be reachable with today's active fleet.
    public readonly struct P45ObjectiveTarget
    {
        public readonly string SpeciesId;
        public readonly string HabitatBandId;
        public readonly string RouteId;
        public readonly bool RequiresNight;

        public P45ObjectiveTarget(string speciesId, string habitatBandId, string routeId, bool requiresNight)
        {
            SpeciesId = speciesId;
            HabitatBandId = habitatBandId;
            RouteId = routeId;
            RequiresNight = requiresNight;
        }
    }

    public static class P45WorldObjectiveEligibility
    {
        private static bool ContainsSpecies(IReadOnlyCollection<string> species, string id)
        {
            if (species == null) return false;
            foreach (var value in species)
                if (string.Equals(value, id, StringComparison.Ordinal)) return true;
            return false;
        }

        public static bool CanOffer(in P45ObjectiveTarget target, IReadOnlyCollection<string> authoredSpecies,
            VehicleClass activeClass, bool hasReefDiscovery, P45WeatherKind weather)
        {
            if (string.IsNullOrWhiteSpace(target.SpeciesId) || authoredSpecies == null ||
                !ContainsSpecies(authoredSpecies, target.SpeciesId)) return false;

            // Night is reached naturally during a playable day (19:30-00:00).
            // Nothing here unlocks a route or publishes the subject's current position.
            switch (target.HabitatBandId)
            {
                case DepthBandIds.Shallow:
                    if (target.RouteId != BoatTripIds.NearRouteId) return false;
                    return activeClass == VehicleClass.Rowboat ||
                           activeClass == VehicleClass.Motorboat ||
                           activeClass == VehicleClass.ResearchBoat;
                case DepthBandIds.Reef:
                    if (target.RouteId != BoatTripIds.ReefRouteId) return false;
                    return activeClass == VehicleClass.Motorboat ||
                           activeClass == VehicleClass.ResearchBoat;
                case DepthBandIds.Deep:
                    return target.RouteId == BoatTripIds.DeepRouteId &&
                           activeClass == VehicleClass.ResearchBoat &&
                           hasReefDiscovery && P45WorldRules.CanDepart(target.RouteId, weather);
                default:
                    return false;
            }
        }
    }
}
