using System;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Session;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.5-C: client intents are only role/upgrade ids. Position, money and evidence come
    // from the host's live player, economy and channel; snapshot messages are read-only.
    [DisallowMultipleComponent]
    public sealed class LivingWorldNetworkBinding : MonoBehaviour
    {
        private const string RequestMessage = "DeepDive.P45.LivingRequest.v1";
        private const string ResultMessage = "DeepDive.P45.LivingResult.v1";
        private const string MirrorMessage = "DeepDive.P45.LivingMirror.v1";
        private const float TownRangeMetres = 5f;

        public static LivingWorldSaveData Mirrored { get; private set; } = new LivingWorldSaveData();
        public static int MirroredRevision { get; private set; } = -1;
        public static bool HasMirror { get; private set; }
        public static string LastResult { get; private set; } = "";

        private static LivingWorldNetworkBinding current;
        private NetworkManager manager;
        private SessionNetworkAdapter adapter;
        private EconomyManager economy;
        private EconomySaveStore store;
        private DayManager day;
        private MediaNetworkBinding media;
        private CrewRoleEffectBinding crew;
        private LivingWorldAuthority authority;
        private bool registered, serverRegistered, bound;
        private int lastMirrorRevision = -1;
        private ulong localRequestId;
        private int lastReconciledDay = -1;
        private int lastReconciledSaleCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var adapter = FindFirstObjectByType<SessionNetworkAdapter>();
            if (adapter != null && adapter.GetComponent<LivingWorldNetworkBinding>() == null)
                adapter.gameObject.AddComponent<LivingWorldNetworkBinding>();
        }

        private void Awake()
        {
            current = this;
            manager = GetComponent<NetworkManager>();
            adapter = GetComponent<SessionNetworkAdapter>();
            if (manager == null || adapter == null) enabled = false;
        }

        private bool IsHost => manager != null && manager.IsListening && manager.IsServer && adapter.IsAuthority;

        private void Update()
        {
            if (manager.IsListening && !registered) Register();
            if (!manager.IsListening && registered) Unregister();
            if (!IsHost) { Unbind(); return; }
            Bind();
            if (!bound || day == null) return;
            // Process yesterday's *settled* channel output before changing to today's objectives.
            // This survives a restart between the day-close disk write and the morning settlement.
            if (authority.DayNumber > 0 && authority.DayNumber < day.Engine.DayNumber)
                ReconcileSettledPublications(authority.DayNumber);
            if (authority.DayNumber != day.Engine.DayNumber)
                authority.BeginDay(day.Engine.DayNumber, "local-host");
            if (authority.DayNumber != day.Engine.DayNumber) return;

            // The host's stable D06 role is restored after the network player spawns.
            if (manager.ConnectedClients.TryGetValue(0, out var host) && host.PlayerObject != null)
                ApplyRole(new PlayerId(0), authority.RoleFor(new PlayerId(0)));

            if (authority.Revision == lastMirrorRevision) return;
            lastMirrorRevision = authority.Revision;
            Mirrored = authority.Export();
            MirroredRevision = lastMirrorRevision;
            HasMirror = true;
            foreach (var id in manager.ConnectedClientsIds)
                if (id != NetworkManager.ServerClientId) SendMirror(id);
        }

        private void Bind()
        {
            if (bound) return;
            economy = GetComponent<EconomyManager>();
            store = GetComponent<EconomySaveStore>();
            day = GetComponent<DayManager>();
            crew = GetComponent<CrewRoleEffectBinding>();
            media = GetComponent<MediaNetworkBinding>();
            if (economy == null || store == null || day == null || crew == null || media == null) return;
            authority = new LivingWorldAuthority(economy, store.SaveNow, ApplyRole,
                id => id != LivingWorldCatalog.NightRecording);
            store.Living = authority;
            economy.OnVerifiedCatchesSold += OnVerifiedCatches;
            bound = true;
            lastMirrorRevision = -1;
        }

        private void Unbind()
        {
            if (!bound) return;
            if (economy != null) economy.OnVerifiedCatchesSold -= OnVerifiedCatches;
            if (store != null) store.Living = null;
            bound = false;
            authority = null;
            lastMirrorRevision = -1;
        }

        private void OnVerifiedCatches(System.Collections.Generic.IReadOnlyList<PendingTurnInState> catches)
        {
            if (IsHost && authority != null) authority.OnVerifiedCatchesSold(catches);
        }

        private void ReconcileSettledPublications(int orderDay)
        {
            if (media == null || authority == null) return;
            foreach (var post in media.Channel.Publications())
            {
                if (post.QueuedDay != orderDay || string.IsNullOrWhiteSpace(post.SettledId)) continue;
                if (media.Channel.TryGetClip(post.ClipId, out var clip))
                    authority.OnVerifiedPublication(clip, post);
            }
        }

        private bool ApplyRole(PlayerId player, CrewRole role)
        {
            if (crew == null) return false;
            if (!manager.ConnectedClients.TryGetValue(player.Value, out var client) || client.PlayerObject == null)
                return false;
            var diver = client.PlayerObject.GetComponent<NetworkPlayer>();
            if (diver == null || !diver.IsSpawned) return false;
            crew.TryApplyRoleServer(player, role);
            return diver.CurrentCrewRole == role;
        }

        private bool IsInTown(ulong clientId)
        {
            if (!IsHost || DayLock.IsLocked || adapter.Session.State.Phase == SessionPhase.Dive ||
                !manager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
                return false;
            foreach (var anchor in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
                if (anchor.Definition.ServiceId == TownServiceCatalog.EquipmentShopId &&
                    Vector3.Distance(anchor.WorldPosition, client.PlayerObject.transform.position) <= TownRangeMetres)
                    return true;
            return false;
        }

        private TransactionResult HandleAction(ulong sender, byte operation, string target, ulong requestId)
        {
            if (!bound || !IsInTown(sender)) return TransactionResult.Reject(requestId, "NotInTown", 0);
            if (operation == 1 && byte.TryParse(target, out var roleId))
                return authority.TrySelectRole(new PlayerId(sender), (CrewRole)roleId, requestId, true);
            if (operation == 2)
                return authority.TryUpgrade(new PlayerId(sender), target, requestId, true);
            return TransactionResult.Reject(requestId, "InvalidTarget", authority.Revision);
        }

        public static ulong RequestRole(CrewRole role) => Request(1, ((byte)role).ToString());
        public static ulong RequestUpgrade(string upgradeId) => Request(2, upgradeId);

        private static ulong Request(byte op, string target)
        {
            if (current == null || current.manager == null || !current.manager.IsListening) return 0;
            var binding = current;
            var id = ++binding.localRequestId;
            if (binding.manager.IsServer)
            {
                var result = binding.HandleAction(binding.manager.LocalClientId, op, target, id);
                LastResult = result.Accepted ? "ISLEM ONAYLANDI" : result.ReasonCode;
                return id;
            }
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(op);
            writer.WriteValueSafe(id);
            writer.WriteValueSafe(new FixedString64Bytes(target ?? ""));
            binding.manager.CustomMessagingManager.SendNamedMessage(RequestMessage,
                NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            LastResult = "ISTEK GONDERILDI";
            return id;
        }

        private void ReceiveRequest(ulong sender, FastBufferReader reader)
        {
            if (!IsHost) return;
            reader.ReadValueSafe(out byte op);
            reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out FixedString64Bytes target);
            var result = HandleAction(sender, op, target.ToString(), id);
            using var writer = new FastBufferWriter(96, Allocator.Temp);
            writer.WriteValueSafe(id);
            writer.WriteValueSafe(result.Accepted);
            writer.WriteValueSafe(new FixedString64Bytes(result.ReasonCode ?? ""));
            manager.CustomMessagingManager.SendNamedMessage(ResultMessage, sender, writer, NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveResult(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out bool accepted);
            reader.ReadValueSafe(out FixedString64Bytes reason);
            LastResult = accepted ? "ISLEM ONAYLANDI" : reason.ToString();
        }

        private void SendMirror(ulong clientId)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Mirrored));
            using var writer = new FastBufferWriter(bytes.Length + 16, Allocator.Temp);
            writer.WriteValueSafe(MirroredRevision);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            manager.CustomMessagingManager.SendNamedMessage(MirrorMessage, clientId, writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void ReceiveMirror(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int revision);
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > 32768 || revision < MirroredRevision) return;
            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            try
            {
                var restored = JsonUtility.FromJson<LivingWorldSaveData>(System.Text.Encoding.UTF8.GetString(bytes));
                if (restored == null) return;
                Mirrored = restored;
                MirroredRevision = revision;
                HasMirror = true;
            }
            catch (ArgumentException) { }
        }

        private void Register()
        {
            var messages = manager.CustomMessagingManager;
            if (messages == null) return;
            serverRegistered = manager.IsServer;
            if (serverRegistered)
            {
                messages.RegisterNamedMessageHandler(RequestMessage, ReceiveRequest);
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
                if (serverRegistered) messages.UnregisterNamedMessageHandler(RequestMessage);
                else
                {
                    messages.UnregisterNamedMessageHandler(ResultMessage);
                    messages.UnregisterNamedMessageHandler(MirrorMessage);
                }
            }
            if (serverRegistered) manager.OnClientConnectedCallback -= OnClientConnected;
            registered = false;
            serverRegistered = false;
            HasMirror = false;
            MirroredRevision = -1;
            Mirrored = new LivingWorldSaveData();
            Unbind();
        }

        private void OnClientConnected(ulong id)
        {
            if (HasMirror && id != NetworkManager.ServerClientId) SendMirror(id);
        }

        private void OnDisable()
        {
            if (registered && manager != null) Unregister();
            Unbind();
            if (current == this) current = null;
        }
    }
}
