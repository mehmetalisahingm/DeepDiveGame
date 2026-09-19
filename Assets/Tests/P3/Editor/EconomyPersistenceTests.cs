using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    public class EconomyPersistenceTests
    {
        private GameObject root;
        private EconomyManager economy;
        private PlayerId alice;
        private ulong nextRequest = 1000;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("economy-persistence-test");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            alice = new PlayerId(1);
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        private static RecordingResult Recording(string id, int quality) =>
            new RecordingResult(id, "dive-1", new PlayerId(1), "sea_bass", quality, 4f);

        // A recording earns nothing by itself; queue it, then hand it in at the recording buyer.
        private TurnInResult QueueAndTurnIn(RecordingResult recording, PlayerId? by = null)
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryQueueRecordingTurnIn(recording));
            return economy.TryTurnInRecordings(by ?? recording.PlayerId, nextRequest++);
        }

        [Test]
        public void UnknownSubjectIsRejectedAndEventHasExplicitPrice()
        {
            Assert.AreEqual(PlayerActionResult.Rejected, economy.TryQueueRecordingTurnIn(
                new RecordingResult("unknown", "dive-1", alice, "not-priced", 3, 4)));
            Assert.AreEqual(0, economy.PendingCountFor(alice, TurnInKind.Recording));

            var paid = QueueAndTurnIn(new RecordingResult("event", "dive-1", alice, "event_bioluminescence", 3, 4));
            Assert.AreEqual(100, paid.Earned);
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void RecordingQualityTablePaysBronzeThroughPlatinum()
        {
            Assert.AreEqual(25, economy.RecordingRewardFor(1));
            Assert.AreEqual(50, economy.RecordingRewardFor(2));
            Assert.AreEqual(100, economy.RecordingRewardFor(3));
            Assert.AreEqual(200, economy.RecordingRewardFor(4));
        }

        [Test]
        public void QueuingARecordingNeverPaysMoney()
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryQueueRecordingTurnIn(Recording("rec-1", 4)));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void RecordingIdCanNeverPayTwice()
        {
            var recording = Recording("rec-1", 3);
            Assert.AreEqual(100, QueueAndTurnIn(recording).Earned);
            Assert.AreEqual(100, economy.SharedBalance);

            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryQueueRecordingTurnIn(recording));
            Assert.IsFalse(economy.TryTurnInRecordings(alice, nextRequest++).Accepted);
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void QueuedRecordingCannotBeQueuedAgain()
        {
            var recording = Recording("rec-dup", 3);
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryQueueRecordingTurnIn(recording));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryQueueRecordingTurnIn(recording));
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Recording));
        }

        [Test]
        public void InvalidOrUnpayableRecordingDoesNotChangeMoney()
        {
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                economy.TryQueueRecordingTurnIn(Recording("rec-zero", 0)));
            Assert.AreEqual(0, economy.SharedBalance);
            Assert.AreEqual(0, economy.PendingCountFor(alice, TurnInKind.Recording));
        }

        [Test]
        public void RestoredSnapshotKeepsMoneyEquipmentAndPaidRecordingIds()
        {
            var recording = Recording("rec-restore", 4);
            Assert.AreEqual(200, QueueAndTurnIn(recording).Earned);
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", 1).Accepted);
            Assert.AreEqual(100, economy.SharedBalance);

            var snapshot = economy.ExportSaveData("campaign-a", "checkpoint-a");

            Object.DestroyImmediate(root);
            root = new GameObject("economy-restored-test");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            Assert.IsTrue(economy.TryRestore(snapshot));

            Assert.AreEqual(100, economy.SharedBalance);
            CollectionAssert.Contains(economy.LoadoutFor(alice), "tube-1");
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryQueueRecordingTurnIn(recording));
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void SharedBalanceNeverGoesNegativeAcrossCompetingPurchases()
        {
            Assert.AreEqual(200, QueueAndTurnIn(Recording("rec-funds", 4)).Earned);
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", 1).Accepted);

            var bob = new PlayerId(2);
            var rejected = economy.TryPurchase(bob, "tube-2", 1);
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual("InsufficientFunds", rejected.ReasonCode);
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void RecordingTurnInRollsBackWhenPersistenceFailsAndCanBeRetriedWithTheSameRequest()
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryQueueRecordingTurnIn(Recording("rec-save-fail", 4)));
            economy.SetPersistenceHandler(() => false);

            var failed = economy.TryTurnInRecordings(alice, 77);
            Assert.IsFalse(failed.Accepted);
            Assert.AreEqual("SaveFailed", failed.ReasonCode);
            Assert.AreEqual(0, economy.SharedBalance, "no money without a durable save");
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Recording), "item must still be handed in later");

            economy.SetPersistenceHandler(null);
            var retried = economy.TryTurnInRecordings(alice, 77);
            Assert.IsTrue(retried.Accepted);
            Assert.AreEqual(200, economy.SharedBalance);
            Assert.AreEqual(0, economy.PendingCountFor(alice, TurnInKind.Recording));
        }

        [Test]
        public void PurchaseReturnsSaveFailedAndRollsBackWhenPersistenceFails()
        {
            Assert.AreEqual(200, QueueAndTurnIn(Recording("rec-buy-funds", 4)).Earned);
            economy.SetPersistenceHandler(() => false);

            var result = economy.TryPurchase(alice, "tube-1", 7);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("SaveFailed", result.ReasonCode);
            Assert.AreEqual(200, economy.SharedBalance);
            CollectionAssert.DoesNotContain(economy.LoadoutFor(alice), "tube-1");
        }
    }
}
