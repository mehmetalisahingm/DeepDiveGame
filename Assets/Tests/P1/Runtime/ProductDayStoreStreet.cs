using System.Collections.Generic;
using DeepDive.Composition;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #296 explicit development walk (-p296-store-street): before the day's normal walk, every player walks with owner
    // input from home to the square, along the side street to the equipment store's door and back to the square,
    // capturing its own view at the street's turn (towards the door) and at the door (back along the street). Everyone
    // keeps right, 1.2 s apart, so pairs meet in the street and four reach the door together. With -p296-night 1 the host
    // applies a LABELLED clock fixture (22:00 for the walk). No teleport, no speed change.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool StoreStreetWalk => Arg("-p296-store-street") == "1";
        private bool StoreStreetNight => Arg("-p296-night") == "1";
        private bool storeStreetDone, storeStreetStarted;
        private int storeStreetSavedMinute = -1, storeStreetPoint, storeStreetShot;
        private float storeStreetHold, storeStreetStartAt;
        private readonly Dictionary<int, Vector3> storeStreetShots = new Dictionary<int, Vector3>();

        private bool StoreStreetTick()
        {
            if (storeStreetDone) return true;
            if (!storeStreetStarted)
            {
                storeStreetStarted = true;
                StoreStreetClock(true);
                storeStreetStartAt = Time.realtimeSinceStartup + report.slot * 1.2f;
                var street = ContinuousTownLayout.StoreStreet;
                var points = new List<Vector3> { H(-4.7f, -2f), H(-4.7f, 5f), ContinuousTownLayout.HomeRig.Door, P(0f, -36f) };
                // Out on the right of the street, a spot of one's own at the door, back on the other side.
                var outward = Shift(street, 0.55f);
                points.AddRange(outward);
                var turn = points.Count - 2;
                var door = ContinuousTownLayout.EquipmentStoreDoorOutside;
                points[points.Count - 1] = P(door.x + (report.slot % 2 == 0 ? 0.6f : -0.6f), door.z + 0.3f + (report.slot / 2) * 0.9f);
                var atDoor = points.Count - 1;
                var back = new List<Vector3>(street); back.Reverse();
                points.AddRange(Shift(back.ToArray(), 0.55f));
                storeStreetShots[turn] = door + Vector3.up * 1.6f;
                storeStreetShots[atDoor] = street[0] + Vector3.up * 1.5f;
                route = points; waypoint = 0;
            }
            if (Time.realtimeSinceStartup < storeStreetStartAt) { Stop(); return false; }
            if (storeStreetHold > 0f)
            {
                Stop();
                Aim(storeStreetShots[storeStreetPoint]);
                if (Time.realtimeSinceStartup < storeStreetHold) return false;
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureView(".store-street-" + (++storeStreetShot) + ".png");
                storeStreetHold = 0f;
                storeStreetShots.Remove(storeStreetPoint);
            }
            var before = waypoint;
            if (!Follow())
            {
                if (waypoint != before && storeStreetShots.ContainsKey(waypoint - 1))
                { storeStreetPoint = waypoint - 1; storeStreetHold = Time.realtimeSinceStartup + 1.5f; }
                return false;
            }
            report.storeStreetWalks++;
            storeStreetDone = true; route = null;
            StoreStreetClock(false);
            return true;
        }

        // The polyline moved `right` metres to the walker's right on every leg.
        private static List<Vector3> Shift(Vector3[] path, float right)
        {
            var shifted = new List<Vector3>();
            for (var i = 0; i < path.Length; i++)
            {
                var a = path[Mathf.Max(0, i - 1)];
                var b = path[Mathf.Min(path.Length - 1, i + 1)];
                var dir = new Vector2(b.x - a.x, b.z - a.z).normalized;
                shifted.Add(P(path[i].x + dir.y * right, path[i].z - dir.x * right));
            }
            return shifted;
        }

        private void StoreStreetClock(bool night)
        {
            if (!Host || !StoreStreetNight) return;
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (engine == null) return;
            var saved = engine.ExportDay();
            if (night && storeStreetSavedMinute < 0) { storeStreetSavedMinute = saved.ClockMinute; saved.ClockMinute = 22 * 60; engine.RestoreDay(saved); }
            else if (!night && storeStreetSavedMinute >= 0) { saved.ClockMinute = storeStreetSavedMinute; storeStreetSavedMinute = -1; engine.RestoreDay(saved); }
        }
    }
}
