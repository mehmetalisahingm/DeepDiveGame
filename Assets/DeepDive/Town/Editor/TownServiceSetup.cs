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
    // P3.2-C: places the three physical NPC service points on the DiveTestArea beach. Since PR #65 Prep, Dive and
    // Return all run in DiveTestArea (PrepArea is the lobby only), and service interaction is gated by phase, so the
    // town has to stand in the coast world the players walk back into. Each NPC is scenery plus a ServicePointAnchor
    // (Mehmet's seam); prices, money and pending items live in the economy, never here.
    //
    // Same rules as DiveTestAreaWaterSetup: it creates what is missing and edits the rest in place, never
    // destroys, and adds no NetworkObject, so re-running leaves the scene byte for byte identical
    // (ApplyAndVerify checks that). The NPCs are primitive placeholders; real NPC art is a later pass.
    public static class TownServiceSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string RootName = "TownServices";

        // Utku's #61 marker for where the beach meets the town (a bare transform on the south strip). The NPC row is
        // derived from it, not typed twice, so if World moves the gate a re-run of this menu follows it.
        public const string GateName = "Beach_TownGate";

        private const string ArtRoot = "Assets/DeepDive/Town/Art";

        // The three NPCs stand in a row on the beach strip, east of the gate and facing north (+z, toward the water the
        // divers come out of). East because the wade shelf that walks divers out of the water lands west of the gate
        // (x -9.5..-3.5): the arrival lane stays clear and the fuel-tank part (x 12.5) is 4.7 m past the last NPC.
        // Offsets are relative to the gate; only the gate's height is inherited (it is the strip's top).
        public static readonly Vector3 RowOffset = new Vector3(3f, 0f, 0f);
        public static readonly Vector3 RowStep = new Vector3(2.4f, 0f, 0f);
        public static readonly Quaternion Facing = Quaternion.identity;

        public static string NpcName(ServicePointDefinition definition) => "Npc_" + definition.ServiceId;

        [MenuItem("DeepDive/P3.2/Dive test area: town service NPCs")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var gate = scene.GetRootGameObjects().FirstOrDefault(x => x.name == GateName);
            if (gate == null)
                throw new InvalidOperationException($"P3_TOWN_GATE_MISSING scene={ScenePath} name={GateName} reason=run DeepDive/P3.2/Dive test area: beach and shallows first");
            var origin = gate.transform.position + RowOffset;

            var root = FindRoot(scene, RootName) ?? new GameObject(RootName);
            if (root.scene != scene) SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            for (var i = 0; i < TownServiceCatalog.All.Count; i++)
                EnsureNpc(root.transform, TownServiceCatalog.All[i], origin + RowStep * i);

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
            npc.transform.SetPositionAndRotation(position, Facing);
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
