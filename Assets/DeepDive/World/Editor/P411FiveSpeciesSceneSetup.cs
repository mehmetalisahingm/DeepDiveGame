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
        public const int Population = P411FiveSpeciesRoster.Population;
        public static readonly string[] SpeciesIds = P411FiveSpeciesRoster.SpeciesIds;
        public static readonly Vector3[] Homes = P411FiveSpeciesRoster.Homes;
        private static readonly string[] AssetNames =
            { "SeaBass", "Tuna", "RedSnapper", "Grouper", "Lanternfish" };
        private static readonly string[] VisualNames = P411FiveSpeciesRoster.VisualNames;

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
            // Validate every authored fish before generating any new art assets.
            // Never infer slot order from an incomplete, duplicated or renamed roster.
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var actors = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<FishActor>(true)).ToArray();
            if (actors.Length != Population)
                throw new InvalidDataException("P411_FISH_SCENE_COUNT_INVALID expected=" +
                    Population + " actual=" + actors.Length);
            var fish = new FishActor[Population];
            for (var i = 0; i < Population; i++)
            {
                var name = i == 0 ? DiveTestAreaSetup.FishName : $"Fish_SeaBass_{i + 1:00}";
                var matches = actors.Where(actor => actor.name == name).ToArray();
                if (matches.Length != 1)
                    throw new InvalidDataException("P411_FISH_SCENE_SLOT_INVALID name=" +
                        name + " count=" + matches.Length);
                var actor = matches[0];
                // Removing all old visual children must not remove a nested NGO identity.
                if (actor.GetComponent<NetworkObject>() == null ||
                    actor.GetComponentsInChildren<NetworkObject>(true).Length != 1)
                    throw new InvalidDataException("P411_FISH_NETWORK_HIERARCHY_INVALID name=" + name);
                fish[i] = actor;
            }

            SuppliedKitImport.BuildFiveSpeciesArt();

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

    }
}
