using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public class BoatHullSeatRulesTests
    {
        [Test]
        public void ExistingRowboatDimensionsPreserveCurrentFourSeatLayout()
        {
            AssertSeat(BoatTripIds.Seat0, new Vector3(-0.55f, 0.35f, -0.90f));
            AssertSeat(BoatTripIds.Seat1, new Vector3(0.55f, 0.35f, -0.90f));
            AssertSeat(BoatTripIds.Seat2, new Vector3(-0.55f, 0.35f, 0.80f));
            AssertSeat(BoatTripIds.Seat3, new Vector3(0.55f, 0.35f, 0.80f));
        }

        [TestCase(BoatHullKind.Rowboat)]
        [TestCase(BoatHullKind.Motorboat)]
        [TestCase(BoatHullKind.ResearchVessel)]
        public void EveryP4HullKeepsExactlyTheSameFourStableSeatIds(BoatHullKind kind)
        {
            foreach (var seatId in BoatSeatLayoutRules.SeatIds)
            {
                Assert.IsTrue(BoatHullSeatRules.TryResolveLocalOffset(kind, seatId, out var offset));
                Assert.Greater(offset.y, 0f);
            }

            Assert.IsFalse(BoatHullSeatRules.TryResolveLocalOffset(kind, "seat-4", out _));
        }

        [Test]
        public void LargerHullScalesSeatFootprintWithoutChangingStableSeatIds()
        {
            Assert.IsTrue(BoatHullSeatRules.TryResolveLocalOffset(4f, 8f, 0.6f,
                BoatTripIds.Seat0, out var aftLeft));
            Assert.IsTrue(BoatHullSeatRules.TryResolveLocalOffset(4f, 8f, 0.6f,
                BoatTripIds.Seat3, out var foreRight));

            Assert.Less(aftLeft.x, 0f);
            Assert.Less(aftLeft.z, 0f);
            Assert.Greater(foreRight.x, 0f);
            Assert.Greater(foreRight.z, 0f);
            Assert.AreEqual(0.6f, aftLeft.y, 0.0001f);
            Assert.AreEqual(0.6f, foreRight.y, 0.0001f);
        }

        [Test]
        public void P4HullProfilesIncreaseFootprintByVehicleTier()
        {
            Assert.IsTrue(BoatHullSeatRules.TryGetDimensions(BoatHullKind.Rowboat, out var rowboat));
            Assert.IsTrue(BoatHullSeatRules.TryGetDimensions(BoatHullKind.Motorboat, out var motorboat));
            Assert.IsTrue(BoatHullSeatRules.TryGetDimensions(BoatHullKind.ResearchVessel, out var research));

            Assert.Less(rowboat.Width, motorboat.Width);
            Assert.Less(motorboat.Width, research.Width);
            Assert.Less(rowboat.Length, motorboat.Length);
            Assert.Less(motorboat.Length, research.Length);
        }

        [TestCase(0f, 5f, 0.35f)]
        [TestCase(-2f, 5f, 0.35f)]
        [TestCase(2.4f, 0f, 0.35f)]
        [TestCase(2.4f, -5f, 0.35f)]
        [TestCase(2.4f, 5f, -0.1f)]
        public void InvalidHullDimensionsAreRejected(float width, float length, float height)
        {
            Assert.IsFalse(BoatHullSeatRules.TryResolveLocalOffset(width, length, height,
                BoatTripIds.Seat0, out _));
        }

        [Test]
        public void InvalidSeatIdIsRejectedInsteadOfCreatingASecondSeatAuthority()
        {
            Assert.IsFalse(BoatHullSeatRules.TryResolveLocalOffset(2.4f, 5f, 0.35f,
                "seat-4", out _));
        }

        private static void AssertSeat(string seatId, Vector3 expected)
        {
            Assert.IsTrue(BoatHullSeatRules.TryResolveLocalOffset(BoatHullKind.Rowboat, seatId, out var actual));
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }
    }
}
