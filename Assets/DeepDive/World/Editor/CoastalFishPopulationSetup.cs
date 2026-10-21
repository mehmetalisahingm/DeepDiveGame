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
                // Two staggered rows, clear of the shoreline, ramp and walls. Their entire
                // 6m horizontal / 3m vertical wander envelope stays inside the water volume.
                // The first row stood at z -6, where that envelope reached into Beach_Wade,
                // Shore_Ramp and Beach_Platform and FishMotion (water box only) swam the fish
                // into them. At z 2.5 it stays 0.6 m clear of the closed wade (north edge
                // z -4.14) for every column (docs/reports/P4-WADE-UNDERSIDE.md).
                var column = (index - 1) % 7;
                var row = (index - 1) / 7;
                instance.transform.SetPositionAndRotation(new Vector3(-8f + column * (16f / 6f),
                    row == 0 ? 3.5f : 4.5f, row == 0 ? 2.5f : 3f), Quaternion.Euler(0f, index * 43f, 0f));
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
            Debug.Log("P4_COAST_POPULATION authored=15 species=sea_bass");
        }
    }
}
