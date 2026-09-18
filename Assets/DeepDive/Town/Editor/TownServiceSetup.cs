using System;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Town.Editor
{
    // P3.2-C: places the three physical NPC service points in PrepArea (the town / non-dive scene). Each NPC is scenery plus a
    // ServicePointAnchor (Mehmet's seam); prices, money and pending items live in the economy, never here.
    //
    // Same rules as DiveTestAreaWaterSetup: it creates what is missing and edits the rest in place, never
    // destroys, and adds no NetworkObject, so re-running leaves the scene byte for byte identical
    // (ApplyAndVerify checks that). The NPCs are primitive placeholders; real NPC art is a later pass.
    public static class TownServiceSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/PrepArea.unity";
        public const string RootName = "TownServices";

        private const string ArtRoot = "Assets/DeepDive/Town/Art";

        // The town is where the non-dive phases run: SessionNetworkAdapter loads PrepArea for Lobby, Prep and
        // Return (and DiveTestArea only for Dive, where service interaction is refused). The three NPCs stand in
        // a row along the west wall, facing the room. Moving TownServices is enough to relocate them; ids and
        // anchors do not change.
        public static readonly Vector3 RowStart = new Vector3(-4f, 0f, -2.4f);
        public static readonly Vector3 RowStep = new Vector3(0f, 0f, 2.4f);

        public static string NpcName(ServicePointDefinition definition) => "Npc_" + definition.ServiceId;

        [MenuItem("DeepDive/P3.2/Prep area: town service NPCs")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = FindRoot(scene, RootName) ?? new GameObject(RootName);
            if (root.scene != scene) SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            for (var i = 0; i < TownServiceCatalog.All.Count; i++)
                EnsureNpc(root.transform, TownServiceCatalog.All[i], RowStart + RowStep * i);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P3_TOWN_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();
            Debug.Log($"P3_TOWN_SERVICES_READY npcs={TownServiceCatalog.All.Count} root={RootName}");
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P3_TOWN_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P3_TOWN_SETUP_IDEMPOTENT");
        }

        private static void EnsureNpc(Transform parent, ServicePointDefinition definition, Vector3 position)
        {
            var name = NpcName(definition);
            var npc = parent.Find(name)?.gameObject ?? new GameObject(name);
            npc.transform.SetParent(parent, false);
            // Faces east, into the room.
            npc.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 90f, 0f));
            npc.transform.localScale = Vector3.one;

            var body = EnsurePrimitive(npc.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f),
                new Vector3(0.8f, 1f, 0.8f), Material(NpcColor(definition.ServiceType, 0.55f), "Body"), keepCollider: true);
            EnsurePrimitive(npc.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.15f, 0f),
                new Vector3(0.5f, 0.5f, 0.5f), Material(new Color(0.93f, 0.78f, 0.62f), "Skin"), keepCollider: false);
            // A colored counter sign so the three services read apart at a glance.
            EnsurePrimitive(npc.transform, "Sign", PrimitiveType.Cube, new Vector3(0f, 2.9f, 0f),
                new Vector3(1.2f, 0.4f, 0.1f), Material(NpcColor(definition.ServiceType, 1f), "Sign"), keepCollider: false);
            body.layer = 0;

            var anchor = npc.GetComponent<ServicePointAnchor>() ?? npc.AddComponent<ServicePointAnchor>();
            anchor.Configure(definition.ServiceId, definition.ServiceType, definition.WorldAnchor,
                definition.InteractionDistance, definition.CatalogId);
            EditorUtility.SetDirty(anchor);
        }

        private static GameObject EnsurePrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            var existing = parent.Find(name);
            var go = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            var collider = go.GetComponent<Collider>();
            if (!keepCollider && collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static Color NpcColor(ServicePointType type, float strength)
        {
            var color = type == ServicePointType.EquipmentShop ? new Color(0.20f, 0.45f, 0.85f)
                : type == ServicePointType.FishBuyer ? new Color(0.25f, 0.70f, 0.35f)
                : new Color(0.85f, 0.55f, 0.15f);
            return Color.Lerp(Color.white, color, strength);
        }

        private static Material Material(Color color, string label)
        {
            Directory.CreateDirectory(ArtRoot);
            var path = $"{ArtRoot}/Town_{label}_{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(x => x.name == name);
    }
}
