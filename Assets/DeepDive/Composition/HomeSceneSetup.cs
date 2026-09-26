using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.1's first physical home rig. PrepArea is already the shared walkable room used by the
    // integrated build, so these targets are authored deterministically into that room at runtime
    // until the final home art scene replaces the primitives. State remains in DayEngine/storage;
    // these objects are only physical targets.
    public static class HomeSceneSetup
    {
        private const string HomeSceneName = "PrepArea";
        private const string RootName = "P4_HomeInteractionRig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != HomeSceneName) return;
            EnsureRig();
        }

        public static void EnsureRig()
        {
            if (GameObject.Find(RootName) != null) return;

            var root = new GameObject(RootName);
            var bedXs = new[] { -3f, -1f, 1f, 3f };
            for (var i = 0; i < DayIds.BedCount; i++)
            {
                var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bed.name = "HomeBed_" + i;
                bed.transform.SetParent(root.transform, false);
                bed.transform.position = new Vector3(bedXs[i], 0.3f, 3.45f);
                bed.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                bed.transform.localScale = new Vector3(1.25f, 0.35f, 2.05f);
                var anchor = bed.AddComponent<HomeInteractionAnchor>();
                anchor.ConfigureBed(DayIds.Beds[i], new Vector3(0f, 0.7f, 0f), new Vector3(0f, 0.15f, 1.45f));
            }

            var storage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storage.name = "HomeSharedStorage";
            storage.transform.SetParent(root.transform, false);
            storage.transform.position = new Vector3(-3.9f, 0.8f, -3.55f);
            storage.transform.localScale = new Vector3(1.35f, 1.6f, 0.75f);
            storage.AddComponent<HomeInteractionAnchor>().ConfigureStorage();
        }
    }
}
