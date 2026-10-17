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
    public static class SuppliedKitImport
    {
        private const string Output = "Assets/DeepDive/Art/Resources/SuppliedKit";
        public static void Apply()
        {
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Output + "/Materials");
            AssetDatabase.Refresh();
            Make("Fish/Boat", "Rowboat", new Vector3(2.4f, 1.3f, 5f), false, true);
            Make("Fish/CoralGrouper", "CoastalFish", new Vector3(0.3f, 0.5f, 1.05f), true);
            Make("Fish/Tuna", "Tuna", new Vector3(0.3f, 0.5f, 1.05f), true);
            Make("Fish/RedSnapper", "RedSnapper", new Vector3(0.3f, 0.5f, 1.05f), true);
            Make("Buildings/1Story_Sign_Mat", "ShopBuilding", new Vector3(4.3f, 4.5f, 4.3f));
            Make("Buildings/2Story_Balcony_Mat", "TownBuilding", new Vector3(7f, 5.2f, 8f));
            Make("Interior/Bed_Single", "Bed", new Vector3(1.25f, 0.6f, 2.05f));
            Make("Interior/Drawer_1", "Storage", new Vector3(1.35f, 1.6f, 0.75f));
            Make("Interior/Bookshelf", "Bookshelf", new Vector3(1.6f, 2f, 0.5f));
            Make("Interior/Couch_Medium1", "Couch", new Vector3(2.2f, 1.15f, 0.9f));
            Make("Interior/Houseplant_1", "Plant", new Vector3(0.55f, 1.2f, 0.55f));
            Make("Interior/Light_Desk", "DeskLight", new Vector3(0.3f, 0.5f, 0.3f));
            var scene = EditorSceneManager.OpenScene("Assets/DeepDive/World/Scenes/DiveTestArea.unity");
            var fishes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true)).OrderBy(f => f.name).ToArray();
            var tuna = Species("Tuna", "tuna", "Ton baligi");
            var snapper = Species("RedSnapper", "red_snapper", "Kirmizi mercan");
            for (var index = 0; index < fishes.Length; index++)
            {
                var fish = fishes[index];
                for (var c = fish.transform.childCount - 1; c >= 0; c--) Object.DestroyImmediate(fish.transform.GetChild(c).gameObject);
                var key = index < 5 ? "CoastalFish" : index < 10 ? "Tuna" : "RedSnapper";
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/" + key + ".prefab"));
                visual.transform.SetParent(fish.transform, false);
                if (index >= 5)
                {
                    var serialized = new SerializedObject(fish);
                    serialized.FindProperty("species").objectReferenceValue = index < 10 ? tuna : snapper;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(fish);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SUPPLIED_KIT_READY prefabs=12 fish=15 species=3");
        }

        private static SpeciesDefinition Species(string file, string id, string display)
        {
            var path = "Assets/DeepDive/World/Fish/Species/" + file + ".asset";
            var species = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(path);
            if (species == null)
            {
                species = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SpeciesDefinition>("Assets/DeepDive/World/Fish/Species/SeaBass.asset"));
                AssetDatabase.CreateAsset(species, path);
            }
            var serialized = new SerializedObject(species);
            serialized.FindProperty("speciesId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = display;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(species);
            return species;
        }

        private static void Make(string source, string name, Vector3 size, bool fish = false, bool boat = false)
        {
            var sourcePath = "Assets/DeepDive/Art/Supplied/" + source + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(sourcePath);
            importer.importCameras = importer.importLights = importer.addCollider = false;
            importer.importAnimation = fish;
            if (fish) importer.animationType = ModelImporterAnimationType.Legacy;
            importer.SaveAndReimport();
            var root = new GameObject(name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
                model.transform.SetParent(root.transform, false);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException(source + " has no visible mesh");
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Material(m, name)).ToArray();
                }
                var bounds = Bounds(renderers);
                if ((fish || boat) && bounds.size.x > bounds.size.z)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    bounds = Bounds(renderers);
                }
                var factor = fish ? size.z / bounds.size.z : Mathf.Min(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
                var scale = fish || boat ? Vector3.one * factor : new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
                model.transform.localScale = Vector3.Scale(model.transform.localScale, scale);
                model.transform.localPosition = new Vector3(-bounds.center.x * scale.x,
                    -(fish ? bounds.center.y : bounds.min.y) * scale.y - (boat ? 0.3f : 0f), -bounds.center.z * scale.z);
                if (fish)
                {
                    var animation = model.GetComponent<Animation>();
                    var clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
                    if (animation != null && clips.Length > 0)
                    {
                        var clip = clips.FirstOrDefault(c => c.name.IndexOf("swim", StringComparison.OrdinalIgnoreCase) >= 0) ?? clips[0];
                        animation.clip = clip;
                        animation.wrapMode = WrapMode.Loop;
                        animation.playAutomatically = true;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/" + name + ".prefab");
                Debug.Log($"SUPPLIED_MODEL {name} source={source} bounds={bounds.size} scale={scale}");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Bounds Bounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Material Material(Material source, string model)
        {
            var name = model + "_" + (source != null ? source.name : "default");
            var path = Output + "/Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(result, path);
            }
            var color = source != null && source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(result);
            return result;
        }
    }
}
