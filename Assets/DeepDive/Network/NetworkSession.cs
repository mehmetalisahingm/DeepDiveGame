using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Network
{
    [RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
    public sealed class NetworkSession : MonoBehaviour, INetworkSession
    {
        public const string Protocol = "DeepDive-P1-2";
        [SerializeField] private string offlineScene = "";
        private NetworkManager manager;
        private UnityTransport transport;
        private NetworkSceneManager subscribedScenes;
        private readonly AdmissionPolicy admission = new AdmissionPolicy();
        private readonly Dictionary<ulong, NetworkPlayer> players = new Dictionary<ulong, NetworkPlayer>();
        private readonly Dictionary<ulong, SceneLoadResult> completedLoads = new Dictionary<ulong, SceneLoadResult>();
        private readonly Dictionary<ulong, double> pendingConnections = new Dictionary<ulong, double>();
        private readonly List<ulong> expiredConnections = new List<ulong>();
        private bool joinAllowed;
        private bool manualLeave;
        private bool returningOffline;
        private bool subscribed;
        private double connectDeadline;
        private ulong activeRequest;
        private string activeScene;

        public ConnectionStatus Status { get; private set; }
        public string LastError { get; private set; } = "";
        public bool IsHost => manager != null && manager.IsHost;
        public bool IsSceneLoading { get; private set; }
        public bool DiveActive { get; private set; }
        public PlayerId? LocalPlayerId => manager != null && manager.IsConnectedClient ? new PlayerId(manager.LocalClientId) : (PlayerId?)null;
        public IReadOnlyList<PlayerId> Players => players.Keys.OrderBy(id => id).Select(id => new PlayerId(id)).ToArray();
        public event Action Changed;
        public event Action<SceneLoadResult> SceneLoaded;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton != manager)
            { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
            transport = GetComponent<UnityTransport>();
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.EnableSceneManagement = true;
            manager.NetworkConfig.TickRate = 30;
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(Protocol);
            manager.ConnectionApprovalCallback = Approve;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnClientStopped += Stopped;
            manager.OnTransportFailure += TransportFailed;
            subscribed = true;
        }

        private bool Prepare(ConnectionStatus status)
        {
            if (Status != ConnectionStatus.Offline || returningOffline || manager.IsListening || manager.ShutdownInProgress) return false;
            admission.Clear(); players.Clear(); pendingConnections.Clear(); completedLoads.Clear();
            LastError = ""; manualLeave = false; joinAllowed = true; IsSceneLoading = false; DiveActive = false;
            activeRequest = 0; activeScene = null;
            Status = status; connectDeadline = Time.realtimeSinceStartupAsDouble + 12;
            Changed?.Invoke();
            return true;
        }

        public bool StartHost(ushort port = 7777, string listenAddress = "0.0.0.0")
        {
            if (port == 0 || !IPAddress.TryParse(listenAddress, out _)) return Fail("InvalidAddress");
            // NGO cannot decline its own host, so validate spawn setup before starting it.
            for (var slot = 0; slot < AdmissionPolicy.MaxPlayers; slot++)
                if (!PlayerSpawnPoint.TryGet(slot, out _)) return Fail("SpawnPointMissing");
            if (manager.NetworkConfig.PlayerPrefab == null) return Fail("PlayerPrefabMissing");
            if (!Prepare(ConnectionStatus.StartingHost)) return false;
            transport.SetConnectionData("127.0.0.1", port, listenAddress);
            try
            {
                if (!manager.StartHost()) { Abort("HostStartFailed"); return false; }
                SubscribeScenes();
                if (!manager.IsConnectedClient) { Abort("SpawnPointMissing"); return false; }
                return true;
            }
            catch (Exception error) { Debug.LogException(error); Abort("HostStartFailed"); return false; }
        }

        public bool Join(string address, ushort port = 7777)
        {
            if (port == 0 || !IPAddress.TryParse(address, out _)) return Fail("InvalidAddress");
            if (!Prepare(ConnectionStatus.Connecting)) return false;
            transport.SetConnectionData(address, port);
            try
            {
                if (!manager.StartClient()) { Abort("ClientStartFailed"); return false; }
                SubscribeScenes();
                return true;
            }
            catch (Exception error) { Debug.LogException(error); Abort("ClientStartFailed"); return false; }
        }

        private void SubscribeScenes()
        {
            UnsubscribeScenes();
            subscribedScenes = manager.SceneManager;
            if (subscribedScenes != null) subscribedScenes.OnSceneEvent += SceneEvent;
        }

        private void UnsubscribeScenes()
        {
            if (subscribedScenes != null) subscribedScenes.OnSceneEvent -= SceneEvent;
            subscribedScenes = null;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Pending = false;
            var payload = request.Payload;
            var protocolMatches = payload != null && payload.Length == Protocol.Length && Encoding.UTF8.GetString(payload) == Protocol;
            response.Approved = admission.TryReserve(request.ClientNetworkId, joinAllowed && !IsSceneLoading,
                protocolMatches, out var slot, out var reason);
            if (response.Approved)
            {
                if (PlayerSpawnPoint.TryGet(slot, out var pose))
                {
                    response.Position = pose.position; response.Rotation = pose.rotation;
                    pendingConnections[request.ClientNetworkId] = Time.realtimeSinceStartupAsDouble + 15;
                }
                else
                {
                    admission.Release(request.ClientNetworkId);
                    response.Approved = false; reason = "SpawnPointMissing";
                }
            }
            response.CreatePlayerObject = response.Approved;
            response.Reason = reason;
            Debug.Log($"P1_APPROVAL id={request.ClientNetworkId} accepted={response.Approved} reason={reason}");
        }

        private void Connected(ulong id)
        {
            pendingConnections.Remove(id);
            if (id == manager.LocalClientId) Status = ConnectionStatus.Connected;
            Debug.Log($"P1_CONNECTED id={id}");
            Changed?.Invoke();
        }

        private void Disconnected(ulong id)
        {
            admission.Release(id); pendingConnections.Remove(id); players.Remove(id);
            if (!manager.IsServer && id == manager.LocalClientId && !manualLeave)
            {
                LastError = string.IsNullOrEmpty(manager.DisconnectReason) ? "HostDisconnected" : manager.DisconnectReason;
                Status = ConnectionStatus.Disconnecting;
                manager.Shutdown();
            }
            Debug.Log($"P1_DISCONNECTED id={id}");
            Changed?.Invoke();
        }

        public void Leave()
        {
            if (Status == ConnectionStatus.Offline || Status == ConnectionStatus.Disconnecting) return;
            manualLeave = true; LastError = ""; Status = ConnectionStatus.Disconnecting;
            manager.Shutdown(); Changed?.Invoke();
        }

        private void Abort(string reason)
        {
            LastError = reason; Status = ConnectionStatus.Disconnecting;
            manager.Shutdown(); Changed?.Invoke();
        }

        private void TransportFailed() => Abort("TransportFailure");

        private void Stopped(bool wasHost)
        {
            UnsubscribeScenes();
            admission.Clear(); pendingConnections.Clear(); players.Clear();
            completedLoads.Clear(); activeRequest = 0; activeScene = null; IsSceneLoading = false; DiveActive = false;
            joinAllowed = false; Status = ConnectionStatus.Offline;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (!string.IsNullOrEmpty(offlineScene) && SceneManager.GetActiveScene().name != offlineScene && !returningOffline)
                StartCoroutine(ReturnOffline());
            Debug.Log($"P1_STOPPED reason={LastError}");
            Changed?.Invoke();
        }

        private System.Collections.IEnumerator ReturnOffline()
        {
            returningOffline = true;
            // Shutdown finishes after OnClientStopped; load only on the following frame.
            yield return null;
            yield return SceneManager.LoadSceneAsync(offlineScene, LoadSceneMode.Single);
            returningOffline = false;
            Changed?.Invoke();
        }

        public bool SetJoinAllowed(bool allowed)
        {
            if (!IsHost || IsSceneLoading) return false;
            joinAllowed = allowed; Changed?.Invoke(); return true;
        }

        // Composition owns SessionPhase; NetworkPlayer only consumes this host-side gate.
        public void SetDiveActiveServer(bool active)
        {
            if (!IsHost) return;
            DiveActive = active;
        }

        public bool TryLoadScene(string sceneName, ulong requestId)
        {
            if (!IsHost || Status != ConnectionStatus.Connected) return Fail("NotHost");
            if (requestId == 0) return Fail("InvalidRequest");
            if (completedLoads.TryGetValue(requestId, out var prior))
            {
                if (prior.SceneName != sceneName) return Fail("RequestConflict");
                SceneLoaded?.Invoke(prior); return prior.Succeeded;
            }
            if (IsSceneLoading) return activeRequest == requestId && activeScene == sceneName;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName)) return Fail("InvalidScene");
            if (pendingConnections.Count > 0) return Fail("PlayerConnecting");
            var previousJoinPolicy = joinAllowed;
            joinAllowed = false; IsSceneLoading = true; activeRequest = requestId; activeScene = sceneName;
            var progress = manager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (progress != SceneEventProgressStatus.Started)
            {
                IsSceneLoading = false; activeRequest = 0; activeScene = null; joinAllowed = previousJoinPolicy;
                return Fail(progress.ToString());
            }
            Changed?.Invoke(); return true;
        }

        private void SceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType == SceneEventType.Load) IsSceneLoading = true;
            if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && sceneEvent.ClientId == manager.LocalClientId)
                IsSceneLoading = false;
            if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted) return;
            IsSceneLoading = false;
            if (IsHost && activeRequest != 0)
            {
                var error = sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0 ? "SceneLoadTimeout" : "";
                foreach (var pair in players)
                {
                    if (PlayerSpawnPoint.TryGet(admission.SlotOf(pair.Key), out var pose)) pair.Value.Teleport(pose);
                    else error = "SpawnPointMissing";
                }
                var result = new SceneLoadResult(activeRequest, activeScene, error.Length == 0, error);
                completedLoads.Add(activeRequest, result);
                activeRequest = 0; activeScene = null; LastError = error;
                Debug.Log($"P1_SCENE name={result.SceneName} success={result.Succeeded} reason={error}");
                SceneLoaded?.Invoke(result);
            }
            Changed?.Invoke();
        }

        // Called by composition when SessionManager opens a new DiveId. Player objects survive
        // scene loads, so resetting here prevents oxygen/health/action state leaking between dives.
        public void ResetPlayersForDiveServer()
        {
            if (!IsHost) return;
            foreach (var player in players.Values)
                player.ResetForDiveServer();
        }

        internal void Register(NetworkPlayer player)
        { players[player.OwnerClientId] = player; Changed?.Invoke(); }
        internal void Unregister(NetworkPlayer player)
        { players.Remove(player.OwnerClientId); Changed?.Invoke(); }

        private bool Fail(string reason) { LastError = reason; Changed?.Invoke(); return false; }

        private void Update()
        {
            if ((Status == ConnectionStatus.Connecting || Status == ConnectionStatus.StartingHost) && Time.realtimeSinceStartupAsDouble > connectDeadline)
                Abort("ConnectionTimeout");
            if (Status == ConnectionStatus.Disconnecting && !manager.IsListening && !manager.ShutdownInProgress)
                Stopped(false);
            if (!IsHost) return;
            expiredConnections.Clear();
            foreach (var pair in pendingConnections)
                if (Time.realtimeSinceStartupAsDouble > pair.Value) expiredConnections.Add(pair.Key);
            foreach (var id in expiredConnections)
            { manager.DisconnectClient(id, "ConnectionTimeout"); pendingConnections.Remove(id); admission.Release(id); }
        }

        private void OnDestroy()
        {
            if (!subscribed || manager == null) return;
            UnsubscribeScenes();
            manager.ConnectionApprovalCallback = null;
            manager.OnClientConnectedCallback -= Connected;
            manager.OnClientDisconnectCallback -= Disconnected;
            manager.OnClientStopped -= Stopped;
            manager.OnTransportFailure -= TransportFailed;
        }
    }
}
