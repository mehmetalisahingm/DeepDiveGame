using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Session;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    // #218: the sale end of the five-species chain, through the real session, inventory and economy. Safe return queues each species,
    // the buyer pays the species table price once, and a save that already paid the captures never pays them again after a reload.
    // Real harpoon/pickup/swimming are the integrated -p2-hunt smoke's job.
    public class P411SpeciesSaleTests
    {
        private static readonly string[] Ids = { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
        private static readonly int[] Prices = { 120, 120, 120, 180, 320 };

        private GameObject root;
        private SessionManager session;
        private InventoryManager inventory;
        private EconomyManager economy;
        private PlayerId alice, bob;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("p411-sale");
            session = root.AddComponent<SessionManager>();
            inventory = root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            // EditMode AddComponent does not guarantee Awake; a public call initialises the default species price table.
            economy.SetPrice("__p411_bootstrap__", 0);
            alice = new PlayerId(1);
            bob = new PlayerId(2);
            session.Initialize("room", "DiveTestArea");
            session.Join(alice);
            session.Join(bob);
            session.SetReady(alice, true);
            session.SetReady(bob, true);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.Ok, session.BeginDive("dive-1"));
        }

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        private static CaptureResult Capture(int index) =>
            new CaptureResult("cap-" + Ids[index], "dive-1", Ids[index], 700 + index * 100, catchObjectId: (ulong)(index + 1));

        [Test]
        public void EverySpeciesIsSoldOnceAtItsOwnPriceAndNeverPaidAgainAfterReload()
        {
            for (var i = 0; i < Ids.Length; i++) Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(alice, Capture(i)), Ids[i]);
            inventory.TryMarkSafeReturn(alice);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());
            Assert.AreEqual(5, economy.PendingCountFor(alice, TurnInKind.Catch), "all five species queue as pending, unpaid");
            Assert.AreEqual(0, economy.SharedBalance, "diving never pays");

            var sale = economy.TrySellCatches(alice, requestId: 1);
            Assert.IsTrue(sale.Accepted);
            Assert.AreEqual(5, sale.ItemCount);
            Assert.AreEqual(Prices.Sum(), sale.Earned, "120 + 120 + 120 + 180 + 320: each species pays its own table price");
            Assert.AreEqual(Prices.Sum(), economy.SharedBalance);
            Assert.AreEqual("NothingToTurnIn", economy.TrySellCatches(alice, requestId: 2).ReasonCode, "a second request pays nothing");

            // A saved campaign that already paid these captures: after a reload, the same captures reaching a safe return again
            // are not queued and not paid a second time.
            var saved = economy.ExportSaveData("camp", "ck");
            foreach (var id in Ids) CollectionAssert.Contains(saved.SoldCaptureIds, "cap-" + id);
            var reloaded = new GameObject("p411-sale-reload");
            try
            {
                var inventory2 = reloaded.AddComponent<InventoryManager>();
                var economy2 = reloaded.AddComponent<EconomyManager>();
                economy2.SetPrice("__p411_bootstrap__", 0);
                Assert.IsTrue(economy2.TryRestore(saved));
                Assert.AreEqual(Prices.Sum(), economy2.SharedBalance);
                var again = new DiveSummary("dive-1", new[] { alice }, Ids.Select(id => "cap-" + id).ToArray(), new string[0], "ck");
                Assert.AreEqual(0, economy2.QueuePreservedCatches(again), "already paid captures are not queued again");
                Assert.AreEqual(0, economy2.PendingCountFor(alice, TurnInKind.Catch));
                Assert.AreEqual(Prices.Sum(), economy2.SharedBalance);
            }
            finally { Object.DestroyImmediate(reloaded); }
        }
    }
}
