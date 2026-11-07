using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #249: builds the enterable equipment store as an ISOLATED prefab (Resources/CityKit/EquipmentStore) from the Quaternius MegaKit modules
    // (2 m wall modules, 3.12 m storeys) plus a few procedural pieces (shelves, counter, product models). It is not placed in any scene: the
    // building's street position, ground and world collision are Utku's, the shelf selection, basket and till binding are Mehmet's.
    //
    // Frame: origin at the ground centre of the FRONT wall's outer line; the street is at -Z, the shop body extends to +Z (10 m wide x 8 m
    // deep); the door is centred on X = 0. The ground floor is flush with the street (threshold 0.00 m).
    // DeepDive > P4.10 > Build equipment store prefab, or -executeMethod DeepDive.EditorTools.P410EquipmentStoreBuilder.Build
    public static class P410EquipmentStoreBuilder
    {
        public const string Folder = "Assets/DeepDive/Art/Resources/CityKit";
        public const string PrefabPath = Folder + "/EquipmentStore.prefab";
        private const string Models = "Assets/DeepDive/Art/MegaKit/Models/";
        private const string KitMaterials = "Assets/DeepDive/Art/MegaKit/Materials/";

        // Dimensions the tests and the owners can rely on.
        public const float Width = 10f, Depth = 8f, Storey = 3.12f;
        public const float OpeningWidth = 2.6f, OpeningHeight = 2.6f;
        public const float WallOuter = -0.31f, WallInner = 0.09f;

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly List<(string id, Transform anchor, int items)> Displays = new List<(string, Transform, int)>();

        [MenuItem("DeepDive/P4.10/Build equipment store prefab")]
        public static void Build()
        {
            Cache.Clear();
            Displays.Clear();
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();

            var root = new GameObject("EquipmentStore");
            try
            {
                Shell(root.transform);
                Interior(root.transform);
                Collision(root.transform);
                Lights(root.transform);
                Anchors(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("P410_STORE_BUILT " + PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }

        // ---- helpers ------------------------------------------------------------------------------------

        // The kit's FBX roots carry their own axis fix-up and scale, so a module is never scaled through its own transform: a scale goes on a
        // wrapper (world axes, no rotation) and the module sits unchanged inside it. Use it for non-uniform fills only (plain plaster).
        private static GameObject Part(Transform parent, string model, Vector3 position, float yaw = 0f, Vector3? scale = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
            if (asset == null) throw new FileNotFoundException("P410_STORE_MODEL_MISSING " + model);
            if (scale.HasValue)
            {
                var wrapper = new GameObject(model + "_Scaled");
                wrapper.transform.SetParent(parent, false);
                wrapper.transform.localPosition = position;
                wrapper.transform.localScale = scale.Value;
                parent = wrapper.transform;
                position = Vector3.zero;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            instance.name = model;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.localRotation;   // keep the FBX root axis fix-up
            return instance;
        }

        private static Material Mat(string name, Color color, float smoothness = 0.25f, Color? emission = null)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            var path = Folder + "/Materials/Store_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", emission.Value); }
            EditorUtility.SetDirty(material);
            Cache[name] = material;
            return material;
        }

        private static Material Kit(string name) => AssetDatabase.LoadAssetAtPath<Material>(KitMaterials + name + ".mat");

        private static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Transform Empty(Transform parent, string name, Vector3 position, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        // ---- shell: walls, floor, ceiling, storey above, roof -----------------------------------------------

        private static void Shell(Transform root)
        {
            var shell = Empty(root, "Shell", Vector3.zero);
            const float half = Width * 0.5f;                       // 5
            const float piece = (Width - OpeningWidth) * 0.5f;     // 3.7 m of front wall each side of the opening
            const float module = piece * 0.5f;                     // two 1.85 m modules (scaled 0.925) per side
            var sx = new Vector3(module / 2f, 1f, 1f);

            // Ground floor front: display window + plain wall each side of the 2.6 m opening.
            Part(shell, "Wall_Plaster_Window_Wide_Flat", new Vector3(-half + module * 0.5f, 0f, 0f), 0f, sx);
            Part(shell, "Wall_Plaster_Straight", new Vector3(-half + module * 1.5f, 0f, 0f), 0f, sx);
            Part(shell, "Wall_Plaster_Straight", new Vector3(half - module * 1.5f, 0f, 0f), 0f, sx);
            Part(shell, "Wall_Plaster_Window_Wide_Flat", new Vector3(half - module * 0.5f, 0f, 0f), 0f, sx);
            Part(shell, "Window_Wide_Flat1", new Vector3(-half + module * 0.5f, 0f, 0f), 0f, sx);
            Part(shell, "Window_Wide_Flat1", new Vector3(half - module * 0.5f, 0f, 0f), 0f, sx);
            // Wall above the opening (clear height 2.6 m), a timber lintel under it, and the sign board.
            var bandHeight = Storey - OpeningHeight;
            Part(shell, "Wall_Plaster_Straight", new Vector3(0f, OpeningHeight, 0f), 0f, new Vector3(OpeningWidth / 2f, bandHeight / Storey, 1f));
            Prim(shell, PrimitiveType.Cube, "Lintel", new Vector3(0f, OpeningHeight - 0.09f, -0.11f), new Vector3(OpeningWidth + 0.3f, 0.18f, 0.5f), Kit("MI_WoodTrim"));
            foreach (var side in new[] { -1f, 1f })
                Prim(shell, PrimitiveType.Cube, "Jamb" + (side < 0 ? "L" : "R"), new Vector3(side * (OpeningWidth * 0.5f + 0.06f), OpeningHeight * 0.5f, -0.11f),
                    new Vector3(0.12f, OpeningHeight, 0.46f), Kit("MI_WoodTrim"));
            var sign = Prim(shell, PrimitiveType.Cube, "SignBoard", new Vector3(0f, Storey + 0.55f, -0.40f), new Vector3(3.2f, 0.75f, 0.12f), Kit("Shop_Accent_Equipment"));
            var text = new GameObject("SignText");
            text.transform.SetParent(sign.transform.parent, false);
            text.transform.localPosition = new Vector3(0f, Storey + 0.55f, -0.48f);
            text.transform.localRotation = Quaternion.identity;
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = "EKIPMAN";
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.1f; mesh.fontSize = 100; mesh.color = new Color(0.97f, 0.93f, 0.8f);

            // Ground floor sides and back (2 m modules), ground floor slab.
            for (var i = 0; i < 4; i++)
            {
                Part(shell, "Wall_Plaster_Straight", new Vector3(-half, 0f, 1f + 2f * i), 90f);
                Part(shell, "Wall_Plaster_Straight", new Vector3(half, 0f, 1f + 2f * i), -90f);
            }
            for (var i = -2; i <= 2; i++) Part(shell, "Wall_Plaster_Straight", new Vector3(2f * i, 0f, Depth), 180f);
            for (var x = -4; x <= 4; x += 2)
                for (var z = 1; z <= 7; z += 2) Part(shell, "Floor_WoodDark", new Vector3(x, 0f, z));
            Prim(shell, PrimitiveType.Cube, "Ceiling", new Vector3(0f, Storey - 0.1f, Depth * 0.5f), new Vector3(Width - 0.1f, 0.2f, Depth - 0.1f), Kit("MI_Plaster"));

            // Closed storey above (not an interior): windows on the street side, plain walls elsewhere.
            for (var i = -2; i <= 2; i++)
            {
                Part(shell, "Wall_Plaster_Window_Wide_Flat", new Vector3(2f * i, Storey, 0f));
                Part(shell, "Window_Wide_Flat1", new Vector3(2f * i, Storey, 0f));
                Part(shell, "Wall_Plaster_Straight", new Vector3(2f * i, Storey, Depth), 180f);
            }
            for (var i = 0; i < 4; i++)
            {
                Part(shell, "Wall_Plaster_Straight", new Vector3(-half, Storey, 1f + 2f * i), 90f);
                Part(shell, "Wall_Plaster_Straight", new Vector3(half, Storey, 1f + 2f * i), -90f);
            }
            foreach (var x in new[] { -half, half })
                foreach (var z in new[] { -0.1f, Depth + 0.1f })
                {
                    Part(shell, "Corner_Exterior_Wood", new Vector3(x, 0f, z));
                    Part(shell, "Corner_Exterior_Wood", new Vector3(x, Storey, z));
                }
            Part(shell, "Roof_RoundTiles_8x8", new Vector3(0f, Storey * 2f, Depth * 0.5f));
            var gable = Vector3.one * (9.95f / 8.51f);
            Part(shell, "Roof_Front_Brick8", new Vector3(0f, Storey * 2f, 0f), 0f, gable);
            Part(shell, "Roof_Front_Brick8", new Vector3(0f, Storey * 2f, Depth), 180f, gable);
        }

        // ---- interior: shelves, counter, central display, products ------------------------------------------------

        private static readonly float[] Levels = { 0.12f, 0.72f, 1.32f };

        private static Transform Shelf(Transform parent, string name, Vector3 position, float yaw, float width)
        {
            var shelf = Empty(parent, name, position, yaw);
            var wood = Kit("MI_WoodTrim");
            foreach (var side in new[] { -1f, 1f })
                Prim(shelf, PrimitiveType.Cube, "Side", new Vector3(side * (width * 0.5f - 0.03f), 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.46f), wood);
            Prim(shelf, PrimitiveType.Cube, "Back", new Vector3(0f, 1.05f, 0.21f), new Vector3(width, 2.1f, 0.03f), Mat("ShelfBack", new Color(0.36f, 0.25f, 0.15f), 0.15f));
            foreach (var y in Levels) Prim(shelf, PrimitiveType.Cube, "Board", new Vector3(0f, y, 0f), new Vector3(width - 0.06f, 0.04f, 0.46f), wood);
            Prim(shelf, PrimitiveType.Cube, "Top", new Vector3(0f, 2.1f, 0f), new Vector3(width, 0.05f, 0.5f), wood);
            return shelf;
        }

        // Places `count` copies of one product along a shelf board; the group's anchor is registered for the layout.
        private static void Stock(Transform shelf, string equipmentId, int level, float from, float to, int count, float yaw = 0f)
        {
            var group = Empty(shelf, "Display_" + equipmentId + "_" + level, new Vector3((from + to) * 0.5f, Levels[level] + 0.02f, -0.02f));
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : i / (float)(count - 1);
                var item = Product(group, equipmentId);
                item.localPosition = new Vector3((Mathf.Lerp(from, to, t) - (from + to) * 0.5f), 0f, 0f);
                item.localRotation = Quaternion.Euler(0f, yaw, 0f);   // a product's front is -Z, the customer stands at -Z of the shelf frame
            }
            Displays.Add((equipmentId, group, count));
        }

        // Layout (prefab frame, door at z = 0, depth +Z): the counter runs across the back wall, centred, so a customer who walks in from the
        // street meets it head on; shelf units line both side walls and flank the counter on the back wall; one display table stands in
        // the middle. The shopkeeper stands 1.7 m behind the customer point, exactly like the town's stalls (customer = clerk + 1.7 m).
        public const float CounterZ = 6.4f, CounterHalfLength = 1.6f, CounterDepth = 0.8f;
        public const float CustomerZ = 5.45f, KeeperZ = 7.15f, TableZ = 3.0f;
        public const float SideShelfX = 4.67f, BackShelfX = 3.4f;
        public static readonly float[] SideShelfZ = { 2.0f, 4.5f };

        private static void Interior(Transform root)
        {
            var interior = Empty(root, "Interior", Vector3.zero);
            var wood = Kit("MI_WoodTrim");
            var wear = Kit("MI_WoodTrim_Wear");

            // Back wall: a shelf unit on each side of the counter, harpoons on brackets above the right one.
            var backZ = Depth - 0.09f - 0.25f;
            var shelfA = Shelf(interior, "Shelf_Back_A", new Vector3(-BackShelfX, 0f, backZ), 0f, 2.2f);
            var shelfB = Shelf(interior, "Shelf_Back_B", new Vector3(BackShelfX, 0f, backZ), 0f, 2.2f);
            Stock(shelfA, "tube-2", 0, -0.85f, 0.85f, 5);
            Stock(shelfA, "tube-1", 1, -0.9f, 0.9f, 6);
            Stock(shelfA, "bag-1", 2, -0.75f, 0.75f, 4);
            Stock(shelfB, "fins-1", 0, -0.85f, 0.85f, 5);
            Stock(shelfB, "tube-1", 1, -0.9f, 0.9f, 5);
            Stock(shelfB, "camera-basic", 2, -0.85f, 0.85f, 4);
            var rack = Empty(interior, "Display_harpoon-1_rack", new Vector3(BackShelfX, 0f, Depth - 0.09f - 0.1f));
            var brackets = Empty(interior, "Harpoon_Brackets", new Vector3(BackShelfX, 0f, Depth - 0.09f - 0.1f));
            for (var i = 0; i < 3; i++)
            {
                var harpoon = Product(rack, "harpoon-1");
                harpoon.localPosition = new Vector3(0f, 2.3f + 0.28f * i, 0f);
                harpoon.localRotation = Quaternion.identity;
                foreach (var bx in new[] { -0.55f, 0.55f }) Prim(brackets, PrimitiveType.Cube, "Bracket", new Vector3(bx, 2.3f + 0.28f * i - 0.05f, 0.04f), new Vector3(0.05f, 0.05f, 0.1f), wear);
            }
            Displays.Add(("harpoon-1", rack, 3));

            // Side walls: two shelf units each, facing the room.
            var left1 = Shelf(interior, "Shelf_Left_1", new Vector3(-SideShelfX, 0f, SideShelfZ[0]), -90f, 2.0f);
            var left2 = Shelf(interior, "Shelf_Left_2", new Vector3(-SideShelfX, 0f, SideShelfZ[1]), -90f, 2.0f);
            var right1 = Shelf(interior, "Shelf_Right_1", new Vector3(SideShelfX, 0f, SideShelfZ[0]), 90f, 2.0f);
            var right2 = Shelf(interior, "Shelf_Right_2", new Vector3(SideShelfX, 0f, SideShelfZ[1]), 90f, 2.0f);
            Stock(left1, "tube-2", 0, -0.8f, 0.8f, 4);
            Stock(left1, "tube-1", 1, -0.8f, 0.8f, 5);
            Stock(left1, "bag-1", 2, -0.7f, 0.7f, 3);
            Stock(left2, "fins-1", 0, -0.8f, 0.8f, 4);
            Stock(left2, "bag-1", 1, -0.8f, 0.8f, 4);
            Stock(left2, "camera-basic", 2, -0.7f, 0.7f, 3);
            Stock(right1, "tube-1", 0, -0.8f, 0.8f, 5);
            Stock(right1, "fins-1", 1, -0.8f, 0.8f, 4);
            Stock(right1, "bag-1", 2, -0.7f, 0.7f, 3);
            Stock(right2, "tube-2", 0, -0.8f, 0.8f, 4);
            Stock(right2, "camera-basic", 1, -0.8f, 0.8f, 4);
            Stock(right2, "fins-1", 2, -0.7f, 0.7f, 3);

            // Central display table with the three camera tiers on plinths.
            var table = Empty(interior, "Table_Center", new Vector3(0f, 0f, TableZ));
            Prim(table, PrimitiveType.Cube, "Top", new Vector3(0f, 0.82f, 0f), new Vector3(2.4f, 0.06f, 1.0f), wood);
            foreach (var lx in new[] { -1.05f, 1.05f })
                foreach (var lz in new[] { -0.4f, 0.4f }) Prim(table, PrimitiveType.Cube, "Leg", new Vector3(lx, 0.4f, lz), new Vector3(0.08f, 0.8f, 0.08f), wear);
            var tiers = new[] { "camera-basic", "camera-advanced", "camera-professional" };
            for (var i = 0; i < 3; i++)
            {
                var x = -0.8f + 0.8f * i;
                Prim(table, PrimitiveType.Cube, "Plinth", new Vector3(x, 0.89f, 0f), new Vector3(0.6f, 0.08f, 0.5f), wear);
                var group = Empty(table, "Display_" + tiers[i], new Vector3(x, 0.93f, 0f));
                for (var k = 0; k < 2; k++)
                {
                    var item = Product(group, tiers[i]);
                    item.localPosition = new Vector3(-0.12f + 0.24f * k, 0f, 0f);
                    item.localRotation = Quaternion.identity;
                }
                Displays.Add((tiers[i], group, 2));
            }

            // Counter across the back wall with the till, a bell and a lamp.
            var counter = Empty(interior, "Counter", new Vector3(0f, 0f, CounterZ));
            Prim(counter, PrimitiveType.Cube, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(CounterHalfLength * 2f, 1.0f, CounterDepth), wear);
            Prim(counter, PrimitiveType.Cube, "Top", new Vector3(0f, 1.02f, -0.02f), new Vector3(CounterHalfLength * 2f + 0.12f, 0.05f, CounterDepth + 0.12f), wood);
            var till = Empty(counter, "Till", new Vector3(0.6f, 1.045f, 0f));
            Prim(till, PrimitiveType.Cube, "Box", new Vector3(0f, 0.1f, 0f), new Vector3(0.34f, 0.2f, 0.3f), Mat("TillBody", new Color(0.28f, 0.28f, 0.3f)));
            Prim(till, PrimitiveType.Cube, "Screen", new Vector3(0f, 0.25f, -0.05f), new Vector3(0.3f, 0.14f, 0.03f), Mat("TillScreen", new Color(0.2f, 0.5f, 0.4f), 0.5f, new Color(0.1f, 0.5f, 0.4f)), new Vector3(20f, 0f, 0f));
            Prim(counter, PrimitiveType.Cylinder, "BellBase", new Vector3(-0.7f, 1.07f, -0.1f), new Vector3(0.14f, 0.015f, 0.14f), Mat("Brass", new Color(0.78f, 0.6f, 0.25f), 0.6f));
            // Crates by the door and a floor mat inside it so the entrance reads from the street.
            Part(interior, "Prop_Crate", new Vector3(-4.2f, 0f, 0.55f), 20f, Vector3.one * 0.7f);
            Part(interior, "Prop_Crate", new Vector3(-4.25f, 0.74f, 0.55f), 5f, Vector3.one * 0.5f);
            Part(interior, "Prop_Crate", new Vector3(4.2f, 0f, 0.6f), -15f, Vector3.one * 0.6f);
            Prim(interior, PrimitiveType.Cube, "DoorMat", new Vector3(0f, 0.02f, 0.9f), new Vector3(2.0f, 0.02f, 1.1f), Mat("DoorMat", new Color(0.42f, 0.16f, 0.14f), 0.1f));
        }

        // ---- products (procedural, one model per sold equipment id) --------------------------------------------

        private static Transform Product(Transform parent, string id)
        {
            var root = Empty(parent, "Product_" + id, Vector3.zero);
            root.localScale = Vector3.one * 1.3f;
            switch (id)
            {
                case "camera-basic": Camera(root, new Color(0.45f, 0.5f, 0.55f), 1f); break;
                case "camera-advanced": Camera(root, new Color(0.2f, 0.35f, 0.6f), 1.15f); break;
                case "camera-professional": Camera(root, new Color(0.1f, 0.1f, 0.12f), 1.3f, true); break;
                case "tube-1": Tube(root, new Color(0.92f, 0.78f, 0.2f), 1f); break;
                case "tube-2": Tube(root, new Color(0.9f, 0.4f, 0.12f), 1.4f); break;
                case "fins-1": Fins(root); break;
                case "bag-1": Bag(root); break;
                case "harpoon-1": Harpoon(root); break;
                default: throw new ArgumentException("P410_STORE_UNKNOWN_PRODUCT " + id);
            }
            return root;
        }

        private static void Camera(Transform root, Color body, float scale, bool gold = false)
        {
            var housing = Mat("Cam_" + ColorUtility.ToHtmlStringRGB(body), body, 0.4f);
            Prim(root, PrimitiveType.Cube, "Body", new Vector3(0f, 0.08f, 0f) * scale, new Vector3(0.22f, 0.14f, 0.12f) * scale, housing);
            Prim(root, PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0.08f, -0.09f) * scale, new Vector3(0.1f, 0.04f, 0.1f) * scale, Mat("CamLens", new Color(0.05f, 0.08f, 0.12f), 0.8f), new Vector3(90f, 0f, 0f));
            Prim(root, PrimitiveType.Cube, "Grip", new Vector3(0.08f, 0.17f, 0f) * scale, new Vector3(0.05f, 0.03f, 0.08f) * scale,
                gold ? Mat("Brass", new Color(0.78f, 0.6f, 0.25f), 0.6f) : Mat("CamGrip", new Color(0.8f, 0.8f, 0.82f), 0.3f));
        }

        private static void Tube(Transform root, Color paint, float length)
        {
            Prim(root, PrimitiveType.Cylinder, "Tank", new Vector3(0f, 0.17f * length, 0f), new Vector3(0.18f, 0.17f * length, 0.18f), Mat("Tube_" + ColorUtility.ToHtmlStringRGB(paint), paint, 0.35f));
            Prim(root, PrimitiveType.Sphere, "Dome", new Vector3(0f, 0.34f * length, 0f), new Vector3(0.18f, 0.12f, 0.18f), Mat("Tube_" + ColorUtility.ToHtmlStringRGB(paint), paint, 0.35f));
            Prim(root, PrimitiveType.Cylinder, "Valve", new Vector3(0f, 0.4f * length, 0f), new Vector3(0.05f, 0.04f, 0.05f), Mat("Brass", new Color(0.78f, 0.6f, 0.25f), 0.6f));
            Prim(root, PrimitiveType.Cylinder, "Band", new Vector3(0f, 0.2f * length, 0f), new Vector3(0.19f, 0.012f, 0.19f), Mat("TubeBand", new Color(0.15f, 0.15f, 0.17f)));
        }

        private static void Fins(Transform root)
        {
            var blue = Mat("Fin", new Color(0.1f, 0.55f, 0.65f), 0.3f);
            foreach (var x in new[] { -0.1f, 0.1f })
            {
                Prim(root, PrimitiveType.Cube, "Blade", new Vector3(x, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.4f), blue, new Vector3(0f, x < 0 ? 8f : -8f, 0f));
                Prim(root, PrimitiveType.Cube, "Foot", new Vector3(x, 0.07f, 0.17f), new Vector3(0.1f, 0.1f, 0.1f), Mat("FinFoot", new Color(0.12f, 0.12f, 0.14f)));
            }
        }

        private static void Bag(Transform root)
        {
            Prim(root, PrimitiveType.Cube, "Pack", new Vector3(0f, 0.13f, 0f), new Vector3(0.34f, 0.26f, 0.18f), Mat("Bag", new Color(0.45f, 0.3f, 0.16f), 0.15f));
            Prim(root, PrimitiveType.Cube, "Flap", new Vector3(0f, 0.2f, -0.02f), new Vector3(0.35f, 0.09f, 0.2f), Mat("BagFlap", new Color(0.36f, 0.23f, 0.12f), 0.15f));
            Prim(root, PrimitiveType.Cube, "Strap", new Vector3(0f, 0.15f, -0.095f), new Vector3(0.05f, 0.24f, 0.02f), Mat("BagStrap", new Color(0.2f, 0.14f, 0.09f), 0.1f));
        }

        private static void Harpoon(Transform root)
        {
            var steel = Mat("Steel", new Color(0.62f, 0.64f, 0.68f), 0.6f);
            Prim(root, PrimitiveType.Cylinder, "Shaft", Vector3.zero, new Vector3(0.025f, 0.6f, 0.025f), steel, new Vector3(0f, 0f, 90f));
            Prim(root, PrimitiveType.Cube, "Barb", new Vector3(0.62f, 0f, 0f), new Vector3(0.1f, 0.05f, 0.012f), steel, new Vector3(0f, 0f, 45f));
            Prim(root, PrimitiveType.Cube, "Grip", new Vector3(-0.5f, 0f, 0f), new Vector3(0.28f, 0.05f, 0.05f), Mat("Grip", new Color(0.22f, 0.14f, 0.09f), 0.1f));
        }

        // ---- collision (simple boxes; ground and street are Utku's) --------------------------------------------

        private static void Box(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        private static void Collision(Transform root)
        {
            var c = Empty(root, "Collision", Vector3.zero);
            const float half = Width * 0.5f;
            const float piece = (Width - OpeningWidth) * 0.5f;
            const float thick = WallInner - WallOuter;              // 0.40
            const float wallMid = (WallInner + WallOuter) * 0.5f;   // -0.11
            Box(c, "Floor", new Vector3(0f, -0.1f, Depth * 0.5f), new Vector3(Width + 0.8f, 0.2f, Depth + 0.8f));
            // The side and back walls reach 0.31 m past the 5 m line; the front pieces run to the same outer edge.
            const float reach = Width * 0.5f - WallOuter - 0.0f;   // 5.31 (outer edge of a side wall)
            Box(c, "FrontLeft", new Vector3(-reach + (piece + (reach - half)) * 0.5f, Storey * 0.5f, wallMid), new Vector3(piece + (reach - half), Storey, thick));
            Box(c, "FrontRight", new Vector3(reach - (piece + (reach - half)) * 0.5f, Storey * 0.5f, wallMid), new Vector3(piece + (reach - half), Storey, thick));
            Box(c, "Lintel", new Vector3(0f, (OpeningHeight + Storey) * 0.5f, wallMid), new Vector3(OpeningWidth, Storey - OpeningHeight, thick));
            Box(c, "Back", new Vector3(0f, Storey * 0.5f, Depth - wallMid), new Vector3(reach * 2f, Storey, thick));
            Box(c, "Left", new Vector3(-half + wallMid, Storey * 0.5f, Depth * 0.5f), new Vector3(thick, Storey, Depth));
            Box(c, "Right", new Vector3(half - wallMid, Storey * 0.5f, Depth * 0.5f), new Vector3(thick, Storey, Depth));
            Box(c, "Counter", new Vector3(0f, 0.52f, CounterZ), new Vector3(CounterHalfLength * 2f + 0.1f, 1.04f, CounterDepth + 0.1f));
            Box(c, "Table", new Vector3(0f, 0.41f, TableZ), new Vector3(2.4f, 0.82f, 1.0f));
            var backZ = Depth - 0.09f - 0.25f;
            Box(c, "ShelfBackA", new Vector3(-BackShelfX, 1.05f, backZ), new Vector3(2.2f, 2.1f, 0.5f));
            Box(c, "ShelfBackB", new Vector3(BackShelfX, 1.05f, backZ), new Vector3(2.2f, 2.1f, 0.5f));
            Box(c, "ShelfLeft1", new Vector3(-SideShelfX, 1.05f, SideShelfZ[0]), new Vector3(0.5f, 2.1f, 2.0f));
            Box(c, "ShelfLeft2", new Vector3(-SideShelfX, 1.05f, SideShelfZ[1]), new Vector3(0.5f, 2.1f, 2.0f));
            Box(c, "ShelfRight1", new Vector3(SideShelfX, 1.05f, SideShelfZ[0]), new Vector3(0.5f, 2.1f, 2.0f));
            Box(c, "ShelfRight2", new Vector3(SideShelfX, 1.05f, SideShelfZ[1]), new Vector3(0.5f, 2.1f, 2.0f));
            Box(c, "CratesLeft", new Vector3(-4.2f, 0.45f, 0.55f), new Vector3(0.8f, 0.9f, 0.8f));
            Box(c, "CratesRight", new Vector3(4.2f, 0.4f, 0.6f), new Vector3(0.7f, 0.8f, 0.7f));
        }

        // ---- lights, anchors -------------------------------------------------------------------------------

        private static void Lights(Transform root)
        {
            var lights = Empty(root, "Lights", Vector3.zero);
            foreach (var x in new[] { -2.5f, 2.5f })
                foreach (var z in new[] { 2.2f, 5.8f })
                {
                    var go = new GameObject("ShopLight");
                    go.transform.SetParent(lights, false);
                    go.transform.localPosition = new Vector3(x, Storey - 0.6f, z);
                    var light = go.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.9f, 0.74f);
                    light.range = 6.5f;
                    light.intensity = 0.9f;
                    light.shadows = LightShadows.None;
                    Prim(go.transform, PrimitiveType.Sphere, "Bulb", Vector3.zero, Vector3.one * 0.16f, Mat("Bulb", new Color(1f, 0.95f, 0.8f), 0.5f, new Color(2f, 1.7f, 1.2f)));
                }
        }

        private static void Anchors(Transform root)
        {
            var anchors = Empty(root, "Anchors", Vector3.zero);
            var entrance = Empty(anchors, "Entrance", new Vector3(0f, 0f, WallOuter - 0.05f));
            var threshold = Empty(anchors, "Threshold", new Vector3(0f, 0f, (WallOuter + WallInner) * 0.5f));
            var customer = Empty(anchors, "CustomerPoint", new Vector3(0f, 0f, CustomerZ), 0f);
            var register = Empty(anchors, "Register", new Vector3(0.6f, 1.05f, CounterZ), 0f);
            var keeper = Empty(anchors, "ShopkeeperPoint", new Vector3(0f, 0f, KeeperZ), 180f);
            var exit = Empty(anchors, "ExitPoint", new Vector3(0f, 0f, -1.2f), 180f);

            var layout = root.gameObject.AddComponent(AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/DeepDive/Composition/EquipmentStoreLayout.cs").GetClass());
            var so = new SerializedObject(layout);
            so.FindProperty("Entrance").objectReferenceValue = entrance;
            so.FindProperty("Threshold").objectReferenceValue = threshold;
            so.FindProperty("CustomerPoint").objectReferenceValue = customer;
            so.FindProperty("Register").objectReferenceValue = register;
            so.FindProperty("ShopkeeperPoint").objectReferenceValue = keeper;
            so.FindProperty("ExitPoint").objectReferenceValue = exit;
            so.FindProperty("OpeningWidth").floatValue = OpeningWidth;
            so.FindProperty("OpeningHeight").floatValue = OpeningHeight;
            var list = so.FindProperty("Displays");
            list.arraySize = Displays.Count;
            for (var i = 0; i < Displays.Count; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("EquipmentId").stringValue = Displays[i].id;
                item.FindPropertyRelative("Anchor").objectReferenceValue = Displays[i].anchor;
                item.FindPropertyRelative("Items").intValue = Displays[i].items;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
