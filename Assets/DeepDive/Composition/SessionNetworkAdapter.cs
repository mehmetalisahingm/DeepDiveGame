using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.Session.UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    [RequireComponent(typeof(NetworkSession), typeof(SessionManager), typeof(SessionRoomUI))]
    public sealed class SessionNetworkAdapter : MonoBehaviour, ISessionNetworkBridge, ISessionControls
    {
        public const string PrepScene = "PrepArea", DiveScene = "DiveTestArea";
        public const string CommandMessage = "deepdive/p1/session-command-v1";
        private const string SnapshotMessage = "deepdive/p1/session-snapshot-v1";
        public Camera OfflineCamera;
        public SessionManager Session { get; private set; }
        public INetworkSession Connection => network;
        public bool IsAuthority => network != null && network.IsHost;
        public string LastAction { get; private set; } = "";
        public event Action Changed;
        private NetworkSession network;
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private readonly Dictionary<ulong, uint> requests = new Dictionary<ulong, uint>();
        private uint requestSequence, snapshotSequence, receivedSnapshot;
        private bool hadConnection, dirty;
        private double nextSnapshot;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton != manager)
            { enabled = false; Destroy(gameObject); return; }
            network = GetComponent<NetworkSession>();
            Session = GetComponent<SessionManager>();
            Session.Bridge = this;
            GetComponent<SessionRoomUI>().Controls = this;
            Session.Initialize("", DiveScene);
            Session.OnSessionStateChanged += StateChanged;
            Session.OnRosterChanged += RosterChanged;
            network.Changed += ConnectionChanged;
            network.SceneLoaded += Loaded;
            DontDestroyOnLoad(gameObject);
        }

        public bool CreateRoom(ushort port)
        {
            if (network.Status != ConnectionStatus.Offline) return false;
            ResetSession();
            Session.Initialize(Guid.NewGuid().ToString("N"), DiveScene);
            return network.StartHost(port);
        }
        public bool JoinRoom(string address, ushort port)
        {
            if (network.Status != ConnectionStatus.Offline) return false;
            ResetSession();
            return network.Join(address, port);
        }
        public void LeaveRoom() => network.Leave();

        private void ResetSession()
        {
            requests.Clear(); requestSequence = snapshotSequence = receivedSnapshot = 0;
            LastAction = "";
            Session.Initialize("", DiveScene);
        }

        public void ToggleReady()
        {
            var local = network.LocalPlayerId;
            if (!local.HasValue || !Session.Roster.TryGetValue(local.Value, out var ready)) return;
            SendCommand(1, !ready);
        }
        public void AdvancePhase() => SendCommand(2, false);

        private void SendCommand(byte kind, bool ready)
        {
            if (network.Status != ConnectionStatus.Connected || network.IsSceneLoading) return;
            var sequence = ++requestSequence;
            if (IsAuthority) HandleCommand(manager.LocalClientId, sequence, kind, ready, Session.State.Revision);
            else if (messages != null)
            {
                using var writer = new FastBufferWriter(32, Allocator.Temp);
                writer.WriteValueSafe(sequence); writer.WriteValueSafe(kind);
                writer.WriteValueSafe(ready); writer.WriteValueSafe(Session.State.Revision);
                messages.SendNamedMessage(CommandMessage, NetworkManager.ServerClientId, writer);
            }
        }

        private void CommandReceived(ulong sender, FastBufferReader reader)
        {
            if (!IsAuthority || !SessionCommand.TryRead(reader, out var sequence, out var kind, out var ready, out var revision)) return;
            HandleCommand(sender, sequence, kind, ready, revision);
        }

        private void HandleCommand(ulong sender, uint sequence, byte kind, bool ready, int revision)
        {
            var player = new PlayerId(sender);
            if (!IsAuthority || !Session.Roster.ContainsKey(player) ||
                !manager.ConnectedClients.ContainsKey(sender)) return;
            if (requests.TryGetValue(sender, out var prior) && unchecked((int)(sequence - prior)) <= 0) return;
            requests[sender] = sequence;
            if (network.IsSceneLoading || revision != Session.State.Revision) return;
            SessionActionResult result;
            if (kind == 1) result = Session.SetReady(player, ready);
            else if (kind == 2 && sender == NetworkManager.ServerClientId)
            {
                switch (Session.State.Phase)
                {
                    case SessionPhase.Lobby: result = Session.BeginPrep(); break;
                    case SessionPhase.Prep: result = Session.BeginDive(Guid.NewGuid().ToString("N")); break;
                    case SessionPhase.Dive: result = Session.BeginReturn(); break;
                    default: result = Session.CompleteReturn(); break;
                }
            }
            else result = SessionActionResult.NotHost;
            LastAction = result.ToString(); dirty = true;
            Debug.Log($"P1_SESSION_ACTION sender={sender} kind={kind} result={result} phase={Session.State.Phase}");
            Changed?.Invoke();
        }

        public bool RequestSceneLoad(SessionState next)
        {
            if (!IsAuthority || network.IsSceneLoading) return false;
            var scene = next.Phase == SessionPhase.Dive ? DiveScene : PrepScene;
            if (SceneManager.GetActiveScene().name == scene)
                return network.SetJoinAllowed(next.Phase == SessionPhase.Lobby);
            return network.TryLoadScene(scene, (ulong)next.Revision);
        }

        private void Loaded(SceneLoadResult result)
        {
            if (!IsAuthority) return;
            if (!result.Succeeded)
            {
                LastAction = result.Error;
                network.Leave(); // No half-loaded session is presented as playable.
                return;
            }
            network.SetJoinAllowed(Session.State.Phase == SessionPhase.Lobby);
            dirty = true;
        }

        private void ConnectionChanged()
        {
            if (network.Status != ConnectionStatus.Offline) hadConnection = true;
            else if (hadConnection)
            {
                hadConnection = false;
                UnregisterMessages();
                ResetSession();
            }
            if (IsAuthority)
            {
                var ids = network.Players;
                foreach (var id in Session.Roster.Keys.Where(id => !ids.Contains(id)).ToArray())
                { Session.Leave(id); requests.Remove(id.Value); }
                foreach (var id in ids)
                    if (!Session.Roster.ContainsKey(id)) Session.Join(id);
            }
            dirty = true;
            Changed?.Invoke();
        }

        private void Update()
        {
            var current = manager.CustomMessagingManager;
            if (manager.IsListening && current != null && current != messages)
            {
                UnregisterMessages(); messages = current;
                messages.RegisterNamedMessageHandler(CommandMessage, CommandReceived);
                messages.RegisterNamedMessageHandler(SnapshotMessage, SnapshotReceived);
            }
            if (OfflineCamera != null)
            {
                var offline = !network.LocalPlayerId.HasValue;
                OfflineCamera.enabled = offline;
                OfflineCamera.GetComponent<AudioListener>().enabled = offline;
            }
            if (IsAuthority && network.Status == ConnectionStatus.Connected && messages != null &&
                (dirty || Time.realtimeSinceStartupAsDouble >= nextSnapshot))
            {
                Broadcast(); dirty = false;
                nextSnapshot = Time.realtimeSinceStartupAsDouble + 0.5;
            }
        }

        private void Broadcast()
        {
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            var state = Session.State;
            writer.WriteValueSafe(++snapshotSequence);
            writer.WriteValueSafe((int)state.Phase); writer.WriteValueSafe(state.Revision);
            writer.WriteValueSafe(state.SessionId ?? ""); writer.WriteValueSafe(state.DiveId ?? "");
            writer.WriteValueSafe(state.RegionId ?? ""); writer.WriteValueSafe((byte)Session.Roster.Count);
            foreach (var pair in Session.Roster)
            { writer.WriteValueSafe(pair.Key.Value); writer.WriteValueSafe(pair.Value); }
            foreach (var id in manager.ConnectedClientsIds)
                if (id != NetworkManager.ServerClientId) messages.SendNamedMessage(SnapshotMessage, id, writer);
        }

        private void SnapshotReceived(ulong sender, FastBufferReader reader)
        {
            if (IsAuthority || sender != NetworkManager.ServerClientId || reader.Length > 1024) return;
            reader.ReadValueSafe(out uint sequence);
            if (unchecked((int)(sequence - receivedSnapshot)) <= 0) return;
            reader.ReadValueSafe(out int phase); reader.ReadValueSafe(out int revision);
            reader.ReadValueSafe(out string sessionId); reader.ReadValueSafe(out string diveId);
            reader.ReadValueSafe(out string regionId); reader.ReadValueSafe(out byte count);
            if (count > 4 || phase < 0 || phase > 3 || revision < Session.State.Revision) return;
            var roster = new Dictionary<PlayerId, bool>();
            for (var i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out ulong id); reader.ReadValueSafe(out bool ready);
                if (!roster.TryAdd(new PlayerId(id), ready)) return;
            }
            receivedSnapshot = sequence;
            Session.ApplyRemoteSnapshot(new SessionState { Phase = (SessionPhase)phase, Revision = revision,
                SessionId = sessionId, DiveId = diveId, RegionId = regionId }, roster);
            Changed?.Invoke();
        }

        private void StateChanged(SessionState state)
        {
            dirty = true;
            Debug.Log($"P1_SESSION_STATE phase={state.Phase} revision={state.Revision}");
        }
        private void RosterChanged(IReadOnlyDictionary<PlayerId, bool> roster) => dirty = true;
        private void UnregisterMessages()
        {
            if (messages == null) return;
            messages.UnregisterNamedMessageHandler(CommandMessage);
            messages.UnregisterNamedMessageHandler(SnapshotMessage);
            messages = null;
        }
        private void OnDestroy()
        {
            UnregisterMessages();
            if (network == null) return;
            network.Changed -= ConnectionChanged; network.SceneLoaded -= Loaded;
            Session.OnSessionStateChanged -= StateChanged; Session.OnRosterChanged -= RosterChanged;
        }
    }
}
