using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Media;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Trip;
using DeepDive.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P1.Lab
{
    // Explicit development-player command only. All mutations below are owner input/RPCs;
    // no stock, money, camera, clip, clock, sleep or position fixture is installed.
    // Capture after RepairCarryView (200) applies visual hand IK. An earlier camera
    // request can observe the locomotion pose between the two cosmetic updates.
    [DefaultExecutionOrder(500)]
    public sealed partial class ProductDaySmokeDriver : MonoBehaviour
    {
        private const string StateMessage = "test/p46/product-state";
        private const string DoneMessage = "test/p46/product-done";
        [Serializable] public sealed class Report
        {
            public bool passed, reload, initialClean, homeSpawn, repaired, cameraBought, realClip,
                clipPlayed, clipAdvanced, published, duplicateRefused, slept, morningHome, reloadClean, mapMoved, mapDocked;
            public int slot, players, step, worldLoads, diveCount, soldCount, day, balance, income, boatParts, mapPlayers;
            public bool observedCarry, observedPlaced, cancelRecovered;
            public bool repairRace, repairDisconnect, repairRejoin, repairMidnight;
            public bool cameraHudRecording, cameraHudTimerAdvanced, cameraHudStopped;
            public bool purchasedMaterial, purchaseDidNotRepair, saveFaultRefused, saveFaultRecovered;
            public int repairVideoFrames;
            public bool repairArtSeen, repairToolContactSeen, repairGripVerified;
            public float minRepairGripError=999f;
            public int repairRejected;
            // #188 hunt-first: parts state when the first catch was sold (-1 = not observed).
            public bool huntFirst;
            public int partsAtFirstSale = -1;
            // Bed step diagnostics: what the local eye ray met when the one-shot request left, and the home answer.
            public bool inBed;
            public string bedRay = "", bedAnswer = "";
            public string clipId = "", recordingId = "", hash = "", position = "", failure = "";
            public long clipBytes;
            public float distance, maxTravelStep;
            public double maxMotionSampleGap;
            public int motionTrackingSegments;
            public bool runnerAbortReceived, injectedFailure;
            public bool equipmentLoweredSeen,equipmentRaisedSeen,equipmentCancelled,unownedCameraRefused;
            public int equipmentVideoFrames;
            public bool oceanReady, oceanReflection, oceanAnimated, oceanSurfaceEye;
            public int oceanBands, oceanVideoFrames;
            public string oceanGpu="";
            public List<MotionSample> motionSamples=new List<MotionSample>();
            public List<string> trace = new List<string>(), tripPhases = new List<string>();
            public List<ulong> fishIds = new List<ulong>();
            public List<ulong> primaryFishIds = new List<ulong>();
            public List<string> fullyPopulatedDives = new List<string>();
            public List<string> errors = new List<string>();
        }
        private readonly Report report = new Report();
        private readonly HashSet<ulong> done = new HashSet<ulong>();
        private readonly HashSet<string> diveIds = new HashSet<string>(), mapPositions = new HashSet<string>();
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private NetworkPlayer local;
        private EconomyPlayerSync sync;
        private BoatTripPlayerSync trip;
        private List<Vector3> route;
        private string output;
        private int expected, step, substep, waypoint, part, firstDay;
        private bool localDone, finished, leaving, homeChecked, engineEntered, partRoute;
        private float deadline, stageDeadline, nextBroadcast, nextAction, nextTrace, aimAt, recordingAt, finishAt, moveAfter, playbackAt;
        private Vector3 previous;
        private bool wasSeated, tracked;
        private int previousDay;
        private ulong publishRequest;
        private Report baseline;

        private bool Host => report.slot == 0;
        private bool repairApproach, repairAtDock, repairExit;
        private bool RepairOnly => Arg("-p187-repair-only") == "1";
        // #188: catch and sell first, repair afterwards in the same day (the repair must not gate the first catch).
        private bool HuntFirst => Arg("-p188-hunt-first") == "1";
        private bool sawCarry, sawPlaced;
        private bool cancelSent;
        private bool cancelObserved;
        private ulong repairObservedRequest;
        private FishActor recordingFish;
        private float nextHuntTrace;
        private static string Arg(string key, string fallback = "")
        {
            var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p46-day-role").Length == 0) return;
            var go = new GameObject(nameof(ProductDaySmokeDriver)); DontDestroyOnLoad(go);
            go.AddComponent<ProductDaySmokeDriver>();
        }
        private IEnumerator Start()
        {
            output = Arg("-p1-report"); expected = int.Parse(Arg("-p46-day-count", "1"));
            report.slot = int.Parse(Arg("-p46-day-slot", "0"));
            report.reload = Arg("-p46-day-reload") == "1";
            if (report.reload) baseline = JsonUtility.FromJson<Report>(File.ReadAllText(Arg("-p46-day-baseline")));
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            // Opt-in quiet run: the smoke checks no audio, so local runs need not play the game's sound.
            if (Arg("-p46-mute") == "1") AudioListener.volume = 0f;
            if (HuntFirst && RepairOnly) { Fail("-p188-hunt-first and -p187-repair-only are exclusive"); yield break; }
            deadline = Time.realtimeSinceStartup + 1000f;
            SceneManager.sceneLoaded += SceneLoaded;
            Application.logMessageReceived += Log;
            yield return null; yield return null;
            adapter = FindFirstObjectByType<SessionNetworkAdapter>(); manager = adapter.GetComponent<NetworkManager>();
            var port = ushort.Parse(Arg("-p46-day-port", "24777"));
            if (!(Host ? adapter.CreateRoom(port) : adapter.JoinRoom("127.0.0.1", port))) Fail("Could not start session");
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == ContinuousTownLayout.SceneName) report.worldLoads++;
        }
        private void Update()
        {
            if (finished) return;
            if (!string.IsNullOrEmpty(output) && File.Exists(output+".abort"))
            { report.runnerAbortReceived=true; Fail("Runner aborted: "+File.ReadAllText(output+".abort").Trim()); return; }
            if (adapter == null) return;
            var now = Time.realtimeSinceStartup;
            if (now > deadline || (step > 0 && now > stageDeadline))
            { Fail($"Timeout step={step} sub={substep} waypoint={waypoint} position={report.position}{BedState()}"); return; }
            if (leaving)
            { if (adapter.Connection.Status == ConnectionStatus.Offline) Finish(); return; }
            if (RepairInterruptions && RepairReconnect(now)) return;
            if (adapter.Connection.Status != ConnectionStatus.Connected || adapter.Connection.IsSceneLoading) return;
            if (messages == null && manager.CustomMessagingManager != null)
            {
                messages = manager.CustomMessagingManager;
                messages.RegisterNamedMessageHandler(StateMessage, ReceiveState);
                if (Host) messages.RegisterNamedMessageHandler(DoneMessage, ReceiveDone);
            }
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).ToArray();
            report.players = Math.Max(report.players, players.Length);
            local = players.FirstOrDefault(p => p.IsOwner);
            if (local == null) return;
            local.ReadKeyboard = false;
            sync = local.GetComponent<EconomyPlayerSync>(); trip = local.GetComponent<BoatTripPlayerSync>();
            if (adapter.Session.State.Phase == SessionPhase.Lobby && step < 14 &&
                adapter.Session.Roster.TryGetValue(new PlayerId(local.OwnerClientId), out var ready) && !ready)
                adapter.ToggleReady();
            if (step == 0)
            {
                if (Host && adapter.Session.State.Phase == SessionPhase.Lobby && AllReady()) adapter.AdvancePhase();
                if (SceneManager.GetActiveScene().name != ContinuousTownLayout.SceneName) return;
                if (!homeChecked)
                {
                    if (Vector3.Distance(local.transform.position, ContinuousTownLayout.HomeRig.Spawn(report.slot)) > 0.7f) return;
                    homeChecked = true; report.homeSpawn = true; firstDay = sync.DayNumber.Value;
                    BeginMotionSegment();
                    if (Host)
                    {
                        var economy = adapter.GetComponent<EconomyManager>();
                        report.initialClean = economy.SharedBalance == 0 && economy.BoatRepair.CompletedPartIds.Count == 0 &&
                            !economy.LoadoutFor(new PlayerId(0)).Contains(EconomyManager.CameraBasicId);
                        if (!report.reload && !report.initialClean) { Fail("Starting campaign is not fresh/zero"); return; }
                    }
                }
                if (report.reload)
                {
                    if (RepairOnly)
                    {
                        var economy=adapter.GetComponent<EconomyManager>();
                        report.reloadClean=economy.BoatRepair.CompletedPartIds.Count==3 && economy.UnusedBoatParts.Count==0 &&
                            economy.SharedBalance==baseline.balance && sync.DayNumber.Value==baseline.day &&
                            economy.ExportSaveData("read-only","").SoldCaptureIds.Count==baseline.soldCount;
                        if(!report.reloadClean){Fail("Fresh host did not restore the exact repaired campaign");return;}
                        Snapshot();Finish();
                    }
                    else ReloadTick();
                    return;
                }
                localDone = true;
            }
            if(!ResumeMotionAfterRejoin())return;
            Track();
            if (finished) return;
            Observe(); Broadcast(now);
            if (step==1 && Arg("-p46-fault-slot")==report.slot.ToString())
            { report.injectedFailure=true; Fail("Injected QA peer failure"); return; }
            foreach (var peer in players)
            { sawCarry |= peer.RepairHandsBusy && !peer.RepairPlaced.Value; sawPlaced |= peer.RepairPlaced.Value && peer.RepairHits.Value > 0; }
            if (step==1 && local.LastActionRequestId.Value!=repairObservedRequest)
            { repairObservedRequest=local.LastActionRequestId.Value; if(local.LastActionResult.Value!=(int)PlayerActionResult.Accepted)report.repairRejected++; }
            if (step == 16) { Snapshot(); if (now > stageDeadline - 167f) Finish(); return; }
            if (now > nextTrace)
            {
                nextTrace = now + 5f;
                Debug.Log($"P46_PRODUCT step={step} sub={substep} wp={waypoint} pos={local.transform.position} " +
                    $"phase={adapter.Session.State.Phase} balance={sync.SharedBalance.Value} parts={sync.BoatPartsMask.Value} part={part} seated={trip.IsSeated} boat={trip.BoatWorldPosition.Value} boatResult={trip.LastReasonCode.Value}");
                if (step == 8 && Host)
                {
                    var subject = FindObjectsByType<RecordingSubject>(FindObjectsSortMode.None).FirstOrDefault(s => s.IsSpawned && s.GetComponent<FishActor>() != null);
                    var bridge = adapter.GetComponent<RecordingNetworkBridge>();
                    var camera = local.GetComponentInChildren<Camera>(true);
                    Physics.Raycast(camera.ViewportPointToRay(new Vector3(.5f, .5f)), out var hit, 35f, ~0, QueryTriggerInteraction.Ignore);
                    Debug.Log($"P46_PRODUCT_REC active={bridge.IsRecordingLocal} held={local.CurrentHeldEquipment} tier={local.CurrentCameraTier} " +
                        $"open={subject?.OpenTakeCount} valid={subject?.ValidSecondsFor(new PlayerId(0))} hit={hit.collider?.name} " +
                        $"status={typeof(RecordingNetworkBridge).GetField("localStatus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bridge)}");
                }
            }
            if (!localDone) Tick();
            else if (step != 14) Stop();
            if (Host) AdvanceHost();
        }
        private void LateUpdate()
        {
            // Step 16 (repair-only) ends the same way: the host quits 3 s after reaching it, which can close a guest's
            // connection before the guest's own 3 s are up. Its repair evidence is already mirrored by then.
            if (!finished && (step == 15 || step == 16) && adapter != null && adapter.Connection.Status == ConnectionStatus.Offline) Finish();
            CaptureRepairVideo();
            CaptureEquipmentVideo();
            CaptureOceanVideo();
        }
        private void Broadcast(float now)
        {
            if (Host && localDone) done.Add(local.OwnerClientId);
            if (now < nextBroadcast || messages == null) return;
            nextBroadcast = now + 0.2f;
            using var writer = new FastBufferWriter(24, Allocator.Temp);
            writer.WriteValueSafe(step);
            if (Host)
            {
                if (RepairInterruptions) { writer.WriteValueSafe(repairPhase); writer.WriteValueSafe(repairWinner); }
                foreach (var id in manager.ConnectedClientsIds)
                    if (id != 0) messages.SendNamedMessage(StateMessage, id, writer, NetworkDelivery.ReliableSequenced);
            }
            else
            {
                writer.WriteValueSafe(localDone);
                messages.SendNamedMessage(DoneMessage, 0, writer, NetworkDelivery.ReliableSequenced);
            }
        }
        private void ReceiveState(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int value);
            if (RepairInterruptions && !Host && sender == 0)
            {
                reader.ReadValueSafe(out int phase); reader.ReadValueSafe(out ulong winner);
                repairWinner = winner;
                SetRepairPhase(phase);
            }
            if (!Host && sender == 0 && value > step) SetStep(value);
        }
        private void ReceiveDone(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int value); reader.ReadValueSafe(out bool completed);
            if (value == step && completed && manager.ConnectedClients.ContainsKey(sender)) done.Add(sender);
        }
        private bool AllReady() => adapter.Session.Roster.Count == expected && adapter.Session.Roster.Values.All(x => x);
        private bool AllDone() => manager.ConnectedClientsIds.Count == expected && manager.ConnectedClientsIds.All(id => done.Contains(id));
        private void SetStep(int value)
        {
            step = value; report.step = value; localDone = false; substep = waypoint = 0;
            route = null; aimAt = recordingAt = 0; publishRequest = 0; done.Clear();
            stageDeadline = Time.realtimeSinceStartup + 170f;
            nextAction = Time.realtimeSinceStartup + report.slot * 0.7f;
            moveAfter = Time.realtimeSinceStartup + report.slot * 2f;
            report.trace.Add($"{value}:{adapter.Session.State.Phase}:{local.transform.position}");
            Debug.Log("P46_PRODUCT_STEP " + report.trace.Last());
        }
        private void AdvanceHost()
        {
            if (!AllDone()) return;
            switch (step)
            {
                case 0: SetStep(1); break;
                case 1:
                    if (RepairOnly) { SetStep(16); break; }
                    adapter.AdvancePhase(); if (adapter.Session.State.Phase == SessionPhase.Dive) SetStep(2); break;
                case 2: SetStep(3); break;
                case 3: adapter.AdvancePhase(); if (adapter.Session.State.Phase == SessionPhase.Return) SetStep(4); break;
                case 4: CycleDive(5); break;
                case 5: SetStep(6); break;
                case 6: adapter.AdvancePhase(); if (adapter.Session.State.Phase == SessionPhase.Return) SetStep(7); break;
                case 7: CycleDive(8); break;
                case 8: SetStep(9); break;
                case 9: adapter.AdvancePhase(); if (adapter.Session.State.Phase == SessionPhase.Return) SetStep(10); break;
                case 10: SetStep(11); break;
                case 11: SetStep(12); break;
                case 12: SetStep(13); break;
                case 13:
                    if (adapter.Session.State.Phase == SessionPhase.Return) adapter.AdvancePhase();
                    if (adapter.Session.State.Phase == SessionPhase.Lobby) SetStep(14);
                    break;
                case 14: SetStep(15); finishAt = Time.realtimeSinceStartup + 2f; break;
                case 15:
                    if (Time.realtimeSinceStartup < finishAt) return;
                    if (!File.Exists(adapter.GetComponent<EconomySaveStore>().SavePath)) { Fail("No automatic campaign save"); return; }
                    leaving = true; adapter.LeaveRoom(); break;
            }
        }
        private void CycleDive(int next)
        {
            var phase = adapter.Session.State.Phase;
            if (phase == SessionPhase.Return || phase == SessionPhase.Prep || (phase == SessionPhase.Lobby && AllReady())) adapter.AdvancePhase();
            if (adapter.Session.State.Phase == SessionPhase.Dive) SetStep(next);
        }
        private void Tick()
        {
            if ((step == 1 || step == 2 || step == 3 || step == 5 || step == 6 || step == 8 || step == 9 || step == 13) && Time.realtimeSinceStartup < moveAfter)
            { Stop(); return; }
            switch (step)
            {
                case 1:
                    if (substep == 0 && !(RepairPurchase && purchaseHomeExited))
                    {
                        if (route == null) SetRoute(Outward());
                        if (!Follow()) return;
                        substep = 1; route = null; purchaseHomeExited = true;
                    }
                    if(OceanVideo && !OceanShore())return;
                    if ((!Host && !RepairOnly) || HuntFirst) { localDone = true; return; }
                    CollectParts(); break;
                case 2: case 5:
                    if(step==2 && OceanVideo && !OceanSurvey())return;
                    if (!Host) { WaitInWater(); return; }
                    Hunt(); break;
                case 3: case 6: case 9:
                    if (route == null) SetRoute(new[] { P(-6.5f, -11.2f), P(0f, -12.5f), P(9f, -12.5f),
                        P(8.2f + (report.slot % 2) * 1.6f, -9.8f + (report.slot / 2) * 1.6f) });
                    if (!Follow()) return;
                    if (!local.GetComponent<InventoryPlayerSync>().SafelyReturned.Value) return;
                    localDone = !local.Swimming.Value; break;
                case 4: case 7:
                    if (!Host) { localDone = true; return; }
                    SellAndBuy(); break;
                case 8:
                    if (!Host) { if(EquipmentVideo)EquipmentObserver();else WaitInWater(); return; }
                    Record(); break;
                case 10: TripOut(); break;
                case 11:
                    Stop();
                    if (trip.IsSeated) { Act(trip.RequestDisembarkLocal); return; }
                    localDone = true; break;
                case 12: TripBack(); break;
                case 13: HomePc(); break;
                case 14: Sleep(); break;
                case 15: Snapshot(); localDone = report.income > 0; break;
            }
        }

        private void CollectParts()
        {
            if (RepairPurchase && !PurchasedRepairAcceptance()) return;
            if (RepairInterruptions && repairPhase < 5 && !RepairAcceptance()) return;
            var names = new[] { "BoatPart_Hull", "BoatPart_Engine", "BoatPart_FuelTank" };
            if (RepairOnly && !RepairPurchase && expected>1 && (!RepairInterruptions || repairPhase == 5))
            {
                var assignment=report.slot;
                while(part<3 && part%Mathf.Min(3,expected)!=assignment)part++;
            }
            if (part >= names.Length)
            {
                if(RepairOnly && expected>1 && sync.BoatPartsDone.Value<3)
                {
                    var park=P(11.2f+report.slot*.6f,-12.5f);
                    var exit=local.transform.position.z > -11.8f ? P(8.35f,-12.5f) : park;
                    var toPark=exit-local.transform.position;toPark.y=0;
                    if(toPark.magnitude>.2f)MoveToward(Around(exit));else Stop();
                    return;
                }
                localDone = sync.BoatPartsDone.Value == 3; return;
            }
            if ((sync.BoatPartsMask.Value & (1 << part)) != 0)
            { part++; aimAt = 0; repairApproach = repairAtDock = false; repairExit = true; route = null; return; }
            var site = BoatRepairNetworkBinding.Instance;
            if (local.RepairHandsBusy)
            {
                if (site == null) return;
                if (!repairAtDock)
                {
                    if (!repairApproach)
                    {
                        SetRoute(RepairApproach(site, part));
                        repairApproach=true;
                    }
                    if (!Follow()) { aimAt=0; return; }
                    repairAtDock=true;route=null;
                }
                var physicalStand=site.StandPosition(part);physicalStand.y=ContinuousTownLayout.WalkSurfaceY;
                var toStand=physicalStand-local.transform.position;toStand.y=0;
                // Q/recovery can leave repairAtDock cached while the carrier is at
                // the shared rack on the opposite end. Walk round the solid hull.
                if(toStand.magnitude>.2f){MoveToward(Around(physicalStand));aimAt=0;return;}
                Aim(site.SlotPosition(part));
                if (!Settled()) return;
                if(RepairOnly && !cancelSent && !local.RepairPlaced.Value)
                { Act(()=>{cancelSent=true;local.SubmitRepairCancelLocal();}); return; }
                if(cancelObserved)report.cancelRecovered=true;
                if (!local.RepairPlaced.Value) Act(local.SubmitPickupLocal);
                else
                {
                    if (local.RepairHits.Value==1 && Host && !File.Exists(output+".repair.png")) CaptureView(".repair.png");
                    if (RepairPurchase && Host && part==0 && local.RepairHits.Value==2 && !report.saveFaultRefused)
                    { RefuseFinalStrokeOnDisk(); return; }
                    Act(local.SubmitRepairHammerLocal);
                }
                return;
            }
            if(site!=null && (sync.BoatMaterialsMask.Value&(1<<part))!=0)
            {
                if(cancelSent)cancelObserved=true;
                if(!repairAtDock)
                {
                    if(!repairApproach)
                    {SetRoute(RepairApproach(site, part));repairApproach=true;}
                    if(!Follow())return;
                    repairAtDock=true;route=null;
                }
                var rack=site.RackPosition(part);
                var rackStand=rack+Vector3.right*.65f;rackStand.y=ContinuousTownLayout.WalkSurfaceY;
                var rackDelta=rackStand-local.transform.position;rackDelta.y=0;
                if(rackDelta.magnitude>.2f){MoveToward(Around(rackStand));aimAt=0;return;}
                Aim(rack);if(Settled())Act(local.SubmitPickupLocal);return;
            }
            if (repairExit)
            {
                // From the beach cradle (#188) the next leg starts on dry sand already; from the berth, walk back off the pier.
                if (route==null) SetRoute(site != null && site.OnCradle ? new Vector3[0] : new[]{P(9f,-9f),P(9f,-12.5f)});
                if (!Follow()) return;
                repairExit=false;route=null;
            }
            var target = GameObject.Find(names[part]).transform.position;
            var surface = FindFirstObjectByType<SwimVolume>().GetComponent<Collider>().bounds.max.y;
            var submerged = target.y < surface - 0.1f;
            // #188: the engine now rests on the dry beach; take the water entry route only for a part that is really under the surface.
            if (submerged && !engineEntered)
            {
                if (!partRoute)
                {
                    SetRoute(new[] { P(-6.5f, -11.2f), new Vector3(-6.5f, 6f, -5f) });
                    partRoute = true;
                }
                if (!Follow(true)) return;
                engineEntered = true; route = null;
            }
            var stand = submerged ? target + Vector3.forward * 1.8f : target;
            if (submerged)
            {
                var eyeHeight = local.GetComponentInChildren<Camera>(true).transform.position.y - local.transform.position.y;
                stand.y = Mathf.Min(stand.y, surface - eyeHeight - 0.1f);
            }
            if (Vector3.Distance(local.transform.position, stand) > (submerged ? 0.45f : 1.3f))
            { MoveToward(submerged ? stand : CoastTarget(Around(stand))); aimAt = 0; return; }
            Aim(target + Vector3.up * 0.3f);
            if (Settled()) Act(local.SubmitPickupLocal);
        }
        private void WaitInWater()
        {
            // Guests reach the wade together with the host, so each takes its own entry point east of the host's
            // (-6.5) entry and shore hold (-7.6) instead of all pressing on one waypoint (#188; 4p jam 29 Oct, #155).
            var entryX = -6.5f + report.slot * 0.9f;
            if (route == null) SetRoute(new[] { P(9f, -12.5f), P(-6.5f, -11.2f), new Vector3(entryX, 6f, -5f),
                new Vector3(1f + report.slot * 1.2f, 5.3f, -4f) });
            localDone = Follow(true);
        }
        private void Hunt()
        {
            var inventory = adapter.GetComponent<InventoryManager>();
            if (inventory.Bags.TryGetValue(new PlayerId(0), out var bag) && bag.Items.Count > 0)
            { HoldForShore(); return; }
            if (substep == 0)
            {
                if (route == null) SetRoute(new[] { P(9f, -12.5f), P(-6.5f, -11.2f), new Vector3(-6.5f, 6f, -5f) });
                if (!Follow(true)) return;
                substep = 1;
            }
            var eye=local.RecordingEyePosition;
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None)
                .Where(f=>f.IsSpawned && (!f.IsDead || f.GetComponent<CatchObject>().IsAvailable))
                .Where(f=>!Physics.Linecast(eye,f.GetComponent<Collider>().bounds.center,out var blocker,~0,QueryTriggerInteraction.Ignore) ||
                    blocker.collider.GetComponentInParent<FishActor>()==f)
                .OrderByDescending(f=>f.GetComponent<CatchObject>().IsAvailable)
                .ThenBy(f=>(f.transform.position-eye).sqrMagnitude).FirstOrDefault();
            if (Time.realtimeSinceStartup >= nextHuntTrace)
            {
                nextHuntTrace = Time.realtimeSinceStartup + 5f;
                Debug.Log($"P46_PRODUCT_HUNT fish={fish?.name} target={fish?.GetComponent<Collider>().bounds.center} " +
                    $"available={fish?.GetComponent<CatchObject>().IsAvailable} eye={eye} " +
                    $"lastAction={local.LastActionRequestId.Value}/{local.LastActionResult.Value} " +
                    $"swimming={local.Swimming.Value} passive={local.Passive.Value}");
            }
            if (fish == null) { Stop(); return; }
            local.SetHeldEquipmentLocal(HeldEquipmentMode.Harpoon);
            var available = fish.GetComponent<CatchObject>().IsAvailable;
            var target = fish.GetComponent<Collider>().bounds.center;
            var delta = target - local.GetComponentInChildren<Camera>(true).transform.position;
            // The real E ray reaches 2.5 m. Stop with a margin inside that range rather than
            // forcing the body to swim underneath a low catch until its eye is only 1.7 m away.
            // Match the actual eye offset and keep the root above the shallow seabed.
            if (available && delta.magnitude > 2.2f)
            {
                var stand = target - (eye - local.transform.position);
                stand.y = Mathf.Max(0.1f, stand.y);
                MoveToward(stand); aimAt = 0; return;
            }
            Aim(target);
            if (Settled())
            {
                if (available) Act(local.SubmitPickupLocal);
                else Act(local.SubmitHarpoonLocal);
            }
        }
        private void SellAndBuy()
        {
            var economy = adapter.GetComponent<EconomyManager>();
            if (substep == 0)
            {
                if (!GoToService(TownServiceCatalog.FishBuyerId)) return;
                if (sync.PendingCatches.Value > 0) { Act(local.SubmitServiceInteractionLocal); return; }
                var sold = economy.ExportSaveData("read-only", "read-only").SoldCaptureIds.Count;
                if (sold < (step == 4 ? 1 : 2)) { Act(local.SubmitServiceInteractionLocal); return; }
                report.soldCount = sold;
                substep = 1; aimAt = 0;
                if (step == 4 && HuntFirst)
                {
                    report.partsAtFirstSale = sync.BoatPartsDone.Value;
                    substep = 2; stageDeadline = Time.realtimeSinceStartup + 170f;
                }
                if (step == 4 && !HuntFirst) { localDone = true; return; }
            }
            if (substep == 2)
            {
                // Town -> beach on the same legs Outward() ends with, then the normal part/repair walk.
                if (route == null)
                {
                    var cradle = BoatRepairNetworkBinding.Instance;
                    var x = cradle != null && cradle.Blocking ? -1.8f - report.slot * 1.1f : -1.8f + report.slot * 1.1f;
                    SetRoute(new[] { P(0f, -24f), P(x, -16f), P(x, -13f) });
                }
                if (!Follow()) return;
                route = null; substep = 3;
            }
            if (substep == 3) { CollectParts(); return; }
            if (!GoToService(TownServiceCatalog.EquipmentShopId)) return;
            if (!sync.ShopOpen) { Act(local.SubmitServiceInteractionLocal); return; }
            if (!economy.LoadoutFor(new PlayerId(0)).Contains(EconomyManager.CameraBasicId))
            { Act(() => sync.RequestPurchase(EconomyManager.CameraBasicId)); return; }
            report.cameraBought = true; localDone = true;
        }
        private void Record()
        {
            var bridge = adapter.GetComponent<RecordingNetworkBridge>();
            if (recordingAt > 0 && !bridge.IsRecordingLocal &&
                adapter.GetComponent<RecordingWorldBinding>().Binding.Director.ClaimCount > 0)
            {
                report.cameraHudStopped = bridge.RecordingElapsedSeconds == 0;
                if (!File.Exists(output + ".camera-standby.png")) CaptureView(".camera-standby.png");
                HoldForShore(); return;
            }
            if (substep == 0)
            {
                if (route == null) SetRoute(new[] { P(9f, -12.5f), P(-6.5f, -11.2f), new Vector3(-6.5f, 6f, -5f) });
                if (!Follow(true)) return;
                substep = 1;
            }
            if(EquipmentVideo && !EquipmentSwitchProbe())return;
            if(recordingFish==null || !recordingFish.IsSpawned || recordingFish.IsDead ||
                (!bridge.IsRecordingLocal && !ClearFishLine(recordingFish)))
                recordingFish=FindObjectsByType<FishActor>(FindObjectsSortMode.None)
                    .Where(f=>f.IsSpawned && !f.IsDead && ClearFishLine(f))
                    .OrderBy(f=>(f.transform.position-local.RecordingEyePosition).sqrMagnitude).FirstOrDefault();
            var fish=recordingFish;
            if (fish == null) { Stop(); return; }
            var delta = fish.transform.position - local.RecordingEyePosition;
            // Start in the real basic-camera band before closing further on a moving subject.
            // The fish need not reach an exact 8 m approach distance to accept an actual take.
            if (delta.magnitude > 10f)
            {
                var stand=fish.transform.position-Vector3.up*1.4f;stand.y=Mathf.Max(.1f,stand.y);
                MoveToward(stand);aimAt=0;return;
            }
            Aim(fish.transform.position);
            if (bridge.IsRecordingLocal)
            {
                report.cameraHudRecording = true;
                report.cameraHudTimerAdvanced |= bridge.RecordingElapsedSeconds >= 2f;
                if (bridge.RecordingElapsedSeconds >= 3f && !File.Exists(output + ".camera-rec.png"))
                    CaptureView(".camera-rec.png");
                if (recordingAt == 0) recordingAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - recordingAt > 12f) Act(bridge.ToggleRecordingLocal);
            }
            else if (Settled()) { recordingAt = 0; Act(bridge.ToggleRecordingLocal); }
        }
        private bool ClearFishLine(FishActor fish) =>
            !Physics.Linecast(local.RecordingEyePosition,fish.GetComponent<Collider>().bounds.center,out var blocker,~0,QueryTriggerInteraction.Ignore) ||
            blocker.collider.GetComponentInParent<FishActor>()==fish;
        private void HoldForShore()
        {
            var stand = new Vector3(-7.6f, 5.5f, -5f);
            if (Vector3.Distance(local.transform.position, stand) > 0.45f) { MoveToward(stand); return; }
            Stop(); localDone = true;
        }
        private void TripOut()
        {
            if (substep == 0)
            {
                if (route == null) SetRoute(new[] { P(9f, -12.5f), P(9f, -9f),
                    P(8.6f + (report.slot % 2) * 0.8f, -3.6f) });
                if (!Follow()) return;
                substep = 1;
            }
            Stop();
            var phase = (BoatTripPhase)trip.Phase.Value;
            if (!trip.IsSeated) { Act(trip.RequestBoardNearestLocal); return; }
            if (trip.SeatedCount.Value < expected) return;
            if (phase == BoatTripPhase.Anchored) { localDone = true; return; }
            if (phase == BoatTripPhase.Docked && trip.AmOwner.Value) Act(() => trip.RequestStartRouteLocal(BoatTripIds.NearRouteId));
        }
        private void TripBack()
        {
            Stop();
            var phase = (BoatTripPhase)trip.Phase.Value;
            if (phase == BoatTripPhase.Anchored)
            {
                if (!trip.IsSeated) { Act(trip.RequestBoardNearestLocal); return; }
                if (trip.SeatedCount.Value == expected && trip.AmOwner.Value) Act(trip.RequestReturnLocal);
                return;
            }
            if (phase == BoatTripPhase.Docked && report.tripPhases.Contains("Inbound"))
            {
                if (trip.IsSeated) { Act(trip.RequestDisembarkLocal); return; }
                localDone = trip.SeatedCount.Value == 0;
            }
        }
        private void HomePc()
        {
            if (route == null) SetRoute(new[] { P(9f, -9f), P(9f, -12.5f), P(0f, -12.5f), P(0f, -16f),
                P(0f, -24f), P(0f, -36f), ContinuousTownLayout.HomeRig.Door, H(4.95f, 5f),
                H(4.95f, -2.5f), H(2f + report.slot * 0.8f, -2.4f) });
            if (!Follow()) return;
            Stop();
            if (!HomePcView.PanelOpen) return;
            var clip = MediaNetworkBinding.Mirrored.Clips.FirstOrDefault(c => c != null && c.MediaReady && c.SafeReturned);
            if (clip == null) return;
            report.realClip = clip.SizeBytes > 0 && clip.ContentHash.Length == 64 && clip.RecordingId.Length > 0 &&
                clip.ClipId == RecordingClipFile.ClipIdForRecording(clip.RecordingId);
            report.clipId = clip.ClipId; report.recordingId = clip.RecordingId; report.hash = clip.ContentHash; report.clipBytes = clip.SizeBytes;
            if (!Host) { localDone = report.realClip; return; }
            if (!report.clipPlayed)
            {
                report.clipPlayed = ClipPlayback.CanPlay(clip.ClipId) && ClipPlayback.TryPlay(clip.ClipId);
                if (report.clipPlayed) playbackAt = Time.realtimeSinceStartup;
            }
            if (!report.clipPlayed) return;
            if (!PlaybackAdvanced()) return;
            Publish(clip.ClipId);
        }
        private void Publish(string clipId)
        {
            if (substep == 0)
            { publishRequest = MediaNetworkBinding.RequestPublish(clipId, "Ilk gercek gun"); substep = 1; return; }
            if (MediaNetworkBinding.LastResultRequest != publishRequest) return;
            if (substep == 1)
            {
                if (!MediaNetworkBinding.LastResultAccepted) { Fail("Publish: " + MediaNetworkBinding.LastResultReason); return; }
                report.published = true;
                publishRequest = MediaNetworkBinding.RequestPublish(clipId, "tekrar"); substep = 2; return;
            }
            report.duplicateRefused = !MediaNetworkBinding.LastResultAccepted && MediaNetworkBinding.LastResultReason == "PublicationAlreadyQueued";
            localDone = report.duplicateRefused;
        }
        private void Sleep()
        {
            var bed = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None)
                .Single(b => b.Kind == HomeInteractionKind.Bed && b.BedId == DayIds.Beds[report.slot]);
            if (substep == 0)
            {
                var exit = new Vector3(bed.ExitPose.position.x, ContinuousTownLayout.WalkSurfaceY, bed.ExitPose.position.z);
                // The crew reaches the bedroom together, so walk a lane .9 m south of the exit row and turn north only at
                // one's own bed; the host judges each bed ray on its own positions, and a neighbour crossing the exit row
                // cut a waiting player's ray (InvalidTarget: #188 hunt-first 23 Oct, default 4p day 29 Oct, #155).
                if (route == null) SetRoute(new[] { H(4.95f, -2.5f), H(4.95f, exit.z - ContinuousTownLayout.HomeOrigin.z - .9f),
                    exit - new Vector3(0f, 0f, .9f), exit });
                if (!Follow()) return;
                Aim(bed.GetComponent<Collider>().bounds.center);
                if (!Settled()) return;
                var eye = local.GetComponentInChildren<Camera>(true).transform.position;
                report.bedRay = Physics.Raycast(eye, bed.GetComponent<Collider>().bounds.center - eye, out var seen, 3.25f, ~0,
                    QueryTriggerInteraction.Collide) ? seen.collider.name : "none";
                HomeStatus = "";
                typeof(HomePlayerInteractionBinding).GetMethod("SendRequest", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(adapter.GetComponent<HomePlayerInteractionBinding>(), new object[] { (byte)1 });
                substep = 1;
            }
            report.inBed = local.Seated.Value;
            if (!string.IsNullOrEmpty(HomeStatus)) report.bedAnswer = HomeStatus;
            if (sync.DayNumber.Value <= firstDay || local.Seated.Value || DayLock.IsLocked) return;
            report.slept = sync.SummaryDayNumber.Value == firstDay && sync.SummaryReason.Value == (byte)DayCloseReason.EarlySleep;
            report.morningHome = Vector3.Distance(local.transform.position, ContinuousTownLayout.HomeRig.Spawn(report.slot)) < 0.7f;
            localDone = report.slept && report.morningHome;
        }

        private void ReloadTick()
        {
            if (route == null) SetRoute(new[] { H(0f, -2.4f), H(2f, -2.4f) });
            if (!Follow() || !HomePcView.PanelOpen) return;
            var economy = adapter.GetComponent<EconomyManager>();
            var channel = adapter.GetComponent<MediaNetworkBinding>().Channel;
            var clip = MediaNetworkBinding.Mirrored.Clips.FirstOrDefault(c => c.ClipId == baseline.clipId);
            if (clip == null) return;
            if (substep == 0)
            {
                report.reloadClean = sync.DayNumber.Value == baseline.day && economy.SharedBalance == baseline.balance &&
                    economy.BoatRepair.CompletedPartIds.Count == 3 && economy.LoadoutFor(new PlayerId(0)).Contains(EconomyManager.CameraBasicId) &&
                    economy.ExportSaveData("read-only", "read-only").SoldCaptureIds.Count == baseline.soldCount &&
                    clip.ContentHash == baseline.hash && clip.SizeBytes == baseline.clipBytes &&
                    channel.Publications().Count == 1 && ClipPlayback.CanPlay(clip.ClipId) && ClipPlayback.TryPlay(clip.ClipId) &&
                    adapter.GetComponent<BoatTripManager>().State.Phase == BoatTripPhase.Docked &&
                    adapter.GetComponent<BoatTripManager>().State.Seats.Count == 0 &&
                    adapter.GetComponent<BoatTripManager>().ActiveBoatId == BoatTripIds.BoatId;
                playbackAt = Time.realtimeSinceStartup; substep = 1; return;
            }
            if (substep == 1)
            {
                if (!PlaybackAdvanced()) return;
                publishRequest = MediaNetworkBinding.RequestPublish(clip.ClipId, "reload retry"); substep = 2; return;
            }
            if (MediaNetworkBinding.LastResultRequest != publishRequest) return;
            report.reloadClean &= !MediaNetworkBinding.LastResultAccepted && MediaNetworkBinding.LastResultReason == "PublicationAlreadyQueued" &&
                economy.SharedBalance == baseline.balance;
            Snapshot(); Finish();
        }
        private bool PlaybackAdvanced()
        {
            if (Time.realtimeSinceStartup < playbackAt + 1f) return false;
            var binding = adapter.GetComponent<RecordingMediaCaptureBinding>();
            var playing = typeof(RecordingMediaCaptureBinding).GetField("playing", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(binding);
            report.clipAdvanced = playing != null && (int)playing.GetType().GetField("Frame").GetValue(playing) >= 3;
            return report.clipAdvanced;
        }
        private void Observe()
        {
            report.repaired |= sync.BoatPartsDone.Value == 3;
            var dive = adapter.Session.State.DiveId;
            if (!string.IsNullOrEmpty(dive)) diveIds.Add(dive);
            report.diveCount = diveIds.Count;
            foreach (var fish in FindObjectsByType<FishActor>(FindObjectsSortMode.None))
            {
                if (fish.IsSpawned && !report.fishIds.Contains(fish.NetworkObjectId)) report.fishIds.Add(fish.NetworkObjectId);
                if (fish.IsSpawned && fish.name == "Fish_SeaBass" && !report.primaryFishIds.Contains(fish.NetworkObjectId))
                    report.primaryFishIds.Add(fish.NetworkObjectId);
            }
            if (!string.IsNullOrEmpty(dive) && !report.fullyPopulatedDives.Contains(dive) &&
                FindObjectsByType<FishActor>(FindObjectsSortMode.None).Count(f => f.IsSpawned && !f.IsDead) == 15)
                report.fullyPopulatedDives.Add(dive);
            if (step < 10 || step > 13) return;
            var phase = ((BoatTripPhase)trip.Phase.Value).ToString();
            if (report.tripPhases.Count == 0 || report.tripPhases.Last() != phase) report.tripPhases.Add(phase);
            var icons = BoatMapView.LastIcons;
            if (icons == null) return;
            report.mapPlayers = Math.Max(report.mapPlayers, icons.Count(i => i.IconId.StartsWith(BoatMapPresenter.PlayerIconPrefix, StringComparison.Ordinal)));
            var boat = icons.FirstOrDefault(i => i.IconId == BoatTripIds.BoatId);
            var dock = icons.FirstOrDefault(i => i.IconId == BoatMapPresenter.DockIconId);
            if (boat.IconId == null || dock.IconId == null) return;
            if (phase == "Outbound" || phase == "Inbound")
            {
                mapPositions.Add($"{Mathf.Round(boat.MapX * 50)}:{Mathf.Round(boat.MapZ * 50)}");
                report.mapMoved |= mapPositions.Count >= 3;
            }
            if (phase == "Docked") report.mapDocked |= Vector2.Distance(new Vector2(boat.MapX, boat.MapZ), new Vector2(dock.MapX, dock.MapZ)) < 0.01f;
        }
        private void Snapshot()
        {
            report.day = sync.DayNumber.Value; report.balance = sync.SharedBalance.Value; report.boatParts = sync.BoatPartsMask.Value;
            var publication = MediaNetworkBinding.Mirrored.Publications.FirstOrDefault();
            report.income = publication != null ? publication.Income : 0;
            if (Host) report.soldCount = adapter.GetComponent<EconomyManager>().ExportSaveData("read-only", "read-only").SoldCaptureIds.Count;
        }
        private void Track()
        {
            report.position = local.transform.position.ToString();
            if (!tracked) return;
            var distance = Vector3.Distance(previous, local.transform.position);
            RecordMotion(distance);
            var seat = local.Seated.Value;
            // Seat/bed pose and D07 morning are explicit body-placement boundaries.
            if (!seat && !wasSeated && sync.DayNumber.Value == previousDay && step < 14)
            {
                report.distance += distance; report.maxTravelStep = Mathf.Max(report.maxTravelStep, distance);
                if (distance > 1.5f) Fail(FormattableString.Invariant($"Unexpected travel discontinuity: {distance:F6}; realGap={motionGap:F6}; fixedGap={motionFixedGap:F6}; from={previous}; to={local.transform.position}"));
            }
            previous = local.transform.position; wasSeated = seat; previousDay = sync.DayNumber.Value;
            if (report.worldLoads != 1 && !(RepairInterruptions && disconnectSent && report.worldLoads == 2))
                Fail("World scene reloaded outside the explicit repair guest rejoin");
        }
        private void SetRoute(IEnumerable<Vector3> points) { route = points.ToList(); waypoint = 0; }
        // #188: the worksite is either at the water berth (pier approach, as before) or on the beach cradle, where the
        // walk goes round the solid hull to the repair-work anchor.
        private IEnumerable<Vector3> RepairApproach(BoatRepairNetworkBinding site, int part)
        {
            var stand = site.StandPosition(part);
            if (site.OnCradle) return site.RouteAround(local.transform.position, P(stand.x, stand.z));
            return new[] { P(9f+(part-1)*.65f,-12.5f), P(9f+(part-1)*.65f,-9f), P(stand.x, stand.z) };
        }
        // Next dry waypoint round the cradle hull while the broken boat lies there; otherwise the target itself.
        private Vector3 Around(Vector3 target)
        {
            var site = BoatRepairNetworkBinding.Instance;
            return site != null && !local.Swimming.Value ? site.NextAround(local.transform.position, target) : target;
        }
        private static Vector3 P(float x, float z) => new Vector3(x, ContinuousTownLayout.WalkSurfaceY, z);
        private static Vector3 H(float x, float z) => ContinuousTownLayout.HomeOrigin + new Vector3(x, 0f, z);
        private IEnumerable<Vector3> Outward()
        {
            yield return H(-4.7f, -2f); yield return H(-4.7f, 5f); yield return ContinuousTownLayout.HomeRig.Door;
            yield return P(0f, -36f); yield return P(0f, -24f);
            // #188: with the broken boat on the beach cradle (x 1.1..6.1, z -11.4..-13.8) the crew fans out west of it.
            var cradle = BoatRepairNetworkBinding.Instance;
            var x = cradle != null && cradle.Blocking ? -1.8f - report.slot * 1.1f : -1.8f + report.slot * 1.1f;
            yield return P(x, -16f);
            yield return P(x, -13f);
        }
        private bool Follow(bool depth = false)
        {
            if (waypoint >= route.Count) { Stop(); return true; }
            var target = route[waypoint]; var delta = target - local.transform.position;
            var flat = new Vector2(delta.x, delta.z).magnitude;
            // The bed is approached head-on from the lane, so a .3 m stop can leave the feet past the host's 3.25 m
            // bed range; stand on the exit pose like the step 10 seat approach.
            var tolerance = (step == 10 || step == 14) && waypoint == route.Count - 1 ? 0.08f : 0.3f;
            if (flat < tolerance && (!depth || waypoint != route.Count - 1 || Mathf.Abs(delta.y) < 0.45f))
            { waypoint++; return waypoint >= route.Count; }
            // #188 hunt-first: the unrepaired hull still lies on the beach cradle, so dry legs go round it.
            if (HuntFirst && !local.Swimming.Value) { MoveToward(Around(target)); return false; }
            MoveToward(waypoint == 0 && target.z < -10f && local.transform.position.z > -11f && local.Swimming.Value ? CoastTarget(target) : target);
            return false;
        }
        private Vector3 CoastTarget(Vector3 stand)
        {
            var wade = GameObject.Find("Beach_Wade")?.GetComponent<Collider>();
            var strip = GameObject.Find("Beach_Platform")?.GetComponent<Collider>();
            var pos = local.transform.position;
            if (wade == null || strip == null || pos.z <= strip.bounds.max.z + 0.2f) return stand;
            if (pos.y >= strip.bounds.max.y - 0.35f) return new Vector3(pos.x, pos.y, strip.bounds.max.z - 0.6f);
            var lane = wade.transform.position.x;
            var ahead = Mathf.Max(pos.z - 1.2f, strip.bounds.max.z);
            var origin = new Vector3(lane, wade.bounds.max.y + 5f, ahead);
            var y = Mathf.Min(pos.y, wade.bounds.min.y - 0.3f);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == wade || hit.collider == strip)) y = hit.point.y - 0.05f;
            return new Vector3(lane, y, ahead);
        }
        private bool GoToService(string id)
        {
            var anchor = FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None).Single(a => a.Definition.ServiceId == id);
            var stand = anchor.WorldPosition + anchor.transform.forward * 1.6f;
            var delta = stand - local.transform.position; delta.y = 0f;
            if (delta.magnitude > 0.2f) { MoveToward(Around(CoastTarget(stand))); aimAt = 0; return false; }
            Aim(anchor.WorldPosition + Vector3.up);
            return Settled();
        }
        private void MoveToward(Vector3 target)
        {
            var delta = target - local.transform.position;
            var yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var move = Quaternion.Inverse(Quaternion.Euler(0f, yaw, 0f)) * delta.normalized;
            if (!local.Swimming.Value) move.y = 0f;
            local.SubmitLocalInput(move, yaw, 0f);
        }
        private void Aim(Vector3 target)
        {
            var delta = target - local.GetComponentInChildren<Camera>(true).transform.position;
            local.SubmitLocalInput(Vector3.zero, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg,
                -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg);
        }
        private bool Settled()
        {
            if (aimAt == 0) aimAt = Time.realtimeSinceStartup + 0.65f;
            return Time.realtimeSinceStartup >= aimAt;
        }
        private void Act(Action action)
        {
            if (Time.realtimeSinceStartup < nextAction) return;
            nextAction = Time.realtimeSinceStartup + 0.8f; action();
        }
        private void Stop() => local.SubmitLocalInput(Vector3.zero, 0f, 0f);
        private void CaptureView(string suffix)
        {
            var camera=local.GetComponentInChildren<Camera>(true);
            var previous=camera.targetTexture; var active=RenderTexture.active;
            var target=new RenderTexture(1280,720,24);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                File.WriteAllBytes(output+suffix,pixels.EncodeToPNG());
            }
            finally {camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(pixels);}
        }
        private void Log(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && !finished) Fail(message);
        }
        // Step 14 timeouts name the bed state recorded while connected, so a refused one-shot request is visible
        // even when the host has already left and the local player object is gone.
        private string BedState() => step == 14 ? $" inBed={report.inBed} ray={report.bedRay} home={report.bedAnswer}" : "";
        private string HomeStatus
        {
            get => HomeStatusField.GetValue(adapter.GetComponent<HomePlayerInteractionBinding>()) as string;
            set => HomeStatusField.SetValue(adapter.GetComponent<HomePlayerInteractionBinding>(), value);
        }
        private static readonly FieldInfo HomeStatusField =
            typeof(HomePlayerInteractionBinding).GetField("statusMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        private void Fail(string reason) { report.failure = reason; report.errors.Add(reason); Finish(); }
        private void Finish(bool quit=true)
        {
            if (finished) return;
            finished = true;
            report.passed = step == 15 && report.players == expected && report.worldLoads == 1 && report.homeSpawn &&
                report.repaired && report.diveCount == 3 && report.primaryFishIds.Count >= 3 &&
                report.fullyPopulatedDives.Count == 3 && report.realClip &&
                report.tripPhases.SequenceEqual(new[] { "Docked", "Outbound", "Anchored", "Inbound", "Docked" }) &&
                report.mapPlayers >= expected && report.mapMoved && report.mapDocked && report.slept && report.morningHome &&
                report.day == firstDay + 1 && report.income > 0 && report.errors.Count == 0;
            if (Host) report.passed &= report.initialClean && report.cameraBought && report.soldCount == 2 &&
                report.clipPlayed && report.clipAdvanced && report.published && report.duplicateRefused &&
                report.cameraHudRecording && report.cameraHudTimerAdvanced && report.cameraHudStopped;
            if (report.reload) report.passed = report.reloadClean && report.clipAdvanced && report.errors.Count == 0;
            report.huntFirst = HuntFirst;
            if (HuntFirst && Host) report.passed &= report.partsAtFirstSale == 0;
            if (RepairOnly) report.passed = step==16 && report.repaired && sawCarry && sawPlaced && report.players==expected && report.errors.Count==0;
            if (RepairInterruptions && Host) report.passed &= report.repairRace && report.repairDisconnect && report.repairRejoin && report.repairMidnight;
            if (RepairPurchase && Host) report.passed &= report.purchasedMaterial && report.purchaseDidNotRepair && report.saveFaultRefused && report.saveFaultRecovered;
            if (Arg("-p187-video")=="1" && !report.reload)
            {
                var visuals=report.repairArtSeen && report.repairToolContactSeen && report.repairGripVerified;
                if(!visuals && report.errors.Count==0)
                {
                    report.failure=$"Repair visuals: art={report.repairArtSeen}, contact={report.repairToolContactSeen}, grip={report.repairGripVerified}, minGripError={report.minRepairGripError}";
                    report.errors.Add(report.failure);
                }
                report.passed &= visuals;
            }
            if (report.reload && RepairOnly) report.passed=report.reloadClean && report.homeSpawn && report.worldLoads==1 && report.errors.Count==0;
            if(OceanVideo && !report.reload)
            {
                var oceanOk=report.oceanReady && report.oceanReflection && report.oceanAnimated &&
                    report.oceanSurfaceEye && report.oceanBands==3 && report.oceanVideoFrames>=30;
                if(!oceanOk && report.errors.Count==0)
                {report.failure=$"Ocean proof missing: ready={report.oceanReady}, reflection={report.oceanReflection}, motion={report.oceanAnimated}, eye={report.oceanSurfaceEye}, bands={report.oceanBands}";report.errors.Add(report.failure);}
                report.passed &= oceanOk;
            }
            if(EquipmentVideo && !report.reload)
            {
                var equipmentOk=report.equipmentLoweredSeen && report.equipmentRaisedSeen && report.equipmentVideoFrames>=8 &&
                    (Host?report.equipmentCancelled:report.unownedCameraRefused);
                if(!equipmentOk && report.errors.Count==0)
                {report.failure="Equipment transition/cancel/refusal visual proof missing";report.errors.Add(report.failure);}
                report.passed &= equipmentOk;
            }
            report.observedCarry=sawCarry;report.observedPlaced=sawPlaced;
            report.motionSamples=motionHistory.ToList();
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("P46_PRODUCT_RESULT passed=" + report.passed + " failure=" + report.failure);
            if(quit) Application.Quit(report.passed ? 0 : 1);
        }
    }
}
