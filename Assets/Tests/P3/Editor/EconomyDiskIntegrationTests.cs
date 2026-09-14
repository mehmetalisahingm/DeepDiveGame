using System;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    public sealed class EconomyDiskIntegrationTests
    {
        private GameObject root;
        private EconomyManager economy;
        private EconomySaveStore store;
        private string path;
        [SetUp] public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P3-tests", Guid.NewGuid().ToString("N") + ".json");
            root = new GameObject("DiskIntegrationTest");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
        }
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
        private void Credit(string id) => Assert.AreEqual(PlayerActionResult.Accepted, economy.TryRewardRecording(
            new RecordingResult(id, "dive", new PlayerId(0), "event_bioluminescence", 4, 8)));

        [Test] public void RealDiskRestoreKeepsHostTubeAndReplayGuardsWithoutReassigningGuestEquipment()
        {
            Credit("a"); Credit("b");
            Assert.IsTrue(economy.TryPurchase(new PlayerId(0), "tube-1", 1).Accepted);
            Assert.IsTrue(economy.TryPurchase(new PlayerId(1), "tube-1", 1).Accepted);
            Assert.IsTrue(store.SaveNow(), store.LastError);
            Assert.IsTrue(economy.TryRestore(new EconomySaveData()));
            Assert.AreEqual(0, economy.SharedBalance);
            Assert.IsTrue(store.LoadNow(), store.LastError);
            Assert.AreEqual(200, economy.SharedBalance);
            Assert.IsEmpty(economy.LoadoutFor(new PlayerId(1)), "Transient guest IDs must not be restored from disk");
            Assert.IsTrue(economy.TryGetEquipmentDefinition("tube-1", out var tube));
            var loadout = economy.LoadoutStateFor(new PlayerId(0));
            Assert.AreEqual(150, DiverEquipmentRules.ResolveMaxOxygen(120, new PlayerId(0), loadout, new[] { tube }));
            Assert.AreEqual(120, DiverEquipmentRules.ResolveMaxOxygen(120, new PlayerId(1), loadout, new[] { tube }));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryRewardRecording(
                new RecordingResult("a", "dive", new PlayerId(0), "event_bioluminescence", 4, 8)));
            Assert.AreEqual(200, economy.SharedBalance);
        }
        [Test] public void CorruptPrimaryFallsBackToBackupAndClientCannotOverwriteIt()
        {
            Credit("a"); Assert.IsTrue(store.SaveNow());
            Credit("b"); Assert.IsTrue(store.SaveNow());
            File.WriteAllText(path, "not valid json");
            Assert.IsTrue(store.LoadNow(), store.LastError);
            Assert.AreEqual(200, economy.SharedBalance);
            var before = File.ReadAllBytes(path);
            store.CanWrite = () => false;
            Assert.IsFalse(store.SaveNow());
            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
        }
    }
}
