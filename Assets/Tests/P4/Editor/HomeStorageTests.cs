using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #90 shared home storage. An item is in exactly one place (pending/carried OR stored), the moves are
    // atomic with the save, ownership follows the existing pending-item rules, and capacity is the bag rule.
    public sealed class HomeStorageTests
    {
        private static readonly PlayerId Host = new PlayerId(0), Guest = new PlayerId(1);

        private GameObject root;
        private EconomyManager economy;
        private EconomySaveStore store;
        private string path;
        private ulong request = 1;

        [SetUp]
        public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-storage", Guid.NewGuid().ToString("N") + ".json");
            root = new GameObject("storage");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
        }

        [TearDown]
        public void Cleanup()
        {
            DayLock.Bind(null);
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private static PendingTurnInSave Catch(string id, int grams, bool shared, ulong carrier = 0) => new PendingTurnInSave
        {
            ItemId = id, Kind = (byte)TurnInKind.Catch, SourceDiveId = "dive-1", SubjectId = "sea_bass",
            WeightGrams = grams, SharedEscrow = shared, CarrierPlayerId = carrier, Revision = 1
        };

        private void Seed(params PendingTurnInSave[] items)
        {
            var data = new EconomySaveData();
            data.PendingTurnIns.AddRange(items);
            Assert.IsTrue(economy.TryRestore(data));
        }

        [Test]
        public void StoringMovesAnItemOutOfPendingAndIntoStorageAndItCannotBeSoldFromThere()
        {
            Seed(Catch("c1", 1000, true));
            Assert.IsTrue(economy.TryStoreItem(Host, "c1", request++).Accepted);

            Assert.AreEqual(0, economy.PendingCountFor(Host, TurnInKind.Catch));
            Assert.AreEqual(1, economy.StoredCount);
            var sold = economy.TrySellCatches(Host, request++);
            Assert.IsFalse(sold.Accepted, "the fish buyer sees nothing pending");
            Assert.AreEqual("NothingToTurnIn", sold.ReasonCode);
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void RetrievingPutsItBackToBeSoldAndItPaysExactlyOnce()
        {
            Seed(Catch("c1", 1000, true));
            economy.TryStoreItem(Host, "c1", request++);
            Assert.IsTrue(economy.TryRetrieveItem(Guest, "c1", request++).Accepted);
            Assert.AreEqual(0, economy.StoredCount);

            var first = economy.TrySellCatches(Guest, request++);
            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(120, economy.SharedBalance);
            Assert.IsFalse(economy.TrySellCatches(Guest, request++).Accepted, "already sold");
            Assert.AreEqual(120, economy.SharedBalance);
            Assert.IsFalse(economy.TryStoreItem(Guest, "c1", request++).Accepted, "a sold catch cannot be stored");
        }

        [Test]
        public void AnItemIsNeverInTwoPlaces()
        {
            Seed(Catch("c1", 1000, true));
            Assert.IsTrue(economy.TryStoreItem(Host, "c1", request++).Accepted);
            Assert.AreEqual("InvalidTarget", economy.TryStoreItem(Host, "c1", request++).ReasonCode, "already stored");
            Assert.IsTrue(economy.TryRetrieveItem(Host, "c1", request++).Accepted);
            Assert.AreEqual("InvalidTarget", economy.TryRetrieveItem(Host, "c1", request++).ReasonCode, "already out");
            Assert.AreEqual(1, economy.PendingTurnIns().Count + economy.StoredCount);
        }

        [Test]
        public void ReplayingTheSameRequestReturnsTheFirstAnswerAndChangesNothing()
        {
            Seed(Catch("c1", 1000, true));
            var first = economy.TryStoreItem(Host, "c1", 77);
            var replay = economy.TryStoreItem(Host, "c1", 77);
            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(first.Revision, replay.Revision);
            Assert.AreEqual(1, economy.StoredCount);

            var out1 = economy.TryRetrieveItem(Host, "c1", 78);
            var out2 = economy.TryRetrieveItem(Host, "c1", 78);
            Assert.IsTrue(out1.Accepted);
            Assert.IsTrue(out2.Accepted, "the replay is the cached first answer, not a second retrieval");
            Assert.AreEqual(1, economy.PendingTurnIns().Count);
        }

        [Test]
        public void SomeoneElsesCarriedCatchCannotBeStoredByAnotherPlayer()
        {
            Seed(Catch("mine", 1000, false, carrier: 0));
            Assert.AreEqual("InvalidTarget", economy.TryStoreItem(Guest, "mine", request++).ReasonCode);
            Assert.IsTrue(economy.TryStoreItem(Host, "mine", request++).Accepted);
        }

        [Test]
        public void OnlyCatchesAreStored()
        {
            var data = new EconomySaveData();
            data.PendingTurnIns.Add(new PendingTurnInSave
            {
                ItemId = "rec1", Kind = (byte)TurnInKind.Recording, SubjectId = "sea_bass", Quality = 3,
                ValidDurationSeconds = 4, SharedEscrow = true
            });
            economy.TryRestore(data);
            Assert.AreEqual("InvalidTarget", economy.TryStoreItem(Host, "rec1", request++).ReasonCode);
        }

        [Test]
        public void StorageHasAFiniteNumberOfSlots()
        {
            var items = new List<PendingTurnInSave>();
            for (var i = 0; i < EconomyManager.StorageCapacityItems + 1; i++) items.Add(Catch("c" + i, 10, true));
            Seed(items.ToArray());
            for (var i = 0; i < EconomyManager.StorageCapacityItems; i++)
                Assert.IsTrue(economy.TryStoreItem(Host, "c" + i, request++).Accepted, "slot " + i);
            Assert.AreEqual("StorageFull", economy.TryStoreItem(Host, "c" + EconomyManager.StorageCapacityItems, request++).ReasonCode);
        }

        [Test]
        public void RetrievalRespectsTheCarryCapacity()
        {
            var cap = InventoryManager.CapacityGrams;
            Seed(Catch("carried", cap - 500, false, carrier: 1), Catch("heavy", 1000, true));
            economy.TryStoreItem(Guest, "heavy", request++);

            var refused = economy.TryRetrieveItem(Guest, "heavy", request++);
            Assert.AreEqual("InventoryFull", refused.ReasonCode);
            Assert.AreEqual(1, economy.StoredCount, "a refused retrieval leaves the item stored");

            Assert.IsTrue(economy.TryRetrieveItem(Host, "heavy", request++).Accepted, "another player has room");
        }

        [Test]
        public void ClosingTheDayLocksStorageAndTheRefusalIsNotCached()
        {
            Seed(Catch("c1", 1000, true));
            var locked = true;
            Func<bool> provider = () => locked;
            DayLock.Bind(provider);

            Assert.AreEqual("DayClosing", economy.TryStoreItem(Host, "c1", 5).ReasonCode);
            locked = false;
            Assert.IsTrue(economy.TryStoreItem(Host, "c1", 5).Accepted, "same request id works once the day reopens");
            DayLock.Unbind(provider);
        }

        [Test]
        public void StorageSurvivesARealSaveAndReload()
        {
            Seed(Catch("c1", 1000, true), Catch("c2", 2000, true));
            economy.TryStoreItem(Host, "c1", request++);
            Assert.IsTrue(store.SaveNow(), store.LastError);

            Assert.IsTrue(economy.TryRestore(new EconomySaveData()));
            Assert.AreEqual(0, economy.StoredCount);
            Assert.IsTrue(store.LoadNow(), store.LastError);

            Assert.AreEqual(1, economy.StoredCount);
            Assert.AreEqual("c1", economy.StoredItems()[0].ItemId);
            Assert.AreEqual(1000, economy.StoredItems()[0].WeightGrams);
            Assert.AreEqual(1, economy.PendingTurnIns().Count, "c2 is still pending, c1 is not duplicated");
        }

        [Test]
        public void AFailedWriteRestoresBothListsAndTheSameRequestCanRetry()
        {
            Seed(Catch("c1", 1000, true));
            store.CanWrite = () => false;                 // the campaign file cannot be written
            economy.SetPersistenceHandler(store.SaveNow);

            var failed = economy.TryStoreItem(Host, "c1", 9);
            Assert.AreEqual("SaveFailed", failed.ReasonCode);
            Assert.AreEqual(0, economy.StoredCount);
            Assert.AreEqual(1, economy.PendingTurnIns().Count);

            store.CanWrite = null;
            Assert.IsTrue(economy.TryStoreItem(Host, "c1", 9).Accepted);
            Assert.AreEqual(1, economy.StoredCount);
        }

        [Test]
        public void ATamperedFileCannotDuplicateOrResurrectItems()
        {
            var data = new EconomySaveData();
            data.PendingTurnIns.Add(Catch("dup", 100, true));
            data.StoredItems.Add(Catch("dup", 100, true));        // also listed as stored
            data.StoredItems.Add(Catch("paid", 100, true));
            data.SoldCaptureIds.Add("paid");                       // already sold
            data.StoredItems.Add(Catch("ok", 100, true));
            Assert.IsTrue(economy.TryRestore(data));

            Assert.AreEqual(1, economy.StoredCount);
            Assert.AreEqual("ok", economy.StoredItems()[0].ItemId);
            Assert.AreEqual(1, economy.PendingTurnIns().Count);
        }

        [Test]
        public void ThePhysicalInteractionSeamRefusesWhenNothingIsBoundAndRoutesWhenBound()
        {
            Assert.AreEqual("InvalidState", HomeStorageItems.TryStore(Host, "c1", 1).ReasonCode);
            Assert.AreEqual("InvalidState", HomeStorageItems.TryRetrieve(Host, "c1", 1).ReasonCode);

            Seed(Catch("c1", 1000, true));
            Func<PlayerId, string, ulong, TransactionResult> store = economy.TryStoreItem;
            Func<PlayerId, string, ulong, TransactionResult> retrieve = economy.TryRetrieveItem;
            HomeStorageItems.Bind(store, retrieve);
            Assert.IsTrue(HomeStorageItems.TryStore(Host, "c1", 2).Accepted);
            Assert.IsTrue(HomeStorageItems.TryRetrieve(Host, "c1", 3).Accepted);
            HomeStorageItems.Unbind(store, retrieve);
            Assert.AreEqual("InvalidState", HomeStorageItems.TryStore(Host, "c1", 4).ReasonCode);
        }
    }
}
