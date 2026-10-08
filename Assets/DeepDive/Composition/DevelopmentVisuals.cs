using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.5-C (#132): the visible side of the home/town development. Each improvement is a small primitive prop group that appears when the
    // campaign owns it (a bare plot marker shows until then). Like the P4.1 home rig these are authored at scene load, so no scene file
    // changes; they read only DevelopmentEffects (the host's state, or the host's mirror on a guest) and never carry any rule. Primitive placeholder
    // art until the real town/home art replaces it.
    [DisallowMultipleComponent]
    public sealed class DevelopmentVisual : MonoBehaviour
    {
        private const string HomeSceneName = "PrepArea";
        private const string TownSceneName = "DiveTestArea";
        private const string RootPrefix = "P45_Development_";
        private const float Interval = 0.25f;

        private string developmentId;
        private GameObject built, plot;
        private float next;
        private int shown = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == HomeSceneName) BuildHome();
            else if (scene.name == TownSceneName) BuildTown();
        }

        // ---- home level 2: a display shelf with trophies between the storage and the PC ---------------------------------

        private static void BuildHome()
        {
            if (GameObject.Find(RootPrefix + DevelopmentIds.Home2) != null) return;
            var root = Make(DevelopmentIds.Home2, new Vector3(0f, 0f, -3.55f));
            var built = Group(root, "Built");
            Prim(built, PrimitiveType.Cube, "ShelfBack", new Vector3(0f, 1.2f, 0.15f), new Vector3(2.6f, 1.6f, 0.12f), Tint.Wood);
            Prim(built, PrimitiveType.Cube, "ShelfLow", new Vector3(0f, 0.55f, 0f), new Vector3(2.6f, 0.08f, 0.55f), Tint.ShelfWood);
            Prim(built, PrimitiveType.Cube, "ShelfHigh", new Vector3(0f, 1.25f, 0f), new Vector3(2.6f, 0.08f, 0.55f), Tint.ShelfWood);
            Prim(built, PrimitiveType.Sphere, "TrophyA", new Vector3(-0.8f, 0.78f, 0f), new Vector3(0.3f, 0.3f, 0.3f), Tint.Gold);
            Prim(built, PrimitiveType.Cube, "TrophyB", new Vector3(0f, 0.72f, 0f), new Vector3(0.25f, 0.35f, 0.25f), Tint.Cyan);
            Prim(built, PrimitiveType.Cylinder, "TrophyC", new Vector3(0.8f, 0.74f, 0f), new Vector3(0.22f, 0.22f, 0.22f), Tint.Rose);
            Prim(built, PrimitiveType.Cube, "Rug", new Vector3(0f, 0.01f, 1.4f), new Vector3(2.8f, 0.02f, 1.6f), Tint.Rug);
            var plot = Group(root, "Plot");
            Prim(plot, PrimitiveType.Cube, "PlotMarker", new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 0.06f, 0.5f), Tint.Plot);
            root.AddComponent<DevelopmentVisual>().Init(DevelopmentIds.Home2, built, plot);
        }

        // ---- town: a stall behind the fisherman, extra stock behind the shop, lanterns on the pier --------------------------

        private static void BuildTown()
        {
            var anchors = FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None);
            foreach (var anchor in anchors)
            {
                var id = anchor.Definition.ServiceId;
                if (id == TownServiceCatalog.FishBuyerId && GameObject.Find(RootPrefix + DevelopmentIds.TownFisher) == null) BuildFisher(anchor.WorldPosition);
                else if (id == TownServiceCatalog.EquipmentShopId && GameObject.Find(RootPrefix + DevelopmentIds.TownShop) == null) BuildShop(anchor.WorldPosition);
            }
            var dock = GameObject.Find("Dock_Town");
            if (dock != null && GameObject.Find(RootPrefix + DevelopmentIds.TownDock) == null) BuildDock(dock);
        }

        private static void BuildFisher(Vector3 npc)
        {
            var root = Make(DevelopmentIds.TownFisher, npc + new Vector3(0f, 0f, -1.7f));
            var built = Group(root, "Built");
            Prim(built, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 0.8f), Tint.ShelfWood);
            Prim(built, PrimitiveType.Cube, "Awning", new Vector3(0f, 2.1f, 0.1f), new Vector3(2.6f, 0.08f, 1.5f), Tint.Orange);
            Prim(built, PrimitiveType.Cylinder, "PostL", new Vector3(-1.2f, 1.05f, 0.7f), new Vector3(0.1f, 1.05f, 0.1f), Tint.Wood);
            Prim(built, PrimitiveType.Cylinder, "PostR", new Vector3(1.2f, 1.05f, 0.7f), new Vector3(0.1f, 1.05f, 0.1f), Tint.Wood);
            Prim(built, PrimitiveType.Cube, "Crate", new Vector3(1.6f, 0.25f, 0.0f), new Vector3(0.5f, 0.5f, 0.5f), Tint.Crate);
            var plot = Group(root, "Plot");
            Prim(plot, PrimitiveType.Cylinder, "StakeL", new Vector3(-1.0f, 0.4f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Tint.Plot);
            Prim(plot, PrimitiveType.Cylinder, "StakeR", new Vector3(1.0f, 0.4f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Tint.Plot);
            root.AddComponent<DevelopmentVisual>().Init(DevelopmentIds.TownFisher, built, plot);
        }

        private static void BuildShop(Vector3 npc)
        {
            var root = Make(DevelopmentIds.TownShop, npc + new Vector3(0f, 0f, -1.5f));
            var built = Group(root, "Built");
            Prim(built, PrimitiveType.Cube, "Rack", new Vector3(0f, 0.95f, 0f), new Vector3(2.2f, 1.9f, 0.45f), Tint.Blue);
            Prim(built, PrimitiveType.Cube, "BoxA", new Vector3(-0.6f, 1.3f, 0.3f), new Vector3(0.4f, 0.4f, 0.3f), Tint.Butter);
            Prim(built, PrimitiveType.Cube, "BoxB", new Vector3(0.1f, 0.7f, 0.3f), new Vector3(0.5f, 0.4f, 0.3f), Tint.Rose);
            Prim(built, PrimitiveType.Cylinder, "TankA", new Vector3(0.7f, 1.4f, 0.3f), new Vector3(0.28f, 0.4f, 0.28f), Tint.Steel);
            Prim(built, PrimitiveType.Cube, "Banner", new Vector3(0f, 2.2f, 0f), new Vector3(1.6f, 0.45f, 0.06f), Tint.Banner);
            var plot = Group(root, "Plot");
            Prim(plot, PrimitiveType.Cube, "PlotMarker", new Vector3(0f, 0.05f, 0f), new Vector3(2.0f, 0.1f, 0.4f), Tint.Plot);
            root.AddComponent<DevelopmentVisual>().Init(DevelopmentIds.TownShop, built, plot);
        }

        private static void BuildDock(GameObject dock)
        {
            var collider = dock.GetComponentInChildren<Collider>();
            var position = collider != null ? new Vector3(collider.bounds.max.x + 0.45f, collider.bounds.max.y, collider.bounds.center.z) : dock.transform.position + new Vector3(1.5f, 0f, 0f);
            var root = Make(DevelopmentIds.TownDock, position);
            var built = Group(root, "Built");
            foreach (var z in new[] { -1.2f, 1.2f })
            {
                Prim(built, PrimitiveType.Cylinder, "Pole" + z, new Vector3(0f, 0.9f, z), new Vector3(0.12f, 0.9f, 0.12f), Tint.DarkWood);
                Prim(built, PrimitiveType.Sphere, "Lamp" + z, new Vector3(0f, 1.9f, z), new Vector3(0.38f, 0.38f, 0.38f), Tint.Lamp);
            }
            var plot = Group(root, "Plot");
            Prim(plot, PrimitiveType.Cylinder, "Stake", new Vector3(0f, 0.3f, 0f), new Vector3(0.08f, 0.3f, 0.08f), Tint.Plot);
            root.AddComponent<DevelopmentVisual>().Init(DevelopmentIds.TownDock, built, plot);
        }

        public enum Tint { Wood, ShelfWood, Gold, Cyan, Rose, Rug, Plot, Orange, DarkWood, Crate, Blue, Butter, Steel, Banner, Pole, Lamp }

        public static readonly Color[] Palette =
        {
            new Color(0.45f, 0.3f, 0.18f),
            new Color(0.55f, 0.38f, 0.22f),
            new Color(0.95f, 0.78f, 0.25f),
            new Color(0.35f, 0.75f, 0.85f),
            new Color(0.85f, 0.35f, 0.4f),
            new Color(0.2f, 0.35f, 0.55f),
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.95f, 0.55f, 0.15f),
            new Color(0.4f, 0.28f, 0.16f),
            new Color(0.7f, 0.55f, 0.3f),
            new Color(0.3f, 0.45f, 0.75f),
            new Color(0.95f, 0.85f, 0.35f),
            new Color(0.75f, 0.8f, 0.85f),
            new Color(0.15f, 0.35f, 0.85f),
            new Color(0.35f, 0.25f, 0.15f),
            new Color(1f, 0.92f, 0.45f)
        };

        public static string MaterialPath(Tint tint) => "DevVisuals/Dev_" + tint;

        // ---- helpers ------------------------------------------------------------------------------------------------------

        private static GameObject Make(string id, Vector3 position)
        {
            var root = new GameObject(RootPrefix + id);
            root.transform.position = position;
            return root;
        }

        private static GameObject Group(GameObject root, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root.transform, false);
            return g;
        }

        // Decoration only: no collider, so it can never block a walk, an aim ray or the boat.
        private static void Prim(GameObject parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Tint tint)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            // A built player strips the shader of CreatePrimitive's default material (the primitive turns magenta), so the props use real material
            // assets under Resources (generated by the editor menu, see DevelopmentMaterialSetup); a missing one falls back to a tinted property block.
            var renderer = go.GetComponent<Renderer>();
            var material = Resources.Load<Material>(MaterialPath(tint));
            if (material != null) renderer.sharedMaterial = material;
            else
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", Palette[(int)tint]);
                block.SetColor("_Color", Palette[(int)tint]);
                renderer.SetPropertyBlock(block);
            }
        }

        private void Init(string id, GameObject builtGroup, GameObject plotGroup)
        {
            developmentId = id;
            built = builtGroup;
            plot = plotGroup;
            Apply(DevelopmentEffects.State.Owns(id));
        }

        public string DevelopmentId => developmentId;

        public bool IsBuiltShown => built != null && built.activeSelf;

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            Apply(DevelopmentEffects.State.Owns(developmentId));
        }

        private void Apply(bool owned)
        {
            var value = owned ? 1 : 0;
            if (value == shown) return;
            shown = value;
            if (built != null) built.SetActive(owned);
            if (plot != null) plot.SetActive(!owned);
        }
    }
}

#if UNITY_EDITOR
namespace DeepDive.Composition
{
    // Generates the material assets the development props load from Resources (idempotent: an existing asset is updated in place, never duplicated).
    // Run: DeepDive/P4.5/Development prop materials, or -executeMethod DeepDive.Composition.DevelopmentMaterialSetup.Apply.
    public static class DevelopmentMaterialSetup
    {
        private const string Folder = "Assets/DeepDive/Town/Resources/DevVisuals";

        [UnityEditor.MenuItem("DeepDive/P4.5/Development prop materials")]
        public static void Apply()
        {
            System.IO.Directory.CreateDirectory(Folder);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (DevelopmentVisual.Tint tint in System.Enum.GetValues(typeof(DevelopmentVisual.Tint)))
            {
                var path = Folder + "/Dev_" + tint + ".mat";
                var color = DevelopmentVisual.Palette[(int)tint];
                var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    UnityEditor.AssetDatabase.CreateAsset(material, path);
                }
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                material.color = color;
                UnityEditor.EditorUtility.SetDirty(material);
            }
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("P45_DEV_MATERIALS_READY count=" + System.Enum.GetValues(typeof(DevelopmentVisual.Tint)).Length);
        }
    }
}
#endif
