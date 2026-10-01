using System;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // Physical seat placement owned by Mehmet's player/vehicle layer. Boat identity, ownership,
    // prices and active-vehicle selection stay outside this rule. The same stable four seat ids
    // can therefore be applied to rowboat, motorboat and research-boat hull dimensions.
    public static class BoatHullSeatRules
    {
        private const float HalfWidthFraction = 0.22916667f; // 2.4m rowboat -> 0.55m
        private const float AftLengthFraction = 0.18f;       // 5m rowboat -> -0.90m
        private const float ForeLengthFraction = 0.16f;      // 5m rowboat -> +0.80m

        public static bool TryResolveLocalOffset(float hullWidth, float hullLength, float seatHeight,
            string seatId, out Vector3 offset)
        {
            offset = Vector3.zero;
            if (!FinitePositive(hullWidth) || !FinitePositive(hullLength) || !FiniteNonNegative(seatHeight) ||
                !BoatTripIds.IsSeat(seatId))
                return false;

            var x = hullWidth * HalfWidthFraction;
            var aftZ = -hullLength * AftLengthFraction;
            var foreZ = hullLength * ForeLengthFraction;

            if (string.Equals(seatId, BoatTripIds.Seat0, StringComparison.Ordinal))
                offset = new Vector3(-x, seatHeight, aftZ);
            else if (string.Equals(seatId, BoatTripIds.Seat1, StringComparison.Ordinal))
                offset = new Vector3(x, seatHeight, aftZ);
            else if (string.Equals(seatId, BoatTripIds.Seat2, StringComparison.Ordinal))
                offset = new Vector3(-x, seatHeight, foreZ);
            else if (string.Equals(seatId, BoatTripIds.Seat3, StringComparison.Ordinal))
                offset = new Vector3(x, seatHeight, foreZ);
            else
                return false;

            return true;
        }

        private static bool FinitePositive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private static bool FiniteNonNegative(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
