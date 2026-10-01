using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // The shared home PC as a physical target (#102). Identity only; all state is the channel's.
    [DisallowMultipleComponent]
    public sealed class HomePcAnchor : MonoBehaviour
    {
        private const string HomeSceneName = "PrepArea";
        private const string PcName = "HomeSharedPc";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        // Placed in the home room opposite the shared storage (Mehmet's rig keeps the beds on the far wall and
        // the storage at -3.9,-3.55); primitive until the home art replaces it, like the rest of the P4 rig.
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != HomeSceneName || GameObject.Find(PcName) != null) return;
            var pc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pc.name = PcName;
            pc.transform.position = new Vector3(3.9f, 0.6f, -3.55f);
            pc.transform.localScale = new Vector3(1.4f, 1.2f, 0.7f);
            pc.AddComponent<HomePcAnchor>();
        }
    }

    // The PC screen (#102): archive list, clip details/preview, title + publish, and the shared channel feed.
    // Shows only what the host mirrored (MediaNetworkBinding.Mirrored) and sends publish requests; the host
    // decides everything (owner, commercial right, PC proximity). No world or fish position is on this screen.
    [DisallowMultipleComponent]
    public sealed class HomePcView : MonoBehaviour
    {
        private const float ShowRange = 4f;

        public static bool PanelOpen { get; private set; }
        public static string Selected { get; private set; } = "";

        private HomePcAnchor pc;
        private NetworkPlayer local;
        private float nextLookup;
        private string title = "";
        private string status = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<HomePcView>() == null)
                session.gameObject.AddComponent<HomePcView>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextLookup || pc == null || local == null)
            {
                nextLookup = Time.unscaledTime + 0.5f;
                pc = FindFirstObjectByType<HomePcAnchor>();
                local = null;
                foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                    if (p.IsSpawned && p.IsOwner) { local = p; break; }
            }
            PanelOpen = pc != null && local != null && Vector3.Distance(local.transform.position, pc.transform.position) <= ShowRange;
        }

        public static void Select(string clipId) => Selected = clipId ?? "";

        private void OnGUI()
        {
            if (!PanelOpen || local == null) return;
            var me = local.OwnerClientId;
            var data = MediaNetworkBinding.Mirrored;
            var published = new HashSet<string>();
            foreach (var p in data.Publications) published.Add(p.ClipId);

            var box = new Rect(Screen.width * 0.5f - 330f, 60f, 660f, 420f);
            GUI.Box(box, $"EV PC  -  arsiv {data.Clips.Count} klip  |  kanal: {data.Followers} takipci");

            GUI.Label(new Rect(box.x + 10, box.y + 24, 320, 20), "ARSIV");
            var y = box.y + 44;
            foreach (var clip in data.Clips)
            {
                if (y > box.y + 250) break;
                var mine = clip.OwnerPlayerId == me;
                var tag = published.Contains(clip.ClipId) ? " [yayinda]" : clip.Quality >= 1 && clip.SafeReturned && clip.MediaReady ? "" : " [ticari degil]";
                if (GUI.Button(new Rect(box.x + 10, y, 320, 20),
                        $"{(mine ? "*" : " ")} gun {clip.DayNumber}  {Label(clip.SubjectId)}  {clip.DurationSeconds:0}s  Q{clip.Quality}{tag}"))
                    Select(clip.ClipId);
                y += 22;
            }

            var selected = data.Clips.Find(c => c.ClipId == Selected);
            if (selected != null)
            {
                var dx = box.x + 340;
                GUI.Label(new Rect(dx, box.y + 24, 310, 20), $"SECILI: {Label(selected.SubjectId)} (gun {selected.DayNumber})");
                GUI.Label(new Rect(dx, box.y + 44, 310, 20), $"cekim: P{selected.OwnerPlayerId}  sure {selected.DurationSeconds:0.0}s  kalite {selected.Quality}");
                if (ClipPlayback.CanPlay(selected.ClipId))
                {
                    if (GUI.Button(new Rect(dx, box.y + 66, 150, 22), "OYNAT")) ClipPlayback.TryPlay(selected.ClipId);
                }
                else GUI.Label(new Rect(dx, box.y + 66, 310, 20), "Oynatici henuz bagli degil (#100).");

                if (selected.OwnerPlayerId == me && !published.Contains(selected.ClipId))
                {
                    GUI.Label(new Rect(dx, box.y + 96, 60, 20), "Baslik");
                    title = GUI.TextField(new Rect(dx + 50, box.y + 96, 260, 20), title, ChannelIds.MaxTitleLength);
                    if (GUI.Button(new Rect(dx, box.y + 122, 150, 24), "YAYINLA"))
                    {
                        MediaNetworkBinding.RequestPublish(selected.ClipId, title);
                        status = "istek gonderildi";
                    }
                }
            }
            if (MediaNetworkBinding.LastResultRequest != 0)
                status = MediaNetworkBinding.LastResultAccepted ? "YAYIN SIRAYA ALINDI (sonuc yarin sabah)" : Friendly(MediaNetworkBinding.LastResultReason);
            GUI.Label(new Rect(box.x + 340, box.y + 152, 310, 20), status);

            GUI.Label(new Rect(box.x + 10, box.y + 270, 640, 20), "KANAL");
            y = box.y + 290;
            for (var i = data.Publications.Count - 1; i >= 0 && y < box.y + 410; i--, y += 20)
            {
                var p = data.Publications[i];
                var result = string.IsNullOrEmpty(p.SettledId) ? "sonuc bekleniyor" : $"{p.Views} izlenme, +{p.FollowersGained} takipci, +{p.Income} kredi";
                GUI.Label(new Rect(box.x + 10, y, 640, 20), $"\"{p.Title}\"  (P{p.OwnerPlayerId}, gun {p.QueuedDay})  -  {result}");
            }
        }

        private static string Label(string subjectId) => subjectId == "sea_bass" ? "Levrek"
            : subjectId == "event_bioluminescence" ? "Biyoluminesans" : string.IsNullOrEmpty(subjectId) ? "bos kadraj" : subjectId;

        private static string Friendly(string reason) => reason switch
        {
            "NotAtPc" => "PC'DEN UZAKSIN",
            "NotOwner" => "BU KLIP SENIN DEGIL",
            "NotPublishable" => "BU KLIP YAYINLANAMAZ",
            "MediaNotReady" => "MEDYA HAZIR DEGIL",
            "PublicationAlreadyQueued" => "ZATEN YAYINDA",
            "RightsConsumed" => "BU KAYIT NPC'YE SATILDI",
            "DayClosing" => "GUN KAPANIYOR",
            "SaveFailed" => "KAYIT HATASI",
            _ => "YAYIN REDDEDILDI"
        };
    }
}
