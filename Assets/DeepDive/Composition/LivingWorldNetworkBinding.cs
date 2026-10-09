using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Living;
using DeepDive.Media;
using DeepDive.Network;
using DeepDive.Session;
using DeepDive.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    [Serializable]
    public sealed class LivingMirrorData
    {
        public int Revision;
        public int Day;
        public string OrderId = "";
        public byte OrderStatus;
        public int OrderProgress;
        public string SponsorId = "";
        public byte SponsorStatus;
        public int SponsorProgress;
        public List<string> Development = new List<string>();
        public List<ulong> RoleClients = new List<ulong>();
        public List<byte> RoleValues = new List<byte>();
    }

    // P4.5-C (#132) composition root for the daily orders/sponsors, the home/town development and the light roles. It owns no rule: those
    // are LivingWorldAuthority's, the money is EconomyManager's. It (a) builds the authority while this machine is the host and binds the
    // seams the other systems read (DevelopmentEffects, OrderWorld default) and applies the chosen roles through Mehmet's CrewRoleEffectBinding, (b) feeds it the HOST-verified events (a settled
    // fish hand-in, an accepted publication) and the day, (c) takes build/role requests from any process, checks on the HOST that the
    // sender is a real player standing at the home PC outside a dive, and answers, and (d) mirrors the state to every process.
    [DisallowMultipleComponent]
    public sealed class LivingWorldNetworkBinding : MonoBehaviour
    {
        private const string ActionMessage = "DeepDive.P45.Action.v1";
        private const string ResultMessage = "DeepDive.P45.ActionResult.v1";
        private const string MirrorMessage = "DeepDive.P45.Living.v1";
        private const float PcRange = 3.25f;
        private const float DayPollInterval = 0.5f;
        private const byte KindBuild = 1;
        private const byte KindRole = 2;

        // What this process shows (host: the live authority; clients: the last mirrored state).
        public static LivingMirrorData Mirrored { get; private set; } = new LivingMirrorData();
        public static bool HasMirror { get; private set; }
        public static ulong LastResultRequest { get; private set; }
        public static bool LastResultAccepted { get; private set; }
        public static string LastResultReason { get; private set; } = "";

        private static LivingWorldNetworkBinding current;

        private SessionNetworkAdapter adapter;
        private NetworkManager manager;
        private EconomyManager economy;
        private EconomySaveStore store;
        private LivingWorldAuthority authority;
        private MediaNetworkBinding media;
        private EconomyMoney moneyAdapter;
        private DefaultOrderWorld defaultWorld;
        private Func<DevelopmentState> developmentProvider;
        private CrewRoleEffectBinding roleEffects;
        private bool registered, registeredAsServer, hostBound;
        private int sentRevision = -1;
        private double nextDayPoll;
        private ulong localRequest;

        public LivingWorldAuthority Authority => authority;

        public static DailyBoardState Board
        {
            get
            {
                var m = Mirrored;
                return new DailyBoardState(m.Day, Slot(m.OrderId, m.OrderStatus, m.OrderProgress), Slot(m.SponsorId, m.SponsorStatus, m.SponsorProgress), m.Revision);
            }
        }

        public static DevelopmentState Development => new DevelopmentState(Mirrored.Development, Mirrored.Revision);

        public static CrewRole RoleOfClient(ulong clientId)
        {
            var i = Mirrored.RoleClients.IndexOf(clientId);
            return i >= 0 && i < Mirrored.RoleValues.Count ? (CrewRole)Mirrored.RoleValues[i] : CrewRole.None;
        }

        private static ContractState Slot(string templateId, byte status, int progress)
        {
            if (string.IsNullOrEmpty(templateId) || !ContractCatalog.TryGet(templateId, out var t)) return default;
            return new ContractState(templateId, (ContractStatus)status, progress, t.Target, t.Reward);
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
            if (session != null && session.GetComponent<LivingWorldNetworkBinding>() == null)
                session.gameObject.AddComponent<LivingWorldNetworkBinding>();
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

            if (!manager.IsServer || !adapter.IsAuthority) { ReleaseHost(); return; }
            BindHost();
            if (!hostBound) return;

            var now = Time.unscaledTimeAsDouble;
            if (now >= nextDayPoll)
            {
                nextDayPoll = now + DayPollInterval;
                EnsureToday();
                ApplyRoles();
            }
            if (authority.Revision != sentRevision)
            {
                sentRevision = authority.Revision;
                Mirrored = BuildMirror();
                HasMirror = true;
                foreach (var client in manager.ConnectedClientsIds)
                    if (client != NetworkManager.ServerClientId) SendMirror(client);
            }
        }

        // ---- host ------------------------------------------------------------------------------------------------------

        private void BindHost()
        {
            if (hostBound) return;
            if (economy == null) economy = GetComponent<EconomyManager>();
            if (store == null) store = GetComponent<EconomySaveStore>();
            if (media == null) media = GetComponent<MediaNetworkBinding>();
            if (economy == null || store == null || media == null) return;

            moneyAdapter = new EconomyMoney(economy);
            authority = new LivingWorldAuthority(moneyAdapter, store.SaveNow);
            store.Living = authority;   // hands the authority what the campaign file already held (absent = the empty default)

            developmentProvider = () => authority.Development;
            DevelopmentEffects.Bind(developmentProvider);
            if (OrderWorld.Current == null)
            {
                defaultWorld = new DefaultOrderWorld(media, economy);
                OrderWorld.Bind(defaultWorld);
            }
            economy.OnCatchesSold += OnCatchesSold;
            media.Channel.OnPublished += OnPublished;
            manager.OnClientDisconnectCallback += OnClientDisconnected;
            manager.OnClientConnectedCallback += OnClientConnected;
            hostBound = true;
            EnsureToday();
        }

        private void ReleaseHost()
        {
            if (!hostBound) return;
            // Save before letting go (a town read after the dive stays current), then unbind everything this machine bound.
            if (store != null) store.SaveNow();
            if (economy != null) economy.OnCatchesSold -= OnCatchesSold;
            if (media != null) media.Channel.OnPublished -= OnPublished;
            if (manager != null)
            {
                manager.OnClientDisconnectCallback -= OnClientDisconnected;
                manager.OnClientConnectedCallback -= OnClientConnected;
            }
            DevelopmentEffects.Unbind(developmentProvider);
            if (defaultWorld != null) { OrderWorld.Unbind(defaultWorld); defaultWorld = null; }
            if (store != null && ReferenceEquals(store.Living, authority)) store.Living = null;
            authority = null;
            developmentProvider = null;
            hostBound = false;
            sentRevision = -1;
        }

        // The chosen roles reach the players through Mehmet's effect layer (TryApplyRoleServer: replacement-only, recomputed from the base value, so a
        // repeat can never stack). Reconciling every poll covers a restore, a respawn (his player resets to None) and a rejoin; it never acts in a dive.
        private void ApplyRoles()
        {
            if (!hostBound) return;
            if (roleEffects == null) roleEffects = GetComponent<CrewRoleEffectBinding>();
            if (roleEffects == null || adapter.Session.State.Phase == SessionPhase.Dive) return;
            foreach (var pair in manager.ConnectedClients)
            {
                if (pair.Value.PlayerObject == null) continue;
                var diver = pair.Value.PlayerObject.GetComponent<NetworkPlayer>();
                if (diver == null || !diver.IsSpawned || !diver.IsServer) continue;
                var player = new PlayerId(pair.Key);
                var desired = authority.RoleOf(player);
                if (diver.CurrentCrewRole != desired) roleEffects.TryApplyRoleServer(player, desired);
            }
        }

        private void EnsureToday()
        {
            if (DayLock.StateProvider == null) return;
            var state = DayLock.StateProvider();
            authority.EnsureDay(state.DayNumber, state.WeatherSeed);
        }

        private void OnCatchesSold(string dealId, IReadOnlyList<SoldCatch> catches)
        {
            if (!hostBound) return;
            EnsureToday();   // a sale on a new day's first minute must meet that day's order
            authority.OnCatchesSold(dealId, catches);
        }

        private void OnPublished(PublicationSave publication, ClipManifest clip)
        {
            if (!hostBound) return;
            EnsureToday();
            authority.OnPublication(publication.PublicationId, clip);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (hostBound) authority.ForgetPlayer(new PlayerId(clientId));
        }

        private void OnClientConnected(ulong clientId)
        {
            if (hostBound && HasMirror && clientId != NetworkManager.ServerClientId) SendMirror(clientId);
        }

        private LivingMirrorData BuildMirror()
        {
            var board = authority.Board;
            var data = new LivingMirrorData
            {
                Revision = authority.Revision,
                Day = board.Day,
                OrderId = board.Order.TemplateId, OrderStatus = (byte)board.Order.Status, OrderProgress = board.Order.Progress,
                SponsorId = board.Sponsor.TemplateId, SponsorStatus = (byte)board.Sponsor.Status, SponsorProgress = board.Sponsor.Progress,
                Development = new List<string>(authority.Development.OwnedIds)
            };
            foreach (var pair in authority.Roles)
            {
                data.RoleClients.Add(pair.Key.Value);
                data.RoleValues.Add((byte)pair.Value);
            }
            return data;
        }

        // The sender's own host-authoritative position against the physical PC, and a real, active, connected player.
        private bool IsAtPc(ulong clientId)
        {
            var pc = FindFirstObjectByType<HomePcAnchor>();
            if (pc == null) return false;
            foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                if (p.IsSpawned && p.OwnerClientId == clientId)
                    return Vector3.Distance(p.transform.position, pc.transform.position) <= PcRange;
            return false;
        }

        private TransactionResult HandleAction(ulong sender, byte kind, string arg, ulong requestId)
        {
            if (!hostBound) return TransactionResult.Reject(requestId, "InvalidState", 0);
            var player = new PlayerId(sender);
            if (!adapter.Session.Roster.ContainsKey(player)) return TransactionResult.Reject(requestId, "PlayerInactive", authority.Revision);
            if (adapter.Session.State.Phase == SessionPhase.Dive) return TransactionResult.Reject(requestId, "WrongPhase", authority.Revision);
            if (!IsAtPc(sender)) return TransactionResult.Reject(requestId, "NotAtPc", authority.Revision);

            switch (kind)
            {
                case KindBuild:
                    if (DayLock.IsLocked) return TransactionResult.Reject(requestId, "DayClosing", authority.Revision);
                    return authority.TryBuildDevelopment(player, arg, requestId);
                case KindRole:
                {
                    if (!int.TryParse(arg, out var role) || role < 0 || role > byte.MaxValue) return TransactionResult.Reject(requestId, "InvalidTarget", authority.Revision);
                    var chosen = authority.TrySelectRole(player, (CrewRole)role, requestId);
                    if (chosen.Accepted) ApplyRoles();   // the effect layer sees the new role at once, not a poll later
                    return chosen;
                }
                default:
                    return TransactionResult.Reject(requestId, "InvalidTarget", authority.Revision);
            }
        }

        // ---- requests from any process ----------------------------------------------------------------------------

        public static ulong RequestBuild(string developmentId) => Request(KindBuild, developmentId);

        public static ulong RequestRole(CrewRole role) => Request(KindRole, ((int)role).ToString());

        private static ulong Request(byte kind, string arg)
        {
            if (current == null || current.manager == null || !current.manager.IsListening) return 0;
            var m = current.manager;
            var requestId = ++current.localRequest;
            if (m.IsServer)
            {
                var r = current.HandleAction(m.LocalClientId, kind, arg, requestId);
                SetResult(requestId, r.Accepted, r.ReasonCode);
                return requestId;
            }
            using var writer = new FastBufferWriter(160, Allocator.Temp);
            writer.WriteValueSafe(requestId);
            writer.WriteValueSafe(kind);
            writer.WriteValueSafe(new FixedString64Bytes(arg ?? ""));
            m.CustomMessagingManager.SendNamedMessage(ActionMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            return requestId;
        }

        private static void SetResult(ulong requestId, bool accepted, string reason)
        {
            LastResultRequest = requestId;
            LastResultAccepted = accepted;
            LastResultReason = reason ?? "";
        }

        private void ReceiveAction(ulong sender, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong requestId);
            reader.ReadValueSafe(out byte kind);
            reader.ReadValueSafe(out FixedString64Bytes arg);
            var r = HandleAction(sender, kind, arg.ToString(), requestId);
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

        // ---- mirror ---------------------------------------------------------------------------------------------------

        private void SendMirror(ulong clientId)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Mirrored));
            using var writer = new FastBufferWriter(bytes.Length + 16, Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            manager.CustomMessagingManager.SendNamedMessage(MirrorMessage, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void ReceiveMirror(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > 1 << 16) return;
            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            LivingMirrorData data;
            try { data = JsonUtility.FromJson<LivingMirrorData>(System.Text.Encoding.UTF8.GetString(bytes)); }
            catch (ArgumentException) { return; }
            if (data == null) return;
            Mirrored = data;
            HasMirror = true;
        }

        private void Register()
        {
            var messages = manager.CustomMessagingManager;
            if (messages == null) return;
            registeredAsServer = manager.IsServer;
            if (registeredAsServer) messages.RegisterNamedMessageHandler(ActionMessage, ReceiveAction);
            else
            {
                messages.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
                messages.RegisterNamedMessageHandler(MirrorMessage, ReceiveMirror);
                // A guest has no authority: its reads (UI) follow the host's mirror.
                clientDevelopment = () => Development;
                DevelopmentEffects.Bind(clientDevelopment);
            }
            registered = true;
        }

        private Func<DevelopmentState> clientDevelopment;

        private void Unregister()
        {
            var messages = manager.CustomMessagingManager;
            if (messages != null)
            {
                if (registeredAsServer) messages.UnregisterNamedMessageHandler(ActionMessage);
                else
                {
                    messages.UnregisterNamedMessageHandler(ResultMessage);
                    messages.UnregisterNamedMessageHandler(MirrorMessage);
                }
            }
            if (clientDevelopment != null) { DevelopmentEffects.Unbind(clientDevelopment); clientDevelopment = null; }
            registered = false;
            registeredAsServer = false;
            HasMirror = false;
            Mirrored = new LivingMirrorData();
            ReleaseHost();
        }

        private void OnDisable()
        {
            if (registered && manager != null) Unregister();
            else ReleaseHost();
            if (current == this) current = null;
        }

        // ---- adapters -------------------------------------------------------------------------------------------------

        private sealed class EconomyMoney : ILivingMoney
        {
            private readonly EconomyManager economy;
            public EconomyMoney(EconomyManager economy) => this.economy = economy;
            public int Balance => economy.SharedBalance;
            public bool TryCredit(string rewardId, int amount) => economy.TryCreditReward(rewardId, amount);
            public void ReleaseCredit(string rewardId, int amount) => economy.ReleaseReward(rewardId, amount);
            public bool TrySpend(int amount) => economy.TrySpend(amount);
            public void RefundSpend(int amount) => economy.RefundSpend(amount);
            public void AnnounceSpend(string spendId, int amount) => economy.AnnounceSpend(spendId, amount);
        }

        // The conservative default until Utku's world binds its own (#131): no night capture, and an unrecorded species remains only while
        // no published clip has been the first recording of its species (the only species content this build has is a single fish).
        private sealed class DefaultOrderWorld : IOrderTargetWorld
        {
            private readonly MediaNetworkBinding media;
            private readonly EconomyManager economy;
            private readonly P45ObjectiveContentCatalog content;
            public DefaultOrderWorld(MediaNetworkBinding media, EconomyManager economy)
            { this.media = media; this.economy = economy; content = Resources.Load<P45ObjectiveContentCatalog>(P45ObjectiveContentCatalog.ResourceName); }
            public bool NightCaptureAvailable => false;

            public bool CanOffer(in ContractTemplate template)
            {
                if (DayLock.StateProvider == null || content == null) return false;
                var day = DayLock.StateProvider();
                var subjects = content.Targets.Where(x => x != null && x.IsValid).Select(x => x.SubjectId).Distinct().ToArray();
                var snapshot = ExplorationFeed.Current != null ? ExplorationFeed.Current.Snapshot() : default;
                var reef = snapshot.Cells != null && snapshot.Cells.Any(x => x.Discovered && x.DepthBandId == DepthBandIds.Reef);
                var fleet = economy.Fleet;
                var vessel = VehicleClass.None;
                foreach (var boat in fleet.OwnedBoatIds)
                    if ((int)VehicleCatalog.ClassOf(boat) > (int)vessel) vessel = VehicleCatalog.ClassOf(boat);
                foreach (var entry in content.Targets)
                {
                    if (entry == null || !entry.IsValid) continue;
                    if (template.Kind == ContractKind.FishOrder && entry.Species == null) continue;
                    if (template.Measure == ContractMeasure.PublishEvent && entry.Event == null) continue;
                    if (template.Measure == ContractMeasure.PublishNewSpecies && entry.Species == null) continue;
                    var id = entry.SubjectId;
                    if (template.SpeciesId.Length > 0 && template.SpeciesId != id) continue;
                    if (template.Measure == ContractMeasure.PublishNewSpecies && AlreadyPublished(id)) continue;
                    var target = new P45ObjectiveTarget(id, entry.HabitatBandId, entry.RouteId, template.RequiresNight);
                    if (P45WorldObjectiveEligibility.CanOffer(target, subjects, vessel, reef,
                        P45WorldRules.Weather(day.DayNumber, day.WeatherSeed), day.ClockMinute, day.Phase)) return true;
                }
                return false;
            }

            private bool AlreadyPublished(string speciesId)
            {
                var clips = media.Channel.Clips();
                return media.Channel.Publications().Any(p => clips.Any(c => c.ClipId == p.ClipId &&
                    c.SubjectId == speciesId && c.WorldContext.FirstRecordingOfSubject));
            }

            public bool UnrecordedSpeciesRemain
            {
                get
                {
                    return content != null && content.Targets.Any(x => x != null && x.IsValid &&
                        x.Species != null && !AlreadyPublished(x.SubjectId));
                }
            }
        }
    }
}
