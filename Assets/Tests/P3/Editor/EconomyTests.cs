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

        // A safe return only queues unpaid pending items; money comes from the fish buyer NPC.
        private void SafeReturnAndSell(PlayerId player, string captureId, int weight = 500)
        {
            inventory.TryAddCatch(player, Capture(captureId, "dive-1", weight));
            inventory.TryMarkSafeReturn(player);
        }

        [Test]
        public void SafeReturnQueuesPendingCatchAndPaysNothing()
        {
            EnterDive("dive-1");
            SafeReturnAndSell(alice, "c1");
            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());

            Assert.AreEqual(0, economy.SharedBalance, "diving must never pay automatically");
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));
            Assert.AreEqual(0, economy.PendingCountFor(bob, TurnInKind.Catch), "another diver cannot hand in alice's catch");
        }

        [Test]
        public void FishBuyerPaysSetPriceOnceForTheCarrier()
        {
            EnterDive("dive-1");
            SafeReturnAndSell(alice, "c1");
            session.BeginReturn();

            var sale = economy.TrySellCatches(alice, requestId: 1);

            Assert.IsTrue(sale.Accepted);
            Assert.AreEqual(50, sale.Earned);
            Assert.AreEqual(1, sale.ItemCount);
            Assert.AreEqual(50, economy.SharedBalance);
            Assert.AreEqual(0, economy.PendingCountFor(alice, TurnInKind.Catch));
        }

        [Test]
        public void SellingAgainWithANewRequestDoesNotPayTwice()
        {
            EnterDive("dive-1");
            SafeReturnAndSell(alice, "c1");
            session.BeginReturn();

            Assert.IsTrue(economy.TrySellCatches(alice, requestId: 1).Accepted);
            var second = economy.TrySellCatches(alice, requestId: 2);

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual("NothingToTurnIn", second.ReasonCode);
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void ReplayedSellRequestReturnsTheFirstResultWithoutPayingAgain()
        {
            EnterDive("dive-1");
            SafeReturnAndSell(alice, "c1");
            session.BeginReturn();

            var first = economy.TrySellCatches(alice, requestId: 5);
            var replay = economy.TrySellCatches(alice, requestId: 5);

            Assert.IsTrue(replay.Accepted);
            Assert.AreEqual(first.Earned, replay.Earned);
            Assert.AreEqual(first.Revision, replay.Revision);
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void LostCatchesNeverBecomePending()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 300));
            // bob never marks a safe return, so c2 is lost by the real FinalizeDive.
            session.BeginReturn();

            Assert.AreEqual(0, economy.PendingCountFor(bob, TurnInKind.Catch));
            Assert.AreEqual(0, economy.TrySellCatches(bob, 1).Earned);
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void OnlySafeReturnedCatchesAreQueued()
        {
            EnterDive("dive-1");
            SafeReturnAndSell(alice, "c1");
            inventory.TryAddCatch(bob, Capture("c2", "dive-1", 300)); // bob never makes it out

            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());

            Assert.AreEqual(1, economy.PendingTurnIns().Count);
            Assert.AreEqual("c1", economy.PendingTurnIns()[0].ItemId);
        }

        [Test]
        public void PurchaseDebitsBalanceAndGrantsLoadout()
        {
            EnterDive("dive-1");
            inventory.TryAddCatch(alice, Capture("c1", "dive-1", 500));
            inventory.TryMarkSafeReturn(alice);
            session.BeginReturn();
            Assert.IsTrue(economy.TrySellCatches(alice, 900).Accepted); // funds the shared balance to 50

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
            economy.TrySellCatches(alice, 900);

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
            session.BeginReturn();
            economy.TrySellCatches(alice, 900);
            economy.TrySellCatches(bob, 900); // two fish-1 captures sold: balance 100

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
            session.BeginReturn();
            economy.TrySellCatches(alice, 900); // one fish-1 capture sold at the flat per-capture price: balance 50

            economy.AddToCatalog(new EquipmentDefinition("tube-1", "tube", 1, 30));
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", requestId: 1).Accepted);
            var second = economy.TryPurchase(alice, "tube-1", requestId: 2);

            Assert.IsFalse(second.Accepted);
            Assert.AreEqual("AlreadyProcessed", second.ReasonCode);
            Assert.AreEqual(20, economy.SharedBalance);
        }
    }
}
