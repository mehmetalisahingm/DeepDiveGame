using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.6-B (#139): the physical side of the four town services. Each ServicePointAnchor (the interaction anchor World and Mehmet's
    // interaction seam already use) gets a storefront built around it: a counter the customer stands in front of, back wall, roof, a sign with the
    // service's name, a floor mark at the customer spot, and a lantern that comes on at night. Like the P4.1 home rig and the P4.5 development props
    // it is authored when the scene loads, so no scene file changes and a storefront FOLLOWS its anchor wherever World places it (#138); an anchor that
    // already carries a child called "Storefront" (an authored building) is left alone. Every part is a child of the anchor, so an aim ray that hits the
    // counter resolves to the same service as one that hits the clerk. It carries no rule: prices, range checks and stock stay in the economy and the
    // host's TownServiceHandler. Primitive placeholder art until real town art replaces it.
    [DisallowMultipleComponent]
    public sealed class TownStorefront : MonoBehaviour
    {
        public const string RootName = "Storefront";
        public const string SceneName = "DiveTestArea";
        public const float CustomerSpot = 1.7f;   // metres in front of the clerk; the same spot TownSceneTests and the smoke walk to

        private const float DaylightPoll = 0.5f;
        private const float LampNightIntensity = 3.2f;

        private ServicePointType type;
        private string label;
        private Light lamp;
        private float nextPoll;
        private float night = -1f;
        private EconomyPlayerSync daySource;

        public ServicePointType ServiceType => type;
        public string Label => label;
        public Light Lamp => lamp;
        public Transform CustomerPoint { get; private set; }
        public Transform Root { get; private set; }

        public static string LabelFor(ServicePointType type) => type switch
        {
            ServicePointType.EquipmentShop => "EKIPMAN",
            ServicePointType.FishBuyer => "BALIK ALICI",
            ServicePointType.RecordingBuyer => "KAYIT ALICI",
            ServicePointType.VehicleVendor => "TEKNE SATICI",
            _ => string.Empty
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName) return;
            ContinuousTownLayout.Build(scene);
            foreach (var anchor in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
                if (anchor.gameObject.scene == scene) Build(anchor);
        }

        // Idempotent; returns the existing/new storefront, or null when the anchor's service has none (or an authored building already stands there).
        public static TownStorefront Build(ServicePointAnchor anchor)
        {
            if (anchor == null) return null;
            var existing = anchor.GetComponent<TownStorefront>();
            if (existing != null) return existing;
            var type = anchor.Definition.ServiceType;
            if (string.IsNullOrEmpty(LabelFor(type)) || anchor.transform.Find(RootName) != null) return null;

            var root = new GameObject(RootName).transform;
            root.SetParent(anchor.transform, false);
            var front = anchor.gameObject.AddComponent<TownStorefront>();
            front.type = type;
            front.label = LabelFor(type);
            front.Root = root;
            front.Construct(root);
            // The old authored beach fixture is only a narrow strip. Full buildings belong
            // on the continuous town plots, after the existing anchors have moved there.
            if (anchor.transform.position.z < -20f &&
                SuppliedTownArt.Attach(root, "ShopBuilding", new Vector3(0f, 0f, -2.8f), Quaternion.identity))
            {
                foreach (var name in new[] { "BackWall", "Roof", "PostL", "PostR" })
                {
                    var part = root.Find(name);
                    if (part != null && part.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
                }
            }
            // The clerk's old colour plate hung at 2.9 m, right where the roof and the sign are now.
            var oldSign = anchor.transform.Find("Sign");
            if (oldSign != null) foreach (var r in oldSign.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            return front;
        }

        private static DevelopmentVisual.Tint Accent(ServicePointType type) => type switch
        {
            ServicePointType.EquipmentShop => DevelopmentVisual.Tint.Blue,
            ServicePointType.FishBuyer => DevelopmentVisual.Tint.Green,
            ServicePointType.RecordingBuyer => DevelopmentVisual.Tint.Orange,
            _ => DevelopmentVisual.Tint.Purple
        };

        private void Construct(Transform root)
        {
            var accent = Accent(type);
            // shell
            Part(root, PrimitiveType.Cube, "BackWall", new Vector3(0f, 1.3f, -0.75f), new Vector3(2.2f, 2.6f, 0.1f), DevelopmentVisual.Tint.Wood);
            Part(root, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.02f, 0.45f), new Vector3(2.15f, 0.04f, 2.4f), DevelopmentVisual.Tint.Sand);
            var counter = Part(root, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.5f, 0.65f), new Vector3(1.8f, 1.0f, 0.5f), DevelopmentVisual.Tint.ShelfWood);
            counter.AddComponent<BoxCollider>();   // the one solid piece: it is what a customer aims at, and what the clerk stands behind
            Part(root, PrimitiveType.Cube, "CounterTop", new Vector3(0f, 1.03f, 0.65f), new Vector3(1.95f, 0.06f, 0.64f), DevelopmentVisual.Tint.DarkWood);
            foreach (var x in new[] { -1.05f, 1.05f })
                Part(root, PrimitiveType.Cylinder, "Post" + (x < 0 ? "L" : "R"), new Vector3(x, 1.3f, 1.0f), new Vector3(0.1f, 1.3f, 0.1f), DevelopmentVisual.Tint.DarkWood);
            var roof = Part(root, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.68f, 0.15f), new Vector3(2.3f, 0.1f, 2.0f), accent);
            roof.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

            // sign: a cream board on the roof's front edge with the service's name on it
            Part(root, PrimitiveType.Cube, "SignBoard", new Vector3(0f, 2.98f, 1.1f), new Vector3(2.0f, 0.55f, 0.08f), DevelopmentVisual.Tint.Cream);
            Part(root, PrimitiveType.Cube, "SignTrim", new Vector3(0f, 2.98f, 1.06f), new Vector3(2.1f, 0.65f, 0.05f), DevelopmentVisual.Tint.Charcoal);
            Text(root, "SignText", new Vector3(0f, 2.98f, 1.16f), label);

            // where a customer stands: an empty marker (the point itself) plus a visible floor disc
            var point = new GameObject("CustomerPoint").transform;
            point.SetParent(root, false);
            point.localPosition = new Vector3(0f, 0f, CustomerSpot);
            CustomerPoint = point;
            Part(root, PrimitiveType.Cylinder, "CustomerMark", new Vector3(0f, 0.03f, CustomerSpot), new Vector3(0.9f, 0.01f, 0.9f), accent);

            // lanterns: two glowing lamps on the posts, one real light that follows the shared day
            foreach (var x in new[] { -0.95f, 0.95f })
                Part(root, PrimitiveType.Sphere, "Lantern" + (x < 0 ? "L" : "R"), new Vector3(x, 2.3f, 1.12f), new Vector3(0.22f, 0.22f, 0.22f), DevelopmentVisual.Tint.Lamp);
            var lampObject = new GameObject("LanternLight");
            lampObject.transform.SetParent(root, false);
            lampObject.transform.localPosition = new Vector3(0f, 2.2f, 1.2f);
            lamp = lampObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.82f, 0.5f);
            lamp.range = 6f;
            lamp.intensity = 0f;
            lamp.shadows = LightShadows.None;

            Props(root);
            night = -1f;
            Poll(true);
        }

        private void Props(Transform root)
        {
            switch (type)
            {
                case ServicePointType.EquipmentShop:
                    Part(root, PrimitiveType.Cube, "Rack", new Vector3(0f, 1.45f, -0.62f), new Vector3(1.9f, 1.3f, 0.14f), DevelopmentVisual.Tint.Steel);
                    Part(root, PrimitiveType.Cylinder, "TankA", new Vector3(-0.6f, 1.32f, 0.62f), new Vector3(0.22f, 0.26f, 0.22f), DevelopmentVisual.Tint.Steel);
                    Part(root, PrimitiveType.Cylinder, "TankB", new Vector3(-0.2f, 1.32f, 0.62f), new Vector3(0.22f, 0.26f, 0.22f), DevelopmentVisual.Tint.Blue);
                    Part(root, PrimitiveType.Cube, "Camera", new Vector3(0.4f, 1.2f, 0.62f), new Vector3(0.34f, 0.2f, 0.24f), DevelopmentVisual.Tint.Charcoal);
                    Part(root, PrimitiveType.Cube, "Box", new Vector3(0.75f, 1.2f, 0.62f), new Vector3(0.26f, 0.2f, 0.26f), DevelopmentVisual.Tint.Butter);
                    break;
                case ServicePointType.FishBuyer:
                    Part(root, PrimitiveType.Cube, "CrateA", new Vector3(-0.55f, 1.2f, 0.62f), new Vector3(0.42f, 0.28f, 0.34f), DevelopmentVisual.Tint.Crate);
                    Part(root, PrimitiveType.Cube, "CrateB", new Vector3(0.0f, 1.2f, 0.62f), new Vector3(0.42f, 0.28f, 0.34f), DevelopmentVisual.Tint.Crate);
                    Part(root, PrimitiveType.Sphere, "FishA", new Vector3(-0.55f, 1.4f, 0.62f), new Vector3(0.36f, 0.14f, 0.12f), DevelopmentVisual.Tint.Cyan);
                    Part(root, PrimitiveType.Sphere, "FishB", new Vector3(0.0f, 1.4f, 0.62f), new Vector3(0.36f, 0.14f, 0.12f), DevelopmentVisual.Tint.Cyan);
                    Part(root, PrimitiveType.Cylinder, "Scale", new Vector3(0.6f, 1.2f, 0.62f), new Vector3(0.3f, 0.12f, 0.3f), DevelopmentVisual.Tint.Steel);
                    Part(root, PrimitiveType.Cube, "NetRail", new Vector3(0f, 1.9f, -0.6f), new Vector3(1.8f, 0.05f, 0.05f), DevelopmentVisual.Tint.DarkWood);
                    break;
                case ServicePointType.RecordingBuyer:
                    Part(root, PrimitiveType.Cube, "Monitor", new Vector3(-0.5f, 1.38f, 0.55f), new Vector3(0.55f, 0.36f, 0.05f), DevelopmentVisual.Tint.Charcoal);
                    Part(root, PrimitiveType.Cube, "Screen", new Vector3(-0.5f, 1.38f, 0.58f), new Vector3(0.46f, 0.28f, 0.02f), DevelopmentVisual.Tint.Cyan);
                    Part(root, PrimitiveType.Cube, "Deck", new Vector3(0.45f, 1.18f, 0.62f), new Vector3(0.5f, 0.14f, 0.32f), DevelopmentVisual.Tint.Charcoal);
                    Part(root, PrimitiveType.Cylinder, "Reel", new Vector3(0.45f, 1.27f, 0.62f), new Vector3(0.16f, 0.03f, 0.16f), DevelopmentVisual.Tint.Red);
                    Part(root, PrimitiveType.Cube, "Shelf", new Vector3(0f, 1.6f, -0.62f), new Vector3(1.8f, 0.06f, 0.2f), DevelopmentVisual.Tint.DarkWood);
                    break;
                default:
                    Part(root, PrimitiveType.Cube, "Hull", new Vector3(0f, 1.18f, 0.62f), new Vector3(0.9f, 0.12f, 0.3f), DevelopmentVisual.Tint.Blue);
                    Part(root, PrimitiveType.Cube, "Cabin", new Vector3(0.1f, 1.3f, 0.62f), new Vector3(0.3f, 0.14f, 0.2f), DevelopmentVisual.Tint.Cream);
                    Part(root, PrimitiveType.Sphere, "Buoy", new Vector3(-0.75f, 1.18f, 0.62f), new Vector3(0.22f, 0.22f, 0.22f), DevelopmentVisual.Tint.Red);
                    Part(root, PrimitiveType.Cylinder, "Oar", new Vector3(0.75f, 1.0f, 0.62f), new Vector3(0.04f, 0.5f, 0.04f), DevelopmentVisual.Tint.Wood);
                    Part(root, PrimitiveType.Cube, "Chart", new Vector3(0f, 1.7f, -0.66f), new Vector3(1.2f, 0.7f, 0.04f), DevelopmentVisual.Tint.Banner);
                    break;
            }
        }

        // ---- night lantern ------------------------------------------------------------------------------------------------

        private void Update()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + DaylightPoll;
            Poll(false);
        }

        private void Poll(bool force)
        {
            if (lamp == null) return;
            var factor = SharedNight();
            if (!force && Mathf.Abs(factor - night) < 0.01f) return;
            night = factor;
            lamp.intensity = LampIntensity(factor);
            lamp.enabled = factor > 0.02f;
        }

        public static float LampIntensity(float night01) => LampNightIntensity * Mathf.Clamp01(night01);

        // 0 = full day, 1 = full night, from the already replicated shared day (the same source CoastalAtmosphere reads); no clock of its own.
        private float SharedNight()
        {
            if (daySource == null || !daySource.IsSpawned)
            {
                daySource = null;
                foreach (var sync in FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None))
                {
                    if (!sync.IsSpawned) continue;
                    if (daySource == null) daySource = sync;
                    if (sync.IsOwner) { daySource = sync; break; }
                }
            }
            if (daySource == null) return 0f;
            return DaylightModel.Evaluate(daySource.DayClockMinute.Value, (DayPhase)daySource.DayPhaseValue.Value, daySource.DayWeatherSeed.Value).NightFactor;
        }

        // ---- helpers ------------------------------------------------------------------------------------------------------

        // Decoration has no collider (it must never block a walk, an aim ray or the boat); the counter adds its own.
        private static GameObject Part(Transform parent, PrimitiveType primitive, string name, Vector3 localPosition, Vector3 localScale, DevelopmentVisual.Tint tint)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) { if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); }
            var renderer = go.GetComponent<Renderer>();
            // CreatePrimitive's default material loses its shader in a built player (magenta): real material assets under Resources
            // (DevelopmentMaterialSetup), with a tinted property block as the fallback.
            var material = Resources.Load<Material>(DevelopmentVisual.MaterialPath(tint));
            if (material != null) renderer.sharedMaterial = material;
            else
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", DevelopmentVisual.Palette[(int)tint]);
                block.SetColor("_Color", DevelopmentVisual.Palette[(int)tint]);
                renderer.SetPropertyBlock(block);
            }
            return go;
        }

        private static void Text(Transform parent, string name, Vector3 localPosition, string value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // A TextMesh reads correctly from its -Z side: turn it round so it faces the customer (+Z of the clerk), not the back wall.
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            mesh.text = value;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = 0.045f;
            mesh.color = new Color(0.1f, 0.1f, 0.12f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (font != null) renderer.sharedMaterial = font.material;
            // fit the 1.9 m board whatever the label length is
            var width = renderer.bounds.size.x;
            var fit = Mathf.Min(1f, 1.8f / Mathf.Max(0.01f, width));
            go.transform.localScale = new Vector3(fit, fit, 1f);
        }
    }
}
