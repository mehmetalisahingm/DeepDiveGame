using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Media;
using DeepDive.Network;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.2-C (#102) composition root for the clip archive and the in-game channel. It owns no rule: the
    // archive/publish/result rules are ChannelAuthority's, the single commercial right and money are
    // EconomyManager's. This (a) binds Mehmet's ClipArchive seam to the archive on the host, (b) takes publish
    // requests from any process, checks on the HOST that the sender really stands at the home PC, and answers,
    // (c) writes archive + publications into the one campaign file, (d) settles results inside the day-close
    // transaction, and (e) mirrors the archive and the feed to every process.
    [DisallowMultipleComponent]
    public sealed class MediaNetworkBinding : MonoBehaviour, ChannelAuthority.IRights
    {
        private const string PublishMessage = "DeepDive.P4.Publish.v1";
        private const string ResultMessage = "DeepDive.P4.PublishResult.v1";
        private const string MirrorMessage = "DeepDive.P4.Channel.v1";
        private const float PcRange = 3.25f;

        // What this process shows (host: live authority; clients: the last mirrored state).
        public static MediaSaveData Mirrored { get; private set; } = new MediaSaveData();
        public static int MirroredRevision { get; private set; } = -1;
        public static bool HasMirror { get; private set; }
        // The last answer to THIS process's own publish request.
        public static ulong LastResultRequest { get; private set; }
        public static bool LastResultAccepted { get; private set; }
        public static string LastResultReason { get; private set; } = "";

        private static MediaNetworkBinding current;

        private readonly ChannelAuthority channel = new ChannelAuthority();
        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private EconomySaveStore store;
        private bool bound, registered, registeredAsServer;
        private int sentRevision = -1;
        private ulong localRequest;
        private Func<ClipManifest, ClipArchiveOutcome> submitDelegate;

        public ChannelAuthority Channel => channel;
        // True only after the host archive and campaign save have been restored.
        public bool IsHostReady => bound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<MediaNetworkBinding>() == null)
                session.gameObject.AddComponent<MediaNetworkBinding>();
        }

        private void Awake()
        {
            current = this;
            adapter = GetComponent<SessionNetworkAdapter>();
            manager = GetComponent<NetworkManager>();
            if (adapter == null || manager == null) enabled = false;
        }

        private void Update()
        {
            if (manager.IsListening && !registered) Register();
            else if (!manager.IsListening && registered) Unregister();
            if (!manager.IsListening) return;

            if (!manager.IsServer || !adapter.IsAuthority) { UnbindHost(); return; }
            BindHost();
            if (!bound || channel.Revision == sentRevision) return;
            sentRevision = channel.Revision;
            Mirrored = channel.ExportMedia();
            MirroredRevision = channel.Revision;
            HasMirror = true;
            foreach (var client in manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId) SendMirror(client);
        }

        // ---- host ------------------------------------------------------------------------------------------

        private void BindHost()
        {
            if (bound) return;
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (store == null) store = GetComponent<EconomySaveStore>();
            var day = GetComponent<DayManager>();
            if (economy == null || store == null || day == null) return;

            channel.Configure(this, () => day.Engine.DayNumber, () => DayLock.IsLocked, () => store.SaveNow(),
                id => day.Engine.RecordPublicationQueued(id));
            store.Media = channel;   // hands the channel what the file already held
            submitDelegate = SubmitClip;
            ClipArchive.Bind(submitDelegate);
            bound = true;
        }

        private void UnbindHost()
        {
            if (!bound) return;
            ClipArchive.Unbind(submitDelegate);
            if (store != null) store.Media = null;
            bound = false;
            sentRevision = -1;
        }

        private ClipArchiveOutcome SubmitClip(ClipManifest manifest)
        {
            var outcome = channel.Submit(manifest);
            if (outcome == ClipArchiveOutcome.Added && store != null) store.SaveNow();
            return outcome;
        }

        // Called by DayNetworkBinding inside the close transaction (before the atomic write).
        public static void SettleThrough(int closingDay)
        {
            if (current != null && current.bound) current.channel.SettleThrough(closingDay);
        }

        public bool TryClaimForChannel(string recordingId) => economy != null && economy.TryClaimRecordingForChannel(recordingId);
        public void ReleaseChannelClaim(string recordingId) => economy?.ReleaseChannelClaim(recordingId);
        public bool CreditChannelIncome(string settleId, int amount) => economy != null && economy.CreditChannelIncome(settleId, amount);

        // The sender's own host-authoritative position against the physical PC.
        private bool IsAtPc(ulong clientId)
        {
            var pc = FindFirstObjectByType<HomePcAnchor>();
            if (pc == null) return false;
            foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                if (p.IsSpawned && p.OwnerClientId == clientId)
                    return Vector3.Distance(p.transform.position, pc.transform.position) <= PcRange;
            return false;
        }

        private TransactionResult HandlePublish(ulong sender, string clipId, string title, ulong requestId)
        {
            if (!bound) return TransactionResult.Reject(requestId, "InvalidState", 0);
            if (!IsAtPc(sender)) return TransactionResult.Reject(requestId, "NotAtPc", channel.Revision);
            return channel.TryPublish(new PlayerId(sender), clipId, title, requestId);
        }

        // ---- requests from any process ----------------------------------------------------------------------

        public static ulong RequestPublish(string clipId, string title)
        {
            if (current == null || current.manager == null || !current.manager.IsListening) return 0;
            var m = current.manager;
            var requestId = ++current.localRequest;
            if (m.IsServer)
            {
                var r = current.HandlePublish(m.LocalClientId, clipId, title, requestId);
                SetResult(requestId, r.Accepted, r.ReasonCode);
                return requestId;
            }
            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe(new FixedString64Bytes(clipId ?? ""));
            writer.WriteValueSafe(new FixedString128Bytes(title ?? ""));
            m.CustomMessagingManager.SendNamedMessage(PublishMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            return requestId;
        }

        private static void SetResult(ulong requestId, bool accepted, string reason)
        {
            LastResultRequest = requestId;
            LastResultAccepted = accepted;
            LastResultReason = reason ?? "";
        }

        private void ReceivePublish(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out FixedString64Bytes clipId);
            reader.ReadValueSafe(out FixedString128Bytes title);
            var r = HandlePublish(sender, clipId.ToString(), title.ToString(), requestId);
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe(r.Accepted);
            writer.WriteValueSafe(new FixedString64Bytes(r.ReasonCode ?? ""));
            manager.CustomMessagingManager.SendNamedMessage(ResultMessage, sender, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveResult(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out bool accepted);
            reader.ReadValueSafe(out FixedString64Bytes reason);
            SetResult(requestId, accepted, reason.ToString());
        }

        // ---- mirror ------------------------------------------------------------------------------------------

        private void SendMirror(ulong clientId)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Mirrored));
            using var writer = new FastBufferWriter(bytes.Length + 16, Allocator.Temp);
            writer.WriteValueSafe(MirroredRevision);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            manager.CustomMessagingManager.SendNamedMessage(MirrorMessage, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void ReceiveMirror(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int revision);
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > 1 << 20) return;
            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            MediaSaveData data;
            try { data = JsonUtility.FromJson<MediaSaveData>(System.Text.Encoding.UTF8.GetString(bytes)); }
            catch (ArgumentException) { return; }
            if (data == null) return;
            Mirrored = data;
            MirroredRevision = revision;
            HasMirror = true;
        }

        private void Register()
        {
            var messages = manager.CustomMessagingManager;
            if (messages == null) return;
            registeredAsServer = manager.IsServer;
            if (registeredAsServer)
            {
                messages.RegisterNamedMessageHandler(PublishMessage, ReceivePublish);
                manager.OnClientConnectedCallback += OnClientConnected;
            }
            else
            {
                messages.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
                messages.RegisterNamedMessageHandler(MirrorMessage, ReceiveMirror);
            }
            registered = true;
        }

        private void Unregister()
        {
            var messages = manager.CustomMessagingManager;
            if (messages != null)
            {
                if (registeredAsServer) messages.UnregisterNamedMessageHandler(PublishMessage);
                else
                {
                    messages.UnregisterNamedMessageHandler(ResultMessage);
                    messages.UnregisterNamedMessageHandler(MirrorMessage);
                }
            }
            if (registeredAsServer) manager.OnClientConnectedCallback -= OnClientConnected;
            registered = false;
            registeredAsServer = false;
            HasMirror = false;
            Mirrored = new MediaSaveData();
            MirroredRevision = -1;
            UnbindHost();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (HasMirror && clientId != NetworkManager.ServerClientId) SendMirror(clientId);
        }

        private void OnDisable()
        {
            if (registered && manager != null) Unregister();
            if (current == this) current = null;
        }
    }
}
