using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.MapUI;
using DeepDive.Network;
using DeepDive.Trip;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // The local player's map screen (#66). It owns no trip, position or transform rules: the trip
    // phase and the boat's approved pose come from Mehmet's BoatTripPlayerSync mirror, the party's
    // approved positions are the host-authoritative NetworkPlayer transforms every client already
    // replicates, the world<->map arithmetic is Utku's DiveRegionField, and which icons exist is
    // BoatMapPresenter's. This class only gathers those inputs and draws the result.
    //
    // Nothing here is a second source of truth: a position that cannot be converted (outside the
    // region) or a boat pose the host has not published is left off the map, never guessed.
    [DisallowMultipleComponent]
    public sealed class BoatMapView : MonoBehaviour
    {
        public const KeyCode ToggleKey = KeyCode.M;
        private const float RefreshInterval = 0.1f;
        private const float MapSize = 220f;

        // What the map currently shows, for the on-screen draw and for smoke evidence. Rebuilt on a
        // timer rather than per frame: icons move at most at boat speed and the presenter allocates.
        public static IReadOnlyList<MapIcon> LastIcons { get; private set; } = new List<MapIcon>();
        public static BoatTripPhase LastPhase { get; private set; }
        public static bool LastHadRegion { get; private set; }

        // #90 exploration layer: the fog tiles and encyclopedia this process is showing, rebuilt from
        // ExplorationMirror (host: the live read model; clients: the host's mirrored snapshot).
        public const KeyCode EncyclopediaKey = KeyCode.J;
        public static IReadOnlyList<FogTile> LastFog { get; private set; } = new List<FogTile>();
        public static IReadOnlyList<EncyclopediaEntry> LastEncyclopedia { get; private set; } = new List<EncyclopediaEntry>();
        private int fogRevision = -1;
        private bool encyclopediaVisible;

        private bool visible = true;
        private float nextRefresh;
        private BoatTripPlayerSync localSync;
        private ulong localId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<BoatMapView>() != null) return;
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null) session.gameObject.AddComponent<BoatMapView>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey)) visible = !visible;
            if (Input.GetKeyDown(EncyclopediaKey)) encyclopediaVisible = !encyclopediaVisible;
            RebuildExploration();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            Rebuild();
        }

        private void Rebuild()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening || !ResolveLocalSync(manager) ||
                !DiveRegionField.TryFind(out var region))
            {
                LastIcons = new List<MapIcon>();
                LastHadRegion = false;
                return;
            }

            LastHadRegion = true;
            if (!TryAnchor(DiveRouteAnchors.Dock, region, out var dock)) return;
            var hasAnchor = TryAnchor(DiveRouteAnchors.AnchorPoint, region, out var anchor);

            (float X, float Z)? live = null;
            if (localSync.BoatVisible.Value && region.TryWorldToMap(localSync.BoatWorldPosition.Value, out var boatMap))
                live = (boatMap.x, boatMap.y);

            var party = new List<(PlayerId Player, float X, float Z)>();
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
            {
                if (!players[i].IsSpawned) continue;
                // Refused out-of-region positions drop the icon; they are not clamped to an edge.
                if (region.TryWorldToMap(players[i].transform.position, out var map))
                    party.Add((new PlayerId(players[i].OwnerClientId), map.x, map.y));
            }

            LastPhase = (BoatTripPhase)localSync.Phase.Value;
            LastIcons = BoatMapPresenter.BuildIcons(
                LastPhase, ActiveVehicle.BoatId, dock, hasAnchor ? anchor : ((float, float)?)null,
                live, party, localSync.IsSeated);
        }

        private void RebuildExploration()
        {
            if (!ExplorationMirror.HasData)
            {
                if (LastFog.Count > 0) { LastFog = new List<FogTile>(); LastEncyclopedia = new List<EncyclopediaEntry>(); }
                fogRevision = -1;
                return;
            }
            var snapshot = ExplorationMirror.Current;
            if (snapshot.Revision == fogRevision) return;
            fogRevision = snapshot.Revision;
            LastFog = ExplorationMapPresenter.BuildFog(snapshot);
            LastEncyclopedia = EncyclopediaPresenter.Build(snapshot);
        }

        private bool ResolveLocalSync(NetworkManager manager)
        {
            if (localSync != null && localSync.IsSpawned && localSync.IsOwner) return true;
            localSync = null;
            var syncs = FindObjectsByType<BoatTripPlayerSync>(FindObjectsSortMode.None);
            for (var i = 0; i < syncs.Length; i++)
            {
                if (!syncs[i].IsSpawned || !syncs[i].IsOwner) continue;
                localSync = syncs[i];
                localId = manager.LocalClientId;
                return true;
            }
            return false;
        }

        private static bool TryAnchor(string anchorId, DiveRegionField region, out (float X, float Z) map)
        {
            map = default;
            var anchors = FindObjectsByType<RouteAnchor>(FindObjectsSortMode.None);
            for (var i = 0; i < anchors.Length; i++)
            {
                if (anchors[i].AnchorId != anchorId) continue;
                if (!region.TryWorldToMap(anchors[i].WorldPosition, out var m)) return false;
                map = (m.x, m.y);
                return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (encyclopediaVisible) DrawEncyclopedia();
            if (!visible || LastIcons.Count == 0) return;

            var rect = new Rect(Screen.width - MapSize - 20f, 20f, MapSize, MapSize);
            var previous = GUI.color;
            GUI.color = new Color(0.05f, 0.18f, 0.28f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            DrawFog(rect);
            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, MapSize, 20f), $"HARITA [{ToggleKey}]  sandal: {LastPhase}");
            if (LastFog.Count > 0)
                GUI.Label(new Rect(rect.x + 4f, rect.y + MapSize - 20f, MapSize, 20f),
                    $"kesif {ExplorationMapPresenter.DiscoveredCount(LastFog)}/{LastFog.Count}   ansiklopedi [{EncyclopediaKey}]");

            for (var i = 0; i < LastIcons.Count; i++) DrawIcon(rect, LastIcons[i]);
        }

        // Undiscovered cells are covered; discovered ones show their depth band. Tiles and icons share the
        // region's 0..1 square (ExplorationMapProjection: exact for the -15..15 arena on the 5 m lines).
        private static void DrawFog(Rect map)
        {
            var previous = GUI.color;
            for (var i = 0; i < LastFog.Count; i++)
            {
                var t = LastFog[i];
                GUI.color = !t.Discovered ? new Color(0.02f, 0.03f, 0.05f, 0.9f)
                    : t.DepthBandId == DepthBandIds.Shallow ? new Color(0.25f, 0.6f, 0.75f, 0.35f)
                    : new Color(0.3f, 0.5f, 0.55f, 0.2f);
                var x = map.x + (t.CenterU - t.Width * 0.5f) * map.width;
                var y = map.y + (1f - t.CenterV - t.Height * 0.5f) * map.height;
                GUI.DrawTexture(new Rect(x, y, t.Width * map.width, t.Height * map.height), Texture2D.whiteTexture);
            }
            GUI.color = previous;
        }

        private static void DrawEncyclopedia()
        {
            var rows = Mathf.Max(1, LastEncyclopedia.Count);
            var box = new Rect(20f, Screen.height - 60f - rows * 44f, 380f, 36f + rows * 44f);
            GUI.Box(box, $"ANSIKLOPEDI [{EncyclopediaKey}]  ({LastEncyclopedia.Count} tur)");
            if (LastEncyclopedia.Count == 0)
            {
                GUI.Label(new Rect(box.x + 10f, box.y + 26f, 360f, 20f), "Henuz dogrulanmis bir karsilasma yok.");
                return;
            }
            for (var i = 0; i < LastEncyclopedia.Count; i++)
            {
                var e = LastEncyclopedia[i];
                var y = box.y + 26f + i * 44f;
                GUI.Label(new Rect(box.x + 10f, y, 360f, 20f),
                    $"{e.DisplayName}  -  {e.Category}   [{e.ProgressSteps}/{EncyclopediaEntry.MaxProgressSteps}]");
                var habitat = e.HabitatKnown ? string.Join(", ", System.Linq.Enumerable.Select(e.HabitatBandIds, EncyclopediaPresenter.BandLabel)) : "kayit gerekir";
                GUI.Label(new Rect(box.x + 22f, y + 18f, 350f, 20f),
                    $"siluet {(e.Silhouette ? "acik" : "-")}  |  av verisi {(e.CatchData ? "acik" : "av gerekir")}  |  habitat: {habitat}");
            }
        }

        private void DrawIcon(Rect map, MapIcon icon)
        {
            // Map Z grows "up" the screen, so the screen Y is inverted.
            var center = new Vector2(map.x + icon.MapX * map.width, map.y + (1f - icon.MapZ) * map.height);
            Color color;
            string label;
            float size;

            if (icon.IconId == BoatMapPresenter.DockIconId)
            { color = new Color(0.95f, 0.8f, 0.3f); label = "iskele"; size = 10f; }
            else if (icon.IconId == BoatMapPresenter.ReturnMarkerIconId)
            { color = new Color(0.4f, 1f, 0.5f, 0.45f); label = "DON"; size = 18f; }
            else if (icon.IconId.StartsWith(BoatMapPresenter.PlayerIconPrefix, System.StringComparison.Ordinal))
            {
                var isSelf = icon.IconId == BoatMapPresenter.PlayerIconPrefix + localId;
                color = isSelf ? Color.white : new Color(0.75f, 0.85f, 1f);
                label = isSelf ? "sen" : "P" + icon.IconId.Substring(BoatMapPresenter.PlayerIconPrefix.Length);
                size = 7f;
            }
            else
            { color = new Color(1f, 0.55f, 0.2f); label = "sandal"; size = 12f; }

            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(center.x + size * 0.5f + 1f, center.y - 9f, 60f, 18f), label);
        }
    }
}
