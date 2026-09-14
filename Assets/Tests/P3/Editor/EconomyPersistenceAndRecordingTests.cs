using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Session;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    public class EconomyPersistenceAndRecordingTests
    {
        private GameObject root;
        private EconomyManager economy;
        private string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("p3-economy-persistence-test");
            root.AddComponent<SessionManager>();
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            economy.ConfigureP3Defaults();
            tempDirectory = Path.Combine(Path.GetTempPath(), "DeepDive-P3-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
        }

        [Test]
        public void RecordingRewardPaysQualityTableAndSameRecordingCannotPayTwice()
        {
            var result = new RecordingResult("rec-1", "dive-1", new PlayerId(1), "sea_bass", 3, 8f);

            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryPayRecording(result));
            Assert.AreEqual(350, economy.SharedBalance);
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryPayRecording(result));
            Assert.AreEqual(350, economy.SharedBalance);
        }

        [Test]
        public void InvalidOrUnpricedRecordingNeverMintsCredits()
        {
            var badQuality = new RecordingResult("rec-1", "dive-1", new PlayerId(1), "sea_bass", 0, 8f);
            Assert.AreEqual(PlayerActionResult.InvalidTarget, economy.TryPayRecording(badQuality));
            Assert.AreEqual(0, economy.SharedBalance);

            economy.SetRecordingPrice(2, 0);
            var unpriced = new RecordingResult("rec-2", "dive-1", new PlayerId(1), "sea_bass", 2, 8f);
            Assert.AreEqual(PlayerActionResult.InvalidState, economy.TryPayRecording(unpriced));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void TubeShopAdvancesOneLevelAtATimeAndUsesSharedBalance()
        {
            var host = new PlayerId(0);
            Assert.IsTrue(economy.TryGetNextTubeUpgrade(host, out var first));
            Assert.AreEqual("tube-1", first.EquipmentId);
            Assert.AreEqual(300, first.Price);

            Assert.AreEqual(PlayerActionResult.Accepted,
                economy.TryPayRecording(new RecordingResult("rec-gold", "dive-1", host, "sea_bass", 3, 8f)));
            var bought = economy.TryPurchase(host, "tube-1", 1);
            Assert.IsTrue(bought.Accepted);
            Assert.AreEqual(50, economy.SharedBalance);
            Assert.IsTrue(economy.TryGetNextTubeUpgrade(host, out var second));
            Assert.AreEqual("tube-2", second.EquipmentId);

            var skip = economy.TryPurchase(host, "tube-3", 2);
            Assert.IsFalse(skip.Accepted);
            Assert.AreEqual("InvalidState", skip.ReasonCode);
        }

        [Test]
        public void SnapshotRestoresBalanceHostLoadoutAndPaidRecordingIds()
        {
            var host = new PlayerId(0);
            economy.TryPayRecording(new RecordingResult("rec-gold", "dive-1", host, "sea_bass", 3, 8f));
            Assert.IsTrue(economy.TryPurchase(host, "tube-1", 1).Accepted);
            var snapshot = economy.CreateSaveSnapshot("campaign-1", "checkpoint-1");

            var restoredRoot = new GameObject("p3-restored-economy-test");
            try
            {
                restoredRoot.AddComponent<SessionManager>();
                restoredRoot.AddComponent<InventoryManager>();
                var restored = restoredRoot.AddComponent<EconomyManager>();
                restored.ConfigureP3Defaults();

                Assert.IsTrue(restored.TryRestoreSnapshot(snapshot));
                Assert.AreEqual(50, restored.SharedBalance);
                CollectionAssert.Contains(restored.LoadoutFor(host), "tube-1");
                Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                    restored.TryPayRecording(new RecordingResult("rec-gold", "dive-1", host, "sea_bass", 3, 8f)));
                Assert.AreEqual(50, restored.SharedBalance);
            }
            finally
            {
                Object.DestroyImmediate(restoredRoot);
            }
        }

        [Test]
        public void SaveStoreVerifiesPrimaryAndKeepsBackupOnReplacement()
        {
            var path = Path.Combine(tempDirectory, "campaign.json");
            var first = new EconomySaveData
            {
                campaignId = "campaign-1",
                checkpointId = "cp-1",
                sharedBalance = 100,
                revision = 1
            };
            Assert.IsTrue(EconomySaveStore.TryWriteAtomic(path, first, out var firstError), firstError);

            var second = new EconomySaveData
            {
                campaignId = "campaign-1",
                checkpointId = "cp-2",
                sharedBalance = 250,
                revision = 2
            };
            Assert.IsTrue(EconomySaveStore.TryWriteAtomic(path, second, out var secondError), secondError);
            Assert.IsTrue(File.Exists(path));
            Assert.IsTrue(File.Exists(path + ".bak"));

            Assert.IsTrue(EconomySaveStore.TryLoad(path, out var loaded, out var usedBackup));
            Assert.IsFalse(usedBackup);
            Assert.AreEqual("cp-2", loaded.checkpointId);
            Assert.AreEqual(250, loaded.sharedBalance);
        }
    }
}
