using System;
using System.IO;
using System.Linq;
using DeepDive.Network;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.World.Editor
{
    // Authors the P3.1 half of DiveTestArea: the WaterField that reads the existing SwimVolume,
    // and a piece of shore to cross the water line on.
    //
    // Separate from DiveTestAreaSetup (P2-B) and DiveTestAreaRecordingSetup/EventSetup (P3-B) on
    // purpose, the same way those are separate from each other: its menu path, its log tags and
    // its comments all say P3.1, and phases are tracked file by file (docs/plan/WORKFLOW.md:
    // "Faz acikken bir kisi gelecekteki faza ait kod, sahne veya icerik eklemez").
    //
    // It never destroys anything. DiveTestAreaSetup rebuilds WaterSurface by destroying and
    // recreating it, which is what makes a P2 re-run rewrite half the scene file; this script
    // creates a missing object and otherwise edits in place, so running it twice leaves the
    // scene byte for byte identical (ApplyAndVerify checks exactly that).
    //
    // No new NetworkObject: the shore is scenery and the field is host-side logic, so nothing
    // here regenerates a GlobalObjectIdHash and no NetworkPrefabs entry is needed.
    //
    // SwimVolume stays where it is and as it is. It is the canonical water in this scene and it
    // belongs to DeepDive.Network; the field only holds a reference to its collider.
    public static class DiveTestAreaWaterSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string FieldName = "WaterField";
        public const string LedgeName = "Shore_Ledge";
        public const string RampName = "Shore_Ramp";

        private const string WorldRoot = "Assets/DeepDive/World";
        private const string ShoreMaterialPath = WorldRoot + "/Art/ShoreMaterial.mat";

        // The shore sits in the south-east corner: clear of the fish wander box (centred on
        // z=+8), clear of the spawn ring (+-3), clear of the event at (-9, 3.5, -2), and about
        // 3.75 m from the inner face of either wall, so a diver can fall off any edge of it and
        // land in water rather than in the gap behind the geometry.
        public static readonly Vector3 LedgeCenter = new Vector3(9f, 7.9f, -9f);
        public static readonly Vector3 LedgeSize = new Vector3(4f, 1f, 4f);

        // Top of the ledge, a little over the surface (SwimVolume's top is y=8), so standing on
        // it is dry land and stepping off it is a real crossing rather than a rounding error.
        public static float LedgeTopY => LedgeCenter.y + LedgeSize.y * 0.5f;

        // The ramp runs west off the ledge and down toward the sea bed. 25 degrees is inside
        // the diver's slopeLimit of 45 with room to spare, and its top meets the ledge top
        // exactly, so there is no lip for stepOffset (0.3) to have to climb.
        //
        // 7.2 was its original foot, sized to stay clear of the OLD whole-arena DiveExit/
        // SafeReturnZone (y 6..8 everywhere). PR #70 replaced that with a small dry pad on
        // Shore_Ledge itself (Composition's SafeReturnZone), which this ramp's own foot cannot
        // reach or overlap regardless of height - SafeReturnZoneIsAReservedDryPadOnTheLedgeAnd-
        // StaysOffTheRamp checks that directly. So 7.2 no longer serves the purpose it was
        // built for, and it created a different, worse problem: NetworkDiver's CharacterController
        // is height 1.8, and PlayerPresentationRules blocks upward swim input once a diver
        // classifies as Surface, which happens once their feet reach the water's surfaceY (8,
        // SwimVolume's top) minus that height = 6.2. A ramp foot above 6.2 is a wall a
        // swimming diver's feet cannot rise past to ever reach it - this ramp is the only way
        // onto Shore_Ledge, so at 7.2 no diver could ever swim to a safe return at all (the same
        // bug DiveTestAreaBeachSetup.WadeFootY had, found the same way: two real divers in a
        // live 2-process smoke never reached the pad, stuck at y 6.3, see issue #62). 5.2 clears
        // that ceiling by a full metre, the same margin the beach's wade shelf uses.
        public const float RampAngleDegrees = 25f;
        public const float RampBottomY = 5.2f;
        public const float RampThickness = 0.4f;

        [MenuItem("DeepDive/P3.1/Dive test area: water field and shore")]
        public static void Apply()
        {
            var shoreMaterial = EnsureShoreMaterial();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var swim = UnityEngine.Object.FindFirstObjectByType<SwimVolume>();
            if (swim == null)
                throw new InvalidOperationException(
                    $"P3_WATER_NO_SWIMVOLUME scene={ScenePath} reason=run DeepDive/P2/Dive test area first");
            var swimBox = swim.GetComponent<BoxCollider>();
            if (swimBox == null)
                throw new InvalidOperationException($"P3_WATER_SWIMVOLUME_NO_BOX object={swim.name}");

            var field = EnsureField(scene, swimBox);
            var ledge = EnsureLedge(scene, shoreMaterial);
            var ramp = EnsureRamp(scene, shoreMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P3_WATER_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();

            Debug.Log($"P3_DIVEAREA_WATER_READY field={field.name} volumes={field.VolumeCount} " +
                      $"deadband={field.Deadband} ledgeTop={LedgeTopY} ramp={ramp.name} " +
                      $"rampAngle={RampAngleDegrees} ledge={ledge.transform.position}");
        }

        // Byte-idempotence is the real contract: DiveTestArea is shared ground and Unity YAML
        // merges badly, so re-running this has to be a repair rather than a second edit.
        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P3_WATER_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P3_WATER_SETUP_IDEMPOTENT");
        }

        private static WaterField EnsureField(UnityEngine.SceneManagement.Scene scene, BoxCollider swimBox)
        {
            var root = FindRoot(scene, FieldName) ?? new GameObject(FieldName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            root.layer = 0;

            var field = root.GetComponent<WaterField>() ?? root.AddComponent<WaterField>();

            // Written through SerializedObject because both fields are private [SerializeField];
            // assigning them any other way would not survive the scene save.
            var data = new SerializedObject(field);
            var list = data.FindProperty("volumes");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = swimBox;
            data.FindProperty("deadband").floatValue = WaterTracker.DefaultDeadband;
            data.ApplyModifiedPropertiesWithoutUndo();
            return field;
        }

        private static GameObject EnsureLedge(UnityEngine.SceneManagement.Scene scene, Material material)
        {
            var ledge = EnsureShorePiece(scene, LedgeName, material);
            ledge.transform.SetPositionAndRotation(LedgeCenter, Quaternion.identity);
            ledge.transform.localScale = LedgeSize;
            return ledge;
        }

        private static GameObject EnsureRamp(UnityEngine.SceneManagement.Scene scene, Material material)
        {
            var ramp = EnsureShorePiece(scene, RampName, material);

            // The ramp's top face runs from the ledge's west edge down to RampBottomY. Solved
            // from the angle rather than typed in, so the two ends stay joined if the ledge or
            // the angle is ever retuned.
            var angle = RampAngleDegrees * Mathf.Deg2Rad;
            var topX = LedgeCenter.x - LedgeSize.x * 0.5f;
            var drop = LedgeTopY - RampBottomY;
            var run = drop / Mathf.Tan(angle);
            var slopeLength = drop / Mathf.Sin(angle);

            // Centre of the top face, then pushed half a thickness along the face normal to get
            // the cube's centre.
            var topMid = new Vector3(topX - run * 0.5f, (LedgeTopY + RampBottomY) * 0.5f, LedgeCenter.z);
            var normal = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0f);

            ramp.transform.SetPositionAndRotation(
                topMid - normal * (RampThickness * 0.5f),
                Quaternion.Euler(0f, 0f, RampAngleDegrees));
            ramp.transform.localScale = new Vector3(slopeLength, RampThickness, LedgeSize.z);
            return ramp;
        }

        // Creates the cube only when it is missing. An existing one is retuned in place, so a
        // re-run never renumbers the scene's fileIDs.
        private static GameObject EnsureShorePiece(UnityEngine.SceneManagement.Scene scene, string name, Material material)
        {
            var piece = FindRoot(scene, name);
            if (piece == null)
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = name;
            }

            piece.layer = 0;

            // Solid, not a trigger: the diver has to be able to stand on it.
            var box = piece.GetComponent<BoxCollider>() ?? piece.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = Vector3.one;

            var renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            // Scenery only. A NetworkObject here would need a GlobalObjectIdHash and would make
            // the shore part of the netcode surface for no reason.
            if (piece.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException($"P3_WATER_SHORE_NETWORKED object={name}");

            return piece;
        }

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"P3_WATER_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }

        // Sand against the teal water, so the shore reads as somewhere to stand rather than as
        // more arena wall. Lit, unlike WaterSurface: this one is meant to take the sun.
        private static Material EnsureShoreMaterial()
        {
            if (!AssetDatabase.IsValidFolder(WorldRoot + "/Art"))
                AssetDatabase.CreateFolder(WorldRoot, "Art");

            const string shaderName = "Universal Render Pipeline/Lit";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException($"P3_WATER_SHADER_MISSING shader={shaderName}");

            var material = AssetDatabase.LoadAssetAtPath<Material>(ShoreMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ShoreMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.76f, 0.68f, 0.50f));
            material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
