using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using UnityEngine;

namespace DeepDive.Composition
{
    // First physical shopping slice: samples on a shelf, unpaid host selection,
    // then an explicit transaction at the existing cashier. No extra stock/save.
    public sealed class ShopShelfPresentation : MonoBehaviour
    {
        public const string RootName = "EquipmentShelves";
        private ServicePointAnchor shop;
        private EconomyManager economy;
        private EconomyPlayerSync localSync;
        private NetworkPlayer localPlayer;
        private bool pending;
        private ulong receipt;
        public static readonly string[] Labels = {
            "TUP I", "TUP II", "TUP III", "TEMEL KAMERA", "GELISMIS KAMERA", "PRO KAMERA",
            "PALET", "CANTA", "ZIPKIN", "GOVDE TAHTASI", "OTURAK DESTEGI", "KUREK BAGLANTISI"
        };

        public static ShopShelfPresentation Build(ServicePointAnchor shop, Transform parent)
        {
            var existing = parent.GetComponentInChildren<ShopShelfPresentation>();
            if (existing != null) return existing;
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            var view = root.gameObject.AddComponent<ShopShelfPresentation>();
            view.shop = shop;
            view.economy = FindFirstObjectByType<EconomyManager>();
            var catalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            for (var side = 0; side < 2; side++)
            {
                var x = side == 0 ? -2.15f : 2.15f;
                Block(root, "RackBack", new Vector3(x, 1.22f, .88f), new Vector3(1.15f, 1.95f, .10f), DevelopmentVisual.Tint.DarkWood);
                for (var row = 0; row < 3; row++)
                {
                    var y = .62f + row * .53f;
                    Block(root, "Shelf", new Vector3(x, y, 1.1f), new Vector3(1.25f, .065f, .55f), DevelopmentVisual.Tint.ShelfWood);
                    for (var col = 0; col < 2; col++)
                    {
                        var i = side * 6 + row * 2 + col;
                        var sample = new GameObject("Product_" + ShopShelfAnchor.ItemIds[i]);
                        sample.transform.SetParent(root, false);
                        sample.transform.localPosition = new Vector3(x + (col == 0 ? -.29f : .29f), y + .20f, 1.12f);
                        var target = sample.AddComponent<ShopShelfAnchor>();
                        target.Configure(shop, ShopShelfAnchor.ItemIds[i]);
                        var hit = sample.AddComponent<BoxCollider>();
                        hit.size = new Vector3(.48f, .35f, .35f);
                        hit.isTrigger = true; // aim target, never a new obstruction in the walk/escape path
                        AddSample(sample.transform, target.ItemId, catalog);
                        var price = view.Price(target.ItemId);
                        Sign(root, sample.name + "_Label", new Vector3(sample.transform.localPosition.x, y - .065f, 1.405f),
                            Labels[i] + "\n" + (price >= 0 ? price + " KR" : "KASADA"), .49f);
                    }
                }
                Sign(root, "RackTitle", new Vector3(x, 2.28f, 1.0f), "F: URUN SEC", 1.2f);
            }
            Sign(root, "Cashier", new Vector3(0f, 1.10f, 1.0f), "KASA - F", 1.15f);
            return view;
        }

