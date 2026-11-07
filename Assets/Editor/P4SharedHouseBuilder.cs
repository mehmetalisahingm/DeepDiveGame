using System.Collections.Generic;
using System.IO;
using DeepDive.Composition;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #259: bakes the shared house's dressing (Resources/CityKit/SharedHouse) from the Quaternius MegaKit modules, the Shore kit props and a few
    // procedural pieces. Frame: origin = ContinuousTownLayout.HomeOrigin (floor centre), the open front looks to +Z. The house shell (four wall
    // blocks, slab, roof slab) and the physical targets (beds, storage, PC, anchors) are the town's: this adds ONLY collider-free visuals over/inside
    // them, and the tests prove it (no collider, nothing in the walking lanes, nothing outside the plot).
    // DeepDive > P4.10 > Build shared house dressing, or -executeMethod DeepDive.EditorTools.P4SharedHouseBuilder.Build
    public static class P4SharedHouseBuilder
    {
        public const string PrefabPath = P410EquipmentStoreBuilder.Folder + "/SharedHouse.prefab";
        private const string ShoreFolder = "Assets/DeepDive/Art/Resources/ShoreKit/";
        private const string SuppliedFolder = "Assets/DeepDive/Art/Resources/SuppliedKit/";

        // The room, as the town builds it (HomeLeft/Right/Back are 0.35 m blocks centred on +-6 m).
        public const float Inner = 5.825f;      // inner face of the side and back walls
        public const float Outer = 6.175f;      // outer face
        public const float RoofUnder = 2.95f;   // underside of the flat roof slab
        public const float RoofTop = 3.25f;     // top of the flat roof slab
        public const float FrontZ = 6f;         // the open front line

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly List<Renderer> Panes = new List<Renderer>();
        private static readonly List<Renderer> Bulbs = new List<Renderer>();
        private static readonly List<Renderer> Screens = new List<Renderer>();
        private static readonly List<Light> Lamps = new List<Light>();

        [MenuItem("DeepDive/P4.10/Build shared house dressing")]
        public static void Build()
        {
            Cache.Clear(); Panes.Clear(); Bulbs.Clear(); Screens.Clear(); Lamps.Clear();
            Directory.CreateDirectory(P410EquipmentStoreBuilder.Folder + "/Materials");
            AssetDatabase.Refresh();
            var root = new GameObject("SharedHouse");
            try
            {
                Exterior(Group(root.transform, "Exterior"));
                Interior(Group(root.transform, "Interior"));
                Beds(Group(root.transform, "Beds"));
                PcCorner(Group(root.transform, "PcCorner"));
                var lighting = root.AddComponent<DressingLighting>();
                lighting.Panes = Panes.ToArray(); lighting.Bulbs = Bulbs.ToArray(); lighting.Screens = Screens.ToArray(); lighting.Lamps = Lamps.ToArray();
                lighting.PaneDay = Mat("PaneDay", new Color(0.62f, 0.8f, 0.95f), 0.6f, new Color(0.55f, 0.75f, 1.0f));
                lighting.PaneNight = Mat("PaneNight", new Color(0.05f, 0.08f, 0.16f), 0.7f, new Color(0.02f, 0.04f, 0.1f));
                lighting.BulbOn = Mat("BulbOn", new Color(1f, 0.92f, 0.7f), 0.5f, new Color(2.2f, 1.75f, 1.1f));
                lighting.BulbOff = Mat("BulbOff", new Color(0.9f, 0.86f, 0.72f), 0.5f);
                lighting.ScreenDay = Mat("ScreenDay", new Color(0.1f, 0.35f, 0.45f), 0.6f, new Color(0.12f, 0.5f, 0.65f));
                lighting.ScreenNight = Mat("ScreenNight", new Color(0.25f, 0.75f, 0.9f), 0.6f, new Color(0.5f, 1.6f, 2.0f));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("P259_HOUSE_BUILT " + PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        private static Transform Group(Transform parent, string name) => P410EquipmentStoreBuilder.Empty(parent, name, Vector3.zero);
        private static Material Kit(string name) => P410EquipmentStoreBuilder.Kit(name);

        internal static Material Mat(string name, Color color, float smoothness = 0.2f, Color? emission = null)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var path = P410EquipmentStoreBuilder.Folder + "/Materials/Home_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue) { material.EnableKeyword("_EMISSION"); material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; material.SetColor("_EmissionColor", emission.Value); }
            EditorUtility.SetDirty(material);
            Cache[name] = material;
            return material;
        }

        private static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null) =>
            P410EquipmentStoreBuilder.Prim(parent, type, name, position, scale, material, euler);

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, Vector3? euler = null) =>
            Prim(parent, PrimitiveType.Cube, name, position, size, material, euler);

        // A model that is measured and then fitted: its renderer bounds are scaled (on a wrapper, never on the FBX root) to `size`, and its bottom
        // centre is put at `bottomCenter`. Used where the module's native size is not known (roof, chimney, props).
        private static GameObject Fit(Transform parent, string model, Vector3 bottomCenter, Vector3 size, float yaw = 0f, bool longAlongZ = false)
        {
            var wrapper = new GameObject(model + "_Fit");
            wrapper.transform.SetParent(parent, false);
            var part = P410EquipmentStoreBuilder.Part(wrapper.transform, model, Vector3.zero, yaw);
            var bounds = Measure(wrapper.transform);
            if (longAlongZ && bounds.size.z < bounds.size.x)
            {
                part.transform.localRotation = Quaternion.Euler(0f, yaw + 90f, 0f) * AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Art/MegaKit/Models/" + model + ".fbx").transform.localRotation;
                bounds = Measure(wrapper.transform);
            }
            Debug.Log($"P259_MODEL {model} native size {bounds.size.x:0.00} x {bounds.size.y:0.00} x {bounds.size.z:0.00}");
            var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z > 0f ? size.z / bounds.size.z : size.x / bounds.size.x);
            wrapper.transform.localScale = scale;
            // bounds are in the (unscaled) parent frame: recentre so the bottom centre lands on the requested point
            wrapper.transform.localPosition = bottomCenter - new Vector3(bounds.center.x * scale.x, bounds.min.y * scale.y, bounds.center.z * scale.z);
            return wrapper;
        }

        private static Bounds Measure(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static GameObject Shore(Transform parent, string id, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ShoreFolder + id + ".prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return go;
        }

        // ---- exterior -------------------------------------------------------------------------------------------

        private static void Exterior(Transform ex)
        {
            // Plaster skin over the three solid walls (the wall blocks keep the physics): 2 m modules, inner face flush with the block's outer face.
            var skin = Group(ex, "WallSkin");
            for (var i = -5; i <= 5; i += 2)
            {
                P410EquipmentStoreBuilder.Part(skin, "Wall_Plaster_Straight", new Vector3(-Outer - 0.09f, 0f, i), 90f);
                P410EquipmentStoreBuilder.Part(skin, "Wall_Plaster_Straight", new Vector3(Outer + 0.09f, 0f, i), -90f);
                P410EquipmentStoreBuilder.Part(skin, "Wall_Plaster_Straight", new Vector3(i, 0f, -Outer - 0.09f), 0f);
            }
            var wood = Kit("MI_WoodTrim");
            // Corner posts, a stone plinth and a cornice
            var frame = Group(ex, "Frame");
            foreach (var x in new[] { -1f, 1f })
                foreach (var z in new[] { -1f, 1f })
                    Cube(frame, "Post", new Vector3(x * 6.3f, 1.6f, z * 6.3f), new Vector3(0.34f, 3.3f, 0.34f), wood);
            Cube(frame, "PlinthBack", new Vector3(0f, 0.18f, -6.55f), new Vector3(13.0f, 0.36f, 0.3f), Kit("MI_RockTrim"));
            Cube(frame, "PlinthLeft", new Vector3(-6.55f, 0.18f, 0f), new Vector3(0.3f, 0.36f, 13.0f), Kit("MI_RockTrim"));
            Cube(frame, "PlinthRight", new Vector3(6.55f, 0.18f, 0f), new Vector3(0.3f, 0.36f, 13.0f), Kit("MI_RockTrim"));
            Cube(frame, "CorniceBack", new Vector3(0f, 3.12f, -6.45f), new Vector3(13.0f, 0.22f, 0.3f), wood);
            Cube(frame, "CorniceLeft", new Vector3(-6.45f, 3.12f, 0f), new Vector3(0.3f, 0.22f, 13.0f), wood);
            Cube(frame, "CorniceRight", new Vector3(6.45f, 3.12f, 0f), new Vector3(0.3f, 0.22f, 13.0f), wood);

            // Outside windows: a frame, a pane that follows the day, shutters and a flower box (side walls, over the skin)
            foreach (var z in new[] { -1.6f, 1.6f })
            {
                OutsideWindow(frame, new Vector3(-Outer - 0.5f, 1.75f, z), -90f);
                OutsideWindow(frame, new Vector3(Outer + 0.5f, 1.75f, z), 90f);
            }
            foreach (var x in new[] { -2.9f, 2.9f }) OutsideWindow(frame, new Vector3(x, 1.75f, -Outer - 0.5f), 180f);

            // Pitched tile roof on the flat slab, gables front and back
            var roof = Group(ex, "Roof");
            var eave = Fit(roof, "Roof_RoundTiles_8x12", new Vector3(0f, RoofTop - 0.02f, 0f), new Vector3(13.4f, 3.4f, 13.6f), 0f, true);
            var apex = RoofTop - 0.02f + 3.4f;
            Fit(roof, "Roof_Front_Brick8", new Vector3(0f, RoofTop - 0.02f, -6.55f), new Vector3(13.0f, 3.4f, 0.6f), 0f);
            Fit(roof, "Roof_Front_Brick8", new Vector3(0f, RoofTop - 0.02f, 6.55f), new Vector3(13.0f, 3.4f, 0.6f), 180f);
            // Chimney, sunk into the slope so it never floats
            var slopeAt = RoofTop + (apex - RoofTop) * (1f - 3.2f / 6.7f);
            Fit(roof, "Prop_Chimney", new Vector3(3.2f, slopeAt - 0.35f, -2.8f), new Vector3(1.0f, 2.0f, 1.0f));

            // Porch: fascia, posts, a sign and two lanterns on the open front
            var porch = Group(ex, "Porch");
            Cube(porch, "Fascia", new Vector3(0f, 2.98f, 6.02f), new Vector3(12.7f, 0.3f, 0.22f), wood);
            foreach (var x in new[] { -6.3f, 6.3f }) Cube(porch, "PorchPost", new Vector3(x, 1.6f, 6.3f), new Vector3(0.3f, 3.2f, 0.3f), wood);
            var sign = Cube(porch, "SignBoard", new Vector3(0f, 2.55f, 6.1f), new Vector3(2.6f, 0.55f, 0.08f), Mat("SignBoard", new Color(0.2f, 0.34f, 0.46f), 0.3f));
            foreach (var x in new[] { -1.1f, 1.1f }) Cube(porch, "SignRope", new Vector3(x, 2.88f, 6.1f), new Vector3(0.03f, 0.3f, 0.03f), Kit("MI_MetalOrnaments"));
            var text = new GameObject("SignText");
            text.transform.SetParent(porch, false);
            text.transform.localPosition = new Vector3(0f, 2.55f, 6.16f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // a TextMesh reads along +Z; this one is read from the street (looking -Z)
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = "ORTAK EV";
            text.AddComponent<DepthTestedText>();
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.046f; mesh.fontSize = 100; mesh.color = new Color(0.98f, 0.94f, 0.82f);
            foreach (var x in new[] { -4f, 4f }) Lantern(porch, new Vector3(x, 2.5f, 6.15f), 1.2f);

            // Outside the right wall: a bench and gear, the shore props that make it a fisher's house
            var props = Group(ex, "Props");
            Shore(props, "shore-prop-barrel", new Vector3(-6.95f, 0f, -3.2f), 20f);
            Shore(props, "shore-prop-bucket-fishes", new Vector3(-6.9f, 0f, -2.2f), 70f);
            Shore(props, "shore-prop-chest", new Vector3(6.95f, 0f, -3.4f), -10f);
            Shore(props, "shore-prop-anchor", new Vector3(6.9f, 0f, 3.4f), 120f, 0.8f);
        }

        private static void OutsideWindow(Transform parent, Vector3 position, float yaw)
        {
            var w = Group(parent, "OutsideWindow");
            w.localPosition = position;
            w.localRotation = Quaternion.Euler(0f, yaw, 0f);   // local +Z looks away from the house
            var wood = Kit("MI_WoodTrim");
            Cube(w, "FrameTop", new Vector3(0f, 0.65f, 0f), new Vector3(1.6f, 0.1f, 0.12f), wood);
            Cube(w, "FrameBottom", new Vector3(0f, -0.62f, 0f), new Vector3(1.6f, 0.1f, 0.12f), wood);
            foreach (var x in new[] { -0.75f, 0.75f }) Cube(w, "FrameSide", new Vector3(x, 0f, 0f), new Vector3(0.1f, 1.3f, 0.12f), wood);
            var pane = Cube(w, "Pane", new Vector3(0f, 0f, -0.03f), new Vector3(1.42f, 1.15f, 0.03f), Mat("PaneDay", new Color(0.62f, 0.8f, 0.95f), 0.6f, new Color(0.55f, 0.75f, 1.0f)));
            Panes.Add(pane.GetComponent<Renderer>());
            Cube(w, "MullionV", new Vector3(0f, 0f, 0.01f), new Vector3(0.05f, 1.2f, 0.05f), wood);
            Cube(w, "MullionH", new Vector3(0f, 0f, 0.01f), new Vector3(1.45f, 0.05f, 0.05f), wood);
            foreach (var x in new[] { -1f, 1f }) Cube(w, "Shutter", new Vector3(x * 1.0f, 0f, 0.02f), new Vector3(0.42f, 1.32f, 0.06f), Mat("Shutter", new Color(0.18f, 0.42f, 0.5f), 0.2f));
            Cube(w, "Sill", new Vector3(0f, -0.72f, 0.1f), new Vector3(1.9f, 0.07f, 0.26f), wood);
            Cube(w, "FlowerBox", new Vector3(0f, -0.86f, 0.16f), new Vector3(1.5f, 0.22f, 0.26f), wood);
            var green = Mat("Leaf", new Color(0.22f, 0.5f, 0.24f), 0.15f);
            var colours = new[] { new Color(0.92f, 0.3f, 0.35f), new Color(0.98f, 0.82f, 0.3f), new Color(0.96f, 0.96f, 0.92f) };
            for (var i = 0; i < 7; i++)
            {
                var x = -0.62f + i * 0.207f;
                Prim(w, PrimitiveType.Sphere, "Leaf", new Vector3(x, -0.7f, 0.16f), new Vector3(0.2f, 0.2f, 0.2f), green);
                Prim(w, PrimitiveType.Sphere, "Bloom", new Vector3(x, -0.62f, 0.2f), new Vector3(0.09f, 0.09f, 0.09f), Mat("Bloom" + (i % 3), colours[i % 3], 0.3f));
            }
        }

        private static void Lantern(Transform parent, Vector3 position, float intensity)
        {
            var lantern = Group(parent, "Lantern");
            lantern.localPosition = position;
            Cube(lantern, "Cap", new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.06f, 0.3f), Kit("MI_MetalOrnaments"));
            Cube(lantern, "Base", new Vector3(0f, -0.2f, 0f), new Vector3(0.26f, 0.05f, 0.26f), Kit("MI_MetalOrnaments"));
            var bulb = Prim(lantern, PrimitiveType.Sphere, "Bulb", Vector3.zero, new Vector3(0.3f, 0.34f, 0.3f), Mat("BulbOff", new Color(0.9f, 0.86f, 0.72f), 0.5f));
            Bulbs.Add(bulb.GetComponent<Renderer>());
            var light = new GameObject("LanternLight").AddComponent<Light>();
            light.transform.SetParent(lantern, false);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.45f);
            light.range = 7f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            Lamps.Add(light);
        }

        // ---- interior -----------------------------------------------------------------------------------------------

        private static void Interior(Transform inside)
        {
            var wood = Kit("MI_WoodTrim");
            var plaster = Kit("MI_Plaster");

            // Wood floor: the slab's top is the walking surface, the planks lie 2 cm above it
            var floor = Group(inside, "Floor");
            for (var x = -5; x <= 5; x += 2)
                for (var z = -5; z <= 5; z += 2) P410EquipmentStoreBuilder.Part(floor, "Floor_WoodDark", new Vector3(x, 0.02f, z));

            // Wainscot, plaster band, skirting along the three walls
            var walls = Group(inside, "WallFinish");
            var oak = Mat("Wainscot", new Color(0.46f, 0.31f, 0.19f), 0.25f);
            var cream = Mat("Plaster", new Color(0.93f, 0.88f, 0.76f), 0.05f);
            const float run = 11.6f;
            foreach (var side in new[] { -1f, 1f })
            {
                Cube(walls, "Wainscot", new Vector3(side * (Inner - 0.03f), 0.55f, 0f), new Vector3(0.06f, 1.1f, run), oak);
                Cube(walls, "Rail", new Vector3(side * (Inner - 0.06f), 1.12f, 0f), new Vector3(0.1f, 0.06f, run), wood);
                Cube(walls, "Skirting", new Vector3(side * (Inner - 0.05f), 0.07f, 0f), new Vector3(0.1f, 0.14f, run), wood);
                Cube(walls, "Band", new Vector3(side * (Inner - 0.015f), 2.03f, 0f), new Vector3(0.03f, 1.84f, run), cream);
            }
            Cube(walls, "Wainscot", new Vector3(0f, 0.55f, -Inner + 0.03f), new Vector3(run, 1.1f, 0.06f), oak);
            Cube(walls, "Rail", new Vector3(0f, 1.12f, -Inner + 0.06f), new Vector3(run, 0.06f, 0.1f), wood);
            Cube(walls, "Skirting", new Vector3(0f, 0.07f, -Inner + 0.05f), new Vector3(run, 0.14f, 0.1f), wood);
            Cube(walls, "Band", new Vector3(0f, 2.03f, -Inner + 0.015f), new Vector3(run, 1.84f, 0.03f), cream);

            // Ceiling boards and beams
            var ceiling = Group(inside, "Ceiling");
            Cube(ceiling, "Boards", new Vector3(0f, RoofUnder - 0.01f, -0.1f), new Vector3(11.6f, 0.02f, 11.7f), Mat("Ceiling", new Color(0.78f, 0.62f, 0.42f), 0.15f));
            foreach (var z in new[] { -4.6f, -2.3f, 0f, 2.3f, 4.6f }) Cube(ceiling, "Beam", new Vector3(0f, RoofUnder - 0.1f, z), new Vector3(11.6f, 0.18f, 0.22f), oak);

            // Windows: three per side wall pair and the back wall, with curtains
            foreach (var z in new[] { -1.6f, 1.6f })
            {
                InsideWindow(inside, new Vector3(-Inner, 1.75f, z), 90f);
                InsideWindow(inside, new Vector3(Inner, 1.75f, z), -90f);
            }
            foreach (var x in new[] { -2.9f, 2.9f }) InsideWindow(inside, new Vector3(x, 1.75f, -Inner), 0f);

            // Rug in the middle and a small one in front of the couch (flat, walkable)
            var rugs = Group(inside, "Rugs");
            Cube(rugs, "RugBorder", new Vector3(0f, 0.035f, -1.1f), new Vector3(4.6f, 0.012f, 3.4f), Mat("RugBorder", new Color(0.88f, 0.74f, 0.45f), 0.05f));
            Cube(rugs, "RugField", new Vector3(0f, 0.042f, -1.1f), new Vector3(4.2f, 0.012f, 3.0f), Mat("RugField", new Color(0.55f, 0.2f, 0.2f), 0.05f));
            Cube(rugs, "RugStripe", new Vector3(0f, 0.049f, -1.1f), new Vector3(3.2f, 0.012f, 0.35f), Mat("RugBorder", new Color(0.88f, 0.74f, 0.45f), 0.05f));

            // Pendant lamps
            var lamps = Group(inside, "Lamps");
            foreach (var p in new[] { new Vector2(-2.6f, -1.2f), new Vector2(2.6f, -1.2f), new Vector2(0f, 3.45f) }) Pendant(lamps, new Vector3(p.x, RoofUnder - 0.18f, p.y));

            // Walls: framed chart, pennants and gear, lifebuoy, channel board, trophy-shelf brackets stay free for the development visual
            var decor = Group(inside, "WallDecor");
            Frame(decor, new Vector3(-Inner + 0.05f, 1.85f, 0f), 90f, 0.7f, 0.55f, Mat("ChartBlue", new Color(0.22f, 0.5f, 0.66f), 0.2f));
            Frame(decor, new Vector3(Inner - 0.05f, 1.85f, -4.6f), -90f, 0.8f, 0.6f, Mat("ChartFish", new Color(0.9f, 0.55f, 0.25f), 0.2f));
            GearWall(decor, new Vector3(-Inner + 0.05f, 0f, -3.8f), 90f);
            Lifebuoy(decor, new Vector3(Inner - 0.07f, 1.85f, 0f), -90f);
            Pennants(decor, new Vector3(-5.7f, 2.72f, 0f), new Vector3(5.7f, 2.72f, 0f));

            // Corner pieces on the floor behind the beds' heads (kept out of the walking lanes)
            var corners = Group(inside, "Corners");
            Shore(corners, "shore-prop-barrel", new Vector3(-5.4f, 0f, 5.45f), 35f, 0.75f);
            Shore(corners, "shore-prop-chest", new Vector3(5.45f, 0f, 5.45f), -90f, 0.8f);
            Shore(corners, "shore-prop-bucket", new Vector3(-5.45f, 0f, -2.3f), 0f, 0.8f);
            // Storage top: the day's catch
            Shore(corners, "shore-prop-bucket-fishes", new Vector3(-3.9f, 1.6f, -3.55f), 25f, 0.6f);
        }

        private static void InsideWindow(Transform parent, Vector3 position, float yaw)
        {
            var w = Group(parent, "InsideWindow");
            w.localPosition = position;
            w.localRotation = Quaternion.Euler(0f, yaw, 0f);   // local +Z looks into the room
            var wood = Kit("MI_WoodTrim");
            Cube(w, "FrameTop", new Vector3(0f, 0.65f, 0.06f), new Vector3(1.6f, 0.1f, 0.1f), wood);
            Cube(w, "FrameBottom", new Vector3(0f, -0.62f, 0.06f), new Vector3(1.6f, 0.1f, 0.1f), wood);
            foreach (var x in new[] { -0.75f, 0.75f }) Cube(w, "FrameSide", new Vector3(x, 0f, 0.06f), new Vector3(0.1f, 1.3f, 0.1f), wood);
            var pane = Cube(w, "Pane", new Vector3(0f, 0f, 0.04f), new Vector3(1.42f, 1.15f, 0.02f), Mat("PaneDay", new Color(0.62f, 0.8f, 0.95f), 0.6f, new Color(0.55f, 0.75f, 1.0f)));
            Panes.Add(pane.GetComponent<Renderer>());
            Cube(w, "MullionV", new Vector3(0f, 0f, 0.075f), new Vector3(0.04f, 1.2f, 0.04f), wood);
            Cube(w, "MullionH", new Vector3(0f, 0f, 0.075f), new Vector3(1.45f, 0.04f, 0.04f), wood);
            Cube(w, "Sill", new Vector3(0f, -0.7f, 0.14f), new Vector3(1.8f, 0.05f, 0.2f), wood);
            // Curtains with a rod
            var cloth = Mat("Curtain", new Color(0.72f, 0.3f, 0.27f), 0.05f);
            Cube(w, "Rod", new Vector3(0f, 0.78f, 0.13f), new Vector3(2.0f, 0.03f, 0.03f), Kit("MI_MetalOrnaments"));
            foreach (var x in new[] { -0.86f, 0.86f })
            {
                Cube(w, "Curtain", new Vector3(x, 0.05f, 0.14f), new Vector3(0.42f, 1.55f, 0.07f), cloth);
                Cube(w, "Fold", new Vector3(x * 0.92f, 0.05f, 0.18f), new Vector3(0.05f, 1.5f, 0.05f), Mat("CurtainDark", new Color(0.55f, 0.2f, 0.2f), 0.05f));
            }
        }

        private static void Pendant(Transform parent, Vector3 top)
        {
            var lamp = Group(parent, "Pendant");
            lamp.localPosition = top;
            Prim(lamp, PrimitiveType.Cylinder, "Cord", new Vector3(0f, -0.1f, 0f), new Vector3(0.02f, 0.1f, 0.02f), Kit("MI_MetalOrnaments"));
            Prim(lamp, PrimitiveType.Cylinder, "Shade", new Vector3(0f, -0.26f, 0f), new Vector3(0.5f, 0.07f, 0.5f), Mat("Shade", new Color(0.2f, 0.4f, 0.45f), 0.3f));
            var bulb = Prim(lamp, PrimitiveType.Sphere, "Bulb", new Vector3(0f, -0.36f, 0f), new Vector3(0.22f, 0.22f, 0.22f), Mat("BulbOff", new Color(0.9f, 0.86f, 0.72f), 0.5f));
            Bulbs.Add(bulb.GetComponent<Renderer>());
            var light = new GameObject("PendantLight").AddComponent<Light>();
            light.transform.SetParent(lamp, false);
            light.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.55f);
            light.range = 7.5f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            Lamps.Add(light);
        }

        private static void Frame(Transform parent, Vector3 position, float yaw, float width, float height, Material picture)
        {
            var frame = Group(parent, "Frame");
            frame.localPosition = position;
            frame.localRotation = Quaternion.Euler(0f, yaw, 0f);   // local +Z looks into the room
            var wood = Kit("MI_WoodTrim");
            Cube(frame, "Border", new Vector3(0f, 0f, 0f), new Vector3(width + 0.12f, height + 0.12f, 0.04f), wood);
            Cube(frame, "Picture", new Vector3(0f, 0f, 0.025f), new Vector3(width, height, 0.02f), picture);
            Cube(frame, "Sun", new Vector3(width * 0.25f, height * 0.2f, 0.04f), new Vector3(0.14f, 0.14f, 0.01f), Mat("Sun", new Color(0.98f, 0.86f, 0.4f), 0.1f));
            Cube(frame, "Wave", new Vector3(0f, -height * 0.28f, 0.04f), new Vector3(width * 0.9f, height * 0.18f, 0.01f), Mat("Wave", new Color(0.12f, 0.3f, 0.5f), 0.2f));
        }

        // Diving gear on pegs: mask, snorkel, fins, a tank
        private static void GearWall(Transform parent, Vector3 position, float yaw)
        {
            var gear = Group(parent, "GearWall");
            gear.localPosition = position;
            gear.localRotation = Quaternion.Euler(0f, yaw, 0f);   // local +Z looks into the room
            var wood = Kit("MI_WoodTrim");
            Cube(gear, "Board", new Vector3(0f, 1.7f, 0.03f), new Vector3(2.0f, 0.9f, 0.05f), Mat("Wainscot", new Color(0.46f, 0.31f, 0.19f), 0.25f));
            var rubber = Mat("Rubber", new Color(0.1f, 0.12f, 0.16f), 0.3f);
            var fin = Mat("Fin", new Color(0.95f, 0.65f, 0.15f), 0.3f);
            foreach (var x in new[] { -0.55f, -0.3f })
                Cube(gear, "Fin", new Vector3(x, 1.7f, 0.1f), new Vector3(0.18f, 0.7f, 0.05f), fin, new Vector3(0f, 0f, x < -0.4f ? 8f : -8f));
            Prim(gear, PrimitiveType.Cylinder, "Tank", new Vector3(0.45f, 1.45f, 0.14f), new Vector3(0.22f, 0.5f, 0.22f), Mat("Tank", new Color(0.7f, 0.76f, 0.8f), 0.6f));
            Prim(gear, PrimitiveType.Cylinder, "TankValve", new Vector3(0.45f, 1.98f, 0.14f), new Vector3(0.07f, 0.06f, 0.07f), Kit("MI_MetalOrnaments"));
            Cube(gear, "MaskFrame", new Vector3(0.05f, 1.85f, 0.1f), new Vector3(0.26f, 0.14f, 0.05f), rubber);
            Cube(gear, "MaskGlass", new Vector3(0.05f, 1.85f, 0.125f), new Vector3(0.2f, 0.09f, 0.01f), Mat("PaneDay", new Color(0.62f, 0.8f, 0.95f), 0.6f, new Color(0.55f, 0.75f, 1.0f)));
            Prim(gear, PrimitiveType.Cylinder, "Snorkel", new Vector3(0.05f, 1.62f, 0.1f), new Vector3(0.035f, 0.17f, 0.035f), rubber);
            foreach (var x in new[] { -0.9f, 0.9f }) Cube(gear, "Peg", new Vector3(x, 1.3f, 0.07f), new Vector3(0.04f, 0.04f, 0.1f), wood);
        }

        private static void Lifebuoy(Transform parent, Vector3 position, float yaw)
        {
            var buoy = Group(parent, "Lifebuoy");
            buoy.localPosition = position;
            buoy.localRotation = Quaternion.Euler(0f, yaw, 0f);   // local +Z looks into the room
            var white = Mat("BuoyWhite", new Color(0.96f, 0.96f, 0.94f), 0.25f);
            var red = Mat("BuoyRed", new Color(0.85f, 0.18f, 0.16f), 0.25f);
            Prim(buoy, PrimitiveType.Cylinder, "Ring", Vector3.zero, new Vector3(0.62f, 0.035f, 0.62f), white, new Vector3(90f, 0f, 0f));
            for (var i = 0; i < 4; i++)
            {
                var a = i * 90f * Mathf.Deg2Rad;
                Prim(buoy, PrimitiveType.Cube, "Band", new Vector3(Mathf.Cos(a) * 0.24f, Mathf.Sin(a) * 0.24f, 0.01f), new Vector3(0.14f, 0.14f, 0.06f), red, new Vector3(0f, 0f, i * 90f));
            }
            Prim(buoy, PrimitiveType.Cylinder, "Hub", new Vector3(0f, 0f, 0.012f), new Vector3(0.28f, 0.03f, 0.28f), Kit("MI_WoodTrim"), new Vector3(90f, 0f, 0f));
        }

        private static void Pennants(Transform parent, Vector3 from, Vector3 to)
        {
            var line = Group(parent, "Pennants");
            var colours = new[] { new Color(0.9f, 0.3f, 0.3f), new Color(0.98f, 0.82f, 0.3f), new Color(0.3f, 0.65f, 0.85f), new Color(0.4f, 0.75f, 0.45f), new Color(0.95f, 0.55f, 0.2f) };
            const int count = 11;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)(count - 1);
                var sag = Mathf.Sin(t * Mathf.PI) * -0.35f;
                var position = Vector3.Lerp(from, to, t) + new Vector3(0f, sag, 0f);
                Cube(line, "Pennant", position + new Vector3(0f, -0.1f, 0f), new Vector3(0.22f, 0.26f, 0.015f), Mat("Pennant" + (i % colours.Length), colours[i % colours.Length], 0.05f), new Vector3(0f, 0f, 45f));
            }
        }

        // ---- beds, PC corner -----------------------------------------------------------------------------------------

        private static readonly Color[] BedColours =
        {
            new Color(0.2f, 0.45f, 0.82f), new Color(0.24f, 0.66f, 0.4f), new Color(0.95f, 0.56f, 0.2f), new Color(0.8f, 0.25f, 0.3f)
        };
        public static readonly string[] BedColourNames = { "blue", "green", "orange", "red" };

        private static void Beds(Transform beds)
        {
            var pillow = Mat("Pillow", new Color(0.96f, 0.95f, 0.9f), 0.05f);
            for (var i = 0; i < ContinuousTownLayout.HomeRig.BedXs.Length; i++)
            {
                var bed = Group(beds, "BedDress_" + BedColourNames[i]);
                bed.localPosition = new Vector3(ContinuousTownLayout.HomeRig.BedXs[i], 0f, ContinuousTownLayout.HomeRig.BedLocalZ);
                var cloth = Mat("Blanket_" + BedColourNames[i], BedColours[i], 0.05f);
                // The bed is 1.25 x 2.05 m, top at 0.475 m; the head is the front (+Z) end, the foot faces the room
                Cube(bed, "Blanket", new Vector3(0f, 0.53f, -0.2f), new Vector3(1.12f, 0.09f, 1.3f), cloth);
                Cube(bed, "BlanketFold", new Vector3(0f, 0.54f, 0.47f), new Vector3(1.14f, 0.1f, 0.16f), Mat("Fold_" + BedColourNames[i], BedColours[i] * 0.8f, 0.05f));
                Cube(bed, "Pillow", new Vector3(0f, 0.57f, 0.78f), new Vector3(0.74f, 0.13f, 0.34f), pillow);
                // A small number plate on the foot end so four players tell the beds apart
                Cube(bed, "Plate", new Vector3(0f, 0.36f, -1.04f), new Vector3(0.3f, 0.2f, 0.03f), cloth);
            }
        }

        private static void PcCorner(Transform pc)
        {
            var dark = Mat("PcDark", new Color(0.09f, 0.1f, 0.12f), 0.5f);
            var local = ContinuousTownLayout.HomeRig.PcLocal;      // centre of the PC block, top at y = 1.2
            var top = local.y + ContinuousTownLayout.HomeRig.PcSize.y * 0.5f;
            // The PC block is a plain wooden box: a desk top slab with a lip makes it read as a desk
            Cube(pc, "DeskTop", new Vector3(local.x, top + 0.02f, local.z), new Vector3(1.56f, 0.05f, 0.84f), Mat("DeskTop", new Color(0.52f, 0.36f, 0.22f), 0.3f));
            top += 0.045f;
            Cube(pc, "MonitorBase", new Vector3(local.x, top + 0.01f, local.z - 0.06f), new Vector3(0.32f, 0.02f, 0.2f), dark);
            Cube(pc, "MonitorNeck", new Vector3(local.x, top + 0.14f, local.z - 0.06f), new Vector3(0.07f, 0.26f, 0.05f), dark);
            Cube(pc, "MonitorFrame", new Vector3(local.x, top + 0.5f, local.z - 0.06f), new Vector3(0.84f, 0.52f, 0.05f), dark);
            var screen = Cube(pc, "MonitorScreen", new Vector3(local.x, top + 0.5f, local.z - 0.03f), new Vector3(0.76f, 0.44f, 0.01f), Mat("ScreenDay", new Color(0.1f, 0.35f, 0.45f), 0.6f, new Color(0.12f, 0.5f, 0.65f)));
            Screens.Add(screen.GetComponent<Renderer>());
            Cube(pc, "Keyboard", new Vector3(local.x - 0.05f, top + 0.015f, local.z + 0.12f), new Vector3(0.5f, 0.025f, 0.16f), Mat("Keys", new Color(0.8f, 0.82f, 0.85f), 0.3f));
            Cube(pc, "Mouse", new Vector3(local.x + 0.38f, top + 0.016f, local.z + 0.12f), new Vector3(0.07f, 0.03f, 0.11f), dark);
            Prim(pc, PrimitiveType.Cylinder, "Mug", new Vector3(local.x - 0.55f, top + 0.05f, local.z + 0.08f), new Vector3(0.09f, 0.05f, 0.09f), Mat("Mug", new Color(0.95f, 0.95f, 0.9f), 0.5f));
            Cube(pc, "Router", new Vector3(local.x - 0.5f, top + 0.05f, local.z - 0.12f), new Vector3(0.24f, 0.09f, 0.14f), dark);
            Cube(pc, "BookA", new Vector3(local.x + 0.55f, top + 0.04f, local.z - 0.15f), new Vector3(0.24f, 0.06f, 0.18f), Mat("BookRed", new Color(0.7f, 0.2f, 0.2f), 0.2f));
            Cube(pc, "BookB", new Vector3(local.x + 0.55f, top + 0.095f, local.z - 0.15f), new Vector3(0.22f, 0.05f, 0.16f), Mat("BookBlue", new Color(0.2f, 0.35f, 0.65f), 0.2f));
            // The channel board on the wall behind the corner, lit like the screen
            var board = Group(pc, "ChannelBoard");
            board.localPosition = new Vector3(4.75f, 2.0f, -Inner + 0.06f);
            Cube(board, "Border", Vector3.zero, new Vector3(1.4f, 0.62f, 0.05f), Kit("MI_WoodTrim"));
            var face = Cube(board, "Face", new Vector3(0f, 0f, 0.03f), new Vector3(1.28f, 0.5f, 0.02f), Mat("ScreenDay", new Color(0.1f, 0.35f, 0.45f), 0.6f, new Color(0.12f, 0.5f, 0.65f)));
            Screens.Add(face.GetComponent<Renderer>());
            var text = new GameObject("ChannelText");
            text.transform.SetParent(board, false);
            text.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // read from inside the room (looking -Z)
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = "KANAL";
            text.AddComponent<DepthTestedText>();
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.06f; mesh.fontSize = 100; mesh.color = new Color(0.96f, 0.98f, 1f);
        }
    }
}
