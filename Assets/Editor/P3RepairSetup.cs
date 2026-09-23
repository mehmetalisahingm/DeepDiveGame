using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using DeepDive.Network;
using DeepDive.Composition;
using DeepDive.World;
using DeepDive.World.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeepDive.Editor
{
    public static class P3RepairSetup
    {
        public const string HumanSource = "Assets/DeepDive/Player/Character/Source/CoastalHuman.fbx";
        private const string Root = "Assets/DeepDive/Player/Character";
        private const string Animations = Root + "/Source/CoastalAnimations.fbx";

        [MenuItem("DeepDive/P3/Apply playable coast and shared human")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root + "/CoastalMaterials");
            foreach (var path in new[] { HumanSource, Animations })
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.isReadable = true;
                importer.importAnimation = true;
                var settings = importer.defaultClipAnimations;
                foreach (var clip in settings) { clip.loopTime = true; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true; clip.lockRootRotation = true; }
                importer.clipAnimations = settings;
                importer.SaveAndReimport();
            }
            var clips = AssetDatabase.LoadAllAssetsAtPath(Animations).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            File.WriteAllText("Logs/coastal-animation-clips.txt", string.Join("\n", clips.Select(c => c.name)));
            var idle = clips.FirstOrDefault(c => c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0);
            var walk = clips.FirstOrDefault(c => c.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0);
            if (idle == null || walk == null) throw new InvalidOperationException("CC0 idle/walk clips missing; see Logs/coastal-animation-clips.txt");
            var controllerPath = Root + "/Animator/CoastalLocomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var machine = controller.layers[0].stateMachine;
                foreach (var name in new[] { "Land_Idle", "Land_Walk", "Water_Tread", "Water_Surface", "Water_Underwater" })
                {
                    var state = machine.AddState(name);
                    state.motion = name == "Land_Walk" ? walk : idle;
                    if (name == "Land_Idle") machine.defaultState = state;
                }
            }
            var body = BuildHuman(false, controller);
            var arms = BuildHuman(true, controller);
            var catalog = new SerializedObject(AssetDatabase.LoadAssetAtPath<DiverPresentationCatalog>(
                "Assets/DeepDive/Network/Resources/DiverPresentationCatalog.asset"));
            catalog.FindProperty("thirdPersonRigPrefab").objectReferenceValue = body;
            catalog.FindProperty("firstPersonArmsPrefab").objectReferenceValue = arms;
            catalog.ApplyModifiedPropertiesWithoutUndo();
            DiveTestAreaBeachSetup.Apply();
            foreach (var anchor in UnityEngine.Object.FindObjectsByType<BoatPartAnchor>(FindObjectsSortMode.None))
                if (anchor.GetComponent<BoatPartPresentation>() == null) anchor.gameObject.AddComponent<BoatPartPresentation>();
            for (var i = 0; i < 4; i++)
                GameObject.Find("Spawn_" + i).transform.position = new Vector3(-7.8f + 1.1f * i, 8.45f, -12.8f);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("P3_REPAIR_SETUP_READY");
        }

        private static GameObject BuildHuman(bool firstPerson, RuntimeAnimatorController controller)
        {
            var root = new GameObject(firstPerson ? "CoastalArms" : "CoastalDiver");
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanSource), root.transform);
            model.name = "Human";
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var pose = root.AddComponent<CoastalDiverPose>();
            pose.Model = model.transform; pose.FirstPerson = firstPerson;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.updateWhenOffscreen = true;
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = HumanMaterial(materials[i].name);
                renderer.sharedMaterials = materials;
                if (!firstPerson) continue;
                if (renderer.name != "Beach_Body") { UnityEngine.Object.DestroyImmediate(renderer); continue; }
                var meshPath = Root + "/CoastalArmsMesh.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null)
                {
                    mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                    mesh.name = "CoastalArmsMesh";
                    var armIndices = new HashSet<int>();
                    for (var i = 0; i < renderer.bones.Length; i++)
                    {
                        var name = renderer.bones[i].name;
                        if (new[] { "Arm", "Hand", "Index", "Middle", "Ring", "Pinky", "Thumb" }.Any(name.Contains)) armIndices.Add(i);
                    }
                    var weights = mesh.boneWeights;
                    Func<int, bool> arm = v =>
                        (armIndices.Contains(weights[v].boneIndex0) ? weights[v].weight0 : 0) +
                        (armIndices.Contains(weights[v].boneIndex1) ? weights[v].weight1 : 0) +
                        (armIndices.Contains(weights[v].boneIndex2) ? weights[v].weight2 : 0) +
                        (armIndices.Contains(weights[v].boneIndex3) ? weights[v].weight3 : 0) > 0.65f;
                    for (var s = 0; s < mesh.subMeshCount; s++)
                    {
                        var original = mesh.GetTriangles(s); var kept = new List<int>();
                        for (var i = 0; i < original.Length; i += 3)
                            if (arm(original[i]) && arm(original[i + 1]) && arm(original[i + 2]))
                                kept.AddRange(new[] { original[i], original[i + 1], original[i + 2] });
                        mesh.SetTriangles(kept, s);
                    }
                    AssetDatabase.CreateAsset(mesh, meshPath);
                }
                renderer.sharedMesh = mesh;
            }
            if (!firstPerson)
            {
                var hand = model.GetComponentsInChildren<Transform>().First(t => t.name == "Hand.R");
                var socket = new GameObject("Socket_RightHand_Equipment").transform;
                socket.SetParent(hand, false); socket.rotation = Quaternion.identity;
                var spine = model.GetComponentsInChildren<Transform>().First(t => t.name == "Chest");
                Part(spine, "AirTank", PrimitiveType.Capsule, new Vector3(0, 0, -0.22f), new Vector3(0.19f, 0.32f, 0.19f), HumanMaterial("Tank"));
            }
            var saved = PrefabUtility.SaveAsPrefabAsset(root, Root + (firstPerson ? "/CoastalArms.prefab" : "/CoastalDiver.prefab"));
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        public static Material HumanMaterial(string name)
        {
            var path = Root + "/CoastalMaterials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var color = name == "Skin" ? new Color(0.65f, 0.37f, 0.22f) :
                name == "Eye" || name == "White" ? new Color(0.92f, 0.94f, 0.88f) :
                name == "LightBrown" ? new Color(0.055f, 0.25f, 0.29f) :
                name == "Tank" ? new Color(0.93f, 0.57f, 0.12f) :
                name == "Earrings" ? new Color(0.78f, 0.60f, 0.28f) : new Color(0.035f, 0.06f, 0.08f);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            // Source bones have their own axes; position these authored props in model space.
            go.transform.position = parent.position + position;
            go.transform.rotation = Quaternion.identity; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        public static void InspectAndApplyCoast()
        {
            DiveTestAreaBeachSetup.Apply();
            var importer = (ModelImporter)AssetImporter.GetAtPath(HumanSource);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
            var report = new StringBuilder();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(HumanSource))
                report.AppendLine(asset.GetType().Name + " " + asset.name);
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanSource));
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                report.AppendLine("Bone " + t.name + " position=" + t.position);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                report.AppendLine("Renderer " + renderer.name + " bounds=" + renderer.bounds + " materials=" +
                    string.Join(",", renderer.sharedMaterials.Select(x => x != null ? x.name : "null")));
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/coastal-human-import.txt", report.ToString());
            UnityEngine.Object.DestroyImmediate(instance);
            Debug.Log("P3_REPAIR_COAST_READY");
        }
    }
}
