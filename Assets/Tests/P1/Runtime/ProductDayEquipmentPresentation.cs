using System.IO;
using System.Linq;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Opt-in real equipment input, with normal earned camera ownership; never seeds it.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool EquipmentVideo=>Arg("-p49-equipment-video")=="1";
        private int equipmentPhase,equipmentFrame;
        private float equipmentAt,equipmentRefusalAt,nextEquipmentFrame,equipmentUntil=float.PositiveInfinity;
        private bool equipmentComplete,equipmentObserverReady;

        private bool EquipmentSwitchProbe()
        {
            if(equipmentComplete)return true;
            Stop();
            var now=Time.realtimeSinceStartup;
            if(equipmentPhase==0)
            {
                var guest=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&!p.IsOwner);
                if(guest==null || Vector3.Distance(guest.transform.position,local.transform.position)>3.5f)return false;
                Aim(local.RecordingEyePosition+new Vector3(0,-.8f,4));
                local.SetHeldEquipmentLocal(HeldEquipmentMode.Camera);equipmentPhase=1;equipmentAt=now;return false;
            }
            var dwell=equipmentPhase==3 ? .07f : .85f;
            if(now-equipmentAt<dwell)return false;
            var view=local.GetComponent<PlayerPresentationView>();
            if(equipmentPhase==3)
                report.equipmentCancelled=view.DisplayedEquipment==HeldEquipmentMode.Harpoon && view.EquipmentLowering>.03f;
            if(equipmentPhase==5)
            {equipmentComplete=true;equipmentUntil=now+.35f;return true;}
            local.SetHeldEquipmentLocal(equipmentPhase==1||equipmentPhase==3?HeldEquipmentMode.Harpoon:HeldEquipmentMode.Camera);
            equipmentPhase++;equipmentAt=now;return false;
        }

        private void EquipmentObserver()
        {
            var worker=FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&p.OwnerClientId==0);
            if(worker==null)return;
            if(!equipmentObserverReady)
            {
                if(route==null)SetRoute(new[]{P(9f,-12.5f),P(-6.5f,-11.2f),new Vector3(-4.3f,6,-5)});
                if(!Follow(true))return;
                equipmentObserverReady=true;
                local.SetHeldEquipmentLocal(HeldEquipmentMode.Camera);equipmentRefusalAt=Time.realtimeSinceStartup;
            }
            Aim(worker.transform.position+Vector3.up*.7f);
            if(Time.realtimeSinceStartup-equipmentRefusalAt>.4f)
                report.unownedCameraRefused=local.CurrentHeldEquipment==HeldEquipmentMode.Harpoon &&
                    local.GetComponent<PlayerPresentationView>().DisplayedEquipment==HeldEquipmentMode.Harpoon;
            if(worker.RecordingPresentation.Value && !equipmentComplete)
            {equipmentComplete=true;equipmentUntil=Time.realtimeSinceStartup+.35f;}
            if(equipmentComplete && Time.realtimeSinceStartup>equipmentUntil)localDone=true;
        }

        private void CaptureEquipmentVideo()
        {
            if(!EquipmentVideo || finished || local==null || !local.IsSpawned || step!=8 ||
                Time.realtimeSinceStartup>equipmentUntil || (!Host&&!equipmentObserverReady))return;
            var worker=Host?local:FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p=>p.IsSpawned&&p.OwnerClientId==0);
            if(worker==null || (Host&&equipmentPhase==0))return;
            var view=worker.GetComponent<PlayerPresentationView>();
            report.equipmentLoweredSeen |= view.EquipmentLowering>.4f;
            report.equipmentRaisedSeen |= report.equipmentLoweredSeen && !view.IsChangingEquipment &&
                view.DisplayedEquipment==HeldEquipmentMode.Camera;
            if(Time.realtimeSinceStartup<nextEquipmentFrame)return;
            nextEquipmentFrame=Time.realtimeSinceStartup+.05f;
            var folder=output+".equipment-frames";Directory.CreateDirectory(folder);
            CaptureView(".equipment-frames/"+equipmentFrame.ToString("D5")+".png");
            File.AppendAllText(Path.Combine(folder,"frames.csv"),System.FormattableString.Invariant(
                $"{equipmentFrame},{Time.realtimeSinceStartup:F4},{(int)view.DisplayedEquipment},{view.IsChangingEquipment},{view.EquipmentLowering:F4},{(int)worker.CurrentHeldEquipment}\n"));
            equipmentFrame++;report.equipmentVideoFrames=equipmentFrame;
        }
    }
}
