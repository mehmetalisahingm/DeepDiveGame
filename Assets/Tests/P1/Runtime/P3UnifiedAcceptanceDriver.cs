using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Trip;
using DeepDive.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeepDive.P1.Lab
{
    // P3.4 only: one real runtime session that chains equipment -> recording -> hunt ->
    // free boat repair -> NPC turn-in -> trip/map -> save/load. It does not replace
    // the focused P1/P2/P3 smokes; it proves that their production seams compose.
    public sealed class P3UnifiedAcceptanceDriver : MonoBehaviour
    {
        [Serializable]
        public sealed class Result
        {
            public string mode = "", reason = "";
            public bool passed, connected, roster, ready, prep, dive, returned, readyReset, stopped;
            public bool fixtureBalanceSeeded, cameraShopOpened, cameraBought, cameraDuplicateRejected;
            public bool recordingStarted, recordingStopped, recordingClaimed;
            public bool fishMoved, catchObserved, catchDespawned, inventoryAdded, safeReturned;
            public bool boatHullFound, boatEngineFound, boatFuelTankFound, boatDuplicateRejected, boatRepaired, boatPartsHidden;
            public bool recordingTurnedIn, recordingDuplicateNoPay, fishSold, fishDuplicateNoPay, servicesCleared;
            public bool tripBoarded, tripUniqueSeats, tripDuplicateBoardHeld, tripMapDockedAtDock,
                tripMapUnderwayMoved, tripMapAnchoredAtAnchor, tripReturnMarkerSeen, tripReboarded,
                tripDockedEmpty, tripDone, saveReload;
            public bool activeTripHostRestartTested; // intentionally false until a real process-restart run exists.
            public int maxPlayers, finalBalance, tripMapPlayersMax, tripUnderwayPositions;
            public string tripSeatId = "", boatStatusFinal = "";
            public List<int> boatPartsSequence = new List<int>();
            public List<string> tripPhases = new List<string>();
            public List<string> trace = new List<string>();
            public List<string> errors = new List<string>();
        }

        private readonly Result result = new Result();
        private readonly Dictionary<ulong, Vector3> starts = new Dictionary<ulong, Vector3>();
        private readonly HashSet<string> tripPositions = new HashSet<string>();
        private SessionNetworkAdapter adapter;
        private string reportPath = "", observedStage = "";
        private float phaseStarted, nextService, nextBoat, nextHunt, recordingStartedAt;
        private float tripStepAt, tripNext, tripDisembarkedAt, tripMarkerSeatedSince;
        private int boatStep, serviceStep, tripStep, lastBoatCount = -1;
        private ulong serviceRequestBefore;
        private int duplicateBalanceBefore;
        private bool fixtureSeeded, purchaseSent, duplicatePurchaseSent, returnSent, completeSent, leaveSent;
        private bool tripDuplicateSent, tripDropSeen, tripOnBeach, tripMarkerErrored;
        private string tripLastPhase = "", tripSeatBefore = "";
        private Vector3? firstFishPosition;
        private readonly HashSet<string> tripUnderwaySeen = new HashSet<string>();
        private bool capturePrep, captureDive, captureReturn, captureTrip;

        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p3-unified-role").Length == 0) return;
            var go = new GameObject("P3UnifiedAcceptanceDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<P3UnifiedAcceptanceDriver>();
        }

        private IEnumerator Start()
        {
            result.mode = Arg("-p3-unified-role");
            reportPath = Arg("-p3-unified-report");
            var expected = int.Parse(Arg("-p3-unified-count", "2"));
            var port = ushort.Parse(Arg("-p3-unified-port", "18777"));
            var host = result.mode == "host";

            Application.logMessageReceived += Log;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            yield return null;
            yield return null;

            adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null)
            {
                result.errors.Add("No integrated adapter in real scene");
                Finish();
                yield break;
            }

            GameObject.Find("Port").GetComponent<InputField>().text = port.ToString();
            GameObject.Find(host ? "HostButton" : "JoinButton").GetComponent<Button>().onClick.Invoke();

            var started = Time.realtimeSinceStartup;
            var prepSent = false;
            var diveSent = false;
            while (Time.realtimeSinceStartup - started < 300f)
            {
                var connection = adapter.Connection;
                var state = adapter.Session.State;
                result.connected |= connection.Status == ConnectionStatus.Connected;
                result.maxPlayers = Math.Max(result.maxPlayers, adapter.Session.Roster.Count);
                result.roster |= adapter.Session.Roster.Count == expected && connection.Players.Count == expected;

                if (connection.Status == ConnectionStatus.Connected && !connection.IsSceneLoading)
                {
                    UpdateStageClock(state);
                    var local = LocalPlayer();
                    if (local != null) local.ReadKeyboard = false;
                    var allReady = adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(v => v);
                    result.ready |= allReady;

                    if (state.Phase == SessionPhase.Lobby && local != null && connection.LocalPlayerId.HasValue &&
                        adapter.Session.Roster.TryGetValue(connection.LocalPlayerId.Value, out var ready) && !ready && state.Revision == 0)
                        GameObject.Find("ReadyButton").GetComponent<Button>().onClick.Invoke();

                    if (host && !prepSent && allReady && Time.realtimeSinceStartup - started > 5f)
                    {
                        prepSent = true;
                        GameObject.Find("BeginPrepButton").GetComponent<Button>().onClick.Invoke();
                    }

                    if (state.Phase == SessionPhase.Prep)
                    {
                        result.prep = true;
                        TickPrep(local, expected);
                        if (host && !diveSent && HostCameraReady() && phaseStarted > 0 && Time.realtimeSinceStartup - phaseStarted > 7f)
                        {
                            diveSent = true;
                            GameObject.Find("BeginDiveButton").GetComponent<Button>().onClick.Invoke();
                        }
                    }
                    else if (state.Phase == SessionPhase.Dive)
                    {
                        result.dive = SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene;
                        TickDive(local, expected);
                    }
                    else if (state.Phase == SessionPhase.Return)
                    {
                        TickReturn(local, expected);
                    }
                    else if (state.Phase == SessionPhase.Lobby && result.dive && state.Revision >= 4)
                    {
                        result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                        result.readyReset = adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(v => !v) &&
                            string.IsNullOrEmpty(state.DiveId);
                        if (host && result.readyReset && !leaveSent)
                        {
                            leaveSent = true;
                            adapter.LeaveRoom();
                        }
                    }
                }

                if (result.returned && connection.Status == ConnectionStatus.Offline)
                {
                    result.stopped = adapter.Session.Roster.Count == 0 && connection.Players.Count == 0;
                    if (result.stopped) break;
                }
                yield return null;
            }

            result.reason = adapter != null ? adapter.Connection.LastError : "";
            result.passed = result.connected && result.roster && result.ready && result.prep && result.dive && result.returned &&
                result.readyReset && result.stopped && result.boatRepaired && result.boatPartsHidden && result.tripBoarded &&
                result.tripDuplicateBoardHeld && result.tripMapDockedAtDock && result.tripMapUnderwayMoved &&
                result.tripMapAnchoredAtAnchor && result.tripDockedEmpty && result.tripDone && result.tripMapPlayersMax >= expected &&
                result.tripPhases.SequenceEqual(new[] { "Docked", "Outbound", "Anchored", "Inbound", "Docked" }) && result.errors.Count == 0;

            var localFinal = LocalPlayer();
            var actor = localFinal != null && IsActor(localFinal.OwnerClientId);
            if (actor)
                result.passed &= result.cameraBought && result.cameraDuplicateRejected && result.recordingStarted && result.recordingStopped &&
                    result.catchObserved && result.catchDespawned && result.boatEngineFound && result.recordingTurnedIn &&
                    result.recordingDuplicateNoPay && result.fishSold && result.fishDuplicateNoPay && result.tripReturnMarkerSeen && result.tripReboarded;
            if (host)
                result.passed &= result.fixtureBalanceSeeded && result.recordingClaimed && result.inventoryAdded && result.safeReturned &&
                    result.boatHullFound && result.boatFuelTankFound && result.boatDuplicateRejected && result.tripUniqueSeats &&
                    result.servicesCleared && result.saveReload && result.boatPartsSequence.Count >= 4 && result.boatPartsSequence.Last() == 3;

            Finish();
        }

        private NetworkPlayer LocalPlayer() => FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsSpawned && p.IsOwner);

        private PlayerId ActorId()
        {
            var guests = adapter.Session.Roster.Keys.Where(id => id.Value != 0).OrderBy(id => id.Value).ToArray();
            return guests.Length > 0 ? guests[0] : new PlayerId(0);
        }

        private bool IsActor(ulong ownerClientId) => ActorId().Value == ownerClientId;

        private void UpdateStageClock(SessionState state)
        {
            var stage = SceneManager.GetActiveScene().name + ":" + state.Phase;
            if (stage == observedStage) return;
            observedStage = stage;
            phaseStarted = Time.realtimeSinceStartup;
            starts.Clear();
            result.trace.Add("phase=" + stage);
        }

        private void TickPrep(NetworkPlayer local, int expected)
        {
            if (local == null) return;
            if (adapter.IsAuthority && !fixtureSeeded)
            {
                fixtureSeeded = true;
                var economy = adapter.GetComponent<EconomyManager>();
                var seed = new EconomySaveData
                {
                    CampaignId = "p3-unified-acceptance",
                    CheckpointId = "fixture-start",
                    SharedBalance = 150,
                    Revision = 0
                };
                result.fixtureBalanceSeeded = economy.TryRestore(seed) && economy.SharedBalance == 150;
                if (!result.fixtureBalanceSeeded) result.errors.Add("fixture balance seed failed");
            }

            if (!IsActor(local.OwnerClientId))
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                HostCapture("prep", ref capturePrep, HostCameraReady());
                return;
            }

            var sync = local.GetComponent<EconomyPlayerSync>();
            var cam = local.GetComponentInChildren<Camera>(true);
            if (sync == null || cam == null) return;
            if (!sync.ShopOpen)
            {
                if (GoToService(local, cam, TownServiceCatalog.EquipmentShopId)) InteractEvery(local, 1.0f);
                if (sync.ShopOpen) result.cameraShopOpened = true;
                return;
            }
            result.cameraShopOpened = true;

            if (!result.cameraBought)
            {
                if (!purchaseSent)
                {
                    purchaseSent = true;
                    sync.RequestPurchase(EconomyManager.CameraBasicId);
                    serviceRequestBefore = sync.LastRequestId.Value;
                    return;
                }
                if (sync.LastRequestId.Value == serviceRequestBefore) return;
                result.cameraBought = sync.LastAccepted.Value && sync.SharedBalance.Value == 0;
                if (!result.cameraBought) result.errors.Add("real camera purchase failed: " + sync.LastReasonCode.Value);
                return;
            }

            if (!result.cameraDuplicateRejected)
            {
                if (!duplicatePurchaseSent)
                {
                    duplicatePurchaseSent = true;
                    duplicateBalanceBefore = sync.SharedBalance.Value;
                    serviceRequestBefore = sync.LastRequestId.Value;
                    sync.RequestPurchase(EconomyManager.CameraBasicId);
                    return;
                }
                if (sync.LastRequestId.Value == serviceRequestBefore) return;
                result.cameraDuplicateRejected = !sync.LastAccepted.Value && sync.LastReasonCode.Value.ToString() == "AlreadyProcessed" &&
                    sync.SharedBalance.Value == duplicateBalanceBefore;
                if (!result.cameraDuplicateRejected) result.errors.Add("duplicate camera purchase changed state");
            }
            local.SubmitLocalInput(Vector3.zero, 0);
        }

        private bool HostCameraReady()
        {
            if (!adapter.IsAuthority || !fixtureSeeded) return false;
            var economy = adapter.GetComponent<EconomyManager>();
            return economy.SharedBalance == 0 && economy.LoadoutFor(ActorId()).Contains(EconomyManager.CameraBasicId);
        }

        private void TickDive(NetworkPlayer local, int expected)
        {
            if (local == null) return;
            var elapsed = Time.realtimeSinceStartup - phaseStarted;
            if (adapter.IsAuthority) SampleHostBoatRepair();

            if (elapsed < 14f)
            {
                DriveRecording(local);
                HostCapture("dive", ref captureDive, result.recordingClaimed);
                return;
            }
            if (elapsed < 36f)
            {
                DriveHunt(local);
                return;
            }
            if (!result.boatRepaired || !result.boatPartsHidden)
            {
                DriveBoatParts(local);
                return;
            }

            DriveSafeReturn(local);
            if (adapter.IsAuthority && !returnSent && HostReadyForReturn())
            {
                returnSent = true;
                GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke();
            }
        }

        private void DriveRecording(NetworkPlayer local)
        {
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            var subject = FindObjectsByType<RecordingSubject>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.GetComponent<FishActor>() != null && x.IsSpawned);
            if (subject == null)
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }

            if (adapter.IsAuthority)
            {
                var world = adapter.GetComponent<RecordingWorldBinding>();
                result.recordingClaimed |= world.Binding.Director.ClaimCount == 1;
            }
            if (!IsActor(local.OwnerClientId))
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }

            var delta = subject.transform.position - local.RecordingEyePosition;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            var move = delta.magnitude > 6f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
            var bridge = adapter.GetComponent<RecordingNetworkBridge>();
            if (bridge.IsRecordingLocal)
            {
                result.recordingStarted = true;
                if (recordingStartedAt == 0) recordingStartedAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - recordingStartedAt > 8.5f && Time.realtimeSinceStartup >= nextService)
                {
                    nextService = Time.realtimeSinceStartup + 0.8f;
                    bridge.ToggleRecordingLocal();
                }
            }
            else if (recordingStartedAt > 0)
            {
                result.recordingStopped = true;
            }
            else if (Time.realtimeSinceStartup >= nextService)
            {
                nextService = Time.realtimeSinceStartup + 0.8f;
                bridge.ToggleRecordingLocal();
            }
        }

        private void DriveHunt(NetworkPlayer local)
        {
            if (adapter.IsAuthority && adapter.GetComponent<InventoryManager>().Bags.TryGetValue(ActorId(), out var bag))
                result.inventoryAdded |= bag.Items.Count > 0;

            var fish = FindFirstObjectByType<FishActor>();
            if (fish == null || !fish.IsSpawned)
            {
                if (result.catchObserved) result.catchDespawned = true;
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }
            if (!firstFishPosition.HasValue) firstFishPosition = fish.transform.position;
            result.fishMoved |= Vector3.Distance(firstFishPosition.Value, fish.transform.position) > 0.15f;
            var available = fish.GetComponent<CatchObject>().IsAvailable;
            result.catchObserved |= available;

            if (!IsActor(local.OwnerClientId))
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }
            var camera = local.GetComponentInChildren<Camera>(true);
            var delta = fish.GetComponent<Collider>().bounds.center - camera.transform.position;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            var move = available && delta.magnitude > 1.7f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * delta.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, pitch);
            if (Time.realtimeSinceStartup < nextHunt) return;
            nextHunt = Time.realtimeSinceStartup + 0.75f;
            if (available)
            {
                if (delta.magnitude < 2.2f) local.SubmitPickupLocal();
            }
            else local.SubmitHarpoonLocal();
        }

        private void DriveBoatParts(NetworkPlayer local)
        {
            var economySync = local.GetComponent<EconomyPlayerSync>();
            if (economySync != null && economySync.BoatPartsDone.Value >= BoatRepairParts.All.Count) result.boatRepaired = true;
            if (economySync != null && economySync.BoatPartsMask.Value == 7)
            {
                var anchors = FindObjectsByType<BoatPartAnchor>(FindObjectsSortMode.None);
                result.boatPartsHidden = anchors.Length == 3 && anchors.All(a =>
                    a.GetComponentsInChildren<Renderer>().All(r => !r.enabled) &&
                    a.GetComponentsInChildren<Collider>().All(c => !c.enabled));
            }

            var host = adapter.IsAuthority;
            var actor = IsActor(local.OwnerClientId);
            if (!host && !actor)
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }
            var partName = host ? (boatStep <= 1 ? "BoatPart_Hull" : "BoatPart_FuelTank") : "BoatPart_Engine";
            var part = GameObject.Find(partName);
            var cam = local.GetComponentInChildren<Camera>(true);
            if (part == null || cam == null)
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }
            if (!GoToBoatPart(local, cam, part.transform.position)) return;
            if (Time.realtimeSinceStartup < nextBoat) return;
            nextBoat = Time.realtimeSinceStartup + 0.8f;
            var before = local.LastActionRequestId.Value;
            local.SubmitPickupLocal();
            StartCoroutine(ReadBoatPickup(local, before, host));
        }

        private IEnumerator ReadBoatPickup(NetworkPlayer local, ulong before, bool host)
        {
            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (local.LastActionRequestId.Value == before && Time.realtimeSinceStartup < deadline) yield return null;
            if (local.LastActionRequestId.Value == before) yield break;
            var accepted = local.LastActionKind.Value == (byte)PlayerActionKind.Pickup &&
                local.LastActionResult.Value == (int)PlayerActionResult.Accepted;
            var rejected = local.LastActionKind.Value == (byte)PlayerActionKind.Pickup &&
                local.LastActionResult.Value == (int)PlayerActionResult.Rejected;

            if (!host)
            {
                if (accepted) { result.boatEngineFound = true; boatStep = 3; }
                else if (!result.boatRepaired) result.errors.Add("engine pickup rejected=" + local.LastActionResult.Value);
                yield break;
            }

            if (boatStep == 0)
            {
                if (accepted) { result.boatHullFound = true; boatStep = 1; }
                else result.errors.Add("hull pickup rejected=" + local.LastActionResult.Value);
            }
            else if (boatStep == 1)
            {
                var hull = GameObject.Find("BoatPart_Hull");
                var sync = local.GetComponent<EconomyPlayerSync>();
                var hiddenHull = local.LastActionResult.Value == (int)PlayerActionResult.InvalidTarget && sync != null &&
                    (sync.BoatPartsMask.Value & 1) != 0 && hull != null && hull.GetComponentsInChildren<Collider>().All(c => !c.enabled);
                if (rejected || hiddenHull) { result.boatDuplicateRejected = true; boatStep = 2; }
                else result.errors.Add("duplicate hull pickup changed state");
            }
            else if (boatStep == 2)
            {
                if (accepted) { result.boatFuelTankFound = true; boatStep = 3; }
                else if (!result.boatRepaired) result.errors.Add("fuel pickup rejected=" + local.LastActionResult.Value);
            }
        }

        private void SampleHostBoatRepair()
        {
            var state = adapter.GetComponent<EconomyManager>().BoatRepair;
            var count = state.CompletedPartIds.Count;
            if (count != lastBoatCount)
            {
                lastBoatCount = count;
                result.boatPartsSequence.Add(count);
            }
            if (state.Status == BoatRepairStatus.Repaired)
            {
                result.boatRepaired = true;
                result.boatStatusFinal = state.Status.ToString();
            }
        }

        private void DriveSafeReturn(NetworkPlayer local)
        {
            if (!IsActor(local.OwnerClientId))
            {
                local.SubmitLocalInput(Vector3.zero, 0);
                return;
            }
            var target = NextRampWaypoint(local.transform.position);
            var heading = target - local.transform.position;
            var flat = heading; flat.y = 0;
            var yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            local.SubmitLocalInput(heading.magnitude > 0.3f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized : Vector3.zero, yaw, 0);
        }

        private bool HostReadyForReturn()
        {
            if (!adapter.IsAuthority || !result.boatRepaired || !result.recordingClaimed) return false;
            var inventory = adapter.GetComponent<InventoryManager>();
            if (!inventory.Bags.TryGetValue(ActorId(), out var bag) || !bag.SafelyReturned) return false;
            result.safeReturned = true;
            result.inventoryAdded |= bag.Items.Count > 0;
            return result.inventoryAdded;
        }

        private void TickReturn(NetworkPlayer local, int expected)
        {
            if (local == null) return;
            var sync = local.GetComponent<EconomyPlayerSync>();
            if (sync == null) return;

            if (IsActor(local.OwnerClientId) && serviceStep < 4)
                DriveServices(local, sync);
            else
                DriveTrip(local, expected);

            if (adapter.IsAuthority)
            {
                SampleHostBoatRepair();
                var economy = adapter.GetComponent<EconomyManager>();
                result.servicesCleared |= economy.PendingTurnIns().Count == 0 && result.tripDone;
                if (result.tripDone && result.tripDockedEmpty && !result.saveReload) HostSaveReload();
                HostCapture("return", ref captureReturn, economy.PendingTurnIns().Count == 0);
                if (result.tripDone && result.saveReload && !completeSent)
                {
                    completeSent = true;
                    result.finalBalance = economy.SharedBalance;
                    GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke();
                }
            }
        }

        private void DriveServices(NetworkPlayer local, EconomyPlayerSync sync)
        {
            var cam = local.GetComponentInChildren<Camera>(true);
            if (cam == null) return;
            switch (serviceStep)
            {
                case 0:
                    if (sync.PendingRecordings.Value > 0)
                    {
                        if (GoToService(local, cam, TownServiceCatalog.RecordingBuyerId)) InteractEvery(local, 1.0f);
                        if (sync.LastServiceRequestId.Value != 0 && (ServicePointType)sync.LastServiceType.Value == ServicePointType.RecordingBuyer && sync.LastServiceAccepted.Value)
                            result.recordingTurnedIn = true;
                        return;
                    }
                    duplicateBalanceBefore = sync.SharedBalance.Value;
                    serviceRequestBefore = sync.LastServiceRequestId.Value;
                    serviceStep = 1;
                    break;
                case 1:
                    if (sync.LastServiceRequestId.Value == serviceRequestBefore)
                    {
                        if (GoToService(local, cam, TownServiceCatalog.RecordingBuyerId)) InteractEvery(local, 1.0f);
                        return;
                    }
                    result.recordingDuplicateNoPay = !sync.LastServiceAccepted.Value &&
                        sync.LastServiceReason.Value.ToString() == "NothingToTurnIn" && sync.SharedBalance.Value == duplicateBalanceBefore;
                    if (!result.recordingDuplicateNoPay) result.errors.Add("duplicate recording turn-in paid twice");
                    serviceStep = 2;
                    break;
                case 2:
                    if (sync.PendingCatches.Value > 0)
                    {
                        if (GoToService(local, cam, TownServiceCatalog.FishBuyerId)) InteractEvery(local, 1.0f);
                        if (sync.LastServiceRequestId.Value != 0 && (ServicePointType)sync.LastServiceType.Value == ServicePointType.FishBuyer && sync.LastServiceAccepted.Value)
                            result.fishSold = true;
                        return;
                    }
                    duplicateBalanceBefore = sync.SharedBalance.Value;
                    serviceRequestBefore = sync.LastServiceRequestId.Value;
                    serviceStep = 3;
                    break;
                case 3:
                    if (sync.LastServiceRequestId.Value == serviceRequestBefore)
                    {
                        if (GoToService(local, cam, TownServiceCatalog.FishBuyerId)) InteractEvery(local, 1.0f);
                        return;
                    }
                    result.fishDuplicateNoPay = !sync.LastServiceAccepted.Value &&
                        sync.LastServiceReason.Value.ToString() == "NothingToTurnIn" && sync.SharedBalance.Value == duplicateBalanceBefore;
                    if (!result.fishDuplicateNoPay) result.errors.Add("duplicate fish sale paid twice");
                    serviceStep = 4;
                    break;
            }
            if (serviceStep >= 4) local.SubmitLocalInput(Vector3.zero, 0);
        }

        private void DriveTrip(NetworkPlayer local, int expected)
        {
            var sync = local.GetComponent<BoatTripPlayerSync>();
            if (sync == null || !sync.IsSpawned) return;
            var phase = (BoatTripPhase)sync.Phase.Value;
            var phaseName = phase.ToString();
            if (sync.BoatVisible.Value && tripLastPhase != phaseName)
            {
                tripLastPhase = phaseName;
                result.tripPhases.Add(phaseName);
            }
            SampleTripMap(sync, phase);
            var now = Time.realtimeSinceStartup;
            if (tripStepAt == 0) tripStepAt = now;
            if (tripStep < 90 && now - tripStepAt > 40f)
            {
                result.errors.Add($"trip step {tripStep} timeout phase={phaseName} seats={sync.SeatedCount.Value} reason={sync.LastReasonCode.Value}");
                TripStep(99);
            }

            switch (tripStep)
            {
                case 0:
                {
                    var dock = FindObjectsByType<RouteAnchor>(FindObjectsSortMode.None).FirstOrDefault(a => a.AnchorId == DiveRouteAnchors.Dock);
                    if (dock == null || !sync.BoatVisible.Value) { local.SubmitLocalInput(Vector3.zero, 0); return; }
                    var offset = adapter.IsAuthority ? -0.6f : 0.6f + 0.25f * (local.OwnerClientId % 3);
                    var stand = new Vector3(dock.BoardingPosition.x + offset, dock.BoardingPosition.y, -3.6f);
                    var toStand = stand - local.transform.position; toStand.y = 0;
                    if (toStand.magnitude > 0.15f)
                    {
                        var strip = GameObject.Find("Beach_Platform");
                        var stripCollider = strip != null ? strip.GetComponent<Collider>() : null;
                        if (!tripOnBeach && stripCollider != null && local.transform.position.z <= stripCollider.bounds.max.z + 0.2f &&
                            local.transform.position.y >= stripCollider.bounds.max.y - 0.3f) tripOnBeach = true;
                        Vector3 target;
                        if (!tripOnBeach) target = NextBeachWaypoint(local.transform.position, stand);
                        else if (local.transform.position.z < -10f && Mathf.Abs(local.transform.position.x - stand.x) > 1f)
                            target = new Vector3(stand.x, local.transform.position.y, -12.6f);
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
                case 1:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.IsSeated)
                    {
                        result.tripBoarded = true;
                        result.tripSeatId = sync.MySeatId.Value.ToString();
                        TripStep(2);
                    }
                    else if (now >= tripNext)
                    {
                        tripNext = now + 1.0f;
                        sync.RequestBoardNearestLocal();
                    }
                    return;
                case 2:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.SeatedCount.Value < expected) return;
                    if (adapter.IsAuthority)
                    {
                        var manager = adapter.GetComponent<BoatTripManager>();
                        if (manager != null)
                        {
                            var seats = manager.State.Seats;
                            result.tripUniqueSeats = seats.Count == expected && seats.Select(s => s.SeatId).Distinct().Count() == expected &&
                                seats.Select(s => s.Player.Value).Distinct().Count() == expected;
                        }
                    }
                    if (!tripDuplicateSent)
                    {
                        tripDuplicateSent = true;
                        tripSeatBefore = sync.MySeatId.Value.ToString();
                        tripNext = now + 1.0f;
                        sync.RequestBoardNearestLocal();
                        return;
                    }
                    if (now < tripNext) return;
                    result.tripDuplicateBoardHeld = sync.IsSeated && sync.MySeatId.Value.ToString() == tripSeatBefore && sync.SeatedCount.Value == expected;
                    if (!result.tripDuplicateBoardHeld) result.errors.Add("duplicate board changed seat/count");
                    TripStep(3);
                    return;
                case 3:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Anchored) { TripStep(4); return; }
                    if (sync.AmOwner.Value && phase == BoatTripPhase.Docked && now >= tripNext)
                    {
                        if (adapter.IsAuthority && adapter.GetComponent<EconomyManager>().PendingTurnIns().Count != 0) return;
                        tripNext = now + 1.2f;
                        sync.RequestStartRouteLocal(BoatTripIds.NearRouteId);
                    }
                    return;
                case 4:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    HostCapture("trip", ref captureTrip, phase == BoatTripPhase.Anchored);
                    if (phase != BoatTripPhase.Anchored) return;
                    if (!sync.AmOwner.Value)
                    {
                        if (sync.IsSeated)
                        {
                            if (now >= tripNext) { tripNext = now + 1.0f; sync.RequestDisembarkLocal(); }
                            return;
                        }
                        if (tripDisembarkedAt == 0) tripDisembarkedAt = now;
                        if (now - tripDisembarkedAt > 1.5f) TripStep(5);
                        return;
                    }
                    if (sync.SeatedCount.Value < expected) tripDropSeen = true;
                    if (tripDropSeen) TripStep(6);
                    return;
                case 5:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.IsSeated) { result.tripReboarded = true; TripStep(7); return; }
                    if (now >= tripNext) { tripNext = now + 1.0f; sync.RequestBoardNearestLocal(); }
                    return;
                case 6:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.SeatedCount.Value >= expected) TripStep(7);
                    return;
                case 7:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Inbound) tripUnderwaySeen.Add("Inbound");
                    if (phase == BoatTripPhase.Docked && tripUnderwaySeen.Contains("Inbound")) { TripStep(8); return; }
                    if (sync.AmOwner.Value && phase == BoatTripPhase.Anchored && sync.SeatedCount.Value >= expected && now >= tripNext)
                    {
                        tripNext = now + 1.2f;
                        sync.RequestReturnLocal();
                    }
                    return;
                case 8:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!sync.IsSeated) { TripStep(9); return; }
                    if (now >= tripNext) { tripNext = now + 1.0f; sync.RequestDisembarkLocal(); }
                    return;
                case 9:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (phase == BoatTripPhase.Docked && sync.SeatedCount.Value == 0)
                    {
                        result.tripDockedEmpty = true;
                        result.tripDone = true;
                        TripStep(90);
                    }
                    return;
                default:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
            }
        }

        private void TripStep(int step)
        {
            result.trace.Add($"trip {tripStep}->{step} t={Time.realtimeSinceStartup - phaseStarted:F1}");
            tripStep = step;
            tripStepAt = Time.realtimeSinceStartup;
            tripNext = 0;
        }

        private void SampleTripMap(BoatTripPlayerSync sync, BoatTripPhase phase)
        {
            var icons = BoatMapView.LastIcons;
            if (icons == null || icons.Count == 0 || !BoatMapView.LastHadRegion) return;
            MapIcon dock = default, boat = default;
            var hasDock = false; var hasBoat = false; var marker = false; var players = 0;
            foreach (var icon in icons)
            {
                if (icon.IconId == BoatMapPresenter.DockIconId) { dock = icon; hasDock = true; }
                else if (icon.IconId == BoatTripIds.BoatId) { boat = icon; hasBoat = true; }
                else if (icon.IconId == BoatMapPresenter.ReturnMarkerIconId) marker = true;
                else if (icon.IconId.StartsWith(BoatMapPresenter.PlayerIconPrefix, StringComparison.Ordinal)) players++;
            }
            result.tripMapPlayersMax = Math.Max(result.tripMapPlayersMax, players);
            if (marker && sync.IsSeated)
            {
                if (tripMarkerSeatedSince == 0) tripMarkerSeatedSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - tripMarkerSeatedSince > 0.6f && !tripMarkerErrored)
                {
                    tripMarkerErrored = true;
                    result.errors.Add("return marker drawn while aboard");
                }
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
            if (phase == BoatTripPhase.Docked && hasBoat && MapDist(dock, expectedDock) < 0.01f && MapDist(boat, expectedDock) < 0.01f)
                result.tripMapDockedAtDock = true;
            if ((phase == BoatTripPhase.Outbound || phase == BoatTripPhase.Inbound) && hasBoat)
            {
                tripPositions.Add($"{Mathf.Round(boat.MapX * 50)}:{Mathf.Round(boat.MapZ * 50)}");
                result.tripUnderwayPositions = tripPositions.Count;
                if (tripPositions.Count >= 3) result.tripMapUnderwayMoved = true;
            }
            if (phase == BoatTripPhase.Anchored && hasBoat && MapDist(boat, expectedAnchor) < 0.01f)
                result.tripMapAnchoredAtAnchor = true;
            if (phase == BoatTripPhase.Anchored && !sync.IsSeated && marker) result.tripReturnMarkerSeen = true;
        }

        private static float MapDist(MapIcon icon, Vector2 point) =>
            Mathf.Sqrt((icon.MapX - point.x) * (icon.MapX - point.x) + (icon.MapZ - point.y) * (icon.MapZ - point.y));

        private void HostSaveReload()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var store = adapter.GetComponent<EconomySaveStore>();
            var trip = adapter.GetComponent<BoatTripManager>();
            var before = economy.SharedBalance;
            var ok = store.SaveNow() && store.LoadNow();
            var saved = economy.ExportSaveData("p3-unified-acceptance", "after-trip");
            ok &= economy.SharedBalance == before && economy.BoatRepair.Status == BoatRepairStatus.Repaired &&
                economy.LoadoutFor(ActorId()).Contains(EconomyManager.CameraBasicId) && economy.PendingTurnIns().Count == 0 &&
                saved.SoldCaptureIds.Count > 0 && saved.PaidRecordingIds.Count > 0 &&
                trip != null && trip.State.Phase == BoatTripPhase.Docked && trip.State.Seats.Count == 0;
            result.saveReload = ok;
            if (!ok) result.errors.Add("unified save/load check failed: " + store.LastError);
        }

        private bool GoToService(NetworkPlayer local, Camera cam, string serviceId)
        {
            var anchor = FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None)
                .FirstOrDefault(a => a.Definition.ServiceId == serviceId);
            if (anchor == null) { local.SubmitLocalInput(Vector3.zero, 0); return false; }
            var lateral = anchor.transform.right * (adapter.IsAuthority ? 0f : 0.8f + 0.2f * (local.OwnerClientId % 3));
            var stand = anchor.WorldPosition + anchor.transform.forward * 1.6f + lateral;
            var toStand = stand - local.transform.position; toStand.y = 0;
            if (toStand.magnitude > 0.35f)
            {
                var target = NextBeachWaypoint(local.transform.position, stand);
                var heading = target - local.transform.position;
                var flat = heading; flat.y = 0;
                var yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                local.SubmitLocalInput(Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized, yaw, 0);
                return false;
            }
            var toNpc = anchor.WorldPosition + Vector3.up - cam.transform.position;
            var aimYaw = Mathf.Atan2(toNpc.x, toNpc.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(toNpc.y, new Vector2(toNpc.x, toNpc.z).magnitude) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, aimYaw, pitch);
            return true;
        }

        private void InteractEvery(NetworkPlayer local, float seconds)
        {
            if (Time.realtimeSinceStartup < nextService) return;
            nextService = Time.realtimeSinceStartup + seconds;
            local.SubmitServiceInteractionLocal();
        }

        private bool GoToBoatPart(NetworkPlayer local, Camera cam, Vector3 targetPosition)
        {
            const float surfaceY = 8f;
            var submerged = targetPosition.y < surfaceY - 0.1f;
            var stand = submerged ? targetPosition + new Vector3(0, 0, 1.8f) : targetPosition;
            var toStand = stand - local.transform.position;
            if (toStand.magnitude > 1.3f)
            {
                var target = NextBeachWaypoint(local.transform.position, stand);
                var heading = target - local.transform.position;
                var flat = heading; flat.y = 0;
                var yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                local.SubmitLocalInput(Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized, yaw, 0);
                return false;
            }
            var toPart = targetPosition + Vector3.up * 0.3f - cam.transform.position;
            var aimYaw = Mathf.Atan2(toPart.x, toPart.z) * Mathf.Rad2Deg;
            var pitch = -Mathf.Atan2(toPart.y, new Vector2(toPart.x, toPart.z).magnitude) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, aimYaw, pitch);
            return true;
        }

        private Vector3 NextBeachWaypoint(Vector3 position, Vector3 stand)
        {
            var wade = GameObject.Find("Beach_Wade");
            var platform = GameObject.Find("Beach_Platform");
            var ramp = wade != null ? wade.GetComponent<Collider>() : null;
            var strip = platform != null ? platform.GetComponent<Collider>() : null;
            if (ramp == null || strip == null) return stand;
            var north = strip.bounds.max.z;
            if (position.z <= north + 0.2f) return stand;
            var lane = wade.transform.position.x;
            var aheadZ = Mathf.Max(position.z - 1.2f, north);
            var origin = new Vector3(lane, ramp.bounds.max.y + 5f, aheadZ);
            float y;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == ramp || hit.collider == strip)) y = hit.point.y - 0.05f;
            else y = Mathf.Min(position.y, ramp.bounds.min.y - 0.3f);
            return new Vector3(lane, y, aheadZ);
        }

        private Vector3 NextRampWaypoint(Vector3 position)
        {
            var rampGo = GameObject.Find("Shore_Ramp");
            var ledgeGo = GameObject.Find("Shore_Ledge");
            var ramp = rampGo != null ? rampGo.GetComponent<Collider>() : null;
            var ledge = ledgeGo != null ? ledgeGo.GetComponent<Collider>() : null;
            if (ramp == null || ledge == null) return ledge != null ? ledge.bounds.center : position;
            var edge = ledge.bounds.min.x;
            if (position.x >= edge - 0.2f) return ledge.bounds.center;
            var aheadX = Mathf.Min(position.x + 1.2f, edge);
            var origin = new Vector3(aheadX, ramp.bounds.max.y + 5f, rampGo.transform.position.z);
            float y;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == ramp || hit.collider == ledge)) y = hit.point.y - 0.05f;
            else y = Mathf.Min(position.y, ramp.bounds.min.y - 0.3f);
            return new Vector3(aheadX, y, rampGo.transform.position.z);
        }

        private void HostCapture(string suffix, ref bool done, bool condition)
        {
            if (!adapter.IsAuthority || done || !condition) return;
            var basePath = Arg("-p3-unified-screenshot");
            if (string.IsNullOrWhiteSpace(basePath)) return;
            done = true;
            CaptureRoom(basePath, suffix);
        }

        private void CaptureRoom(string basePath, string suffix)
        {
            var camera = Camera.allCameras.FirstOrDefault(c => c.enabled);
            var canvas = adapter.GetComponentInChildren<Canvas>();
            if (camera == null || canvas == null) return;
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
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.01f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                var path = Path.Combine(Path.GetDirectoryName(basePath) ?? "", Path.GetFileNameWithoutExtension(basePath) + "-" + suffix + ".png");
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget;
                camera.targetTexture = previousCameraTarget;
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousPlane;
                target.Release();
                Destroy(target);
                Destroy(pixels);
            }
        }

        private void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && result.errors.Count < 20)
                result.errors.Add(message);
        }

        private void Finish()
        {
            Application.logMessageReceived -= Log;
            if (!string.IsNullOrWhiteSpace(reportPath)) File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 2);
        }
    }
}
