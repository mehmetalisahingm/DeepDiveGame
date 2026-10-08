using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.5-C: readable daily board at the physical equipment shop; all actions are
    // intents. The only authoritative state comes back from LivingWorldNetworkBinding.
    [DisallowMultipleComponent]
    public sealed class LivingWorldBoardView : MonoBehaviour
    {
        private const float UseRange = 5f;
        private static readonly string[] UpgradeIds =
        {
            "home-archive", "home-gallery", "town-fish-market", "town-equipment", "town-harbor"
        };
        private static readonly string[] UpgradeNames =
        {
            "Ev arsiv raflari", "Ev sergi duvari", "Balikci tezgahi", "Ekipman vitrini", "Iskele aydinlatmasi"
        };
        private readonly Dictionary<string, GameObject> decorations = new Dictionary<string, GameObject>();
        private NetworkPlayer local;
        private float lookup;
        private bool open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = FindFirstObjectByType<SessionNetworkAdapter>();
            if (session != null && session.GetComponent<LivingWorldBoardView>() == null)
                session.gameObject.AddComponent<LivingWorldBoardView>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= lookup || local == null)
            {
                lookup = Time.unscaledTime + 0.5f;
                local = null;
                foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                    if (player.IsSpawned && player.IsOwner) { local = player; break; }
            }
            if (Input.GetKeyDown(KeyCode.B) && Near(TownServiceCatalog.EquipmentShopId)) open = !open;
            if (!Near(TownServiceCatalog.EquipmentShopId)) open = false;
            UpdateDecorations();
        }

        private bool Near(string serviceId)
        {
            if (local == null) return false;
            foreach (var anchor in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
                if (anchor.Definition.ServiceId == serviceId &&
                    Vector3.Distance(local.transform.position, anchor.WorldPosition) <= UseRange)
                    return true;
            return false;
        }

        private void UpdateDecorations()
        {
            if (SceneManager.GetActiveScene().name != "PrepArea") { ClearDecorations(); return; }
            var upgrades = LivingWorldNetworkBinding.Mirrored.PurchasedUpgradeIds;
            if (!LivingWorldNetworkBinding.HasMirror || upgrades == null) return;
            foreach (var id in UpgradeIds)
            {
                var bought = upgrades.Contains(id);
                if (!bought) { if (decorations.TryGetValue(id, out var old)) Destroy(old); decorations.Remove(id); continue; }
                if (decorations.ContainsKey(id) && decorations[id] != null) continue;
                Vector3 position;
                if (id == "home-archive" || id == "home-gallery")
                {
                    var pc = FindFirstObjectByType<HomePcAnchor>();
                    if (pc == null) continue;
                    position = pc.transform.position + (id == "home-archive"
                        ? new Vector3(-1.1f, 0.3f, 0f) : new Vector3(1.1f, 0.35f, 0f));
                }
                else
                {
                    var serviceId = id == "town-fish-market" ? TownServiceCatalog.FishBuyerId :
                        id == "town-equipment" ? TownServiceCatalog.EquipmentShopId : TownServiceCatalog.VehicleVendorId;
                    bool found = false;
                    position = Vector3.zero;
                    foreach (var a in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
                        if (a.Definition.ServiceId == serviceId)
                        {
                            position = a.WorldPosition + new Vector3(0f, 2.1f, 0f);
                            found = true;
                            break;
                        }
                    if (!found) continue;
                }
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "P45Upgrade_" + id;
                marker.transform.position = position;
                marker.transform.localScale = id.StartsWith("home-")
                    ? new Vector3(0.55f, 0.9f, 0.25f) : new Vector3(0.75f, 0.22f, 0.4f);
                var collider = marker.GetComponent<Collider>();
                if (collider != null) Destroy(collider); // presentation must not interfere with navigation
                var textObject = new GameObject("Label");
                textObject.transform.SetParent(marker.transform, false);
                textObject.transform.localPosition = new Vector3(-0.45f, 0.8f, 0f);
                textObject.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
                var label = textObject.AddComponent<TextMesh>();
                label.text = id.Replace("town-", "").Replace("home-", "").ToUpperInvariant();
                label.fontSize = 18;
                label.characterSize = 0.18f;
                label.color = Color.white;
                decorations[id] = marker;
            }
        }

        private void ClearDecorations()
        {
            foreach (var marker in decorations.Values) if (marker != null) Destroy(marker);
            decorations.Clear();
        }

        private void OnGUI()
        {
            if (!Near(TownServiceCatalog.EquipmentShopId)) return;
            GUI.Label(new Rect(16, Screen.height - 45, 360, 22), "GUNLUK HEDEFLER / ROLLER [B]");
            if (!open) return;
            var state = LivingWorldNetworkBinding.Mirrored;
            var rect = new Rect(20, 60, 515, 510);
            GUI.Box(rect, "GUNLUK PANO - GUN " + state.DayNumber);
            if (!LivingWorldNetworkBinding.HasMirror)
            {
                GUI.Label(new Rect(30, 90, 450, 22), "Host hedef verisi bekleniyor...");
                return;
            }
            GUI.Label(new Rect(30, 90, 475, 25), $"BALIK SIPARISI: {state.OrderId}  {state.OrderProgress}/{OrderTarget(state.OrderId)}");
            GUI.Label(new Rect(30, 115, 475, 25), state.OrderPaid ? "ODUL ALINDI" : "Guvenle getirip balikciya sat");
            GUI.Label(new Rect(30, 148, 475, 25), $"SPONSOR: {state.SponsorId}   {(state.SponsorPaid ? "TAMAMLANDI" : "BEKLIYOR")}");
            GUI.Label(new Rect(30, 173, 475, 25), "Gecerli kaydi ev PC'sinden yayinla, ertesi sabah sonuclanir.");

            GUI.Label(new Rect(30, 213, 400, 22), "UCRETSIZ ROL SECIMI (tek aktif rol)");
            CrewRole[] roles = { CrewRole.CameraOperator, CrewRole.Hunter, CrewRole.Explorer, CrewRole.Carrier };
            string[] labels = { "Kameraci", "Avci", "Kasif", "Tasiyici" };
            for (int i = 0; i < roles.Length; i++)
                if (GUI.Button(new Rect(30 + i * 118, 242, 112, 28), labels[i]))
                    LivingWorldNetworkBinding.RequestRole(roles[i]);

            GUI.Label(new Rect(30, 289, 475, 22), "KALICI EV / KASABA IYILESTIRMELERI");
            for (var i = 0; i < UpgradeIds.Length; i++)
            {
                var id = UpgradeIds[i];
                var owned = state.PurchasedUpgradeIds != null && state.PurchasedUpgradeIds.Contains(id);
                var price = id == "home-archive" ? 280 : id == "home-gallery" ? 460 :
                    id == "town-fish-market" ? 240 : id == "town-equipment" ? 330 : 420;
                var label = owned ? UpgradeNames[i] + " [ALINDI]" : UpgradeNames[i] + " - " + price + " kredi";
                if (GUI.Button(new Rect(30, 318 + i * 30, 465, 27), label) && !owned)
                    LivingWorldNetworkBinding.RequestUpgrade(id);
            }
            GUI.Label(new Rect(30, 478, 475, 23), LivingWorldNetworkBinding.LastResult);
        }

        private static int OrderTarget(string id) =>
            id == "shore-one" ? 1 : id == "shore-two" ? 2 : id == "shore-three" ? 3 : 0;

        private void OnDisable() => ClearDecorations();
    }
}
