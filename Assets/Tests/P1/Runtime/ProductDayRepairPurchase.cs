using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    public sealed partial class ProductDaySmokeDriver
    {
        private bool RepairPurchase => Arg("-p187-purchase") == "1";
        private int purchaseStage, purchaseBalance = -1;
        private string lockedCheckpoint;
        private int faultRevision, faultBalance;
        private float nextRepairFrame, repairVideoUntil;
        private int repairFrame;
        private bool purchaseObserverReady, purchaseHomeExited;

        // Earn the real price through a coastal hunt and NPC sale. No wallet/item fixture.
        private bool PurchasedRepairAcceptance()
        {
            if (!Host)
            {
                var stand=P(1.2f,-16.5f);
                var delta=stand-local.transform.position;delta.y=0;
                if(delta.magnitude>.2f) { MoveToward(Around(stand)); return false; }
                purchaseObserverReady=true;
                var worker=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&p.OwnerClientId==0);
                if(worker!=null) Aim(worker.transform.position+Vector3.up*1.1f);
                localDone=sync.BoatPartsDone.Value==3;return false;
            }
            var economy=adapter.GetComponent<EconomyManager>();
            if(purchaseStage==0)
            {
                var phase=adapter.Session.State.Phase;
                if(phase==SessionPhase.Prep || phase==SessionPhase.Return || (phase==SessionPhase.Lobby&&AllReady())) adapter.AdvancePhase();
                if(adapter.Session.State.Phase==SessionPhase.Dive) {purchaseStage=1;substep=0;route=null;}
                return false;
            }
            if(purchaseStage==1)
            {
                // Before repairing, the solid cradle still blocks the ordinary post-repair
                // product route across the beach. Enter the shallows from its west side.
                if(substep==0 && route==null)SetRoute(new[]{P(-6.5f,-11.2f),new Vector3(-6.5f,6f,-5f)});
                Hunt();
                if(localDone) {localDone=false;purchaseStage=2;route=null;aimAt=0;}
                return false;
            }
            if(purchaseStage==2)
            {
                if(route==null)SetRoute(new[]{P(-6.5f,-11.2f),P(-6.5f,-16),P(9,-16),P(8.2f,-9.8f)});
                if(!Follow() || local.Swimming.Value || !local.GetComponent<DeepDive.Inventory.InventoryPlayerSync>().SafelyReturned.Value)return false;
                adapter.AdvancePhase();
                if(adapter.Session.State.Phase==SessionPhase.Return){purchaseStage=3;substep=0;route=null;aimAt=0;}
                return false;
            }
            if(purchaseStage==3)
            {
                if(substep==0)
                {
                    if(!GoToService(TownServiceCatalog.FishBuyerId))return false;
                    if(sync.PendingCatches.Value>0){Act(local.SubmitServiceInteractionLocal);return false;}
                    var sold=economy.ExportSaveData("read-only","").SoldCaptureIds.Count;
                    if(sold==0){Act(local.SubmitServiceInteractionLocal);return false;}
                    if(economy.SharedBalance<economy.BoatPartPrice){purchaseStage=0;substep=0;aimAt=0;return false;}
                    substep=1;aimAt=0;
                }
                if(!GoToService(TownServiceCatalog.EquipmentShopId))return false;
                if(!sync.ShopOpen){Act(local.SubmitServiceInteractionLocal);return false;}
                if(!economy.UnusedBoatParts.Contains(BoatRepairParts.All[0]))
                {
                    if(purchaseBalance<0)purchaseBalance=economy.SharedBalance;
                    Act(()=>sync.RequestPurchase(BoatRepairParts.All[0]));return false;
                }
                if(economy.SharedBalance!=purchaseBalance-economy.BoatPartPrice || economy.BoatRepair.CompletedPartIds.Count!=0)
                {Fail("Purchase did not charge exactly once or prematurely completed the hull");return false;}
                report.purchasedMaterial=true;report.purchaseDidNotRepair=true;
                Debug.Log("P187_PURCHASE earned="+purchaseBalance+" cost="+economy.BoatPartPrice+" acquired-not-completed");
                purchaseStage=4;route=null;part=0;aimAt=0;substep=1;
                repairApproach=repairAtDock=false;stageDeadline=Time.realtimeSinceStartup+170f;
            }
            if(report.saveFaultRefused && !report.saveFaultRecovered && economy.BoatRepair.CompletedPartIds.Contains(BoatRepairParts.All[0]))
            {
                var disk=JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(adapter.GetComponent<EconomySaveStore>().SavePath));
                report.saveFaultRecovered=disk.BoatPartIds.Count(x=>x==BoatRepairParts.All[0])==1 &&
                    !disk.UnusedBoatPartIds.Contains(BoatRepairParts.All[0]) && economy.SharedBalance==faultBalance;
                if(!report.saveFaultRecovered){Fail("Final-stroke retry did not persist exactly one completion");return false;}
                Debug.Log("P187_SAVE_RETRY completed-once material-consumed-once balance-unchanged");
            }
            return true;
        }

        private void RefuseFinalStrokeOnDisk()
        {
            if(Time.realtimeSinceStartup<nextAction)return;
            var store=adapter.GetComponent<EconomySaveStore>();var economy=adapter.GetComponent<EconomyManager>();
            lockedCheckpoint=File.ReadAllText(store.SavePath);faultRevision=economy.Revision;faultBalance=economy.SharedBalance;
            // Actual Windows sharing violation in the real atomic File.Replace path. Only
            // this development run's isolated campaign is locked, and always released.
            using(var locked=new FileStream(store.SavePath,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                Act(local.SubmitRepairHammerLocal);
            report.saveFaultRefused=!string.IsNullOrEmpty(store.LastError) &&
                local.LastActionResult.Value!=(int)PlayerActionResult.Accepted &&
                economy.Revision==faultRevision && economy.SharedBalance==faultBalance &&
                economy.UnusedBoatParts.Contains(BoatRepairParts.All[0]) &&
                !economy.BoatRepair.CompletedPartIds.Contains(BoatRepairParts.All[0]) &&
                local.RepairHits.Value==2 && File.ReadAllText(store.SavePath)==lockedCheckpoint;
            if(!report.saveFaultRefused)Fail("Real final-stroke disk refusal did not preserve material/checkpoint/work");
            else Debug.Log("P187_SAVE_REFUSED actual-disk-lock checkpoint-unchanged material-retained hits=2");
        }

        private void CaptureRepairVideo()
        {
            if(Arg("-p187-video")!="1" || finished || local==null || !local.IsSpawned || step!=1)return;
            var worker=Host?local:FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&p.OwnerClientId==0);
            if(worker==null || (!Host&&!purchaseObserverReady))return;
            var presentation=worker.GetComponent<RepairCarryView>();
            if(presentation!=null)
            {
                report.repairArtSeen|=presentation.UsesAuthoredArt;
                if(presentation.StrokeAge<.2f && presentation.NailContactError<.04f)
                    report.repairToolContactSeen=true;
                if(worker.RepairPlaced.Value)
                {
                    var rig=worker.GetComponentsInChildren<CoastalDiverPose>(true).FirstOrDefault(p=>p.FirstPerson==worker.IsOwner);
                    var hand=rig!=null?RepairPropPlacement.Marker(rig.transform,"Hand.R"):null;
                    if(hand!=null)
                    {
                        var error=Vector3.Distance(hand.position,presentation.RightGripPosition);
                        report.minRepairGripError=Mathf.Min(report.minRepairGripError,error);
                        if(error<.08f)report.repairGripVerified=true;
                    }
                }
            }
            // The first (purchased) hull material, including Q/recovery and three real strokes.
            if(worker.RepairHandsBusy && worker.RepairPartIndex.Value==0 && repairVideoUntil==0)repairVideoUntil=float.PositiveInfinity;
            if(float.IsPositiveInfinity(repairVideoUntil) && (sync.BoatPartsMask.Value&1)!=0)repairVideoUntil=Time.realtimeSinceStartup+1f;
            if(repairVideoUntil<Time.realtimeSinceStartup || Time.realtimeSinceStartup<nextRepairFrame)return;
            nextRepairFrame=Time.realtimeSinceStartup+.05f;
            var folder=output+".repair-frames";Directory.CreateDirectory(folder);
            CaptureView(".repair-frames/"+(repairFrame++).ToString("D5")+".png");
            File.AppendAllText(Path.Combine(folder,"frames.csv"),System.FormattableString.Invariant($"{repairFrame-1},{Time.realtimeSinceStartup:F4},{worker.RepairPartIndex.Value},{worker.RepairPlaced.Value},{worker.RepairHits.Value},{worker.RepairStroke.Value}\n"));
            report.repairVideoFrames=repairFrame;
        }
    }
}
