using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P3-A transport + owner UI for camera recording intent. The client may send only the
    // NetworkObject id it is aiming at; the host resolves the concrete IRecordingTarget before
    // the request reaches P3-B. Start locks that target for the whole recording and Stop never
    // performs a second raycast.
    [DisallowMultipleComponent]
    public sealed class RecordingNetworkBridge : MonoBehaviour
    {
        public const string RequestMessage = "deepdive/p3/recording-request-v1";
        public const string ResultMessage = "deepdive/p3/recording-result-v1";
        private const float TargetRange = 35f;
        private const ulong CollectedStopNotice = ulong.MaxValue;

        private readonly struct ActiveRecordingTarget
        {
            public readonly ulong NetworkObjectId;
            public readonly IRecordingTarget Target;
            public readonly double StartedAt;
            public readonly float MaxSeconds;

            public ActiveRecordingTarget(ulong networkObjectId, IRecordingTarget target, double startedAt, float maxSeconds)
            {
                NetworkObjectId = networkObjectId;
                Target = target;
                StartedAt = startedAt;
                MaxSeconds = maxSeconds;
            }
        }

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private readonly Dictionary<ulong, ulong> lastRequestByPlayer = new Dictionary<ulong, ulong>();
        private readonly Dictionary<ulong, ActiveRecordingTarget> activeTargetByPlayer =
            new Dictionary<ulong, ActiveRecordingTarget>();

        private ulong localRequestSequence;
        private ulong localLastAppliedRequest;
        private ulong automaticStopSequence;
        private bool cameraMode;
        private bool localRecording;
        private ulong localRecordingTarget;
        private string localStatus = string.Empty;
        private float localStatusUntil;
        private double recordingStartedAt;
        private RecordingCameraHud cameraHud;

        public bool IsRecordingLocal => localRecording;
        public string CurrentStatus => Time.unscaledTime < localStatusUntil ? localStatus : string.Empty;
        public float RecordingElapsedSeconds => localRecording
            ? (float)System.Math.Max(0, Time.unscaledTimeAsDouble - recordingStartedAt) : 0f;

        // Same owner intent used by keyboard input and opt-in multiplayer smoke tests.
        public void ToggleRecordingLocal()
        {
            var player = LocalPlayer();
            if (player == null || adapter.Session.State.Phase != SessionPhase.Dive) return;
            if (player.RepairHandsBusy) { ShowStatus("ONCE ONARIM PARCASINI BIRAK"); return; }
            cameraMode = true;
            player.SetHeldEquipmentLocal(HeldEquipmentMode.Camera);
            SubmitLocal(player);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<RecordingNetworkBridge>() == null)
                session.gameObject.AddComponent<RecordingNetworkBridge>();
        }

        private void Awake()
        {
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private void OnEnable() => CatchObject.CollectedForPresentation += OnCatchCollected;
        private void OnDisable() => CatchObject.CollectedForPresentation -= OnCatchCollected;

        // The target is about to despawn after a successful, host-validated pickup.
        // Finalise any already-earned takes before World removes that exact object.
        private void OnCatchCollected(CatchObject caught, PlayerId collector, CaptureResult capture)
        {
            if (adapter == null || !adapter.IsAuthority || manager == null || !manager.IsServer ||
                caught == null || caught.NetworkObject == null || !caught.NetworkObject.IsSpawned ||
                adapter.Session.State.Phase != SessionPhase.Dive) return;
            var objectId = caught.NetworkObject.NetworkObjectId;
            Debug.Log($"P257_RECORDING_PICKUP target={objectId} active={activeTargetByPlayer.Count}");
            foreach (var entry in new List<KeyValuePair<ulong, ActiveRecordingTarget>>(activeTargetByPlayer))
                if (entry.Value.NetworkObjectId == objectId)
                    StopOnServer(entry.Key, entry.Value, CollectedStopNotice);
        }

        private void Update()
        {
            EnsureMessaging();
            if (manager == null || !manager.IsListening)
            {
                if (cameraHud != null) cameraHud.Hide();
                cameraMode = false;
                localRecording = false;
                recordingStartedAt = 0;
                localRecordingTarget = 0;
                localRequestSequence = localLastAppliedRequest = 0;
                lastRequestByPlayer.Clear();
                activeTargetByPlayer.Clear();
                automaticStopSequence = 0;
                return;
            }

            if (adapter.IsAuthority && adapter.Session.State.Phase == SessionPhase.Dive)
                StopExpiredRecordings();

            var player = LocalPlayer();
            if (player == null) { if (cameraHud != null) cameraHud.Hide(); return; }

            if (adapter.Session.State.Phase != SessionPhase.Dive)
            {
                if (cameraMode || localRecording)
                    player.SetHeldEquipmentLocal(HeldEquipmentMode.None);
                if (cameraHud != null) cameraHud.Hide();
                cameraMode = localRecording = false;
                recordingStartedAt = 0;
                localRecordingTarget = 0;
                if (adapter.IsAuthority)
                {
                    foreach (var activePlayer in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                        if (activePlayer.IsSpawned) activePlayer.SetRecordingPresentationServer(false);
                }
                activeTargetByPlayer.Clear();
                return;
            }

            if (Application.isFocused && Input.GetKeyDown(KeyCode.C))
            {
                if (localRecording)
                    ShowStatus("STOP RECORDING FIRST");
                else
                {
                    cameraMode = !cameraMode;
                    player.SetHeldEquipmentLocal(cameraMode ? HeldEquipmentMode.Camera : HeldEquipmentMode.Harpoon);
                    ShowStatus(cameraMode ? "CAMERA OPEN" : "CAMERA CLOSED");
                }
            }

            if (Application.isFocused && cameraMode && Input.GetKeyDown(KeyCode.R))
                ToggleRecordingLocal();
            var visible = (cameraMode || localRecording) && !player.Passive.Value && !player.RepairHandsBusy &&
                player.CurrentHeldEquipment == HeldEquipmentMode.Camera;
            if (visible)
            {
                if (cameraHud == null) cameraHud = gameObject.AddComponent<RecordingCameraHud>();
                cameraHud.Show(OwnerCamera(player), localRecording, RecordingElapsedSeconds, player.CurrentCameraTier,
                    Time.unscaledTime < localStatusUntil ? localStatus : string.Empty);
            }
            else if (cameraHud != null) cameraHud.Hide();
        }

        private void EnsureMessaging()
        {
            var current = manager != null ? manager.CustomMessagingManager : null;
            if (manager == null || !manager.IsListening || current == null)
            {
                UnregisterMessages();
                return;
            }
            if (ReferenceEquals(messages, current)) return;

            UnregisterMessages();
            messages = current;
            messages.RegisterNamedMessageHandler(RequestMessage, RequestReceived);
            messages.RegisterNamedMessageHandler(ResultMessage, ResultReceived);
        }

        private void UnregisterMessages()
        {
            if (messages == null) return;
            messages.UnregisterNamedMessageHandler(RequestMessage);
            messages.UnregisterNamedMessageHandler(ResultMessage);
            messages = null;
        }

        private NetworkPlayer LocalPlayer()
        {
            foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                if (player.IsSpawned && player.IsOwner) return player;
            return null;
        }

        private static Camera OwnerCamera(NetworkPlayer player)
        {
            if (player == null) return null;
            var cameras = player.GetComponentsInChildren<Camera>(true);
            foreach (var camera in cameras)
                if (camera != null && camera.enabled) return camera;
            return cameras.Length > 0 ? cameras[0] : null;
        }

        private static bool TryAimTarget(NetworkPlayer player, out ulong targetId)
        {
            targetId = 0;
            var camera = OwnerCamera(player);
            if (camera == null) return false;
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!EyeRay.Raycast(ray.origin, ray.direction, TargetRange, QueryTriggerInteraction.Ignore,
                    player.GetComponent<CharacterController>(), out var hit)) return false;
            var networkObject = hit.collider.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsSpawned || networkObject == player.NetworkObject)
                return false;
            if (!TryResolveRecordingTarget(networkObject, out _)) return false;
            targetId = networkObject.NetworkObjectId;
            return true;
        }

        private static bool TryResolveRecordingTarget(NetworkObject networkObject, out IRecordingTarget target)
        {
            target = null;
            if (networkObject == null) return false;
            var behaviours = networkObject.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IRecordingTarget recordingTarget && IsUsableTarget(recordingTarget))
                {
                    target = recordingTarget;
                    return true;
                }
            }
            return false;
        }

        private static bool IsUsableTarget(IRecordingTarget target)
        {
            if (target == null) return false;
            return !(target is Object unityObject) || unityObject != null;
        }

        private void SubmitLocal(NetworkPlayer player)
        {
            var command = localRecording ? RecordingCommand.Stop : RecordingCommand.Start;
            var hasTarget = localRecording;
            var targetId = 0UL;
            if (command == RecordingCommand.Start)
                hasTarget = TryAimTarget(player, out targetId);

            var requestId = ++localRequestSequence;
            ShowStatus(command == RecordingCommand.Start ? "START REQUEST SENT" : "STOP REQUEST SENT");

            if (adapter.IsAuthority)
            {
                HandleRequest(manager.LocalClientId, requestId, command, hasTarget, targetId);
                return;
            }
            if (messages == null) return;

            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe((byte)command);
            writer.WriteValueSafe(hasTarget);
            writer.WriteValueSafe(targetId);
            messages.SendNamedMessage(RequestMessage, NetworkManager.ServerClientId, writer);
        }

        private void RequestReceived(ulong sender, FastBufferReader reader)
        {
            // NGO's named-message reader still includes its already-consumed name hash.
            if (!adapter.IsAuthority || reader.Length > 64 || !reader.TryBeginRead(18)) return;
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out byte commandByte);
            reader.ReadValueSafe(out bool hasTarget);
            reader.ReadValueSafe(out ulong targetId);
            HandleRequest(sender, requestId, (RecordingCommand)commandByte, hasTarget, targetId);
        }

        private void HandleRequest(ulong sender, ulong requestId, RecordingCommand command,
            bool hasTarget, ulong targetId)
        {
            if (requestId == 0 || requestId >= (1UL << 63)) return;
            lastRequestByPlayer.TryGetValue(sender, out var lastRequestId);
            var isRecording = activeTargetByPlayer.ContainsKey(sender);
            var result = RecordingRequestRules.Validate(requestId, lastRequestId, command, isRecording, hasTarget);
            if (result == PlayerActionResult.DuplicateRequest)
            {
                SendResult(sender, requestId, command, result);
                return;
            }
            lastRequestByPlayer[sender] = requestId;

            var state = adapter.Session.State;
            if (result == PlayerActionResult.Accepted &&
                (state.Phase != SessionPhase.Dive || string.IsNullOrWhiteSpace(state.DiveId)))
                result = PlayerActionResult.InvalidState;

            NetworkPlayer player = null;
            if (manager.ConnectedClients.TryGetValue(sender, out var connected) && connected.PlayerObject != null)
                player = connected.PlayerObject.GetComponent<NetworkPlayer>();

            if (result == PlayerActionResult.Accepted &&
                (player == null || !player.IsSpawned || player.Passive.Value || player.RepairHandsBusy ||
                 player.CurrentHeldEquipment != HeldEquipmentMode.Camera))
                result = PlayerActionResult.InvalidState;
            if (result == PlayerActionResult.Accepted && command == RecordingCommand.Start &&
                CameraRecordingLimits.MaxSeconds(player.CurrentCameraTier) <= 0f)
                result = PlayerActionResult.InvalidState;

            IRecordingTarget recordingTarget = null;
            var lockedNetworkObjectId = 0UL;
            if (result == PlayerActionResult.Accepted && command == RecordingCommand.Start)
            {
                if (!hasTarget || !manager.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject) ||
                    targetObject == null || !targetObject.IsSpawned ||
                    (player != null && targetObject == player.NetworkObject) ||
                    !TryResolveRecordingTarget(targetObject, out recordingTarget))
                {
                    result = PlayerActionResult.InvalidTarget;
                }
                else
                {
                    lockedNetworkObjectId = targetId;
                }
            }
            else if (result == PlayerActionResult.Accepted && command == RecordingCommand.Stop)
            {
                if (!activeTargetByPlayer.TryGetValue(sender, out var locked) || !IsUsableTarget(locked.Target))
                    result = PlayerActionResult.InvalidTarget;
                else
                {
                    recordingTarget = locked.Target;
                    lockedNetworkObjectId = locked.NetworkObjectId;
                }
            }

            if (result == PlayerActionResult.Accepted)
            {
                // The host supplies the replicated tier from NetworkPlayer. No client-sent tier or
                // range value is trusted here; Utku's recording evaluator remains the validation authority.
                var tier = player != null ? player.CurrentCameraTier : CameraTier.None;
                var candidate = new RecordingCandidate(requestId, state.DiveId, new PlayerId(sender), recordingTarget, tier);
                result = command == RecordingCommand.Start
                    ? RecordingEvaluation.TryStart(candidate)
                    : RecordingEvaluation.TryStop(candidate);
            }

            if (result == PlayerActionResult.Accepted)
            {
                if (command == RecordingCommand.Start)
                    activeTargetByPlayer[sender] = new ActiveRecordingTarget(lockedNetworkObjectId, recordingTarget,
                        Time.realtimeSinceStartupAsDouble, CameraRecordingLimits.MaxSeconds(player.CurrentCameraTier));
                else
                    activeTargetByPlayer.Remove(sender);
            }
            else if (command == RecordingCommand.Stop &&
                (result == PlayerActionResult.InvalidTarget || result == PlayerActionResult.InvalidState))
                activeTargetByPlayer.Remove(sender); // a destroyed target must not lock REC forever

            if (player != null && player.IsSpawned)
                player.SetRecordingPresentationServer(activeTargetByPlayer.ContainsKey(sender));

            SendResult(sender, requestId, command, result);
        }

        private void StopExpiredRecordings()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            foreach (var entry in new List<KeyValuePair<ulong, ActiveRecordingTarget>>(activeTargetByPlayer))
            {
                var owner = entry.Key;
                var take = entry.Value;
                if (now - take.StartedAt < take.MaxSeconds) continue;
                StopOnServer(owner, take, 0);
            }
        }

        private void StopOnServer(ulong owner, ActiveRecordingTarget take, ulong notice)
        {
            var result = PlayerActionResult.InvalidTarget;
            var connected = manager.ConnectedClients.TryGetValue(owner, out var client);
            if (connected && IsUsableTarget(take.Target))
            {
                var id = ulong.MaxValue - ++automaticStopSequence;
                var candidate = new RecordingCandidate(id, adapter.Session.State.DiveId,
                    new PlayerId(owner), take.Target);
                result = RecordingEvaluation.TryStop(candidate);
            }
            activeTargetByPlayer.Remove(owner);
            Debug.Log($"P257_RECORDING_STOP owner={owner} target={take.NetworkObjectId} notice={notice} result={result} connected={connected}");
            if (connected && client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent<NetworkPlayer>(out var player) && player.IsSpawned)
                player.SetRecordingPresentationServer(false);
            if (connected) SendResult(owner, notice, RecordingCommand.Stop, result, take.NetworkObjectId);
        }

        private void SendResult(ulong receiver, ulong requestId, RecordingCommand command, PlayerActionResult result,
            ulong finishedTargetId = 0)
        {
            var active = activeTargetByPlayer.TryGetValue(receiver, out var locked);
            // Automatic stop notices must identify the take they closed. Otherwise
            // a delayed notice for fish A can clear a newer take on fish B.
            var targetId = active ? locked.NetworkObjectId : finishedTargetId;
            if (receiver == manager.LocalClientId)
            {
                ApplyLocalResult(requestId, command, result, active, targetId);
                return;
            }
            if (messages == null) return;

            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe((byte)command);
            writer.WriteValueSafe((int)result);
            writer.WriteValueSafe(active);
            writer.WriteValueSafe(targetId);
            messages.SendNamedMessage(ResultMessage, receiver, writer);
        }

        private void ResultReceived(ulong sender, FastBufferReader reader)
        {
            if (adapter.IsAuthority || sender != NetworkManager.ServerClientId || reader.Length > 64 || !reader.TryBeginRead(22)) return;
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out byte commandByte);
            reader.ReadValueSafe(out int resultInt);
            reader.ReadValueSafe(out bool active);
            reader.ReadValueSafe(out ulong targetId);
            ApplyLocalResult(requestId, (RecordingCommand)commandByte,
                (PlayerActionResult)resultInt, active, targetId);
        }

        // #257: An automatic stop is not proof of a usable recording.
        // The host's real action result decides whether the camera may claim the take succeeded.
        public static string AutomaticStopStatus(bool caughtTarget, PlayerActionResult result)
        {
            if (result == PlayerActionResult.Accepted)
                return caughtTarget ? "AV ALINDI - KAYIT BITTI" : "KAYIT SURESI DOLDU";
            return caughtTarget ? "AV KAYDI OLUSTURULAMADI" : "SURE DOLDU - KAYIT GECERSIZ";
        }

        public static bool ShouldApplyAutomaticStop(bool currentlyRecording, ulong currentTargetId, ulong finishedTargetId)
            => !currentlyRecording || (finishedTargetId != 0 && currentTargetId == finishedTargetId);

        private void ApplyLocalResult(ulong requestId, RecordingCommand command, PlayerActionResult result,
            bool active, ulong targetId)
        {
            var automatic = requestId == 0 || requestId == CollectedStopNotice;
            // Out-of-order reliable notifications from a previous take cannot
            // reset a new active take or force its equipment/owner HUD to stop.
            if (automatic && !ShouldApplyAutomaticStop(localRecording, localRecordingTarget, targetId)) return;
            if (automatic) Debug.Log($"P257_RECORDING_NOTICE notice={requestId} result={result} active={active}");
            if (!automatic && requestId <= localLastAppliedRequest) return;
            if (!automatic) localLastAppliedRequest = requestId;
            if (active && !localRecording) recordingStartedAt = Time.unscaledTimeAsDouble;
            else if (!active) recordingStartedAt = 0;
            localRecording = active;
            localRecordingTarget = active ? targetId : 0;

            var player = LocalPlayer();
            if (player != null)
                player.SetHeldEquipmentLocal(active || cameraMode ? HeldEquipmentMode.Camera : HeldEquipmentMode.Harpoon);

            if (requestId == CollectedStopNotice && command == RecordingCommand.Stop)
                ShowStatus(AutomaticStopStatus(true, result));
            else if (requestId == 0 && command == RecordingCommand.Stop)
                ShowStatus(AutomaticStopStatus(false, result));
            else if (result == PlayerActionResult.Accepted)
                ShowStatus(command == RecordingCommand.Start ? "RECORDING STARTED" : "RECORDING STOPPED");
            else if (result == PlayerActionResult.InvalidTarget)
                ShowStatus("NO RECORD TARGET");
            else if (result == PlayerActionResult.DuplicateRequest)
                ShowStatus("DUPLICATE BLOCKED");
            else if (result == PlayerActionResult.InvalidState)
                ShowStatus("RECORDING BLOCKED");
            else
                ShowStatus("RECORDING REJECTED");
        }

        private void ShowStatus(string text)
        {
            localStatus = text ?? string.Empty;
            localStatusUntil = Time.unscaledTime + 1.25f;
        }

        private void OnGUI()
        {
            if (adapter == null || adapter.Session.State.Phase != SessionPhase.Dive || LocalPlayer() == null || cameraMode || localRecording) return;
            GUI.Box(new Rect(Screen.width - 190, 18, 170, 26), "[C] CAMERA");
            if (Time.unscaledTime < localStatusUntil)
                GUI.Box(new Rect(Screen.width * .5f - 110, 118, 220, 28), localStatus);
        }

        private void OnDestroy()
        {
            UnregisterMessages();
            if (cameraHud != null) Destroy(cameraHud);
        }
    }
}
