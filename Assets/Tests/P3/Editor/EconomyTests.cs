using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Session;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    public class EconomyTests
    {
        private GameObject root;
        private SessionManager session;
        private InventoryManager inventory;
        private EconomyManager economy;
        private PlayerId alice, bob;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("economy-test");
            session = root.AddComponent<SessionManager>();
            inventory = root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            alice = new PlayerId(1);
            bob = new PlayerId(2);

            session.Initialize("room", "DiveTestArea");
            session.Join(alice);
            session.Join(bob);
            economy.SetPrice("fish-1", 50);
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        private void EnterDive(string diveId)
        {
            session.SetReady(alice, true);
            session.SetReady(bob, true);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.Ok, session.BeginDive(diveId));
        }

        private static CaptureResult Capture(string id, string diveId, int weightGrams, string speciesId = "fish-1") =>
            new CaptureResult(id, diveId, speciesId, weightGrams, catchObjectId: 1);

        [Test]
        public void SellingPreservedCatchesPaysSetPrice()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);

            var summary = new DiveSummary("dive-1",
                new[] { alice }, new[] { "c1" }, new string[0], "cp-1");

            Assert.AreEqual(50, economy.SellPreservedCatches(summary));
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void SellingTheSameSummaryTwiceDoesNotPayTwice()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);

            var summary = new DiveSummary("dive-1",
                new[] { alice }, new[] { "c1" }, new string[0], "cp-1");

            Assert.AreEqual(50, economy.SellPreservedCatches(summary));
            Assert.AreEqual(0, economy.SellPreservedCatches(summary));
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void LostCaptureIdsAreNeverPaidEvenIfPassedIn()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 300));
            // bob never marks safe; c2 goes to lost per real FinalizeDive, but even if a caller
            // mistakenly passed it as preserved, an untracked/never-added id pays nothing here
            // because pricing always requires a real captured item.
            var summary = new DiveSummary("dive-1", new PlayerId[0], new string[0], new[] { "c2" }, "cp-1");
            Assert.AreEqual(0, economy.SellPreservedCatches(summary));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void RealDiveSummaryFromInventoryManagerPaysOnlySafeReturnedCatches()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 300));
            inventory.TryMarkSafeReturn(alice); // bob never makes it out

            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());

            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void PurchaseDebitsBalanceAndGrantsLoadout()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);
            session.BeginReturn(); // funds the shared balance to 50

            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            var result = economy.TryPurchase(alice, "tube-1", requestId: 1);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(20, economy.SharedBalance);
            CollectionAssert.Contains(economy.LoadoutFor(alice), "tube-1");
        }

        [Test]
        public void PurchaseRejectedWhenBalanceIsInsufficient()
        {
            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            var result = economy.TryPurchase(alice, "tube-1", requestId: 1);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("InsufficientFunds", result.ReasonCode);
            Assert.AreEqual(0, economy.SharedBalance);
            CollectionAssert.IsEmpty(economy.LoadoutFor(alice));
        }

        [Test]
        public void PurchaseRejectedForUnknownEquipment()
        {
            var result = economy.TryPurchase(alice, "does-not-exist", requestId: 1);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("InvalidTarget", result.ReasonCode);
        }

        [Test]
        public void DuplicateRequestIdReplaysTheSameResultInsteadOfChargingAgain()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);
            session.BeginReturn();

            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            var first = economy.TryPurchase(alice, "tube-1", requestId: 7);
            var replay = economy.TryPurchase(alice, "tube-1", requestId: 7);

            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(first.Revision, replay.Revision);
            Assert.AreEqual(20, economy.SharedBalance);
        }

        [Test]
        public void TwoDifferentPlayersReusingTheSameRequestIdAreProcessedIndependently()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);
            inventory.TryMarkSafeReturn(bob);
            session.BeginReturn(); // two fish-1 captures sold: balance 100

            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            // requestId is generated per-player, so both legitimately start at 1 (Mehmet's
            // review on #37) - keying replay tracking on requestId alone would let bob's
            // purchase silently replay alice's stale result instead of being charged.
            var aliceResult = economy.TryPurchase(alice, "tube-1", requestId: 1);
            var bobResult = economy.TryPurchase(bob, "tube-1", requestId: 1);

            Assert.IsTrue(aliceResult.Accepted);
            Assert.IsTrue(bobResult.Accepted);
            Assert.AreNotEqual(aliceResult.Revision, bobResult.Revision);
            Assert.AreEqual(40, economy.SharedBalance);
            CollectionAssert.Contains(economy.LoadoutFor(alice), "tube-1");
            CollectionAssert.Contains(economy.LoadoutFor(bob), "tube-1");
        }

        [Test]
        public void PurchasingTheSameEquipmentTwiceWithANewRequestIdIsRejectedAsAlreadyProcessed()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 1000));
            inventory.TryMarkSafeReturn(alice);
            session.BeginReturn(); // one fish-1 capture sold at the flat per-capture price: balance 50

            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", requestId: 1).Accepted);
            var second = economy.TryPurchase(alice, "tube-1", requestId: 2);

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual("AlreadyProcessed", second.ReasonCode);
            Assert.AreEqual(20, economy.SharedBalance);
        }
    }
}
