using System;
using System.IO;
using System.Linq;
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
            var cameraProp = BuildCamera();
            var catalog = new SerializedObject(AssetDatabase.LoadAssetAtPath<DiverPresentationCatalog>(
                "Assets/DeepDive/Network/Resources/DiverPresentationCatalog.asset"));
            catalog.FindProperty("thirdPersonRigPrefab").objectReferenceValue = body;
            catalog.FindProperty("firstPersonArmsPrefab").objectReferenceValue = arms;
            catalog.FindProperty("cameraPropPrefab").objectReferenceValue = cameraProp;
            catalog.ApplyModifiedPropertiesWithoutUndo();
            DiveTestAreaBeachSetup.Apply();
            foreach (var anchor in UnityEngine.Object.FindObjectsByType<BoatPartAnchor>(FindObjectsSortMode.None))
                if (anchor.GetComponent<BoatPartPresentation>() == null) anchor.gameObject.AddComponent<BoatPartPresentation>();
            if (UnityEngine.Object.FindFirstObjectByType<CoastalAtmosphere>() == null)
                new GameObject("CoastalAtmosphere").AddComponent<CoastalAtmosphere>();
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
                socket.localScale = InverseScale(hand.lossyScale);
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
                name == "Screen" ? new Color(0.08f, 0.65f, 0.70f) :
                name == "RecordingRed" ? new Color(1f, 0.05f, 0.02f) :
                name == "Earrings" ? new Color(0.78f, 0.60f, 0.28f) : new Color(0.035f, 0.06f, 0.08f);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject BuildCamera()
        {
            var root = new GameObject("CoastalCamera");
            Part(root.transform, "Housing", PrimitiveType.Cube, Vector3.zero, new Vector3(.26f, .16f, .14f), HumanMaterial("Hair"));
            Part(root.transform, "RearScreen", PrimitiveType.Cube, new Vector3(0, 0, -.075f), new Vector3(.19f, .11f, .01f), HumanMaterial("Screen"));
            foreach (var x in new[] { -.18f, .18f })
            {
                Part(root.transform, "Grip", PrimitiveType.Capsule, new Vector3(x, -.025f, 0), new Vector3(.035f, .07f, .035f), HumanMaterial("Tank"));
                Part(root.transform, "GripBracket", PrimitiveType.Cube, new Vector3(x * .85f, -.07f, 0), new Vector3(.075f, .025f, .055f), HumanMaterial("Hair"));
            }
            Part(root.transform, "Lens", PrimitiveType.Cylinder, new Vector3(0, 0, .115f), new Vector3(.11f, .045f, .11f), HumanMaterial("Eye"));
            root.transform.Find("Lens").localRotation = Quaternion.Euler(90, 0, 0);
            Part(root.transform, "LensGlass", PrimitiveType.Cylinder, new Vector3(0, 0, .164f), new Vector3(.087f, .007f, .087f), HumanMaterial("Screen"));
            root.transform.Find("LensGlass").localRotation = Quaternion.Euler(90, 0, 0);
            Part(root.transform, "Shutter", PrimitiveType.Sphere, new Vector3(.08f, .09f, 0), new Vector3(.035f, .02f, .035f), HumanMaterial("RecordingRed"));
            var grip = new GameObject("GripPoint").transform; grip.SetParent(root.transform, false); grip.localPosition = new Vector3(.18f, -.025f, 0);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, Root + "/CoastalCamera.prefab");
            UnityEngine.Object.DestroyImmediate(root); return saved;
        }
        private static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            // Source bones have their own axes; position these authored props in model space.
            go.transform.position = parent.position + position;
            go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.Scale(scale, InverseScale(parent.lossyScale));
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static Vector3 InverseScale(Vector3 scale) => new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);

    }
}
