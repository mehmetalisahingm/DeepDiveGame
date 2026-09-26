using DeepDive.Economy;
using DeepDive.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // The shared home storage panel (#90). It shows only what the host mirrored into the local player's
    // EconomyPlayerSync and sends two requests; it owns no item state and decides nothing. The host
    // re-checks that the player is really at the storage (DayNetworkBinding), so showing the panel from a
    // local distance estimate is convenience, not authority.
    [DisallowMultipleComponent]
    public sealed class HomeStorageView : MonoBehaviour
    {
        private const int VisibleRows = 8;
        private const float ShowRange = HomePlayerInteractionBinding.InteractionRange + 0.75f;

        // What the panel currently offers, for smoke evidence: true while the local player is in range.
        public static bool PanelOpen { get; private set; }

        private HomeInteractionAnchor storage;
        private NetworkPlayer local;
        private EconomyPlayerSync sync;
        private float nextLookup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<HomeStorageView>() == null)
                session.gameObject.AddComponent<HomeStorageView>();
        }

        private void Lookup()
        {
            if (Time.unscaledTime < nextLookup && storage != null && local != null && sync != null) return;
            nextLookup = Time.unscaledTime + 0.5f;
            storage = null;
            var anchors = FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None);
            for (var i = 0; i < anchors.Length; i++)
                if (anchors[i].Kind == HomeInteractionKind.Storage) { storage = anchors[i]; break; }

            local = null;
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            for (var i = 0; i < players.Length; i++)
                if (players[i].IsSpawned && players[i].IsOwner) { local = players[i]; break; }
            sync = local != null ? local.GetComponent<EconomyPlayerSync>() : null;
        }

        private void OnGUI()
        {
            PanelOpen = false;
            Lookup();
            if (storage == null || local == null || sync == null || !sync.IsSpawned) return;
            if (Vector3.Distance(local.transform.position, storage.WorldPosition) > ShowRange) return;
            PanelOpen = true;

            var rows = Mathf.Max(Mathf.Min(sync.CarriedCatchIds.Count, VisibleRows), Mathf.Min(sync.StoredCatchIdList.Count, VisibleRows));
            var height = 56f + Mathf.Max(1, rows) * 24f + 28f;
            var box = new Rect(Screen.width - 350f, 210f, 330f, height);
            GUI.Box(box, $"ORTAK DEPO  ({sync.StoredCatches.Value}/{EconomyManager.StorageCapacityItems})");
            GUI.Label(new Rect(box.x + 8, box.y + 24, 150, 20), "Tasidigin av");
            GUI.Label(new Rect(box.x + 170, box.y + 24, 150, 20), "Depodaki av");

            for (var i = 0; i < Mathf.Min(sync.CarriedCatchIds.Count, VisibleRows); i++)
            {
                var id = sync.CarriedCatchIds[i].ToString();
                if (GUI.Button(new Rect(box.x + 8, box.y + 46 + i * 24, 150, 22), "KOY " + Short(id))) sync.RequestStoreItem(id);
            }
            for (var i = 0; i < Mathf.Min(sync.StoredCatchIdList.Count, VisibleRows); i++)
            {
                var id = sync.StoredCatchIdList[i].ToString();
                if (GUI.Button(new Rect(box.x + 170, box.y + 46 + i * 24, 150, 22), "AL " + Short(id))) sync.RequestRetrieveItem(id);
            }
            if (sync.CarriedCatchIds.Count == 0 && sync.StoredCatchIdList.Count == 0)
                GUI.Label(new Rect(box.x + 8, box.y + 46, 310, 20), "Depo bos, tasidigin av yok.");
        }

        private static string Short(string id) => id.Length <= 14 ? id : id.Substring(0, 14);
    }
}
