using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.1's physical home rig: primitive beds/storage (and, in DiveTestArea, the PC) until the final home
    // art replaces them. State remains in DayEngine/storage; these objects are only physical targets.
    // #138 PR-2: in DiveTestArea every target stands on its WorldAnchor (position AND rotation) and a missing
    // anchor builds nothing. PrepArea keeps its own legacy rig at the origin until PR-3 removes that scene.
    public static class HomeSceneSetup
    {
        private const string LegacyHomeSceneName = "PrepArea";
        public const string RootName = "P4_HomeInteractionRig";
        public const string AnchorsMissingCode = "P46_HOME_ANCHORS_MISSING";

        private static readonly Vector3 BedScale = new Vector3(1.25f, 0.35f, 2.05f);
        private static readonly Vector3 StorageScale = new Vector3(1.35f, 1.6f, 0.75f);

        // PrepArea's own values, local to the origin. Deliberately NOT ContinuousTownLayout.HomeRig: that one
        // moved for the #138 bed corridor and PrepArea must not. Deleted together with PrepArea in PR-3.
        private static class LegacyPrepArea
        {
            public static readonly float[] BedXs = { -3f, -1f, 1f, 3f };
            public const float BedY = 0.3f;
            public const float BedZ = 3.45f;
            public static readonly Vector3 Storage = new Vector3(-3.9f, 0.8f, -3.55f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LegacyHomeSceneName && scene.name != ContinuousTownLayout.SceneName) return;
            EnsureRig(scene);
        }

        public static void EnsureRig() => EnsureRig(SceneManager.GetActiveScene());

        // Builds into the given scene, never into whichever scene happens to be active.
        public static void EnsureRig(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            if (scene.name == ContinuousTownLayout.SceneName) EnsureAnchoredRig(scene);
            else EnsureLegacyRig(scene);
        }

        private static void EnsureAnchoredRig(Scene scene)
        {
            // sceneLoaded order is undefined, so the town (and its anchors) is built here first. Idempotent.
            ContinuousTownLayout.Build(scene);
            if (FindRoot(scene, RootName) != null) return;

            if (!HomeRigAnchors.TryResolve(scene, out var poses, out var missing))
            {
                Debug.LogError($"{AnchorsMissingCode} missing={string.Join(",", missing)} scene={scene.name}");
                return;
            }

            var root = NewRoot(scene);
            for (var i = 0; i < DayIds.BedCount; i++)
                Bed(root, i, poses.Beds[i].position, poses.Beds[i].rotation);
            Storage(root, poses.Storage.position, poses.Storage.rotation);
            HomePcAnchor.EnsurePc(scene, poses.Pc);
            SuppliedTownArt.Home(root);
        }

        private static void EnsureLegacyRig(Scene scene)
        {
            if (FindRoot(scene, RootName) != null) return;
            var root = NewRoot(scene);
            for (var i = 0; i < DayIds.BedCount; i++)
                Bed(root, i, new Vector3(LegacyPrepArea.BedXs[i], LegacyPrepArea.BedY, LegacyPrepArea.BedZ),
                    Quaternion.Euler(0f, 180f, 0f));
            Storage(root, LegacyPrepArea.Storage, Quaternion.identity);
        }

        private static Transform NewRoot(Scene scene)
        {
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        private static void Bed(Transform root, int index, Vector3 position, Quaternion rotation)
        {
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "HomeBed_" + index;
            bed.transform.SetParent(root, false);
            bed.transform.SetPositionAndRotation(position, rotation);
            bed.transform.localScale = BedScale;
            var bedMaterial = Resources.Load<Material>("DevVisuals/Dev_Blue");
            if (bedMaterial != null) bed.GetComponent<Renderer>().sharedMaterial = bedMaterial;
            SuppliedTownArt.Attach(bed.transform, "Bed", new Vector3(0f, -0.5f, 0f), Quaternion.identity, true);
            var anchor = bed.AddComponent<HomeInteractionAnchor>();
            anchor.ConfigureBed(DayIds.Beds[index], new Vector3(0f, 0.7f, 0f), new Vector3(0f, 0.15f, 1.45f));
        }

        private static void Storage(Transform root, Vector3 position, Quaternion rotation)
        {
            var storage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storage.name = "HomeSharedStorage";
            storage.transform.SetParent(root, false);
            storage.transform.SetPositionAndRotation(position, rotation);
            storage.transform.localScale = StorageScale;
            var storageMaterial = Resources.Load<Material>("DevVisuals/Dev_Crate");
            if (storageMaterial != null) storage.GetComponent<Renderer>().sharedMaterial = storageMaterial;
            SuppliedTownArt.Attach(storage.transform, "Storage", new Vector3(0f, -0.5f, 0f), Quaternion.identity, true);
            storage.AddComponent<HomeInteractionAnchor>().ConfigureStorage();
        }

        internal static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }
    }

    // The six home anchors the rig stands on. All or nothing: a missing, duplicated or inactive anchor, or one
    // that lives in another scene, counts as missing so the rig never half-builds or borrows a foreign place.
    public static class HomeRigAnchors
    {
        public sealed class Poses
        {
            public readonly Pose[] Beds = new Pose[WorldAnchorIds.HomeBedCount];
            public Pose Storage;
            public Pose Pc;
        }

        public static bool TryResolve(Scene scene, out Poses poses, out List<string> missing)
        {
            poses = new Poses();
            missing = new List<string>();
            for (var i = 0; i < WorldAnchorIds.HomeBedCount; i++)
                if (TryPose(scene, WorldAnchorIds.HomeBed(i), missing, out var bed)) poses.Beds[i] = bed;
            if (TryPose(scene, WorldAnchorIds.HomeStorage, missing, out var storage)) poses.Storage = storage;
            if (TryPose(scene, WorldAnchorIds.HomePc, missing, out var pc)) poses.Pc = pc;
            if (missing.Count == 0) return true;
            poses = null;
            return false;
        }

        private static bool TryPose(Scene scene, string id, List<string> missing, out Pose pose)
        {
            pose = default;
            if (!WorldAnchors.TryFind(id, out var anchor) || anchor.gameObject.scene != scene)
            {
                missing.Add(id);
                return false;
            }
            pose = new Pose(anchor.transform.position, anchor.transform.rotation);
            return true;
        }
    }
}
