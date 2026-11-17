using System.IO;
using DeepDive.Composition;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Explicit development command. Survey uses the same owner input and host movement
    // as play; no camera teleport, oxygen refill, clock, inventory or weather fixture.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool OceanVideo=>Arg("-p411-ocean-video")=="1";
        // #298 explicit development frames: the same survey swims out over the shore-to-arch trail instead and, at
        // its three band stops, looks at the reef arch's south mouth. Same owner input and host movement.
        private bool ReefGateFrames=>Arg("-p298-reef-gate-frames")=="1";
        private static readonly Vector3 ReefGateMouth=new Vector3(-7,-6.8f,29);
        // #303 explicit development frames: the survey swims round the shallow wreck in front of the beach and looks
        // at its hull from the wade side, the dock side and from seaward.
        private bool WreckFrames=>Arg("-p303-wreck-frames")=="1";
        private static readonly Vector3 WreckHull=new Vector3(1.5f,1f,3f);
        private int oceanWaypoint,oceanFrame;
        private float oceanHold,nextOceanFrame,oceanFirstTime,oceanFirstHeight;
        private bool oceanShoreDone,oceanShoreCaptured,oceanSurveyDone,oceanCapturing;
        private Vector3[] oceanRoute;

        private bool OceanShore()
        {
            if(oceanShoreDone)return true;
            if(!oceanShoreCaptured)
            {
                oceanCapturing=true;
                Aim(new Vector3(-4,8,40));
                if(oceanHold==0)oceanHold=Time.realtimeSinceStartup+3;
                if(Time.realtimeSinceStartup<oceanHold)return false;
                CaptureView(".ocean-shore.png");oceanShoreCaptured=true;oceanCapturing=false;
            }
            // Outward() ends at the dry engine pickup. The observer must walk out of
            // its E ray instead of standing over it until the host finishes repairing.
            if(!Host)
            {
                var parking=P(-8,-15.5f);var delta=parking-local.transform.position;delta.y=0;
                if(delta.magnitude>.25f){MoveToward(Around(parking));return false;}
                Stop();
            }
            oceanShoreDone=true;oceanHold=0;return true;
        }

        private bool OceanSurvey()
        {
            if(oceanSurveyDone)return true;
            oceanCapturing=true;
            if(oceanRoute==null)
            {
                var x=-11+report.slot*1.3f;
                if(WreckFrames)
                {
                    WreckNightFixture(true);
                    var wx=report.slot*0.8f;
                    oceanRoute=new[]{P(9,-12.5f),P(-6.5f,-11.2f),new Vector3(-6.5f+report.slot,6.3f,-5),
                        new Vector3(-4.5f+wx,4.2f,0),new Vector3(6.5f,3.6f,5.5f+wx),new Vector3(1.5f+wx,3.8f,8.5f),
                        new Vector3(6.5f,3.6f,5.5f+wx),new Vector3(-4.5f+wx,4.2f,0),new Vector3(-6.5f+report.slot,6.3f,-5)};
                }
                else if(ReefGateFrames)
                {
                    var gx=-7+report.slot*1.5f;
                    oceanRoute=new[]{P(9,-12.5f),P(-6.5f,-11.2f),new Vector3(-6.5f+report.slot,6.3f,-5),
                        new Vector3(gx,3,6),new Vector3(gx,1.5f,16),new Vector3(gx,-2,21.5f),
                        new Vector3(gx,1.5f,16),new Vector3(gx,3,6),new Vector3(-6.5f+report.slot,6.3f,-5)};
                }
                else oceanRoute=new[]{P(9,-12.5f),P(-6.5f,-11.2f),new Vector3(-6.5f+report.slot,6.3f,-5),
                    new Vector3(x,2,9),new Vector3(x,-4,30),new Vector3(x,-16,53),
                    new Vector3(x,-4,30),new Vector3(x,2,9),new Vector3(-6.5f+report.slot,6.3f,-5)};
            }
            var target=oceanRoute[oceanWaypoint];var delta=target-local.transform.position;
            if(delta.magnitude>.45f)
            {
                MoveToward(oceanWaypoint<2?CoastTarget(Around(target)):target);oceanHold=0;return false;
            }
            if(oceanWaypoint<2){oceanWaypoint++;return false;}
            if(WreckFrames && oceanWaypoint>=2 && oceanWaypoint<=5)Aim(WreckHull);
            else if(ReefGateFrames && oceanWaypoint>=2 && oceanWaypoint<=5)Aim(ReefGateMouth);
            else Aim(local.RecordingEyePosition+new Vector3(1,oceanWaypoint==2||oceanWaypoint==8?0:-2,12));
            if(oceanHold==0)oceanHold=Time.realtimeSinceStartup+2;
            if(Time.realtimeSinceStartup<oceanHold)return false;
            if(SeabedFrames && oceanWaypoint>=3 && oceanWaypoint<=5 && !SeabedFrameStop())return false;
            if(WreckFrames && oceanWaypoint>=3 && oceanWaypoint<=5 && !WreckFrameStop())return false;
            if(oceanWaypoint>=3 && oceanWaypoint<=5)
            {report.oceanBands++;CaptureView((WreckFrames?".wreck-":ReefGateFrames?".reef-gate-":".ocean-band-")+report.oceanBands+".png");}
            if(oceanWaypoint==2 || oceanWaypoint==8)CaptureView(".ocean-surface-"+oceanWaypoint+".png");
            oceanWaypoint++;oceanHold=0;
            if(oceanWaypoint<oceanRoute.Length)return false;
            oceanCapturing=false;oceanSurveyDone=true;route=null;substep=1;
            if(WreckFrames)WreckNightFixture(false);
            // Guest has finished its physical survey and waits in water while the host hunts.
            if(!Host){localDone=true;return false;}
            return true;
        }

        private void CaptureOceanVideo()
        {
            if(!OceanVideo || SeabedFrames || finished || !oceanCapturing || local==null || !local.IsSpawned)return;
            var ocean=OceanPresentation.Active;if(ocean==null)return;
            report.oceanReady|=ocean.IsReady;report.oceanReflection|=ocean.ReflectionReady;
            report.oceanSurfaceEye|=local.Swimming.Value && local.RecordingEyePosition.y>ocean.SurfaceY+.015f;
            report.oceanGpu=SystemInfo.graphicsDeviceName;
            var height=OceanVisualMath.WaveHeight(new Vector2(2,24),ocean.VisualTime,ocean.Wind);
            if(oceanFirstTime==0){oceanFirstTime=ocean.VisualTime;oceanFirstHeight=height;}
            if(ocean.VisualTime-oceanFirstTime>.5f && Mathf.Abs(height-oceanFirstHeight)>.01f)report.oceanAnimated=true;
            // Held at a wreck stop the frame times are being measured; video frames would swamp them.
            if(WreckFrames && oceanWaypoint>=3 && oceanWaypoint<=5 && oceanHold!=0)return;
            if(Time.realtimeSinceStartup<nextOceanFrame)return;
            nextOceanFrame=Time.realtimeSinceStartup+.1f;
            var folder=output+".ocean-frames";Directory.CreateDirectory(folder);
            var index=oceanFrame++;var suffix=".ocean-frames/"+index.ToString("D5")+".png";
            CaptureView(suffix);report.oceanVideoFrames=oceanFrame;
            var ci=System.Globalization.CultureInfo.InvariantCulture;
            File.AppendAllText(Path.Combine(folder,"frames.csv"),string.Format(ci,"{0},{1:F6},{2},{3},{4:F6},{5:F6}\n",
                index,Time.realtimeSinceStartup,step,oceanWaypoint,ocean.VisualTime,height));
        }
    }
}
