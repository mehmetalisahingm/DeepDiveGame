using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // Explicit development command: real owner input/RPCs, live NGO disconnect/rejoin,
    // then the real day engine at an accelerated clock rate. No material/progress seeding.
    public sealed partial class ProductDaySmokeDriver
    {
        private bool RepairInterruptions => Arg("-p187-interruptions") == "1";
        private int repairPhase;
        private ulong repairWinner;
        private bool raceSent, disconnectSent, reconnectSent, midnightStarted;
        private float reconnectAt, clockRate, midnightObservedAt;

        private void SetRepairPhase(int value)
        {
            if (repairPhase == value) return;
            repairPhase = value;
            route = null; waypoint = part = 0; aimAt = 0;
            repairApproach = repairAtDock = repairExit = partRoute = engineEntered = false;
            stageDeadline = Time.realtimeSinceStartup + 170f;
            Debug.Log("P187_ACCEPTANCE phase=" + value + " winner=" + repairWinner);
        }

        private bool RepairReconnect(float now)
        {
            if (!disconnectSent || reconnectSent) return false;
            if (adapter.Connection.Status != ConnectionStatus.Offline) return true;
            if (now < reconnectAt) return true;
            reconnectSent = true; tracked = false;
            route = null; repairApproach = repairAtDock = false;
            if (!adapter.JoinRoom("127.0.0.1", ushort.Parse(Arg("-p46-day-port", "24777"))))
                Fail("Repair guest could not rejoin the live Prep session");
            return true;
        }

        private bool RepairAcceptance()
        {
            var site = BoatRepairNetworkBinding.Instance;
            if (site == null || !site.OnCradle) return false;
            var peers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            if (Host)
            {
                if (repairPhase == 0)
                {
                    var contenders = peers.Where(p => p.OwnerClientId == 1 || p.OwnerClientId == 2).ToArray();
                    var source = GameObject.Find("BoatPart_Hull");
                    if (source != null && contenders.Length == 2 && contenders.All(p =>
                        Vector3.Distance(p.transform.position, source.transform.position + Vector3.back * 1.25f +
                            Vector3.right * (p.OwnerClientId == 1 ? -.6f : .6f)) < .4f)) SetRepairPhase(1);
                }
                if (repairPhase == 1)
                {
                    var carriers = peers.Where(p => p.RepairPartIndex.Value == 0).ToArray();
                    if (carriers.Length > 1) { Fail("Same material reserved by two live clients"); return false; }
                    if (carriers.Length == 1 && peers.Any(p => (p.OwnerClientId == 1 || p.OwnerClientId == 2) &&
                        p != carriers[0] && p.LastActionRequestId.Value > 0 && p.LastActionResult.Value != (int)PlayerActionResult.Accepted))
                    {
                        repairWinner = carriers[0].OwnerClientId;
                        report.repairRace = true; SetRepairPhase(2);
                    }
                }
                if (repairPhase == 2 && !manager.ConnectedClients.ContainsKey(repairWinner) &&
                    (sync.BoatMaterialsMask.Value & 1) != 0 && sync.BoatPartsDone.Value == 0 &&
                    peers.All(p => p.RepairPartIndex.Value != 0))
                { report.repairDisconnect = true; SetRepairPhase(3); }
                if (repairPhase == 3 && peers.Length == expected && sync.BoatPartsMask.Value == 1)
                { report.repairRejoin = true; SetRepairPhase(4); }
                if (repairPhase == 4 && midnightStarted && adapter.GetComponent<DayNetworkBinding>().Engine.DayNumber > firstDay)
                {
                    var engine = adapter.GetComponent<DayNetworkBinding>().Engine;
                    engine.GameMinutesPerRealSecond = clockRate;
                    if (midnightObservedAt == 0) midnightObservedAt = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - midnightObservedAt < 2f) { Stop(); return false; }
                    if (peers.Any(p => p.RepairHandsBusy)) { Fail("Transient repair ownership survived midnight"); return false; }
                    if (sync.BoatPartsMask.Value != 1 || (sync.BoatMaterialsMask.Value & 2) == 0)
                    { Fail("Midnight lost acquired material or completed a partial repair"); return false; }
                    report.repairMidnight = true; SetRepairPhase(5);
                }
            }
            if (repairPhase == 5) return true;
            if (repairPhase <= 1 && (report.slot == 1 || report.slot == 2))
            {
                var source = GameObject.Find("BoatPart_Hull");
                if (source == null) { Stop(); return false; }
                var stand = source.transform.position + Vector3.back * 1.25f + Vector3.right * (report.slot == 1 ? -.6f : .6f);
                stand.y = ContinuousTownLayout.WalkSurfaceY;
                if (route == null) SetRoute(new[] { P(0, -24), P(0, -16), P(stand.x, -16), stand });
                if (!Follow()) return false;
                Aim(source.transform.position);
                if (repairPhase == 1 && !raceSent && Settled())
                { raceSent = true; local.SubmitPickupLocal(); }
                return false;
            }
            if (repairPhase == 2 && local.OwnerClientId == repairWinner)
            {
                part = 0;
                if (local.RepairPlaced.Value && local.RepairHits.Value >= 1)
                {
                    disconnectSent = true; reconnectAt = Time.realtimeSinceStartup + 3.5f;
                    adapter.LeaveRoom(); return false;
                }
                return true;
            }
            if (Host && repairPhase == 3) { part = 0; return true; }
            if (Host && repairPhase == 4)
            {
                part = 1;
                if (local.RepairPlaced.Value && local.RepairHits.Value >= 1)
                {
                    if (!midnightStarted)
                    {
                        var engine = adapter.GetComponent<DayNetworkBinding>().Engine;
                        clockRate = engine.GameMinutesPerRealSecond;
                        // Drive midnight through DayManager.Update -> Tick -> actual close hooks/save.
                        engine.GameMinutesPerRealSecond = 960f;
                        midnightStarted = true;
                    }
                    Stop(); return false;
                }
                return true;
            }
            var park = P(10 + report.slot * 1.2f, -23);
            if (route == null) SetRoute(new[] { P(0, -24), park });
            Follow(); return false;
        }
    }
}
