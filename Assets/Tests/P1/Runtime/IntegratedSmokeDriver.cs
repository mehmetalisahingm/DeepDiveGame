using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using DeepDive.Inventory;
using DeepDive.Economy;
using DeepDive.Trip;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace DeepDive.P1.Lab
{
    // Opt-in automation only, never a scene component or a replacement game provider.
    public sealed class IntegratedSmokeDriver : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string mode, reason;
            public bool passed, connected, roster, ready, prep, dive, returned, readyReset;
            public bool walk, swim, collisions, cameras, stopped, clientRejoined;
            public bool fishMoved, catchObserved, catchDespawned, inventoryAdded;
            public bool huntShooter, inventoryReplicated;
            public bool recordingStarted, recordingStopped, recordingClaimed, recordingViews, recordingPending;
            public bool recordingViewActive, recordingSafe;
            public bool recordingRoleResolved, recordingRecorder;
            public bool recordingPaid, eventOpened, eventClosed, tubePurchased, saveLoaded;
            public bool townNoAutoPay, townSold, townDenied, townShopOpened, townCameraBought, townPartBought;
            public bool townProgress, townHostChecks, townSaveRoundTrip;
            public int townBalance;
            public List<int> boatPartsSequence = new List<int>();
            public bool boatHullFound, boatEngineFound, boatFuelTankFound, boatDuplicateRejected, boatRepaired;
            public string boatStatusFinal;
            public bool boatPartsHidden;
            public List<string> tripPhases = new List<string>();
            public string tripSeatId;
            public bool tripBoarded, tripDuplicateBoardHeld, tripMapDockedAtDock, tripMapUnderwayMoved,
                tripMapAnchoredAtAnchor, tripReturnMarkerSeen, tripReboarded, tripDockedEmpty, tripDone, tripSaveReload;
            public int tripMapPlayersMax, tripUnderwayPositions;
            public List<string> tripTrace = new List<string>();
            public List<int> dayNumbers = new List<int>(), daySummaries = new List<int>();
            public bool dayClockAdvanced, dayMidnightClosed, daySavedOnDisk, dayReloadNoAdvance, dayReplayIgnored,
                dayBedAccepted, dayEarlyGateHeld, dayEarlyClosed, dayHostDone, dayReloadPass;
            public int dayStartMinute, dayLostDivers, dayMirroredLostDivers, dayFinalNumber, dayReloadNumber, dayReloadHistory, dayReloadMinute;
            public int dayMirroredEarlyReason = -1, dayMirroredMidnightReason = -1;
            public string homeStatus = "";
            public bool homeBedAccepted, homeMorningClean, homePingSeen, homePingOnMap, homePingAccepted;
            public int homeSleepersMax;
            public float returnToPendingSeconds, returnToTurnInSeconds;
            public List<string> townTrace = new List<string>();
            public float recordingValidSeconds;
            public int recordingQuality;
            public int maxPlayers;
            public List<string> errors = new List<string>();
        }
        private readonly Result result = new Result();
        private SessionNetworkAdapter adapter;
        private string path, observedScene;
        private float sceneStarted;
        private double nextInput;
        private float nextAction;
        private Vector3? firstFishPosition;
        private bool Hunt => Arg("-p2-hunt") == "1";
        private bool Event => Arg("-p3-event") == "1";
        private bool Record => Arg("-p3-record") == "1" || Event;
        private bool Town => Arg("-p3-town") == "1";
        private bool Home => Arg("-p4-home") == "1";
        private bool Day => Arg("-p4-day") == "1";
        private bool DayReload => Arg("-p4-day-reload") == "1";
        private bool Trip => Arg("-p3-trip") == "1";
        private bool Boat => Arg("-p3-boat") == "1" || Trip;
        private bool purchaseSent;
        private int townStep, townBefore;
        private float townNext, townSettleAt;
        private int boatStep;
        private float boatSettleAt, boatNext;
        private int boatSequenceLast = -1;
        private bool townAwaiting;
        private float townTraceAt;
        private bool townInjected, townSampledReturn, cameraSeeded;
        private float returnPhaseAt, pendingSeenAt, turnInAt;
        private float recordingStartTime;
        private readonly Dictionary<ulong, Vector3> starts = new Dictionary<ulong, Vector3>();
        private readonly HashSet<ulong> walked = new HashSet<ulong>(), swam = new HashSet<ulong>();

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p1-integrated").Length == 0) return;
            var go = new GameObject("IntegratedSmokeDriver"); DontDestroyOnLoad(go);
            go.AddComponent<IntegratedSmokeDriver>();
        }
        private IEnumerator Start()
        {
            result.mode = Arg("-p1-integrated"); path = Arg("-p1-report");
            var expected = int.Parse(Arg("-p1-count", "4"));
            var port = ushort.Parse(Arg("-p1-port", "17777"));
            var host = result.mode == "host";
            var reject = result.mode == "reject";
            var rejoin = result.mode == "rejoin";
            Application.logMessageReceived += Log;
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            yield return null; yield return null;
            adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null) { result.errors.Add("No integrated adapter in real scene"); Finish(); yield break; }
            GameObject.Find("Port").GetComponent<InputField>().text = port.ToString();
            GameObject.Find(host ? "HostButton" : "JoinButton").GetComponent<Button>().onClick.Invoke();
            var started = Time.realtimeSinceStartup;
            bool prepSent = false, diveSent = false, returnSent = false, lobbySent = false, leaveSent = false;
            bool rejoinLeft = false, reconnectSent = false, sawOffline = false, lobbyLogged = false, unauthorizedSent = false, screenshot = false;
            bool diveScreenshot = false, shoreScreenshot = false;
            var leaveAt = 0f;
            var duration = Home ? 96f : DayReload ? 40f : Day ? 90f : Trip ? 134f : Boat ? 78f : Town ? 76f : Event ? 114f : Record ? 66f : Hunt ? 58f : 46f;
            while (Time.realtimeSinceStartup - started < duration)
            {
                var elapsed = Time.realtimeSinceStartup - started;
                var connection = adapter.Connection;
                var state = adapter.Session.State;
                if (host && !screenshot && elapsed > 5 && Arg("-p1-screenshot").Length > 0)
                { screenshot = true; CaptureRoom(); }
                if (host && Arg("-p1-screenshot").Length > 0 && !connection.IsSceneLoading)
                {
                    if (!diveScreenshot && state.Phase == SessionPhase.Dive && elapsed > 28)
                    { diveScreenshot = true; CaptureRoom("-dive"); }
                    if (!shoreScreenshot && state.Phase == SessionPhase.Return && returnPhaseAt > 0 && Time.realtimeSinceStartup - returnPhaseAt > 15)
                    { shoreScreenshot = true; CaptureRoom("-shore"); }
                }
                if (reject)
                {
                    if (connection.Status == ConnectionStatus.Offline && connection.LastError.Length > 0)
                    {
                        result.reason = connection.LastError;
                        result.passed = result.reason == Arg("-p1-reason") && result.errors.Count == 0;
                        Finish(); yield break;
                    }
                    yield return null; continue;
                }
                result.connected |= connection.Status == ConnectionStatus.Connected;
                result.maxPlayers = Math.Max(result.maxPlayers, adapter.Session.Roster.Count);
                result.roster |= adapter.Session.Roster.Count == expected && connection.Players.Count == expected;
                var allReady = adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => value);
                result.ready |= allReady;
                if (host && !lobbyLogged && result.roster)
                { lobbyLogged = true; Debug.Log("P1_INTEGRATED_LOBBY_READY"); }
                if (connection.Status == ConnectionStatus.Connected && !connection.IsSceneLoading)
                {
                    Probe(expected);
                    if (state.Phase == SessionPhase.Lobby && connection.LocalPlayerId.HasValue &&
                        adapter.Session.Roster.TryGetValue(connection.LocalPlayerId.Value, out var ready) && !ready && state.Revision == 0)
                        GameObject.Find("ReadyButton").GetComponent<Button>().onClick.Invoke();
                    // Exercise the server rejection as well as the disabled client UI.
                    if (!host && !unauthorizedSent && state.Phase == SessionPhase.Lobby && elapsed > 4)
                    { unauthorizedSent = true; adapter.AdvancePhase(); }
                }
                if (rejoin && elapsed > 8 && !rejoinLeft)
                { rejoinLeft = true; leaveAt = elapsed; adapter.LeaveRoom(); }
                if (rejoinLeft && connection.Status == ConnectionStatus.Offline) sawOffline = true;
                if (rejoinLeft && !reconnectSent && sawOffline && elapsed > leaveAt + 2)
                { reconnectSent = true; adapter.JoinRoom("127.0.0.1", port); }
                if (reconnectSent && connection.Status == ConnectionStatus.Connected && connection.Players.Count == expected)
                    result.clientRejoined = true;
                if (host && elapsed > (Home ? 40 : 19) && !prepSent && allReady && (!Home || result.dayNumbers.Contains(2)))
                { prepSent = true; GameObject.Find("BeginPrepButton").GetComponent<Button>().onClick.Invoke(); }
                result.prep |= state.Phase == SessionPhase.Prep && !connection.IsSceneLoading;
                if (host && elapsed > 22 && !diveSent && state.Phase == SessionPhase.Prep && !connection.IsSceneLoading)
                { diveSent = true; GameObject.Find("BeginDiveButton").GetComponent<Button>().onClick.Invoke(); }
                result.dive |= state.Phase == SessionPhase.Dive && SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene;
                if (Record && host && state.Phase == SessionPhase.Return)
                    result.recordingPending |= adapter.GetComponent<RecordingWorldBinding>().Binding.PendingDiveCount == 1;
                if (host && Event && state.Phase == SessionPhase.Return)
                {
                    var economy = adapter.GetComponent<EconomyManager>();
                    var local = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
                    if (local != null && economy.SharedBalance >= 100 && !purchaseSent && local.GetComponent<EconomyPlayerSync>().ShopOpen)
                    { purchaseSent = true; local.GetComponent<EconomyPlayerSync>().RequestPurchase("tube-1"); }
                    if (local != null && local.OxygenCapacity.Value >= 150 && economy.LoadoutFor(new PlayerId(0)).Contains("tube-1"))
                    {
                        result.tubePurchased = true;
                        if (!result.saveLoaded) result.saveLoaded = adapter.GetComponent<EconomySaveStore>().LoadNow();
                    }
                }
                if (state.Phase == SessionPhase.Return && returnPhaseAt == 0) returnPhaseAt = Time.realtimeSinceStartup;
                if (DayReload) { if (host && HostDayReload()) { Finish(); yield break; } yield return null; continue; }
                if (Day || Home) ObserveDay();
                if (host && Day) HostDay(state);
                if (host && Town) HostTown(state);
                if (host && Record && !Town) SeedRecorderCamera(state);
                if (host && Boat) HostSampleBoatRepair();
                if (host && Record && (state.Phase == SessionPhase.Return || result.returned))
                    result.recordingPaid |= adapter.GetComponent<EconomyManager>().SharedBalance > 0;
                if (host && elapsed > (Home ? 72 : Day ? 999 : Town ? 34 : Event ? 92 : Boat ? 58 : Hunt || Record ? 44 : 33) && !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (host && elapsed > (Home ? 82 : Day ? 72 : Town ? 64 : Event ? 104 : Trip ? 124 : Boat ? 66 : Record ? 56 : Hunt ? 48 : 36) && !lobbySent && state.Phase == SessionPhase.Return && !connection.IsSceneLoading)
                { lobbySent = true; GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (result.dive && state.Phase == SessionPhase.Lobby && state.Revision >= 4 && !connection.IsSceneLoading)
                {
                    result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    result.readyReset |= adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => !value) &&
                        string.IsNullOrEmpty(state.DiveId);
                }
                if (host && elapsed > (Home ? 86 : Day ? 76 : Town ? 68 : Event ? 108 : Trip ? 128 : Boat ? 70 : Record ? 60 : Hunt ? 54 : 41) && !leaveSent) { leaveSent = true; adapter.LeaveRoom(); }
                if (result.returned && connection.Status == ConnectionStatus.Offline)
                    result.stopped = adapter.Session.Roster.Count == 0 && connection.Players.Count == 0;
                yield return null;
            }
            result.reason = adapter.Connection.LastError;
            result.passed = result.connected && result.roster && result.ready && result.prep && result.dive && result.returned &&
                result.readyReset && result.walk && result.swim && result.collisions && result.cameras && result.stopped &&
                (!rejoin || result.clientRejoined) && result.errors.Count == 0;
            if (Hunt) result.passed &= result.fishMoved && result.catchObserved && result.catchDespawned &&
                (!host || result.inventoryAdded) && (!result.huntShooter || result.inventoryReplicated);
            // This scenario assigns ONE guest to record. Bystanders still must pass every
            // roster/movement/scene assertion, but must not be required to send recording input.
            if (Record) result.passed &= result.recordingRoleResolved &&
                (!(host || result.recordingRecorder) || (result.recordingStarted && result.recordingStopped)) &&
                (!host || (result.recordingClaimed && result.recordingViews && result.recordingPaid && result.recordingSafe));
            if (Event) result.passed &= result.eventOpened && result.eventClosed && (!host || (result.tubePurchased && result.saveLoaded));
            if (Town) result.passed &= result.townSold && result.townDenied && result.townShopOpened && result.townCameraBought &&
                result.townProgress && (!host || (result.townNoAutoPay && result.townPartBought && result.townHostChecks && result.townSaveRoundTrip));
            if (Boat) result.passed &= result.boatRepaired && result.boatPartsHidden &&
                (!host || (result.boatHullFound && result.boatFuelTankFound && result.boatDuplicateRejected &&
                    result.boatPartsSequence.Count >= 4 && result.boatPartsSequence[result.boatPartsSequence.Count - 1] == 3)) &&
                (host || result.boatEngineFound);
            if (Day) result.passed &= result.dayClockAdvanced &&
                result.dayNumbers.SequenceEqual(new[] { 1, 2, 3 }) && result.daySummaries.SequenceEqual(new[] { 1, 2 }) &&
                result.dayMirroredMidnightReason == (int)DayCloseReason.Midnight &&
                result.dayMirroredEarlyReason == (int)DayCloseReason.EarlySleep &&
                result.dayMirroredLostDivers == expected &&
                (!host || (result.dayMidnightClosed && result.dayLostDivers == expected && result.daySavedOnDisk &&
                    result.dayReloadNoAdvance && result.dayReplayIgnored && result.dayBedAccepted && result.dayEarlyGateHeld &&
                    result.dayEarlyClosed && result.dayHostDone && result.dayFinalNumber == 3));
            if (Home) result.passed &= result.homeBedAccepted && result.homeMorningClean &&
                result.dayNumbers.Contains(2) && result.daySummaries.Contains(1) && result.homePingSeen && result.homePingOnMap &&
                (!host || result.homePingAccepted);
            if (Trip) result.passed &= result.tripBoarded && result.tripDuplicateBoardHeld && result.tripMapDockedAtDock &&
                result.tripMapUnderwayMoved && result.tripMapAnchoredAtAnchor && result.tripDockedEmpty && result.tripDone &&
                result.tripMapPlayersMax >= expected &&
                result.tripPhases.SequenceEqual(new[] { "Docked", "Outbound", "Anchored", "Inbound", "Docked" }) &&
                (host || (result.tripReturnMarkerSeen && result.tripReboarded)) && (!host || result.tripSaveReload);
            Finish();
        }

        private void Probe(int expected)
        {
            var scene = SceneManager.GetActiveScene().name;
            var phase = adapter.Session.State.Phase;
            // Only Lobby lives in PrepArea; Prep, Dive and Return all run in DiveTestArea (SceneForPhase), so a
            // scene change alone no longer marks the start of a stage. Key the stage on scene and phase.
            var stage = scene + ":" + phase;
            if (observedScene != stage)
            { observedScene = stage; sceneStarted = Time.realtimeSinceStartup; starts.Clear(); }
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            foreach (var player in players)
            {
                player.ReadKeyboard = false;
                if (!starts.ContainsKey(player.OwnerClientId)) starts[player.OwnerClientId] = player.transform.position;
            }
            var local = players.FirstOrDefault(p => p.IsOwner);
            var elapsed = Time.realtimeSinceStartup - sceneStarted;
            if (local != null && Time.realtimeSinceStartupAsDouble >= nextInput)
            {
                nextInput = Time.realtimeSinceStartupAsDouble + 1.0 / 30;
                var move = scene == SessionNetworkAdapter.DiveScene ?
                    // A long upward probe reaches the y=6 surface-return zone and permanently
                    // marks the diver safe before the hunt/recording test even starts.
                    (elapsed > 1 && elapsed < 1.6f ? Vector3.up : Vector3.zero) :
                    (elapsed > 1 && elapsed < 5 ? Vector3.forward : Vector3.zero);
                if (Hunt && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeHunt(local, players);
                else if (Record && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeRecording(local, players);
                else if (Boat && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeBoatParts(local);
                else if (Trip && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Return) ProbeTrip(local, expected);
                else if (Home && scene == SessionNetworkAdapter.PrepScene && phase == SessionPhase.Lobby && homeStage < 90) ProbeHome(local);
                else if (Home && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeHomePing(local);
                else if ((Town || Record) && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Return) ProbeTown(local);
                else local.SubmitLocalInput(move, 0);
            }
            foreach (var player in players)
            {
                var delta = player.transform.position - starts[player.OwnerClientId];
                if (scene == SessionNetworkAdapter.PrepScene && delta.z > 0.5f) walked.Add(player.OwnerClientId);
                if (scene == SessionNetworkAdapter.DiveScene && player.Swimming.Value && delta.y > 0.7f) swam.Add(player.OwnerClientId);
            }
            result.walk |= walked.Count >= expected;
            result.swim |= swam.Count >= expected;
            if (scene == SessionNetworkAdapter.PrepScene && elapsed > 6 && players.Length == expected)
                result.collisions = players.All(p => Mathf.Abs(p.transform.position.x) < 4.9f && Mathf.Abs(p.transform.position.z) < 4.9f && p.transform.position.y > -0.5f);
            if (players.Length == expected && local != null && elapsed > 1)
                result.cameras = Camera.allCamerasCount == 1 && players.All(p =>
                    p.GetComponentInChildren<Camera>(true).enabled == p.IsOwner && p.GetComponentInChildren<AudioListener>(true).enabled == p.IsOwner);
        }

        private void ProbeRecording(NetworkPlayer local, NetworkPlayer[] players)
        {
            var subject = FindObjectsByType<RecordingSubject>(FindObjectsSortMode.None)
                .FirstOrDefault(x => Event ? x.GetComponent<SpecialEventRunner>() != null : x.GetComponent<FishActor>() != null);
            if (subject == null || !subject.IsSpawned) return;
            if (Event)
            {
                var active = subject.GetComponent<SpecialEventRunner>().Active.Value;
                result.eventOpened |= active;
                result.eventClosed |= result.eventOpened && !active;
            }
            var recorder = players.Length == 1 ? local.OwnerClientId : players.Where(p => p.OwnerClientId != 0).Min(p => p.OwnerClientId);
            result.recordingRoleResolved = true;
            result.recordingRecorder = local.OwnerClientId == recorder;
            var world = adapter.GetComponent<RecordingWorldBinding>();
            if (adapter.IsAuthority)
            {
                result.recordingViews |= RecorderViews.Count == players.Length;
                result.recordingStarted |= subject.OpenTakeCount > 0;
                result.recordingClaimed |= world.Binding.Director.ClaimCount == 1;
                result.recordingStopped |= result.recordingClaimed && subject.OpenTakeCount == 0;
                result.recordingViewActive |= RecorderViews.TryGetActive(new PlayerId(recorder), out _);
                result.recordingValidSeconds = Mathf.Max(result.recordingValidSeconds, subject.ValidSecondsFor(new PlayerId(recorder)));
                if (adapter.GetComponent<InventoryManager>().Bags.TryGetValue(new PlayerId(recorder), out var recordingBag))
                    result.recordingSafe |= recordingBag.SafelyReturned;
                if (world.Binding.Director.TryGetBestClaim(new PlayerId(recorder), subject.SubjectId, out var take))
                    result.recordingQuality = take.Quality;
            }
            if (local.OwnerClientId != recorder) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var delta = subject.transform.position - local.RecordingEyePosition;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            // After the take finishes, actually swim to the exit. Do not inject a safe-return flag: SafeReturnZone
            // must mark the remote diver through real movement. Since PR #70 that is a small pad on Shore_Ledge, not
            // "up" from anywhere in the arena, so this aims at the pad's real collider (still in the yaw frame being
            // submitted this call, which faces the subject for the shot, not the pad - SubmitLocalInput re-rotates
            // move by that yaw, so the heading is pre-rotated by its inverse the same way GoToService does it).
            Vector3 move;
            if (result.recordingStopped)
            {
                // A first version aimed the full heading straight at the pad's fixed centre, the same mistake the
                // beach wade had: the pad is mostly "up" from open water (dy far bigger than dx/dz), so once Surface
                // mode zeroes the positive-y request the LEFTOVER heading is whatever tiny x/z the original unit
                // vector had - which shrinks even further as the diver closes in, a real diver was measured
                // asymptoting toward a dead stop short of the ledge. NextRampWaypoint paces this the same way
                // NextBeachWaypoint paces the wade: a near target read off the real ramp/ledge colliders, never far
                // enough ahead for its own dy to swamp dx/dz.
                var toRamp = NextRampWaypoint(local.transform.position) - local.transform.position;
                move = toRamp.magnitude > 0.3f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * toRamp.normalized : Vector3.zero;
            }
            else move = delta.magnitude > 6f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
            var bridge = adapter.GetComponent<RecordingNetworkBridge>();
            if (bridge.IsRecordingLocal)
            {
                result.recordingStarted = true;
                if (recordingStartTime == 0) recordingStartTime = Time.realtimeSinceStartup;
            }
            else if (recordingStartTime > 0) result.recordingStopped = true;
            if (result.recordingStopped || Time.realtimeSinceStartup < nextAction) return;
            if (!bridge.IsRecordingLocal || Time.realtimeSinceStartup - recordingStartTime > 8f)
            {
                nextAction = Time.realtimeSinceStartup + .75f;
                bridge.ToggleRecordingLocal();
            }
        }

        // PR #70's dry return pad sits on Shore_Ledge, reachable only by climbing the P3.1 ramp - the same kind of
        // solid slope NextBeachWaypoint already knows how to pace a diver up, just descending along X here (the
        // P3.1 shore) instead of Z (the P3.2 beach). A short step ahead, read off the real Shore_Ramp/Shore_Ledge
        // colliders rather than DiveTestAreaWaterSetup's Editor-only formula, aimed a hair below the actual surface.
        private Vector3 NextRampWaypoint(Vector3 position)
        {
            var ramp = GameObject.Find("Shore_Ramp");
            var ledge = GameObject.Find("Shore_Ledge");
            var rampCollider = ramp != null ? ramp.GetComponent<Collider>() : null;
            var ledgeCollider = ledge != null ? ledge.GetComponent<Collider>() : null;
            if (rampCollider == null || ledgeCollider == null)
                return ledgeCollider != null ? ledgeCollider.bounds.center : position;

            // The ledge's own west edge, not the ramp's foot: a diver starts east of the ramp's foot (open water
            // reaches under the whole arena), so a check against the foot would be true immediately and skip
            // the paced climb entirely - it has to be "am I already over the ledge" the way the wade's check is
            // "am I already over the platform", using the near end of the solid ground, not the far one.
            var ledgeWestEdge = ledgeCollider.bounds.min.x;
            if (position.x >= ledgeWestEdge - 0.2f) return ledgeCollider.bounds.center;   // already over solid ground

            var laneZ = ramp.transform.position.z;
            var aheadX = Mathf.Min(position.x + 1.2f, ledgeWestEdge);
            var probeOrigin = new Vector3(aheadX, rampCollider.bounds.max.y + 5f, laneZ);
            float targetY;
            if (Physics.Raycast(probeOrigin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == rampCollider || hit.collider == ledgeCollider))
                targetY = hit.point.y - 0.05f;
            else
                targetY = Mathf.Min(position.y, rampCollider.bounds.min.y - 0.3f);   // still short of the ramp: dive under it
            return new Vector3(aheadX, targetY, laneZ);
        }

        private void ProbeHunt(NetworkPlayer local, NetworkPlayer[] players)
        {
            // Real owner inputs/RPCs, live fish AI and real inventory; no injected catch.
            var shooter = players.Length == 1 ? local.OwnerClientId : players.Where(p => p.OwnerClientId != 0).Min(p => p.OwnerClientId);
            result.huntShooter = local.OwnerClientId == shooter;
            if (result.huntShooter)
            {
                var bagSync = local.GetComponent<InventoryPlayerSync>();
                result.inventoryReplicated |= bagSync != null && bagSync.BagItemCount.Value == 1 && bagSync.BagWeightGrams.Value > 0;
            }
            if (adapter.IsAuthority && adapter.GetComponent<InventoryManager>().Bags.TryGetValue(new PlayerId(shooter), out var bag))
                result.inventoryAdded |= bag.Items.Count == 1;
            var fish = FindFirstObjectByType<FishActor>();
            if (fish == null || !fish.IsSpawned)
            {
                result.catchDespawned |= result.catchObserved;
                local.SubmitLocalInput(Vector3.zero, 0); return;
            }
            if (!firstFishPosition.HasValue) firstFishPosition = fish.transform.position;
            result.fishMoved |= Vector3.Distance(firstFishPosition.Value, fish.transform.position) > 0.15f;
            var available = fish.GetComponent<CatchObject>().IsAvailable;
            result.catchObserved |= available;
            if (local.OwnerClientId != shooter) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var delta = fish.GetComponent<Collider>().bounds.center - local.GetComponentInChildren<Camera>(true).transform.position;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            var move = available && delta.magnitude > 1.7f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
            if (nextAction == 0) nextAction = Time.realtimeSinceStartup + 0.2f;
            if (Time.realtimeSinceStartup < nextAction) return;
            nextAction = Time.realtimeSinceStartup + 0.75f;
            if (available) { if (delta.magnitude < 2.2f) local.SubmitPickupLocal(); }
            else local.SubmitHarpoonLocal();
        }

        // ---- P3.2-C acceptance: two real divers find all three free boat parts with the E-pickup RPC, one of
        // them re-requests an already-claimed part to prove it is rejected, and the host samples the real
        // BoatRepairState the whole way from Broken to Repaired. No purchase, no injected claim - the same
        // NetworkPlayer.HandlePickup -> IBoatPartPickupTarget -> BoatPartClaim.TryClaimFound path PR #70 wired,
        // walked and aimed by real owner input against the real BoatPart_* objects on the beach.
        //
        // Host takes Hull (then repeats the same claim once, expecting Rejected) and Fuel Tank; the guest takes
        // the Engine, which sits on the wade slope itself. Both parts on the platform need the same climb out
        // of the water the beach fix (previous commits) proved works, so this reuses GoToService's waypoint
        // logic rather than walking a straight line at the target.
        private void ProbeBoatParts(NetworkPlayer local)
        {
            var host = adapter.IsAuthority;
            // BoatPartsDone mirrors the same shared EconomyManager.BoatRepair count to every player's own sync
            // component (EconomyPlayerSync.cs: "host-written and only informational for the client UI"), so the
            // guest can confirm Repaired from its own replicated state without any host-only access.
            var sync = local.GetComponent<EconomyPlayerSync>();
            if (sync != null && sync.BoatPartsDone.Value >= BoatRepairParts.All.Count) result.boatRepaired = true;
            if (sync != null && sync.BoatPartsMask.Value == 7)
            {
                var anchors = FindObjectsByType<BoatPartAnchor>(FindObjectsSortMode.None);
                result.boatPartsHidden |= anchors.Length == 3 && anchors.All(a =>
                    a.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
                    a.GetComponentsInChildren<Collider>().All(c => !c.enabled));
            }
            var partName = host ? boatStep == 0 || boatStep == 1 ? "BoatPart_Hull" : "BoatPart_FuelTank" : "BoatPart_Engine";
            var part = GameObject.Find(partName);
            var cam = local.GetComponentInChildren<Camera>(true);
            if (part == null || cam == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }

            if (!GoToBoatPart(local, cam, part.transform.position)) return;
            if (Time.realtimeSinceStartup < boatNext) return;
            boatNext = Time.realtimeSinceStartup + 0.75f;

            var before = local.LastActionRequestId.Value;
            local.SubmitPickupLocal();
            StartCoroutine(ReadBoatPickupResult(local, before, host));
        }

        private IEnumerator ReadBoatPickupResult(NetworkPlayer local, ulong before, bool host)
        {
            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (local.LastActionRequestId.Value == before && Time.realtimeSinceStartup < deadline) yield return null;
            if (local.LastActionRequestId.Value == before) yield break;   // no answer arrived in time; retry next cycle
            var accepted = local.LastActionKind.Value == (byte)PlayerActionKind.Pickup &&
                local.LastActionResult.Value == (int)PlayerActionResult.Accepted;
            var rejected = local.LastActionKind.Value == (byte)PlayerActionKind.Pickup &&
                local.LastActionResult.Value == (int)PlayerActionResult.Rejected;

            switch (boatStep)
            {
                case 0:   // host: claim the hull for real
                    if (host && accepted) { result.boatHullFound = true; boatStep = 1; }
                    else if (host && !accepted) result.errors.Add($"boat hull claim rejected={local.LastActionResult.Value}");
                    else if (!host && accepted) { result.boatEngineFound = true; boatStep = 2; }   // guest: engine claimed
                    else if (!host && !accepted) result.errors.Add($"boat engine claim rejected={local.LastActionResult.Value}");
                    break;
                case 1:   // host only: the same hull again must be refused, not paid or progressed twice
                    // Collected parts now vanish, including their pickup collider. A second E ray therefore
                    // returns InvalidTarget. Still require the committed hull bit and its disabled collider.
                    var hull = GameObject.Find("BoatPart_Hull");
                    var sync = local.GetComponent<EconomyPlayerSync>();
                    var hiddenHull = local.LastActionResult.Value == (int)PlayerActionResult.InvalidTarget &&
                        sync != null && (sync.BoatPartsMask.Value & 1) != 0 && hull != null &&
                        hull.GetComponentsInChildren<Collider>().All(c => !c.enabled);
                    if (rejected || hiddenHull) { result.boatDuplicateRejected = true; boatStep = 2; }
                    else result.errors.Add($"duplicate hull claim was not rejected, result={local.LastActionResult.Value}");
                    break;
                case 2:   // host: fuel tank; guest: already done, idles here
                    if (host)
                    {
                        if (accepted) { result.boatFuelTankFound = true; boatStep = 3; }
                        else result.errors.Add($"boat fuel tank claim rejected={local.LastActionResult.Value}");
                    }
                    break;
            }
        }

        // Mirrors GoToService: climb the wade the same paced way while still in the water (NextBeachWaypoint
        // already reads the real Beach_Wade/Beach_Platform colliders, so it does not care whether the eventual
        // target is an NPC or a boat part), then walk and aim directly once on dry ground.
        // SwimVolume's own collider is a trigger too (Network/SwimVolume.cs, needed for its own Contains query),
        // and HandlePickup's raycast has to include triggers to ever reach a BoatPartAnchor's. The side effect:
        // aimed from above the surface (y 8, SwimVolume's real top) down through it at a still-submerged target,
        // that same raycast hits the water's own boundary first and never reaches the anchor - measured directly
        // (a driver-side probe raycast at the exact aim used here returned "SwimVolume@0.32" every time). A real
        // diver would have exactly the same problem; it is not specific to this driver. So this approaches a
        // submerged target from BELOW rather than level with it: the engine sits at y~7, only 1 m under the
        // surface, and 1.55 m of eye height alone is enough to surface the camera while still standing right at
        // the anchor's own height. Aiming up at it from underwater keeps the camera below the boundary the
        // raycast would otherwise clip.
        private bool GoToBoatPart(NetworkPlayer local, Camera cam, Vector3 targetPosition)
        {
            const float surfaceY = 8f;   // SwimVolume's real top (pos.y 4 + half-size 4), not the decorative mesh at 8.1
            var submerged = targetPosition.y < surfaceY - 0.1f;
            // A vertical-only offset does not work here: the ramp is solid, so a diver already standing on it at
            // the target's own (x, z) cannot sink any further there (measured: pressing down just pushes into the
            // ground, position does not move). What actually clears the camera is standing further OUT along the
            // slope - south, toward open water - where the real surface is deeper, then aiming back up-slope at
            // the target. 1.8 m out is past where the surface drops below surfaceY - eye height (6.45).
            var standTarget = submerged ? targetPosition + new Vector3(0f, 0f, 1.8f) : targetPosition;

            // Full 3D, not flat: the engine sits partway up the wade slope (unlike an NPC, always on the flat
            // platform), so a diver can be horizontally right over it while still metres below it, underwater,
            // still mid-climb - a flat check would call that "close enough" and the pickup raycast (range 2.5 m)
            // would miss high and low every time. Requiring the real 3D distance keeps the paced climb running
            // until height has caught up too, which it does naturally since the climb tracks the real slope.
            var toStand = standTarget - local.transform.position;
            if (toStand.magnitude > 1.3f)
            {
                boatSettleAt = 0;
                var target = NextBeachWaypoint(local.transform.position, standTarget);
                var heading = target - local.transform.position;
                var flatHeading = heading; flatHeading.y = 0;
                var walkYaw = Mathf.Atan2(flatHeading.x, flatHeading.z) * Mathf.Rad2Deg;
                var move = Quaternion.Inverse(Quaternion.Euler(0, walkYaw, 0)) * heading.normalized;
                local.SubmitLocalInput(move, walkYaw, 0);
                return false;
            }
            var toPart = targetPosition + Vector3.up * 0.3f - cam.transform.position;
            var yaw = Mathf.Atan2(toPart.x, toPart.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(toPart.y, new Vector2(toPart.x, toPart.z).magnitude) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, yaw, pitch);
            if (boatSettleAt == 0) boatSettleAt = Time.realtimeSinceStartup + 0.6f;
            return Time.realtimeSinceStartup >= boatSettleAt;
        }

        // Host-only, authoritative: samples the real EconomyManager.BoatRepair (not the replicated
        // BoatPartsDone NetworkVariable) so the recorded sequence is the source of truth itself, not a mirror
        // of it, and records the exact 0/1/2/3 progression rather than just a before/after snapshot.
        // ---- P4.1 physical home (Mehmet #88 / PR #92): real H interaction against the real beds, real P ping --------------
        // Each process walks its own player to its own bed in PrepArea, AIMS at it (the host resolves the target from
        // the player's own view ray, the client never names a bed) and presses H through the binding's own request
        // path. The day must close by everyone being in bed, the morning must leave nobody seated, and a host P ping in
        // the dive scene must reach every process on the map.
        private int homeStage;
        private float homeStageAt, homeAimAt, homeNext;
        private int homeAttempts;
        private float homeCleanSince;

        private static string HomeStatus(HomePlayerInteractionBinding binding) =>
            (string)typeof(HomePlayerInteractionBinding).GetField("statusMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(binding);

        private static void HomeSend(HomePlayerInteractionBinding binding, byte op) =>
            typeof(HomePlayerInteractionBinding).GetMethod("SendRequest",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(binding, new object[] { op });

        private static void AimAt(NetworkPlayer local, Camera cam, Vector3 target, out float yaw, out float pitch)
        {
            var to = target - cam.transform.position;
            yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
        }

        private void ProbeHome(NetworkPlayer local)
        {
            var binding = FindFirstObjectByType<HomePlayerInteractionBinding>();
            var cam = local.GetComponentInChildren<Camera>(true);
            var sync = local.GetComponent<EconomyPlayerSync>();
            var now = Time.realtimeSinceStartup;
            if (homeStageAt == 0) homeStageAt = now;
            if (binding == null || cam == null || sync == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            if (homeStage < 90 && now - homeStageAt > 40f)
            {
                result.errors.Add($"home stage {homeStage} timeout status='{HomeStatus(binding)}' day={sync.DayNumber.Value} sleepers={HomePlayerInteractionBinding.SnapshotSleepers().Count} pos={local.transform.position}");
                homeStage = 99;
            }

            var bedId = adapter.IsAuthority ? DayIds.Bed0 : DayIds.Bed1;
            var anchor = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None)
                .FirstOrDefault(a => a.Kind == HomeInteractionKind.Bed && a.BedId == bedId);
            if (anchor == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }

            switch (homeStage)
            {
                case 0:   // walk to the bed
                {
                    var to = anchor.WorldPosition - local.transform.position; to.y = 0;
                    if (to.magnitude > 1.9f)
                    {
                        var yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                        local.SubmitLocalInput(Vector3.forward, yaw, 0);
                        return;
                    }
                    homeStage = 1; homeAimAt = 0; return;
                }
                case 1:   // aim at it, let the aim reach the host, press H
                {
                    var target = anchor.GetComponentInChildren<Collider>() != null ? anchor.GetComponentInChildren<Collider>().bounds.center : anchor.WorldPosition;
                    AimAt(local, cam, target, out var yaw, out var pitch);
                    local.SubmitLocalInput(Vector3.zero, yaw, pitch);
                    if (homeAimAt == 0) homeAimAt = now + 0.7f;
                    if (now < homeAimAt) return;
                    HomeSend(binding, 1);
                    homeAttempts++;
                    homeNext = now + 1.0f;
                    homeStage = 2; return;
                }
                case 2:   // did the host accept? (status text is the client-visible result)
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < homeNext) return;
                    var status = HomeStatus(binding);
                    result.homeStatus = status;
                    if (status == "EV ETKILESIMI TAMAM") { result.homeBedAccepted = true; homeStage = 3; return; }
                    if (homeAttempts >= 3) { result.errors.Add("home bed refused: " + status); homeStage = 99; return; }
                    homeStage = 1; homeAimAt = 0; return;
                }
                case 3:   // everybody in bed -> the day closes, morning leaves nobody seated
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    result.homeSleepersMax = Math.Max(result.homeSleepersMax, HomePlayerInteractionBinding.SnapshotSleepers().Count);
                    if (sync.DayNumber.Value < 2 || (DayPhase)sync.DayPhaseValue.Value != DayPhase.Running) return;
                    // The sleep mirror is a named message; give it a moment to arrive after the day advanced.
                    if (homeCleanSince == 0) homeCleanSince = now;
                    var clean = !local.Seated.Value && HomePlayerInteractionBinding.SnapshotSleepers().Count == 0 &&
                        SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    if (clean) { result.homeMorningClean = true; homeStage = 90; return; }
                    if (now - homeCleanSince > 8f)
                    {
                        result.errors.Add($"morning not clean seated={local.Seated.Value} sleepers={HomePlayerInteractionBinding.SnapshotSleepers().Count} scene={SceneManager.GetActiveScene().name}");
                        homeStage = 90;
                    }
                    return;
                }
                default:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
            }
        }

        // In the dive scene (it has Utku's region): the host presses P, every process must see the ping on the map.
        private void ProbeHomePing(NetworkPlayer local)
        {
            var binding = FindFirstObjectByType<HomePlayerInteractionBinding>();
            var cam = local.GetComponentInChildren<Camera>(true);
            if (binding == null || cam == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var now = Time.realtimeSinceStartup;
            if (homePingAt == 0) homePingAt = now + 5f;

            var pings = HomePlayerInteractionBinding.SnapshotPings();
            if (pings.Count > 0)
            {
                var ping = pings[0];
                result.homePingSeen = true;
                result.homePingOnMap |= P4MapPositionFeed.TryWorldToMap(ping.WorldPosition, out _);
            }

            if (adapter.IsAuthority && !homePingSent && now >= homePingAt)
            {
                AimAt(local, cam, local.transform.position + Vector3.down * 3f + local.transform.forward * 2f, out var yaw, out var pitch);
                local.SubmitLocalInput(Vector3.zero, yaw, pitch);
                if (homePingAimAt == 0) homePingAimAt = now + 0.7f;
                if (now < homePingAimAt) return;
                homePingSent = true;
                HomeSend(binding, 2);
                homePingCheckAt = now + 1f;
                return;
            }
            if (homePingSent && now >= homePingCheckAt && homePingCheckAt > 0)
            {
                result.homePingAccepted = HomeStatus(binding) == "PING GONDERILDI";
                if (!result.homePingAccepted) result.errors.Add("home ping refused: " + HomeStatus(binding));
                homePingCheckAt = 0;
            }
            local.SubmitLocalInput(Vector3.zero, 0);
        }

        private float homePingAt, homePingAimAt, homePingCheckAt;
        private bool homePingSent;

        // ---- P4.1 shared day: real host clock, real close through the real session/inventory/save --------------
        // Day 1 runs to a real 00:00 (the host only raises the clock RATE, never sets the time): the open dive is
        // closed through the normal return path (D07), one summary is produced, the next day is written to the
        // campaign file, and every process's mirrored state follows. Day 2 then closes by sleep: the host's own bed
        // must NOT close it while the connected guest is awake; the guest's bed does. Beds are entered through
        // HomeBedInteraction, the seam Mehmet's physical bed layer (#88) will call - the physical part is his.
        private int dayStage;
        private float dayStageAt;
        private ulong dayRequest = 1000;
        private readonly List<int> dayNumbersSeen = new List<int>();
        private readonly List<int> daySummariesSeen = new List<int>();

        private void ObserveDay()
        {
            var local = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
            var sync = local != null ? local.GetComponent<EconomyPlayerSync>() : null;
            if (sync == null || !sync.IsSpawned) return;

            var number = sync.DayNumber.Value;
            if (dayNumbersSeen.Count == 0 || dayNumbersSeen[dayNumbersSeen.Count - 1] != number) dayNumbersSeen.Add(number);
            result.dayNumbers = dayNumbersSeen;
            var summary = sync.SummaryDayNumber.Value;
            if (summary > 0 && (daySummariesSeen.Count == 0 || daySummariesSeen[daySummariesSeen.Count - 1] != summary))
            {
                daySummariesSeen.Add(summary);
                if (summary == 1) result.dayMirroredLostDivers = sync.SummaryLostDivers.Value;
                if (summary == 2) result.dayMirroredEarlyReason = sync.SummaryReason.Value;
                if (summary == 1) result.dayMirroredMidnightReason = sync.SummaryReason.Value;
            }
            result.daySummaries = daySummariesSeen;
            if (number == 1 && sync.DayClockMinute.Value > DayIds.DayStartMinute + 30) result.dayClockAdvanced = true;
        }

        private void HostDay(SessionState state)
        {
            var binding = adapter.GetComponent<DayNetworkBinding>();
            var engine = binding != null ? binding.Engine : null;
            if (engine == null) return;
            var now = Time.realtimeSinceStartup;
            if (dayStageAt == 0) dayStageAt = now;

            switch (dayStage)
            {
                case 0:   // Dive is running with everybody in it: let the day's own clock reach 00:00, quickly.
                    if (state.Phase != SessionPhase.Dive || adapter.Connection.IsSceneLoading ||
                        SceneManager.GetActiveScene().name != SessionNetworkAdapter.DiveScene ||
                        Time.realtimeSinceStartup - sceneStarted < 5f || engine.State.ActivePlayers.Count < result.maxPlayers) return;
                    result.dayStartMinute = engine.ClockMinute;
                    engine.GameMinutesPerRealSecond = 60f;
                    dayStage = 1; dayStageAt = now;
                    return;
                case 1:   // 00:00 closes the day exactly once
                    if (engine.DayNumber < 2) { if (now - dayStageAt > 40f) { result.errors.Add("day never closed at 00:00"); dayStage = 99; } return; }
                    engine.GameMinutesPerRealSecond = 0.8f;   // no accidental second midnight during the checks
                    var summary = engine.History.Count > 0 ? engine.History[0] : null;
                    result.dayMidnightClosed = summary != null && summary.Reason == DayCloseReason.Midnight &&
                        summary.CloseId == DayIds.CloseId(1) && engine.History.Count == 1;
                    result.dayLostDivers = summary != null ? summary.LostDivers : -1;
                    var store = adapter.GetComponent<EconomySaveStore>();
                    var onDisk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                    result.daySavedOnDisk = onDisk.HasDay && onDisk.Day.DayNumber == 2 &&
                        onDisk.Day.ClosedCloseIds.Contains(DayIds.CloseId(1)) && onDisk.Day.SummaryHistory.Count == 1;
                    var before = engine.DayNumber;
                    result.dayReloadNoAdvance = store.LoadNow() && engine.DayNumber == before && engine.History.Count == 1;
                    // A replay of day 1's close (same id) arriving now, on day 2, must do nothing.
                    result.dayReplayIgnored = engine.BeginClose(DayCloseReason.Midnight, DayIds.CloseId(1)) == DayIds.CloseId(1) &&
                        engine.History.Count == 1 && engine.DayNumber == before && engine.Phase != DayPhase.Closing;
                    dayStage = 2; dayStageAt = now;
                    return;
                case 2:   // day 2: everyone is in the Return phase now. The host alone in bed must not close it.
                    if (engine.Phase != DayPhase.Running || engine.State.ActivePlayers.Count < result.maxPlayers) return;
                    var host = adapter.Connection.LocalPlayerId ?? new PlayerId(0);
                    result.dayBedAccepted = HomeBedInteraction.TryEnterBed(host, DayIds.Bed0, dayRequest++).Accepted;
                    dayStage = 3; dayStageAt = now;
                    return;
                case 3:
                    if (now - dayStageAt < 1.5f) return;
                    result.dayEarlyGateHeld = engine.DayNumber == 2 && engine.State.SleepingPlayers.Count == 1;
                    var bed = 1;
                    foreach (var id in engine.State.ActivePlayers)
                    {
                        if (engine.State.SleepingPlayers.Contains(id)) continue;
                        HomeBedInteraction.TryEnterBed(id, DayIds.Beds[bed++], dayRequest++);
                    }
                    dayStage = 4; dayStageAt = now;
                    return;
                case 4:
                    if (engine.DayNumber < 3) { if (now - dayStageAt > 10f) { result.errors.Add("day did not close when everyone slept"); dayStage = 99; } return; }
                    var second = engine.History.Count > 1 ? engine.History[1] : null;
                    result.dayEarlyClosed = second != null && second.Reason == DayCloseReason.EarlySleep &&
                        second.CloseId == DayIds.CloseId(2) && engine.History.Count == 2;
                    result.dayFinalNumber = engine.DayNumber;
                    result.dayHostDone = true;
                    dayStage = 5;
                    return;
            }
        }

        // ---- P3.3 trip: real owner inputs + RPCs against the real dock, seats, route and map -----------
        // Walks the beach to the dock, boards through BoatTripPlayerSync's owner RPCs (host resolves the seat), the
        // trip owner starts the route, the non-owner steps off at the anchorage (return marker must show) and back on,
        // the owner recalls, everybody steps off at the dock. Every process samples ITS OWN map (BoatMapView.LastIcons).
        private int tripStep;
        private float tripStepAt, tripNext, tripDisembarkedAt;
        private bool tripDupSent, tripDropSeen, tripOnBeach;
        private float tripHostLogAt, tripMarkerSeatedSince;
        private bool tripMarkerErrored;
        private string tripLastPhase, tripSeatBefore;
        private readonly HashSet<string> tripUnderwaySeen = new HashSet<string>();
        private readonly HashSet<string> tripPositionsSeen = new HashSet<string>();

        private void TripStep(int step)
        {
            result.tripTrace.Add($"step {tripStep}->{step} t={Time.realtimeSinceStartup - sceneStarted:F1}");
            tripStep = step; tripStepAt = Time.realtimeSinceStartup; tripNext = 0;
        }

        private static float MapDist(MapIcon a, (float X, float Z) b) =>
            Mathf.Sqrt((a.MapX - b.X) * (a.MapX - b.X) + (a.MapZ - b.Z) * (a.MapZ - b.Z));

        private void ProbeTrip(NetworkPlayer local, int expected)
        {
            var sync = local.GetComponent<BoatTripPlayerSync>();
            if (sync == null || !sync.IsSpawned) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var phase = (BoatTripPhase)sync.Phase.Value;
            var phaseName = phase.ToString();
            if (sync.BoatVisible.Value && tripLastPhase != phaseName)
            { tripLastPhase = phaseName; result.tripPhases.Add(phaseName); }
            SampleTripMap(sync, phase);

            var owner = sync.AmOwner.Value;
            var now = Time.realtimeSinceStartup;
            if (tripStepAt == 0) tripStepAt = now;
            if (tripStep < 90 && now - tripStepAt > 32f)
            {
                result.errors.Add($"trip step {tripStep} timeout phase={phaseName} seated={sync.SeatedCount.Value} seat={sync.MySeatId.Value} last={sync.LastReasonCode.Value}");
                TripStep(99);
            }

            if (adapter.IsAuthority && tripStep >= 3 && tripStep < 90 && now >= tripHostLogAt && result.tripTrace.Count < 90)
            {
                tripHostLogAt = now + 1.5f;
                var seen = string.Join(" ", FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(q => q.IsSpawned)
                    .Select(q => $"P{q.OwnerClientId}={q.transform.position} seated={q.Seated.Value}"));
                result.tripTrace.Add($"hostview phase={phaseName} step={tripStep} {seen}");
            }

            switch (tripStep)
            {
                case 0:   // walk from the water onto the beach and along it to the dock
                {
                    var dock = FindObjectsByType<RouteAnchor>(FindObjectsSortMode.None).FirstOrDefault(a => a.AnchorId == DiveRouteAnchors.Dock);
                    if (dock == null || !sync.BoatVisible.Value) { local.SubmitLocalInput(Vector3.zero, 0); return; }
                    var stand = new Vector3(dock.BoardingPosition.x + (adapter.IsAuthority ? -0.4f : 0.4f), dock.BoardingPosition.y, -3.6f);   // deck end, as close to the stern as the deck goes
                    var toStand = stand - local.transform.position; toStand.y = 0;
                    if (now - tripNext > 1.5f) { tripNext = now; result.tripTrace.Add($"walk pos={local.transform.position} onBeach={tripOnBeach} platMaxZ={(GameObject.Find("Beach_Platform") != null ? GameObject.Find("Beach_Platform").GetComponent<Collider>().bounds.max.z : 0f)}"); }
                    if (toStand.magnitude > 0.1f)
                    {
                        // NextBeachWaypoint answers "how do I climb out of the water" and, once on the platform, "walk to
                        // the stand" - but it decides that from z alone, so it would pull a diver standing on the ledge
                        // (north of the platform) back towards the wade lane. Latch once on dry sand and walk from there.
                        var strip = GameObject.Find("Beach_Platform");
                        var stripCollider = strip != null ? strip.GetComponent<Collider>() : null;
                        if (!tripOnBeach && stripCollider != null && local.transform.position.z <= stripCollider.bounds.max.z + 0.2f &&
                            local.transform.position.y >= stripCollider.bounds.max.y - 0.3f)   // on the dry top, not still wading
                            tripOnBeach = true;
                        Vector3 target;
                        if (!tripOnBeach) target = NextBeachWaypoint(local.transform.position, stand);
                        else if (local.transform.position.z < -10f && Mathf.Abs(local.transform.position.x - stand.x) > 1f)
                            target = new Vector3(stand.x, local.transform.position.y, -12.6f);   // along the sand to the jetty lane
                        else target = stand;
                        var heading = target - local.transform.position;
                        var flat = heading; flat.y = 0;
                        var yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                        local.SubmitLocalInput(Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized, yaw, 0);
                        return;
                    }
                    local.SubmitLocalInput(Vector3.zero, 0);
                    TripStep(1);
                    return;
                }
                case 1:   // board: the HOST resolves the seat, the client never picks one
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.IsSeated)
                    {
                        result.tripBoarded = true; result.tripSeatId = sync.MySeatId.Value.ToString();
                        TripStep(2); return;
                    }
                    if (now >= tripNext)
                    {
                        tripNext = now + 1.2f; sync.RequestBoardNearestLocal();
                        if (result.tripTrace.Count < 40) result.tripTrace.Add($"board pos={local.transform.position} boat={sync.BoatWorldPosition.Value} yaw={sync.BoatYaw.Value:F0} last={sync.LastReasonCode.Value} seated={sync.SeatedCount.Value}");
                    }
                    return;
                case 2:   // everyone aboard; a repeat board request must neither move the seat nor add a passenger
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.SeatedCount.Value < expected) return;
                    if (!tripDupSent)
                    {
                        tripDupSent = true; tripNext = now + 1.0f;
                        tripSeatBefore = sync.MySeatId.Value.ToString();
                        sync.RequestBoardNearestLocal();
                        return;
                    }
                    if (now < tripNext) return;
                    result.tripDuplicateBoardHeld = sync.IsSeated && sync.MySeatId.Value.ToString() == tripSeatBefore &&
                        sync.SeatedCount.Value == expected;
                    if (!result.tripDuplicateBoardHeld) result.errors.Add($"duplicate board changed state seat={sync.MySeatId.Value} count={sync.SeatedCount.Value}");
                    TripStep(3); return;
                case 3:   // owner starts the route; everyone rides Outbound to Anchored
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Anchored) { TripStep(4); return; }
                    if (owner && phase == BoatTripPhase.Docked && now >= tripNext) { tripNext = now + 1.5f; sync.RequestStartRouteLocal(BoatTripIds.NearRouteId); }
                    return;
                case 4:   // anchorage: the non-owner steps off (return marker), the owner waits for the dip
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase != BoatTripPhase.Anchored) return;
                    if (!owner)
                    {
                        if (sync.IsSeated) { if (now >= tripNext) { tripNext = now + 1.2f; sync.RequestDisembarkLocal(); } return; }
                        if (tripDisembarkedAt == 0) tripDisembarkedAt = now;
                        if (now - tripDisembarkedAt > 2.0f) TripStep(5);   // long enough for the return marker to be sampled
                        return;
                    }
                    if (sync.SeatedCount.Value < expected) tripDropSeen = true;
                    if (tripDropSeen) TripStep(6);
                    return;
                case 5:   // non-owner boards again beside the hull
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.IsSeated) { result.tripReboarded = true; TripStep(7); return; }
                    if (now >= tripNext)
                    {
                        tripNext = now + 1.2f; sync.RequestBoardNearestLocal();
                        if (result.tripTrace.Count < 60) result.tripTrace.Add($"reboard pos={local.transform.position} boat={sync.BoatWorldPosition.Value} last={sync.LastReasonCode.Value}");
                    }
                    return;
                case 6:   // owner waits until the party is whole again
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.SeatedCount.Value < expected) return;
                    TripStep(7); return;
                case 7:   // owner recalls; everyone rides Inbound to Docked
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Inbound) tripUnderwaySeen.Add("Inbound");
                    if (phase == BoatTripPhase.Docked && tripUnderwaySeen.Contains("Inbound")) { TripStep(8); return; }
                    if (owner && phase == BoatTripPhase.Anchored && sync.SeatedCount.Value >= expected && now >= tripNext)
                    { tripNext = now + 1.5f; sync.RequestReturnLocal(); }
                    return;
                case 8:   // docked: everybody steps off; the boat is left empty
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!sync.IsSeated) { TripStep(9); return; }
                    if (now >= tripNext) { tripNext = now + 1.2f; sync.RequestDisembarkLocal(); }
                    return;
                case 9:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Docked && sync.SeatedCount.Value == 0) result.tripDockedEmpty = true;
                    if (result.tripDockedEmpty) { result.tripDone = true; TripStep(90); }
                    return;
                default:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (adapter.IsAuthority && result.tripDone && !result.tripSaveReload) HostTripSaveCheck();
                    return;
            }
        }

        private void SampleTripMap(BoatTripPlayerSync sync, BoatTripPhase phase)
        {
            var icons = BoatMapView.LastIcons;
            if (icons == null || icons.Count == 0 || !BoatMapView.LastHadRegion) return;
            MapIcon dock = default, boat = default;
            bool hasDock = false, hasBoat = false, marker = false;
            var players = 0;
            for (var i = 0; i < icons.Count; i++)
            {
                var icon = icons[i];
                if (icon.IconId == BoatMapPresenter.DockIconId) { dock = icon; hasDock = true; }
                else if (icon.IconId == BoatTripIds.BoatId) { boat = icon; hasBoat = true; }
                else if (icon.IconId == BoatMapPresenter.ReturnMarkerIconId) marker = true;
                else if (icon.IconId.StartsWith(BoatMapPresenter.PlayerIconPrefix, StringComparison.Ordinal)) players++;
            }
            result.tripMapPlayersMax = Math.Max(result.tripMapPlayersMax, players);
            // The map refreshes every 0.1 s, so the icon list can lag the seat by a frame or two; only a marker that
            // OUTLASTS that window while seated is a real defect.
            if (marker && sync.IsSeated)
            {
                if (tripMarkerSeatedSince == 0) tripMarkerSeatedSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - tripMarkerSeatedSince > 0.6f && !tripMarkerErrored)
                { tripMarkerErrored = true; result.errors.Add("return marker drawn while aboard"); }
            }
            else tripMarkerSeatedSince = 0;
            if (!hasDock) return;

            var region = FindFirstObjectByType<DiveRegionField>();
            var anchors = FindObjectsByType<RouteAnchor>(FindObjectsSortMode.None);
            var dockAnchor = anchors.FirstOrDefault(a => a.AnchorId == DiveRouteAnchors.Dock);
            var seaAnchor = anchors.FirstOrDefault(a => a.AnchorId == DiveRouteAnchors.AnchorPoint);
            if (region == null || dockAnchor == null || seaAnchor == null) return;
            region.TryWorldToMap(dockAnchor.WorldPosition, out var expectedDock);
            region.TryWorldToMap(seaAnchor.WorldPosition, out var expectedAnchor);

            if (phase == BoatTripPhase.Docked && hasBoat && MapDist(dock, (expectedDock.x, expectedDock.y)) < 0.01f &&
                MapDist(boat, (expectedDock.x, expectedDock.y)) < 0.01f) result.tripMapDockedAtDock = true;
            if ((phase == BoatTripPhase.Outbound || phase == BoatTripPhase.Inbound) && hasBoat)
            {
                tripPositionsSeen.Add($"{Mathf.Round(boat.MapX * 50)}:{Mathf.Round(boat.MapZ * 50)}");
                result.tripUnderwayPositions = tripPositionsSeen.Count;
                if (result.tripUnderwayPositions >= 3) result.tripMapUnderwayMoved = true;
            }
            if (phase == BoatTripPhase.Anchored && hasBoat && MapDist(boat, (expectedAnchor.x, expectedAnchor.y)) < 0.01f)
                result.tripMapAnchoredAtAnchor = true;
            if (phase == BoatTripPhase.Anchored && !sync.IsSeated && marker) result.tripReturnMarkerSeen = true;
        }

        // Host only, after the party is back at the dock: the REAL EconomySaveStore round trip must leave the
        // boat repaired and the trip manager Docked with no seats. A live trip is never in the save file.
        private void HostTripSaveCheck()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var store = adapter.GetComponent<EconomySaveStore>();
            var manager = adapter.GetComponent<BoatTripManager>();
            var ok = store.SaveNow() && store.LoadNow() && economy.BoatRepair.Status == BoatRepairStatus.Repaired &&
                manager != null && manager.State.Phase == BoatTripPhase.Docked && manager.State.Seats.Count == 0;
            result.tripSaveReload = ok;
            if (!ok) result.errors.Add("trip save/load check failed: " + store.LastError);
        }

        // Second launch of the host on the SAME campaign file: the closed days must come back as they were.
        private bool HostDayReload()
        {
            var binding = adapter.GetComponent<DayNetworkBinding>();
            var engine = binding != null ? binding.Engine : null;
            if (engine == null || adapter.Connection.Status != ConnectionStatus.Connected) return false;
            result.dayReloadNumber = engine.DayNumber;
            result.dayReloadHistory = engine.History.Count;
            result.dayReloadMinute = engine.ClockMinute;
            result.dayReloadPass = engine.DayNumber == 3 && engine.History.Count == 2 && engine.ClockMinute == DayIds.DayStartMinute &&
                engine.Phase == DayPhase.Running && engine.History[0].CloseId == DayIds.CloseId(1) &&
                engine.History[1].CloseId == DayIds.CloseId(2);
            result.passed = result.dayReloadPass && result.errors.Count == 0;
            return true;
        }

        private void HostSampleBoatRepair()
        {
            var state = adapter.GetComponent<EconomyManager>().BoatRepair;
            var count = state.CompletedPartIds.Count;
            if (count != boatSequenceLast)
            {
                boatSequenceLast = count;
                result.boatPartsSequence.Add(count);
            }
            if (state.Status == BoatRepairStatus.Repaired)
            {
                result.boatRepaired = true;
                result.boatStatusFinal = state.Status.ToString();
            }
        }

        // ---- P3.2 town: real owner inputs + RPCs against the PrepArea NPCs --------------------
        // Walks to the NPC, aims at it and only then lets the caller interact, so the host raycast,
        // range check and service handler all run for real (no injected result).
        //
        // Earlier versions of this smoke could not get a diver out of the water at all (Surface mode drops
        // upward input, so a swimmer's feet topped out below the old wade shelf's foot) and stood in with a
        // host-side NetworkPlayer.Teleport once Return started (result.townLandingTeleported, now removed).
        // Utku's shelf foot is now low enough (WadeFootY = 5.2, DiveTestAreaBeachSetup) that a diver reaches
        // it while still fully submerged, so the exit is a real, ordinary CharacterController slope climb -
        // see NextBeachWaypoint below for how this drives that climb without racing ahead of it.


        private bool GoToService(NetworkPlayer local, Camera cam, string serviceId)
        {
            var anchor = FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None)
                .FirstOrDefault(a => a.Definition.ServiceId == serviceId);
            if (anchor == null) { local.SubmitLocalInput(Vector3.zero, 0); return false; }
            // Each player gets its own spot in front of the NPC: two bodies cannot share one standing point.
            // Keyed on host vs guest, not on the client id: a rejoining guest gets a new id.
            var lateral = anchor.transform.right * (adapter.IsAuthority ? 0f : 0.9f);
            var stand = anchor.WorldPosition + anchor.transform.forward * 1.6f + lateral;
            var toStand = stand - local.transform.position; toStand.y = 0;
            if (toStand.magnitude > 0.35f)
            {
                townSettleAt = 0;
                var target = NextBeachWaypoint(local.transform.position, stand);
                var heading = target - local.transform.position;
                var flatHeading = heading; flatHeading.y = 0;
                var walkYaw = Mathf.Atan2(flatHeading.x, flatHeading.z) * Mathf.Rad2Deg;
                // The full 3D heading, not just the flat one: NextBeachWaypoint's target already carries the climb
                // (see its comment), so a diver approaching a steep bit of the ramp gets a steeply-angled request
                // instead of racing ahead horizontally and hitting the ramp's underside before it has risen enough.
                var move = Quaternion.Inverse(Quaternion.Euler(0, walkYaw, 0)) * heading.normalized;
                local.SubmitLocalInput(move, walkYaw, 0);
                return false;
            }
            var toNpc = anchor.WorldPosition + Vector3.up - cam.transform.position;
            var yaw = Mathf.Atan2(toNpc.x, toNpc.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(toNpc.y, new Vector2(toNpc.x, toNpc.z).magnitude) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, yaw, pitch);
            if (townSettleAt == 0) townSettleAt = Time.realtimeSinceStartup + 0.6f;   // let the aim reach the host
            return Time.realtimeSinceStartup >= townSettleAt;
        }

        // Prep, Dive and Return all run in DiveTestArea and the NPCs stand on the beach strip, a solid block that
        // rises out of the pool. A diver coming back therefore cannot walk straight at an NPC: it heads for the
        // lane of Utku's wade shelf, climbs onto the strip there, and only then walks along it. Waypoints are read
        // from the scene objects, not typed, so they follow the beach if World moves it.
        // A first version of this aimed at a single fixed point past the shelf and let the diver swim there
        // directly. That raced two real divers into the ramp's UNDERSIDE: heading z-first outpaces the y needed
        // to be riding on top of the slope rather than swimming into the bottom of it, since the slope rises
        // 3.2 m over its run and a diver approaching at speed can close the horizontal gap well before climbing
        // that much. So this instead reads the ramp's actual surface with a raycast (not the setup script's
        // formula - Editor-only code cannot ship into this Runtime assembly's standalone build) a short step
        // ahead, and aims a hair below that surface. The result is a target that only ever asks the diver to be
        // a little higher than they already need to be for their next step, so GoToService's 3D heading always
        // carries close to the right amount of climb, whatever the ramp's angle happens to be tuned to.
        private Vector3 NextBeachWaypoint(Vector3 position, Vector3 stand)
        {
            var wade = GameObject.Find("Beach_Wade");
            var platform = GameObject.Find("Beach_Platform");
            var wadeCollider = wade != null ? wade.GetComponent<Collider>() : null;
            var strip = platform != null ? platform.GetComponent<Collider>() : null;
            if (wadeCollider == null || strip == null) return stand;
            var northEdge = strip.bounds.max.z;
            if (position.z <= northEdge + 0.2f) return stand;   // already on the platform: walk to the NPC

            var lane = wade.transform.position.x;
            var aheadZ = Mathf.Max(position.z - 1.2f, northEdge);
            var probeOrigin = new Vector3(lane, wadeCollider.bounds.max.y + 5f, aheadZ);
            float targetY;
            if (Physics.Raycast(probeOrigin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == wadeCollider || hit.collider == strip))
                targetY = hit.point.y - 0.05f;                          // just under the real surface there
            else
                targetY = Mathf.Min(position.y, wadeCollider.bounds.min.y - 0.3f);   // still short of the shelf: dive under it
            return new Vector3(lane, targetY, aheadZ);
        }

        private void InteractEvery(NetworkPlayer local, float seconds)
        {
            if (Time.realtimeSinceStartup < townNext) return;
            townNext = Time.realtimeSinceStartup + seconds;
            local.SubmitServiceInteractionLocal();
        }

        private void RequestPurchaseAndWait(EconomyPlayerSync sync, string itemId)
        {
            townBefore = (int)sync.LastRequestId.Value;
            townAwaiting = true;
            sync.RequestPurchase(itemId);
        }

        private bool PurchaseAnswered(EconomyPlayerSync sync) =>
            townAwaiting && (int)sync.LastRequestId.Value != townBefore;

        private void ProbeTown(NetworkPlayer local)
        {
            var sync = local.GetComponent<EconomyPlayerSync>();
            var cam = local.GetComponentInChildren<Camera>(true);
            if (sync == null || cam == null) return;
            var host = adapter.IsAuthority;

            if (!Town)
            {
                // -p3-record / -p3-event: the recorder hands the recording to the recording NPC (recordings no
                // longer pay on their own); the event host then opens the equipment shop for the tube.
                var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
                var recorder = players.Length == 1 ? local.OwnerClientId :
                    players.Where(p => p.OwnerClientId != 0).Min(p => p.OwnerClientId);
                if (Time.realtimeSinceStartup >= townTraceAt && result.townTrace.Count < 40)
                {
                    townTraceAt = Time.realtimeSinceStartup + 2f;
                    var q = local.transform.position;
                    result.townTrace.Add($"recorder={recorder} me={local.OwnerClientId} pos=({q.x:0.0},{q.y:0.0},{q.z:0.0}) sw={local.Swimming.Value} pendingRec={sync.PendingRecordings.Value} svc={sync.LastServiceRequestId.Value}/{sync.LastServiceAccepted.Value}/{sync.LastServiceReason.Value} act={local.LastActionKind.Value}/{local.LastActionResult.Value}/{local.LastActionRequestId.Value} bal={sync.SharedBalance.Value}");
                }
                if (local.OwnerClientId == recorder)
                {
                    if (pendingSeenAt == 0 && sync.PendingRecordings.Value > 0 && returnPhaseAt > 0)
                    { pendingSeenAt = Time.realtimeSinceStartup; result.returnToPendingSeconds = pendingSeenAt - returnPhaseAt; }
                    if (turnInAt == 0 && (ServicePointType)sync.LastServiceType.Value == ServicePointType.RecordingBuyer &&
                        sync.LastServiceAccepted.Value && returnPhaseAt > 0)
                    { turnInAt = Time.realtimeSinceStartup; result.returnToTurnInSeconds = turnInAt - returnPhaseAt; }
                }
                if (local.OwnerClientId == recorder && sync.PendingRecordings.Value > 0)
                { if (GoToService(local, cam, TownServiceCatalog.RecordingBuyerId)) InteractEvery(local, 1.2f); return; }
                if (Event && host && !sync.ShopOpen)
                { if (GoToService(local, cam, TownServiceCatalog.EquipmentShopId)) InteractEvery(local, 1.2f); return; }
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }

            result.townProgress |= sync.BoatPartsDone.Value == 1;
            if (Time.realtimeSinceStartup >= townTraceAt && result.townTrace.Count < 40)
            {
                townTraceAt = Time.realtimeSinceStartup + 2f;
                var p = local.transform.position;
                result.townTrace.Add($"step={townStep} pos=({p.x:0.0},{p.y:0.0},{p.z:0.0}) sw={local.Swimming.Value} act={local.LastActionKind.Value}/{local.LastActionResult.Value}/{local.LastActionRequestId.Value} shop={sync.ActiveServiceId.Value} bal={sync.SharedBalance.Value}");
            }
            switch (townStep)
            {
                case 0: // sell my safely returned catches to the fish buyer
                    if (GoToService(local, cam, TownServiceCatalog.FishBuyerId)) InteractEvery(local, 1.2f);
                    if (sync.LastServiceRequestId.Value != 0 && (ServicePointType)sync.LastServiceType.Value == ServicePointType.FishBuyer &&
                        sync.LastServiceAccepted.Value)
                    {
                        result.townSold = sync.LastServiceAmount.Value == 240 && sync.LastServiceItems.Value == 2;
                        if (!result.townSold) result.errors.Add($"town sale amount={sync.LastServiceAmount.Value} items={sync.LastServiceItems.Value}");
                        townStep = 1;
                    }
                    break;
                case 1: // a purchase attempt away from the shop must be refused
                    if (!townAwaiting) { RequestPurchaseAndWait(sync, EconomyManager.CameraBasicId); break; }
                    if (!PurchaseAnswered(sync)) break;
                    townAwaiting = false;
                    result.townDenied = !sync.LastAccepted.Value && sync.LastReasonCode.Value.ToString() == "NotAtShop";
                    if (!result.townDenied) result.errors.Add($"away-from-shop purchase answer accepted={sync.LastAccepted.Value} reason={sync.LastReasonCode.Value}");
                    townStep = 2;
                    break;
                case 2: // open the shop by really interacting with the shop NPC
                    if (GoToService(local, cam, TownServiceCatalog.EquipmentShopId)) InteractEvery(local, 1.2f);
                    if (sync.ShopOpen) { result.townShopOpened = true; townStep = 3; }
                    break;
                case 3: // buy the basic camera as a separate item
                    if (!townAwaiting) { RequestPurchaseAndWait(sync, EconomyManager.CameraBasicId); break; }
                    if (!PurchaseAnswered(sync)) break;
                    townAwaiting = false;
                    result.townCameraBought = sync.LastAccepted.Value;
                    if (!result.townCameraBought) result.errors.Add($"camera purchase reason={sync.LastReasonCode.Value}");
                    townStep = host ? 4 : 5;
                    break;
                case 4: // host buys the first boat part once the shared money allows it
                    if (!townAwaiting)
                    {
                        if (sync.SharedBalance.Value >= 120) RequestPurchaseAndWait(sync, BoatRepairParts.Hull);
                        break;
                    }
                    if (!PurchaseAnswered(sync)) break;
                    townAwaiting = false;
                    result.townPartBought = sync.LastAccepted.Value;
                    if (!result.townPartBought) result.errors.Add($"boat part purchase reason={sync.LastReasonCode.Value}");
                    townStep = 5;
                    break;
            }
            if (townStep >= 5) local.SubmitLocalInput(Vector3.zero, 0);
        }

        // The basic camera is a separate purchase now, so a recording scenario needs a camera-owning recorder.
        // -p3-town covers the real buy path; this only seeds money + the recorder's camera before the dive.
        private void SeedRecorderCamera(SessionState state)
        {
            if (cameraSeeded || state.Phase != SessionPhase.Prep || adapter.Connection.IsSceneLoading) return;
            var guests = adapter.Session.Roster.Keys.Where(k => k.Value != 0).OrderBy(k => k.Value).ToArray();
            var recorder = guests.Length > 0 ? guests[0] : new PlayerId(0);   // same rule as ProbeRecording
            cameraSeeded = true;
            var economy = adapter.GetComponent<EconomyManager>();
            var seed = economy.ExportSaveData("smoke", "smoke");
            seed.SharedBalance = 150;
            economy.TryRestore(seed);
            if (!economy.TryPurchase(recorder, EconomyManager.CameraBasicId, 9001).Accepted)
                result.errors.Add("smoke camera seed rejected");
        }

        // Host-only bookkeeping for -p3-town: fills both bags through the real InventoryManager during the dive
        // (the pickup path itself is covered by -p2-hunt), then verifies the authoritative economy state.
        private void HostTown(SessionState state)
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var roster = adapter.Session.Roster.Keys.ToArray();
            if (state.Phase == SessionPhase.Dive && !townInjected && !adapter.Connection.IsSceneLoading &&
                SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene &&
                Time.realtimeSinceStartup - sceneStarted > 6f)
            {
                townInjected = true;
                var inventory = adapter.GetComponent<InventoryManager>();
                foreach (var id in roster)
                {
                    for (var i = 0; i < 2; i++)
                        inventory.TryAddCatch(id, new CaptureResult($"town-{id.Value}-{i}", state.DiveId, "sea_bass", 800, 1));
                    inventory.TryMarkSafeReturn(id);
                }
            }

            if (state.Phase != SessionPhase.Return) return;
            if (!townSampledReturn && economy.PendingTurnIns().Count > 0)
            {
                townSampledReturn = true;
                // Safe return must not pay by itself: money is still zero and every catch waits for the NPC.
                result.townNoAutoPay = economy.SharedBalance == 0 && economy.PendingTurnIns().Count == roster.Length * 2;
                if (!result.townNoAutoPay) result.errors.Add($"auto-pay check balance={economy.SharedBalance} pending={economy.PendingTurnIns().Count}");
            }

            if (result.townHostChecks) return;
            if (!townSampledReturn || economy.PendingTurnIns().Count != 0 || economy.BoatRepair.CompletedPartIds.Count != 1) return;
            if (!roster.All(id => economy.LoadoutFor(id).Contains(EconomyManager.CameraBasicId))) return;

            result.townBalance = economy.SharedBalance;
            // 2 players x 2 catches x 120 = 480; two cameras (300) and one boat part (120) leave 60.
            result.townHostChecks = economy.SharedBalance == 60;
            if (!result.townHostChecks) result.errors.Add($"town balance={economy.SharedBalance} expected=60");

            var store = adapter.GetComponent<EconomySaveStore>();
            var balance = economy.SharedBalance;
            result.townSaveRoundTrip = store.SaveNow() && store.LoadNow() && economy.SharedBalance == balance &&
                economy.BoatRepair.CompletedPartIds.Count == 1 && economy.PendingTurnIns().Count == 0 &&
                economy.LoadoutFor(new PlayerId(0)).Contains(EconomyManager.CameraBasicId);
            if (!result.townSaveRoundTrip) result.errors.Add("town save/load round trip failed: " + store.LastError);
        }

        private void CaptureRoom(string suffix = "")
        {
            // A hidden Windows player can skip presenting its backbuffer. Render explicitly
            // into a texture so the capture still contains the real camera and uGUI layout.
            var camera = Camera.allCameras.First(c => c.enabled);
            var canvas = adapter.GetComponentInChildren<Canvas>();
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            var previousCameraTarget = camera.targetTexture;
            var previousPlane = canvas.planeDistance;
            var previousTarget = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.01f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                var capturePath = Arg("-p1-screenshot");
                if (suffix.Length > 0) capturePath = Path.Combine(Path.GetDirectoryName(capturePath),
                    Path.GetFileNameWithoutExtension(capturePath) + suffix + ".png");
                File.WriteAllBytes(capturePath, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = previousCameraTarget;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousPlane;
                target.Release(); Destroy(target); Destroy(pixels);
            }
        }
        private void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && result.errors.Count < 15)
                result.errors.Add(message);
        }
        private void Finish()
        {
            Application.logMessageReceived -= Log;
            File.WriteAllText(path, JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 2);
        }
    }
}
