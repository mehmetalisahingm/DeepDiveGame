using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Media;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    // P4.5-C (#132) "dar denge turu". These are NOT playtest results: they pin the ARITHMETIC the working values were chosen with, so a later
    // price change that breaks the reasoning (an improvement that never pays back, a bonus bigger than the work, a ladder that is bought in a
    // day) fails here and gets re-argued. docs/reports/P4-5-LIVING-WORLD.md has the table and the assumptions; real numbers come from P4.6 play.
    public sealed class BalanceNarrowPassTests
    {
        // ASSUMPTIONS (to be replaced by measured play): a fish is worth 120 whatever its weight (flat price per capture), a diver hands in about
        // 4 fish and 1 quality-3 recording at the NPC (100) per day, the average fish weighs ~1.75 kg (the species range is 0.9..2.6 kg).
        private const int FishPrice = 120;
        private const int FishPerDiverDay = 4;
        private const int RecordingPerDiverDay = 100;
        private const int AverageFishGrams = 1750;

        private static int CrewDailyIncome(int crew) => crew * (FishPerDiverDay * FishPrice + RecordingPerDiverDay);

        private static ContractTemplate Order(string id)
        {
            Assert.IsTrue(ContractCatalog.TryGet(id, out var t), id);
            return t;
        }

        [Test]
        public void EachFishOrderPaysABonusOfRoughlyAThirdToAHalfOfTheFishItAsksFor()
        {
            foreach (var id in new[] { ContractIds.OrderQuick, ContractIds.OrderBass, ContractIds.OrderHeavy })
            {
                var t = Order(id);
                var fish = t.Measure == ContractMeasure.GramsSold ? (t.Target + AverageFishGrams - 1) / AverageFishGrams : t.Target;
                var value = fish * FishPrice;
                var bonus = (float)t.Reward / value;
                Assert.That(bonus, Is.InRange(0.25f, 0.60f), $"{id}: reward {t.Reward} on {fish} fish worth {value}");
            }
        }

        [Test]
        public void ASponsorBonusIsAboutAsBigAsWhatTheClipItselfEarnsNotAMultipleOfIt()
        {
            ChannelResultRules.Evaluate(3, true, 0, out _, out _, out var q3Novel);
            foreach (var t in ContractCatalog.All)
            {
                if (t.Kind != ContractKind.VideoSponsor) continue;
                Assert.LessOrEqual(t.Reward, 2 * q3Novel, $"{t.Id} pays {t.Reward} against a first quality-3 clip's {q3Novel}");
                Assert.Greater(t.Reward, 0);
            }
        }

        [Test]
        public void TheFishermansStallPaysBackWithinAFewDaysForATwoPlayerCrew()
        {
            DevelopmentCatalog.TryGet(DevelopmentIds.TownFisher, out var fisher);
            var extraPerDay = 2 * FishPerDiverDay * FishPrice * DevelopmentCatalog.FisherPricePercent / 100;
            var days = (float)fisher.Price / extraPerDay;
            Assert.LessOrEqual(days, 6f, $"{fisher.Price} credits / {extraPerDay} per day = {days:0.0} days");
        }

        [Test]
        public void TheDockSavesMoreThanItCostsOnceBothBoatsAreBought()
        {
            DevelopmentCatalog.TryGet(DevelopmentIds.TownDock, out var dock);
            var saved = (VehicleCatalog.MotorboatPrice + VehicleCatalog.ResearchBoatPrice) * DevelopmentCatalog.DockVehicleDiscountPercent / 100;
            Assert.Greater(saved, dock.Price, "an improvement that can never pay for itself would just be a tax");
        }

        [Test]
        public void TheNewTubeTierIsTheNextLevelAndCostsMoreThanTheOneBelow()
        {
            var economy = new UnityEngine.GameObject("balance").AddComponent<EconomyManager>();
            try
            {
                Assert.IsTrue(economy.TryRestore(new EconomySaveData()));
                Assert.IsTrue(economy.TryGetEquipmentDefinition("tube-2", out var t2));
                Assert.IsTrue(economy.TryGetEquipmentDefinition(DevelopmentCatalog.ShopUnlockedEquipmentId, out var t3));
                Assert.Greater(t3.Price, t2.Price);
                Assert.AreEqual(t2.Level + 1, t3.Level);
            }
            finally { UnityEngine.Object.DestroyImmediate(economy.gameObject); }
        }

        [Test]
        public void TheWholeLadderTakesAboutTwoWeeksOfPlayForATwoPlayerCrewAndMoreForASoloDiver()
        {
            var economy = new UnityEngine.GameObject("balance").AddComponent<EconomyManager>();
            try
            {
                Assert.IsTrue(economy.TryRestore(new EconomySaveData()));
                var perPlayer = 0;
                foreach (var id in new[] { "tube-1", "tube-2", DevelopmentCatalog.ShopUnlockedEquipmentId, EconomyManager.CameraBasicId, EconomyManager.CameraAdvancedId,
                    EconomyManager.CameraProId, EconomyManager.FinsId, EconomyManager.BagId, EconomyManager.HarpoonId })
                {
                    Assert.IsTrue(economy.TryGetEquipmentDefinition(id, out var d), id);
                    perPlayer += d.Price;
                }
                var parts = 3 * economy.BoatPartPrice;
                var motorAndResearch = VehicleCatalog.MotorboatPrice + VehicleCatalog.ResearchBoatPrice;
                motorAndResearch -= motorAndResearch * DevelopmentCatalog.DockVehicleDiscountPercent / 100;
                var development = 0;
                foreach (var d in DevelopmentCatalog.All) development += d.Price;

                var crew2 = 2 * perPlayer + parts + motorAndResearch + development;
                var days2 = (float)crew2 / CrewDailyIncome(2);
                Assert.That(days2, Is.InRange(8f, 16f), $"2 players: {crew2} credits at {CrewDailyIncome(2)}/day = {days2:0.0} days");

                var solo = perPlayer + parts + motorAndResearch + development;
                var days1 = (float)solo / CrewDailyIncome(1);
                Assert.Greater(days1, days2, "a solo diver takes longer: the ladder is shared, the income is not");
                Assert.LessOrEqual(days1, 30f, $"solo: {solo} credits at {CrewDailyIncome(1)}/day = {days1:0.0} days");
            }
            finally { UnityEngine.Object.DestroyImmediate(economy.gameObject); }
        }

        [Test]
        public void ThePlansContentBudgetIsRespected()
        {
            var orders = 0; var sponsors = 0;
            foreach (var t in ContractCatalog.All)
            {
                if (t.Kind == ContractKind.FishOrder) orders++;
                else if (!t.RequiresNight) sponsors++;
            }
            Assert.AreEqual(3, orders, "three fish orders");
            Assert.AreEqual(3, sponsors, "three video sponsor templates (the night one is a reserved fourth, dormant until the world has night data)");
            Assert.AreEqual(4, DevelopmentCatalog.All.Count, "two house levels (one purchase) + three town improvements = home-2, fisher, shop, dock");
        }
    }
}
