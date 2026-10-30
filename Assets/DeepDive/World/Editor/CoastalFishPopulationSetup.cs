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
            Debug.Log("P4_COAST_POPULATION authored=15 sea_bass=shallow red_snapper=reef tuna=reef-edge");
        }

        // Homes by habitat (#155): SuppliedKitImport gives names in order 5 sea bass, 5 tuna, 5 red snapper, so
        // index 1-4 are the coast's sea bass (with the original Fish_SeaBass at z 8), 5-9 tuna and 10-14 red
        // snapper. Every 6 m horizontal / 3 m vertical wander envelope (+0.45 m body) stays in the water and off
        // the scenery (P4WadeUndersideTests); depth bands are shallow y 0..8, reef -12..0, deep below -12.
        // - Sea bass: the coast row at z 2.5, 0.6 m clear of the closed wade (docs/reports/P4-WADE-UNDERSIDE.md).
        // - Red snapper: over the reef shelf (top y -8, z 19..40) at y -4, east of and 6.45 m off every reef head
        //   (tops y -3) and far enough north that the drop-off (z 15..19) stays under the envelope.
        // - Tuna: open water past the reef edge at y -8.5..-9, over the deep basin, west of the boss arena
        //   (7.5, -18, 57.5) and inside the side walls.
        // No home stands in an anchorage's water column (the research vessel's 4.4 x 10 m footprint round
        // anchor-reef-1 (3, 30) and anchor-deep-1 (0, 56); DiveTestAreaReefDeepSceneTests).
        public static Vector3 Home(int index)
        {
            if (index < 5) return new Vector3(-8f + (index - 1) * (16f / 6f), 3.5f, 2.5f);
            if (index < 10) return TunaHomes[index - 5];
            return SnapperHomes[index - 10];
        }

        private static readonly Vector3[] TunaHomes =
        {
            new Vector3(-7f, -8.5f, 48f), new Vector3(-4f, -8.5f, 50f), new Vector3(-1f, -8.5f, 48f),
            new Vector3(-6f, -9f, 52f), new Vector3(-6f, -9f, 56f)
        };

        private static readonly Vector3[] SnapperHomes =
        {
            new Vector3(7f, -4f, 30.5f), new Vector3(8f, -4f, 35f), new Vector3(6f, -4f, 34f),
            new Vector3(3.5f, -4f, 38f), new Vector3(7f, -4f, 38f)
        };
    }
}
