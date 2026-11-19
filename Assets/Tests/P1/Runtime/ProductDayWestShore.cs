using System.Collections.Generic;
using DeepDive.Composition;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #302 explicit development walk (-p302-west-shore): before the day's normal walk, every player walks with owner input
    // from home through the town and over the beach, through the west fence opening, up the ramp to the terrace and back
    // to the square, capturing its own view at the ramp foot, at the terrace rail looking out to sea and back towards the
    // beach. Odd slots keep to the other side of the 3 m walk and the second pair starts later. With -p302-night 1 the
    // host applies a LABELLED clock fixture (22:00 for the walk). No teleport, no speed change.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool WestShoreWalkOn => Arg("-p302-west-shore") == "1";
        private bool WestShoreNight => Arg("-p302-night") == "1";
        private bool westShoreDone, westShoreStarted;
        private int westShoreSavedMinute = -1, westShorePoint, westShoreShot;
        private float westShoreHold, westShoreStartAt;
        private readonly Dictionary<int, Vector3> westShoreShots = new Dictionary<int, Vector3>();

        // True once this player is back in the square.
        private bool WestShoreTick()
        {
            if (westShoreDone) return true;
            if (!westShoreStarted)
            {
                westShoreStarted = true;
                WestShoreClock(true);
                // Single file, 1.2 s apart; everyone keeps right: up the east half of the 3 m walk, down the west half.
                westShoreStartAt = Time.realtimeSinceStartup + report.slot * 1.2f;
                const float lane = 0.6f;
                var rampX = (WestShoreWalk.WalkMinX + WestShoreWalk.WalkMaxX) / 2f;
                var spot = new Vector2(WestShoreWalk.TerraceMiddle.x + (report.slot % 2) * 1.1f, WestShoreWalk.TerraceMiddle.z + 0.9f - (report.slot / 2) * 1.6f);
                var points = new List<Vector3>
                {
                    H(-4.7f, -2f), H(-4.7f, 5f), ContinuousTownLayout.HomeRig.Door, P(0f, -36f), P(0f, -24f), P(-1.8f, -16f),
                    P(-9f, -13.6f + lane), P(-13.5f, -13.25f + lane),
                    P(WestShoreWalk.LandingPoint.x + lane, WestShoreWalk.LandingPoint.z + lane),
                    P(rampX + lane, WestShoreWalk.RampFoot.z)
                };
                var foot = points.Count - 1;
                points.Add(Lift(P(rampX + lane, WestShoreWalk.RampTop.z)));
                points.Add(Lift(P(spot.x, spot.y)));
                var view = points.Count - 1;
                points.Add(Lift(P(rampX - lane, WestShoreWalk.RampTop.z)));
                points.Add(P(rampX - lane, WestShoreWalk.RampFoot.z));
                points.Add(P(WestShoreWalk.LandingPoint.x - lane, WestShoreWalk.LandingPoint.z - lane));
                points.Add(P(-9f, -13.6f - lane));
                points.Add(P(-1.8f, -16f));
                points.Add(P(0f, -24f));
                westShoreShots[foot] = Lift(P(WestShoreWalk.TerraceMiddle.x, WestShoreWalk.TerraceMiddle.z)) + Vector3.up * 1.4f;
                westShoreShots[view] = new Vector3(-3f, 5f, 22f);
                westShoreShots[view + 1] = new Vector3(0f, ContinuousTownLayout.WalkSurfaceY + 1.5f, -14f);   // from the ramp top, back to the beach
                route = points; waypoint = 0;
            }
            if (local != null) westShoreTop = Mathf.Max(westShoreTop, local.transform.position.y);
            if (Time.realtimeSinceStartup < westShoreStartAt) { Stop(); return false; }
            if (westShoreHold > 0f)
            {
                Stop();
                Aim(westShoreShots[westShorePoint]);
                if (Time.realtimeSinceStartup < westShoreHold) return false;
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureView(".west-shore-" + (++westShoreShot) + ".png");
                westShoreHold = 0f;
                westShoreShots.Remove(westShorePoint);
            }
            var before = waypoint;
            if (!Follow())
            {
                if (waypoint != before && westShoreShots.ContainsKey(waypoint - 1))
                { westShorePoint = waypoint - 1; westShoreHold = Time.realtimeSinceStartup + 1.5f; }
                return false;
            }
            report.westShoreWalks++;
            report.westShoreHighestY = Mathf.Max(report.westShoreHighestY, westShoreTop);
            westShoreDone = true; route = null;
            WestShoreClock(false);
            return true;
        }

        private float westShoreTop;
        private Vector3 Lift(Vector3 p) { p.y = WestShoreWalk.TerraceY; return p; }

        private void WestShoreClock(bool night)
        {
            if (!Host || !WestShoreNight) return;
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (engine == null) return;
            var saved = engine.ExportDay();
            if (night && westShoreSavedMinute < 0) { westShoreSavedMinute = saved.ClockMinute; saved.ClockMinute = 22 * 60; engine.RestoreDay(saved); }
            else if (!night && westShoreSavedMinute >= 0) { saved.ClockMinute = westShoreSavedMinute; westShoreSavedMinute = -1; engine.RestoreDay(saved); }
        }
    }
}
