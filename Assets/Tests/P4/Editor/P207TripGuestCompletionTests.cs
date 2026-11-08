using DeepDive.P1.Lab;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class P207TripGuestCompletionTests
    {
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
