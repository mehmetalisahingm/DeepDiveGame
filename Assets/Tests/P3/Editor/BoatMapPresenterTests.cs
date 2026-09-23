using DeepDive.Core.Contracts;
using DeepDive.Trip;
using NUnit.Framework;

namespace DeepDive.P3.Tests
{
    // #66's map layer: pure logic, fed by test doubles standing in for Utku's dock/anchor data and
    // Mehmet's live-position publishing - neither exists in the repo yet (#75/#76 have no code), so
    // nothing here queries a real scene object. That is the point: BuildIcons is usable and fully
    // provable today, and ready to be fed real anchor/live data the moment either seam lands.
    public class BoatMapPresenterTests
    {
        private static readonly PlayerId Alice = new PlayerId(1);
        private static readonly (float X, float Z) Dock = (0.2f, 0.8f);
        private static readonly (float X, float Z) Anchor = (0.6f, 0.4f);

        private static BoatTripState StateAt(BoatTripPhase phase) => new BoatTripState(
            BoatTripIds.BoatId, "trip-1", BoatTripIds.NearRouteId, phase,
            new[] { new BoatSeatAssignment(BoatTripIds.Seat0, Alice) },
            new[] { Alice }, Alice, true, 1);

        [Test]
        public void DockedShowsTheDockAndTheBoatAtTheDock()
        {
            var icons = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Docked), Dock, Anchor, null);
            Assert.AreEqual(2, icons.Count);
            Assert.AreEqual(BoatMapPresenter.DockIconId, icons[0].IconId);
            Assert.AreEqual(Dock, (icons[0].MapX, icons[0].MapZ));
            Assert.AreEqual(BoatTripIds.BoatId, icons[1].IconId);
            Assert.AreEqual(Dock, (icons[1].MapX, icons[1].MapZ));
        }

        [Test]
        public void AnchoredShowsTheBoatAtTheAnchorWhenUtkusDataIsKnown()
        {
            var icons = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Anchored), Dock, Anchor, null);
            Assert.AreEqual(2, icons.Count);
            Assert.AreEqual(Anchor, (icons[1].MapX, icons[1].MapZ));
        }

        [Test]
        public void AnchoredWithNoAnchorDataYetHidesTheBoatRatherThanGuessing()
        {
            var icons = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Anchored), Dock, null, null);
            Assert.AreEqual(1, icons.Count, "only the dock - no invented boat position");
            Assert.AreEqual(BoatMapPresenter.DockIconId, icons[0].IconId);
        }

        [Test]
        public void UnderwayShowsTheBoatOnlyAtMehmetsApprovedLivePosition()
        {
            var withLive = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Outbound), Dock, Anchor, (0.4f, 0.6f));
            Assert.AreEqual(2, withLive.Count);
            Assert.AreEqual((0.4f, 0.6f), (withLive[1].MapX, withLive[1].MapZ));

            var withoutLive = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Inbound), Dock, Anchor, null);
            Assert.AreEqual(1, withoutLive.Count, "no approved position published yet - not interpolated, not shown");
        }

        [Test]
        public void NeverExposesAnythingBeyondTheDockAndTheBoatItself()
        {
            // The party is on the map only as "the boat is there"; nothing here ever emits a
            // per-player icon id, a fish, or any other hidden-world identity.
            var icons = BoatMapPresenter.BuildIcons(StateAt(BoatTripPhase.Docked), Dock, Anchor, null);
            foreach (var icon in icons)
                Assert.IsTrue(icon.IconId == BoatMapPresenter.DockIconId || icon.IconId == BoatTripIds.BoatId, icon.IconId);
        }
    }
}
