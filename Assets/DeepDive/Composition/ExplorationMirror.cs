using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // #90: every process renders the same exploration map and encyclopedia, but Utku's authorities live on
    // the host only. The host sends its read model's snapshot to everyone whenever its revision moves (and
    // to a newcomer on connect); clients rebuild an ExplorationSnapshot from it and never write one. The
    // payload is exactly the snapshot's canonical data - grid, cells (discovered + band), per-species
    // evidence and habitat ids - so nothing a client shows can be richer than what the host counted, and
    // no position of any kind is in it.
    [DisallowMultipleComponent]
    public sealed class ExplorationMirror : MonoBehaviour
    {
        private const string Message = "DeepDive.P4.Exploration.v1";
        private const float PollInterval = 0.25f;

        // What this process currently shows. On the host it is the live read model's snapshot.
        public static ExplorationSnapshot Current { get; private set; }
        public static bool HasData { get; private set; }

        private NetworkManager manager;
        private bool registered;
        private bool registeredAsServer;
        private int sentRevision = -1;
        private float nextPoll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<ExplorationMirror>() == null)
                session.gameObject.AddComponent<ExplorationMirror>();
        }

        private void Update()
        {
            if (manager == null) manager = GetComponent<NetworkManager>();
            if (manager == null) return;

            if (manager.IsListening && !registered) Register();
            else if (!manager.IsListening && registered) Unregister();
            if (!manager.IsListening) return;

            if (!manager.IsServer || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + PollInterval;

            var model = ExplorationFeed.Current;
            if (model == null)
            {
                HasData = false;
                return;
            }
            if (model.Revision == sentRevision && HasData) return;

            Current = model.Snapshot();
            HasData = true;
            sentRevision = Current.Revision;
            foreach (var client in manager.ConnectedClientsIds)
                if (client != NetworkManager.ServerClientId) Send(client, Current);
        }

        private void Register()
        {
            var messages = manager.CustomMessagingManager;
            if (messages == null) return;
            registeredAsServer = manager.IsServer;
            if (!registeredAsServer) messages.RegisterNamedMessageHandler(Message, Receive);
            else manager.OnClientConnectedCallback += OnClientConnected;
            registered = true;
        }

        private void Unregister()
        {
            if (manager.CustomMessagingManager != null && !registeredAsServer)
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(Message);
            if (registeredAsServer) manager.OnClientConnectedCallback -= OnClientConnected;
            registered = false;
            registeredAsServer = false;
            sentRevision = -1;
            HasData = false;
            Current = default;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (HasData && clientId != NetworkManager.ServerClientId) Send(clientId, Current);
        }

        private void Send(ulong clientId, in ExplorationSnapshot snapshot)
        {
            var bytes = Encode(snapshot);
            using var writer = new FastBufferWriter(bytes.Length + 8, Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes);
            manager.CustomMessagingManager.SendNamedMessage(Message, clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }

        private void Receive(ulong sender, FastBufferReader reader)
        {
            if (sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > 1 << 20) return;
            var bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            if (!TryDecode(bytes, out var snapshot)) return;
            Current = snapshot;
            HasData = true;
        }

        // ---- wire format: plain JSON of a serializable copy (small: 36 cells + a few species) ----------

        [Serializable] private sealed class Wire
        {
            public string RegionId = "";
            public int MinGridX, MinGridZ, Columns, Rows, Revision;
            public List<WireCell> Cells = new List<WireCell>();
            public List<WireSpecies> Species = new List<WireSpecies>();
        }

        [Serializable] private sealed class WireCell
        {
            public string CellId = "";
            public int GridX, GridZ;
            public bool Discovered;
            public string Band = "";
        }

        [Serializable] private sealed class WireSpecies
        {
            public string SpeciesId = "";
            public bool Sighted, Recorded, Caught;
            public List<string> Bands = new List<string>();
            public List<string> Cells = new List<string>();
            public List<string> Observations = new List<string>();
        }

        public static byte[] Encode(in ExplorationSnapshot snapshot)
        {
            var wire = new Wire
            {
                RegionId = snapshot.Grid.RegionId, MinGridX = snapshot.Grid.MinGridX, MinGridZ = snapshot.Grid.MinGridZ,
                Columns = snapshot.Grid.Columns, Rows = snapshot.Grid.Rows, Revision = snapshot.Revision
            };
            foreach (var c in snapshot.Cells)
                wire.Cells.Add(new WireCell { CellId = c.CellId, GridX = c.GridX, GridZ = c.GridZ, Discovered = c.Discovered, Band = c.DepthBandId });
            foreach (var s in snapshot.Species)
                wire.Species.Add(new WireSpecies
                {
                    SpeciesId = s.SpeciesId, Sighted = s.Sighted, Recorded = s.Recorded, Caught = s.Caught,
                    Bands = new List<string>(s.ObservedDepthBandIds), Cells = new List<string>(s.ObservedCellIds),
                    Observations = new List<string>(s.ObservationIds)
                });
            return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(wire));
        }

        public static bool TryDecode(byte[] bytes, out ExplorationSnapshot snapshot)
        {
            snapshot = default;
            Wire wire;
            try { wire = JsonUtility.FromJson<Wire>(System.Text.Encoding.UTF8.GetString(bytes)); }
            catch (ArgumentException) { return false; }
            if (wire == null) return false;

            var grid = new ExplorationGrid(wire.RegionId, wire.MinGridX, wire.MinGridZ, wire.Columns, wire.Rows);
            var cells = new ExplorationCellState[wire.Cells?.Count ?? 0];
            for (var i = 0; i < cells.Length; i++)
            {
                var c = wire.Cells[i];
                cells[i] = new ExplorationCellState(c.CellId, c.GridX, c.GridZ, c.Discovered, c.Band);
            }
            var species = new SpeciesDiscoveryState[wire.Species?.Count ?? 0];
            for (var i = 0; i < species.Length; i++)
            {
                var s = wire.Species[i];
                species[i] = new SpeciesDiscoveryState(s.SpeciesId, s.Sighted, s.Recorded, s.Caught,
                    (s.Bands ?? new List<string>()).ToArray(), (s.Cells ?? new List<string>()).ToArray(),
                    (s.Observations ?? new List<string>()).ToArray());
            }
            snapshot = new ExplorationSnapshot(grid, cells, species, wire.Revision);
            return true;
        }

        private void OnDisable()
        {
            if (registered && manager != null) Unregister();
        }
    }
}
