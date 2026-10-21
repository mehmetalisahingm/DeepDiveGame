using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // Development-build, command-line opt-in. Uses the product spawn provider, input RPCs,
    // service interaction and physical bed request. Never moves a transform or calls Teleport.
    public sealed class ContinuousWorldSmokeDriver : MonoBehaviour
    {
        private const string ProgressMessage = "test/p46/world-progress";
        [Serializable] private sealed class Report
        {
            public bool passed, homeSpawn, shopOpened, dockReached, swam, pcOpened, slept, morningHome, earlySleepSummary, reloadClean;
            public int players, worldLoads, completedStep, firstDay, morningDay;
            public float distance, largestTravelStep;
            public string position, phase;
            public List<string> trace = new List<string>(), errors = new List<string>();
        }
        private readonly Report report = new Report();
        private readonly Dictionary<ulong, int> progress = new Dictionary<ulong, int>();
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private NetworkPlayer local;
        private int expected, slot, step, waypoint, firstDay;
        private string output;
        private float deadline, nextReport, waitUntil, aimAt, nextTrace;
        private Vector3 previous;
        private bool tracking, finished, leaving;
        private List<Vector3> route;

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p46-world-role").Length == 0) return;
            var go = new GameObject(nameof(ContinuousWorldSmokeDriver));
            DontDestroyOnLoad(go); go.AddComponent<ContinuousWorldSmokeDriver>();
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            output = Arg("-p1-report"); expected = int.Parse(Arg("-p46-world-count", "2"));
            slot = int.Parse(Arg("-p46-world-slot", "0"));
            deadline = Time.realtimeSinceStartup + 230f;
            SceneManager.sceneLoaded += SceneLoaded;
            Application.logMessageReceived += Log;
            yield return null; yield return null;
            adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            manager = adapter.GetComponent<NetworkManager>();
            var port = ushort.Parse(Arg("-p46-world-port", "19777"));
            // Optional explicit host is for *actual separate-computer* regression.
            // Default remains localhost for existing 1/2/4 runner; no network authority changes.
            var started = slot == 0 ? adapter.CreateRoom(port) :
                adapter.JoinRoom(Arg("-p46-world-host", "127.0.0.1"), port);
            if (!started) Fail("Could not start session: " + adapter.Connection.LastError);
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == ContinuousTownLayout.SceneName) report.worldLoads++;
        }

        private void Update()
        {
            if (finished || adapter == null) return;
            if (Time.realtimeSinceStartup > deadline) { Fail("Timed out at step " + step + " waypoint " + waypoint); return; }
            if (leaving)
            {
                if (adapter.Connection.Status == ConnectionStatus.Offline) Finish();
                return;
            }
            if (adapter.Connection.Status != ConnectionStatus.Connected || adapter.Connection.IsSceneLoading) return;
            if (messages == null && manager.CustomMessagingManager != null)
            {
                messages = manager.CustomMessagingManager;
                if (adapter.IsAuthority) messages.RegisterNamedMessageHandler(ProgressMessage, ProgressReceived);
            }
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            report.players = Math.Max(report.players, players.Length);
            local = players.FirstOrDefault(p => p.IsOwner);
            if (local == null) return;
            local.ReadKeyboard = false;
            var state = adapter.Session.State;
            var sync = local.GetComponent<EconomyPlayerSync>();
            if (step == 0 && state.Revision == 0)
            {
                if (adapter.Session.Roster.TryGetValue(new PlayerId(local.OwnerClientId), out var ready) && !ready)
                    adapter.ToggleReady();
                if (adapter.IsAuthority && adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(r => r))
                    adapter.AdvancePhase();
                return;
            }
            if (SceneManager.GetActiveScene().name != ContinuousTownLayout.SceneName) return;
            if (step == 0)
            {
                var home = ContinuousTownLayout.HomeRig.Spawn(slot);
                report.homeSpawn = Vector3.Distance(local.transform.position, home) < 0.7f;
                // Scene completion and the authoritative spawn transform can arrive on adjacent ticks.
                if (!report.homeSpawn)
                {
                    if (waitUntil == 0) waitUntil = Time.realtimeSinceStartup + 5f;
                    if (Time.realtimeSinceStartup > waitUntil) Fail("Product spawn is outside home slot " + slot);
                    return;
                }
                firstDay = sync.DayNumber.Value;
                report.firstDay = firstDay;
                if (int.TryParse(Arg("-p46-world-reload-day"), out var reloadDay))
                {
                    var summary = adapter.GetComponent<DayNetworkBinding>().Engine.LastSummary;
                    report.reloadClean = firstDay == reloadDay && summary != null && summary.DayNumber == reloadDay - 1 &&
                        summary.Reason == DayCloseReason.EarlySleep;
                    Finish(); return;
                }
                previous = local.transform.position; tracking = true;
                waitUntil = Time.realtimeSinceStartup + slot * 2f;
                SetRoute(Outward()); SetStep(1);
            }
            Track(sync);
            if (finished) return;
            if (Time.realtimeSinceStartup >= nextTrace)
            {
                nextTrace = Time.realtimeSinceStartup + 5f;
                Debug.Log($"P46_WORLD_TRAVEL step={step} waypoint={waypoint} position={local.transform.position} swimming={local.Swimming.Value}");
            }
            if (Time.realtimeSinceStartup >= nextReport)
            {
                nextReport = Time.realtimeSinceStartup + 0.25f;
                if (adapter.IsAuthority) progress[local.OwnerClientId] = step;
                else if (messages != null)
                {
                    using var writer = new FastBufferWriter(4, Allocator.Temp);
                    writer.WriteValueSafe(step);
                    messages.SendNamedMessage(ProgressMessage, NetworkManager.ServerClientId, writer);
                }
            }
            switch (step)
            {
                case 1:
                    if (Time.realtimeSinceStartup < waitUntil) { Stop(); return; }
                    if (!Follow()) return;
                    report.dockReached = !local.Swimming.Value &&
                        Mathf.Abs(local.transform.position.y - ContinuousTownLayout.WalkSurfaceY) < 0.2f;
                    if (!report.dockReached) { Fail("Dock endpoint is not dry ground"); return; }
                    SetStep(2); break;
                case 2:
                    Stop();
                    if (adapter.IsAuthority && AllAt(2) && state.Phase == SessionPhase.Prep) adapter.AdvancePhase();
                    if (state.Phase == SessionPhase.Dive)
                    {
                        SetRoute(new[] { P(9f, -9f), P(9f, -12.5f), P(0f, -12.5f), P(-6.5f, -11.2f),
                            P(-6.5f, -8.7f), new Vector3(-7.6f + slot * 0.75f, 6f, -5f) });
                        // On the 2m-wide pier the rear row leaves first, so it cannot trap
                        // the front row. No actor is placed outside the authored dock.
                        waitUntil = Time.realtimeSinceStartup + (expected - 1 - slot) * 2f;
                        SetStep(3);
                    }
                    break;
                case 3:
                    if (Time.realtimeSinceStartup < waitUntil) { Stop(); return; }
                    report.swam |= local.Swimming.Value;
                    if (!Follow()) return;
                    if (!report.swam) { Fail("Water route never entered swimming"); return; }
                    SetStep(4); break;
                case 4:
                    Stop();
                    if (adapter.IsAuthority && AllAt(4) && state.Phase == SessionPhase.Dive) adapter.AdvancePhase();
                    if (state.Phase == SessionPhase.Return)
                    {
                        SetRoute(Homeward()); waitUntil = Time.realtimeSinceStartup + slot * 2f; SetStep(5);
                    }
                    break;
                case 5:
                    if (Time.realtimeSinceStartup < waitUntil) { Stop(); return; }
                    if (!Follow()) return;
                    Stop();
                    report.pcOpened |= HomePcView.PanelOpen;
                    if (!report.pcOpened) return;
                    SetStep(6); break;
                case 6:
                    Stop();
                    if (adapter.IsAuthority && AllAt(6) && state.Phase == SessionPhase.Return) adapter.AdvancePhase();
                    if (state.Phase == SessionPhase.Lobby && state.Revision >= 4)
                    {
                        var bed = Bed();
                        SetRoute(new[] { H(4.95f, -2.5f), H(4.95f, 0f),
                            new Vector3(bed.ExitPose.position.x, ContinuousTownLayout.WalkSurfaceY, bed.ExitPose.position.z) });
                        SetStep(7);
                    }
                    break;
                case 7:
                    if (!Follow()) return;
                    Aim(Bed().GetComponent<Collider>().bounds.center);
                    if (aimAt == 0) aimAt = Time.realtimeSinceStartup + 0.8f;
                    if (Time.realtimeSinceStartup < aimAt) return;
                    typeof(HomePlayerInteractionBinding).GetMethod("SendRequest", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(adapter.GetComponent<HomePlayerInteractionBinding>(), new object[] { (byte)1 });
                    SetStep(8); break;
                case 8:
                    report.slept |= local.Seated.Value || HomePlayerInteractionBinding.TryGetBed(local.OwnerClientId, out _);
                    // The last/solo H request can synchronously complete the morning before the
                    // next frame. The authoritative early-sleep summary proves that acceptance.
                    report.earlySleepSummary = sync.SummaryDayNumber.Value == firstDay &&
                        sync.SummaryReason.Value == (byte)DayCloseReason.EarlySleep;
                    report.slept |= report.earlySleepSummary;
                    // Bed placement and the explicit new-day placement are the only permitted position discontinuities.
                    if (sync.DayNumber.Value <= firstDay || local.Seated.Value || DayLock.IsLocked || !report.earlySleepSummary) return;
                    report.morningDay = sync.DayNumber.Value;
                    report.morningHome = Vector3.Distance(local.transform.position, ContinuousTownLayout.HomeRig.Spawn(slot)) < 0.7f;
                    if (!report.morningHome) { Fail("Morning did not return to the reserved home slot"); return; }
                    SetStep(9); break;
                case 9:
                    Stop();
                    if (adapter.IsAuthority && AllAt(9))
                    {
                        if (waitUntil == 0 || waitUntil < Time.realtimeSinceStartup - 10f) waitUntil = Time.realtimeSinceStartup + 2f;
                        if (Time.realtimeSinceStartup >= waitUntil) { leaving = true; adapter.LeaveRoom(); }
                    }
                    else if (!adapter.IsAuthority) { /* host shutdown finishes the run below */ }
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!finished && step == 9 && !adapter.IsAuthority && adapter.Connection.Status == ConnectionStatus.Offline) Finish();
        }

        private void Track(EconomyPlayerSync sync)
        {
            if (!tracking) return;
            var delta = Vector3.Distance(previous, local.transform.position);
            previous = local.transform.position;
            if (step >= 8 || local.Seated.Value || sync.DayNumber.Value > firstDay) return;
            report.distance += delta;
            report.largestTravelStep = Mathf.Max(report.largestTravelStep, delta);
            if (delta > 1.5f) Fail("Unexpected movement discontinuity: " + delta);
            if (report.worldLoads != 1) Fail("World reloaded during daily travel: " + report.worldLoads);
        }

        private void ProgressReceived(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int value);
            if (manager.ConnectedClients.ContainsKey(sender) && value >= 0 && value <= 9) progress[sender] = value;
        }
        private bool AllAt(int value) => manager.ConnectedClientsIds.Count == expected &&
            manager.ConnectedClientsIds.All(id => progress.TryGetValue(id, out var p) && p >= value);
        private void SetStep(int value)
        {
            step = value; report.completedStep = value;
            report.trace.Add(value + " " + adapter.Session.State.Phase + " " + local.transform.position);
            Debug.Log("P46_WORLD_STEP " + report.trace.Last());
        }
        private void SetRoute(IEnumerable<Vector3> points) { route = points.ToList(); waypoint = 0; }
        private static Vector3 P(float x, float z) => new Vector3(x, ContinuousTownLayout.WalkSurfaceY, z);
        private static Vector3 H(float x, float z) => ContinuousTownLayout.HomeOrigin + new Vector3(x, 0f, z);
        private IEnumerable<Vector3> Outward()
        {
            yield return H(-4.7f, -2f); yield return H(-4.7f, 5f); yield return ContinuousTownLayout.HomeRig.Door;
            yield return P(0f, -36f); yield return P(0f, -24f);
            // Stop in front of the real equipment store and send the same F request as keyboard input.
            // Separate standing spots let arrivals and departures pass each other at the counter.
            var shop = ContinuousTownLayout.StorefrontPositions[0] +
                Vector3.right * ((slot - (expected - 1) * 0.5f) * 0.9f);
            yield return shop + new Vector3(0f, 0f, 2.8f);
            yield return shop + new Vector3(0f, 0f, 2f);
            yield return shop + new Vector3(0f, 0f, 2.8f);
            yield return P(0f, -24f); yield return P(0f, -16f); yield return P(0f, -12.5f);
            yield return P(9f, -12.5f); yield return P(9f, -9f);
            yield return P(8.55f + (slot % 2) * 0.9f, -4.5f - (slot / 2) * 1f);
        }
        private IEnumerable<Vector3> Homeward()
        {
            yield return P(-6.5f, -8.7f); yield return P(-6.5f, -11.2f); yield return P(0f, -12.5f);
            yield return P(0f, -16f); yield return P(0f, -24f); yield return P(0f, -36f);
            yield return ContinuousTownLayout.HomeRig.Door; yield return H(4.95f, 5f);
            yield return H(4.95f, -2.5f); yield return H(2f + slot * 0.8f, -2.4f);
        }
        private HomeInteractionAnchor Bed() => FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None)
            .Single(b => b.Kind == HomeInteractionKind.Bed && b.BedId == DayIds.Beds[slot]);

        private bool Follow()
        {
            if (waypoint >= route.Count) return true;
            var target = route[waypoint];
            var heading = target - local.transform.position;
            var flat = new Vector2(heading.x, heading.z).magnitude;
            // Dry-route y is a floor datum, not a command to swim above the water surface.
            // Only the explicit final submerged waypoint requires its target depth.
            var needsDepth = step == 3 && waypoint == route.Count - 1;
            if (flat < 0.3f && (!needsDepth || Mathf.Abs(heading.y) < 0.5f))
            {
                // The request is aim/range validated by the host; no service id or reward fixture.
                if (step == 1 && waypoint == 6)
                {
                    Aim(ContinuousTownLayout.StorefrontPositions[0] + Vector3.up);
                    if (aimAt == 0) aimAt = Time.realtimeSinceStartup + 0.7f;
                    if (Time.realtimeSinceStartup < aimAt) return false;
                    local.SubmitServiceInteractionLocal();
                    if (!local.GetComponent<EconomyPlayerSync>().ShopOpen) return false;
                    report.shopOpened = true; aimAt = 0;
                }
                waypoint++; return waypoint >= route.Count;
            }
            // A fresh campaign now has the real broken hull on the dry beach. Keep the
            // original waypoint/service assertions, but walk around that solid hull just
            // like the physical-repair driver instead of steering straight through it.
            var repair = BoatRepairNetworkBinding.Instance;
            if (!local.Swimming.Value && repair != null && repair.Blocking)
            {
                heading = repair.NextAround(local.transform.position, target) - local.transform.position;
                flat = new Vector2(heading.x, heading.z).magnitude;
            }
            var yaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            var move = Vector3.forward;
            if (local.Swimming.Value)
            {
                // Climbing out of the sea must use the actual wade slope and upward input.
                move.y = Mathf.Clamp(heading.y, -1f, 1f);
                if (flat < 0.15f) move.z = 0f;
            }
            local.SubmitLocalInput(move.normalized * Mathf.Min(1f, Mathf.Max(flat, Mathf.Abs(heading.y))), yaw, 0f);
            return false;
        }
        private void Stop() => local.SubmitLocalInput(Vector3.zero, 0f, 0f);
        private void Aim(Vector3 target)
        {
            var direction = target - local.GetComponentInChildren<Camera>(true).transform.position;
            local.SubmitLocalInput(Vector3.zero, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
                -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
        }
        private void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) report.errors.Add(message);
        }
        private void Fail(string reason) { report.errors.Add(reason); Finish(); }
        private void Finish()
        {
            if (finished) return;
            finished = true;
            if (local != null) report.position = local.transform.position.ToString();
            report.phase = adapter != null ? adapter.Session.State.Phase.ToString() : "missing";
            report.passed = step == 9 && report.players == expected && report.worldLoads == 1 && report.homeSpawn &&
                report.shopOpened && report.dockReached && report.swam && report.pcOpened && report.slept && report.morningHome &&
                report.distance > 120f && report.errors.Count == 0;
            if (Arg("-p46-world-reload-day").Length > 0)
                report.passed = report.reloadClean && report.homeSpawn && report.players == 1 && report.worldLoads == 1 && report.errors.Count == 0;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("P46_WORLD_RESULT passed=" + report.passed);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
