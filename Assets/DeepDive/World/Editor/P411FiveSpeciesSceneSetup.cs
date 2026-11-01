using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.World.Editor
{
    // #218/#155: an explicit Unity EDITOR authoring step, not a runtime network spawn.
    // Reuses the 15 authored in-scene NetworkObjects and their stable scene identities.
    // Never writes a half-populated five-species scene if a model/asset is missing.
    public static class P411FiveSpeciesSceneSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const int Population = 15;

        public static readonly string[] SpeciesIds =
        {
            "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish"
        };

        private static readonly string[] AssetNames =
        {
            "SeaBass", "Tuna", "RedSnapper", "Grouper", "Lanternfish"
        };

        private static readonly string[] VisualNames =
        {
            "SeaBass", "Tuna", "RedSnapper", "Grouper", "Lanternfish"
        };

        // Three individuals of each species. Two different species are accessible
        // from the early shallow coast, two inhabit the reef and one the deep basin.
        // The existing 15 NGO scene identities survive; no extra prefab registrations.
        public static readonly Vector3[] Homes =
        {
            // Shallow / the original game-day hunting approach.
            new Vector3(0f, 4f, 8f),
            new Vector3(-8f, 3.5f, 2.5f),
            new Vector3(-5.3333f, 3.5f, 2.5f),
            // Tuna / open shallows.
            new Vector3(-5f, 3.5f, 10f),
            new Vector3(0f, 3.5f, 11f),
            new Vector3(5f, 3.5f, 10f),
            // Red snapper / eastern reef shelf.
            new Vector3(7f, -4f, 30.5f),
            new Vector3(8f, -4f, 35f),
            new Vector3(6f, -4f, 38f),
            // Grouper / western reef band, above reef floor.
            new Vector3(-10f, -4.5f, 28f),
            new Vector3(-10f, -4.5f, 33f),
            new Vector3(-10f, -4.5f, 38f),
            // Lanternfish / deep open-water column, west of boss & boat.
            new Vector3(-8f, -18f, 49f),
            new Vector3(-9f, -18f, 56f),
            new Vector3(-8f, -18f, 62f)
        };

        public static string SpeciesForSlot(int index) => SpeciesIds[index / 3];
        public static string VisualForSlot(int index) => VisualNames[index / 3];

        [MenuItem("DeepDive/P4.11/Author 5 animated fish species (15 NGO scene fish)")]
        public static void Apply()
        {
            // Fail before touching the shared scene or replacing any existing model.
            for (var i = 0; i < SpeciesIds.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(
                    "Assets/DeepDive/World/Fish/Species/" + AssetNames[i] + ".asset");
                if (definition == null || definition.SpeciesId != SpeciesIds[i] || !definition.IsValid(out _))
                    throw new InvalidDataException("P411_SPECIES_INVALID: " + SpeciesIds[i]);
            }
            foreach (var source in new[] { "Humphead", "CoralGrouper", "Anglerfish" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/DeepDive/Art/Supplied/Fish/" + source + ".fbx") == null)
                    throw new FileNotFoundException("P411_FISH_SOURCE_MISSING: " + source);
            SuppliedKitImport.BuildFiveSpeciesArt();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var fish = scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<FishActor>(true))
                .OrderBy(actor => actor.name == DiveTestAreaSetup.FishName ? 0 :
                    ParseFishIndex(actor.name)).ToArray();
            if (fish.Length != Population || fish[0].name != DiveTestAreaSetup.FishName)
                throw new InvalidDataException("P411_FISH_SCENE_COUNT_OR_ROOT_INVALID");

            var definitions = AssetNames.Select(name => AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(
                "Assets/DeepDive/World/Fish/Species/" + name + ".asset")).ToArray();
            var visuals = VisualNames.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/DeepDive/Art/Resources/SuppliedKit/" + name + ".prefab")).ToArray();
            if (visuals.Any(x => x == null))
                throw new InvalidDataException("P411_FISH_PREFAB_MISSING");

            for (var i = 0; i < fish.Length; i++)
            {
                var actor = fish[i];
                var descriptor = new SerializedObject(actor);
                descriptor.FindProperty("species").objectReferenceValue = definitions[i / 3];
                descriptor.ApplyModifiedPropertiesWithoutUndo();
                for (var child = actor.transform.childCount - 1; child >= 0; child--)
                    Object.DestroyImmediate(actor.transform.GetChild(child).gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(visuals[i / 3], scene);
                model.transform.SetParent(actor.transform, false);
                actor.transform.SetPositionAndRotation(Homes[i], Quaternion.Euler(0f, i * 43f, 0f));
                EditorUtility.SetDirty(actor);
                EditorUtility.SetDirty(actor.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("P411_FISH_SCENE_SAVE_FAILED");

            // Match the existing CoastalFishPopulationSetup NGO save/reopen path.
            // Unity OnValidate owns NetworkObject.GlobalObjectIdHash; never fake the hash.
            scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var networkObject in root.GetComponentsInChildren<NetworkObject>(true))
                    EditorUtility.SetDirty(networkObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("P411_FISH_NETWORK_IDS_SAVE_FAILED");
            AssetDatabase.SaveAssets();
            Debug.Log("P411_FIVE_SPECIES_SCENE_READY sea_bass=3 tuna=3 red_snapper=3 grouper=3 lanternfish=3");
        }

        private static int ParseFishIndex(string name)
        {
            const string prefix = "Fish_SeaBass_";
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !int.TryParse(name.Substring(prefix.Length), out var value))
                throw new InvalidDataException("P411_FISH_NAME_INVALID " + name);
            return value - 1;
        }
    }
}
