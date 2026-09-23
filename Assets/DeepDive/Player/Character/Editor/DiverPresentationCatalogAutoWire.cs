using UnityEditor;
using UnityEngine;

namespace DeepDive.Editor
{
    // Mixamo source FBX files are intentionally not committed to the public repository.
    // Keep the runtime catalog valid on a fresh checkout, then wire the human rig as soon
    // as a developer imports the licensed source files locally.
    public sealed class DiverPresentationCatalogAutoWire : AssetPostprocessor
    {
        private const string CharacterRoot = "Assets/DeepDive/Player/Character";
        private const string BaseFbx = CharacterRoot + "/DiverBase.fbx";
        private const string DiverPrefab = CharacterRoot + "/DiverCharacter.prefab";
        private const string HarpoonPrefab = CharacterRoot + "/Equipment/HarpoonProp.prefab";
        private const string CameraPrefab = CharacterRoot + "/Equipment/CameraProp.prefab";
        private const string CatalogPath = "Assets/DeepDive/Network/Resources/DiverPresentationCatalog.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleInitialSync()
        {
            EditorApplication.delayCall += Sync;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (TouchesCharacterAssets(importedAssets) || TouchesCharacterAssets(deletedAssets) ||
                TouchesCharacterAssets(movedAssets) || TouchesCharacterAssets(movedFromAssetPaths))
            {
                EditorApplication.delayCall += Sync;
            }
        }

        private static bool TouchesCharacterAssets(string[] paths)
        {
            if (paths == null) return false;
            foreach (var path in paths)
                if (!string.IsNullOrEmpty(path) && path.StartsWith(CharacterRoot)) return true;
            return false;
        }

        private static void Sync()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CatalogPath);
            if (catalog == null) return;

            var serialized = new SerializedObject(catalog);
            // The shared build always uses the redistributable CC0 rig. A developer's
            // optional Mixamo import must not silently change the shipped character.
            SetReference(serialized, "thirdPersonRigPrefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(CharacterRoot + "/CoastalDiver.prefab"));
            SetReference(serialized, "harpoonPropPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(HarpoonPrefab));
            SetReference(serialized, "cameraPropPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefab));

            if (!serialized.ApplyModifiedPropertiesWithoutUndo()) return;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void SetReference(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }
    }
}
