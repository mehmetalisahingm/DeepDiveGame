using System;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #156: the sandal quest told a crew "2/3" and nothing else. These pin (1) what the quest line now says and (2) that a campaign saved at 2/3
    // keeps going without any reset: the missing part can still be found or bought, finding it twice (two players at once, a replay, a reopen)
    // contributes once, and the repair survives the next reopen.
    public sealed class BoatQuestGuideTests
    {
        private static readonly PlayerId Alice = new PlayerId(1), Bob = new PlayerId(2);

        [Test]
        public void TheLineNamesEveryMissingPartAndNothingElse()
        {
            Assert.AreEqual(3, BoatRepairGuide.Missing(0).Count);
            var missing = BoatRepairGuide.Missing(BoatRepairGuide.Bit(BoatRepairParts.Hull) | BoatRepairGuide.Bit(BoatRepairParts.FuelTank));
            Assert.AreEqual(new[] { BoatRepairParts.Engine }, missing.ToArray());
            Assert.IsEmpty(BoatRepairGuide.Missing(7));
            foreach (var part in BoatRepairParts.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(BoatRepairGuide.PartName(part)), part);
                Assert.AreNotEqual(0, BoatRepairGuide.Bit(part), part);
            }
            Assert.AreEqual(BoatRepairParts.All.Count, BoatRepairParts.All.Select(BoatRepairGuide.PartName).Distinct().Count(), "three different names");
            Assert.AreEqual(string.Empty, BoatRepairGuide.PartName("boat-part-wing"));
        }

        [Test]
        public void TheMaskBitIsTheSameRuleTheTownBindingPublishes()
        {
            for (var i = 0; i < BoatRepairParts.All.Count; i++)
                Assert.AreEqual(1 << i, BoatRepairGuide.Bit(BoatRepairParts.All[i]), "TownServiceBinding.PublishProgress sets 1 << index");
        }

        [Test]
        public void TheCompassPointsTowardThePartInTheScenesOwnFrame()
        {
            Assert.AreEqual("kuzey", BoatRepairGuide.Compass(0f, 5f));
            Assert.AreEqual("dogu", BoatRepairGuide.Compass(5f, 0f));
            Assert.AreEqual("guney", BoatRepairGuide.Compass(0f, -5f));
            Assert.AreEqual("bati", BoatRepairGuide.Compass(-5f, 0f));
            Assert.AreEqual("kuzeybati", BoatRepairGuide.Compass(-4f, 4f));
            Assert.AreEqual("guneydogu", BoatRepairGuide.Compass(4f, -4f));
            Assert.AreEqual("burada", BoatRepairGuide.Compass(0f, 0f));
        }

        [Test]
        public void TheLineReadsNameDirectionAndDistance()
        {
            Assert.AreEqual("Oturak destegi: bati, 12 m", BoatRepairGuide.Line(BoatRepairParts.Engine, 0f, 0f, -12f, 0.2f));
            Assert.AreEqual("Kurek baglantisi: burada", BoatRepairGuide.Line(BoatRepairParts.FuelTank, 3f, 3f, 3f, 3f));
        }

        // ---- a campaign saved at 2/3 goes on ------------------------------------------------------------------------------

        private sealed class Rig : IDisposable
        {
            public readonly GameObject Root = new GameObject("boat-quest");
            public readonly EconomyManager Economy;
            public readonly EconomySaveStore Store;

            public Rig(string path, int balance = 0)
            {
                Root.AddComponent<InventoryManager>();
                Economy = Root.AddComponent<EconomyManager>();
                Store = Root.AddComponent<EconomySaveStore>();
                Store.SetPathForTests(path);
                if (File.Exists(path)) Assert.IsTrue(Store.LoadNow());
                else Assert.IsTrue(Economy.TryRestore(new EconomySaveData { SharedBalance = balance }));
                Economy.SetPersistenceHandler(Store.SaveNow);
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }

        private static string NewPath() => Path.Combine(Path.GetTempPath(), "DeepDive-boat-quest", Guid.NewGuid().ToString("N") + ".json");

        [TestCase(BoatRepairParts.Hull)]
        [TestCase(BoatRepairParts.Engine)]
        [TestCase(BoatRepairParts.FuelTank)]
        public void ACampaignSavedAtTwoOfThreeCanStillFindTheMissingPartAndKeepsItAfterTheNextReopen(string missingPart)
        {
            var path = NewPath();
            ulong request = 1;
            using (var first = new Rig(path))
            {
                foreach (var part in BoatRepairParts.All.Where(p => p != missingPart))
                    Assert.IsTrue(first.Economy.TryContributeBoatPart(Alice, part, BoatPartSource.Found, request++).Accepted);
                Assert.AreEqual(2, first.Economy.BoatRepair.CompletedPartIds.Count);
                Assert.IsTrue(first.Store.SaveNow());
            }
            using (var second = new Rig(path))
            {
                Assert.AreEqual(2, second.Economy.BoatRepair.CompletedPartIds.Count, "reopened at 2/3, nothing reset");
                Assert.AreEqual(BoatRepairStatus.InProgress, second.Economy.BoatRepair.Status);
                Assert.AreEqual(missingPart, BoatRepairGuide.Missing(Mask(second.Economy)).Single(), "the guide names exactly the missing part");
                // Acquisition is durable but is not a contribution; only one material exists.
                var a = second.Economy.TryAcquireBoatPart(Alice, missingPart, BoatPartSource.Found, 1);
                var b = second.Economy.TryAcquireBoatPart(Bob, missingPart, BoatPartSource.Found, 1);
                Assert.IsTrue(a.Accepted);
                Assert.AreEqual("AlreadyProcessed", b.ReasonCode);
                Assert.AreEqual(BoatRepairStatus.InProgress, second.Economy.BoatRepair.Status);
                Assert.IsTrue(second.Economy.TryFinishBoatPart(Alice, missingPart, 2).Accepted);
                Assert.AreEqual(BoatRepairStatus.Repaired, second.Economy.BoatRepair.Status);
                Assert.AreEqual(0, second.Economy.SharedBalance, "a found part costs nothing");
            }
            using (var third = new Rig(path))
            {
                Assert.AreEqual(BoatRepairStatus.Repaired, third.Economy.BoatRepair.Status, "the repair survived the next reopen");
                Assert.IsEmpty(BoatRepairGuide.Missing(Mask(third.Economy)));
            }
        }

        [Test]
        public void TheShopPartIsTheWayOnWhenTheCrewCannotFindTheLastOne()
        {
            var path = NewPath();
            using (var first = new Rig(path, balance: 500))
            {
                Assert.IsTrue(first.Economy.TryContributeBoatPart(Alice, BoatRepairParts.Hull, BoatPartSource.Found, 1).Accepted);
                Assert.IsTrue(first.Economy.TryContributeBoatPart(Alice, BoatRepairParts.Engine, BoatPartSource.Found, 2).Accepted);
                Assert.IsTrue(first.Store.SaveNow());
            }
            using (var second = new Rig(path))
            {
                var price = second.Economy.BoatPartPrice;
                var before = second.Economy.SharedBalance;
                Assert.IsTrue(second.Economy.TryAcquireBoatPart(Bob, BoatRepairParts.FuelTank, BoatPartSource.Purchased, 1).Accepted);
                Assert.AreEqual(before - price, second.Economy.SharedBalance);
                Assert.AreEqual(BoatRepairStatus.InProgress, second.Economy.BoatRepair.Status);
                Assert.IsTrue(second.Economy.TryFinishBoatPart(Bob, BoatRepairParts.FuelTank, 2).Accepted);
                Assert.AreEqual(BoatRepairStatus.Repaired, second.Economy.BoatRepair.Status);
                // The same part found afterwards is not charged and not counted twice.
                var again = second.Economy.TryContributeBoatPart(Alice, BoatRepairParts.FuelTank, BoatPartSource.Found, 5);
                Assert.AreEqual("AlreadyProcessed", again.ReasonCode);
                Assert.AreEqual(before - price, second.Economy.SharedBalance);
            }
        }

        private static int Mask(EconomyManager economy)
        {
            var mask = 0;
            foreach (var part in economy.BoatRepair.CompletedPartIds) mask |= BoatRepairGuide.Bit(part);
            return mask;
        }
    }
}
