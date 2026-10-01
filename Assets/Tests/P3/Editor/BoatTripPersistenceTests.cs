using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Session;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    // #66's persistence half, proven against the real save mechanism rather than a fake: repair
    // status is the only thing a trip needs from disk, and it is #62's EconomySaveData that already
    // carries it (BoatPartIds, schema v2). This is a deliberate scope decision, not an oversight -
    // documented in docs/plan/CONTRACTS.md's P3.3-C note - so it is pinned here rather than left
    // implicit: no new save fields exist for BoatTripManager because none are needed for the one
    // near route v1 ships (CONTRACTS: "hareket halindeki sefer restore edilmez", and gating is
    // entirely "is the boat repaired", which #62 already persists). A second route or a trip reward
    // (P4.3) is the trigger to revisit this, not before.
    public class BoatTripPersistenceTests
    {
        private GameObject root;
        private SessionManager session;
        private InventoryManager inventory;
        private EconomyManager economy;
        private BoatTripManager trip;
        private PlayerId alice;
        private ulong nextRequestId;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("boat-trip-persistence-test");
            session = root.AddComponent<SessionManager>();
            inventory = root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            trip = root.AddComponent<BoatTripManager>();
            trip.Configure(() => economy.BoatRepair.Status, null);
            alice = new PlayerId(1);
            session.Initialize("room", "DiveTestArea");
            session.Join(alice);
            nextRequestId = 1;
        }

        [TearDown]
        public void Cleanup()
        {
            trip.Shutdown();
            Object.DestroyImmediate(root);
        }

        private ulong NextId() => nextRequestId++;

        private void RepairTheBoat()
        {
            foreach (var part in BoatRepairParts.All)
                Assert.IsTrue(economy.TryContributeBoatPart(alice, part, BoatPartSource.Found, NextId()).Accepted);
        }

        [Test]
        public void RouteStartTracksLiveRepairStatusWithoutItsOwnGate()
        {
            BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, NextId());

            var beforeRepair = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsFalse(beforeRepair.Accepted);
            Assert.AreEqual("BoatNotRepaired", beforeRepair.ReasonCode);

            RepairTheBoat();

            var afterRepair = trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            Assert.IsTrue(afterRepair.Accepted);
        }

        // The real round trip: export through EconomyManager.ExportSaveData, restore into a FRESH
        // EconomyManager + BoatTripManager pair (a new campaign load), and check the gate a second
        // time - not a mock of "repaired", the actual persisted JSON-shaped data.
        [Test]
        public void ARestoredRepairedBoatUnlocksTheRouteOnAFreshLoad()
        {
            RepairTheBoat();
            var saved = economy.ExportSaveData("campaign-1", "checkpoint-1");
            Assert.Contains(BoatRepairParts.Hull, saved.BoatPartIds);
            Assert.Contains(BoatRepairParts.Engine, saved.BoatPartIds);
            Assert.Contains(BoatRepairParts.FuelTank, saved.BoatPartIds);

            var freshRoot = new GameObject("fresh-load");
            try
            {
                var freshSession = freshRoot.AddComponent<SessionManager>();
                var freshInventory = freshRoot.AddComponent<InventoryManager>();
                var freshEconomy = freshRoot.AddComponent<EconomyManager>();
                var freshTrip = freshRoot.AddComponent<BoatTripManager>();
                freshTrip.Configure(() => freshEconomy.BoatRepair.Status, null);
                freshSession.Initialize("room", "DiveTestArea");
                freshSession.Join(alice);

                Assert.IsTrue(freshEconomy.TryRestore(saved));
                Assert.AreEqual(BoatRepairStatus.Repaired, freshEconomy.BoatRepair.Status);

                BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, NextId());
                var start = freshTrip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
                Assert.IsTrue(start.Accepted, "a boat repaired before the save must still be repaired after loading it");

                freshTrip.Shutdown();
            }
            finally { Object.DestroyImmediate(freshRoot); }
        }

        // CONTRACTS: a trip under way is never restored - the campaign always opens with the boat
        // docked and every seat empty, regardless of what was happening when the game closed. Proven
        // by never having anywhere to put trip state in EconomySaveData at all: a fresh instance (what
        // loading a campaign actually produces, since BoatTripManager keeps no save file of its own)
        // is Docked/empty by construction, whatever the OLD instance's live phase was.
        [Test]
        public void ATripInProgressWhenTheGameClosesIsNeverRestoredAsInProgress()
        {
            RepairTheBoat();
            BoatBoarding.TryBoard(alice, BoatTripIds.BoatId, BoatTripIds.Seat0, NextId());
            trip.TryStartRoute(alice, BoatTripIds.NearRouteId, NextId());
            BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, BoatTripPhase.Anchored, NextId());
            Assert.AreEqual(BoatTripPhase.Anchored, trip.State.Phase, "sanity: the old instance really was mid-trip");

            var freshRoot = new GameObject("fresh-load-2");
            try
            {
                var freshTrip = freshRoot.AddComponent<BoatTripManager>();
                freshTrip.Configure(() => BoatRepairStatus.Repaired, null);
                var state = freshTrip.State;
                Assert.AreEqual(BoatTripPhase.Docked, state.Phase);
                Assert.AreEqual(0, state.Seats.Count);
                Assert.AreEqual(0, state.Party.Count);
                freshTrip.Shutdown();
            }
            finally { Object.DestroyImmediate(freshRoot); }
        }
    }
}
