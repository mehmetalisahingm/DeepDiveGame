using System.Collections.Generic;
using DeepDive.Composition;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #301 explicit development walk (-p301-east-court): before the day's normal walk to the beach, every player walks
    // the east neighbourhood loop with owner input - even slots out by the north lane, odd slots out by the south lane,
    // so they meet in the lanes and the court - and captures its own view at the lane mouth, in the court and at the
    // other lane. With -p301-night 1 the host applies a LABELLED clock fixture (22:00 for the walk, the minute put back
    // after), so the frames show the real night lighting. No teleport, no speed change.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool EastCourtWalk => Arg("-p301-east-court") == "1";
        private bool EastCourtNight => Arg("-p301-night") == "1";
        private bool eastCourtDone, eastCourtNightSet;
        private int eastCourtSavedMinute = -1;
        private List<Vector3> eastCourtRoute;
        private int eastCourtPoint, eastCourtShot;
        private float eastCourtHold;
        // Route index -> where to look when standing there.
        private readonly Dictionary<int, Vector3> eastCourtShots = new Dictionary<int, Vector3>();

        // True once this player is back in the square.
        private bool EastCourtTick()
        {
            if (eastCourtDone) return true;
            if (!eastCourtNightSet) { eastCourtNightSet = true; EastCourtClock(true); }
            if (eastCourtRoute == null)
            {
                var loop = new List<Vector3>(ContinuousTownLayout.EastCourt.Loop);
                if (report.slot % 2 == 1) loop.Reverse();
                // Everyone keeps to their own right, the second pair of a direction a little further out (3 m clear lane).
                var right = 0.45f + (report.slot / 2) * 0.55f;
                var points = new List<Vector3> { new Vector3(9f, 0f, loop[0].z) };
                points.AddRange(loop);
                points.Add(new Vector3(9f, 0f, loop[loop.Count - 1].z));
                eastCourtRoute = new List<Vector3> { H(-4.7f, -2f), H(-4.7f, 5f), ContinuousTownLayout.HomeRig.Door, P(0f, -36f) };
                var first = eastCourtRoute.Count + 1;
                for (var i = 0; i < points.Count; i++)
                {
                    var a = points[Mathf.Max(0, i - 1)];
                    var b = points[Mathf.Min(points.Count - 1, i + 1)];
                    var dir = new Vector2(b.x - a.x, b.z - a.z).normalized;
                    eastCourtRoute.Add(P(points[i].x + dir.y * right, points[i].z - dir.x * right));
                }
                eastCourtRoute.Add(P(0f, -30f));
                // At the lane mouth looking into the lane, in the court looking at its lamp, at the far mouth looking back.
                eastCourtShots[first] = ContinuousTownLayout.EastCourt.CourtMiddle + Vector3.up * 1.2f;
                eastCourtShots[first + 3] = new Vector3(26f, ContinuousTownLayout.WalkSurfaceY + 2f, -26.2f);
                eastCourtShots[first + loop.Count - 1] = ContinuousTownLayout.TownSquareCenter + Vector3.up * 1.4f;
                route = eastCourtRoute; waypoint = 0;
            }
            if (eastCourtHold > 0f)
            {
                Stop();
                Aim(eastCourtShots[eastCourtPoint]);
                if (Time.realtimeSinceStartup < eastCourtHold) return false;
                // A -nographics process has no device to render with; it still walks and stops at the same points.
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureView(".east-court-" + (++eastCourtShot) + ".png");
                eastCourtHold = 0f;
                eastCourtShots.Remove(eastCourtPoint);
            }
            var before = waypoint;
            if (!Follow())
            {
                if (waypoint != before && eastCourtShots.ContainsKey(waypoint - 1))
                { eastCourtPoint = waypoint - 1; eastCourtHold = Time.realtimeSinceStartup + 1.5f; }
                return false;
            }
            report.eastCourtLoops++;
            eastCourtDone = true; route = null;
            EastCourtClock(false);
            return true;
        }

        private void EastCourtClock(bool night)
        {
            if (!Host || !EastCourtNight) return;
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (engine == null) return;
            var saved = engine.ExportDay();
            if (night && eastCourtSavedMinute < 0) { eastCourtSavedMinute = saved.ClockMinute; saved.ClockMinute = 22 * 60; engine.RestoreDay(saved); }
            else if (!night && eastCourtSavedMinute >= 0) { saved.ClockMinute = eastCourtSavedMinute; eastCourtSavedMinute = -1; engine.RestoreDay(saved); }
        }
    }
}
