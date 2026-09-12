using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
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

        private readonly struct ActiveRecordingTarget
        {
            public readonly ulong NetworkObjectId;
            public readonly IRecordingTarget Target;

            public ActiveRecordingTarget(ulong networkObjectId, IRecordingTarget target)
            {
                NetworkObjectId = networkObjectId;
                Target = target;
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
        private bool cameraMode;
        private bool localRecording;
        private ulong localRecordingTarget;
        private string localStatus = string.Empty;
        private float localStatusUntil;

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

        private void Update()
        {
            EnsureMessaging();
            if (manager == null || !manager.IsListening)
            {
                cameraMode = false;
                localRecording = false;
                return;
            }

            if (adapter.IsAuthority && adapter.Session.State.Phase != SessionPhase.Dive)
                activeTargetByPlayer.Clear();

            var player = LocalPlayer();
            if (player == null) return;

            if (Application.isFocused && Input.GetKeyDown(KeyCode.C))
            {
                if (localRecording)
                    ShowStatus("STOP RECORDING FIRST");
                else
                {
                    cameraMode = !cameraMode;
                    ShowStatus(cameraMode ? "CAMERA OPEN" : "CAMERA CLOSED");
                }
            }

            if (Application.isFocused && cameraMode && Input.GetKeyDown(KeyCode.R))
                SubmitLocal(player);
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
            if (!Physics.Raycast(ray, out var hit, TargetRange, ~0, QueryTriggerInteraction.Ignore)) return false;
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
            if (!adapter.IsAuthority || reader.Length > 64) return;
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out byte commandByte);
            reader.ReadValueSafe(out bool hasTarget);
            reader.ReadValueSafe(out ulong targetId);
            HandleRequest(sender, requestId, (RecordingCommand)commandByte, hasTarget, targetId);
        }

        private void HandleRequest(ulong sender, ulong requestId, RecordingCommand command,
            bool hasTarget, ulong targetId)
        {
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
            if (result == PlayerActionResult.Accepted)
            {
                if (!manager.ConnectedClients.TryGetValue(sender, out var client) || client.PlayerObject == null)
                    result = PlayerActionResult.InvalidState;
                else
                {
                    player = client.PlayerObject.GetComponent<NetworkPlayer>();
                    if (player == null || !player.IsSpawned || player.Passive.Value)
                        result = PlayerActionResult.InvalidState;
                }
            }

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
                var candidate = new RecordingCandidate(requestId, state.DiveId, new PlayerId(sender), recordingTarget);
                result = command == RecordingCommand.Start
                    ? RecordingEvaluation.TryStart(candidate)
                    : RecordingEvaluation.TryStop(candidate);
            }

            if (result == PlayerActionResult.Accepted)
            {
                if (command == RecordingCommand.Start)
                    activeTargetByPlayer[sender] = new ActiveRecordingTarget(lockedNetworkObjectId, recordingTarget);
                else
                    activeTargetByPlayer.Remove(sender);
            }

            SendResult(sender, requestId, command, result);
        }

        private void SendResult(ulong receiver, ulong requestId, RecordingCommand command, PlayerActionResult result)
        {
            var active = activeTargetByPlayer.TryGetValue(receiver, out var locked);
            var targetId = active ? locked.NetworkObjectId : 0UL;
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
            if (adapter.IsAuthority || sender != NetworkManager.ServerClientId || reader.Length > 64) return;
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out byte commandByte);
            reader.ReadValueSafe(out int resultInt);
            reader.ReadValueSafe(out bool active);
            reader.ReadValueSafe(out ulong targetId);
            ApplyLocalResult(requestId, (RecordingCommand)commandByte,
                (PlayerActionResult)resultInt, active, targetId);
        }

        private void ApplyLocalResult(ulong requestId, RecordingCommand command, PlayerActionResult result,
            bool active, ulong targetId)
        {
            if (requestId <= localLastAppliedRequest) return;
            localLastAppliedRequest = requestId;
            localRecording = active;
            localRecordingTarget = active ? targetId : 0;

            if (result == PlayerActionResult.Accepted)
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
            if (adapter == null || adapter.Session.State.Phase != SessionPhase.Dive) return;
            var player = LocalPlayer();
            if (player == null) return;

            GUI.Box(new Rect(Screen.width - 190, 18, 170, 26), "[C] CAMERA");
            if (!cameraMode && !localRecording)
            {
                if (Time.unscaledTime < localStatusUntil)
                    GUI.Box(new Rect(Screen.width * 0.5f - 110, 118, 220, 28), localStatus);
                return;
            }

            var width = Mathf.Min(640f, Screen.width * 0.68f);
            var height = Mathf.Min(360f, Screen.height * 0.58f);
            var frame = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(frame, localRecording ? "REC" : "CAMERA");

            var hasTarget = TryAimTarget(player, out var previewTarget);
            var targetText = localRecording
                ? $"TARGET #{localRecordingTarget}"
                : hasTarget ? $"TARGET LOCK #{previewTarget}" : "NO TARGET";
            GUI.Box(new Rect(frame.x + 12, frame.y + 12, 190, 26), targetText);
            GUI.Box(new Rect(frame.x + 12, frame.yMax - 38, 270, 26),
                localRecording ? "[R] STOP RECORDING" : "[R] START RECORDING   [C] CLOSE");

            var cx = frame.center.x;
            var cy = frame.center.y;
            GUI.Label(new Rect(cx - 8, cy - 12, 30, 30), "+");
            if (Time.unscaledTime < localStatusUntil)
                GUI.Box(new Rect(cx - 110, frame.y + 46, 220, 28), localStatus);
        }

        private void OnDestroy()
        {
            UnregisterMessages();
        }
    }
}