        private void OnEnable() => EconomyPlayerSync.DrawPhysicalShop = DrawCashier;
        private void OnDisable()
        {
            if (EconomyPlayerSync.DrawPhysicalShop == DrawCashier) EconomyPlayerSync.DrawPhysicalShop = null;
        }
        private int Price(string id)
        {
            if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
            if (economy == null) return -1;
            if (BoatRepairParts.IsPart(id)) return economy.BoatPartPrice;
            return economy.TryGetEquipmentDefinition(id, out var item) ? item.Price : -1;
        }
        public static string Label(string id)
        {
            var index = System.Array.IndexOf(ShopShelfAnchor.ItemIds, id);
            return index >= 0 ? Labels[index] : "";
        }
        private void Update()
        {
            if (localSync == null || !localSync.IsSpawned || !localSync.IsOwner)
            {
                localSync = null; localPlayer = null; pending = false;
                foreach (var sync in FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None))
                    if (sync.IsSpawned && sync.IsOwner) { localSync = sync; localPlayer = sync.GetComponent<NetworkPlayer>(); break; }
            }
            if (pending && localSync != null && localSync.LastRequestId.Value != receipt) pending = false;
        }
        private bool DrawCashier(EconomyPlayerSync sync)
        {
            if (shop == null || localPlayer == null || sync != localSync) return false;
            var width = Mathf.Min(410f, Screen.width - 30f);
            var x = Screen.width - width - 15f;
            GUI.Box(new Rect(x, 30, width, 188), "EKIPMAN KASASI");
            var id = localPlayer.SelectedShopItem.Value.ToString();
            GUI.Label(new Rect(x + 14, 62, width - 28, 40), id.Length == 0
                ? "Once raftaki urune bakip F ile sec.\nUrun secmek para harcamaz."
                : Label(id) + " | " + Price(id) + " KREDI\nHenuz satin alinmadi.");
            var enabled = GUI.enabled;
            GUI.enabled = enabled && id.Length > 0 && !pending;
            if (GUI.Button(new Rect(x + 14, 111, width - 28, 30), pending ? "ISLEM BEKLENIYOR" : "KASADA ODE"))
            {
                receipt = sync.LastRequestId.Value;
                pending = true;
                sync.RequestPurchase(id);
            }
            GUI.enabled = enabled && id.Length > 0 && !pending;
            if (GUI.Button(new Rect(x + 14, 151, width - 28, 26), "SECIMI IPTAL ET")) localPlayer.CancelShopSelectionLocal();
            GUI.enabled = enabled;
            return true;
        }
        private void OnGUI()
        {
            if (localPlayer == null || !localPlayer.IsSpawned || localSync == null || localSync.ShopOpen) return;
            var selected = localPlayer.SelectedShopItem.Value.ToString();
            var text = selected.Length > 0 ? Label(selected) + " secildi - kasaya git ve F'ye bas. Henuz odenmedi." : "";
            var camera = localPlayer.GetComponentInChildren<Camera>(true);
            if (selected.Length == 0 && camera != null && EyeRay.Raycast(camera.transform.position, camera.transform.forward, 3.25f,
                QueryTriggerInteraction.Collide, localPlayer.GetComponent<CharacterController>(), out var hit))
            {
                var sample = hit.collider.GetComponentInParent<ShopShelfAnchor>();
                if (sample != null && sample.Shop == shop) text = "F: " + Label(sample.ItemId) + " sec | " + Price(sample.ItemId) + " KR - odeme kasada";
            }
            if (text.Length > 0) GUI.Box(new Rect(Mathf.Max(10, (Screen.width - 620) / 2f), Screen.height - 105, Mathf.Min(620, Screen.width - 20), 32), text);
        }

        private static void AddSample(Transform parent, string id, DiverPresentationCatalog catalog)
        {
            GameObject prefab = null;
            if (catalog != null)
            {
                if (id == "camera-basic") prefab = catalog.CameraPropPrefab;
                if (id == "camera-advanced") prefab = catalog.CameraAdvancedPropPrefab;
                if (id == "camera-pro") prefab = catalog.CameraProfessionalPropPrefab;
                if (id == "harpoon-1") prefab = catalog.HarpoonPropPrefab;
                if (BoatRepairParts.IsPart(id)) prefab = catalog.RepairPlankPrefab;
            }
            if (id == "bag-1") prefab = Resources.Load<GameObject>(PlayerActionView.BagResource);
            if (prefab != null)
            {
                var model = Instantiate(prefab, parent, false);
                foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var scale = .36f / Mathf.Max(.001f, bounds.size.x, bounds.size.y, bounds.size.z);
                model.transform.localScale *= scale;
                model.transform.position += parent.position - (bounds.center - model.transform.position) * scale - model.transform.position;
                return;
            }
            // Limited display geometry for products without a standalone art asset.
            var tube = id.StartsWith("tube-");
            var modelPart = Block(parent, "DisplaySample", Vector3.zero, tube ? new Vector3(.12f, .16f, .12f) : new Vector3(.28f, .07f, .19f),
                tube ? DevelopmentVisual.Tint.Steel : DevelopmentVisual.Tint.Blue, tube ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            if (tube) Block(modelPart.transform, "Valve", new Vector3(0f, 1.1f, 0f), new Vector3(.65f, .2f, .65f), DevelopmentVisual.Tint.DarkWood);
        }
        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, DevelopmentVisual.Tint tint, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Collider>().enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>(DevelopmentVisual.MaterialPath(tint));
            return go;
        }
        private static void Sign(Transform parent, string name, Vector3 position, string text, float width)
        {
            var board = Block(parent, name, position, new Vector3(width, .18f, .025f), DevelopmentVisual.Tint.Cream);
            var go = new GameObject("Text"); go.transform.SetParent(parent, false);
            go.transform.localPosition = position + Vector3.forward * .02f;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 48; mesh.characterSize = .024f;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = Color.black;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = mesh.font.material;
            var factor = Mathf.Min(1f, width * .9f / Mathf.Max(.001f, renderer.bounds.size.x));
            go.transform.localScale = Vector3.one * factor;
        }
    }
}
