using System;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    public enum BoatHullKind : byte
    {
        Rowboat = 0,
        Motorboat = 1,
        ResearchVessel = 2
    }

    public readonly struct BoatHullDimensions
    {
        public readonly float Width;
        public readonly float Length;
        public readonly float SeatHeight;

        public BoatHullDimensions(float width, float length, float seatHeight)
        {
            Width = width;
            Length = length;
            SeatHeight = seatHeight;
        }
    }

    // Physical seat placement owned by Mehmet's player/vehicle layer. Boat identity, ownership,
    // prices and active-vehicle selection stay outside this rule. The same stable four seat ids
    // can therefore be applied to rowboat, motorboat and research-vessel hull dimensions.
    public static class BoatHullSeatRules
    {
        private const float HalfWidthFraction = 0.22916667f; // 2.4m rowboat -> 0.55m
        private const float AftLengthFraction = 0.18f;       // 5m rowboat -> -0.90m
        private const float ForeLengthFraction = 0.16f;      // 5m rowboat -> +0.80m

        // First-pass physical footprints only. These values do not grant ownership/unlock and do
        // not choose an active vehicle; Mert's progression state supplies the stable boat id.
        public static bool TryGetDimensions(BoatHullKind kind, out BoatHullDimensions dimensions)
        {
            switch (kind)
            {
                case BoatHullKind.Rowboat:
                    dimensions = new BoatHullDimensions(2.4f, 5f, 0.35f);
                    return true;
                case BoatHullKind.Motorboat:
                    dimensions = new BoatHullDimensions(3.2f, 7f, 0.50f);
                    return true;
                case BoatHullKind.ResearchVessel:
                    dimensions = new BoatHullDimensions(4.4f, 10f, 0.65f);
                    return true;
                default:
                    dimensions = default;
                    return false;
            }
        }

        public static bool TryResolveLocalOffset(BoatHullKind kind, string seatId, out Vector3 offset)
        {
            offset = Vector3.zero;
            return TryGetDimensions(kind, out var dimensions) &&
                   TryResolveLocalOffset(dimensions.Width, dimensions.Length, dimensions.SeatHeight, seatId, out offset);
        }

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
