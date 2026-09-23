using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    // Guards issue #66's own scope: BoatTripState/BoatTripManager only. Nothing here exercises
    // Mehmet's seat/movement or Utku's dock/route data - neither exists yet (#75/#76 have no code
    // as of this commit) - so every call goes through BoatBoarding/BoatRouteProgress exactly the
    // way their future real callers will, with a fake repair/roster underneath instead of a scene.
    public class BoatTripTests
    {
        private sealed class FakeRoster : BoatTripManager.ISessionRoster
        {
            private readonly HashSet<ulong> connected = new HashSet<ulong>();
            public void Connect(PlayerId player) => connected.Add(player.Value);
            public void Disconnect(PlayerId player) => connected.Remove(player.Value);
            public bool IsConnected(PlayerId player) => connected.Contains(player.Value);
        }

        private GameObject root;
        private BoatTripManager trip;
        private FakeRoster roster;
        private BoatRepairStatus repairStatus;
        private PlayerId alice, bob, carol, dave;
        private ulong nextRequestId;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("boat-trip-test");
            trip = root.AddComponent<BoatTripManager>();
            roster = new FakeRoster();
            repairStatus = BoatRepairStatus.Repaired;
            trip.Configure(() => repairStatus, roster);
            alice = new PlayerId(1); bob = new PlayerId(2); carol = new PlayerId(3); dave = new PlayerId(4);
            foreach (var p in new[] { alice, bob, carol, dave }) roster.Connect(p);
            nextRequestId = 1;
        }

        [TearDown]
        public void Cleanup()
        {
            if (trip != null) trip.Shutdown();
            if (root != null) Object.DestroyImmediate(root);
        }

        private ulong NextId() => nextRequestId++;

        private TransactionResult Board(PlayerId player, string seatId) =>
            BoatBoarding.TryBoard(player, BoatTripIds.BoatId, seatId, NextId());

        [Test]
        public void BoardingSetsSeatPartyAndFirstBoarderAsOwner()
        {
            var result = Board(alice, BoatTripIds.Seat0);
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(BoatTripPhase.Docked, trip.State.Phase);
            Assert.AreEqual(1, trip.State.Seats.Count);
            Assert.AreEqual(alice, trip.State.Seats[0].Player);
            Assert.IsTrue(trip.State.HasOwner);
            Assert.AreEqual(alice, trip.State.Owner);
        }

        [Test]
        public void BoardingAnUnknownSeatIsRejected()
        {
            var result = Board(alice, "seat-does-not-exist");
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("InvalidTarget", result.ReasonCode);
            Assert.AreEqual(0, trip.State.Seats.Count);
        }

        [Test]
        public void ASeatAlreadyTakenRefusesASecondPlayer()
        {
            Board(alice, BoatTripIds.Seat0);
            var result = Board(bob, BoatTripIds.Seat0);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("AlreadyProcessed", result.ReasonCode);
            Assert.AreEqual(alice, trip.State.Seats[0].Player);
        }

        [Test]
        public void APlayerCannotTakeASecondSeat()
        {
            Board(alice, BoatTripIds.Seat0);
            var result = Board(alice, BoatTripIds.Seat1);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("AlreadyProcessed", result.ReasonCode);
            Assert.AreEqual(1, trip.State.Seats.Count);
        }

        [Test]
        public void AllFourSeatsFillAndAFifthPlayerIsRefused()
        {
            Assert.IsTrue(Board(alice, BoatTripIds.Seat0).Accepted);
            Assert.IsTrue(Board(bob, BoatTripIds.Seat1).Accepted);
            Assert.IsTrue(Board(carol, BoatTripIds.Seat2).Accepted);
            Assert.IsTrue(Board(dave, BoatTripIds.Seat3).Accepted);
            roster.Connect(new PlayerId(5));
            var fifth = BoatBoarding.TryBoard(new PlayerId(5), BoatTripIds.BoatId, BoatTripIds.Seat0, NextId());
            Assert.IsFalse(fifth.Accepted);
            Assert.AreEqual(4, trip.State.Seats.Count);
        }

        [Test]
        public void RepeatingTheSameRequestIdReplaysWithoutASecondEffect()
        {
            var requestId = NextId();
            var first = BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, requestId);
            var second = BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, requestId);
            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(first.Revision, second.Revision);
            Assert.AreEqual(1, trip.State.Seats.Count);
        }

        [Test]
        public void StartingTheRouteRequiresTheOwnerARepairedBoatAndAtLeastOnePassenger()
        {
            var noPassengers = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsFalse(noPassengers.Accepted);
            Assert.AreEqual("NoPassengers", noPassengers.ReasonCode);

            Board(alice, BoatTripIds.Seat0);
            var notOwner = trip.TryStartRoute(bob, BoatTripIds.NearRouteId, NextId());
            Assert.IsFalse(notOwner.Accepted);
            Assert.AreEqual("InvalidState", notOwner.ReasonCode);

            repairStatus = BoatRepairStatus.InProgress;
            var notRepaired = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsFalse(notRepaired.Accepted);
            Assert.AreEqual("BoatNotRepaired", notRepaired.ReasonCode);

            repairStatus = BoatRepairStatus.Repaired;
            var ok = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsTrue(ok.Accepted);
            Assert.AreEqual(BoatTripPhase.Outbound, trip.State.Phase);
            Assert.IsNotEmpty(trip.State.TripId);
        }

        [Test]
        public void BoardingAndDisembarkingAreClosedWhileUnderway()
        {
            Board(alice, BoatTripIds.Seat0);
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.AreEqual(BoatTripPhase.Outbound, trip.State.Phase);

            var boardWhileMoving = Board(bob, BoatTripIds.Seat1);
            Assert.IsFalse(boardWhileMoving.Accepted);
            Assert.AreEqual("WrongPhase", boardWhileMoving.ReasonCode);

            var disembarkWhileMoving = BoatBoarding.TryDisembark(alice, NextId());
            Assert.IsFalse(disembarkWhileMoving.Accepted);
            Assert.AreEqual("WrongPhase", disembarkWhileMoving.ReasonCode);
        }

        [Test]
        public void ArrivalOnlyAdvancesTheMatchingLegAndOnlyForTheRealBoat()
        {
            Board(alice, BoatTripIds.Seat0);
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());

            var wrongBoat = BoatRouteProgress.ReportArrival("not-a-real-boat", BoatTripPhase.Anchored, NextId());
            Assert.IsFalse(wrongBoat.Accepted);
            Assert.AreEqual(BoatTripPhase.Outbound, trip.State.Phase);

            var wrongLeg = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Docked, NextId());
            Assert.IsFalse(wrongLeg.Accepted);
            Assert.AreEqual("WrongPhase", wrongLeg.ReasonCode);

            var anchored = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, NextId());
            Assert.IsTrue(anchored.Accepted);
            Assert.AreEqual(BoatTripPhase.Anchored, trip.State.Phase);
        }

        // The heart of the anchor rules (CONTRACTS point 7): diving off the boat at anchor keeps the
        // trip party intact, re-boarding for the way back works, and only the owner can call it in.
        [Test]
        public void AtAnchorADiverCanLeaveAndReturnWithoutLeavingTheTripParty()
        {
            Board(alice, BoatTripIds.Seat0);
            Board(bob, BoatTripIds.Seat1);
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, NextId());

            var offBoard = BoatBoarding.TryDisembark(bob, NextId());
            Assert.IsTrue(offBoard.Accepted);
            Assert.AreEqual(1, trip.State.Seats.Count); // bob's seat freed, alice (still aboard) remains
            CollectionAssert.Contains(trip.State.Party, bob); // still party at anchor

            var reboard = Board(bob, BoatTripIds.Seat1);
            Assert.IsTrue(reboard.Accepted);

            var notOwner = trip.TryRequestReturn(bob, NextId());
            Assert.IsFalse(notOwner.Accepted);
            Assert.AreEqual("InvalidState", notOwner.ReasonCode);

            var callBack = trip.TryRequestReturn(alice, NextId());
            Assert.IsTrue(callBack.Accepted);
            Assert.AreEqual(BoatTripPhase.Inbound, trip.State.Phase);

            var docked = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Docked, NextId());
            Assert.IsTrue(docked.Accepted);
            Assert.AreEqual(BoatTripPhase.Docked, trip.State.Phase);
            Assert.AreEqual("", trip.State.TripId);
        }

        [Test]
        public void OwnershipPassesToAConnectedPassengerOnDisconnectAndClearsWithNoneLeft()
        {
            Board(alice, BoatTripIds.Seat0);
            Board(bob, BoatTripIds.Seat1);
            Assert.AreEqual(alice, trip.State.Owner);

            roster.Disconnect(alice);
            trip.HandlePlayerDisconnected(alice);
            Assert.IsTrue(trip.State.HasOwner);
            Assert.AreEqual(bob, trip.State.Owner);
            Assert.AreEqual(1, trip.State.Seats.Count); // only bob remains seated, alice's freed

            roster.Disconnect(bob);
            trip.HandlePlayerDisconnected(bob);
            Assert.IsFalse(trip.State.HasOwner);
            Assert.AreEqual(0, trip.State.Party.Count);
        }

        [Test]
        public void AnEmptyAnchoredBoatRecallsItselfOnceAndOnlyOnce()
        {
            Board(alice, BoatTripIds.Seat0);
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, NextId());

            Assert.IsFalse(trip.TryAutoRecallIfEmpty()); // alice is still party

            BoatBoarding.TryDisembark(alice, NextId());
            roster.Disconnect(alice);
            trip.HandlePlayerDisconnected(alice); // leaves the party for good (not just the seat)

            Assert.IsTrue(trip.TryAutoRecallIfEmpty());
            Assert.AreEqual(BoatTripPhase.Inbound, trip.State.Phase);
            Assert.IsFalse(trip.TryAutoRecallIfEmpty()); // already inbound, not anchored: no second call
        }

        [Test]
        public void StartingTheRouteTwiceNeverOpensASecondTrip()
        {
            Board(alice, BoatTripIds.Seat0);
            var first = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            var second = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsTrue(first.Accepted);
            Assert.IsFalse(second.Accepted);
            Assert.AreEqual("WrongPhase", second.ReasonCode);
        }

        [Test]
        public void ResetToDockedMatchesWhatAFreshCampaignLoadMustLookLike()
        {
            Board(alice, BoatTripIds.Seat0);
            Board(bob, BoatTripIds.Seat1);
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, NextId());

            trip.ResetToDocked();

            var state = trip.State;
            Assert.AreEqual(BoatTripPhase.Docked, state.Phase);
            Assert.AreEqual(0, state.Seats.Count);
            Assert.AreEqual(0, state.Party.Count);
            Assert.IsFalse(state.HasOwner);
            Assert.AreEqual("", state.TripId);
        }

        [Test]
        public void UnboundSeamsRefuseRatherThanThrow()
        {
            // Nothing bound yet: this is the state a client (no BoatTripManager) or a scene before
            // Composition wires it would be in. Neither seam may throw or silently no-op-accept.
            trip.Shutdown();

            var board = BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, 1);
            Assert.IsFalse(board.Accepted);
            Assert.AreEqual("InvalidState", board.ReasonCode);

            var arrival = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, 1);
            Assert.IsFalse(arrival.Accepted);
        }
    }
}
