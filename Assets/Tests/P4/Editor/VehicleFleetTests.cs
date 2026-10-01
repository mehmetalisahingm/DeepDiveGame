using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // P4.3-C (#109): vehicle ownership/progression/active selection and the equipment tiers, on the REAL EconomyManager, the
    // real save store + file, the real trip authority and the real harbor-vendor handler. No fixture stands in for a rule.
    public sealed class VehicleFleetTests
    {
        private static readonly PlayerId Host = new PlayerId(0), Guest = new PlayerId(1);

        private GameObject root;
        private EconomyManager economy;
        private EconomySaveStore store;
        private string path;
        private ulong request = 1;
        private Func<string> gateForCleanup;

        [SetUp]
        public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-fleet", Guid.NewGuid().ToString("N") + ".json");
            root = new GameObject("fleet");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
        }

        [TearDown]
        public void Cleanup()
        {
            DayLock.Bind(null);
            if (gateForCleanup != null) VehicleSwitchGate.Unbind(gateForCleanup);
            BoatBoarding.Unbind(BoatBoarding.BoardHandler, BoatBoarding.DisembarkHandler);
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        // A campaign with money and (optionally) a repaired rowboat, restored the way a real load would.
        private void Seed(int balance, bool repaired, string[] purchased = null, string active = "", bool hasFleet = true)
        {
            var data = new EconomySaveData { SharedBalance = balance, HasFleet = hasFleet, FleetActiveBoatId = active };
            if (repaired) data.BoatPartIds.AddRange(BoatRepairParts.All);
            if (purchased != null) data.FleetPurchasedBoatIds.AddRange(purchased);
            Assert.IsTrue(economy.TryRestore(data));
        }

        private static string[] Owned(EconomyManager manager) => manager.Fleet.OwnedBoatIds.ToArray();

        // ---- catalog and the single rowboat -------------------------------------------------------------------

        [Test]
        public void CatalogHasThreeStableVehiclesInProgressionOrder()
        {
            CollectionAssert.AreEqual(new[] { "boat-1", "boat-2", "boat-3" }, VehicleIds.All);
            Assert.AreEqual(BoatRepairParts.BoatId, VehicleIds.Rowboat, "the P3 rowboat keeps its id: saves, map and trip already reference it");
            Assert.IsFalse(VehicleCatalog.All[0].IsForSale, "the rowboat is repaired, never bought");
            Assert.AreEqual(VehicleIds.Rowboat, VehicleCatalog.All[1].RequiresBoatId);
            Assert.AreEqual(VehicleIds.Motorboat, VehicleCatalog.All[2].RequiresBoatId);
            Assert.Greater(VehicleCatalog.ResearchBoatPrice, VehicleCatalog.MotorboatPrice);
            Assert.AreEqual(VehicleClass.None, VehicleCatalog.ClassOf("boat-9"));
        }

        [Test]
        public void TheRowboatIsOwnedExactlyWhenItsRepairIsCompleteAndThereIsNeverASecondOne()
        {
            Assert.AreEqual(0, economy.Fleet.OwnedBoatIds.Count);
            Assert.AreEqual("", economy.ActiveVehicleId, "nothing to sail yet");

            economy.TryContributeBoatPart(Host, BoatRepairParts.Hull, BoatPartSource.Found, request++);
            economy.TryContributeBoatPart(Host, BoatRepairParts.Engine, BoatPartSource.Found, request++);
            Assert.AreEqual(0, economy.Fleet.OwnedBoatIds.Count, "two of three parts is still a broken boat");

            economy.TryContributeBoatPart(Host, BoatRepairParts.FuelTank, BoatPartSource.Found, request++);
            CollectionAssert.AreEqual(new[] { "boat-1" }, Owned(economy));
            Assert.AreEqual("boat-1", economy.ActiveVehicleId, "the repaired rowboat is the active one until another is chosen");

            Assert.AreEqual("InvalidTarget", economy.TryPurchaseVehicle(Host, VehicleIds.Rowboat, request++).ReasonCode,
                "the rowboat cannot be bought: no second sandal");
            CollectionAssert.AreEqual(new[] { "boat-1" }, Owned(economy));
        }

        // ---- purchase -------------------------------------------------------------------------------------------

        [Test]
        public void MotorboatThenResearchBoatAreBoughtInOrderAndMoneyNeverGoesNegative()
        {
            Seed(0, repaired: false);
            Assert.AreEqual("RequirementMissing", economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++).ReasonCode, "no repaired sandal yet");

            Seed(100, repaired: true);
            var poor = economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);
            Assert.AreEqual("InsufficientFunds", poor.ReasonCode);
            Assert.AreEqual(100, economy.SharedBalance, "a refused purchase takes nothing");

            Seed(VehicleCatalog.MotorboatPrice + VehicleCatalog.ResearchBoatPrice, repaired: true);
            Assert.AreEqual("RequirementMissing", economy.TryPurchaseVehicle(Host, VehicleIds.ResearchBoat, request++).ReasonCode,
                "the research boat needs the motorboat first even when the money is there");

            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++).Accepted);
            Assert.AreEqual(VehicleCatalog.ResearchBoatPrice, economy.SharedBalance);
            Assert.IsTrue(economy.TryPurchaseVehicle(Guest, VehicleIds.ResearchBoat, request++).Accepted, "any player at the vendor may buy for the campaign");
            Assert.AreEqual(0, economy.SharedBalance);
            CollectionAssert.AreEqual(new[] { "boat-1", "boat-2", "boat-3" }, Owned(economy));
        }

        [Test]
        public void BuyingDoesNotSwitchTheActiveBoatAndDoesNotPutAnythingInAPlayersLoadout()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++).Accepted);
            Assert.AreEqual("boat-1", economy.ActiveVehicleId, "selection is a separate, gated step");
            Assert.AreEqual(0, economy.LoadoutFor(Host).Count, "a vehicle is campaign property, not a personal item");
            Assert.AreEqual(0, economy.LoadoutFor(Guest).Count);
        }

        [Test]
        public void ReplayedAndDuplicatePurchasesChargeOnceAndCreateOneVehicle()
        {
            Seed(VehicleCatalog.MotorboatPrice * 3, repaired: true);
            var id = request++;
            var first = economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id);
            var replay = economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id);
            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(first.Accepted, replay.Accepted);
            Assert.AreEqual(first.Revision, replay.Revision, "the replay answers from the cache");
            Assert.AreEqual(VehicleCatalog.MotorboatPrice * 2, economy.SharedBalance, "one charge");

            var again = economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);
            Assert.AreEqual("AlreadyProcessed", again.ReasonCode);
            var otherPlayer = economy.TryPurchaseVehicle(Guest, VehicleIds.Motorboat, request++);
            Assert.AreEqual("AlreadyProcessed", otherPlayer.ReasonCode, "a second player cannot buy the same boat again");
            Assert.AreEqual(VehicleCatalog.MotorboatPrice * 2, economy.SharedBalance);
            Assert.AreEqual(2, economy.Fleet.OwnedBoatIds.Count);
        }

        [Test]
        public void UnknownVehicleIdsAreRefused()
        {
            Seed(10000, repaired: true);
            foreach (var bad in new[] { "", null, "boat-9", "tube-1", "BOAT-2" })
                Assert.AreEqual("InvalidTarget", economy.TryPurchaseVehicle(Host, bad, request++).ReasonCode, "id: " + bad);
            Assert.AreEqual(10000, economy.SharedBalance);
        }

        [Test]
        public void ADayThatIsClosingRefusesPurchaseAndSelectionAndTheSameRequestWorksAfterwards()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            var closing = true;
            DayLock.Bind(() => closing);
            var id = request++;
            Assert.AreEqual("DayClosing", economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id).ReasonCode);
            Assert.AreEqual(VehicleCatalog.MotorboatPrice, economy.SharedBalance);
            closing = false;
            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id).Accepted, "DayClosing is not cached against the request");

            closing = true;
            var selectId = request++;
            Assert.AreEqual("DayClosing", economy.TrySelectVehicle(Host, VehicleIds.Motorboat, selectId).ReasonCode);
            closing = false;
            Assert.IsTrue(economy.TrySelectVehicle(Host, VehicleIds.Motorboat, selectId).Accepted);
        }

        [Test]
        public void AFailedSaveRollsThePurchaseBackAndTheSameRequestCanBeRetried()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            var saveWorks = false;
            economy.SetPersistenceHandler(() => saveWorks);
            var id = request++;
            var failed = economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id);
            Assert.AreEqual("SaveFailed", failed.ReasonCode);
            Assert.AreEqual(VehicleCatalog.MotorboatPrice, economy.SharedBalance, "no money left without a vehicle");
            CollectionAssert.AreEqual(new[] { "boat-1" }, Owned(economy), "no vehicle without a charge");

            saveWorks = true;
            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, id).Accepted);
            Assert.AreEqual(0, economy.SharedBalance);
            CollectionAssert.AreEqual(new[] { "boat-1", "boat-2" }, Owned(economy));
        }

        // ---- active selection -----------------------------------------------------------------------------------

        [Test]
        public void SelectionNeedsAnOwnedVehicleAndExactlyOneIsActive()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            Assert.AreEqual("NotOwned", economy.TrySelectVehicle(Host, VehicleIds.Motorboat, request++).ReasonCode);
            Assert.AreEqual("NotOwned", economy.TrySelectVehicle(Host, VehicleIds.ResearchBoat, request++).ReasonCode);
            Assert.AreEqual("InvalidTarget", economy.TrySelectVehicle(Host, "boat-9", request++).ReasonCode);
            Assert.AreEqual("AlreadyProcessed", economy.TrySelectVehicle(Host, VehicleIds.Rowboat, request++).ReasonCode, "it is already the active one");

            economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);
            Assert.IsTrue(economy.TrySelectVehicle(Guest, VehicleIds.Motorboat, request++).Accepted);
            Assert.AreEqual("boat-2", economy.ActiveVehicleId);
            Assert.AreEqual("boat-2", economy.Fleet.ActiveBoatId);
            Assert.IsTrue(economy.TrySelectVehicle(Host, VehicleIds.Rowboat, request++).Accepted, "back to the sandal: ownership is kept, only the active one moves");
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);
            CollectionAssert.AreEqual(new[] { "boat-1", "boat-2" }, Owned(economy), "switching never deletes or duplicates a vehicle");
        }

        [Test]
        public void SwitchingIsRefusedWhileATripIsUnderWayOrSomeoneIsSeatedAndAllowedAgainOnceDocked()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);

            var reason = "TripActive";
            gateForCleanup = () => reason;
            VehicleSwitchGate.Bind(gateForCleanup);
            var id = request++;
            var refused = economy.TrySelectVehicle(Host, VehicleIds.Motorboat, id);
            Assert.AreEqual("TripActive", refused.ReasonCode);
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);

            reason = "SeatsOccupied";
            Assert.AreEqual("SeatsOccupied", economy.TrySelectVehicle(Host, VehicleIds.Motorboat, id).ReasonCode, "the gate answer changes on its own: not cached");

            reason = null;
            Assert.IsTrue(economy.TrySelectVehicle(Host, VehicleIds.Motorboat, id).Accepted, "same request id, boat docked and empty now");
        }

        [Test]
        public void AFailedSaveRollsTheSelectionBack()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);
            economy.SetPersistenceHandler(() => false);
            Assert.AreEqual("SaveFailed", economy.TrySelectVehicle(Host, VehicleIds.Motorboat, request++).ReasonCode);
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);
        }

        [Test]
        public void ActiveVehicleSeamFallsBackToTheRowboatWhenUnboundAndFollowsTheProviderWhenBound()
        {
            Assert.AreEqual("boat-1", ActiveVehicle.BoatId);
            Func<string> provider = () => "boat-3";
            ActiveVehicle.Bind(provider);
            Assert.AreEqual("boat-3", ActiveVehicle.BoatId);
            Assert.AreEqual(VehicleClass.ResearchBoat, ActiveVehicle.Class);
            ActiveVehicle.Unbind(provider);
            Assert.AreEqual("boat-1", ActiveVehicle.BoatId);
            Func<string> empty = () => "";
            ActiveVehicle.Bind(empty);
            Assert.AreEqual("boat-1", ActiveVehicle.BoatId, "an empty answer (nothing owned) never invents a vehicle");
            ActiveVehicle.Unbind(empty);
        }

        // ---- save / load ----------------------------------------------------------------------------------------

        [Test]
        public void OwnershipAndTheActiveSelectionSurviveASaveAndLoadThroughTheRealFileWithoutDuplicating()
        {
            Seed(VehicleCatalog.MotorboatPrice + VehicleCatalog.ResearchBoatPrice + 77, repaired: true);
            economy.SetPersistenceHandler(store.SaveNow);
            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++).Accepted);
            Assert.IsTrue(economy.TryPurchaseVehicle(Host, VehicleIds.ResearchBoat, request++).Accepted);
            Assert.IsTrue(economy.TrySelectVehicle(Host, VehicleIds.ResearchBoat, request++).Accepted);

            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
            Assert.AreEqual(EconomySaveData.CurrentSchemaVersion, disk.SchemaVersion);
            Assert.IsTrue(disk.HasFleet);
            CollectionAssert.AreEqual(new[] { "boat-2", "boat-3" }, disk.FleetPurchasedBoatIds, "the rowboat is not stored twice: its repair is the single source");
            Assert.AreEqual("boat-3", disk.FleetActiveBoatId);
            Assert.AreEqual(77, disk.SharedBalance);

            var reopened = new GameObject("reopened");
            try
            {
                reopened.AddComponent<InventoryManager>();
                var other = reopened.AddComponent<EconomyManager>();
                var otherStore = reopened.AddComponent<EconomySaveStore>();
                otherStore.SetPathForTests(path);
                Assert.IsTrue(otherStore.LoadNow());
                CollectionAssert.AreEqual(new[] { "boat-1", "boat-2", "boat-3" }, Owned(other));
                Assert.AreEqual("boat-3", other.ActiveVehicleId);
                Assert.AreEqual(77, other.SharedBalance);
                Assert.AreEqual("AlreadyProcessed", other.TryPurchaseVehicle(Host, VehicleIds.ResearchBoat, request++).ReasonCode, "a reopened game cannot rebuy");
                Assert.AreEqual(77, other.SharedBalance);
            }
            finally { UnityEngine.Object.DestroyImmediate(reopened); }
        }

        [Test]
        public void AnOlderFileWithoutFleetDataLoadsAsOnlyTheRepairedRowboat()
        {
            Seed(500, repaired: true, hasFleet: false);
            CollectionAssert.AreEqual(new[] { "boat-1" }, Owned(economy));
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);

            Seed(500, repaired: false, hasFleet: false);
            Assert.AreEqual(0, economy.Fleet.OwnedBoatIds.Count);
            Assert.AreEqual("", economy.ActiveVehicleId);
        }

        [Test]
        public void ATamperedFileCannotGrantVehiclesTheRulesWouldNotHave()
        {
            // research boat without the motorboat, an unknown id, the rowboat listed as bought, duplicates
            Seed(0, repaired: true, purchased: new[] { "boat-3", "boat-9", "boat-1", "boat-2", "boat-2" }, active: "boat-3");
            CollectionAssert.AreEqual(new[] { "boat-1", "boat-2", "boat-3" }, Owned(economy), "boat-2 is listed, so the chain boat-1 -> boat-2 -> boat-3 holds");

            Seed(0, repaired: true, purchased: new[] { "boat-3" }, active: "boat-3");
            CollectionAssert.AreEqual(new[] { "boat-1" }, Owned(economy), "the research boat without its motorboat is dropped");
            Assert.AreEqual("boat-1", economy.ActiveVehicleId, "an active id that is not owned falls back to the rowboat");

            Seed(0, repaired: false, purchased: new[] { "boat-2" }, active: "boat-2");
            Assert.AreEqual(0, economy.Fleet.OwnedBoatIds.Count, "no motorboat on a campaign whose sandal was never repaired");
            Assert.AreEqual("", economy.ActiveVehicleId);

            Seed(0, repaired: true, purchased: new[] { "boat-2" }, active: "boat-9");
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);
        }

        // ---- trip authority follows the active vehicle ------------------------------------------------------------

        private sealed class Roster : BoatTripManager.ISessionRoster
        {
            public bool IsConnected(PlayerId player) => true;
        }

        [Test]
        public void TheTripAuthorityBoardsOnlyTheActiveVehicleAndBlocksSwitchingWhileSomeoneIsSeated()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            economy.TryPurchaseVehicle(Host, VehicleIds.Motorboat, request++);
            var tripRoot = new GameObject("trip");
            try
            {
                var trip = tripRoot.AddComponent<BoatTripManager>();
                trip.Configure(() => economy.BoatRepair.Status, new Roster(), () => economy.ActiveVehicleId);
                Assert.AreEqual("boat-1", trip.State.BoatId);

                Assert.AreEqual("InvalidTarget", BoatBoarding.TryBoard(Host, VehicleIds.Motorboat, BoatTripIds.Seat0, request++).ReasonCode,
                    "the motorboat is owned but not the active one: it cannot be boarded");
                Assert.IsTrue(BoatBoarding.TryBoard(Host, VehicleIds.Rowboat, BoatTripIds.Seat0, request++).Accepted);

                Assert.AreEqual("SeatsOccupied", economy.TrySelectVehicle(Guest, VehicleIds.Motorboat, request++).ReasonCode, "the real trip gate: somebody is seated");
                Assert.AreEqual("boat-1", economy.ActiveVehicleId);

                Assert.IsTrue(BoatBoarding.TryDisembark(Host, request++).Accepted);
                Assert.IsTrue(economy.TrySelectVehicle(Guest, VehicleIds.Motorboat, request++).Accepted, "docked and empty");
                Assert.AreEqual("boat-2", trip.State.BoatId, "the trip authority now serves the motorboat");

                Assert.AreEqual("InvalidTarget", BoatBoarding.TryBoard(Host, VehicleIds.Rowboat, BoatTripIds.Seat0, request++).ReasonCode, "the sandal is parked now");
                Assert.IsTrue(BoatBoarding.TryBoard(Host, VehicleIds.Motorboat, BoatTripIds.Seat0, request++).Accepted);
                Assert.IsTrue(trip.TryStartRoute(Host, BoatTripIds.NearRouteId, request++).Accepted, "the motorboat needs no rowboat repair check of its own");
                Assert.AreEqual("boat-2-trip-1", trip.State.TripId);
                Assert.AreEqual("TripActive", economy.TrySelectVehicle(Guest, VehicleIds.Rowboat, request++).ReasonCode, "no switching while it sails");
                trip.Shutdown();
            }
            finally { UnityEngine.Object.DestroyImmediate(tripRoot); }
        }

        [Test]
        public void TheTripAuthorityWithoutAnActiveProviderKeepsTheP3RowboatBehaviour()
        {
            var tripRoot = new GameObject("trip");
            try
            {
                var trip = tripRoot.AddComponent<BoatTripManager>();
                trip.Configure(() => BoatRepairStatus.Repaired, new Roster());
                Assert.AreEqual("boat-1", trip.State.BoatId);
                Assert.IsTrue(BoatBoarding.TryBoard(Host, VehicleIds.Rowboat, BoatTripIds.Seat0, request++).Accepted);
                trip.Shutdown();
            }
            finally { UnityEngine.Object.DestroyImmediate(tripRoot); }
        }

        [Test]
        public void NothingCanBeBoardedWhileNoVehicleIsOwned()
        {
            var tripRoot = new GameObject("trip");
            try
            {
                var trip = tripRoot.AddComponent<BoatTripManager>();
                trip.Configure(() => economy.BoatRepair.Status, new Roster(), () => economy.ActiveVehicleId);
                Assert.AreEqual("", trip.State.BoatId);
                Assert.AreEqual("InvalidTarget", BoatBoarding.TryBoard(Host, VehicleIds.Rowboat, BoatTripIds.Seat0, request++).ReasonCode);
                trip.Shutdown();
            }
            finally { UnityEngine.Object.DestroyImmediate(tripRoot); }
        }

        // ---- the harbor vendor ---------------------------------------------------------------------------------

        private static readonly ServicePointDefinition Vendor = TownServiceCatalog.All.First(d => d.ServiceType == ServicePointType.VehicleVendor);
        private static readonly ServicePointDefinition Shop = TownServiceCatalog.All.First(d => d.ServiceType == ServicePointType.EquipmentShop);

        private TownServiceHandler Handler(Func<bool> inRange = null) =>
            new TownServiceHandler(economy, () => true, p => true, (p, def) => inRange == null || inRange());

        [Test]
        public void TheVendorIsPartOfTheServiceCatalogWithItsOwnIdAndType()
        {
            Assert.AreEqual(4, TownServiceCatalog.All.Count);
            Assert.AreEqual(TownServiceCatalog.VehicleVendorId, Vendor.ServiceId);
            Assert.IsTrue(TownServiceCatalog.Matches(Vendor));
            Assert.IsFalse(TownServiceCatalog.Matches(new ServicePointDefinition(Vendor.ServiceId, ServicePointType.EquipmentShop, "a", 3f, "c")),
                "a mislabelled anchor cannot act as the vendor");
        }

        [Test]
        public void VehiclesAreSoldOnlyAtTheVendorAndEquipmentOnlyAtTheEquipmentShop()
        {
            Seed(VehicleCatalog.MotorboatPrice + 1000, repaired: true);
            var handler = Handler();

            // equipment shop session: a boat is refused, a tube is fine
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(Host, Shop, request++));
            Assert.AreEqual("NotAtShop", handler.HandlePurchase(Host, VehicleIds.Motorboat, request++).ReasonCode);
            Assert.IsTrue(handler.HandlePurchase(Host, "tube-1", request++).Accepted);

            // vendor session: equipment is refused, the boat is fine
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(Host, Vendor, request++));
            Assert.IsTrue(handler.IsShopOpenFor(Host));
            Assert.AreEqual("NotAtShop", handler.HandlePurchase(Host, "tube-2", request++).ReasonCode);
            Assert.IsTrue(handler.HandlePurchase(Host, VehicleIds.Motorboat, request++).Accepted);
            Assert.AreEqual(1000 - 100, economy.SharedBalance);
        }

        [Test]
        public void SelectionRequiresAnOpenVendorSessionAndTheVendorInRange()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            var near = true;
            var handler = Handler(() => near);

            Assert.AreEqual("NotAtShop", handler.HandleSelectVehicle(Host, VehicleIds.Rowboat, request++).ReasonCode, "no session yet");
            handler.HandleInteraction(Host, Vendor, request++);
            handler.HandlePurchase(Host, VehicleIds.Motorboat, request++);

            near = false;
            Assert.AreEqual("NotAtShop", handler.HandleSelectVehicle(Host, VehicleIds.Motorboat, request++).ReasonCode, "walked away");
            Assert.AreEqual("boat-1", economy.ActiveVehicleId);

            near = true;
            handler.HandleInteraction(Host, Vendor, request++);
            Assert.IsTrue(handler.HandleSelectVehicle(Host, VehicleIds.Motorboat, request++).Accepted);
            Assert.AreEqual("boat-2", economy.ActiveVehicleId);

            handler.HandleInteraction(Host, Shop, request++);
            Assert.AreEqual("NotAtShop", handler.HandleSelectVehicle(Host, VehicleIds.Rowboat, request++).ReasonCode, "the equipment shop does not switch boats");
        }

        [Test]
        public void TheVendorPublishesTheOutcomeToTheActingPlayer()
        {
            Seed(VehicleCatalog.MotorboatPrice, repaired: true);
            var handler = Handler();
            var outcomes = new List<TownServiceOutcome>();
            handler.OutcomeReady += (player, outcome) => outcomes.Add(outcome);
            handler.HandleInteraction(Host, Vendor, request++);
            handler.HandlePurchase(Host, VehicleIds.ResearchBoat, request++);
            handler.HandlePurchase(Host, VehicleIds.Motorboat, request++);

            Assert.AreEqual("ShopOpen", outcomes[0].ReasonCode);
            Assert.AreEqual(ServicePointType.VehicleVendor, outcomes[0].ServiceType);
            Assert.AreEqual("RequirementMissing", outcomes[1].ReasonCode);
            Assert.IsTrue(outcomes[2].Accepted);
        }

        // ---- equipment tiers ---------------------------------------------------------------------------------------

        [Test]
        public void TheTierCatalogGivesTheSlotsAndLevelsMehmetResolves()
        {
            Seed(0, repaired: false);   // loads the catalog defaults
            void Expect(string id, string slot, int level)
            {
                Assert.IsTrue(economy.TryGetEquipmentDefinition(id, out var definition), id);
                Assert.AreEqual(slot, definition.Slot, id);
                Assert.AreEqual(level, definition.Level, id);
                Assert.Greater(definition.Price, 0, id);
            }
            Expect(EconomyManager.CameraBasicId, "camera", 1);
            Expect(EconomyManager.CameraAdvancedId, "camera", 2);
            Expect(EconomyManager.CameraProId, "camera", 3);
            Expect(EconomyManager.FinsId, "fins", 1);
            Expect(EconomyManager.BagId, "bag", 1);
            Expect(EconomyManager.HarpoonId, "harpoon", 1);
        }

        [Test]
        public void CameraTiersAreBoughtInOrderAndLandInTheBuyersOwnLoadoutOnly()
        {
            Seed(5000, repaired: false);
            Assert.AreEqual("RequirementMissing", economy.TryPurchase(Host, EconomyManager.CameraProId, request++).ReasonCode);
            Assert.AreEqual("RequirementMissing", economy.TryPurchase(Host, EconomyManager.CameraAdvancedId, request++).ReasonCode, "no basic camera yet");

            Assert.IsTrue(economy.TryPurchase(Guest, EconomyManager.CameraBasicId, request++).Accepted);
            Assert.AreEqual("RequirementMissing", economy.TryPurchase(Host, EconomyManager.CameraAdvancedId, request++).ReasonCode,
                "the guest's basic camera does not unlock the host's advanced one");

            Assert.IsTrue(economy.TryPurchase(Guest, EconomyManager.CameraAdvancedId, request++).Accepted);
            Assert.AreEqual("RequirementMissing", economy.TryPurchase(Host, EconomyManager.CameraProId, request++).ReasonCode);
            Assert.IsTrue(economy.TryPurchase(Guest, EconomyManager.CameraProId, request++).Accepted);

            CollectionAssert.AreEquivalent(new[] { EconomyManager.CameraBasicId, EconomyManager.CameraAdvancedId, EconomyManager.CameraProId }, economy.LoadoutFor(Guest));
            Assert.AreEqual(0, economy.LoadoutFor(Host).Count, "nothing leaked to the other player");
            Assert.AreEqual(5000 - 150 - 450 - 1100, economy.SharedBalance);
        }

        [Test]
        public void FinsBagAndHarpoonUpgradesAreOnePurchasePerPlayerAndSurviveTheFile()
        {
            Seed(5000, repaired: false);
            economy.SetPersistenceHandler(store.SaveNow);
            foreach (var id in new[] { EconomyManager.FinsId, EconomyManager.BagId, EconomyManager.HarpoonId })
            {
                Assert.IsTrue(economy.TryPurchase(Host, id, request++).Accepted, id);
                Assert.AreEqual("AlreadyProcessed", economy.TryPurchase(Host, id, request++).ReasonCode, id);
            }
            Assert.IsTrue(economy.TryPurchase(Guest, EconomyManager.FinsId, request++).Accepted, "each player buys their own");
            Assert.AreEqual(5000 - 220 - 260 - 300 - 220, economy.SharedBalance);

            Assert.IsTrue(store.LoadNow());
            CollectionAssert.AreEquivalent(new[] { EconomyManager.FinsId, EconomyManager.BagId, EconomyManager.HarpoonId }, economy.LoadoutFor(Host));
            Assert.AreEqual(0, economy.LoadoutFor(Guest).Count, "D06: session client ids are not persistent identities, only the host loadout is saved (same as tubes and cameras today)");
            Assert.AreEqual(5000 - 220 - 260 - 300 - 220, economy.SharedBalance, "a reload does not charge or grant again");
        }
    }
}
