using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.Session;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    // P3.2-C: NPC services, pending turn-ins, camera purchase, boat repair progress and save/load.
    public class TownServiceTests
    {
        private static readonly ServicePointDefinition Shop = TownServiceCatalog.All[0];
        private static readonly ServicePointDefinition FishBuyer = TownServiceCatalog.All[1];
        private static readonly ServicePointDefinition RecordingBuyer = TownServiceCatalog.All[2];

        private GameObject root;
        private SessionManager session;
        private InventoryManager inventory;
        private EconomyManager economy;
        private PlayerId alice, bob;
        private bool townOpen;
        private bool inShopRange;
        private readonly HashSet<PlayerId> served = new HashSet<PlayerId>();
        private TownServiceHandler handler;
        private ulong request;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("town-test");
            session = root.AddComponent<SessionManager>();
            inventory = root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            alice = new PlayerId(1);
            bob = new PlayerId(2);
            session.Initialize("room", "DiveTestArea");
            session.Join(alice);
            session.Join(bob);
            economy.SetPrice("fish-1", 50);

            townOpen = true;
            inShopRange = true;
            served.Clear();
            served.Add(alice);
            served.Add(bob);
            request = 100;
            handler = new TownServiceHandler(economy, () => townOpen, p => served.Contains(p),
                (p, def) => inShopRange);
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        private ulong NextRequest() => ++request;

        private static CaptureResult Capture(string id, int weight = 500, string species = "fish-1") =>
            new CaptureResult(id, "dive-1", species, weight, catchObjectId: 1);

        private void DiveAndReturn(params (PlayerId Player, string CaptureId)[] safe)
        {
            session.SetReady(alice, true);
            session.SetReady(bob, true);
            Assert.AreEqual(SessionActionResult.Ok, session.BeginPrep());
            Assert.AreEqual(SessionActionResult.Ok, session.BeginDive("dive-1"));
            foreach (var entry in safe)
            {
                Assert.AreEqual(InventoryActionResult.Ok, inventory.TryAddCatch(entry.Player, Capture(entry.CaptureId)));
                inventory.TryMarkSafeReturn(entry.Player);
            }
            Assert.AreEqual(SessionActionResult.Ok, session.BeginReturn());
        }

        private void FundViaRecording(string id, int quality = 4)
        {
            Assert.AreEqual(PlayerActionResult.Accepted, economy.TryQueueRecordingTurnIn(
                new RecordingResult(id, "dive-1", alice, "sea_bass", quality, 4f)));
            Assert.IsTrue(economy.TryTurnInRecordings(alice, NextRequest()).Accepted);
        }

        private EconomyManager RestoreInto(EconomySaveData snapshot)
        {
            Object.DestroyImmediate(root);
            root = new GameObject("town-restored");
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            Assert.IsTrue(economy.TryRestore(snapshot));
            economy.SetPrice("fish-1", 50); // prices are configuration, not saved state
            handler = new TownServiceHandler(economy, () => townOpen, p => served.Contains(p), (p, def) => inShopRange);
            return economy;
        }

        // ---- Catalog ----------------------------------------------------------------------

        [Test]
        public void CatalogDefinesExactlyThreeDistinctServicesWithUniqueAnchors()
        {
            Assert.AreEqual(3, TownServiceCatalog.All.Count);
            var ids = new HashSet<string>();
            var anchors = new HashSet<string>();
            var types = new HashSet<ServicePointType>();
            foreach (var definition in TownServiceCatalog.All)
            {
                Assert.IsTrue(ServiceInteractionRules.IsValid(definition), definition.ServiceId);
                ids.Add(definition.ServiceId);
                anchors.Add(definition.WorldAnchor);
                types.Add(definition.ServiceType);
            }
            Assert.AreEqual(3, ids.Count);
            Assert.AreEqual(3, anchors.Count);
            Assert.AreEqual(3, types.Count);
        }

        [Test]
        public void SceneDefinitionMustMatchCatalogIdAndType()
        {
            Assert.IsTrue(TownServiceCatalog.Matches(FishBuyer));
            Assert.IsFalse(TownServiceCatalog.Matches(new ServicePointDefinition(
                FishBuyer.ServiceId, ServicePointType.EquipmentShop, "a", 3f, "c")));
            Assert.IsFalse(TownServiceCatalog.Matches(new ServicePointDefinition(
                "shop-unknown", ServicePointType.FishBuyer, "a", 3f, "c")));
        }

        // ---- Fish buyer -------------------------------------------------------------------

        [Test]
        public void SafeReturnAloneProducesNoMoneyOnlyAPendingItem()
        {
            DiveAndReturn((alice, "c1"));
            Assert.AreEqual(0, economy.SharedBalance);
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));
        }

        [Test]
        public void FishBuyerInteractionPaysOnceAndReportsTheOutcome()
        {
            DiveAndReturn((alice, "c1"));
            TownServiceOutcome? outcome = null;
            handler.OutcomeReady += (p, o) => outcome = o;

            var result = handler.HandleInteraction(alice, FishBuyer, NextRequest());

            Assert.AreEqual(PlayerActionResult.Accepted, result);
            Assert.AreEqual(50, economy.SharedBalance);
            Assert.IsNotNull(outcome);
            Assert.AreEqual(50, outcome.Value.Amount);
            Assert.AreEqual(1, outcome.Value.ItemCount);
            Assert.IsTrue(outcome.Value.Accepted);
        }

        [Test]
        public void HandingTheSameCatchToTheBuyerTwiceNeverPaysTwice()
        {
            DiveAndReturn((alice, "c1"));
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            Assert.AreEqual(PlayerActionResult.Rejected, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void ResendingTheSameRequestIdDoesNotPayAgain()
        {
            DiveAndReturn((alice, "c1"));
            var id = NextRequest();
            handler.HandleInteraction(alice, FishBuyer, id);
            handler.HandleInteraction(alice, FishBuyer, id);
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void ADiverCannotSellAnotherDiversCatch()
        {
            DiveAndReturn((alice, "c1"));
            Assert.AreEqual(PlayerActionResult.Rejected, handler.HandleInteraction(bob, FishBuyer, NextRequest()));
            Assert.AreEqual(0, economy.SharedBalance);
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));
        }

        [Test]
        public void WrongNpcDoesNotAcceptTheProduct()
        {
            DiveAndReturn((alice, "c1"));
            economy.TryQueueRecordingTurnIn(new RecordingResult("r1", "dive-1", alice, "sea_bass", 3, 4f));

            // The recording buyer must not take fish, and the fish buyer must not take recordings.
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(alice, RecordingBuyer, NextRequest()));
            Assert.AreEqual(100, economy.SharedBalance, "only the recording was paid");
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            Assert.AreEqual(150, economy.SharedBalance);
        }

        [Test]
        public void MisconfiguredAnchorCannotActAsAnotherService()
        {
            DiveAndReturn((alice, "c1"));
            var fake = new ServicePointDefinition(FishBuyer.ServiceId, ServicePointType.RecordingBuyer, "a", 3f, "c");
            Assert.AreEqual(PlayerActionResult.InvalidTarget, handler.HandleInteraction(alice, fake, NextRequest()));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void TownServicesAreClosedWhileDivingAndForInactivePlayers()
        {
            DiveAndReturn((alice, "c1"));
            townOpen = false;
            Assert.AreEqual(PlayerActionResult.InvalidState, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            townOpen = true;
            served.Remove(alice);
            Assert.AreEqual(PlayerActionResult.InvalidState, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void SaleRollsBackOnDiskFailureAndCanBeRetriedWithTheSameRequest()
        {
            DiveAndReturn((alice, "c1"));
            economy.SetPersistenceHandler(() => false);
            var id = NextRequest();

            Assert.AreEqual(PlayerActionResult.Rejected, handler.HandleInteraction(alice, FishBuyer, id));
            Assert.AreEqual(0, economy.SharedBalance, "no success/money without a durable save");
            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));

            economy.SetPersistenceHandler(null);
            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(alice, FishBuyer, id));
            Assert.AreEqual(50, economy.SharedBalance);
        }

        // ---- Escrow / two players ---------------------------------------------------------

        [Test]
        public void GuestCarriedItemsBecomeSharedEscrowAcrossSaveAndPayOnlyOnce()
        {
            DiveAndReturn((alice, "c1"));
            var snapshot = economy.ExportSaveData("camp", "cp");
            Assert.AreEqual(1, snapshot.PendingTurnIns.Count);
            Assert.IsTrue(snapshot.PendingTurnIns[0].SharedEscrow, "a guest id is not a persistent identity");

            RestoreInto(snapshot);
            Assert.AreEqual(1, economy.PendingCountFor(bob, TurnInKind.Catch), "escrow can be handed in by anyone");

            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(bob, FishBuyer, NextRequest()));
            Assert.AreEqual(PlayerActionResult.Rejected, handler.HandleInteraction(alice, FishBuyer, NextRequest()));
            Assert.AreEqual(50, economy.SharedBalance, "two divers, one item, one payment");
        }

        [Test]
        public void SharedBalanceIsSharedByTheTeam()
        {
            DiveAndReturn((alice, "c1"), (bob, "c2"));
            handler.HandleInteraction(alice, FishBuyer, NextRequest());
            handler.HandleInteraction(bob, FishBuyer, NextRequest());
            Assert.AreEqual(100, economy.SharedBalance);
        }

        [Test]
        public void RequestIdsOfDifferentOperationsDoNotCollide()
        {
            DiveAndReturn((alice, "c1"));
            FundViaRecording("r-fund");
            // The purchase RPC and the interaction request use independent per-player counters.
            Assert.IsTrue(economy.TryPurchase(alice, "tube-1", requestId: 1).Accepted);
            var sale = economy.TrySellCatches(alice, requestId: 1);
            Assert.IsTrue(sale.Accepted, "sell request 1 must not replay the purchase result");
            Assert.AreEqual(50, sale.Earned);
        }

        [Test]
        public void PendingItemsSurviveIntoALaterSessionWithoutTheInventory()
        {
            DiveAndReturn((alice, "c1"));
            RestoreInto(economy.ExportSaveData("camp", "cp")); // fresh inventory knows no captures

            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Catch));
            Assert.IsTrue(economy.TrySellCatches(alice, NextRequest()).Accepted);
            Assert.AreEqual(50, economy.SharedBalance);
        }

        [Test]
        public void RestoreNeverResurrectsAnAlreadyPaidItem()
        {
            DiveAndReturn((alice, "c1"));
            economy.TrySellCatches(alice, NextRequest());
            var snapshot = economy.ExportSaveData("camp", "cp");
            snapshot.PendingTurnIns.Add(new PendingTurnInSave
            {
                ItemId = "c1", Kind = (byte)TurnInKind.Catch, SubjectId = "fish-1", SharedEscrow = true
            });

            RestoreInto(snapshot);

            Assert.AreEqual(0, economy.PendingCountFor(alice, TurnInKind.Catch));
            Assert.AreEqual(50, economy.SharedBalance);
        }

        // ---- Equipment shop / camera ------------------------------------------------------

        [Test]
        public void ShopPurchaseNeedsAnOpenShopSession()
        {
            FundViaRecording("r-fund");
            var denied = handler.HandlePurchase(alice, "tube-1", NextRequest());
            Assert.IsFalse(denied.Accepted);
            Assert.AreEqual("NotAtShop", denied.ReasonCode);

            Assert.AreEqual(PlayerActionResult.Accepted, handler.HandleInteraction(alice, Shop, NextRequest()));
            Assert.IsTrue(handler.HandlePurchase(alice, "tube-1", NextRequest()).Accepted);
            CollectionAssert.Contains(economy.LoadoutFor(alice), "tube-1");
        }

        [Test]
        public void LeavingTheShopClosesItAndBlocksPurchases()
        {
            FundViaRecording("r-fund");
            handler.HandleInteraction(alice, Shop, NextRequest());
            var closed = new List<PlayerId>();
            handler.ShopClosed += closed.Add;

            inShopRange = false;
            handler.Tick();

            CollectionAssert.Contains(closed, alice);
            Assert.IsFalse(handler.IsShopOpenFor(alice));
            Assert.AreEqual("NotAtShop", handler.HandlePurchase(alice, "tube-1", NextRequest()).ReasonCode);
        }

        [Test]
        public void DivePhaseClosesTheShop()
        {
            handler.HandleInteraction(alice, Shop, NextRequest());
            townOpen = false;
            handler.Tick();
            Assert.IsFalse(handler.IsShopOpenFor(alice));
            Assert.AreEqual("WrongPhase", handler.HandlePurchase(alice, "tube-1", NextRequest()).ReasonCode);
        }

        [Test]
        public void BasicCameraIsASeparatePurchaseNotAFreeDefault()
        {
            Assert.IsTrue(economy.TryGetEquipmentDefinition(EconomyManager.CameraBasicId, out var camera));
            Assert.AreEqual("camera", camera.Slot);
            CollectionAssert.IsEmpty(economy.LoadoutFor(alice));
            Assert.IsFalse(DiverEquipmentRules.OwnsEquipmentSlot(alice, economy.LoadoutStateFor(alice),
                new[] { camera }, "camera"), "no camera before buying it");
        }

        [Test]
        public void BoughtCameraIsAllocatedOnlyToTheBuyingPlayer()
        {
            FundViaRecording("r-fund", quality: 4);
            handler.HandleInteraction(alice, Shop, NextRequest());
            var bought = handler.HandlePurchase(alice, EconomyManager.CameraBasicId, NextRequest());
            Assert.IsTrue(bought.Accepted);
            Assert.AreEqual(50, economy.SharedBalance);

            economy.TryGetEquipmentDefinition(EconomyManager.CameraBasicId, out var camera);
            Assert.IsTrue(DiverEquipmentRules.OwnsEquipmentSlot(alice, economy.LoadoutStateFor(alice),
                new[] { camera }, "camera"));
            Assert.IsFalse(DiverEquipmentRules.OwnsEquipmentSlot(bob, economy.LoadoutStateFor(bob),
                new[] { camera }, "camera"), "bob must not get alice's camera");
            Assert.IsFalse(DiverEquipmentRules.OwnsEquipmentSlot(bob, economy.LoadoutStateFor(alice),
                new[] { camera }, "camera"), "a loadout for another player never applies");
        }

        [Test]
        public void CameraCannotBeBoughtWithoutFundsOrTwice()
        {
            handler.HandleInteraction(alice, Shop, NextRequest());
            Assert.AreEqual("InsufficientFunds",
                handler.HandlePurchase(alice, EconomyManager.CameraBasicId, NextRequest()).ReasonCode);
            Assert.AreEqual(0, economy.SharedBalance);

            FundViaRecording("r1"); FundViaRecording("r2");
            Assert.IsTrue(handler.HandlePurchase(alice, EconomyManager.CameraBasicId, NextRequest()).Accepted);
            var balance = economy.SharedBalance;
            Assert.AreEqual("AlreadyProcessed",
                handler.HandlePurchase(alice, EconomyManager.CameraBasicId, NextRequest()).ReasonCode);
            Assert.AreEqual(balance, economy.SharedBalance);
        }

        // ---- Boat repair ------------------------------------------------------------------

        [Test]
        public void BoatStartsBrokenWithThreeFixedParts()
        {
            var boat = economy.BoatRepair;
            Assert.AreEqual(BoatRepairStatus.Broken, boat.Status);
            Assert.AreEqual(3, boat.RequiredPartIds.Count);
            Assert.AreEqual(0, boat.CompletedPartIds.Count);
        }

        [Test]
        public void FoundAndBoughtPartsAdvanceTheSameProgressAndNeverRepeat()
        {
            var found = economy.TryContributeBoatPart(alice, BoatRepairParts.Hull, BoatPartSource.Found, NextRequest());
            Assert.IsTrue(found.Accepted);
            Assert.AreEqual(0, economy.SharedBalance, "a free part costs nothing");
            Assert.AreEqual(BoatRepairStatus.InProgress, economy.BoatRepair.Status);

            FundViaRecording("r-fund");
            var before = economy.SharedBalance;
            var again = economy.TryContributeBoatPart(alice, BoatRepairParts.Hull, BoatPartSource.Purchased, NextRequest());
            Assert.AreEqual("AlreadyProcessed", again.ReasonCode);
            Assert.AreEqual(before, economy.SharedBalance, "buying an already-installed part must not charge");

            var bought = economy.TryContributeBoatPart(alice, BoatRepairParts.Engine, BoatPartSource.Purchased, NextRequest());
            Assert.IsTrue(bought.Accepted);
            Assert.AreEqual(before - economy.BoatPartPrice, economy.SharedBalance);
            Assert.AreEqual(2, economy.BoatRepair.CompletedPartIds.Count);
        }

        [Test]
        public void ThirdPartRepairsTheBoat()
        {
            foreach (var part in BoatRepairParts.All)
                Assert.IsTrue(economy.TryContributeBoatPart(alice, part, BoatPartSource.Found, NextRequest()).Accepted);
            Assert.AreEqual(BoatRepairStatus.Repaired, economy.BoatRepair.Status);
        }

        [Test]
        public void UnknownPartAndUnaffordablePurchaseAreRejectedWithoutSideEffects()
        {
            Assert.AreEqual("InvalidTarget",
                economy.TryContributeBoatPart(alice, "boat-part-wing", BoatPartSource.Found, NextRequest()).ReasonCode);
            Assert.AreEqual("InsufficientFunds",
                economy.TryContributeBoatPart(alice, BoatRepairParts.Hull, BoatPartSource.Purchased, NextRequest()).ReasonCode);
            Assert.AreEqual(0, economy.BoatRepair.CompletedPartIds.Count);
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void PartPurchaseRollsBackOnDiskFailure()
        {
            FundViaRecording("r-fund");
            var balance = economy.SharedBalance;
            economy.SetPersistenceHandler(() => false);

            var result = economy.TryContributeBoatPart(alice, BoatRepairParts.Hull, BoatPartSource.Purchased, NextRequest());

            Assert.AreEqual("SaveFailed", result.ReasonCode);
            Assert.AreEqual(balance, economy.SharedBalance);
            Assert.AreEqual(0, economy.BoatRepair.CompletedPartIds.Count);
        }

        [Test]
        public void ShopSellsBoatPartsThroughTheSameGateAsEquipment()
        {
            FundViaRecording("r-fund");
            Assert.AreEqual("NotAtShop",
                handler.HandlePurchase(alice, BoatRepairParts.Hull, NextRequest()).ReasonCode);
            handler.HandleInteraction(alice, Shop, NextRequest());
            Assert.IsTrue(handler.HandlePurchase(alice, BoatRepairParts.Hull, NextRequest()).Accepted);
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
        }

        [Test]
        public void FoundPartClaimGoesThroughTheSeamOnlyWhileBound()
        {
            Assert.IsFalse(BoatPartClaim.IsBound);
            Assert.AreEqual("InvalidState", BoatPartClaim.TryClaimFound(alice, BoatRepairParts.Hull, 1).ReasonCode);

            System.Func<PlayerId, string, ulong, TransactionResult> bound = handler.HandleFoundPart;
            BoatPartClaim.Bind(bound);
            try
            {
                Assert.IsTrue(BoatPartClaim.TryClaimFound(alice, BoatRepairParts.Hull, 2).Accepted);
                Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            }
            finally { BoatPartClaim.Unbind(bound); }
            Assert.IsFalse(BoatPartClaim.IsBound);
        }

        // ---- Save / load ------------------------------------------------------------------

        [Test]
        public void SaveLoadKeepsMoneyCameraPendingAndBoatProgressWithoutDuplication()
        {
            DiveAndReturn((alice, "c1"));
            FundViaRecording("r-fund");                       // +200
            var host = new PlayerId(0);
            Assert.IsTrue(economy.TryPurchase(host, EconomyManager.CameraBasicId, NextRequest()).Accepted); // -150
            economy.TryContributeBoatPart(host, BoatRepairParts.Engine, BoatPartSource.Found, NextRequest());
            economy.TryQueueRecordingTurnIn(new RecordingResult("r1", "dive-1", alice, "sea_bass", 2, 4f));
            var balance = economy.SharedBalance;

            RestoreInto(economy.ExportSaveData("camp", "cp"));

            Assert.AreEqual(balance, economy.SharedBalance);
            CollectionAssert.Contains(economy.LoadoutFor(host), EconomyManager.CameraBasicId);
            CollectionAssert.Contains(economy.BoatRepair.CompletedPartIds, BoatRepairParts.Engine);
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.AreEqual(1, economy.PendingCountFor(host, TurnInKind.Catch));
            Assert.AreEqual(1, economy.PendingCountFor(host, TurnInKind.Recording));

            // Paid ids are remembered: a paid recording can never be queued again.
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, economy.TryQueueRecordingTurnIn(
                new RecordingResult("r-fund", "dive-1", alice, "sea_bass", 4, 4f)));
        }

        [Test]
        public void HostCarriedItemStaysHostCarriedButGuestItemsBecomeEscrow()
        {
            economy.TryQueueRecordingTurnIn(new RecordingResult("host-rec", "d", new PlayerId(0), "sea_bass", 3, 4f));
            economy.TryQueueRecordingTurnIn(new RecordingResult("guest-rec", "d", alice, "sea_bass", 3, 4f));

            RestoreInto(economy.ExportSaveData("camp", "cp"));

            Assert.AreEqual(1, economy.PendingCountFor(alice, TurnInKind.Recording), "guest sees only the shared escrow item");
            Assert.AreEqual(2, economy.PendingCountFor(new PlayerId(0), TurnInKind.Recording), "host sees own + escrow");
            Assert.AreEqual(1, economy.PendingCountFor(new PlayerId(9), TurnInKind.Recording),
                "a stranger only sees the escrow item, not the host's own");
        }

        [Test]
        public void SchemaV1SavesStillLoadWithNothingPendingAndABrokenBoat()
        {
            var v1 = new EconomySaveData { SchemaVersion = 1, SharedBalance = 75, Revision = 4 };
            v1.PendingTurnIns = null;
            v1.BoatPartIds = null;

            Assert.IsTrue(economy.TryRestore(v1));

            Assert.AreEqual(75, economy.SharedBalance);
            Assert.AreEqual(0, economy.PendingTurnIns().Count);
            Assert.AreEqual(BoatRepairStatus.Broken, economy.BoatRepair.Status);
        }

        [Test]
        public void FutureOrCorruptSchemaIsRefusedAndKeepsCurrentState()
        {
            FundViaRecording("r-fund");
            Assert.IsFalse(economy.TryRestore(new EconomySaveData { SchemaVersion = 99 }));
            Assert.IsFalse(economy.TryRestore(new EconomySaveData { SchemaVersion = 0 }));
            Assert.IsFalse(economy.TryRestore(new EconomySaveData { SharedBalance = -5 }));
            Assert.AreEqual(200, economy.SharedBalance);
        }

        [Test]
        public void RestoreIgnoresUnknownPartsAndMalformedPendingEntries()
        {
            var data = new EconomySaveData();
            data.BoatPartIds.Add("boat-part-wing");
            data.BoatPartIds.Add(BoatRepairParts.Hull);
            data.BoatPartIds.Add(BoatRepairParts.Hull);
            data.PendingTurnIns.Add(null);
            data.PendingTurnIns.Add(new PendingTurnInSave { ItemId = "", Kind = (byte)TurnInKind.Catch });
            data.PendingTurnIns.Add(new PendingTurnInSave { ItemId = "x", Kind = 9 });
            data.PendingTurnIns.Add(new PendingTurnInSave { ItemId = "ok", Kind = (byte)TurnInKind.Catch, SubjectId = "fish-1" });

            Assert.IsTrue(economy.TryRestore(data));

            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.AreEqual(1, economy.PendingTurnIns().Count);
        }

        [Test]
        public void MoneyNeverGoesNegativeAcrossTownActions()
        {
            FundViaRecording("r-fund");                        // 200
            handler.HandleInteraction(alice, Shop, NextRequest());
            handler.HandlePurchase(alice, EconomyManager.CameraBasicId, NextRequest());   // 50 left
            handler.HandlePurchase(alice, BoatRepairParts.Hull, NextRequest());           // needs 120: rejected
            handler.HandlePurchase(alice, "tube-2", NextRequest());                        // needs 250: rejected
            Assert.AreEqual(50, economy.SharedBalance);
            Assert.GreaterOrEqual(economy.SharedBalance, 0);
        }
    }
}
