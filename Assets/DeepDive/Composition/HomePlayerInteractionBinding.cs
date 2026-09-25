using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Trip;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    public readonly struct HomeSleepView
    {
        public readonly ulong PlayerId;
        public readonly string BedId;

        public HomeSleepView(ulong playerId, string bedId)
        {
            PlayerId = playerId;
            BedId = bedId ?? string.Empty;
        }
    }

    public readonly struct P4MapPingView
    {
        public readonly ulong PlayerId;
        public readonly Vector3 WorldPosition;
        public readonly float ExpiresAt;

        public P4MapPingView(ulong playerId, Vector3 worldPosition, float expiresAt)
        {
            PlayerId = playerId;
            WorldPosition = worldPosition;
            ExpiresAt = expiresAt;
        }
    }

    // P4.1-A (#88): physical home interaction + replicated sleep/ping presentation.
    //
    // This is intentionally a Composition component instead of another NetworkObject authority.
    // Client intents travel as NGO named messages, the host resolves the real NetworkPlayer and
    // physical ray/proximity, and only then calls Mert's HomeBedInteraction/DayEngine seam. Sleep
    // identity and pings are presentation mirrors; DayEngine remains the only day/sleep gate.
    [DisallowMultipleComponent]
    public sealed class HomePlayerInteractionBinding : MonoBehaviour
    {
        public const KeyCode HomeInteractionKey = KeyCode.H;
        public const KeyCode MapPingKey = KeyCode.P;
        public const float InteractionRange = 3.25f;
        public const float PingRayRange = 80f;
        public const float PingLifetimeSeconds = 8f;

        private const string RequestMessage = "DeepDive.P4.HomeRequest.v1";
        private const string SleepStateMessage = "DeepDive.P4.SleepState.v1";
        private const string PingStateMessage = "DeepDive.P4.MapPing.v1";
        private const string ResultMessage = "DeepDive.P4.HomeResult.v1";
        private const byte OpInteract = 1;
        private const byte OpPing = 2;
        private const byte NoBed = byte.MaxValue;
        private const string HomeSceneName = "PrepArea";
        private const ulong MorningSceneRequestBase = 0xF400000000000000UL;

        private static HomePlayerInteractionBinding current;
        private static readonly Dictionary<ulong, string> sleepByPlayer = new Dictionary<ulong, string>();
        private static readonly Dictionary<ulong, P4MapPingView> pingsByPlayer = new Dictionary<ulong, P4MapPingView>();

        private readonly Dictionary<ulong, ulong> lastRequestByPlayer = new Dictionary<ulong, ulong>();
        private NetworkManager manager;
        private NetworkSession networkSession;
        private bool messagesRegistered;
        private bool registeredAsServer;
        private ulong localRequestSequence;
        private int pendingMorningDay;
        private bool closeFreezeApplied;
        private string statusMessage = string.Empty;
        private float statusUntil;
        private bool mapVisible = true;

        public static IReadOnlyList<HomeSleepView> SnapshotSleepers()
        {
            var result = new List<HomeSleepView>(sleepByPlayer.Count);
            foreach (var pair in sleepByPlayer) result.Add(new HomeSleepView(pair.Key, pair.Value));
            return result;
        }

        public static IReadOnlyList<P4MapPingView> SnapshotPings()
        {
            var result = new List<P4MapPingView>(pingsByPlayer.Count);
            foreach (var pair in pingsByPlayer) result.Add(pair.Value);
            return result;
        }

        public static bool TryGetBed(ulong playerId, out string bedId) => sleepByPlayer.TryGetValue(playerId, out bedId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter == null || adapter.GetComponent<HomePlayerInteractionBinding>() != null) return;
            adapter.gameObject.AddComponent<HomePlayerInteractionBinding>();
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(this);
                return;
            }
            current = this;
            manager = GetComponent<NetworkManager>();
            networkSession = GetComponent<NetworkSession>();
        }

        private void OnEnable()
        {
            DayNetworkBinding.MorningBegan -= OnMorningBegan;
            DayNetworkBinding.MorningBegan += OnMorningBegan;
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnClientConnected;
                manager.OnClientConnectedCallback += OnClientConnected;
                manager.OnClientDisconnectCallback -= OnClientDisconnected;
                manager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = GetComponent<NetworkManager>();
                networkSession = GetComponent<NetworkSession>();
                if (manager == null) return;
            }

            if (manager.IsListening && !messagesRegistered) RegisterMessages();
            else if (!manager.IsListening && messagesRegistered)
            {
                UnregisterMessages();
                ResetMirrors();
            }

            PruneExpiredPings();
            if (!manager.IsListening) return;

            if (Input.GetKeyDown(KeyCode.M)) mapVisible = !mapVisible;
            HandleLocalInput();

            if (manager.IsServer)
            {
                ObserveClosingState();
                if (pendingMorningDay > 0) TryCompleteMorningPlacement();
            }
        }

        private void HandleLocalInput()
        {
            if (!manager.IsConnectedClient || !Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;
            if (Input.GetKeyDown(HomeInteractionKey)) SendRequest(OpInteract);
            if (Input.GetKeyDown(MapPingKey)) SendRequest(OpPing);
        }

        private void SendRequest(byte op)
        {
            var requestId = ++localRequestSequence;
            if (manager.IsServer)
            {
                HandleServerRequest(manager.LocalClientId, op, requestId);
                return;
            }

            if (manager.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(op);
            writer.WriteValueSafe(requestId);
            manager.CustomMessagingManager.SendNamedMessage(
                RequestMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        private void RegisterMessages()
        {
            var messages = manager.CustomMessagingManager;
            if (messages == null) return;
            registeredAsServer = manager.IsServer;
            if (registeredAsServer) messages.RegisterNamedMessageHandler(RequestMessage, ReceiveRequest);
            messages.RegisterNamedMessageHandler(SleepStateMessage, ReceiveSleepState);
            messages.RegisterNamedMessageHandler(PingStateMessage, ReceivePingState);
            messages.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
            messagesRegistered = true;
        }

        private void UnregisterMessages()
        {
            var messages = manager != null ? manager.CustomMessagingManager : null;
            if (messages != null)
            {
                if (registeredAsServer) messages.UnregisterNamedMessageHandler(RequestMessage);
                messages.UnregisterNamedMessageHandler(SleepStateMessage);
                messages.UnregisterNamedMessageHandler(PingStateMessage);
                messages.UnregisterNamedMessageHandler(ResultMessage);
            }
            registeredAsServer = false;
            messagesRegistered = false;
        }

        private void ReceiveRequest(ulong senderClientId, FastBufferReader reader)
        {
            if (!manager.IsServer) return;
            reader.ReadValueSafe(out byte op);
            reader.ReadValueSafe(out ulong requestId);
            HandleServerRequest(senderClientId, op, requestId);
        }

        private void HandleServerRequest(ulong senderClientId, byte op, ulong requestId)
        {
            if (!manager.IsServer || requestId == 0 || !TryGetServerPlayer(senderClientId, out var player))
            {
                SendResult(senderClientId, op, requestId, false, "InvalidState");
                return;
            }

            if (lastRequestByPlayer.TryGetValue(senderClientId, out var last) && requestId <= last)
            {
                SendResult(senderClientId, op, requestId, false, "AlreadyProcessed");
                return;
            }
            lastRequestByPlayer[senderClientId] = requestId;

            if (op == OpInteract) HandleHomeInteraction(senderClientId, player, requestId);
            else if (op == OpPing) HandlePing(senderClientId, player, requestId);
            else SendResult(senderClientId, op, requestId, false, "InvalidState");
        }

        private void HandleHomeInteraction(ulong senderClientId, NetworkPlayer player, ulong requestId)
        {
            var playerId = new PlayerId(senderClientId);

            // H while already asleep means "get up"; the player does not need to keep looking at
            // the bed while lying in it.
            if (sleepByPlayer.TryGetValue(senderClientId, out var currentBed))
            {
                var leave = HomeBedInteraction.TryLeaveBed(playerId, requestId);
                if (leave.Accepted)
                {
                    sleepByPlayer.Remove(senderClientId);
                    player.SetSeatedServer(false);
                    if (TryFindBed(currentBed, out var oldBed)) player.Teleport(oldBed.ExitPose);
                    BroadcastSleep(senderClientId, string.Empty);
                }
                SendResult(senderClientId, OpInteract, requestId, leave.Accepted, leave.ReasonCode);
                return;
            }

            if (networkSession == null || networkSession.DiveActive || player.Passive.Value || DayLock.IsLocked)
            {
                SendResult(senderClientId, OpInteract, requestId, false, "InvalidState");
                return;
            }

            var origin = player.RecordingEyePosition;
            var direction = player.RecordingForwardServer;
            if (direction.sqrMagnitude < 0.001f) direction = player.transform.forward;
            if (!Physics.Raycast(origin, direction.normalized, out var hit, InteractionRange, ~0, QueryTriggerInteraction.Collide))
            {
                SendResult(senderClientId, OpInteract, requestId, false, "InvalidTarget");
                return;
            }

            var anchor = HomeInteractionAnchor.FromCollider(hit.collider);
            if (anchor == null || !anchor.IsValid || Vector3.Distance(player.transform.position, anchor.WorldPosition) > InteractionRange)
            {
                SendResult(senderClientId, OpInteract, requestId, false, "InvalidTarget");
                return;
            }

            if (anchor.Kind == HomeInteractionKind.Storage)
            {
                var storage = HomeStorageInteraction.TryOpen(playerId, requestId);
                SendResult(senderClientId, OpInteract, requestId, storage.Accepted, storage.ReasonCode);
                return;
            }

            // Put the tentative identity in our presentation mirror before calling DayEngine. If this
            // is the final sleeper, BeginMorning can fire synchronously inside TryEnterBed and will
            // clear it. We only publish/set seated after the call if the day did not already advance
            // to Morning, preventing a stale "sleeping" state from leaking into the next day.
            sleepByPlayer[senderClientId] = anchor.BedId;
            var enter = HomeBedInteraction.TryEnterBed(playerId, anchor.BedId, requestId);
            if (!enter.Accepted)
            {
                sleepByPlayer.Remove(senderClientId);
                SendResult(senderClientId, OpInteract, requestId, false, enter.ReasonCode);
                return;
            }

            var phase = CurrentDayPhase();
            if (phase != DayPhase.Morning)
            {
                sleepByPlayer[senderClientId] = anchor.BedId;
                player.SetRecordingPresentationServer(false);
                player.SetSeatedServer(true);
                player.Teleport(anchor.SleepPose);
                BroadcastSleep(senderClientId, anchor.BedId);
            }
            else
            {
                sleepByPlayer.Remove(senderClientId);
            }
            SendResult(senderClientId, OpInteract, requestId, true, string.Empty);
        }

        private void HandlePing(ulong senderClientId, NetworkPlayer player, ulong requestId)
        {
            if (DayLock.IsLocked || player.Passive.Value)
            {
                SendResult(senderClientId, OpPing, requestId, false, "InvalidState");
                return;
            }

            var origin = player.RecordingEyePosition;
            var direction = player.RecordingForwardServer;
            if (direction.sqrMagnitude < 0.001f) direction = player.transform.forward;

            var world = player.transform.position;
            if (Physics.Raycast(origin, direction.normalized, out var hit, PingRayRange, ~0, QueryTriggerInteraction.Ignore))
                world = hit.point;

            // Utku's region conversion is the validity gate. If the aimed point is outside the known
            // map, fall back to the host-authored player position; never clamp/guess a hidden target.
            if (!P4MapPositionFeed.TryWorldToMap(world, out _))
            {
                world = player.transform.position;
                if (!P4MapPositionFeed.TryWorldToMap(world, out _))
                {
                    SendResult(senderClientId, OpPing, requestId, false, "MapUnavailable");
                    return;
                }
            }

            ApplyPing(senderClientId, world, PingLifetimeSeconds);
            BroadcastPing(senderClientId, world, PingLifetimeSeconds);
            SendResult(senderClientId, OpPing, requestId, true, string.Empty);
        }

        private void OnMorningBegan(int dayNumber)
        {
            if (manager == null || !manager.IsServer) return;

            var sleeperIds = new List<ulong>(sleepByPlayer.Keys);
            sleepByPlayer.Clear();
            for (var i = 0; i < sleeperIds.Count; i++) BroadcastSleep(sleeperIds[i], string.Empty);

            // Normalize transient player presentation/input immediately. Placement may need to wait
            // for the home scene load, but nobody carries seated/recording/passive state into morning.
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                if (!players[i].IsSpawned) continue;
                players[i].SetRecordingPresentationServer(false);
                players[i].ResetForDiveServer();
            }

            var trip = FindFirstObjectByType<BoatTripManager>();
            if (trip != null) trip.ResetToDocked();

            pendingMorningDay = Math.Max(1, dayNumber);
            closeFreezeApplied = false;
            TryCompleteMorningPlacement();
        }

        private void ObserveClosingState()
        {
            var phase = CurrentDayPhase();
            if (phase == DayPhase.Closing || phase == DayPhase.Summary)
            {
                if (closeFreezeApplied) return;
                closeFreezeApplied = true;
                var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
                for (var i = 0; i < players.Length; i++)
                {
                    if (!players[i].IsSpawned) continue;
                    players[i].SetRecordingPresentationServer(false);
                    players[i].SetSeatedServer(true);
                }
            }
            else if (phase == DayPhase.Running)
            {
                closeFreezeApplied = false;
            }
        }

        private void TryCompleteMorningPlacement()
        {
            if (!manager.IsServer || pendingMorningDay <= 0) return;
            if (networkSession == null) networkSession = GetComponent<NetworkSession>();
            if (networkSession == null || !networkSession.IsHost) return;

            if (SceneManager.GetActiveScene().name != HomeSceneName)
            {
                if (networkSession.IsSceneLoading) return;
                var requestId = MorningSceneRequestBase + (ulong)pendingMorningDay;
                networkSession.TryLoadScene(HomeSceneName, requestId);
                return;
            }

            if (networkSession.IsSceneLoading) return;
            HomeSceneSetup.EnsureRig();

            var players = new List<NetworkPlayer>(FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None));
            players.RemoveAll(p => p == null || !p.IsSpawned);
            players.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
            for (var i = 0; i < players.Count && i < AdmissionPolicy.MaxPlayers; i++)
            {
                players[i].SetSeatedServer(false);
                if (PlayerSpawnPoint.TryGet(i, out var pose)) players[i].Teleport(pose);
            }
            pendingMorningDay = 0;
        }

        private DayPhase CurrentDayPhase()
        {
            var day = FindFirstObjectByType<DayManager>();
            return day != null ? day.Engine.Phase : (DayLock.IsLocked ? DayPhase.Closing : DayPhase.Running);
        }

        private bool TryGetServerPlayer(ulong clientId, out NetworkPlayer player)
        {
            player = null;
            if (!manager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null) return false;
            player = client.PlayerObject.GetComponent<NetworkPlayer>();
            return player != null && player.IsSpawned;
        }

        private static bool TryFindBed(string bedId, out HomeInteractionAnchor anchor)
        {
            var anchors = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None);
            for (var i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].Kind != HomeInteractionKind.Bed || !string.Equals(anchors[i].BedId, bedId, StringComparison.Ordinal)) continue;
                anchor = anchors[i];
                return true;
            }
            anchor = null;
            return false;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!manager.IsServer || !messagesRegistered) return;
            foreach (var pair in sleepByPlayer) SendSleepState(clientId, pair.Key, pair.Value);
            foreach (var pair in pingsByPlayer)
            {
                var remaining = pair.Value.ExpiresAt - Time.unscaledTime;
                if (remaining > 0f) SendPingState(clientId, pair.Key, pair.Value.WorldPosition, remaining);
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            lastRequestByPlayer.Remove(clientId);
            if (manager != null && manager.IsServer)
            {
                if (sleepByPlayer.Remove(clientId)) BroadcastSleep(clientId, string.Empty);
                if (pingsByPlayer.Remove(clientId)) BroadcastPing(clientId, default, 0f);
            }
            else
            {
                sleepByPlayer.Remove(clientId);
                pingsByPlayer.Remove(clientId);
            }
        }

        private void BroadcastSleep(ulong playerId, string bedId)
        {
            ApplySleep(playerId, bedId);
            if (!manager.IsServer || manager.CustomMessagingManager == null) return;
            foreach (var pair in manager.ConnectedClients)
            {
                if (manager.IsHost && pair.Key == manager.LocalClientId) continue;
                SendSleepState(pair.Key, playerId, bedId);
            }
        }

        private void SendSleepState(ulong targetClientId, ulong playerId, string bedId)
        {
            using var writer = new FastBufferWriter(24, Allocator.Temp);
            writer.WriteValueSafe(playerId);
            writer.WriteValueSafe(BedIndex(bedId));
            manager.CustomMessagingManager.SendNamedMessage(
                SleepStateMessage, targetClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveSleepState(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId && !manager.IsServer) return;
            reader.ReadValueSafe(out ulong playerId);
            reader.ReadValueSafe(out byte bedIndex);
            ApplySleep(playerId, BedId(bedIndex));
        }

        private static void ApplySleep(ulong playerId, string bedId)
        {
            if (string.IsNullOrEmpty(bedId)) sleepByPlayer.Remove(playerId);
            else sleepByPlayer[playerId] = bedId;
        }

        private void BroadcastPing(ulong playerId, Vector3 world, float lifetime)
        {
            if (!manager.IsServer || manager.CustomMessagingManager == null) return;
            foreach (var pair in manager.ConnectedClients)
            {
                if (manager.IsHost && pair.Key == manager.LocalClientId) continue;
                SendPingState(pair.Key, playerId, world, lifetime);
            }
        }

        private void SendPingState(ulong targetClientId, ulong playerId, Vector3 world, float lifetime)
        {
            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(playerId);
            writer.WriteValueSafe(world.x);
            writer.WriteValueSafe(world.y);
            writer.WriteValueSafe(world.z);
            writer.WriteValueSafe(lifetime);
            manager.CustomMessagingManager.SendNamedMessage(
                PingStateMessage, targetClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReceivePingState(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId && !manager.IsServer) return;
            reader.ReadValueSafe(out ulong playerId);
            reader.ReadValueSafe(out float x);
            reader.ReadValueSafe(out float y);
            reader.ReadValueSafe(out float z);
            reader.ReadValueSafe(out float lifetime);
            ApplyPing(playerId, new Vector3(x, y, z), lifetime);
        }

        private static void ApplyPing(ulong playerId, Vector3 world, float lifetime)
        {
            if (lifetime <= 0f)
            {
                pingsByPlayer.Remove(playerId);
                return;
            }
            pingsByPlayer[playerId] = new P4MapPingView(playerId, world, Time.unscaledTime + lifetime);
        }

        private void SendResult(ulong targetClientId, byte op, ulong requestId, bool accepted, string reason)
        {
            if (manager.IsHost && targetClientId == manager.LocalClientId)
            {
                ApplyResult(op, accepted, reason);
                return;
            }
            if (manager.CustomMessagingManager == null || !manager.ConnectedClients.ContainsKey(targetClientId)) return;
            using var writer = new FastBufferWriter(96, Allocator.Temp);
            writer.WriteValueSafe(op);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe(accepted);
            writer.WriteValueSafe(new FixedString64Bytes(reason ?? string.Empty));
            manager.CustomMessagingManager.SendNamedMessage(
                ResultMessage, targetClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveResult(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId && !manager.IsServer) return;
            reader.ReadValueSafe(out byte op);
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out bool accepted);
            reader.ReadValueSafe(out FixedString64Bytes reason);
            ApplyResult(op, accepted, reason.ToString());
        }

        private void ApplyResult(byte op, bool accepted, string reason)
        {
            if (accepted)
                statusMessage = op == OpPing ? "PING GONDERILDI" : "EV ETKILESIMI TAMAM";
            else
                statusMessage = FriendlyReason(reason);
            statusUntil = Time.unscaledTime + 1.6f;
        }

        private static string FriendlyReason(string reason) => reason switch
        {
            "BedTaken" => "YATAK DOLU",
            "AlreadyInBed" => "BASKA YATAKTASIN",
            "NotInBed" => "YATAKTA DEGILSIN",
            "NotActive" => "OYUNCU AKTIF DEGIL",
            "DayClosing" => "GUN KAPANIYOR",
            "StorageUnavailable" => "DEPO HENUZ BAGLANMADI",
            "MapUnavailable" => "HARITA BU NOKTAYI KABUL ETMIYOR",
            "AlreadyProcessed" => "TEKRAR ISTEK ENGELLENDI",
            "InvalidTarget" => "ETKILESIM HEDEFI YOK",
            _ => "ISLEM REDDEDILDI"
        };

        private void PruneExpiredPings()
        {
            if (pingsByPlayer.Count == 0) return;
            var expired = new List<ulong>();
            foreach (var pair in pingsByPlayer)
                if (pair.Value.ExpiresAt <= Time.unscaledTime) expired.Add(pair.Key);
            for (var i = 0; i < expired.Count; i++) pingsByPlayer.Remove(expired[i]);
        }

        private static byte BedIndex(string bedId)
        {
            for (byte i = 0; i < DayIds.BedCount; i++)
                if (string.Equals(DayIds.Beds[i], bedId, StringComparison.Ordinal)) return i;
            return NoBed;
        }

        private static string BedId(byte index) => index < DayIds.BedCount ? DayIds.Beds[index] : string.Empty;

        private void ResetMirrors()
        {
            sleepByPlayer.Clear();
            pingsByPlayer.Clear();
            lastRequestByPlayer.Clear();
            localRequestSequence = 0;
            pendingMorningDay = 0;
            closeFreezeApplied = false;
        }

        private void OnGUI()
        {
            if (manager == null || !manager.IsListening || !manager.IsConnectedClient) return;
            GUI.Box(new Rect(20, Screen.height - 58, 330, 38),
                $"{HomeInteractionKey}: YATAK/DEPO   {MapPingKey}: HARITA PING   M: HARITA");
            if (Time.unscaledTime < statusUntil && !string.IsNullOrEmpty(statusMessage))
                GUI.Box(new Rect(360, Screen.height - 58, 240, 38), statusMessage);

            DrawPingOverlay();
        }

        private void DrawPingOverlay()
        {
            if (!mapVisible || !BoatMapView.LastHadRegion || BoatMapView.LastIcons.Count == 0 || pingsByPlayer.Count == 0) return;
            const float mapSize = 220f;
            var rect = new Rect(Screen.width - mapSize - 20f, 20f, mapSize, mapSize);
            foreach (var pair in pingsByPlayer)
            {
                var ping = pair.Value;
                if (!P4MapPositionFeed.TryWorldToMap(ping.WorldPosition, out var map)) continue;
                var center = new Vector2(rect.x + map.x * rect.width, rect.y + (1f - map.y) * rect.height);
                var previous = GUI.color;
                GUI.color = new Color(1f, 0.95f, 0.25f, 0.9f);
                GUI.DrawTexture(new Rect(center.x - 5f, center.y - 5f, 10f, 10f), Texture2D.whiteTexture);
                GUI.color = previous;
                GUI.Label(new Rect(center.x + 7f, center.y - 9f, 80f, 18f), "PING P" + ping.PlayerId);
            }
        }

        private void OnDisable()
        {
            DayNetworkBinding.MorningBegan -= OnMorningBegan;
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnClientConnected;
                manager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (messagesRegistered) UnregisterMessages();
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
            ResetMirrors();
        }
    }
}
