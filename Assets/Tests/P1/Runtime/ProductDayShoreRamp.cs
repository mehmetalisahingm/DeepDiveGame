using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #297 explicit development walk (-p297-shore-ramp): before the day's normal walk, every player walks with owner input
    // from home through the square onto the beach, up the side apron (ShoreRampApron) onto the dock ramp and its top
    // landing, then back over the platform and the beach to the square. Each captures its own view at the apron foot
    // (towards the apron, where the player ahead is climbing) and at the landing (back down the apron, where the players
    // behind are coming up). At each stop it waits up to 8 s for another player to be on the apron and then aims at that
    // player, so the frame shows someone walking it; with nobody there it frames the empty apron (the foot stop waits only
    // 1.5 s). Nobody starts back until everyone is up, then 2 s apart on two lanes, so the way back never crosses a climber's lane.
    // Lanes and landing spots are the four P297ShoreDockAudit proved with the real controller;
    // starts are 1.2 s apart. No teleport, no speed change.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool ShoreRampWalk => Arg("-p297-shore-ramp") == "1";
        private bool shoreRampDone, shoreRampStarted;
        private int shoreRampPoint, shoreRampShot;
        private float shoreRampHold, shoreRampHoldStart, shoreRampStartAt, shoreRampWalkerSince, shoreRampReleaseAt, shoreRampUpSince;
        private int shoreRampTop = -1;
        private const float ShoreRampMinHold = 1.0f, ShoreRampFootHold = 1.5f, ShoreRampTopHold = 8f, ShoreRampAllUpWait = 25f;
        private readonly Dictionary<int, Vector3> shoreRampShots = new Dictionary<int, Vector3>();

        // True once this player is back in the square.
        private bool ShoreRampTick()
        {
            if (shoreRampDone) return true;
            if (!shoreRampStarted)
            {
                shoreRampStarted = true;
                shoreRampStartAt = Time.realtimeSinceStartup + report.slot * 1.2f;
                // Until the boat is repaired its hull sits on the beach cradle (x 1.1 .. 6.1, z -13.8 .. -11.4, 0.4 m high),
                // 0.4 m short of the apron's foot, so the apron is reached through the sand east of the hull (x 6.1 .. 7):
                // round the hull's south side, a hold spot each, then single file up x 6.6 onto the apron.
                var footSpot = P(6.6f + report.slot * 0.9f, -14.9f);
                var landing = new[] { P(8.3f, -9.6f), P(9.1f, -9.0f), P(8.0f, -8.4f), P(10.3f, -10.4f) }[report.slot];
                var backX = 7.8f + (report.slot % 2) * 0.6f;
                var points = new List<Vector3>
                {
                    H(-4.7f, -2f), H(-4.7f, 5f), ContinuousTownLayout.HomeRig.Door, P(0f, -36f), P(0f, -24f), P(-1.8f, -16f),
                    P(0.3f, -15.1f), footSpot
                };
                var foot = points.Count - 1;
                points.Add(P(6.6f, -13.2f));
                points.Add(P(6.6f, -10.4f));   // up the apron's east half and onto the ramp
                points.Add(landing);
                var top = points.Count - 1;
                shoreRampTop = top;
                // Back over the ramp top and the platform and round the hull's south side, two lanes, 2 s apart.
                var backZ = report.slot % 2 == 0 ? -14.8f : -15.35f;
                points.Add(P(backX, -11.8f));
                points.Add(P(backX, backZ));
                points.Add(P(0.3f, backZ));
                points.Add(P(-1.8f, -16f));
                points.Add(P(0f, -24f));
                var apronMiddle = new Vector3((ShoreRampApron.MinX + ShoreRampApron.MaxX) / 2f, ShoreRampApron.PlatformTopY,
                    ShoreRampApron.SeamZ + ShoreRampApron.Run / 2f);
                shoreRampShots[foot] = apronMiddle + Vector3.up * 0.9f;
                shoreRampShots[top] = apronMiddle + Vector3.up * 0.6f;
                route = points; waypoint = 0;
            }
            NoteShoreRampHeight();
            if (Time.realtimeSinceStartup < shoreRampStartAt) { Stop(); return false; }
            if (shoreRampHold > 0f)
            {
                Stop();
                var walker = PlayerOnApron();
                var now = Time.realtimeSinceStartup;
                if (walker == null) shoreRampWalkerSince = 0f;
                else if (shoreRampWalkerSince <= 0f) shoreRampWalkerSince = now;
                Aim(walker != null ? walker.transform.position + Vector3.up * 1.2f : shoreRampShots[shoreRampPoint]);
                var held = now - shoreRampHoldStart;
                var walkerFramed = walker != null && now - shoreRampWalkerSince >= 0.35f;
                if (held < ShoreRampMinHold || (!walkerFramed && now < shoreRampHold)) return false;
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    CaptureView(".shore-ramp-" + (++shoreRampShot) + ".png");
                report.shoreRampFrames.Add($"shot {shoreRampShot} from={local.transform.position} " +
                    (walker != null ? $"walker=client{walker.OwnerClientId} at={walker.transform.position}" : "walker=none"));
                shoreRampHold = 0f;
                shoreRampWalkerSince = 0f;
                shoreRampShots.Remove(shoreRampPoint);
            }
            if (waypoint == shoreRampTop + 1 && !ShoreRampMayGoBack()) { Stop(); return false; }
            var before = waypoint;
            if (!Follow())
            {
                if (waypoint != before && shoreRampShots.ContainsKey(waypoint - 1))
                {
                    shoreRampPoint = waypoint - 1;
                    shoreRampHoldStart = Time.realtimeSinceStartup;
                    shoreRampHold = shoreRampHoldStart + (shoreRampPoint == shoreRampTop ? ShoreRampTopHold : ShoreRampFootHold);
                }
                return false;
            }
            report.shoreRampWalks++;
            shoreRampDone = true; route = null;
            return true;
        }

        // Once everyone is past the apron's foot (or after 25 s), leave in slot order 2 s apart.
        private bool ShoreRampMayGoBack()
        {
            var now = Time.realtimeSinceStartup;
            if (shoreRampReleaseAt <= 0f)
            {
                if (shoreRampUpSince <= 0f) shoreRampUpSince = now;
                var allUp = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                    .Where(p => p.IsSpawned).All(p => p.transform.position.z > ShoreRampApron.SeamZ - 0.2f);
                if (!allUp && now - shoreRampUpSince < ShoreRampAllUpWait) return false;
                shoreRampReleaseAt = now + report.slot * 2f;
                report.shoreRampFrames.Add($"back allUp={allUp} waited={now - shoreRampUpSince:0.0}");
            }
            return now >= shoreRampReleaseAt;
        }

        // Another live player standing on the apron or where it meets the ramp.
        private NetworkPlayer PlayerOnApron() =>
            FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p != local &&
                p.transform.position.x > ShoreRampApron.MinX - 0.2f && p.transform.position.x < ShoreRampApron.MaxX + 0.2f &&
                p.transform.position.z > ShoreRampApron.SeamZ - 0.3f && p.transform.position.z < ShoreRampApron.SeamZ + ShoreRampApron.Run + 0.4f);

        // The lowest the feet got over the apron/ramp join (x 4.8 .. 6.8, z -10.6 .. -9.6): below the platform's 8.4 only
        // if this player really stepped down the apron onto the ramp rather than round it.
        private void NoteShoreRampHeight()
        {
            if (local == null) return;
            var p = local.transform.position;
            if (p.x < ShoreRampApron.MinX || p.x > 6.8f || p.z < -10.6f || p.z > -9.6f) return;
            report.shoreRampLowestY = report.shoreRampLowestY <= 0f ? p.y : Mathf.Min(report.shoreRampLowestY, p.y);
        }
    }
}
