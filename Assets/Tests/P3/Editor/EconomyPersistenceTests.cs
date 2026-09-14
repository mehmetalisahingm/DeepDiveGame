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

        [Test] public void UnknownSubjectIsRejectedAndEventHasExplicitPrice()
        {
            Assert.AreEqual(PlayerActionResult.Rejected, economy.TryRewardRecording(
                new RecordingResult("unknown", "dive-1", alice, "not-priced", 3, 4)));
            Assert.AreEqual(0, economy.SharedBalance);
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(
                new RecordingResult("event", "dive-1", alice, "event_bioluminescence", 3, 4)));
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
        public void RecordingIdCanNeverPayTwice()
        {
            var recording = Recording("rec-1", 3);
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(recording));
            Assert.AreEqual(100, economy.SharedBalance);

            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryRewardRecording(recording));
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void InvalidOrUnpayableRecordingDoesNotChangeMoney()
        {
            Assert.AreEqual(PlayerActionResult.InvalidTarget,
                economy.TryRewardRecording(Recording("rec-zero", 0)));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void RestoredSnapshotKeepsMoneyEquipmentAndPaidRecordingIds()
        {
            var recording = Recording("rec-restore", 4);
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(recording));
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
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryRewardRecording(recording));
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void SharedBalanceNeverGoesNegativeAcrossCompetingPurchases()
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(Recording("rec-funds", 4)));
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", 1).Accepted);

            var bob = new PlayerId(2);
            var rejected = economy.TryPurchase(bob, "tube-2", 1);
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual("InsufficientFunds", rejected.ReasonCode);
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void RecordingRewardRollsBackWhenPersistenceFails()
        {
            economy.SetPersistenceHandler(() => false);
            Assert.AreEqual(PlayerActionResult.Rejected, economy.TryRewardRecording(Recording("rec-save-fail", 4)));
            Assert.AreEqual(0, economy.SharedBalance);

            economy.SetPersistenceHandler(null);
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(Recording("rec-save-fail", 4)));
            Assert.AreEqual(200, economy.SharedBalance);
        }

        [Test]
        public void PurchaseReturnsSaveFailedAndRollsBackWhenPersistenceFails()
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(Recording("rec-buy-funds", 4)));
            economy.SetPersistenceHandler(() => false);

            var result = economy.TryPurchase(alice, "tube-1", 7);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("SaveFailed", result.ReasonCode);
            Assert.AreEqual(200, economy.SharedBalance);
            CollectionAssert.DoesNotContain(economy.LoadoutFor(alice), "tube-1");
        }
    }
}
