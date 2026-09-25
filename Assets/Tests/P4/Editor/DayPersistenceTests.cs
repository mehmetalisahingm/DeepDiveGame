using System;
using System.IO;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #90: the day is written into the SAME campaign file as the money and the boat, so the close's
    // "one atomic write" is real: tested here against the real EconomySaveStore on a real file and a
    // fresh process-equivalent (a second store instance loading that file).
    public sealed class DayPersistenceTests
    {
        private static readonly PlayerId Host = new PlayerId(0);

        private sealed class OneActive : IDayRoster
        {
            public IReadOnlyList<PlayerId> ActivePlayers() => new[] { Host };
        }

        // The real close hooks' persistence step: write through the campaign store.
        private sealed class StoreHooks : IDayCloseHooks
        {
            public EconomySaveStore Store;
            public bool WriteAllowed = true;
            public void SettleAcceptedActions(string closeId) { }
            public DayDiveClosure CloseOpenDives(string closeId) => DayDiveClosure.None;
            public bool Persist() => WriteAllowed && Store.SaveNow();
            public void BeginMorning(int dayNumber) { }
        }

        private sealed class Rig
        {
            public GameObject Root;
            public EconomyManager Economy;
            public EconomySaveStore Store;
            public DayEngine Engine;
            public StoreHooks Hooks;
        }

        private string path;
        private readonly List<GameObject> roots = new List<GameObject>();

        [SetUp]
        public void Setup() =>
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-tests", Guid.NewGuid().ToString("N") + ".json");

        [TearDown]
        public void Cleanup()
        {
            foreach (var root in roots) UnityEngine.Object.DestroyImmediate(root);
            roots.Clear();
            DayLock.Bind(null);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        // A "process": inventory + economy + store loading whatever is at `path`, then the day bound.
        private Rig Boot()
        {
            var root = new GameObject("DayPersistence");
            roots.Add(root);
            root.AddComponent<InventoryManager>();
            var economy = root.AddComponent<EconomyManager>();
            var store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
            store.LoadNow();

            var engine = new DayEngine();
            var hooks = new StoreHooks { Store = store };
            engine.Configure(new OneActive(), hooks, 3);
            store.Day = engine;   // hands the engine whatever day the file already held

            return new Rig { Root = root, Economy = economy, Store = store, Engine = engine, Hooks = hooks };
        }

        [Test]
        public void ADayClosedThroughTheStoreComesBackAsTheNextDayInAFreshBoot()
        {
            var first = Boot();
            first.Engine.RecordSale("s", 2, 2000, 240);
            first.Engine.TryEnterBed(Host, DayIds.Bed0, 1);
            Assert.AreEqual(2, first.Engine.DayNumber);

            var second = Boot();
            Assert.AreEqual(2, second.Engine.DayNumber);
            Assert.AreEqual(DayPhase.Running, second.Engine.Phase);
            Assert.AreEqual(DayIds.DayStartMinute, second.Engine.ClockMinute);
            Assert.AreEqual(1, second.Engine.History.Count);
            Assert.AreEqual(240, second.Engine.History[0].Income);
            Assert.AreEqual(DayIds.CloseId(1), second.Engine.History[0].CloseId);
        }

        [Test]
        public void ReopeningTwiceNeverAdvancesTheDayAndNothingIsPaidTwice()
        {
            var first = Boot();
            first.Engine.TryEnterBed(Host, DayIds.Bed0, 1);

            for (var i = 0; i < 3; i++)
            {
                var again = Boot();
                Assert.AreEqual(2, again.Engine.DayNumber, "boot #" + i);
                Assert.AreEqual(1, again.Engine.History.Count);
            }
        }

        [Test]
        public void AFailedWriteLeavesTheFileOnTheOldDayAndTheRetryWritesTheSameClose()
        {
            var first = Boot();
            Assert.IsTrue(first.Store.SaveNow());
            first.Hooks.WriteAllowed = false;
            first.Engine.TryEnterBed(Host, DayIds.Bed0, 1);
            Assert.AreEqual(DayPhase.Summary, first.Engine.Phase);

            Assert.AreEqual(1, Boot().Engine.DayNumber, "a host crash here reloads day 1, not a half-closed day");

            first.Hooks.WriteAllowed = true;
            Assert.IsTrue(first.Engine.RetryClose());
            var reloaded = Boot();
            Assert.AreEqual(2, reloaded.Engine.DayNumber);
            Assert.AreEqual(1, reloaded.Engine.History.Count);
        }

        [Test]
        public void TheDayIsBoundLateStillGetsTheDayThatWasAlreadyOnDisk()
        {
            var first = Boot();
            first.Engine.TryEnterBed(Host, DayIds.Bed0, 1);

            // Same order the real host uses: the store loads in Awake, the day binds afterwards.
            var root = new GameObject("late");
            roots.Add(root);
            root.AddComponent<InventoryManager>();
            root.AddComponent<EconomyManager>();
            var store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
            store.LoadNow();
            var engine = new DayEngine();
            store.Day = engine;
            Assert.AreEqual(2, engine.DayNumber);
        }

        [Test]
        public void OlderV2SavesLoadAsDayOneAtEight()
        {
            File.Delete(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new EconomySaveData { SchemaVersion = 2, SharedBalance = 55 }));

            var rig = Boot();
            Assert.AreEqual(55, rig.Economy.SharedBalance);
            Assert.AreEqual(1, rig.Engine.DayNumber);
            Assert.AreEqual(DayIds.DayStartMinute, rig.Engine.ClockMinute);
        }

        [Test]
        public void MoneyAndDaySurviveTogetherInOneFile()
        {
            var first = Boot();
            first.Economy.TryRestore(new EconomySaveData { SharedBalance = 300 });
            first.Engine.TryEnterBed(Host, DayIds.Bed0, 1);

            var second = Boot();
            Assert.AreEqual(300, second.Economy.SharedBalance);
            Assert.AreEqual(2, second.Engine.DayNumber);
        }

        [Test]
        public void ClosingTheDayRefusesNewTradeAndTravelAndTomorrowReopensThem()
        {
            var rig = Boot();
            var manager = new GameObject("dm").AddComponent<DayManager>();
            roots.Add(manager.gameObject);
            var hooks = new StoreHooks { Store = rig.Store };
            manager.Configure(new OneActive(), hooks);
            rig.Store.Day = manager.Engine;
            rig.Economy.TryRestore(new EconomySaveData { SharedBalance = 500 });
            var trip = manager.gameObject.AddComponent<BoatTripManager>();

            hooks.WriteAllowed = false;             // hold the day in Summary
            manager.Engine.BeginClose(DayCloseReason.Midnight);

            Assert.AreEqual("DayClosing", rig.Economy.TryPurchase(Host, "tube-1", 10).ReasonCode);
            Assert.AreEqual("DayClosing", rig.Economy.TrySellCatches(Host, 11).ReasonCode);
            Assert.AreEqual("DayClosing", rig.Economy.TryContributeBoatPart(
                Host, BoatRepairParts.Hull, BoatPartSource.Purchased, 12).ReasonCode);
            Assert.AreEqual("DayClosing", trip.TryStartRoute(Host, BoatTripIds.NearRouteId, 13).ReasonCode);

            hooks.WriteAllowed = true;
            Assert.IsTrue(manager.Engine.RetryClose());
            manager.Engine.CompleteMorning();

            Assert.IsTrue(rig.Economy.TryPurchase(Host, "tube-1", 10).Accepted,
                "the same request id works tomorrow: a lock rejection is not cached");
            manager.Shutdown();
        }
    }
}
