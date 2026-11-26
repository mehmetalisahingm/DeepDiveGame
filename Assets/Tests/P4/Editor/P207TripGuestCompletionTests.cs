using DeepDive.P1.Lab;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P207TripGuestCompletionTests
    {
        [Test]
        public void FourHomeReturnSlotsDoNotParkOnTheSharedApproachLane()
        {
            for (var slot = 0; slot < 4; slot++)
            {
                var stop = ContinuousWorldSmokeDriver.HomeReturnStop(slot);
                var approach = ContinuousWorldSmokeDriver.HomeReturnApproach(slot);
                Assert.AreEqual(stop.x, approach.x, .001f);
                Assert.AreEqual(1.6f, approach.z - stop.z, .001f);
                for (var other = 0; other < 4; other++)
                {
                    var occupied = ContinuousWorldSmokeDriver.HomeReturnStop(other);
                    Assert.GreaterOrEqual(UnityEngine.Vector3.Distance(occupied, approach), 1.59f);
                }
            }
        }

        [TestCase(true, true, 1, 0, true)]
        [TestCase(true, true, 2, 0, false)]
        [TestCase(true, true, 2, 1, true)]
        [TestCase(true, true, 4, 2, false)]
        [TestCase(true, true, 4, 3, true)]
        [TestCase(true, false, 2, 1, false)]
        [TestCase(false, true, 2, 1, false)]
        [TestCase(true, true, 0, 0, false)]
        public void HostWaitsUntilEveryActualGuestObservedEmptyBoat(
            bool hostDone, bool saved, int players, int guestsConfirmed, bool expected)
        {
            Assert.AreEqual(expected,
                IntegratedSmokeDriver.CanAdvanceTripLobby(hostDone, saved, players, guestsConfirmed));
        }
    }
}
