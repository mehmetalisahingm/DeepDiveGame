using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DeepDive.Editor
{
    // Issue #54 (P3.1-C): configures the Mixamo-sourced rig/clips into a small, usable package
    // for Mehmet's NetworkDiver prefab integration - docs/plan/CONTRACTS.md "Insan ve elde
    // ekipman sunumu - P3 taslak". Idempotent: safe to re-run after re-downloading source FBX.
    //
    // Does not touch player movement/netcode authority (out of scope per issue #54); only
    // produces the visual rig, animation clips, a minimal LocomotionMode-driven Animator
    // Controller, and simple placeholder equipment props with named hand-attachment sockets.
    public static class DiverCharacterSetup
    {
        private const string CharacterRoot = "Assets/DeepDive/Player/Character";
        private const string BaseFbx = CharacterRoot + "/DiverBase.fbx";
        private const string AnimDir = CharacterRoot + "/Animations";
        private const string ControllerPath = CharacterRoot + "/Animator/DiverLocomotion.controller";
        private const string EquipmentDir = CharacterRoot + "/Equipment";
        private const string PrefabPath = CharacterRoot + "/DiverCharacter.prefab";

        [MenuItem("DeepDive/P3.1-C/Setup Diver Character")]
        public static void Apply()
        {
            var baseAvatar = ConfigureBaseCharacter();
            RenameAndLoop(BaseFbx, "mixamo.com", "Dive_SwimUnderwater", true);

            ConfigureAnimationOnly(AnimDir + "/Walk.fbx", baseAvatar);
            RenameAndLoop(AnimDir + "/Walk.fbx", "mixamo.com", "Dive_Walk", true);

            ConfigureAnimationOnly(AnimDir + "/SwimSurface.fbx", baseAvatar);
            RenameAndLoop(AnimDir + "/SwimSurface.fbx", "mixamo.com", "Dive_SwimSurface", true);

            ConfigureAnimationOnly(AnimDir + "/SwimIdle.fbx", baseAvatar);
            RenameAndLoop(AnimDir + "/SwimIdle.fbx", "mixamo.com", "Dive_TreadWater", true);

            ConfigureAnimationOnly(AnimDir + "/Idle.fbx", baseAvatar);
            RenameAndLoop(AnimDir + "/Idle.fbx", "mixamo.com", "Dive_Idle", true);

            var controller = BuildController(baseAvatar);
            var characterPrefab = BuildCharacterPrefab(baseAvatar, controller);
            BuildEquipmentProps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"P3_DIVER_CHARACTER_SETUP_SUCCEEDED prefab={characterPrefab}");
        }

        private static Avatar ConfigureBaseCharacter()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(BaseFbx);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(BaseFbx) is { } assets
                ? System.Array.Find(assets, a => a is Avatar) as Avatar
                : null;
        }

        private static void ConfigureAnimationOnly(string path, Avatar sourceAvatar)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.SaveAndReimport();
        }

        // Mixamo bakes every download to a single clip literally named "mixamo.com"; this
        // renames it to something meaningful and marks it as a loop (all five clips are cycles,
        // not one-shots).
        private static void RenameAndLoop(string path, string fromClipName, string toClipName, bool loop)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.Length == 0) return;
            var clip = clips[0];
            if (clip.name == fromClipName || importer.clipAnimations.Length == 0) clip.name = toClipName;
            var settings = clip.loopTime;
            clip.loopTime = loop;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController BuildController(Avatar avatar)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath)!);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("LocomotionMode", AnimatorControllerParameterType.Int);

            var sm = controller.layers[0].stateMachine;
            var idle = sm.AddState("Land_Idle");
            idle.motion = LoadClip(AnimDir + "/Idle.fbx", "Dive_Idle");
            sm.defaultState = idle;

            var walk = sm.AddState("Land_Walk");
            walk.motion = LoadClip(AnimDir + "/Walk.fbx", "Dive_Walk");

            var surface = sm.AddState("Water_Surface");
            surface.motion = LoadClip(AnimDir + "/SwimSurface.fbx", "Dive_SwimSurface");

            var tread = sm.AddState("Water_Tread");
            tread.motion = LoadClip(AnimDir + "/SwimIdle.fbx", "Dive_TreadWater");

            var underwater = sm.AddState("Water_Underwater");
            underwater.motion = LoadClip(BaseFbx, "Dive_SwimUnderwater");

            // docs/plan/CONTRACTS.md LocomotionMode: kara(0)/su ustu(1)/sualti(2)/oturmus(3)/pasif(4).
            // 0/1 land states pick Idle vs Walk by whether Walk clip is playing already; a real
            // speed parameter is Mehmet's to add once this is wired to actual movement (this
            // controller is a starting point, not the final blend logic).
            AddDirectTransition(sm, idle, walk, "LocomotionMode", 0);
            AddDirectTransition(sm, walk, idle, "LocomotionMode", 0);
            AddDirectTransition(sm, idle, surface, "LocomotionMode", 1);
            AddDirectTransition(sm, surface, tread, "LocomotionMode", 1);
            AddDirectTransition(sm, idle, underwater, "LocomotionMode", 2);
            AddDirectTransition(sm, surface, underwater, "LocomotionMode", 2);
            AddDirectTransition(sm, underwater, surface, "LocomotionMode", 1);
            AddDirectTransition(sm, underwater, idle, "LocomotionMode", 0);
            AddDirectTransition(sm, tread, idle, "LocomotionMode", 0);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddDirectTransition(AnimatorStateMachine sm, AnimatorState from, AnimatorState to,
            string param, int value)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.25f;
            transition.AddCondition(AnimatorConditionMode.Equals, value, param);
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (asset is AnimationClip clip && clip.name == clipName) return clip;
            return null;
        }

        private static string BuildCharacterPrefab(Avatar avatar, AnimatorController controller)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseFbx);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; // CONTRACTS: kok hareket ag oyuncusunu ikinci kez oynatmaz.

            AddSocket(animator, HumanBodyBones.RightHand, "Socket_RightHand_Equipment", new Vector3(0.02f, 0.04f, 0.06f));
            AddSocket(animator, HumanBodyBones.Spine, "Socket_Back_Equipment", new Vector3(0f, 0.1f, -0.08f));

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            return AssetDatabase.GetAssetPath(prefab);
        }

        private static void AddSocket(Animator animator, HumanBodyBones bone, string socketName, Vector3 localOffset)
        {
            var boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null) return;
            var existing = boneTransform.Find(socketName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var socket = new GameObject(socketName).transform;
            socket.SetParent(boneTransform, false);
            socket.localPosition = localOffset;
            socket.localRotation = Quaternion.identity;
        }

        // Simple CC0-safe placeholder geometry (primitives), not sourced art: this satisfies
        // issue #54's "yerel el/kol ve uzak tam vucut kullanimina uygun baglanti noktalarini
        // netlestir" without a "kapsamli" art pass, which is explicitly out of scope here.
        // GripPoint on each marks where it aligns to Socket_RightHand_Equipment.
        private static void BuildEquipmentProps()
        {
            Directory.CreateDirectory(EquipmentDir);
            BuildCameraProp();
            BuildHarpoonProp();
        }

        private static void BuildCameraProp()
        {
            var root = new GameObject("CameraProp");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.08f, 0.07f, 0.14f);
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());

            var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Lens";
            lens.transform.SetParent(root.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lens.transform.localScale = new Vector3(0.035f, 0.03f, 0.035f);
            Object.DestroyImmediate(lens.GetComponent<CapsuleCollider>());

            var grip = new GameObject("GripPoint").transform;
            grip.SetParent(root.transform, false);
            grip.localPosition = new Vector3(0f, -0.05f, -0.02f);

            Directory.CreateDirectory(EquipmentDir);
            PrefabUtility.SaveAsPrefabAsset(root, EquipmentDir + "/CameraProp.prefab");
            Object.DestroyImmediate(root);
        }

        private static void BuildHarpoonProp()
        {
            var root = new GameObject("HarpoonProp");
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localScale = new Vector3(0.02f, 0.55f, 0.02f);
            Object.DestroyImmediate(shaft.GetComponent<CapsuleCollider>());

            var tip = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tip.name = "Tip";
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            tip.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
            Object.DestroyImmediate(tip.GetComponent<CapsuleCollider>());

            var grip = new GameObject("GripPoint").transform;
            grip.SetParent(root.transform, false);
            grip.localPosition = new Vector3(0f, -0.35f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, EquipmentDir + "/HarpoonProp.prefab");
            Object.DestroyImmediate(root);
        }
    }
}
