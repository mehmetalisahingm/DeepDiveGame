using System;
using System.Collections.Generic;
using DeepDive.Composition;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    public sealed partial class ProductDaySmokeDriver
    {
        [Serializable] public sealed class MotionSample
        {
            public double realTime, fixedTime, serverTime, realGap, fixedGap, serverGap;
            public int frame, frameGap, step, substep, waypoint, day, locomotion;
            public ulong networkObject;
            public bool seated, swimming, host, owner;
            public string phase;
            public float x,y,z,previousX,previousY,previousZ,distance,metresPerRealSecond;
        }
        private readonly Queue<MotionSample> motionHistory=new Queue<MotionSample>();
        private double motionAt, motionFixedAt, motionServerAt, motionGap, motionFixedGap, motionServerGap;
        private int motionFrameAt, motionFrameGap;

        private void BeginMotionSegment()
        {
            previous=local.transform.position; wasSeated=local.Seated.Value;
            previousDay=sync.DayNumber.Value;
            motionAt=motionFixedAt=motionServerAt=motionGap=motionFixedGap=motionServerGap=0;
            motionFrameAt=motionFrameGap=0;tracked=true;
            report.motionTrackingSegments++;
        }

        private bool ResumeMotionAfterRejoin()
        {
            if(tracked)return true;
            // The real join spawns a new object in Lobby, then the product's
            // Prep transition places it in the house. As on initial entry,
            // wait for that placement before sending route input/tracking.
            var atHome=false;
            for(var i=0;i<4;i++)
                atHome |= Vector3.Distance(local.transform.position,ContinuousTownLayout.HomeRig.Spawn(i))<.7f;
            if(RepairInterruptions && reconnectSent &&
                (adapter.Session.State.Phase!=SessionPhase.Prep || !atHome))
            {Stop();return false;}
            BeginMotionSegment();
            return true;
        }

        private void RecordMotion(float distance)
        {
            var real=Time.realtimeSinceStartupAsDouble;
            var fixedAt=Time.fixedTimeAsDouble;
            var serverAt=manager.ServerTime.Time;
            var frame=Time.frameCount;
            var first=motionAt==0;
            motionGap=first ? 0 : real-motionAt;
            motionFixedGap=first ? 0 : fixedAt-motionFixedAt;
            motionServerGap=first ? 0 : serverAt-motionServerAt;
            motionFrameGap=first ? 0 : frame-motionFrameAt;
            motionAt=real;motionFixedAt=fixedAt;motionServerAt=serverAt;motionFrameAt=frame;
            report.maxMotionSampleGap=Math.Max(report.maxMotionSampleGap,motionGap);
            var position=local.transform.position;
            if(motionHistory.Count==128)motionHistory.Dequeue();
            motionHistory.Enqueue(new MotionSample{realTime=real,fixedTime=fixedAt,serverTime=serverAt,
                realGap=motionGap,fixedGap=motionFixedGap,serverGap=motionServerGap,
                frame=frame,frameGap=motionFrameGap,step=step,substep=substep,waypoint=waypoint,
                day=sync.DayNumber.Value,networkObject=local.NetworkObjectId,
                locomotion=local.Locomotion.Value,phase=adapter.Session.State.Phase.ToString(),
                seated=local.Seated.Value,swimming=local.Swimming.Value,host=manager.IsServer,owner=local.IsOwner,
                previousX=previous.x,previousY=previous.y,previousZ=previous.z,
                x=position.x,y=position.y,z=position.z,distance=distance,
                metresPerRealSecond=motionGap>0? (float)(distance/motionGap):0f});
        }

        private void OnApplicationQuit()
        {
            if(finished || string.IsNullOrEmpty(output))return;
            report.failure="Application quit before gameplay assertions completed";
            report.errors.Add(report.failure);
            Finish(false);
        }
    }
}
