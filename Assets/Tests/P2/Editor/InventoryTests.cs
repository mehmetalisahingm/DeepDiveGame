using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Inventory;
using DeepDive.Session;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P2.Tests
{
    public class InventoryTests
    {
        private GameObject root;
        private SessionManager session;
        private InventoryManager inventory;
        private PlayerId alice, bob;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("inventory-test");
            session = root.AddComponent<SessionManager>();
            inventory = root.AddComponent<InventoryManager>();
            alice = new PlayerId(1);
            bob = new PlayerId(2);

            session.Initialize("room", "DiveTestArea");
            session.Join(alice);
            session.Join(bob);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        private void EnterDive(string diveId)
        {
            session.SetReady(alice, true);
            session.SetReady(bob, true);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.Ok, session.BeginDive(diveId));
        }

        private static CaptureResult Capture(string id, string diveId, int weightGrams) =>
            new CaptureResult(id, diveId, speciesId: "fish-1", weightGrams, catchObjectId: 1);

        [Test]
        public void AddCatchRejectedOutsideDivePhase()
        {
            Assert.AreEqual(InventoryActionResult.WrongPhase,
                inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500)));
        }

        [Test]
        public void AddCatchRejectedForUnknownPlayer()
        {
            EnterDive("dive-1");
            var stranger = new PlayerId(99);
            Assert.AreEqual(InventoryActionResult.PlayerInactive,
                inventory.TryAddCatch(stranger, Capture("c1", "dive-1", 500)));
        }

        [Test]
        public void AddCatchRejectedForStaleDiveId()
        {
            EnterDive("dive-1");
            Assert.AreEqual(InventoryActionResult.InvalidTarget,
                inventory.TryAddCatch(alice, Capture("c1", "old-dive", 500)));
        }

        [Test]
        public void AddCatchSucceedsAndUpdatesWeight()
        {
            EnterDive("dive-1");
            Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500)));
            Assert.AreEqual(500, inventory.Bags[alice].WeightGrams);
            Assert.AreEqual(1, inventory.Bags[alice].Items.Count);
        }

        [Test]
        public void DuplicateCaptureIsRejectedEvenForADifferentPlayer()
        {
            EnterDive("dive-1");
            Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500)));
            Assert.AreEqual(InventoryActionResult.AlreadyClaimed,
                inventory.TryAddCatch(bob, Capture("c1", "dive-1", 500)));
        }

        [Test]
        public void OverCapacityIsRejectedAndCatchStaysClaimable()
        {
            EnterDive("dive-1");
            Assert.AreEqual(InventoryActionResult.Ok,
                inventory.TryAddCatch(alice, Capture("big-1", "dive-1", InventoryManager.CapacityGrams)));
            // The bag is now full; a second catch must not be silently dropped as claimed.
            Assert.AreEqual(InventoryActionResult.InventoryFull,
                inventory.TryAddCatch(alice, Capture("c2", "dive-1", 1)));
            // Same captureId is still pickable by someone with room, since it was never claimed.
            Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(bob, Capture("c2", "dive-1", 1)));
        }

        [Test]
        public void SafeReturnCanOnlyBeMarkedOnceDuringDive()
        {
            EnterDive("dive-1");
            Assert.AreEqual(InventoryActionResult.Ok, inventory.TryMarkSafeReturn(alice));
            Assert.IsTrue(inventory.Bags[alice].SafelyReturned);
            Assert.AreEqual(InventoryActionResult.AlreadyClaimed, inventory.TryMarkSafeReturn(alice));
        }

        [Test]
        public void FinalizeDiveSeparatesSafeFromLostCatchesAndReturnResetsBags()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 300));
            inventory.TryMarkSafeReturn(alice); // bob never makes it out

            DiveSummary? summary = null;
            inventory.OnDiveSummaryReady += s => summary = s;
            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());

            Assert.IsTrue(summary.HasValue);
            Assert.AreEqual(1, summary.Value.SafelyReturned.Count);
            Assert.AreEqual(alice, summary.Value.SafelyReturned[0]);
            CollectionAssert.AreEquivalent(new[] { "c1" }, summary.Value.PreservedCaptureIds);
            CollectionAssert.AreEquivalent(new[] { "c2" }, summary.Value.LostCaptureIds);

            // Bags are still visible during Return (for UI), then cleared once back in Lobby.
            Assert.AreEqual(500, inventory.Bags[alice].WeightGrams);
            Assert.AreEqual(SessionActionResult.Ok, session.CompleteReturn());
            Assert.AreEqual(0, inventory.Bags[alice].WeightGrams);
            Assert.AreEqual(0, inventory.Bags[bob].WeightGrams);
            Assert.IsFalse(inventory.Bags[alice].SafelyReturned);
        }

        [Test]
        public void NextDiveClearsClaimTrackingSoAnOldCaptureIdCanBeReusedByANewDive()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);
            session.BeginReturn();
            session.CompleteReturn();

            session.SetReady(alice, true);
            session.SetReady(bob, true);
            session.BeginPrep();
            session.BeginDive("dive-2");

            // A brand-new dive's catch happens to reuse id "c1" (different real-world catch);
            // it must not be rejected as AlreadyClaimed from the previous dive.
            Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(alice, Capture("c1", "dive-2", 200)));
        }
    }
}
