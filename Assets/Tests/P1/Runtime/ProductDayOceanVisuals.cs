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
        private int oceanWaypoint,oceanFrame;
        private float oceanHold,nextOceanFrame,oceanFirstTime,oceanFirstHeight;
        private bool oceanShoreDone,oceanSurveyDone,oceanCapturing;
        private Vector3[] oceanRoute;

        private bool OceanShore()
        {
            if(oceanShoreDone)return true;
            oceanCapturing=true;
            Aim(new Vector3(-4,8,40));
            if(oceanHold==0)oceanHold=Time.realtimeSinceStartup+3;
            if(Time.realtimeSinceStartup<oceanHold)return false;
            CaptureView(".ocean-shore.png");oceanShoreDone=true;oceanCapturing=false;oceanHold=0;return true;
        }

        private bool OceanSurvey()
        {
            if(oceanSurveyDone)return true;
            oceanCapturing=true;
            if(oceanRoute==null)
            {
                var x=-11+report.slot*1.3f;
                oceanRoute=new[]{P(9,-12.5f),P(-6.5f,-11.2f),new Vector3(-6.5f+report.slot,6.3f,-5),
                    new Vector3(x,2,9),new Vector3(x,-4,30),new Vector3(x,-16,53),
                    new Vector3(x,-4,30),new Vector3(x,2,9),new Vector3(-6.5f+report.slot,6.3f,-5)};
            }
            var target=oceanRoute[oceanWaypoint];var delta=target-local.transform.position;
            if(delta.magnitude>.45f)
            {
                MoveToward(oceanWaypoint<2?CoastTarget(Around(target)):target);oceanHold=0;return false;
            }
            if(oceanWaypoint<2){oceanWaypoint++;return false;}
            Aim(local.RecordingEyePosition+new Vector3(1,oceanWaypoint==2||oceanWaypoint==8?0:-2,12));
            if(oceanHold==0)oceanHold=Time.realtimeSinceStartup+2;
            if(Time.realtimeSinceStartup<oceanHold)return false;
            if(oceanWaypoint>=3 && oceanWaypoint<=5)
            {report.oceanBands++;CaptureView(".ocean-band-"+report.oceanBands+".png");}
            if(oceanWaypoint==2 || oceanWaypoint==8)CaptureView(".ocean-surface-"+oceanWaypoint+".png");
            oceanWaypoint++;oceanHold=0;
            if(oceanWaypoint<oceanRoute.Length)return false;
            oceanCapturing=false;oceanSurveyDone=true;route=null;substep=1;
            // Guest has finished its physical survey and waits in water while the host hunts.
            if(!Host){localDone=true;return false;}
            return true;
        }

        private void CaptureOceanVideo()
        {
            if(!OceanVideo || finished || !oceanCapturing || local==null || !local.IsSpawned)return;
            var ocean=OceanPresentation.Active;if(ocean==null)return;
            report.oceanReady|=ocean.IsReady;report.oceanReflection|=ocean.ReflectionReady;
            report.oceanSurfaceEye|=local.Swimming.Value && local.RecordingEyePosition.y>ocean.SurfaceY+.015f;
            report.oceanGpu=SystemInfo.graphicsDeviceName;
            var height=OceanVisualMath.WaveHeight(new Vector2(2,24),ocean.VisualTime,ocean.Wind);
            if(oceanFirstTime==0){oceanFirstTime=ocean.VisualTime;oceanFirstHeight=height;}
            if(ocean.VisualTime-oceanFirstTime>.5f && Mathf.Abs(height-oceanFirstHeight)>.01f)report.oceanAnimated=true;
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
