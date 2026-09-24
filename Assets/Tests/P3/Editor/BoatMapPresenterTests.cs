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
            Assert.AreEqual(3, icons.Count, "dock, boat at the anchor, and the return marker");
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

        private static readonly PlayerId Bob = new PlayerId(2);

        private static (PlayerId Player, float X, float Z)[] Party() => new[]
        {
            (Alice, 0.5f, 0.5f), (Bob, 0.7f, 0.3f)
        };

        [Test]
        public void ConnectedPartyAppearsAsOneIconPerPlayerAtTheirApprovedPosition()
        {
            var icons = BoatMapPresenter.BuildIcons(
                BoatTripPhase.Docked, BoatTripIds.BoatId, Dock, Anchor, null, Party(), false);
            Assert.AreEqual(4, icons.Count, "dock, boat, two players");
            Assert.AreEqual(BoatMapPresenter.PlayerIconId(Alice), icons[2].IconId);
            Assert.AreEqual((0.5f, 0.5f), (icons[2].MapX, icons[2].MapZ));
            Assert.AreEqual(BoatMapPresenter.PlayerIconId(Bob), icons[3].IconId);
        }

        [Test]
        public void AbsentPlayersAreNotDrawn()
        {
            var icons = BoatMapPresenter.BuildIcons(
                BoatTripPhase.Docked, BoatTripIds.BoatId, Dock, Anchor, null,
                new[] { (Alice, 0.5f, 0.5f) }, false);
            Assert.AreEqual(3, icons.Count);
        }

        [Test]
        public void ReturnMarkerAppearsOnlyWhileAnchoredAndNotAboard()
        {
            bool HasMarker(BoatTripPhase phase, bool aboard, (float X, float Z)? live = null)
            {
                foreach (var icon in BoatMapPresenter.BuildIcons(phase, BoatTripIds.BoatId, Dock, Anchor, live, null, aboard))
                    if (icon.IconId == BoatMapPresenter.ReturnMarkerIconId) return true;
                return false;
            }

            Assert.IsTrue(HasMarker(BoatTripPhase.Anchored, false));
            Assert.IsFalse(HasMarker(BoatTripPhase.Anchored, true), "already aboard - nothing to swim back to");
            Assert.IsFalse(HasMarker(BoatTripPhase.Docked, false), "the dock icon is the destination");
            Assert.IsFalse(HasMarker(BoatTripPhase.Outbound, false, (0.4f, 0.6f)), "a fixed marker underway would point at where the boat was");
            Assert.IsFalse(HasMarker(BoatTripPhase.Inbound, false, (0.4f, 0.6f)));
        }

        [Test]
        public void ReturnMarkerSitsOnTheBoatAndIsAbsentWhenTheAnchorIsUnknown()
        {
            var icons = BoatMapPresenter.BuildIcons(
                BoatTripPhase.Anchored, BoatTripIds.BoatId, Dock, Anchor, null, null, false);
            var marker = icons[icons.Count - 1];
            Assert.AreEqual(BoatMapPresenter.ReturnMarkerIconId, marker.IconId);
            Assert.AreEqual(Anchor, (marker.MapX, marker.MapZ));

            var noAnchor = BoatMapPresenter.BuildIcons(
                BoatTripPhase.Anchored, BoatTripIds.BoatId, Dock, null, null, null, false);
            Assert.AreEqual(1, noAnchor.Count, "no anchor data: no boat and no marker, never a guess");
        }
    }
}
