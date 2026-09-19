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
        private bool purchaseSent;
        private int townStep, townBefore;
        private float townNext, townSettleAt;
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
            var leaveAt = 0f;
            var duration = Town ? 76f : Event ? 114f : Record ? 66f : Hunt ? 58f : 46f;
            while (Time.realtimeSinceStartup - started < duration)
            {
                var elapsed = Time.realtimeSinceStartup - started;
                var connection = adapter.Connection;
                var state = adapter.Session.State;
                if (host && !screenshot && elapsed > 5 && Arg("-p1-screenshot").Length > 0)
                { screenshot = true; CaptureRoom(); }
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
                if (host && elapsed > 19 && !prepSent && allReady)
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
                if (host && Town) HostTown(state);
                if (host && Record && !Town) SeedRecorderCamera(state);
                if (host && Record && (state.Phase == SessionPhase.Return || result.returned))
                    result.recordingPaid |= adapter.GetComponent<EconomyManager>().SharedBalance > 0;
                if (host && elapsed > (Town ? 34 : Event ? 92 : Hunt || Record ? 44 : 33) && !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (host && elapsed > (Town ? 64 : Event ? 104 : Record ? 56 : Hunt ? 48 : 36) && !lobbySent && state.Phase == SessionPhase.Return && !connection.IsSceneLoading)
                { lobbySent = true; GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (result.dive && state.Phase == SessionPhase.Lobby && state.Revision >= 4 && !connection.IsSceneLoading)
                {
                    result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    result.readyReset |= adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => !value) &&
                        string.IsNullOrEmpty(state.DiveId);
                }
                if (host && elapsed > (Town ? 68 : Event ? 108 : Record ? 60 : Hunt ? 54 : 41) && !leaveSent) { leaveSent = true; adapter.LeaveRoom(); }
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
            // After the take finishes, actually swim to the surface exit. Do not inject a
            // safe-return flag: SafeReturnZone must mark the remote diver through real movement.
            var move = result.recordingStopped ? Vector3.up :
                delta.magnitude > 6f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
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


        // ---- P3.2 town: real owner inputs + RPCs against the PrepArea NPCs --------------------
        // Walks to the NPC, aims at it and only then lets the caller interact, so the host raycast,
        // range check and service handler all run for real (no injected result).
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
                var walkYaw = Mathf.Atan2(toStand.x, toStand.z) * Mathf.Rad2Deg;
                local.SubmitLocalInput(Quaternion.Inverse(Quaternion.Euler(0, walkYaw, 0)) * toStand.normalized, walkYaw, 0);
                return false;
            }
            var toNpc = anchor.WorldPosition + Vector3.up - cam.transform.position;
            var yaw = Mathf.Atan2(toNpc.x, toNpc.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(toNpc.y, new Vector2(toNpc.x, toNpc.z).magnitude) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, yaw, pitch);
            if (townSettleAt == 0) townSettleAt = Time.realtimeSinceStartup + 0.6f;   // let the aim reach the host
            return Time.realtimeSinceStartup >= townSettleAt;
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
                    result.townTrace.Add($"recorder={recorder} me={local.OwnerClientId} pos=({q.x:0.0},{q.z:0.0}) pendingRec={sync.PendingRecordings.Value} svc={sync.LastServiceRequestId.Value}/{sync.LastServiceAccepted.Value}/{sync.LastServiceReason.Value} act={local.LastActionKind.Value}/{local.LastActionResult.Value}/{local.LastActionRequestId.Value} bal={sync.SharedBalance.Value}");
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
                result.townTrace.Add($"step={townStep} pos=({p.x:0.0},{p.z:0.0}) act={local.LastActionKind.Value}/{local.LastActionResult.Value}/{local.LastActionRequestId.Value} shop={sync.ActiveServiceId.Value} bal={sync.SharedBalance.Value}");
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

        private void CaptureRoom()
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
                File.WriteAllBytes(Arg("-p1-screenshot"), pixels.EncodeToPNG());
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
