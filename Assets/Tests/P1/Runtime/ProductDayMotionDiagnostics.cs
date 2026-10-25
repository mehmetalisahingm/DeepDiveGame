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
            public double realTime, fixedTime, serverTime, realGap, fixedGap;
            public int frame, step, day;
            public ulong networkObject;
            public bool seated;
            public float x,y,z,distance;
        }
        private readonly Queue<MotionSample> motionHistory=new Queue<MotionSample>();
        private double motionAt, motionFixedAt, motionGap, motionFixedGap;

        private void BeginMotionSegment()
        {
            previous=local.transform.position; wasSeated=local.Seated.Value;
            previousDay=sync.DayNumber.Value; motionAt=0; tracked=true;
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
            motionGap=motionAt==0 ? 0 : real-motionAt;
            motionFixedGap=motionAt==0 ? 0 : fixedAt-motionFixedAt;
            motionAt=real; motionFixedAt=fixedAt;
            report.maxMotionSampleGap=Math.Max(report.maxMotionSampleGap,motionGap);
            var position=local.transform.position;
            if(motionHistory.Count==128)motionHistory.Dequeue();
            motionHistory.Enqueue(new MotionSample{realTime=real,fixedTime=fixedAt,
                serverTime=manager.ServerTime.Time,realGap=motionGap,fixedGap=motionFixedGap,
                frame=Time.frameCount,step=step,day=sync.DayNumber.Value,networkObject=local.NetworkObjectId,
                seated=local.Seated.Value,x=position.x,y=position.y,z=position.z,distance=distance});
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
