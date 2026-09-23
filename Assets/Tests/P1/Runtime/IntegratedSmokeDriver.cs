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
            public List<int> boatPartsSequence = new List<int>();
            public bool boatHullFound, boatEngineFound, boatFuelTankFound, boatDuplicateRejected, boatRepaired;
            public string boatStatusFinal;
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
        private bool Boat => Arg("-p3-boat") == "1";
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
            var leaveAt = 0f;
            var duration = Boat ? 78f : Town ? 76f : Event ? 114f : Record ? 66f : Hunt ? 58f : 46f;
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
                if (host && Boat) HostSampleBoatRepair();
                if (host && Record && (state.Phase == SessionPhase.Return || result.returned))
                    result.recordingPaid |= adapter.GetComponent<EconomyManager>().SharedBalance > 0;
                if (host && elapsed > (Town ? 34 : Event ? 92 : Boat ? 58 : Hunt || Record ? 44 : 33) && !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (host && elapsed > (Town ? 64 : Event ? 104 : Boat ? 66 : Record ? 56 : Hunt ? 48 : 36) && !lobbySent && state.Phase == SessionPhase.Return && !connection.IsSceneLoading)
                { lobbySent = true; GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (result.dive && state.Phase == SessionPhase.Lobby && state.Revision >= 4 && !connection.IsSceneLoading)
                {
                    result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    result.readyReset |= adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => !value) &&
                        string.IsNullOrEmpty(state.DiveId);
                }
                if (host && elapsed > (Town ? 68 : Event ? 108 : Boat ? 70 : Record ? 60 : Hunt ? 54 : 41) && !leaveSent) { leaveSent = true; adapter.LeaveRoom(); }
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
            if (Boat) result.passed &= result.boatRepaired &&
                (!host || (result.boatHullFound && result.boatFuelTankFound && result.boatDuplicateRejected &&
                    result.boatPartsSequence.Count >= 4 && result.boatPartsSequence[result.boatPartsSequence.Count - 1] == 3)) &&
                (host || result.boatEngineFound);
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
                    if (rejected) { result.boatDuplicateRejected = true; boatStep = 2; }
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
