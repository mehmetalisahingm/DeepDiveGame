using System;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.World.Editor
{
    // Authored scene fish: NGO generates a separate scene identity for every animal.
    // The existing host AI, harpoon/catch and next-dive lifecycle remain the only authorities.
    public static class CoastalFishPopulationSetup
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const int Count = 15;

        [MenuItem("DeepDive/P4/Coast: author 15 huntable fish")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var fish = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true)).ToArray();
            var original = fish.Single(f => f.name == DiveTestAreaSetup.FishName);
            // Preserve the original object and all non-population content on repeat runs.
            for (var index = 1; index < Count; index++)
            {
                var name = $"Fish_SeaBass_{index + 1:00}";
                var actor = fish.SingleOrDefault(f => f.name == name);
                var instance = actor != null ? actor.gameObject : Object.Instantiate(original.gameObject);
                instance.name = name;
                instance.transform.SetPositionAndRotation(Home(index), Quaternion.Euler(0f, index * 43f, 0f));
                EditorUtility.SetDirty(instance);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Fish population scene save failed");
            // Clones initially inherit the source hash. Only after their first save do they
            // have durable GlobalObjectIds. Reload lets NGO OnValidate generate their hashes;
            // save these component values too, rather than relying on a build-time repair.
            scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var networkObject in root.GetComponentsInChildren<NetworkObject>(true))
                    EditorUtility.SetDirty(networkObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Fish NGO identity save failed");
            AssetDatabase.SaveAssets();
            Debug.Log("P4_COAST_POPULATION authored=15 homes=P411FiveSpeciesRoster");
        }

        // One source of homes: this menu moves the fish of the five-species scene, it must not put them back on an
        // older layout. Why each home stands where it does is in P411FiveSpeciesRoster and P219SeabedTests.
        public static Vector3 Home(int index) => P411FiveSpeciesRoster.Homes[index];
    }
}
