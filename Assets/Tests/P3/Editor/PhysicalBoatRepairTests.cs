using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3.Tests
{
    public class PhysicalBoatRepairTests
    {
        private GameObject root;
        private EconomyManager economy;
        private BoatRepairWorkSession work;
        private readonly PlayerId alice = new PlayerId(1), bob = new PlayerId(2);
        private ulong request;
        [SetUp] public void Setup()
        {
            root=new GameObject("physical-repair-test"); economy=root.AddComponent<EconomyManager>(); request=0;
            work=new BoatRepairWorkSession(i=>Contains(economy.UnusedBoatParts,i),i=>Contains(economy.BoatRepair.CompletedPartIds,i),
                (p,i,r)=>economy.TryAcquireBoatPart(p,BoatRepairParts.All[i],BoatPartSource.Found,r),
                (p,i,r)=>economy.TryFinishBoatPart(p,BoatRepairParts.All[i],r));
        }
        private static bool Contains(IReadOnlyList<string> list,int i)
        {foreach(var id in list)if(id==BoatRepairParts.All[i])return true;return false;}
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);
        private TransactionResult Do(BoatRepairCommand c,int i=0,double time=0)=>work.Execute(alice,c,i,++request,time);
        [Test] public void PickupAndTwoStrokesCannotUnlockBoat_FinalStrokeConsumesOnlyOneMaterial()
        {
            Assert.IsTrue(Do(BoatRepairCommand.Grab).Accepted);
            Assert.AreEqual(0,economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsTrue(Do(BoatRepairCommand.Place).Accepted);
            Assert.IsTrue(Do(BoatRepairCommand.Hammer,0,1).Accepted);
            Assert.IsTrue(Do(BoatRepairCommand.Hammer,0,2).Accepted);
            Assert.AreEqual(0,economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsTrue(Do(BoatRepairCommand.Hammer,0,3).Accepted);
            Assert.IsFalse(work.TryGet(alice,out _));Assert.AreEqual(1,economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsEmpty(economy.UnusedBoatParts);
        }
        [Test] public void ReservationRejectsOtherPlayer_AndCancelKeepsMaterialForRecovery()
        {
            Do(BoatRepairCommand.Grab);
            Assert.AreEqual("PartInUse",work.Execute(bob,BoatRepairCommand.Grab,0,1,0).ReasonCode);
            Do(BoatRepairCommand.Cancel);
            Assert.AreEqual(1,economy.UnusedBoatParts.Count);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Grab,0,2,0).Accepted);
            Assert.AreEqual(1,economy.UnusedBoatParts.Count);
        }
        [Test] public void DifferentSlotsDoNotLockOneAnother()
        {
            Assert.IsTrue(Do(BoatRepairCommand.Grab).Accepted);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Grab,1,1,0).Accepted);
            Assert.IsTrue(Do(BoatRepairCommand.Place).Accepted);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Place,1,2,0).Accepted);
            Assert.IsTrue(Do(BoatRepairCommand.Hammer,0,1).Accepted);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Hammer,1,3,1).Accepted);
            Assert.IsTrue(work.TryGet(alice,out var a));Assert.IsTrue(work.TryGet(bob,out var b));
            Assert.AreEqual(1,a.Hits);Assert.AreEqual(1,b.Hits);
        }
        [Test] public void DisconnectReleaseReturnsMaterialAndDiscardsPartialStrokes()
        {
            Do(BoatRepairCommand.Grab);Do(BoatRepairCommand.Place);Do(BoatRepairCommand.Hammer,0,1);
            work.Release(alice);
            Assert.AreEqual(1,economy.UnusedBoatParts.Count);Assert.IsEmpty(economy.BoatRepair.CompletedPartIds);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Grab,0,1,2).Accepted);
            Assert.IsTrue(work.Execute(bob,BoatRepairCommand.Place,0,2,2).Accepted);
            work.TryGet(bob,out var state);Assert.AreEqual(0,state.Hits);
        }
        [Test] public void FourPlayersContendForOnePart_OnlyOneCompletionIsRecorded()
        {
            // Host RPCs are serialized; all four clients submit their first request in the same simulated tick.
            var players = new[] {alice, bob, new PlayerId(3), new PlayerId(4)};
            var winners = 0;
            foreach (var player in players)
            {
                var result = work.Execute(player, BoatRepairCommand.Grab, 0, 1, 10);
                if (result.Accepted) winners++;
                else Assert.AreEqual("PartInUse", result.ReasonCode);
            }
            Assert.AreEqual(1, winners);
            Assert.IsTrue(work.IsReserved(0));
            Assert.AreEqual(1, economy.UnusedBoatParts.Count);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Place, 0, 2, 10).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 3, 11).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 4, 12).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 5, 13).Accepted);

            Assert.IsFalse(work.IsReserved(0));
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsEmpty(economy.UnusedBoatParts);
            for (int i = 1; i < players.Length; i++)
            {
                // An old rejected RPC must not become a success after the material state changes.
                Assert.AreEqual("PartInUse",
                    work.Execute(players[i], BoatRepairCommand.Grab, 0, 1, 14).ReasonCode);
                Assert.AreEqual("AlreadyProcessed",
                    work.Execute(players[i], BoatRepairCommand.Grab, 0, 2, 14).ReasonCode);
            }
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Grab, 1, 3, 15).Accepted);
        }

        [Test] public void DisconnectReleasesOneReservationWithoutInterruptingAnotherSlot()
        {
            var charlie = new PlayerId(3);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Grab, 0, 1, 0).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Grab, 1, 1, 0).Accepted);
            Assert.AreEqual("PartInUse", work.Execute(charlie, BoatRepairCommand.Grab, 0, 1, 0).ReasonCode);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Place, 0, 2, 0).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Place, 1, 2, 0).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 3, 1).Accepted);
            work.Release(alice); // Simulates the host's cleanup when this guest disconnects.
            Assert.IsTrue(work.IsReserved(1));
            Assert.AreEqual("PartInUse", work.Execute(charlie, BoatRepairCommand.Grab, 0, 1, 2).ReasonCode);
            Assert.IsTrue(work.Execute(charlie, BoatRepairCommand.Grab, 0, 2, 2).Accepted);
            Assert.IsTrue(work.Execute(charlie, BoatRepairCommand.Place, 0, 3, 2).Accepted);
            Assert.IsTrue(work.TryGet(charlie, out var recovered));
            Assert.AreEqual(0, recovered.Hits); // The disconnected worker's partial strokes do not transfer.

            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 1, 3, 3).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 1, 4, 4).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 1, 5, 5).Accepted);
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsTrue(work.TryGet(charlie, out _));
            Assert.IsTrue(work.Execute(charlie, BoatRepairCommand.Hammer, 0, 4, 6).Accepted);
            Assert.IsTrue(work.Execute(charlie, BoatRepairCommand.Hammer, 0, 5, 7).Accepted);
            Assert.IsTrue(work.Execute(charlie, BoatRepairCommand.Hammer, 0, 6, 8).Accepted);
            Assert.AreEqual(2, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsEmpty(economy.UnusedBoatParts);
        }

        [Test] public void ClosingDayAfterTwoStrokes_KeepsPurchasedMaterialAndNoFinishedRepairOnReload()
        {
            // Simulate the host's closing-day cleanup: unfinished transient work goes away,
            // but acquisition, one-time purchase cost, and completed parts are durable.
            Assert.IsTrue(economy.TryRestore(new EconomySaveData { SharedBalance = 240 }));
            Assert.IsTrue(economy.TryAcquireBoatPart(alice, BoatRepairParts.Hull, BoatPartSource.Purchased, 100).Accepted);
            Assert.AreEqual(120, economy.SharedBalance);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Grab, 0, 1, 0).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Place, 0, 2, 0).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 3, 1).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 4, 2).Accepted);
            work.Release(alice); // BoatRepairNetworkBinding.Update when DayLock.IsLocked.
            Assert.IsFalse(work.IsReserved(0));
            Assert.IsEmpty(economy.BoatRepair.CompletedPartIds);
            var snapshot = JsonUtility.FromJson<EconomySaveData>(
                JsonUtility.ToJson(economy.ExportSaveData("day-close", "checkpoint")));

            Object.DestroyImmediate(root);
            root = new GameObject("next-day-repair-test");
            economy = root.AddComponent<EconomyManager>();
            Assert.IsTrue(economy.TryRestore(snapshot));
            Assert.AreEqual(120, economy.SharedBalance);
            Assert.IsEmpty(economy.BoatRepair.CompletedPartIds);
            Assert.AreEqual(1, economy.UnusedBoatParts.Count);
            work = new BoatRepairWorkSession(i => Contains(economy.UnusedBoatParts, i),
                i => Contains(economy.BoatRepair.CompletedPartIds, i),
                (p, i, r) => economy.TryAcquireBoatPart(p, BoatRepairParts.All[i], BoatPartSource.Found, r),
                (p, i, r) => economy.TryFinishBoatPart(p, BoatRepairParts.All[i], r));
            Assert.IsFalse(work.TryGet(alice, out _));
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Grab, 0, 1, 3).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Place, 0, 2, 3).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 3, 4).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 4, 5).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 5, 6).Accepted);
            Assert.AreEqual(120, economy.SharedBalance); // No second purchase.
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsEmpty(economy.UnusedBoatParts);
        }

        [Test] public void FailedFinalSaveThenGuestDisconnect_DoesNotLoseMaterialOrCarryPartialHits()
        {
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Grab, 0, 1, 0).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Place, 0, 2, 0).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 3, 1).Accepted);
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 4, 2).Accepted);
            economy.SetPersistenceHandler(() => false);
            Assert.AreEqual("SaveFailed",
                work.Execute(alice, BoatRepairCommand.Hammer, 0, 5, 3).ReasonCode);
            Assert.AreEqual(1, economy.UnusedBoatParts.Count);
            Assert.IsEmpty(economy.BoatRepair.CompletedPartIds);
            work.Release(alice); // Guest disconnected before its next save attempt.
            Assert.IsFalse(work.IsReserved(0));
            economy.SetPersistenceHandler(() => true);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Grab, 0, 1, 4).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Place, 0, 2, 4).Accepted);
            Assert.IsTrue(work.TryGet(bob, out var recovered));
            Assert.AreEqual(0, recovered.Hits);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 3, 5).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 4, 6).Accepted);
            Assert.IsTrue(work.Execute(bob, BoatRepairCommand.Hammer, 0, 5, 7).Accepted);
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
            Assert.IsEmpty(economy.UnusedBoatParts);
            // A stale request from the old guest cannot complete or consume a second time.
            Assert.IsTrue(work.Execute(alice, BoatRepairCommand.Hammer, 0, 5, 8).Accepted == false);
            Assert.AreEqual(1, economy.BoatRepair.CompletedPartIds.Count);
        }

        [Test] public void WrongSlot_UnplacedHammer_AndFastStrokesAreRejected()
        {
            Do(BoatRepairCommand.Grab);
            Assert.AreEqual("WrongPart",Do(BoatRepairCommand.Place,1).ReasonCode);
            Assert.AreEqual("NotPlaced",Do(BoatRepairCommand.Hammer).ReasonCode);
            Do(BoatRepairCommand.Place);Do(BoatRepairCommand.Hammer,0,1);
            Assert.AreEqual("TooFast",Do(BoatRepairCommand.Hammer,0,1.1).ReasonCode);
            Assert.IsTrue(work.TryGet(alice,out var state));Assert.AreEqual(1,state.Hits);
        }
        [Test] public void ReplayedStrokeDoesNotHitAgain_AndChangedPayloadIsRejected()
        {
            Do(BoatRepairCommand.Grab);Do(BoatRepairCommand.Place);Do(BoatRepairCommand.Hammer,0,1);
            Assert.IsTrue(work.Execute(alice,BoatRepairCommand.Hammer,0,request,2).Accepted);
            Assert.AreEqual("RequestMismatch",work.Execute(alice,BoatRepairCommand.Place,0,request,2).ReasonCode);
            work.TryGet(alice,out var state);Assert.AreEqual(1,state.Hits);
        }
        [Test] public void FailedFinalSaveRollsBackAndSameStrokeCanRetry()
        {
            Do(BoatRepairCommand.Grab);Do(BoatRepairCommand.Place);Do(BoatRepairCommand.Hammer,0,1);Do(BoatRepairCommand.Hammer,0,2);
            economy.SetPersistenceHandler(()=>false);
            Assert.AreEqual("SaveFailed",Do(BoatRepairCommand.Hammer,0,3).ReasonCode);
            Assert.AreEqual(1,economy.UnusedBoatParts.Count);Assert.AreEqual(0,economy.BoatRepair.CompletedPartIds.Count);
            work.TryGet(alice,out var state);Assert.AreEqual(2,state.Hits);
            economy.SetPersistenceHandler(()=>true);
            Assert.IsTrue(work.Execute(alice,BoatRepairCommand.Hammer,0,request,4).Accepted);
            Assert.AreEqual(1,economy.BoatRepair.CompletedPartIds.Count);
        }
        [Test] public void PurchaseChargesOnce_ThenReloadKeepsUnusedMaterialAndFinalStrokeDoesNotCharge()
        {
            var data=new EconomySaveData { SharedBalance=240 };
            Assert.IsTrue(economy.TryRestore(data));
            Assert.IsTrue(economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,1).Accepted);
            Assert.AreEqual(120,economy.SharedBalance);
            Assert.IsTrue(economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,1).Accepted);
            Assert.AreEqual("RequestMismatch",economy.TryAcquireBoatPart(alice,BoatRepairParts.Engine,BoatPartSource.Purchased,1).ReasonCode);
            var save=JsonUtility.FromJson<EconomySaveData>(JsonUtility.ToJson(economy.ExportSaveData("campaign","checkpoint")));
            Assert.IsTrue(economy.TryRestore(save));Assert.AreEqual(1,economy.UnusedBoatParts.Count);
            Assert.IsTrue(economy.TryFinishBoatPart(bob,BoatRepairParts.Hull,4).Accepted);
            Assert.AreEqual(120,economy.SharedBalance);Assert.IsEmpty(economy.UnusedBoatParts);
        }
        [Test] public void AcquisitionSaveFailureDoesNotChargeOrGrant()
        {
            Assert.IsTrue(economy.TryRestore(new EconomySaveData {SharedBalance=240}));
            economy.SetPersistenceHandler(()=>false);
            Assert.AreEqual("SaveFailed",economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,1).ReasonCode);
            Assert.AreEqual(240,economy.SharedBalance);Assert.IsEmpty(economy.UnusedBoatParts);
        }
        [Test] public void ActualCampaignFile_ReopensTwoOfThreeAndUnusedLastPart_ThenFinalRepairPersists()
        {
            var path=Path.Combine(Path.GetTempPath(),"DeepDive-p187-"+System.Guid.NewGuid().ToString("N")+".json");
            try
            {
                var store=root.AddComponent<EconomySaveStore>();store.SetPathForTests(path);
                economy.SetPersistenceHandler(store.SaveNow);
                Assert.IsTrue(economy.TryContributeBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Found,1).Accepted);
                Assert.IsTrue(economy.TryContributeBoatPart(alice,BoatRepairParts.Engine,BoatPartSource.Found,2).Accepted);
                Assert.IsTrue(economy.TryAcquireBoatPart(alice,BoatRepairParts.FuelTank,BoatPartSource.Found,3).Accepted);
                Object.DestroyImmediate(root);root=new GameObject("reopened-p187");economy=root.AddComponent<EconomyManager>();
                store=root.AddComponent<EconomySaveStore>();store.SetPathForTests(path);Assert.IsTrue(store.LoadNow());
                Assert.AreEqual(2,economy.BoatRepair.CompletedPartIds.Count);Assert.AreEqual(1,economy.UnusedBoatParts.Count);
                economy.SetPersistenceHandler(store.SaveNow);
                Assert.IsTrue(economy.TryFinishBoatPart(bob,BoatRepairParts.FuelTank,4).Accepted);
                Assert.IsTrue(store.LoadNow());Assert.AreEqual(BoatRepairStatus.Repaired,economy.BoatRepair.Status);
                Assert.IsEmpty(economy.UnusedBoatParts);
            }
            finally {foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void EveryOldMaskMigratesWithoutFreeMaterialOrLosingCompletedParts(int mask)
        {
            for(int schema=1;schema<=8;schema++)
            {
                var data=new EconomySaveData {SchemaVersion=schema,SharedBalance=75};
                for(int i=0;i<3;i++)if((mask&(1<<i))!=0)data.BoatPartIds.Add(BoatRepairParts.All[i]);
                Assert.IsTrue(economy.TryRestore(data));
                // Preserve all supplied legacy bits, including older files with a backported part list.
                for(int i=0;i<3;i++)Assert.AreEqual((mask&(1<<i))!=0,Contains(economy.BoatRepair.CompletedPartIds,i));
                Assert.IsEmpty(economy.UnusedBoatParts);Assert.AreEqual(75,economy.SharedBalance);
            }
        }
        [Test] public void InvalidNewMaterialDomainFailsBeforeMutatingLiveEconomy()
        {
            Assert.IsTrue(economy.TryRestore(new EconomySaveData {SharedBalance=70}));
            foreach(var parts in new[]{new[]{"invalid"},new[]{BoatRepairParts.Hull,BoatRepairParts.Hull}})
            {
                var data=new EconomySaveData{SharedBalance=900,UnusedBoatPartIds=new List<string>(parts)};
                Assert.IsFalse(economy.TryRestore(data));Assert.AreEqual(70,economy.SharedBalance);
            }
            var overlap=new EconomySaveData();overlap.BoatPartIds.Add(BoatRepairParts.Hull);overlap.UnusedBoatPartIds.Add(BoatRepairParts.Hull);
            Assert.IsFalse(economy.TryRestore(overlap));Assert.AreEqual(70,economy.SharedBalance);
        }
        [Test] public void RejectedPurchaseCannotSucceedOnReplayAfterTheBalanceChanges()
        {
            var rejected=economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,1);
            Assert.AreEqual("InsufficientFunds",rejected.ReasonCode);
            Assert.IsTrue(economy.TryCreditReward("funds",240));
            var replay=economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,1);
            Assert.AreEqual(rejected.ReasonCode,replay.ReasonCode);Assert.AreEqual(rejected.Revision,replay.Revision);
            Assert.AreEqual(240,economy.SharedBalance);Assert.IsEmpty(economy.UnusedBoatParts);
            Assert.AreEqual("RequestMismatch",economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Found,1).ReasonCode);
            Assert.AreEqual("RequestMismatch",economy.TryAcquireBoatPart(alice,BoatRepairParts.Engine,BoatPartSource.Purchased,1).ReasonCode);
            Assert.IsTrue(economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Purchased,2).Accepted);
            Assert.AreEqual(120,economy.SharedBalance);
        }
        [Test] public void FailedAcquisitionSaveCanRetryOnlyItsOriginalPayload()
        {
            economy.SetPersistenceHandler(()=>false);
            Assert.AreEqual("SaveFailed",economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Found,1).ReasonCode);
            Assert.IsEmpty(economy.UnusedBoatParts);
            economy.SetPersistenceHandler(()=>true);
            Assert.AreEqual("RequestMismatch",economy.TryAcquireBoatPart(alice,BoatRepairParts.Engine,BoatPartSource.Found,1).ReasonCode);
            Assert.IsTrue(economy.TryAcquireBoatPart(alice,BoatRepairParts.Hull,BoatPartSource.Found,1).Accepted);
            Assert.AreEqual(1,economy.UnusedBoatParts.Count);
        }
        [Test] public void RejectedFinishStaysRejectedAfterMaterialArrives()
        {
            Assert.AreEqual("MaterialMissing",economy.TryFinishBoatPart(alice,BoatRepairParts.Hull,1).ReasonCode);
            Assert.IsTrue(economy.TryAcquireBoatPart(bob,BoatRepairParts.Hull,BoatPartSource.Found,2).Accepted);
            Assert.AreEqual("MaterialMissing",economy.TryFinishBoatPart(alice,BoatRepairParts.Hull,1).ReasonCode);
            Assert.IsEmpty(economy.BoatRepair.CompletedPartIds);
            Assert.IsTrue(economy.TryFinishBoatPart(alice,BoatRepairParts.Hull,3).Accepted);
        }
    }
}
