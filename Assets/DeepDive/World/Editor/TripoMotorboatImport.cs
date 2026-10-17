using System;
using System.Linq;
using DeepDive.Network;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.World.Editor
{
    public static class TripoMotorboatImport
    {
        public const string Folder = "Assets/DeepDive/Network/Resources/BoatArt/TripoMotorboat";
        private const string Source = Folder + "/tripo_convert_7888c55b-2cb4-455c-804a-c9ae75a50e4e.fbx";
        private const string Textures = Folder + "/tripo_convert_7888c55b-2cb4-455c-804a-c9ae75a50e4e.fbm/luxury_motorboat_3d_model_";

        [MenuItem("DeepDive/P4/Import supplied Tripo motorboat")]
        public static void Apply()
        {
            var importer = AssetImporter.GetAtPath(Source) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Supplied Tripo FBX is missing");
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            ConfigureTexture(Textures + "basecolor.JPEG", false);
            ConfigureTexture(Textures + "normal.PNG", true);
            var path = Folder + "/Motorboat.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "basecolor.JPEG"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "normal.PNG"));
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Smoothness", 0.4f);
            EditorUtility.SetDirty(material);
            var root = new GameObject("MotorboatVisual");
            try
            {
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
                mesh.transform.SetParent(root.transform, false);
                var renderers = mesh.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                }
                var vertices = mesh.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount);
                Debug.Log($"TRIPO_SOURCE bounds={bounds} vertices={vertices}");
                // Tripo exports are centred/unitless. Long axis must face approved boat +Z.
                if (bounds.size.x > bounds.size.z)
                {
                    mesh.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                }
                BoatHullSeatRules.TryGetDimensions(BoatHullKind.Motorboat, out var size);
                var scale = Mathf.Min(size.Length / bounds.size.z, size.Width / bounds.size.x);
                mesh.transform.localScale *= scale;
                // Waterline sits slightly above the keel; deck alignment is visually reviewed.
                mesh.transform.localPosition = new Vector3(-bounds.center.x * scale,
                    -bounds.min.y * scale - 0.45f, -bounds.center.z * scale);
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/MotorboatVisual.prefab");
                Debug.Log($"TRIPO_IMPORTED scale={scale} footprint={bounds.size * scale}");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void ConfigureTexture(string path, bool normal)
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(path);
            texture.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            texture.sRGBTexture = !normal;
            texture.maxTextureSize = 2048;
            texture.SaveAndReimport();
        }

        public static void Prepare()
        {
            CoastalFishPopulationSetup.Apply();
            Apply();
        }
    }
}
