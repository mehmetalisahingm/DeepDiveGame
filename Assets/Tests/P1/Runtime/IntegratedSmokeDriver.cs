using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Media;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using DeepDive.Inventory;
using DeepDive.Economy;
using DeepDive.Trip;
using DeepDive.MapUI;
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
            public bool tripFleetVehicleReady, tripHullKindCorrect;
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
            public List<string> mediaTrace = new List<string>();
            public int mediaSubmitted, mediaArchiveSeen, mediaPaid, mediaFollowersSeen;
            public int mediaReloadClips, mediaReloadPublications, mediaReloadBalance;
            public bool mediaReloadPass;
            public bool mediaRetryHeld, mediaFarRefused, mediaPanelOpen, mediaNotOwnerRefused, mediaNotPublishableRefused, mediaPublished,
                mediaDuplicateRefused, mediaFeedBoth, mediaResultSeen, mediaNpcWithdrawn, mediaPaidOnce, mediaReplayPaysNothing,
                mediaSavedOnDisk, mediaReloadClean;
            public bool acceptOwner, acceptClipReal, acceptWorldContext, acceptPlayable, acceptPanelOpen, acceptFarRefused, acceptNotOwnerRefused, acceptPublished,
                acceptDuplicateRefused, acceptResultSeen, acceptNpcQueuedBefore, acceptNpcWithdrawn, acceptNpcRefused, acceptNoNpcPay, acceptPaidOnce,
                acceptReplayPaysNothing, acceptSavedOnDisk, acceptReloadClean, acceptHostDone, acceptReloadPass, acceptReloadPlayable;
            public string acceptClipId = "", acceptRecordingId = "", acceptHash = "", acceptSubject = "", acceptRegion = "", acceptCell = "", acceptBand = "";
            public long acceptBytes;
            public int acceptQuality, acceptDayBefore, acceptDayAfter, acceptIncome, acceptViews, acceptFollowers, acceptBalance, acceptExploreObs;
            public string acceptReloadClipId = "", acceptReloadRecordingId = "", acceptReloadHash = "";
            public long acceptReloadBytes;
            public int acceptReloadClips, acceptReloadPublications, acceptReloadBalance, acceptReloadIncome, acceptReloadViews, acceptReloadFollowers, acceptReloadDay, acceptReloadExploreObs;
            public bool fleetMotorSeeded, fleetReefGateRefused, fleetReefSwum, fleetReefOnDisk, fleetReloadReefKept;
            public bool fleetSeeded, fleetShopOpened, fleetTierGateRefused, fleetTiersBought, fleetVendorOpened, fleetMotorBought, fleetMotorDuplicateRefused,
                fleetResearchBought, fleetHostSeated, fleetSeatBlocked, fleetSelected, fleetMirrorOwned, fleetMirrorActive, fleetActiveSeam, fleetParkedRefused,
                fleetHostChecks, fleetSavedOnDisk, fleetReloadClean, fleetHostDone, fleetReloadPass;
            public int fleetBalance, fleetReloadBalance, fleetReloadOwned;
            public string fleetReloadActive = "", fleetReloadTripBoat = "";
            public List<string> fleetTrace = new List<string>();
            public bool livingSeeded, livingFarRefused, livingBuildOk, livingDuplicateRefused, livingRolesOk, livingMirrorDev, livingEffectsMirrored, livingHomeVisual,
                livingSold, livingTownVisuals, livingOrderMirrored, livingSpendOk, livingEffectsHost, livingOrderPaidOnce, livingSavedOnDisk, livingReloadClean,
                livingHostLobbyDone, livingHostDone, livingReloadPass;
            public int livingBalanceAfterBuild, livingFinalBalance, livingReloadBalance, livingSaleAmount, livingDay, livingReloadDay;
            public string livingOrderTemplate = "", livingReloadOrderStatus = "";
            public List<string> livingTrace = new List<string>();
            public bool deepOrderRefused, deepSightingCounted, deepEncyclopedia, deepRumor, deepFailClosed, deepContextRefused, deepTracesOk, deepTraceStage,
                deepDiscoveryRefused, deepUnlocked, deepCompletedOnce, deepSavedOnDisk, deepReloadClean, deepHostDone, deepReloadPass;
            public bool deepGuestSeqOk, deepMirrorUnlocked, deepLineShown, deepMirrorCompleted;
            public bool bossSighting, bossRumor, bossTraceStage, bossArenaUnlocked, bossActiveSeen, bossDamageSeen,
                bossShotSent, bossDefeatedPending, bossPartySafe, bossCompletedSeen, bossTwoAttackers, bossSaved, bossHostDone, bossReloadPass;
            public string deepReloadStage = "";
            public int deepReloadTraces, deepReloadCompleted;
            public List<int> deepStagesSeen = new List<int>();
            public List<string> deepTrace = new List<string>();
            public bool exploreMirrorSeen, exploreEncyclopediaSilhouette, exploreEncyclopediaNameHidden, exploreReloaded;
            public int exploreFogCells, exploreGridCells, exploreFogDiscoveredMax, exploreSavedCells, exploreSavedObservations;
            public string exploreEncyclopediaSpecies = "", exploreSightingOutcome = "", exploreSightingReplay = "";
            public bool storageCarriedSeen, storageFarRefused, storageOpenAccepted, storagePanelOpen, storageStored,
                storageDuplicateRefused, storageRetrieved, storageAllStored, storageHostChecked, storageHostFinal, storageReloadKept;
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
        private bool Record => Arg("-p3-record") == "1" || Event || Acceptance;
        // #106 acceptance: NO fixture. The real -Record capture produces the clip; the recorder does not sell it to the NPC but
        // publishes it from the real PC, the day is closed by the real sleep gate, and a second host launch reloads the campaign.
        private bool Acceptance => Arg("-p4-acceptance") == "1";
        private bool AcceptanceReload => Arg("-p4-acceptance-reload") == "1";
        private bool Town => Arg("-p3-town") == "1";
        private bool Storage => Arg("-p4-storage") == "1";
        // #109 fleet: real walking to the real equipment shop and harbor vendor, real purchase/select RPCs from two processes,
        // then a second host launch on the same campaign file. The money is a labelled seed (like SeedRecorderCamera).
        private bool Fleet => Arg("-p4-fleet") == "1";
        private bool FleetReload => Arg("-p4-fleet-reload") == "1";
        // #123 deep progression: real encyclopedia sighting + REAL swim to the reef, then the host-fed trace/discovery steps through a LABELLED
        // fixture world validator (Utku's #122 world rule does not exist yet), then a second host launch on the same campaign file.
        private bool Deep => Arg("-p4-deep") == "1";
        private bool DeepReload => Arg("-p4-deep-reload") == "1";
        // #132 living world: REAL walking to the home PC (builds + roles, requests decided by the host), a real dive and a real fish hand-in at the
        // fish buyer that completes the day's order once, then a second host launch on the same campaign file. Labelled: the start money.
        private bool Living => Arg("-p4-living") == "1";
        private bool LivingReload => Arg("-p4-living-reload") == "1";
        private bool BossAcceptance => Arg("-p4-boss-acceptance") == "1";
        private bool BossAcceptanceReload => Arg("-p4-boss-acceptance-reload") == "1";
        private bool MediaFlow => Arg("-p4-media") == "1";
        private bool MediaReload => Arg("-p4-media-reload") == "1";
        private bool Explore => Arg("-p4-explore") == "1";
        private bool Home => Arg("-p4-home") == "1";
        private bool Day => Arg("-p4-day") == "1";
        private bool DayReload => Arg("-p4-day-reload") == "1";
        private bool Trip => Arg("-p3-trip") == "1";
        private string TripVehicleId => Arg("-p4-trip-vehicle");
        private bool FleetRoute => !string.IsNullOrEmpty(TripVehicleId);
        private string TripRouteId => FleetRoute ? Arg("-p4-trip-route", BoatTripIds.NearRouteId) : BoatTripIds.NearRouteId;
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
        private float bossNextShot, bossTraceAt;
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
            var duration = LivingReload ? 40f : Living ? 230f : BossAcceptanceReload ? 50f : BossAcceptance ? 360f : DeepReload ? 40f : Deep ? 200f : FleetReload ? 40f : Fleet ? 200f : AcceptanceReload ? 40f : Acceptance ? 200f : MediaReload ? 40f : MediaFlow ? 120f : Explore ? 60f : Storage ? 100f : Home ? 96f : DayReload ? 40f : Day ? 90f : FleetRoute ? 260f : Trip ? 134f : Boat ? 78f : Town ? 76f : Event ? 114f : Record ? 100f : Hunt ? 58f : 46f;
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
                if (host && elapsed > (Home ? 40 : 19) && !prepSent && allReady && (!Home || result.dayNumbers.Contains(2)) && (!Living || result.livingHostLobbyDone))
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
                if (BossAcceptanceReload) { if (host && HostBossAcceptanceReload()) { Finish(); yield break; } yield return null; continue; }
                if (LivingReload) { if (host && HostLivingReload()) { Finish(); yield break; } yield return null; continue; }
                if (DeepReload) { if (host && HostDeepReload()) { Finish(); yield break; } yield return null; continue; }
                if (FleetReload) { if (host && HostFleetReload()) { Finish(); yield break; } yield return null; continue; }
                if (AcceptanceReload) { if (host && HostAcceptanceReload()) { Finish(); yield break; } yield return null; continue; }
                if (MediaReload) { if (host && HostMediaReload()) { Finish(); yield break; } yield return null; continue; }
                if (DayReload) { if (host && HostDayReload()) { Finish(); yield break; } yield return null; continue; }
                if (Day || Home) ObserveDay();
                if (Explore) { if (host) HostExplore(state); ObserveExplore(); }
                if (MediaFlow && host) { HostSubmitMediaClips(state); HostMediaChecks(); }
                if (Acceptance && host) HostAcceptance(state);
                if (Fleet && host) HostFleet(state);
                if (Deep) { ObserveDeep(); if (host) HostDeep(state); }
                if (Living) { ObserveLiving(); if (host) HostLiving(state); }
                if (BossAcceptance) ObserveBossAcceptance();
                if (host && Storage) { HostInjectStorageCatches(state); HostStorageChecks(); }
                if (host && Day) HostDay(state);
                if (host && Town) HostTown(state);
                if (host && Record && !Town) SeedRecorderCamera(state);
                if (host && Boat) HostSampleBoatRepair();
                if (host && Record && (state.Phase == SessionPhase.Return || result.returned))
                    result.recordingPaid |= adapter.GetComponent<EconomyManager>().SharedBalance > 0;
                // Recording scenarios end the dive when the recorder has REALLY swum to the safe pad (an event), not at a fixed
                // second: how long the climb takes depends on machine load, and a fixed budget made -Record flaky (3 of 4
                // runs failed on an idle-looking machine even at the P3 close commit). The ceiling still bounds a real failure.
                var recordingSettled = (!Record || MediaFlow || result.recordingSafe) && ((!Fleet && !Deep) || (result.fleetReefSwum && Time.realtimeSinceStartup - fleetBackAt > 6f));
                var returnAfter = MediaFlow ? 34 : Explore ? 36 : Storage ? 40 : Home ? 72 : Day ? 999 : Town ? 34 : Event ? 92 : Boat ? 58 : Hunt || Record ? 44 : 33;
                var returnCeiling = Fleet || Deep || Living ? 100f : Record && !MediaFlow ? (Event ? 140f : 70f) : returnAfter;
                if (host && BossAcceptance && result.bossDefeatedPending && result.bossTwoAttackers && result.bossPartySafe &&
                    !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                else if (!BossAcceptance && host && ((elapsed > returnAfter && recordingSettled) || elapsed > returnCeiling) && !returnSent && state.Phase == SessionPhase.Dive && !connection.IsSceneLoading)
                { returnSent = true; GameObject.Find("BeginReturnButton").GetComponent<Button>().onClick.Invoke(); }
                // Plain -Record keeps the fixed P3 schedule relative to when Return REALLY began (20 s to walk to the buyer and hand in, +4 s to leave),
                // because the dive now ends when the recorder is safe, not at a fixed second.
                var recordRelative = Record && !MediaFlow && !Event && !Acceptance && returnPhaseAt > 0;
                var lobbyDue = Living ? result.livingHostDone && Time.realtimeSinceStartup - livingDoneAt > 3f : BossAcceptance ? result.bossHostDone && returnPhaseAt > 0 && Time.realtimeSinceStartup - returnPhaseAt > 3f : Deep ? result.deepHostDone && Time.realtimeSinceStartup - deepDoneAt > 3f : Fleet ? result.fleetHostDone && Time.realtimeSinceStartup - fleetDoneAt > 3f : Acceptance ? AcceptanceLobbyReady() : FleetRoute ? result.tripDone && result.tripSaveReload : recordRelative ? Time.realtimeSinceStartup - returnPhaseAt > 20f : elapsed > (MediaFlow ? 44 : Explore ? 48 : Storage ? 52 : Home ? 82 : Day ? 72 : Town ? 64 : Event ? 104 : Trip ? 124 : Boat ? 66 : Record ? 56 : Hunt ? 48 : 36);
                if (host && lobbyDue && !lobbySent && state.Phase == SessionPhase.Return && !connection.IsSceneLoading)
                { lobbySent = true; GameObject.Find("CompleteReturnButton").GetComponent<Button>().onClick.Invoke(); }
                if (result.dive && state.Phase == SessionPhase.Lobby && state.Revision >= 4 && !connection.IsSceneLoading)
                {
                    result.returned = SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene;
                    result.readyReset |= adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(value => !value) &&
                        string.IsNullOrEmpty(state.DiveId);
                }
                var leaveDue = Living ? (result.livingHostDone && Time.realtimeSinceStartup - livingDoneAt > 10f) || elapsed > 215f : BossAcceptance ? result.returned : Deep ? (result.deepHostDone && Time.realtimeSinceStartup - deepDoneAt > 10f) || elapsed > 190f : Fleet ? (result.fleetHostDone && Time.realtimeSinceStartup - fleetDoneAt > 10f) || elapsed > 190f : Acceptance ? (result.acceptHostDone && Time.realtimeSinceStartup - acceptDoneAt > 8f) || elapsed > 190f : FleetRoute ? result.returned : recordRelative ? lobbySent && Time.realtimeSinceStartup - returnPhaseAt > 24f : elapsed > (MediaFlow ? 114 : Explore ? 54 : Storage ? 94 : Home ? 86 : Day ? 76 : Town ? 68 : Event ? 108 : Trip ? 128 : Boat ? 70 : Record ? 60 : Hunt ? 54 : 41);
                if (host && leaveDue && !leaveSent) { leaveSent = true; adapter.LeaveRoom(); }
                if (result.returned && connection.Status == ConnectionStatus.Offline)
                    result.stopped = adapter.Session.Roster.Count == 0 && connection.Players.Count == 0;
                // Boss acceptance can finish as soon as the real session has returned and fully
                // shut down. The longer ceiling is only a failure bound, not a mandatory wait.
                if (BossAcceptance && result.stopped) break;
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
                (!host || (result.recordingClaimed && result.recordingViews && (Acceptance || result.recordingPaid) && result.recordingSafe));
            if (Acceptance) result.passed &= result.acceptClipReal && result.acceptWorldContext && result.acceptPanelOpen && result.acceptResultSeen &&
                (!result.acceptOwner || (result.acceptFarRefused && result.acceptPublished && result.acceptDuplicateRefused)) &&
                (result.acceptOwner || result.acceptNotOwnerRefused) &&
                (!host || (result.acceptPlayable && result.acceptNpcQueuedBefore && result.acceptNpcWithdrawn && result.acceptNpcRefused && result.acceptNoNpcPay &&
                    result.acceptPaidOnce && result.acceptReplayPaysNothing && result.acceptSavedOnDisk && result.acceptReloadClean && result.acceptHostDone));
            if (Fleet) result.passed &= result.fleetShopOpened && result.fleetTierGateRefused && result.fleetTiersBought && result.fleetVendorOpened &&
                result.fleetMirrorOwned && result.fleetMirrorActive && result.fleetActiveSeam &&
                (!host || (result.fleetSeeded && result.fleetMotorSeeded && result.fleetReefGateRefused && result.fleetReefSwum && result.fleetReefOnDisk && result.fleetHostSeated && result.fleetParkedRefused && result.fleetHostChecks &&
                    result.fleetSavedOnDisk && result.fleetReloadClean && result.fleetHostDone)) &&
                (host || (result.fleetMotorDuplicateRefused && result.fleetResearchBought && result.fleetSeatBlocked && result.fleetSelected));
            if (Deep) result.passed &= result.deepGuestSeqOk && result.deepMirrorUnlocked && result.deepLineShown && result.deepMirrorCompleted &&
                (!host || (result.deepOrderRefused && result.deepSightingCounted && result.deepEncyclopedia && result.fleetReefSwum && result.deepRumor &&
                    result.deepFailClosed && result.deepContextRefused && result.deepTracesOk && result.deepTraceStage && result.deepDiscoveryRefused &&
                    result.deepUnlocked && result.deepCompletedOnce && result.deepSavedOnDisk && result.deepReloadClean && result.deepHostDone));
            if (BossAcceptance) result.passed &= result.bossRumor && result.bossTraceStage && result.bossArenaUnlocked &&
                result.bossActiveSeen && result.bossShotSent && result.bossDefeatedPending && result.bossCompletedSeen &&
                (!host || (result.bossSighting && result.bossDamageSeen && result.bossTwoAttackers && result.bossPartySafe &&
                    result.bossSaved && result.bossHostDone));
            if (Living) result.passed &= result.livingFarRefused && result.livingBuildOk && (host || result.livingDuplicateRefused) && result.livingRolesOk && result.livingMirrorDev &&
                result.livingEffectsMirrored && result.livingHomeVisual && result.livingSold && result.livingTownVisuals && result.livingOrderMirrored &&
                (!host || (result.livingSeeded && result.livingSpendOk && result.livingEffectsHost && result.livingOrderPaidOnce && result.livingSavedOnDisk &&
                    result.livingReloadClean && result.livingHostDone));
            if (Event) result.passed &= result.eventOpened && result.eventClosed && (!host || (result.tubePurchased && result.saveLoaded));
            if (Town) result.passed &= result.townSold && result.townDenied && result.townShopOpened && result.townCameraBought &&
                result.townProgress && (!host || (result.townNoAutoPay && result.townPartBought && result.townHostChecks && result.townSaveRoundTrip));
            if (Boat && !FleetRoute) result.passed &= result.boatRepaired && result.boatPartsHidden &&
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
            if (MediaFlow) result.passed &= result.mediaArchiveSeen >= 2 * expected && result.mediaFarRefused && result.mediaPanelOpen &&
                result.mediaNotOwnerRefused && result.mediaNotPublishableRefused && result.mediaPublished && result.mediaDuplicateRefused &&
                result.mediaFeedBoth && result.mediaResultSeen &&
                (!host || (result.mediaSubmitted == 2 * expected && result.mediaRetryHeld && result.mediaNpcWithdrawn && result.mediaPaidOnce &&
                    result.mediaReplayPaysNothing && result.mediaSavedOnDisk && result.mediaReloadClean));
            // P4.3 (#108): the cell count is the region's own grid (Columns x Rows), not a number - the region grew.
            if (Explore) result.passed &= result.exploreMirrorSeen && result.exploreGridCells > 0 &&
                result.exploreFogCells == result.exploreGridCells && result.exploreFogDiscoveredMax >= 1 &&
                result.exploreEncyclopediaSpecies == "sea_bass" && result.exploreEncyclopediaSilhouette && result.exploreEncyclopediaNameHidden &&
                (!host || (result.exploreSightingOutcome == "CountedNewEvidence" && result.exploreSightingReplay == "AlreadyCounted" &&
                    result.exploreSavedCells >= 1 && result.exploreSavedObservations == 1 && result.exploreReloaded));
            if (Home) result.passed &= result.homeBedAccepted && result.homeMorningClean &&
                result.dayNumbers.Contains(2) && result.daySummaries.Contains(1) && result.homePingSeen && result.homePingOnMap &&
                (!host || result.homePingAccepted);
            if (Storage) result.passed &= result.storageCarriedSeen && result.storageOpenAccepted && result.storagePanelOpen &&
                result.storageStored && result.storageDuplicateRefused && result.storageRetrieved && result.storageAllStored &&
                (!host || (result.storageHostChecked && result.storageHostFinal && result.storageReloadKept));
            if (Trip) result.passed &= result.tripBoarded && result.tripDuplicateBoardHeld && result.tripMapDockedAtDock &&
                result.tripMapUnderwayMoved && result.tripMapAnchoredAtAnchor && result.tripDockedEmpty && result.tripDone &&
                result.tripMapPlayersMax >= expected &&
                result.tripPhases.SequenceEqual(new[] { "Docked", "Outbound", "Anchored", "Inbound", "Docked" }) &&
                (host || (result.tripReturnMarkerSeen && result.tripReboarded)) && (!host || result.tripSaveReload) &&
                (!FleetRoute || (result.tripFleetVehicleReady && result.tripHullKindCorrect && result.boatRepaired && result.boatPartsHidden));
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
                else if (MediaFlow && result.dive && scene == SessionNetworkAdapter.PrepScene && phase == SessionPhase.Lobby && mediaStage < 90) ProbeMedia(local);
                else if (Acceptance && result.dive && scene == SessionNetworkAdapter.PrepScene && phase == SessionPhase.Lobby && acceptStage < 90) ProbeAcceptance(local);
                else if (Storage && result.dive && scene == SessionNetworkAdapter.PrepScene && phase == SessionPhase.Lobby && storageStage < 90) ProbeStorage(local);
                else if (Home && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeHomePing(local);
                else if (BossAcceptance && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeBossAcceptance(local);
                else if ((Fleet || Deep) && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Dive && elapsed > 3) ProbeFleetDive(local);
                else if (Living && !result.dive && scene == SessionNetworkAdapter.PrepScene && phase == SessionPhase.Lobby && livingStage < 90) ProbeLivingLobby(local);
                else if (Living && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Return) ProbeLivingTown(local);
                else if (Fleet && scene == SessionNetworkAdapter.DiveScene && phase == SessionPhase.Return) ProbeFleet(local);
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
            // Where the recorder is while it climbs to the safe pad, every 2 s: a -Record run that never gets safe is diagnosed
            // from this trace instead of guessed at.
            if (Time.realtimeSinceStartup >= townTraceAt && result.townTrace.Count < 60)
            {
                townTraceAt = Time.realtimeSinceStartup + 2f;
                result.townTrace.Add($"rec t={Time.realtimeSinceStartup - sceneStarted:F0} pos={local.transform.position} stopped={result.recordingStopped} swimming={local.Swimming.Value}");
            }
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
            // "Over the ledge" needs BOTH axes. A recorder measured stuck for 15+ s at (7.57, 6.32, -6.59): east of the ledge's west
            // edge but NORTH of its z range (-11..-7), so the old x-only test sent it at the ledge centre and it pressed against the
            // ledge's north face. Until it is over the ledge in z as well it first swims WEST along its own z, clear of the ledge,
            // and only then turns into the ramp lane (the existing paced climb below).
            var overLedgeZ = position.z >= ledgeCollider.bounds.min.z - 0.2f && position.z <= ledgeCollider.bounds.max.z + 0.2f;
            if (position.x >= ledgeWestEdge - 0.2f)
            {
                if (overLedgeZ) return ledgeCollider.bounds.center;   // already over solid ground
                return new Vector3(ledgeWestEdge - 1.5f, position.y, position.z);
            }

            var laneZ = ramp.transform.position.z;

            // The ramp is a solid slab: entered from its SIDE at x near the ledge the diver's body (feet 6.3 .. head 8.1) is level with the
            // slab and just pushes against its north face (measured: stuck at (6.7, 6.4, -6.6) for 20+ s). It has to enter at the FOOT
            // and climb east. So: (1) off the lane, swim west along its own side, clear of the slab, to beyond the foot;
            // (2) slide into the lane there, in open water; (3) only then the paced climb below.
            var rampBounds = rampCollider.bounds;
            var footX = rampBounds.min.x;
            var inLane = position.z <= rampBounds.max.z - 0.3f && position.z >= rampBounds.min.z + 0.3f;
            // How high the slab's top is just past its foot: the height the diver has to ARRIVE at, or it climbs from underneath the slab
            // (measured: entering the lane at y 1.9 it swam up under the ramp and then under the ledge at y 5.6, never on top).
            var footSurfaceY = rampBounds.min.y + 0.6f;
            if (Physics.Raycast(new Vector3(footX + 0.6f, rampBounds.max.y + 5f, laneZ), Vector3.down, out var footHit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                footHit.collider == rampCollider)
                footSurfaceY = footHit.point.y;
            if (!inLane)
            {
                if (position.x > footX - 0.5f)
                {
                    var sideZ = position.z >= rampBounds.max.z ? Mathf.Max(position.z, rampBounds.max.z + 1.0f)
                                                              : Mathf.Min(position.z, rampBounds.min.z - 1.0f);
                    return new Vector3(footX - 1.0f, position.y, sideZ);
                }
                return new Vector3(footX - 1.0f, footSurfaceY + 0.2f, laneZ);
            }

            var aheadX = Mathf.Min(position.x + 1.2f, ledgeWestEdge);
            var probeOrigin = new Vector3(aheadX, rampCollider.bounds.max.y + 5f, laneZ);
            float targetY;
            if (Physics.Raycast(probeOrigin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == rampCollider || hit.collider == ledgeCollider))
                targetY = hit.point.y - 0.05f;
            else if (position.x < footX + 0.3f)
                targetY = footSurfaceY + 0.1f;   // west of the foot: arrive at the slab's height and push east onto it
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
            // A fleet-route run loads a previously repaired/purchased campaign. Keep verifying
            // replicated repair/hidden parts, but do not attempt to collect already consumed parts.
            if (FleetRoute) { local.SubmitLocalInput(Vector3.zero, 0f); return; }
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

        // ---- P4.1 shared home storage: real safe-return catches, real walk/aim/H, real store/retrieve requests ------------
        // The catches enter the way the town smoke does (inventory add + safe-return mark, then the real dive summary
        // queues them as unpaid). Back home each process walks to the storage, aims, presses H (Mehmet's physical open),
        // and uses the panel's own request path. Checked: a request from too far away is refused by the HOST, storing
        // moves an item, a repeat is refused, retrieving puts it back, and everything left stored survives the save.
        private int storageStage;
        private float storageStageAt, storageAimAt, storageNext;
        private ulong storageLastRequest;
        private string storageFirstId = "";
        private bool storageInjected;

        private void HostInjectStorageCatches(SessionState state)
        {
            if (storageInjected || state.Phase != SessionPhase.Dive || adapter.Connection.IsSceneLoading ||
                SceneManager.GetActiveScene().name != SessionNetworkAdapter.DiveScene || Time.realtimeSinceStartup - sceneStarted < 6f) return;
            storageInjected = true;
            var inventory = adapter.GetComponent<InventoryManager>();
            foreach (var id in adapter.Session.Roster.Keys)
            {
                for (var i = 0; i < 2; i++)
                    inventory.TryAddCatch(id, new CaptureResult($"store-{id.Value}-{i}", state.DiveId, "sea_bass", 800, 1));
                inventory.TryMarkSafeReturn(id);
            }
        }

        private bool StorageAnswered(EconomyPlayerSync sync, out bool accepted, out string reason)
        {
            accepted = sync.LastAccepted.Value;
            reason = sync.LastReasonCode.Value.ToString();
            return sync.LastRequestId.Value == storageLastRequest && storageLastRequest != 0;
        }

        private void ProbeStorage(NetworkPlayer local)
        {
            var binding = FindFirstObjectByType<HomePlayerInteractionBinding>();
            var cam = local.GetComponentInChildren<Camera>(true);
            var sync = local.GetComponent<EconomyPlayerSync>();
            var now = Time.realtimeSinceStartup;
            if (storageStageAt == 0) storageStageAt = now;
            if (binding == null || cam == null || sync == null || !sync.IsSpawned) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            if (storageStage < 90 && now - storageStageAt > 45f)
            {
                result.errors.Add($"storage stage {storageStage} timeout carried={sync.CarriedCatchIds.Count} stored={sync.StoredCatchIdList.Count} status='{HomeStatus(binding)}' last={sync.LastReasonCode.Value}");
                storageStage = 99;
            }
            var anchor = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None).FirstOrDefault(a => a.Kind == HomeInteractionKind.Storage);
            if (anchor == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var far = Vector3.Distance(local.transform.position, anchor.WorldPosition) > HomePlayerInteractionBinding.InteractionRange + 1.5f;

            switch (storageStage)
            {
                case 0:   // the panel offers this player's own two catches once the day's dive summary was queued
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.CarriedCatchIds.Count < 2) return;
                    result.storageCarriedSeen = true;
                    storageFirstId = sync.CarriedCatchIds[0].ToString();
                    if (far)
                    {
                        // Too far from the storage: the HOST must refuse, whatever the client shows.
                        sync.RequestStoreItem(storageFirstId);
                        storageLastRequest = sync.LastRequestId.Value + 1;
                        storageNext = now + 1.2f;
                        storageStage = 1; return;
                    }
                    storageStage = 2; return;
                case 1:
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < storageNext) return;
                    result.storageFarRefused = !sync.LastAccepted.Value && sync.LastReasonCode.Value.ToString() == "NotAtStorage" &&
                        sync.CarriedCatchIds.Count == 2;
                    if (!result.storageFarRefused) result.errors.Add($"far store not refused: accepted={sync.LastAccepted.Value} reason={sync.LastReasonCode.Value}");
                    storageStage = 2; return;
                }
                case 2:   // walk to the storage
                {
                    var to = anchor.WorldPosition - local.transform.position; to.y = 0;
                    if (to.magnitude > 1.9f)
                    {
                        var yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                        local.SubmitLocalInput(Vector3.forward, yaw, 0);
                        return;
                    }
                    storageAimAt = 0; storageStage = 3; return;
                }
                case 3:   // aim, press H: Mehmet's physical layer opens it
                {
                    var col = anchor.GetComponentInChildren<Collider>();
                    AimAt(local, cam, col != null ? col.bounds.center : anchor.WorldPosition, out var yaw, out var pitch);
                    local.SubmitLocalInput(Vector3.zero, yaw, pitch);
                    if (storageAimAt == 0) storageAimAt = now + 0.7f;
                    if (now < storageAimAt) return;
                    HomeSend(binding, 1);
                    storageNext = now + 1.0f; storageStage = 4; return;
                }
                case 4:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < storageNext) return;
                    result.storageOpenAccepted = HomeStatus(binding) == "EV ETKILESIMI TAMAM";
                    result.storagePanelOpen = HomeStorageView.PanelOpen;
                    if (!result.storageOpenAccepted) result.errors.Add("storage open refused: " + HomeStatus(binding));
                    storageStage = 5; return;
                case 5:   // store one
                    local.SubmitLocalInput(Vector3.zero, 0);
                    sync.RequestStoreItem(storageFirstId);
                    storageLastRequest = sync.LastRequestId.Value + 1;
                    storageNext = now + 1.2f; storageStage = 6; return;
                case 6:
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < storageNext) return;
                    var moved = sync.LastAccepted.Value && sync.StoredCatchIdList.Count >= 1 && sync.CarriedCatchIds.Count == 1;
                    result.storageStored = moved;
                    if (!moved) result.errors.Add($"store failed accepted={sync.LastAccepted.Value} reason={sync.LastReasonCode.Value} carried={sync.CarriedCatchIds.Count} stored={sync.StoredCatchIdList.Count}");
                    storageStage = 7; return;
                }
                case 7:   // the same item again: it is already in storage, so it is not offered and not accepted
                    local.SubmitLocalInput(Vector3.zero, 0);
                    sync.RequestStoreItem(storageFirstId);
                    storageLastRequest = sync.LastRequestId.Value + 1;
                    storageNext = now + 1.2f; storageStage = 8; return;
                case 8:
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < storageNext) return;
                    result.storageDuplicateRefused = !sync.LastAccepted.Value && sync.LastReasonCode.Value.ToString() == "InvalidTarget" &&
                        sync.CarriedCatchIds.Count == 1;
                    if (!result.storageDuplicateRefused) result.errors.Add($"duplicate store not refused accepted={sync.LastAccepted.Value} reason={sync.LastReasonCode.Value}");
                    storageStage = 9; return;
                }
                case 9:   // take it back out
                    local.SubmitLocalInput(Vector3.zero, 0);
                    sync.RequestRetrieveItem(storageFirstId);
                    storageLastRequest = sync.LastRequestId.Value + 1;
                    storageNext = now + 1.2f; storageStage = 10; return;
                case 10:
                {
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (now < storageNext) return;
                    result.storageRetrieved = sync.LastAccepted.Value && sync.CarriedCatchIds.Count == 2 && !ContainsId(sync.StoredCatchIdList, storageFirstId);
                    if (!result.storageRetrieved) result.errors.Add($"retrieve failed accepted={sync.LastAccepted.Value} reason={sync.LastReasonCode.Value} carried={sync.CarriedCatchIds.Count}");
                    storageStage = 11; return;
                }
                case 11:  // leave both stored: this is what the save must keep
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (sync.CarriedCatchIds.Count > 0)
                    {
                        if (now >= storageNext)
                        {
                            storageNext = now + 0.8f;
                            sync.RequestStoreItem(sync.CarriedCatchIds[0].ToString());
                        }
                        return;
                    }
                    result.storageAllStored = true;
                    storageStage = 90; return;
                default:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
            }
        }

        private static bool ContainsId(Unity.Netcode.NetworkList<Unity.Collections.FixedString64Bytes> list, string id)
        {
            for (var i = 0; i < list.Count; i++) if (list[i].ToString() == id) return true;
            return false;
        }

        // Host: once every process finished storing, the authority, the mirror count and the file must agree.
        private void HostStorageChecks()
        {
            if (result.storageHostChecked || !result.storageAllStored) return;
            var economy = adapter.GetComponent<EconomyManager>();
            var want = adapter.Session.Roster.Count * 2;
            if (economy.StoredCount < want) return;
            var store = adapter.GetComponent<EconomySaveStore>();
            var onDisk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
            result.storageHostChecked = true;
            result.storageHostFinal = economy.StoredCount == want && economy.PendingTurnIns().Count == 0 &&
                onDisk.StoredItems.Count == want && onDisk.PendingTurnIns.Count == 0;
            var before = economy.StoredCount;
            result.storageReloadKept = store.LoadNow() && economy.StoredCount == before && economy.PendingTurnIns().Count == 0;
            if (!result.storageHostFinal) result.errors.Add($"storage host final stored={economy.StoredCount} pending={economy.PendingTurnIns().Count} disk={onDisk.StoredItems.Count}/{onDisk.PendingTurnIns.Count}");
        }

        // ---- P4.1 exploration map/encyclopedia/save (#90) over the network -------------------------------------------
        // TEST FIXTURE, NOT PRODUCT WIRING: the host shell that owns Utku's authorities in the game is Mehmet's
        // composition binding (#89 follow-up), which does not exist yet. So that the parts that ARE product code -
        // ExplorationMirror, the map fog / encyclopedia presenters, ExplorationPersistenceAdapter and the campaign
        // save - can be proven across real processes now, the host here builds Utku's real authorities from the
        // real scene (DiveRegionField bounds, WaterField bodies), feeds them the host-authoritative NetworkPlayer
        // positions every tick, binds ExplorationFeed and the save, and accepts ONE sighting at the host's own
        // position. Nothing of this ships; it lives in the smoke driver only.
        private ExplorationCellAuthority exploreCells;
        private SpeciesObservationAuthority exploreSpecies;
        private bool exploreSighted, exploreSaveChecked;

        private sealed class NetworkPlayerExplorers : IExplorerPositionSource
        {
            public void CollectPositions(List<ExplorerPosition> into)
            {
                into.Clear();
                foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                    if (p.IsSpawned) into.Add(new ExplorerPosition(new PlayerId(p.OwnerClientId), p.transform.position));
            }
        }

        private readonly NetworkPlayerExplorers exploreFeed = new NetworkPlayerExplorers();

        private void HostExplore(SessionState state)
        {
            if (exploreCells == null)
            {
                if (SceneManager.GetActiveScene().name != SessionNetworkAdapter.DiveScene || adapter.Connection.IsSceneLoading) return;
                if (!DiveRegionField.TryFind(out var region)) return;
                var water = FindFirstObjectByType<WaterField>();
                exploreCells = new ExplorationCellAuthority(region.RegionId, region.Bounds, water != null ? water.Bodies : null);
                exploreSpecies = new SpeciesObservationAuthority(exploreCells);
                ExplorationFeed.Bind(exploreSpecies);
                adapter.GetComponent<EconomySaveStore>().Exploration = new ExplorationPersistenceAdapter(exploreCells, exploreSpecies);
            }

            if (SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene && !adapter.Connection.IsSceneLoading)
                exploreCells.Tick(exploreFeed);

            if (!exploreSighted && state.Phase == SessionPhase.Dive && Time.realtimeSinceStartup - sceneStarted > 8f)
            {
                var me = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
                if (me != null)
                {
                    exploreSighted = true;
                    result.exploreSightingOutcome = exploreSpecies.AcceptSighting("sea_bass", me.transform.position, 1).ToString();
                    // The same sighting again is not a second observation.
                    result.exploreSightingReplay = exploreSpecies.AcceptSighting("sea_bass", me.transform.position, 1).ToString();
                }
            }

            if (!exploreSaveChecked && state.Phase == SessionPhase.Return && exploreSighted)
            {
                exploreSaveChecked = true;
                var store = adapter.GetComponent<EconomySaveStore>();
                var saved = store.SaveNow();
                var onDisk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                result.exploreSavedCells = onDisk.Exploration.DiscoveredCells.Count;
                result.exploreSavedObservations = onDisk.Exploration.Observations.Count;

                // A fresh authority pair, re-hydrated from that file through the product adapter.
                var fresh = new ExplorationCellAuthority(exploreCells.Grid.RegionId, DiveRegionField.TryFind(out var r) ? r.Bounds : default,
                    FindFirstObjectByType<WaterField>()?.Bodies);
                var freshSpecies = new SpeciesObservationAuthority(fresh);
                var freshAdapter = new ExplorationPersistenceAdapter(fresh, freshSpecies);
                result.exploreReloaded = saved && freshAdapter.RestoreExploration(onDisk.Exploration) &&
                    freshSpecies.Observations.Count == result.exploreSavedObservations &&
                    freshAdapter.ExportExploration().DiscoveredCells.Count == result.exploreSavedCells;
            }
        }

        // Every process: what its OWN map and encyclopedia show (host: live model; guest: mirrored from the host).
        private void ObserveExplore()
        {
            if (!ExplorationMirror.HasData) return;
            result.exploreMirrorSeen = true;
            var fog = BoatMapView.LastFog;
            result.exploreFogCells = fog.Count;
            // Expected tiles = the scene region's grid, Columns x Rows, measured on this process's own scene.
            if (DiveRegionField.TryFind(out var region))
            {
                var grid = new ExplorationCellLayout(region.RegionId, region.Bounds).Grid;
                result.exploreGridCells = grid.Columns * grid.Rows;
            }
            result.exploreFogDiscoveredMax = Math.Max(result.exploreFogDiscoveredMax, ExplorationMapPresenter.DiscoveredCount(fog));
            var encyclopedia = BoatMapView.LastEncyclopedia;
            if (encyclopedia.Count > 0)
            {
                result.exploreEncyclopediaSpecies = encyclopedia[0].SpeciesId;
                result.exploreEncyclopediaSilhouette = encyclopedia[0].Silhouette;
                result.exploreEncyclopediaNameHidden = !encyclopedia[0].NameKnown;
            }
        }

        // ---- P4.2 home PC + channel (#102) over the network ----------------------------------------------------------
        // TEST FIXTURE, NOT PRODUCT WIRING: real clips come from Mehmet's capture (#100), which does not exist yet.
        // So that the archive, the PC publish path, the single commercial right, the day-close result and the save
        // (all product code) can be proven across real processes now, the host here submits clip manifests through
        // the SAME ClipArchive seam #100 will call, and later lets the day close the way midnight would. Every
        // publish below is a real request from each process's own player standing (or not) at the real home PC.
        private int mediaStage;
        private float mediaStageAt, mediaNext;
        private ulong mediaAwait;
        private bool mediaSubmitted, mediaHostClosed;

        private void HostSubmitMediaClips(SessionState state)
        {
            if (mediaSubmitted || state.Phase != SessionPhase.Return || adapter.Connection.IsSceneLoading) return;
            var day = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (day == null) return;
            mediaSubmitted = true;
            foreach (var id in adapter.Session.Roster.Keys)
            {
                var owner = new PlayerId(id.Value);
                result.mediaSubmitted += ClipArchive.TrySubmit(new ClipManifest($"clip-{id.Value}-a", $"rec-media-{id.Value}", state.DiveId,
                    day.DayNumber, owner, "sea_bass", 3, 14f, $"hash-{id.Value}-a", 900000, true, true)) == ClipArchiveOutcome.Added ? 1 : 0;
                result.mediaSubmitted += ClipArchive.TrySubmit(new ClipManifest($"clip-{id.Value}-b", "", state.DiveId,
                    day.DayNumber, owner, "", 0, 6f, $"hash-{id.Value}-b", 300000, true, true)) == ClipArchiveOutcome.Added ? 1 : 0;
            }
            // The host's commercial clip is ALSO an NPC candidate: publishing must withdraw it from the NPC.
            adapter.GetComponent<EconomyManager>().TryQueueRecordingTurnIn(
                new RecordingResult("rec-media-0", state.DiveId, new PlayerId(0), "sea_bass", 3, 14f));
            // A capture retry of the same clip is not a second clip.
            result.mediaRetryHeld = ClipArchive.TrySubmit(new ClipManifest("clip-0-a", "rec-media-0", state.DiveId, day.DayNumber,
                new PlayerId(0), "sea_bass", 3, 14f, "hash-0-a", 900000, true, true)) == ClipArchiveOutcome.AlreadyArchived;
        }

        private bool MediaAnswered(out bool accepted, out string reason)
        {
            accepted = MediaNetworkBinding.LastResultAccepted;
            reason = MediaNetworkBinding.LastResultReason;
            var answered = mediaAwait != 0 && MediaNetworkBinding.LastResultRequest == mediaAwait;
            if (answered) result.mediaTrace.Add($"stage{mediaStage}:{(accepted ? "ok" : reason)}");
            return answered;
        }

        private void MediaAsk(string clipId, string title, int next)
        {
            mediaAwait = MediaNetworkBinding.RequestPublish(clipId, title);
            mediaStage = next;
        }

        private void ProbeMedia(NetworkPlayer local)
        {
            local.SubmitLocalInput(Vector3.zero, 0);
            var now = Time.realtimeSinceStartup;
            if (mediaStageAt == 0) mediaStageAt = now;
            if (mediaStage < 90 && now - mediaStageAt > 45f)
            {
                result.errors.Add($"media stage {mediaStage} timeout clips={MediaNetworkBinding.Mirrored.Clips.Count} pubs={MediaNetworkBinding.Mirrored.Publications.Count} last={MediaNetworkBinding.LastResultReason}");
                mediaStage = 99;
            }
            var me = local.OwnerClientId;
            // The other player is whoever else owns a clip: a rejoining guest gets a NEW client id, never assume 1.
            var otherClip = MediaNetworkBinding.Mirrored.Clips.Find(c => c.OwnerPlayerId != me);
            var other = otherClip != null ? otherClip.OwnerPlayerId : ulong.MaxValue;
            var data = MediaNetworkBinding.Mirrored;
            var pc = FindFirstObjectByType<HomePcAnchor>();
            bool ok; string reason;

            switch (mediaStage)
            {
                case 0:   // my two clips (and the other player's) arrived in my own mirror
                    if (data.Clips.Count < 4 || pc == null) return;
                    result.mediaArchiveSeen = data.Clips.Count;
                    {
                        // Make sure we really ARE far first (a spawn point can be near the PC): step away, then ask.
                        var away = local.transform.position - pc.transform.position; away.y = 0;
                        if (away.magnitude < 6f)
                        {
                            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                            local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg, 0);
                            return;
                        }
                    }
                    MediaAsk($"clip-{me}-a", "uzaktan", 1);   // not at the PC: the HOST must refuse
                    return;
                case 1:
                    if (!MediaAnswered(out ok, out reason)) return;
                    result.mediaFarRefused = !ok && reason == "NotAtPc";
                    mediaStage = 2; return;
                case 2:   // walk to the PC
                {
                    var to = pc.transform.position - local.transform.position; to.y = 0;
                    if (to.magnitude > 1.9f)
                    {
                        local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 0);
                        return;
                    }
                    result.mediaPanelOpen = HomePcView.PanelOpen;
                    MediaAsk($"clip-{other}-a", "baskasinin", 3);
                    return;
                }
                case 3:
                    if (!MediaAnswered(out ok, out reason)) return;
                    result.mediaNotOwnerRefused = !ok && reason == "NotOwner";
                    MediaAsk($"clip-{me}-b", "bos", 4); return;
                case 4:
                    if (!MediaAnswered(out ok, out reason)) return;
                    result.mediaNotPublishableRefused = !ok && reason == "NotPublishable";
                    MediaAsk($"clip-{me}-a", $"Levrek P{me}", 5); return;
                case 5:
                    if (!MediaAnswered(out ok, out reason)) return;
                    result.mediaPublished = ok;
                    if (!ok) result.errors.Add("publish refused: " + reason);
                    MediaAsk($"clip-{me}-a", "tekrar", 6); return;
                case 6:
                    if (!MediaAnswered(out ok, out reason)) return;
                    result.mediaDuplicateRefused = !ok && reason == "PublicationAlreadyQueued";
                    mediaStage = 7; return;
                case 7:   // both players' posts in MY feed
                    if (data.Publications.Count < 2) return;
                    result.mediaFeedBoth = data.Publications.Exists(p => p.OwnerPlayerId == me) && data.Publications.Exists(p => p.OwnerPlayerId == other);
                    mediaStage = 8; return;
                case 8:   // after the day closes, my post shows its result in MY feed
                {
                    var mine = data.Publications.Find(p => p.OwnerPlayerId == me);
                    if (mine == null || string.IsNullOrEmpty(mine.SettledId)) return;
                    result.mediaResultSeen = mine.Views > 0 && mine.Income > 0;
                    result.mediaFollowersSeen = data.Followers;
                    mediaStage = 90; return;
                }
            }
        }

        // Host: once both posts exist, the NPC can no longer pay the host's published recording; then the day closes
        // (fixture: midnight arrives) and the results must be paid exactly once, into the same file, and survive a load.
        // Second launch of the host on the SAME campaign file: archive, posts, results, followers and the NPC/channel
        // right must come back as they were, and nothing may be paid or published again.
        private bool HostMediaReload()
        {
            var binding = adapter.GetComponent<MediaNetworkBinding>();
            var economy = adapter.GetComponent<EconomyManager>();
            if (binding == null || economy == null || adapter.Connection.Status != ConnectionStatus.Connected || binding.Channel.ClipCount == 0) return false;
            var pubs = binding.Channel.Publications();
            result.mediaReloadClips = binding.Channel.ClipCount;
            result.mediaReloadPublications = pubs.Count;
            result.mediaReloadBalance = economy.SharedBalance;
            var before = economy.SharedBalance;
            result.mediaReloadPass = binding.Channel.ClipCount == 4 && pubs.Count == 2 &&
                pubs.All(p => !string.IsNullOrEmpty(p.SettledId) && p.Views > 0) &&
                binding.Channel.Followers > 0 && economy.IsChannelClaimed("rec-media-0") &&
                binding.Channel.SettleThrough(99) == 0 && economy.SharedBalance == before &&
                binding.Channel.TryPublish(new PlayerId(0), "clip-0-a", "x", 777).ReasonCode == "PublicationAlreadyQueued";
            result.passed = result.mediaReloadPass && result.errors.Count == 0;
            return true;
        }

        // ---- #124 fixture-free deep/boss acceptance ---------------------------------------------------------------
        private bool bossSightingInjected;

        private void ObserveBossAcceptance()
        {
            // The real product exploration binding may verify the sea-bass sighting before this
            // smoke driver gets its first Locked-stage tick. Observe that authoritative state
            // directly instead of requiring our fallback AcceptSighting call to be the writer.
            if (adapter.IsAuthority)
            {
                var exploration = adapter.GetComponent<ExplorationNetworkBinding>();
                if (exploration != null && exploration.Species != null &&
                    exploration.Species.TryGetSpecies("sea_bass", out var discoveredSpecies))
                    result.bossSighting |= discoveredSpecies.Sighted;
            }

            result.bossRumor |= BossProgression.State.Stage >= DeepProgressionStage.Rumor;
            result.bossTraceStage |= BossProgression.State.Stage >= DeepProgressionStage.Trace;
            result.bossArenaUnlocked |= BossProgression.State.Stage >= DeepProgressionStage.Discovery;
            var boss = BossEncounterRuntime.Snapshot;
            result.bossActiveSeen |= boss.Phase == BossEncounterPhase.Active;
            result.bossCompletedSeen |= BossProgression.IsCompleted(DeepProgressionIds.BossId) ||
                                        boss.Phase == BossEncounterPhase.Completed;
            if (boss.MaxHealth > 0f && boss.Health < boss.MaxHealth) result.bossDamageSeen = true;
            result.bossDefeatedPending |= boss.Phase == BossEncounterPhase.Active && boss.Health <= 0f;

            if (!adapter.IsAuthority || result.bossHostDone) return;
            var binding = adapter.GetComponent<BossEncounterSessionBinding>();
            result.bossTwoAttackers |= binding != null && binding.AcceptedHitPlayerCount >= 2;
            var inventory = adapter.GetComponent<InventoryManager>();
            result.bossPartySafe |= inventory != null && adapter.Session.Roster.Keys.All(id =>
                inventory.Bags.TryGetValue(id, out var bag) && bag.SafelyReturned);

            // Durable completion must appear only after BeginReturn finalized the real DiveSummary.
            if (adapter.Session.State.Phase != SessionPhase.Return || !result.bossCompletedSeen || !result.bossTwoAttackers)
                return;

            var store = adapter.GetComponent<EconomySaveStore>();
            if (store == null || !File.Exists(store.SavePath)) return;
            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
            result.bossSaved = disk.HasProgression &&
                               disk.Progression.Stage == (byte)DeepProgressionStage.Discovery &&
                               disk.Progression.TraceIds.Count == DeepProgressionIds.RequiredTraceCount &&
                               disk.Progression.CompletedBossIds.Contains(DeepProgressionIds.BossId);
            result.bossHostDone = result.bossSaved;
        }

        private void ProbeBossAcceptance(NetworkPlayer local)
        {
            var state = BossProgression.State;
            if (Time.realtimeSinceStartup >= bossTraceAt && result.deepTrace.Count < 100)
            {
                bossTraceAt = Time.realtimeSinceStartup + 3f;
                var encounter = BossEncounterRuntime.Snapshot;
                result.deepTrace.Add($"stage={state.Stage} pos={local.transform.position} hp={encounter.Health} passive={local.Passive.Value} oxygen={local.Oxygen.Value}");
            }

            // First proof is created through the real host species authority; all later steps are caused only
            // by real player movement through the real Reef/deep world volumes.
            if (state.Stage == DeepProgressionStage.Locked)
            {
                local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                if (adapter.IsAuthority && !bossSightingInjected)
                {
                    var exploration = adapter.GetComponent<ExplorationNetworkBinding>();
                    if (exploration != null && exploration.Species != null)
                    {
                        var sighting = exploration.Species.AcceptSighting("sea_bass", local.transform.position, 1);
                        result.bossSighting = sighting.ToString() == "CountedNewEvidence" || sighting.ToString() == "AlreadyCounted";
                        bossSightingInjected = result.bossSighting;
                    }
                }
                return;
            }

            if (state.Stage == DeepProgressionStage.Encyclopedia)
            {
                if (adapter.IsAuthority) SwimAcceptance(local, FleetReefTarget, 1.0f);
                else local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                return;
            }

            if (state.Stage == DeepProgressionStage.Rumor)
            {
                if (adapter.IsAuthority)
                {
                    var index = Mathf.Clamp(state.TracesFound, 0, DeepEncounterWorld.TracePositions.Length - 1);
                    SwimAcceptance(local, DeepEncounterWorld.TracePositions[index], 0.75f, surfaceTransit: true);
                }
                else local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                return;
            }

            if (state.Stage == DeepProgressionStage.Trace)
            {
                SwimAcceptance(local, BossFiringPosition(local), 0.8f, surfaceTransit: true);
                return;
            }

            if (state.Stage != DeepProgressionStage.Discovery)
            {
                local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                return;
            }

            var boss = BossEncounterRuntime.Snapshot;
            // A defeated boss stays transient until the crew reaches the real safe-return zone.
            // Both processes physically swim back; the host only advances Return after every roster bag is marked safe.
            if (boss.Phase == BossEncounterPhase.Active && boss.Health <= 0f)
            {
                result.bossDefeatedPending = true;
                ReturnBossPartyToShore(local);
                return;
            }

            // Both real processes converge on the arena, aim at the locally presented boss collider and fire
            // through NetworkPlayer's existing harpoon RPC/raycast path.
            var toArena = BossFiringPosition(local) - local.transform.position;
            if (toArena.magnitude > 1.4f)
            {
                SwimAcceptance(local, BossFiringPosition(local), 0.8f, surfaceTransit: true);
                return;
            }

            if (boss.Phase == BossEncounterPhase.Completed || BossProgression.IsCompleted(DeepProgressionIds.BossId))
            {
                result.bossCompletedSeen = true;
                local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                return;
            }

            var arenaPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None)
                .Count(player => player.IsSpawned && !player.Passive.Value &&
                                 Vector3.Distance(player.transform.position, BossFiringPosition(player)) <= 1.4f);
            if (arenaPlayers < Math.Min(2, adapter.Session.Roster.Count))
            {
                local.SubmitLocalInput(Vector3.zero, 0f, 0f);
                return;
            }

            var delta = DeepEncounterWorld.BossPosition - local.RecordingEyePosition;
            var flat = new Vector3(delta.x, 0f, delta.z);
            var yaw = flat.sqrMagnitude > 0.0001f ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : 0f;
            var pitch = -Mathf.Atan2(delta.y, Mathf.Max(0.01f, flat.magnitude)) * Mathf.Rad2Deg;
            local.SubmitLocalInput(Vector3.zero, yaw, pitch);
            if (boss.Phase == BossEncounterPhase.Active && Time.realtimeSinceStartup >= bossNextShot)
            {
                bossNextShot = Time.realtimeSinceStartup + 0.9f;
                local.SetHeldEquipmentLocal(HeldEquipmentMode.Harpoon);
                local.SubmitHarpoonLocal();
                result.bossShotSent = true;
            }
        }

        private void ReturnBossPartyToShore(NetworkPlayer local)
        {
            if (local.transform.position.z > -3f)
            {
                SwimAcceptance(local, FleetBackTarget, 0.7f, surfaceTransit: true);
                return;
            }
            var zone = FindFirstObjectByType<SafeReturnZone>();
            var box = zone != null ? zone.GetComponent<BoxCollider>() : null;
            if (box == null) { local.SubmitLocalInput(Vector3.zero, 0f); return; }
            var target = box.bounds.center - Vector3.up * 0.9f;
            target.x += local.OwnerClientId == 0 ? -0.7f : 0.7f;
            var next = NextBeachWaypoint(local.transform.position, target);
            var heading = next - local.transform.position;
            var yaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            var move = heading.magnitude > 0.4f ? Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized : Vector3.zero;
            local.SubmitLocalInput(move, yaw, 0f);
        }

        private static Vector3 BossFiringPosition(NetworkPlayer player) =>
            DeepEncounterWorld.ArenaPosition + new Vector3(player.OwnerClientId == 0 ? -1.2f : 1.2f, 0f, -1.2f);

        private static void SwimAcceptance(NetworkPlayer local, Vector3 target, float tolerance, bool surfaceTransit = false)
        {
            var current = local.transform.position;
            var horizontal = new Vector2(target.x - current.x, target.z - current.z).magnitude;
            var waypoint = target;
            if (surfaceTransit && horizontal > 2.2f)
                waypoint = new Vector3(target.x, 7f, target.z);

            var heading = waypoint - current;
            var flat = new Vector3(heading.x, 0f, heading.z);
            var yaw = flat.sqrMagnitude > 0.0001f ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : 0f;
            var move = heading.magnitude > tolerance
                ? Quaternion.Inverse(Quaternion.Euler(0f, yaw, 0f)) * heading.normalized
                : Vector3.zero;
            local.SubmitLocalInput(move, yaw, 0f);
        }

        private bool HostBossAcceptanceReload()
        {
            var store = adapter.GetComponent<EconomySaveStore>();
            if (store == null || adapter.Connection.Status != ConnectionStatus.Connected || !DeepProgressionEvidence.IsBound)
                return false;

            var state = BossProgression.State;
            if (state.Stage == DeepProgressionStage.Locked) return false;
            var boss = BossEncounterRuntime.Snapshot;
            var disk = File.Exists(store.SavePath)
                ? JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath))
                : null;
            result.bossReloadPass =
                state.Stage == DeepProgressionStage.Discovery &&
                state.TracesFound == DeepProgressionIds.RequiredTraceCount &&
                state.IsCompleted(DeepProgressionIds.BossId) &&
                boss.Phase != BossEncounterPhase.Active &&
                disk != null && disk.HasProgression &&
                disk.Progression.CompletedBossIds.Contains(DeepProgressionIds.BossId);
            result.passed = result.bossReloadPass && result.errors.Count == 0;
            return true;
        }

        // ---- #132 living world smoke ---------------------------------------------------------------------------------------
        // Lobby (before any dive): both players walk to the home PC; the far request is refused by the HOST; the guest buys the fisherman's stall, the
        // host the home level 2, the shop and the dock; a duplicate is refused; each player picks a role (the guest changes its mind once). Then a real
        // dive (the host injects two 1 kg catches per player like -Town), a real hand-in at the fish buyer (the stall pays +10%) completes the day's
        // order exactly once, the town props are really built, and a second host launch restores everything without paying or buying again.
        private int livingStage, livingHostStage;
        private float livingStageAt, livingHostAt, livingDoneAt, livingTraceAt;
        private const int LivingSeedBalance = 5000;
        private bool livingCaptured, livingTownCaptured, livingInjected;

        private static bool VisualBuilt(string id)
        {
            var root = GameObject.Find("P45_Development_" + id);
            if (root == null) return false;
            var built = root.transform.Find("Built");
            return built != null && built.gameObject.activeSelf;
        }

        // Every process: what its OWN mirror and read seams show.
        private void ObserveLiving()
        {
            if (!LivingWorldNetworkBinding.HasMirror) return;
            var board = LivingWorldNetworkBinding.Board;
            var dev = LivingWorldNetworkBinding.Development;
            result.livingDay = board.Day;
            result.livingOrderTemplate = board.Order.TemplateId;
            result.livingMirrorDev |= DevelopmentIds.All.All(dev.Owns);
            result.livingEffectsMirrored |= DevelopmentEffects.StorageBonusSlots == DevelopmentCatalog.HomeStorageBonusSlots &&
                DevelopmentEffects.FishPricePercentBonus == DevelopmentCatalog.FisherPricePercent &&
                DevelopmentEffects.VehiclePriceDiscountPercent == DevelopmentCatalog.DockVehicleDiscountPercent && DevelopmentEffects.ShopStockUnlocked;
            result.livingOrderMirrored |= result.livingSold && board.Order.HasContract && board.Order.Status == ContractStatus.Completed;
            if (SceneManager.GetActiveScene().name == SessionNetworkAdapter.PrepScene) result.livingHomeVisual |= dev.Owns(DevelopmentIds.Home2) && VisualBuilt(DevelopmentIds.Home2);
        }

        private bool LivingAnswered(ulong awaiting, out bool accepted, out string reason)
        {
            accepted = LivingWorldNetworkBinding.LastResultAccepted;
            reason = LivingWorldNetworkBinding.LastResultReason;
            var answered = awaiting != 0 && LivingWorldNetworkBinding.LastResultRequest == awaiting;
            if (answered) result.livingTrace.Add($"answer stage={livingStage} ok={accepted} reason={reason}");
            return answered;
        }

        private ulong livingAwait;

        private void LivingStep(int next) { result.livingTrace.Add($"stage {livingStage}->{next} t={Time.realtimeSinceStartup - sceneStarted:F1}"); livingStage = next; livingStageAt = Time.realtimeSinceStartup; livingAwait = 0; }

        private void ProbeLivingLobby(NetworkPlayer local)
        {
            local.SubmitLocalInput(Vector3.zero, 0);
            if (!LivingWorldNetworkBinding.HasMirror) return;
            var now = Time.realtimeSinceStartup;
            if (now - sceneStarted < 2.5f) return;   // like every other probe: the spawn must have settled before the first move
            if (livingStageAt == 0) livingStageAt = now;
            if (livingStage < 90 && now - livingStageAt > 60f)
            {
                result.errors.Add($"living lobby stage {livingStage} timeout dev={LivingWorldNetworkBinding.Mirrored.Development.Count} last={LivingWorldNetworkBinding.LastResultReason}");
                livingStage = 99;
                return;
            }
            var pc = FindFirstObjectByType<HomePcAnchor>();
            if (pc == null) return;
            var host = adapter.IsAuthority;
            var me = local.OwnerClientId;
            var dev = LivingWorldNetworkBinding.Development;
            if (now >= livingTraceAt && result.livingTrace.Count < 60)
            {
                livingTraceAt = now + 3f;
                var q = local.transform.position;
                result.livingTrace.Add($"lobby stage={livingStage} pos=({q.x:0.0},{q.y:0.0},{q.z:0.0}) pc=({pc.transform.position.x:0.0},{pc.transform.position.y:0.0},{pc.transform.position.z:0.0}) d={(new Vector3(pc.transform.position.x - q.x, 0f, pc.transform.position.z - q.z)).magnitude:0.00} host={adapter.IsAuthority} dev={LivingWorldNetworkBinding.Mirrored.Development.Count}");
            }
            bool ok; string reason;

            switch (livingStage)
            {
                case 0:   // still away from the PC: the HOST must refuse
                {
                    var away = local.transform.position - pc.transform.position; away.y = 0;
                    if (away.magnitude < 6f)
                    {
                        if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                        local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg, 0);
                        return;
                    }
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.TownFisher);
                    livingStage = 1; return;
                }
                case 1:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    result.livingFarRefused = !ok && reason == "NotAtPc";
                    LivingStep(2); return;
                case 2:   // walk to the PC
                {
                    // Each player walks to its OWN standing spot beside the PC (host west, guest east, 2 m from it: inside the host's 3.25 m range). Two bodies cannot
                    // share the spot in front of the PC, and a straight walk to the PC let the first player block the second.
                    var stand = pc.transform.position + (host ? new Vector3(-1.3f, 0f, 1.6f) : new Vector3(0.4f, 0f, 1.9f));   // the east wall stands at x=5: the guest spot stays inside the room
                    var to = stand - local.transform.position; to.y = 0;
                    if (to.magnitude > 0.45f) { local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 0); return; }
                    LivingStep(host ? 5 : 3); return;
                }
                case 3:   // guest: the fisherman's stall first (the host waits for it), then the same again, then a role and a changed mind
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.TownFisher); livingStage = 31; return;
                case 31:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    result.livingBuildOk = ok;
                    if (!ok) result.errors.Add("guest build refused: " + reason);
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.TownFisher); livingStage = 32; return;
                case 32:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    result.livingDuplicateRefused = !ok && reason == "AlreadyProcessed";
                    livingAwait = LivingWorldNetworkBinding.RequestRole(PlayerRole.Cameraman); livingStage = 33; return;
                case 33:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    if (!ok) result.errors.Add("guest role refused: " + reason);
                    livingAwait = LivingWorldNetworkBinding.RequestRole(PlayerRole.Carrier); livingStage = 34; return;
                case 34:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    if (!ok) result.errors.Add("guest role change refused: " + reason);
                    LivingStep(8); return;
                case 5:   // host: wait for the guest's stall, then home 2, shop, dock, and its own role
                    if (!dev.Owns(DevelopmentIds.TownFisher)) return;
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.Home2); livingStage = 51; return;
                case 51:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    if (!ok) result.errors.Add("host home-2 refused: " + reason);
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.TownShop); livingStage = 52; return;
                case 52:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    if (!ok) result.errors.Add("host shop refused: " + reason);
                    livingAwait = LivingWorldNetworkBinding.RequestBuild(DevelopmentIds.TownDock); livingStage = 53; return;
                case 53:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    result.livingBuildOk = ok;
                    if (!ok) result.errors.Add("host dock refused: " + reason);
                    livingAwait = LivingWorldNetworkBinding.RequestRole(PlayerRole.Hunter); livingStage = 54; return;
                case 54:
                    if (!LivingAnswered(livingAwait, out ok, out reason)) return;
                    if (!ok) result.errors.Add("host role refused: " + reason);
                    LivingStep(8); return;
                case 8:   // everybody: all four are built and BOTH roles are what the players asked for, seen through this process's mirror
                {
                    var guestId = adapter.Session.Roster.Keys.Where(k => k.Value != 0).Select(k => k.Value).DefaultIfEmpty(1UL).First();
                    if (!DevelopmentIds.All.All(dev.Owns)) return;
                    var hostRole = LivingWorldNetworkBinding.RoleOfClient(0);
                    var guestRole = LivingWorldNetworkBinding.RoleOfClient(adapter.IsAuthority ? guestId : me);
                    if (hostRole != PlayerRole.Hunter || guestRole != PlayerRole.Carrier) return;
                    result.livingRolesOk = true;
                    LivingStep(90); return;
                }
            }
        }

        // Host: the labelled start money, the spend checks, the dive injection, the order/pay/persist/reload checks.
        private void HostLiving(SessionState state)
        {
            var binding = adapter.GetComponent<LivingWorldNetworkBinding>();
            var economy = adapter.GetComponent<EconomyManager>();
            var store = adapter.GetComponent<EconomySaveStore>();
            var living = binding != null ? binding.Authority : null;
            if (living == null || economy == null || store == null) return;
            var now = Time.realtimeSinceStartup;

            switch (livingHostStage)
            {
                case 0:   // lobby, before anything: seed the money (labelled), then wait for the builds and roles
                {
                    if (state.Phase != SessionPhase.Lobby || adapter.Connection.IsSceneLoading || now - sceneStarted < 3f) return;
                    var seed = economy.ExportSaveData("smoke", "smoke");
                    seed.SharedBalance = LivingSeedBalance;
                    result.livingSeeded = economy.TryRestore(seed) && economy.SharedBalance == LivingSeedBalance;
                    livingHostStage = 1; return;
                }
                case 1:
                {
                    if (!DevelopmentIds.All.All(living.Development.Owns) || living.RoleOf(new PlayerId(0)) != PlayerRole.Hunter) return;
                    var guest = adapter.Session.Roster.Keys.FirstOrDefault(k => k.Value != 0);
                    if (living.RoleOf(guest) != PlayerRole.Carrier) return;
                    var spent = 0;
                    foreach (var d in DevelopmentCatalog.All) spent += d.Price;
                    result.livingBalanceAfterBuild = economy.SharedBalance;
                    result.livingSpendOk = economy.SharedBalance == LivingSeedBalance - spent;
                    result.livingEffectsHost = EconomyManager.StorageCapacity == EconomyManager.StorageCapacityItems + DevelopmentCatalog.HomeStorageBonusSlots &&
                        DevelopmentEffects.FishPricePercentBonus == 10 && DevelopmentEffects.VehiclePriceDiscountPercent == 10 && DevelopmentEffects.ShopStockUnlocked &&
                        PlayerRoles.RoleOf(new PlayerId(0)) == PlayerRole.Hunter && PlayerRoles.RoleOf(guest) == PlayerRole.Carrier;
                    if (!result.livingSpendOk) result.errors.Add($"living spend balance={economy.SharedBalance} expected={LivingSeedBalance - spent}");
                    livingHostAt = now;
                    livingHostStage = 2; return;
                }
                case 2:   // every mirror gets a moment to show the builds; with screenshots asked for, the host also aims at the home shelf for a picture
                {
                    var me = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
                    if (me == null || Arg("-p1-screenshot").Length == 0)
                    {
                        if (now - livingHostAt > 3f) { result.livingHostLobbyDone = true; livingHostStage = 3; }
                        return;
                    }
                    var shelf = new Vector3(0f, 1f, -3.55f);
                    var toShelf = shelf - me.transform.position; toShelf.y = 0;
                    me.SubmitLocalInput(Vector3.zero, Mathf.Atan2(toShelf.x, toShelf.z) * Mathf.Rad2Deg, 0);
                    if (now - livingHostAt > 1.5f && !livingCaptured) { livingCaptured = true; CaptureRoom("-home"); }
                    if (now - livingHostAt > 2.5f) { result.livingHostLobbyDone = true; livingHostStage = 3; }
                    return;
                }
                case 3:   // dive: two 1 kg catches per player, safely returned (the pickup path itself is -p2-hunt's)
                    if (state.Phase == SessionPhase.Dive && !livingInjected && !adapter.Connection.IsSceneLoading &&
                        SceneManager.GetActiveScene().name == SessionNetworkAdapter.DiveScene && Time.realtimeSinceStartup - sceneStarted > 6f)
                    {
                        livingInjected = true;
                        var inventory = adapter.GetComponent<InventoryManager>();
                        foreach (var id in adapter.Session.Roster.Keys)
                        {
                            for (var i = 0; i < 2; i++) inventory.TryAddCatch(id, new CaptureResult($"living-{id.Value}-{i}", state.DiveId, "sea_bass", 1000, 1));
                            inventory.TryMarkSafeReturn(id);
                        }
                        livingHostStage = 4; livingHostAt = now;
                    }
                    return;
                case 4:   // town: both hand-ins done -> the day's order is complete and paid ONCE (sale + stall bonus + order reward)
                {
                    if (state.Phase != SessionPhase.Return || economy.PendingTurnIns().Count != 0) return;
                    var board = living.Board;
                    if (!board.Order.HasContract || board.Order.Status != ContractStatus.Completed) return;
                    ContractCatalog.TryGet(board.Order.TemplateId, out var template);
                    var expected = result.livingBalanceAfterBuild + 4 * (120 + 12) + template.Reward;
                    result.livingFinalBalance = economy.SharedBalance;
                    result.livingOrderPaidOnce = economy.SharedBalance == expected && economy.IsRewardPaid(ContractIds.RewardId(board.Day, template.Id));
                    if (!result.livingOrderPaidOnce) result.errors.Add($"living order pay balance={economy.SharedBalance} expected={expected} template={template.Id}");

                    var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                    result.livingSavedOnDisk = disk.HasLiving && disk.SchemaVersion == EconomySaveData.CurrentSchemaVersion && disk.Living.DevelopmentIds.Count == 4 &&
                        disk.Living.HostRole == (byte)PlayerRole.Hunter && disk.Living.OrderStatus == (byte)ContractStatus.Completed && disk.RewardIds.Contains(ContractIds.RewardId(board.Day, template.Id)) &&
                        disk.SharedBalance == economy.SharedBalance;
                    var balance = economy.SharedBalance;
                    result.livingReloadClean = store.LoadNow() && living.Development.OwnedIds.Count == 4 && living.Board.Order.Status == ContractStatus.Completed &&
                        economy.SharedBalance == balance && !living.EnsureDay(board.Day, 7) && living.RoleOf(new PlayerId(0)) == PlayerRole.Hunter;
                    livingDoneAt = now; result.livingHostDone = true; livingHostStage = 90; return;
                }
            }
        }

        // Town (Return phase), every process: walk to the fish buyer and hand in; the sale shows the stall bonus; the town props are really built.
        private void ProbeLivingTown(NetworkPlayer local)
        {
            var sync = local.GetComponent<EconomyPlayerSync>();
            var cam = local.GetComponentInChildren<Camera>(true);
            if (sync == null || cam == null) return;
            var now = Time.realtimeSinceStartup;
            if (livingStageAt == 0 || livingStage < 100) { livingStageAt = now; livingStage = 100; }
            if (now >= livingTraceAt && result.livingTrace.Count < 60)
            {
                livingTraceAt = now + 3f;
                var q = local.transform.position;
                result.livingTrace.Add($"town pos=({q.x:0.0},{q.y:0.0},{q.z:0.0}) sw={local.Swimming.Value} pending={sync.PendingCatches.Value} svc={sync.LastServiceRequestId.Value}/{sync.LastServiceAccepted.Value}/{sync.LastServiceReason.Value} bal={sync.SharedBalance.Value}");
            }
            if (!result.livingSold)
            {
                if (GoToService(local, cam, TownServiceCatalog.FishBuyerId)) InteractEvery(local, 1.2f);
                if (sync.LastServiceRequestId.Value != 0 && (ServicePointType)sync.LastServiceType.Value == ServicePointType.FishBuyer && sync.LastServiceAccepted.Value)
                {
                    result.livingSaleAmount = sync.LastServiceAmount.Value;
                    result.livingSold = sync.LastServiceItems.Value == 2 && sync.LastServiceAmount.Value == 2 * (120 + 12);
                    if (!result.livingSold) result.errors.Add($"living sale amount={sync.LastServiceAmount.Value} items={sync.LastServiceItems.Value}");
                    livingStageAt = now;
                }
                return;
            }
            local.SubmitLocalInput(Vector3.zero, 0);
            result.livingTownVisuals |= VisualBuilt(DevelopmentIds.TownFisher) && VisualBuilt(DevelopmentIds.TownShop) && VisualBuilt(DevelopmentIds.TownDock);
            if (adapter.IsAuthority && !livingTownCaptured && Arg("-p1-screenshot").Length > 0 && now - livingStageAt > 2f && result.livingTownVisuals)
            { livingTownCaptured = true; CaptureRoom("-town"); }
        }

        // Second launch of the host on the SAME campaign file: the board, the builds and the host's role come back; nothing is paid or bought again.
        private bool HostLivingReload()
        {
            var binding = adapter.GetComponent<LivingWorldNetworkBinding>();
            var economy = adapter.GetComponent<EconomyManager>();
            var living = binding != null ? binding.Authority : null;
            if (living == null || economy == null || adapter.Connection.Status != ConnectionStatus.Connected || DayLock.StateProvider == null) return false;
            var board = living.Board;
            if (board.Day == 0) return false;
            var host = new PlayerId(0);
            result.livingReloadDay = board.Day;
            result.livingReloadBalance = economy.SharedBalance;
            result.livingReloadOrderStatus = board.Order.Status.ToString();
            var before = economy.SharedBalance;
            // The reopened session is on day 1 again only if the file said so: the board for THAT day must be exactly what was saved (not re-rolled).
            var dayState = DayLock.StateProvider();
            result.livingReloadPass = DevelopmentIds.All.All(living.Development.Owns) && living.RoleOf(host) == PlayerRole.Hunter &&
                board.Order.Status == ContractStatus.Completed && dayState.DayNumber == board.Day &&
                EconomyManager.StorageCapacity == EconomyManager.StorageCapacityItems + DevelopmentCatalog.HomeStorageBonusSlots &&
                living.TryBuildDevelopment(host, DevelopmentIds.TownFisher, 99001).ReasonCode == "AlreadyProcessed" && !living.EnsureDay(board.Day, 7) &&
                economy.SharedBalance == before && living.RoleOf(new PlayerId(1)) == PlayerRole.None;
            result.passed = result.livingReloadPass && result.errors.Count == 0;
            return true;
        }

        // ---- #123 deep progression smoke -------------------------------------------------------------------------
        // LABELLED FIXTURE: the world rule that says which trace/arena contexts count (Utku's #122). The progression authority, the exploration
        // evidence (a real sighting + a real swim to the reef), the save file, the mirrors and the read seam are all the product's.
        private sealed class SmokeDeepWorld : IDeepProgressionWorld
        {
            public bool IsValidTrace(string traceId, string cellId, string depthBandId) => traceId.StartsWith("smoke-trace-") && depthBandId == DepthBandIds.Deep && cellId.Length > 0;
            public bool IsDiscoveryArea(string arenaId, string cellId, string depthBandId) => arenaId == "smoke-arena" && depthBandId == DepthBandIds.Deep && cellId.Length > 0;
        }

        private readonly SmokeDeepWorld deepWorld = new SmokeDeepWorld();
        private int deepStage;
        private float deepAt, deepDoneAt;
        private ulong deepRequest = 9000;

        private void DeepNext(int next) { result.deepTrace.Add($"stage {deepStage}->{next} t={Time.realtimeSinceStartup - sceneStarted:F1} state={BossProgression.State.Stage}"); deepStage = next; deepAt = Time.realtimeSinceStartup; }

        // Every process: what its OWN mirrored read seam shows, in order (a guest has no authority: it can only watch).
        private void ObserveDeep()
        {
            var local = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
            var sync = local != null ? local.GetComponent<EconomyPlayerSync>() : null;
            if (sync == null || !sync.IsSpawned) return;
            var stage = (int)BossProgression.State.Stage;
            if (result.deepStagesSeen.Count == 0 || result.deepStagesSeen[result.deepStagesSeen.Count - 1] != stage) result.deepStagesSeen.Add(stage);
            result.deepGuestSeqOk = result.deepStagesSeen.SequenceEqual(new[] { 0, 1, 2, 3, 4 });
            result.deepMirrorUnlocked |= BossProgression.IsAvailable(DeepProgressionIds.BossId);
            result.deepLineShown |= BossProgression.State.BossUnlocked && DeepProgressionPresenter.Line(BossProgression.State).Length > 0;
            result.deepMirrorCompleted |= BossProgression.IsCompleted(DeepProgressionIds.BossId);
        }

        private void HostDeep(SessionState state)
        {
            var store = adapter.GetComponent<EconomySaveStore>();
            var now = Time.realtimeSinceStartup;
            var host = new PlayerId(0);
            var guest = adapter.Session.Roster.Keys.FirstOrDefault(k => k.Value != 0);
            switch (deepStage)
            {
                case 0:   // dive: the chain is closed; nothing can be skipped; then ONE real, host-counted sighting opens the encyclopedia step
                {
                    if (state.Phase != SessionPhase.Dive || adapter.Connection.IsSceneLoading || SceneManager.GetActiveScene().name != SessionNetworkAdapter.DiveScene ||
                        Time.realtimeSinceStartup - sceneStarted < 6f || !DeepProgressionEvidence.IsBound) return;
                    var binding = adapter.GetComponent<ExplorationNetworkBinding>();
                    var me = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner && p.IsSpawned);
                    if (binding == null || binding.Species == null || me == null) return;
                    result.deepOrderRefused = DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "OutOfOrder" &&
                        DeepProgressionEvidence.TrySubmitDiscovery(host, "smoke-arena", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "OutOfOrder" &&
                        DeepProgressionEvidence.TryCompleteBoss(DeepProgressionIds.BossId, "smoke-enc", deepRequest++).ReasonCode == "OutOfOrder" &&
                        !BossProgression.IsAvailable(DeepProgressionIds.BossId);
                    // Sighting through the product's real species authority (labelled like -Explore: the fish itself is not hunted here).
                    var sighting = binding.Species.AcceptSighting("sea_bass", me.transform.position, 1).ToString();
                    result.deepSightingCounted = sighting == "CountedNewEvidence" || sighting == "AlreadyCounted";   // the product binding may have counted a real fish first
                    DeepNext(1); return;
                }
                case 1:
                    if (BossProgression.State.Stage >= DeepProgressionStage.Encyclopedia) { result.deepEncyclopedia = BossProgression.State.Stage == DeepProgressionStage.Encyclopedia; DeepNext(2); }
                    else if (now - deepAt > 8f) { result.errors.Add("deep: encyclopedia step never opened"); DeepNext(99); }
                    return;
                case 2:   // the host really swims to the reef (ProbeFleetDive); the discovered Reef cell opens the rumor
                    if (BossProgression.State.Stage >= DeepProgressionStage.Rumor)
                    {
                        result.deepRumor = BossProgression.State.Stage == DeepProgressionStage.Rumor && result.fleetReefSwum;
                        DeepNext(3);
                    }
                    else if (now - deepAt > 60f) { result.errors.Add("deep: rumor never opened (reef not reached?)"); DeepNext(99); }
                    return;
                case 3:   // back at the beach (Return phase): with NO world validator bound a trace is refused (fail-closed)
                    if (state.Phase != SessionPhase.Return || now - deepAt < 2f) return;
                    result.deepFailClosed = DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "WorldUnavailable" &&
                        BossProgression.State.TracesFound == 0;
                    DeepProgressionWorld.Bind(deepWorld);
                    DeepNext(4); return;
                case 4:   // the world validator decides the context; three DISTINCT traces (any player) open the trace stage; a duplicate counts once
                {
                    result.deepContextRefused = DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Shallow, deepRequest++).ReasonCode == "InvalidContext" &&
                        DeepProgressionEvidence.TrySubmitTrace(host, "bogus", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "InvalidContext";
                    var ok = DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Deep, deepRequest++).Accepted &&
                        DeepProgressionEvidence.TrySubmitTrace(guest, "smoke-trace-2", "smoke-cell-2", DepthBandIds.Deep, deepRequest++).Accepted &&
                        DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "AlreadyProcessed" &&
                        BossProgression.State.Stage == DeepProgressionStage.Rumor && BossProgression.State.TracesFound == 2;
                    ok &= DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-3", "smoke-cell-3", DepthBandIds.Deep, deepRequest++).Accepted;
                    result.deepTracesOk = ok;
                    result.deepTraceStage = BossProgression.State.Stage == DeepProgressionStage.Trace && BossProgression.State.TracesFound == DeepProgressionIds.RequiredTraceCount;
                    DeepNext(5); return;
                }
                case 5:   // hold the trace stage long enough for every mirror to show it, then the arena: wrong context refused, real one unlocks
                    if (now - deepAt < 1.5f) return;
                    result.deepDiscoveryRefused = DeepProgressionEvidence.TrySubmitDiscovery(host, "smoke-arena", "smoke-cell", DepthBandIds.Reef, deepRequest++).ReasonCode == "InvalidContext" &&
                        !BossProgression.IsAvailable(DeepProgressionIds.BossId);
                    result.deepUnlocked = DeepProgressionEvidence.TrySubmitDiscovery(guest, "smoke-arena", "smoke-cell", DepthBandIds.Deep, deepRequest++).Accepted &&
                        BossProgression.IsAvailable(DeepProgressionIds.BossId);
                    DeepNext(6); return;
                case 6:   // the encounter layer reports one completion; a second encounter id is not a second completion; the file and a reload agree
                {
                    if (now - deepAt < 1.5f) return;
                    result.deepCompletedOnce = DeepProgressionEvidence.TryCompleteBoss(DeepProgressionIds.BossId, "smoke-enc-1", deepRequest++).Accepted &&
                        DeepProgressionEvidence.TryCompleteBoss(DeepProgressionIds.BossId, "smoke-enc-2", deepRequest++).ReasonCode == "AlreadyProcessed" &&
                        BossProgression.State.CompletedBossIds.Count == 1;
                    var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                    result.deepSavedOnDisk = disk.HasProgression && disk.Progression.Stage == (byte)DeepProgressionStage.Discovery &&
                        disk.Progression.TraceIds.Count == DeepProgressionIds.RequiredTraceCount && disk.Progression.CompletedBossIds.SequenceEqual(new[] { DeepProgressionIds.BossId }) &&
                        disk.SchemaVersion == EconomySaveData.CurrentSchemaVersion;
                    var traces = BossProgression.State.TracesFound;
                    result.deepReloadClean = store.LoadNow() && BossProgression.State.Stage == DeepProgressionStage.Discovery && BossProgression.State.TracesFound == traces &&
                        BossProgression.State.CompletedBossIds.Count == 1 &&
                        DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-9", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "AlreadyProcessed";
                    deepDoneAt = now; result.deepHostDone = true; DeepNext(90); return;
                }
            }
        }

        // Second launch of the host on the SAME campaign file: the chain, the traces and the completion come back, nothing is counted twice, and a boss
        // that was never persisted as "active" is simply available again (the encounter itself is not restored).
        private bool HostDeepReload()
        {
            var store = adapter.GetComponent<EconomySaveStore>();
            if (store == null || adapter.Connection.Status != ConnectionStatus.Connected || !DeepProgressionEvidence.IsBound) return false;
            var host = new PlayerId(0);
            var state = BossProgression.State;
            result.deepReloadStage = state.Stage.ToString();
            result.deepReloadTraces = state.TracesFound;
            result.deepReloadCompleted = state.CompletedBossIds.Count;
            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
            result.deepReloadPass = state.Stage == DeepProgressionStage.Discovery && state.TracesFound == DeepProgressionIds.RequiredTraceCount && state.CompletedBossIds.Count == 1 &&
                BossProgression.IsAvailable(DeepProgressionIds.BossId) && BossProgression.IsCompleted(DeepProgressionIds.BossId) &&
                DeepProgressionEvidence.TrySubmitTrace(host, "smoke-trace-1", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "AlreadyProcessed" &&
                DeepProgressionEvidence.TrySubmitDiscovery(host, "smoke-arena", "smoke-cell", DepthBandIds.Deep, deepRequest++).ReasonCode == "AlreadyProcessed" &&
                DeepProgressionEvidence.TryCompleteBoss(DeepProgressionIds.BossId, "smoke-enc-3", deepRequest++).ReasonCode == "AlreadyProcessed" &&
                BossProgression.State.CompletedBossIds.Count == 1 && disk.HasProgression && disk.Progression.CompletedBossIds.Count == 1;
            result.passed = result.deepReloadPass && result.errors.Count == 0;
            return true;
        }

        // ---- #109 fleet smoke --------------------------------------------------------------------------------
        private int fleetStep, fleetHostStage;
        private float fleetStepAt, fleetHostAt, fleetDoneAt, fleetNext, fleetTraceAt;
        private ulong fleetRequest = 7000;
        private const int FleetSeedBalance = 5000;

        private void FleetStep(int step)
        {
            result.fleetTrace.Add($"step {fleetStep}->{step} t={Time.realtimeSinceStartup - sceneStarted:F1}");
            fleetStep = step; fleetStepAt = Time.realtimeSinceStartup; townAwaiting = false; townSettleAt = 0;
        }

        // Sends one request and reports its host answer on a later frame (the host answer arrives through the player's own sync).
        private bool FleetCall(EconomyPlayerSync sync, Action send, out bool accepted, out string reason)
        {
            accepted = false; reason = "";
            if (!townAwaiting) { townBefore = (int)sync.LastRequestId.Value; townAwaiting = true; send(); return false; }
            if (!PurchaseAnswered(sync)) return false;
            townAwaiting = false;
            accepted = sync.LastAccepted.Value;
            reason = sync.LastReasonCode.Value.ToString();
            result.fleetTrace.Add($"answer step={fleetStep} ok={accepted} reason={reason}");
            return true;
        }

        // The host REALLY swims to the reef shelf (depth 8-20 m, z > 19) so Utku's real exploration binding discovers a Reef-band
        // cell from the approved player position, then swims back near the beach. The research boat's gate reads exactly that
        // discovery. The guest stays put.
        private float fleetBackAt = float.MaxValue;
        private static readonly Vector3 FleetReefTarget = new Vector3(3f, -4f, 28f);
        private static readonly Vector3 FleetBackTarget = new Vector3(-3f, 6f, -4f);

        private void ProbeFleetDive(NetworkPlayer local)
        {
            if (!adapter.IsAuthority) { local.SubmitLocalInput(Vector3.zero, 0); return; }
            var store = adapter.GetComponent<EconomySaveStore>();
            var now = Time.realtimeSinceStartup;
            if (!result.fleetReefSwum && store != null && store.HasDiscoveredCellInBand(DepthBandIds.Reef))
            {
                result.fleetReefSwum = true; fleetBackAt = now;
                result.fleetTrace.Add($"reef band discovered t={now - sceneStarted:F1} pos={local.transform.position}");
            }
            var target = result.fleetReefSwum ? FleetBackTarget : FleetReefTarget;
            var heading = target - local.transform.position;
            var flat = new Vector3(heading.x, 0f, heading.z);
            var yaw = flat.sqrMagnitude > 0.01f ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : 0f;
            var move = Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * heading.normalized;
            local.SubmitLocalInput(heading.magnitude > 0.6f ? move : Vector3.zero, yaw, 0);
            if (now >= fleetTraceAt && result.fleetTrace.Count < 60)
            {
                fleetTraceAt = now + 3f;
                var q = local.transform.position;
                result.fleetTrace.Add($"dive pos=({q.x:0.0},{q.y:0.0},{q.z:0.0}) reef={result.fleetReefSwum}");
            }
        }

        private void ProbeFleet(NetworkPlayer local)
        {
            var sync = local.GetComponent<EconomyPlayerSync>();
            var tripSync = local.GetComponent<BoatTripPlayerSync>();
            var cam = local.GetComponentInChildren<Camera>(true);
            if (sync == null || cam == null || tripSync == null) return;
            var host = adapter.IsAuthority;
            var now = Time.realtimeSinceStartup;
            if (fleetStepAt == 0) fleetStepAt = now;
            if (now >= fleetTraceAt && result.fleetTrace.Count < 60)
            {
                fleetTraceAt = now + 3f;
                var q = local.transform.position;
                result.fleetTrace.Add($"step={fleetStep} pos=({q.x:0.0},{q.y:0.0},{q.z:0.0}) sw={local.Swimming.Value} shop={sync.ActiveServiceId.Value} mask={sync.FleetOwnedMask.Value} active={sync.ActiveVehicleId.Value} seated={tripSync.SeatedCount.Value} bal={sync.SharedBalance.Value}");
            }
            if (fleetStep < 90 && now - fleetStepAt > 60f)
            {
                result.errors.Add($"fleet step {fleetStep} timeout mask={sync.FleetOwnedMask.Value} active={sync.ActiveVehicleId.Value} seated={tripSync.SeatedCount.Value} last={sync.LastReasonCode.Value}");
                FleetStep(99);
            }
            bool ok; string reason;

            switch (fleetStep)
            {
                case 0:   // equipment shop: really walk there and interact
                    if (GoToService(local, cam, TownServiceCatalog.EquipmentShopId)) InteractEvery(local, 1.2f);
                    if (sync.ShopOpen) { result.fleetShopOpened = true; FleetStep(1); }
                    return;
                case 1:   // a tier cannot be bought on top of nothing
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(EconomyManager.CameraAdvancedId), out ok, out reason)) return;
                    result.fleetTierGateRefused = !ok && reason == "RequirementMissing";
                    if (!result.fleetTierGateRefused) result.errors.Add($"tier gate answer ok={ok} reason={reason}");
                    FleetStep(2); return;
                case 2:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(EconomyManager.CameraBasicId), out ok, out reason)) return;
                    if (!ok) result.errors.Add("camera-basic refused: " + reason);
                    FleetStep(3); return;
                case 3:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(EconomyManager.CameraAdvancedId), out ok, out reason)) return;
                    if (!ok) result.errors.Add("camera-advanced refused: " + reason);
                    FleetStep(4); return;
                case 4:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(EconomyManager.FinsId), out ok, out reason)) return;
                    result.fleetTiersBought = ok;
                    if (!ok) result.errors.Add("fins refused: " + reason);
                    FleetStep(5); return;
                case 5:   // harbor vendor: walk along the beach to the fourth NPC and interact
                    if (GoToService(local, cam, TownServiceCatalog.VehicleVendorId)) InteractEvery(local, 1.2f);
                    if (sync.VendorOpen) { result.fleetVendorOpened = true; FleetStep(host ? 10 : 7); }
                    return;
                case 6:   // host: buy the motorboat (needs the repaired sandal the seed repaired through the real part seam)
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(VehicleIds.Motorboat), out ok, out reason)) return;
                    result.fleetMotorBought = ok;
                    if (!ok) result.errors.Add("motorboat refused: " + reason);
                    FleetStep(10); return;
                case 7:   // guest: once the motorboat is owned, buying it again is refused (one vehicle, one charge)
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if ((sync.FleetOwnedMask.Value & 2) == 0) return;
                    if (!FleetCall(sync, () => sync.RequestPurchase(VehicleIds.Motorboat), out ok, out reason)) return;
                    result.fleetMotorDuplicateRefused = !ok && reason == "AlreadyProcessed";
                    if (!result.fleetMotorDuplicateRefused) result.errors.Add($"duplicate motorboat answer ok={ok} reason={reason}");
                    FleetStep(8); return;
                case 8:   // guest: the research boat is available only after the motorboat
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (!FleetCall(sync, () => sync.RequestPurchase(VehicleIds.ResearchBoat), out ok, out reason)) return;
                    result.fleetResearchBought = ok;
                    if (!ok) result.errors.Add("research boat refused: " + reason);
                    FleetStep(11); return;
                case 11:  // guest: while the host sits in the sandal the swap is refused; once it stepped off it works
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (tripSync.SeatedCount.Value <= 0) return;
                    if (!FleetCall(sync, () => sync.RequestSelectVehicle(VehicleIds.Motorboat), out ok, out reason)) return;
                    result.fleetSeatBlocked = !ok && reason == "SeatsOccupied";
                    if (!result.fleetSeatBlocked) result.errors.Add($"seated swap answer ok={ok} reason={reason}");
                    FleetStep(12); return;
                case 12:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    if (tripSync.SeatedCount.Value > 0) return;
                    if (!FleetCall(sync, () => sync.RequestSelectVehicle(VehicleIds.Motorboat), out ok, out reason)) return;
                    result.fleetSelected = ok;
                    if (!ok) result.errors.Add("select motorboat refused: " + reason);
                    FleetStep(10); return;
                case 10:  // everybody: the mirrors and the seam follow the host's decisions
                    local.SubmitLocalInput(Vector3.zero, 0);
                    result.fleetMirrorOwned |= sync.FleetOwnedMask.Value == 7;
                    result.fleetMirrorActive |= sync.ActiveVehicleId.Value.ToString() == VehicleIds.Motorboat;
                    result.fleetActiveSeam |= ActiveVehicle.BoatId == VehicleIds.Motorboat;
                    if (result.fleetMirrorOwned && result.fleetMirrorActive && result.fleetActiveSeam) FleetStep(90);
                    return;
                default:
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
            }
        }

        // Host: the labelled money seed + the real free-part seam repair the sandal; later it sits in the sandal for a while (real
        // trip authority) so the guest's swap request meets a real seated player, then verifies the authoritative state and the file.
        private void HostFleet(SessionState state)
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var trip = adapter.GetComponent<BoatTripManager>();
            if (economy == null || trip == null) return;
            var host = new PlayerId(0);
            var now = Time.realtimeSinceStartup;

            switch (fleetHostStage)
            {
                case 0:
                    if (state.Phase != SessionPhase.Prep || adapter.Connection.IsSceneLoading) return;
                    var seed = economy.ExportSaveData("smoke", "smoke");
                    seed.SharedBalance = FleetSeedBalance;
                    var restored = economy.TryRestore(seed);
                    var repaired = true;
                    foreach (var part in BoatRepairParts.All) repaired &= BoatPartClaim.TryClaimFound(host, part, fleetRequest++).Accepted;
                    result.fleetSeeded = restored && repaired && economy.Fleet.IsOwned(VehicleIds.Rowboat) && economy.SharedBalance == FleetSeedBalance;
                    // Labelled seed: the motorboat is bought through the real economy API here (the RPC purchase path was proven by the first
                    // -Fleet version); the research boat must then be refused until a Reef cell is really discovered (the dive does that).
                    var guestId = adapter.Session.Roster.Keys.First(k => k.Value != 0);
                    result.fleetMotorSeeded = economy.TryPurchaseVehicle(host, VehicleIds.Motorboat, fleetRequest++).Accepted;
                    var beforeGate = economy.SharedBalance;
                    result.fleetReefGateRefused = economy.TryPurchaseVehicle(guestId, VehicleIds.ResearchBoat, fleetRequest++).ReasonCode == "RequirementMissing" &&
                        economy.SharedBalance == beforeGate && !economy.Fleet.IsOwned(VehicleIds.ResearchBoat) &&
                        !adapter.GetComponent<EconomySaveStore>().HasDiscoveredCellInBand(DepthBandIds.Reef);
                    if (!result.fleetMotorSeeded || !result.fleetReefGateRefused) result.errors.Add($"fleet seed motor={result.fleetMotorSeeded} reefGate={result.fleetReefGateRefused}");
                    if (!result.fleetSeeded) result.errors.Add($"fleet seed restored={restored} repaired={repaired} balance={economy.SharedBalance}");
                    fleetHostStage = 1; return;
                case 1:   // the research boat just got bought: the host takes a seat in the (still active) sandal
                    if (!economy.Fleet.IsOwned(VehicleIds.ResearchBoat)) return;
                    result.fleetHostSeated = BoatBoarding.TryBoard(host, VehicleIds.Rowboat, BoatTripIds.Seat0, fleetRequest++).Accepted;
                    if (!result.fleetHostSeated) result.errors.Add("host could not take a seat in the sandal");
                    fleetHostAt = now; fleetHostStage = 2; return;
                case 2:   // hold the seat long enough for the guest's refused swap, then step off
                    if (now - fleetHostAt < 8f) return;
                    BoatBoarding.TryDisembark(host, fleetRequest++);
                    fleetHostStage = 3; return;
                case 3:
                    if (economy.ActiveVehicleId != VehicleIds.Motorboat) return;
                    var guest = adapter.Session.Roster.Keys.First(k => k.Value != 0);
                    // The parked sandal can no longer be boarded: the trip authority serves the active motorboat.
                    result.fleetParkedRefused = BoatBoarding.TryBoard(host, VehicleIds.Rowboat, BoatTripIds.Seat0, fleetRequest++).ReasonCode == "InvalidTarget" &&
                        trip.State.BoatId == VehicleIds.Motorboat;
                    var tiers = new[] { EconomyManager.CameraBasicId, EconomyManager.CameraAdvancedId, EconomyManager.FinsId };
                    var expectedBalance = FleetSeedBalance - 2 * (150 + 400 + 220) - VehicleCatalog.MotorboatPrice - VehicleCatalog.ResearchBoatPrice;
                    result.fleetBalance = economy.SharedBalance;
                    result.fleetHostChecks = economy.SharedBalance == expectedBalance &&
                        economy.Fleet.OwnedBoatIds.Count == 3 && economy.ActiveVehicleId == VehicleIds.Motorboat &&
                        tiers.All(economy.LoadoutFor(host).Contains) && economy.LoadoutFor(host).Count == 3 &&
                        tiers.All(economy.LoadoutFor(guest).Contains) && economy.LoadoutFor(guest).Count == 3 &&
                        economy.TryPurchaseVehicle(guest, VehicleIds.ResearchBoat, fleetRequest++).ReasonCode == "AlreadyProcessed" &&
                        economy.SharedBalance == expectedBalance;
                    if (!result.fleetHostChecks) result.errors.Add($"fleet host checks balance={economy.SharedBalance} expected={expectedBalance} owned={economy.Fleet.OwnedBoatIds.Count} active={economy.ActiveVehicleId}");

                    var store = adapter.GetComponent<EconomySaveStore>();
                    var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                    result.fleetSavedOnDisk = disk.HasFleet && disk.FleetPurchasedBoatIds.SequenceEqual(new[] { VehicleIds.Motorboat, VehicleIds.ResearchBoat }) &&
                        disk.FleetActiveBoatId == VehicleIds.Motorboat && disk.SharedBalance == economy.SharedBalance && disk.SchemaVersion == EconomySaveData.CurrentSchemaVersion;
                    result.fleetReefOnDisk = disk.HasExploration && disk.Exploration.DiscoveredCells.Any(c => c != null && c.DepthBandId == DepthBandIds.Reef);
                    var balance = economy.SharedBalance;
                    result.fleetReloadClean = store.LoadNow() && economy.Fleet.OwnedBoatIds.Count == 3 && economy.ActiveVehicleId == VehicleIds.Motorboat &&
                        economy.SharedBalance == balance && economy.TrySelectVehicle(host, VehicleIds.Motorboat, fleetRequest++).ReasonCode == "AlreadyProcessed";
                    fleetDoneAt = now; result.fleetHostDone = true; fleetHostStage = 90; return;
            }
        }

        // Second launch of the host on the SAME campaign file: ownership, active selection and money come back, nothing was
        // bought again, and "movement" did not survive (the trip authority is docked and empty, serving the active boat).
        private bool HostFleetReload()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            var trip = adapter.GetComponent<BoatTripManager>();
            if (economy == null || trip == null || adapter.Connection.Status != ConnectionStatus.Connected) return false;
            var host = new PlayerId(0);
            result.fleetReloadBalance = economy.SharedBalance;
            result.fleetReloadOwned = economy.Fleet.OwnedBoatIds.Count;
            result.fleetReloadActive = economy.ActiveVehicleId;
            var state = trip.State;
            result.fleetReloadTripBoat = state.BoatId;
            var before = economy.SharedBalance;
            // No dive yet in this launch: the Reef discovery can only come from the loaded campaign file.
            result.fleetReloadReefKept = adapter.GetComponent<EconomySaveStore>().HasDiscoveredCellInBand(DepthBandIds.Reef);
            result.fleetReloadPass = result.fleetReloadReefKept && economy.Fleet.OwnedBoatIds.SequenceEqual(VehicleIds.All) && economy.ActiveVehicleId == VehicleIds.Motorboat &&
                state.BoatId == VehicleIds.Motorboat && state.Phase == BoatTripPhase.Docked && state.Seats.Count == 0 &&
                economy.TryPurchaseVehicle(host, VehicleIds.ResearchBoat, fleetRequest++).ReasonCode == "AlreadyProcessed" &&
                economy.TryPurchaseVehicle(host, VehicleIds.Motorboat, fleetRequest++).ReasonCode == "AlreadyProcessed" &&
                economy.SharedBalance == before && economy.LoadoutFor(host).Contains(EconomyManager.CameraAdvancedId) &&
                economy.TrySelectVehicle(host, VehicleIds.Motorboat, fleetRequest++).ReasonCode == "AlreadyProcessed";
            result.passed = result.fleetReloadPass && result.errors.Count == 0;
            return true;
        }

        // ---- #106 acceptance (no fixture) ------------------------------------------------------------------
        private int acceptStage, acceptHostStage, acceptBalanceBase;
        private float acceptStageAt, acceptHostAt, acceptDoneAt;
        private ulong acceptRequest = 5000;

        private static ClipSave AcceptRealClip() => MediaNetworkBinding.Mirrored.Clips.Find(c => c != null && !string.IsNullOrEmpty(c.RecordingId));

        private static bool IsRealClip(ClipSave c) =>
            c != null && c.MediaReady && c.SafeReturned && !string.IsNullOrEmpty(c.RecordingId) && c.ClipId == RecordingClipFile.ClipIdForRecording(c.RecordingId) &&
            !string.IsNullOrEmpty(c.SubjectId) && c.Quality >= 1 && c.Quality <= 4 && c.DurationSeconds > 0f && c.SizeBytes > 0 &&
            c.ContentHash != null && c.ContentHash.Length == 64;

        private static bool HasWorldContext(ClipSave c)
        {
            var ctx = c.ToManifest().WorldContext;
            return ctx.Kind == RecordingSubjectKind.Species && !string.IsNullOrWhiteSpace(ctx.RegionId) &&
                   !string.IsNullOrWhiteSpace(ctx.CellId) && !string.IsNullOrWhiteSpace(ctx.DepthBandId);
        }

        private bool AcceptanceLobbyReady() =>
            adapter.IsAuthority && returnPhaseAt > 0 && Time.realtimeSinceStartup - returnPhaseAt > 6f && result.recordingSafe && AcceptRealClip() != null;

        private void AcceptAsk(string clipId, string title, int next)
        {
            mediaAwait = MediaNetworkBinding.RequestPublish(clipId, title);
            acceptStage = next;
        }

        private bool AcceptAnswered(out bool accepted, out string reason)
        {
            accepted = MediaNetworkBinding.LastResultAccepted;
            reason = MediaNetworkBinding.LastResultReason;
            var answered = mediaAwait != 0 && MediaNetworkBinding.LastResultRequest == mediaAwait;
            if (answered) result.mediaTrace.Add($"accept{acceptStage}:{(accepted ? "ok" : reason)}");
            return answered;
        }

        // Every process, in the lobby (home) after the real dive. The recorder owns the real clip; the other process does not.
        private void ProbeAcceptance(NetworkPlayer local)
        {
            local.SubmitLocalInput(Vector3.zero, 0);
            var now = Time.realtimeSinceStartup;
            if (acceptStageAt == 0) acceptStageAt = now;
            if (acceptStage < 90 && now - acceptStageAt > 70f)
            {
                result.errors.Add($"acceptance stage {acceptStage} timeout clips={MediaNetworkBinding.Mirrored.Clips.Count} pubs={MediaNetworkBinding.Mirrored.Publications.Count} last={MediaNetworkBinding.LastResultReason}");
                acceptStage = 99;
                return;
            }
            var clip = AcceptRealClip();
            var pc = FindFirstObjectByType<HomePcAnchor>();
            if (clip == null || pc == null) return;
            var me = local.OwnerClientId;
            var mine = clip.OwnerPlayerId == me;
            bool ok; string reason;

            switch (acceptStage)
            {
                case 0:
                {
                    result.acceptOwner = mine;
                    result.acceptClipId = clip.ClipId; result.acceptRecordingId = clip.RecordingId; result.acceptHash = clip.ContentHash;
                    result.acceptBytes = clip.SizeBytes; result.acceptSubject = clip.SubjectId; result.acceptQuality = clip.Quality;
                    var ctx = clip.ToManifest().WorldContext;
                    result.acceptRegion = ctx.RegionId; result.acceptCell = ctx.CellId; result.acceptBand = ctx.DepthBandId;
                    result.acceptClipReal = IsRealClip(clip);
                    result.acceptWorldContext = HasWorldContext(clip);
                    acceptStage = mine ? 1 : 2;
                    return;
                }
                case 1:   // owner, still away from the PC: the HOST must refuse
                {
                    var away = local.transform.position - pc.transform.position; away.y = 0;
                    if (away.magnitude < 6f)
                    {
                        if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                        local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg, 0);
                        return;
                    }
                    AcceptAsk(clip.ClipId, "uzaktan", 11);
                    return;
                }
                case 11:
                    if (!AcceptAnswered(out ok, out reason)) return;
                    result.acceptFarRefused = !ok && reason == "NotAtPc";
                    acceptStage = 2; return;
                case 2:   // walk to the PC; the REAL clip must be listed and playable there
                {
                    var to = pc.transform.position - local.transform.position; to.y = 0;
                    if (to.magnitude > 1.9f)
                    {
                        local.SubmitLocalInput(Vector3.forward, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 0);
                        return;
                    }
                    result.acceptPanelOpen = HomePcView.PanelOpen;
                    result.acceptPlayable = ClipPlayback.CanPlay(clip.ClipId);
                    acceptStage = mine ? 3 : 6;
                    return;
                }
                case 3:
                    AcceptAsk(clip.ClipId, "Gercek klip", 4); return;
                case 4:
                    if (!AcceptAnswered(out ok, out reason)) return;
                    result.acceptPublished = ok;
                    if (!ok) result.errors.Add("acceptance publish refused: " + reason);
                    AcceptAsk(clip.ClipId, "tekrar", 5); return;
                case 5:
                    if (!AcceptAnswered(out ok, out reason)) return;
                    result.acceptDuplicateRefused = !ok && reason == "PublicationAlreadyQueued";
                    acceptStage = 7; return;
                case 6:   // not the owner: asking to publish somebody else's clip must be refused
                    AcceptAsk(clip.ClipId, "baskasinin", 61); return;
                case 61:
                    if (!AcceptAnswered(out ok, out reason)) return;
                    result.acceptNotOwnerRefused = !ok && reason == "NotOwner";
                    acceptStage = 7; return;
                case 7:   // after the day closes the owner's post carries its result, in MY mirror as well
                {
                    var pub = MediaNetworkBinding.Mirrored.Publications.Find(p => p.ClipId == clip.ClipId);
                    if (pub == null || string.IsNullOrEmpty(pub.SettledId)) return;
                    result.acceptResultSeen = pub.Views > 0 && pub.Income > 0;
                    result.acceptViews = pub.Views; result.acceptIncome = pub.Income; result.acceptFollowers = MediaNetworkBinding.Mirrored.Followers;
                    acceptStage = 90; return;
                }
            }
        }

        // Host: the one real clip is archived; the NPC still holds the real recording; the owner publishes; the right moves to the
        // channel (NPC queue empty, a second NPC claim refused, no NPC money); everybody goes to bed and the REAL sleep gate closes
        // the day; the result is paid exactly once into the campaign file.
        private void HostAcceptance(SessionState state)
        {
            var binding = adapter.GetComponent<MediaNetworkBinding>();
            var economy = adapter.GetComponent<EconomyManager>();
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (binding == null || economy == null || engine == null || acceptHostStage >= 90) return;
            var now = Time.realtimeSinceStartup;
            var clip = AcceptRealClip();
            if (clip == null) return;
            var owner = new PlayerId(clip.OwnerPlayerId);

            switch (acceptHostStage)
            {
                case 0:   // home: baseline BEFORE anybody publishes
                    if (state.Phase != SessionPhase.Lobby || adapter.Connection.IsSceneLoading) return;
                    acceptBalanceBase = economy.SharedBalance;
                    result.acceptNpcQueuedBefore = economy.PendingCountFor(owner, TurnInKind.Recording) == 1 && !economy.IsChannelClaimed(clip.RecordingId);
                    acceptHostStage = 1; return;
                case 1:
                {
                    var pubs = binding.Channel.Publications();
                    if (pubs.Count < 1) return;
                    result.acceptNpcWithdrawn = economy.PendingCountFor(owner, TurnInKind.Recording) == 0 && economy.IsChannelClaimed(clip.RecordingId);
                    result.acceptNpcRefused = economy.TryQueueRecordingTurnIn(new RecordingResult(clip.RecordingId, clip.DiveId, owner, clip.SubjectId,
                        clip.Quality, clip.DurationSeconds)) == PlayerActionResult.DuplicateRequest;
                    result.acceptNoNpcPay = economy.SharedBalance == acceptBalanceBase && economy.PendingCountFor(owner, TurnInKind.Recording) == 0;
                    acceptHostAt = now; acceptHostStage = 2; return;
                }
                case 2:   // let the owner's duplicate request land, then everybody sleeps: the real gate closes the day
                {
                    if (now - acceptHostAt < 6f || engine.Phase != DayPhase.Running || engine.State.ActivePlayers.Count < result.maxPlayers) return;
                    result.acceptDayBefore = engine.DayNumber;
                    var bed = 0;
                    foreach (var id in engine.State.ActivePlayers) HomeBedInteraction.TryEnterBed(id, DayIds.Beds[bed++], acceptRequest++);
                    acceptHostAt = now; acceptHostStage = 3; return;
                }
                case 3:
                {
                    if (engine.DayNumber <= result.acceptDayBefore)
                    {
                        if (now - acceptHostAt > 15f) { result.errors.Add("acceptance: day did not close when everybody slept"); acceptHostStage = 99; }
                        return;
                    }
                    var pubs = binding.Channel.Publications();
                    result.acceptDayAfter = engine.DayNumber;
                    var income = 0;
                    foreach (var p in pubs) income += p.Income;
                    result.acceptBalance = economy.SharedBalance;
                    result.acceptPaidOnce = pubs.Count == 1 && income > 0 && economy.SharedBalance - acceptBalanceBase == income &&
                        engine.DayNumber == result.acceptDayBefore + 1 && !string.IsNullOrEmpty(pubs[0].SettledId);
                    result.acceptReplayPaysNothing = binding.Channel.SettleThrough(result.acceptDayBefore) == 0 && economy.SharedBalance == result.acceptBalance;

                    var store = adapter.GetComponent<EconomySaveStore>();
                    var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
                    result.acceptExploreObs = disk.HasExploration ? disk.Exploration.Observations.Count : 0;
                    result.acceptSavedOnDisk = disk.HasMedia && disk.Media.Clips.Count == 1 && disk.Media.Publications.Count == 1 &&
                        disk.Media.Clips[0].ClipId == clip.ClipId && !string.IsNullOrEmpty(disk.Media.Publications[0].SettledId) &&
                        disk.SharedBalance == economy.SharedBalance && disk.ChannelRightIds.Contains(clip.RecordingId) &&
                        disk.ChannelSettleIds.Count == 1 && disk.HasDay && disk.Day.DayNumber == engine.DayNumber;
                    var balance = economy.SharedBalance;
                    result.acceptReloadClean = store.LoadNow() && binding.Channel.Publications().Count == 1 && binding.Channel.ClipCount == 1 &&
                        economy.SharedBalance == balance && binding.Channel.SettleThrough(result.acceptDayBefore + 5) == 0;
                    result.acceptHostDone = true;
                    acceptDoneAt = now; acceptHostStage = 90; return;
                }
            }
        }

        // Second launch of the host on the SAME campaign file. The report is compared by the script with the first run's.
        private bool HostAcceptanceReload()
        {
            var binding = adapter.GetComponent<MediaNetworkBinding>();
            var economy = adapter.GetComponent<EconomyManager>();
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (binding == null || economy == null || engine == null || adapter.Connection.Status != ConnectionStatus.Connected || binding.Channel.ClipCount == 0) return false;
            var clips = binding.Channel.Clips();
            var pubs = binding.Channel.Publications();
            var clip = clips.Count > 0 ? ClipSave.From(clips[0]) : null;
            result.acceptReloadClips = clips.Count;
            result.acceptReloadPublications = pubs.Count;
            result.acceptReloadBalance = economy.SharedBalance;
            result.acceptReloadDay = engine.DayNumber;
            result.acceptReloadFollowers = binding.Channel.Followers;
            if (clip != null)
            {
                result.acceptReloadClipId = clip.ClipId; result.acceptReloadRecordingId = clip.RecordingId;
                result.acceptReloadHash = clip.ContentHash; result.acceptReloadBytes = clip.SizeBytes;
                result.acceptReloadPlayable = ClipPlayback.CanPlay(clip.ClipId);
            }
            if (pubs.Count > 0) { result.acceptReloadIncome = pubs[0].Income; result.acceptReloadViews = pubs[0].Views; }
            var store = adapter.GetComponent<EconomySaveStore>();
            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
            result.acceptReloadExploreObs = disk.HasExploration ? disk.Exploration.Observations.Count : 0;
            var before = economy.SharedBalance;
            var owner = clip != null ? new PlayerId(clip.OwnerPlayerId) : new PlayerId(0);
            result.acceptReloadPass = clip != null && IsRealClip(clip) && HasWorldContext(clip) && clips.Count == 1 && pubs.Count == 1 &&
                !string.IsNullOrEmpty(pubs[0].SettledId) && pubs[0].Views > 0 && pubs[0].Income > 0 && binding.Channel.Followers > 0 &&
                economy.IsChannelClaimed(clip.RecordingId) && economy.PendingCountFor(owner, TurnInKind.Recording) == 0 &&
                result.acceptReloadPlayable &&
                binding.Channel.SettleThrough(99) == 0 && economy.SharedBalance == before &&
                binding.Channel.TryPublish(owner, clip.ClipId, "x", 777).ReasonCode == "PublicationAlreadyQueued" &&
                economy.TryQueueRecordingTurnIn(new RecordingResult(clip.RecordingId, clip.DiveId, owner, clip.SubjectId, clip.Quality, clip.DurationSeconds)) == PlayerActionResult.DuplicateRequest;
            result.passed = result.acceptReloadPass && result.errors.Count == 0;
            return true;
        }

        private void HostMediaChecks()
        {
            if (mediaHostClosed) return;
            var binding = adapter.GetComponent<MediaNetworkBinding>();
            var engine = adapter.GetComponent<DayNetworkBinding>()?.Engine;
            if (binding == null || engine == null || binding.Channel.Publications().Count < result.maxPlayers) return;
            mediaHostClosed = true;
            var economy = adapter.GetComponent<EconomyManager>();
            result.mediaNpcWithdrawn = economy.PendingCountFor(new PlayerId(0), TurnInKind.Recording) == 0 &&
                economy.IsChannelClaimed("rec-media-0");

            var before = economy.SharedBalance;
            var day = engine.DayNumber;
            engine.BeginClose(DayCloseReason.Midnight);
            var expected = 0;
            foreach (var p in binding.Channel.Publications()) expected += p.Income;
            result.mediaPaid = economy.SharedBalance - before;
            result.mediaPaidOnce = expected > 0 && result.mediaPaid == expected && engine.DayNumber == day + 1;
            result.mediaReplayPaysNothing = binding.Channel.SettleThrough(day) == 0 && economy.SharedBalance - before == expected;

            var store = adapter.GetComponent<EconomySaveStore>();
            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(store.SavePath));
            result.mediaSavedOnDisk = disk.HasMedia && disk.Media.Publications.Count == result.maxPlayers &&
                disk.Media.Publications.TrueForAll(p => !string.IsNullOrEmpty(p.SettledId)) && disk.SharedBalance == economy.SharedBalance;
            var balance = economy.SharedBalance;
            result.mediaReloadClean = store.LoadNow() && binding.Channel.Publications().Count == result.maxPlayers &&
                economy.SharedBalance == balance && binding.Channel.SettleThrough(day + 5) == 0;
        }

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
            if (FleetRoute)
            {
                var economySync = local.GetComponent<EconomyPlayerSync>();
                if (economySync == null) { local.SubmitLocalInput(Vector3.zero, 0); return; }
                var expectedMask = TripVehicleId == VehicleIds.ResearchBoat ? 4 : TripVehicleId == VehicleIds.Motorboat ? 2 : 1;
                if ((economySync.FleetOwnedMask.Value & expectedMask) == 0)
                {
                    if (adapter.IsAuthority && !result.errors.Contains($"fleet-route campaign does not own {TripVehicleId}"))
                        result.errors.Add($"fleet-route campaign does not own {TripVehicleId}");
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
                }
                if (economySync.ActiveVehicleId.Value.ToString() != TripVehicleId)
                {
                    // Active selection must use the real harbor vendor session, just like the player UI.
                    if (adapter.IsAuthority)
                    {
                        if (!economySync.VendorOpen)
                        {
                            var camera = local.GetComponentInChildren<Camera>(true);
                            if (camera != null && GoToService(local, camera, TownServiceCatalog.VehicleVendorId))
                                InteractEvery(local, 1.2f);
                            return;
                        }
                        if (Time.realtimeSinceStartup >= tripNext)
                        {
                            tripNext = Time.realtimeSinceStartup + 1.2f;
                            economySync.RequestSelectVehicle(TripVehicleId);
                        }
                    }
                    local.SubmitLocalInput(Vector3.zero, 0);
                    return;
                }
                result.tripFleetVehicleReady = true;

                var expectedHull = TripVehicleId == VehicleIds.ResearchBoat ? BoatHullKind.ResearchVessel :
                    TripVehicleId == VehicleIds.Motorboat ? BoatHullKind.Motorboat : BoatHullKind.Rowboat;
                var localPresentation = FindObjectsByType<BoatHullPresentation>(FindObjectsSortMode.None)
                    .FirstOrDefault(p => p.gameObject.name == "P3BoatVisual");
                var mirrorCorrect = (BoatHullKind)sync.HullKind.Value == expectedHull &&
                    localPresentation != null && localPresentation.PresentedHull == expectedHull;
                if (adapter.IsAuthority)
                {
                    var physical = FindFirstObjectByType<NetworkBoatController>();
                    var hostPhysicalCorrect = physical != null && physical.BoatId == TripVehicleId &&
                        physical.HullKind == expectedHull;
                    result.tripHullKindCorrect |= mirrorCorrect && hostPhysicalCorrect;
                }
                else result.tripHullKindCorrect |= mirrorCorrect;
            }
            var phase = (BoatTripPhase)sync.Phase.Value;
            var phaseName = phase.ToString();
            if (sync.BoatVisible.Value && tripLastPhase != phaseName)
            { tripLastPhase = phaseName; result.tripPhases.Add(phaseName); }
            SampleTripMap(sync, phase);

            var owner = sync.AmOwner.Value;
            var now = Time.realtimeSinceStartup;
            if (tripStepAt == 0) tripStepAt = now;
            var tripStepTimeout = FleetRoute && TripRouteId == BoatTripIds.DeepRouteId ? 75f : FleetRoute ? 50f : 32f;
            if (tripStep < 90 && now - tripStepAt > tripStepTimeout)
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
                    var stand = FleetRoute
                        ? new Vector3(sync.BoatWorldPosition.Value.x + (adapter.IsAuthority ? -0.35f : 0.35f), dock.BoardingPosition.y, sync.BoatWorldPosition.Value.z - 2.5f)
                        : new Vector3(dock.BoardingPosition.x + (adapter.IsAuthority ? -0.4f : 0.4f), dock.BoardingPosition.y, -3.6f);
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
                    if (owner && phase == BoatTripPhase.Docked && now >= tripNext) { tripNext = now + 1.5f; sync.RequestStartRouteLocal(TripRouteId); }
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
                else if (icon.IconId == (FleetRoute ? TripVehicleId : BoatTripIds.BoatId)) { boat = icon; hasBoat = true; }
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
            var targetAnchorId = DiveRouteAnchors.AnchorPoint;
            if (FleetRoute && DiveRoutePath.TryGetDefinition(TripRouteId, out var fleetDefinition))
                targetAnchorId = fleetDefinition.AnchorPointAnchor;
            var seaAnchor = anchors.FirstOrDefault(a => a.AnchorId == targetAnchorId);
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
            // The harbor vendor is the last NPC of the row and the host is already standing in front of it when the guest arrives
            // from the west: the guest takes the west side there, otherwise it would have to walk THROUGH the host.
            var lateral = anchor.transform.right * (adapter.IsAuthority ? 0f : serviceId == TownServiceCatalog.VehicleVendorId ? -0.9f : 0.9f);
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

            // Standing on dry ground that is flush with the platform but is not it - the return pad on Shore_Ledge, z -11..-7 - the
            // next step is straight onto the platform (north, -z). Sending it to the wade lane instead walks it WEST along the ledge
            // and off the ramp into the water beside the shelf, where it pressed against the shelf's side wall for the rest of the
            // run and never sold (measured, recorder stopped at (-3.1, 7.9, -10.6)).
            if (position.y >= strip.bounds.max.y - 0.35f) return new Vector3(position.x, position.y, northEdge - 0.6f);

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
                if (local.OwnerClientId == recorder && sync.PendingRecordings.Value > 0 && !Acceptance)   // acceptance: the channel, not the NPC, takes this recording
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
