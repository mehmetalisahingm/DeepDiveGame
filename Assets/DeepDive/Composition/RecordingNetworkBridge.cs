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
        private double recordingStartedAt;
        private Texture2D recordingDot;
        private GUIStyle hudText, hudSmall, hudRec;

        public bool IsRecordingLocal => localRecording;
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

        private void Update()
        {
            EnsureMessaging();
            if (manager == null || !manager.IsListening)
            {
                cameraMode = false;
                localRecording = false;
                recordingStartedAt = 0;
                localRecordingTarget = 0;
                localRequestSequence = localLastAppliedRequest = 0;
                lastRequestByPlayer.Clear();
                activeTargetByPlayer.Clear();
                return;
            }

            var player = LocalPlayer();
            if (player == null) return;

            if (adapter.Session.State.Phase != SessionPhase.Dive)
            {
                if (cameraMode || localRecording)
                    player.SetHeldEquipmentLocal(HeldEquipmentMode.None);
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
                    activeTargetByPlayer[sender] = new ActiveRecordingTarget(lockedNetworkObjectId, recordingTarget);
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
            if (adapter.IsAuthority || sender != NetworkManager.ServerClientId || reader.Length > 64 || !reader.TryBeginRead(22)) return;
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
            if (active && !localRecording) recordingStartedAt = Time.unscaledTimeAsDouble;
            else if (!active) recordingStartedAt = 0;
            localRecording = active;
            localRecordingTarget = active ? targetId : 0;

            var player = LocalPlayer();
            if (player != null)
                player.SetHeldEquipmentLocal(active || cameraMode ? HeldEquipmentMode.Camera : HeldEquipmentMode.Harpoon);

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

            if (!cameraMode && !localRecording)
            {
                GUI.Box(new Rect(Screen.width - 190, 18, 170, 26), "[C] CAMERA");
                if (Time.unscaledTime < localStatusUntil)
                    GUI.Box(new Rect(Screen.width * 0.5f - 110, 118, 220, 28), localStatus);
                return;
            }

            if (player.Passive.Value || player.RepairHandsBusy || player.CurrentHeldEquipment != HeldEquipmentMode.Camera) return;
            EnsureHud();
            var scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .6f, 1.6f);
            var safe = Screen.safeArea;
            var top = Mathf.Max(142f, 132 * scale);
            var frame = new Rect(safe.x + 30 * scale, Screen.height - safe.yMax + top,
                safe.width - 60 * scale, safe.height - top - 108 * scale);
            var oldColor = GUI.color;
            hudText.fontSize = Mathf.RoundToInt(20 * scale);
            hudRec.fontSize = Mathf.RoundToInt(22 * scale);
            hudSmall.fontSize = Mathf.RoundToInt(14 * scale);
            try
            {
                // Open corners keep the actual camera image visible; this is an owner overlay,
                // never painted into the archived clip's scene render.
                var ink = new Color(.91f, .96f, 1f, .8f);
                Corner(frame.x, frame.y, 1, 1, scale, ink);
                Corner(frame.xMax, frame.y, -1, 1, scale, ink);
                Corner(frame.x, frame.yMax, 1, -1, scale, ink);
                Corner(frame.xMax, frame.yMax, -1, -1, scale, ink);
                var chip = new Rect(frame.x + 14 * scale, frame.y + 14 * scale, 228 * scale, 42 * scale);
                Fill(chip, new Color(.02f, .035f, .045f, .78f));
                var red = new Color(1f, .18f, .22f);
                // REC and elapsed time stay readable even while the tally light pulses.
                GUI.color = localRecording ? new Color(red.r, red.g, red.b,
                    .6f + .4f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 5f))) : new Color(.6f, .7f, .73f);
                GUI.DrawTexture(new Rect(chip.x + 13 * scale, chip.y + 14 * scale, 13 * scale, 13 * scale), recordingDot);
                GUI.color = localRecording ? red : Color.white;
                GUI.Label(new Rect(chip.x + 37 * scale, chip.y + 6 * scale, 70 * scale, 32 * scale),
                    localRecording ? "REC" : "STBY", hudRec);
                GUI.color = Color.white;
                var seconds = (int)RecordingElapsedSeconds;
                GUI.Label(new Rect(chip.x + 116 * scale, chip.y + 8 * scale, 110 * scale, 30 * scale),
                    $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}", hudText);
                var tier = player.CurrentCameraTier == CameraTier.Professional ? "PRO" :
                    player.CurrentCameraTier == CameraTier.Advanced ? "ADVANCED" : "BASIC";
                GUI.Label(new Rect(frame.xMax - 165 * scale, chip.y + 8 * scale, 150 * scale, 26 * scale),
                    "CAM / " + tier, hudSmall);
                var center = frame.center;
                var focus = new Rect(Screen.width * .5f - 24 * scale, Screen.height * .5f - 24 * scale, 48 * scale, 48 * scale);
                var hasTarget = TryAimTarget(player, out _);
                var focusColor = hasTarget || localRecording ? new Color(.6f, .94f, .8f, .85f) : ink;
                Corner(focus.x, focus.y, 1, 1, scale * .4f, focusColor);
                Corner(focus.xMax, focus.y, -1, 1, scale * .4f, focusColor);
                Corner(focus.x, focus.yMax, 1, -1, scale * .4f, focusColor);
                Corner(focus.xMax, focus.yMax, -1, -1, scale * .4f, focusColor);
                var controls = new Rect(frame.x + 14 * scale, frame.yMax - 40 * scale, 370 * scale, 28 * scale);
                Fill(controls, new Color(.02f, .035f, .045f, .72f));
                GUI.Label(new Rect(controls.x + 10 * scale, controls.y + 3 * scale, controls.width, controls.height),
                    localRecording ? "R  KAYDI DURDUR" : "R  KAYDI BASLAT     C  KAMERAYI KAPAT", hudSmall);
                if (Time.unscaledTime < localStatusUntil)
                    GUI.Label(new Rect(center.x - 180 * scale, frame.yMax - 74 * scale, 360 * scale, 28 * scale), localStatus, hudSmall);
            }
            finally { GUI.color = oldColor; }
        }

        private void EnsureHud()
        {
            if (hudText != null) return;
            hudText = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            hudRec = new GUIStyle(hudText);
            hudSmall = new GUIStyle(GUI.skin.label);
            hudText.normal.textColor = hudRec.normal.textColor = hudSmall.normal.textColor = Color.white;
            var pixels = new Color[32 * 32];
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f))));
            recordingDot = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "CameraTallyLight" };
            recordingDot.SetPixels(pixels); recordingDot.Apply();
        }
        private static void Fill(Rect rect, Color color)
        { var previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
        private static void Corner(float x, float y, int dx, int dy, float scale, Color color)
        {
            Fill(new Rect(dx > 0 ? x : x - 28 * scale, y - (dy < 0 ? 2 * scale : 0), 28 * scale, 2 * scale), color);
            Fill(new Rect(x - (dx < 0 ? 2 * scale : 0), dy > 0 ? y : y - 28 * scale, 2 * scale, 28 * scale), color);
        }

        private void OnDestroy()
        {
            UnregisterMessages();
            if (recordingDot != null) Destroy(recordingDot);
        }
    }
}
