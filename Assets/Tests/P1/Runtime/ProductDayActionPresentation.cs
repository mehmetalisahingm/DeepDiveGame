using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    public sealed partial class ProductDaySmokeDriver
    {
        private bool ActionVideo=>Arg("-p49-action-video")=="1";
        private int actionFrame;
        private float nextActionFrame;
        private bool actionObserverEntered;
        private NetworkPlayer ActionWorker=>FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&p.OwnerClientId==0);
        private void HoldActionView()
        {
            var euler=local.GetComponentInChildren<Camera>(true).transform.eulerAngles;
            local.SubmitLocalInput(Vector3.zero,euler.y,Mathf.DeltaAngle(0,euler.x));
        }
        private void ActionObserver()
        {
            var worker=ActionWorker;if(worker==null)return;
            if(!actionObserverEntered)
            {
                if(route==null)SetRoute(new[]{P(9,-12.5f),P(-5.5f,-11.2f),new Vector3(-5.5f,6,-5)});
                if(!Follow(true))return;actionObserverEntered=true;
            }
            var offset=new Vector3(3,0,-1.5f);var target=worker.transform.position+offset;
            if(Vector3.Distance(local.transform.position,target)>1.1f){MoveToward(target);return;}
            Stop();Aim(worker.GetComponent<PlayerActionView>()?.ActionFocus??worker.transform.position+Vector3.up*.95f);
            var view=worker.GetComponent<PlayerActionView>();
            localDone=view!=null&&view.CollectCount>=(step==2?1:2)&&view.ActiveKind==PlayerVisualAction.None;
        }
        private void ActionSaleObserver()
        {
            actionObserverEntered=false;
            var anchor=FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None).Single(a=>a.Definition.ServiceId==TownServiceCatalog.FishBuyerId);
            var target=anchor.WorldPosition+anchor.transform.forward*2.25f+anchor.transform.right*1.8f;
            var delta=target-local.transform.position;delta.y=0;
            if(delta.magnitude>.25f){MoveToward(Around(CoastTarget(target)));return;}
            var worker=ActionWorker;if(worker==null)return;
            Stop();Aim(worker.GetComponent<PlayerActionView>()?.ActionFocus??worker.transform.position+Vector3.up*1.1f);
            var view=worker.GetComponent<PlayerActionView>();
            localDone=view!=null&&view.DeliveryCount>=(step==4?1:2)&&view.ActiveKind==PlayerVisualAction.None;
        }
        private void CaptureActionVideo()
        {
            if(!ActionVideo||finished||local==null||!local.IsSpawned||report.reload)return;
            var worker=ActionWorker;if(worker==null)return;
            var view=worker.GetComponent<PlayerActionView>();if(view==null)return;
            report.actionCollects=view.CollectCount;report.actionDeliveries=view.DeliveryCount;
            report.actionBoards=view.BoardCount;report.actionExits=view.ExitCount;report.actionProps|=view.AuthoredProps;
            if(worker.VisualBoatSeated)
                report.actionSeatedEye|=Mathf.Abs(worker.RecordingEyePosition.y-worker.transform.position.y-1.55f-NetworkPlayer.BoatSeatedVisualOffset)<.07f;
            else if(report.actionSeatedEye && !worker.Seated.Value)
                report.actionStandingEye|=Mathf.Abs(worker.RecordingEyePosition.y-worker.transform.position.y-1.55f-worker.WaterViewLift)<.07f;
            if(view.ActiveKind==PlayerVisualAction.None)return;
            report.actionKinds|=1<<((int)view.ActiveKind-1);
            var camera=local.GetComponentInChildren<Camera>(true);
            var screen=camera.WorldToViewportPoint(view.ActionFocus);
            var clear=!Physics.Linecast(camera.transform.position,view.ActionFocus,out var hit,~0,QueryTriggerInteraction.Ignore) ||
                hit.collider.GetComponentInParent<NetworkPlayer>()==worker;
            if(screen.z>0 && screen.x>.05f && screen.x<.95f && screen.y>.05f && screen.y<.95f && clear)
                report.actionVisibleKinds|=1<<((int)view.ActiveKind-1);
            if(Time.realtimeSinceStartup<nextActionFrame)return;
            nextActionFrame=Time.realtimeSinceStartup+.05f;
            var folder=output+".action-frames";Directory.CreateDirectory(folder);
            CaptureView(".action-frames/"+actionFrame.ToString("D5")+".png");
            File.AppendAllText(Path.Combine(folder,"frames.csv"),System.FormattableString.Invariant(
                $"{actionFrame},{Time.realtimeSinceStartup:F4},{(int)view.ActiveKind},{view.ActionAge:F4},{worker.VisualBoatSeated},{worker.OwnerClientId}\n"));
            actionFrame++;report.actionVideoFrames=actionFrame;
        }
    }
}
