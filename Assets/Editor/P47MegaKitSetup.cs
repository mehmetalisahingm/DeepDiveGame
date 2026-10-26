using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeepDive.EditorTools
{
    // #171 P4.7-C: rebuilds the Quaternius Medieval Village MegaKit (Standard, CC0) setup from the source FBX/PNG files:
    // URP materials, importer material remaps and the sample prefabs. Re-runnable; nothing here touches a scene.
    public static class P47MegaKitSetup
    {
        public const string Root = "Assets/DeepDive/Art/MegaKit";
        private const string Models = Root + "/Models", Textures = Root + "/Textures", Materials = Root + "/Materials", Prefabs = Root + "/Prefabs";

        private struct Spec { public string Name, Base, Normal; public float Smooth, Metal; public bool Clip, Glass; public Color Tint; }

        private static readonly Spec[] Specs =
        {
            new Spec { Name = "MI_Plaster", Base = "T_Plaster_BaseColor", Normal = "T_Plaster_Normal", Smooth = .12f, Tint = Color.white },
            new Spec { Name = "MI_WoodTrim", Base = "T_WoodTrim_BaseColor", Normal = "T_WoodTrim_Normal", Smooth = .22f, Tint = Color.white },
            new Spec { Name = "MI_WoodTrim_Wear", Base = "T_WoodTrim_BaseColor", Normal = "T_WoodTrim_Normal", Smooth = .12f, Tint = new Color(.82f, .78f, .72f) },
            new Spec { Name = "MI_RockTrim", Base = "T_RockTrim_BaseColor", Normal = "T_RockTrim_Normal", Smooth = .15f, Tint = Color.white },
            new Spec { Name = "MI_Brick", Base = "T_Brick_BaseColor", Normal = "T_Brick_Normal", Smooth = .12f, Tint = Color.white },
            new Spec { Name = "MI_UnevenBrick", Base = "T_UnevenBrick_BaseColor", Normal = "T_UnevenBrick_Normal", Smooth = .12f, Tint = Color.white },
            new Spec { Name = "MI_RedBrick", Base = "T_RedBrick_BaseColor", Normal = null, Smooth = .12f, Tint = Color.white },
            new Spec { Name = "MI_RoundTiles", Base = "T_RoundTiles_BaseColor", Normal = "T_RoundTiles_Normal", Smooth = .3f, Tint = Color.white },
            // The Standard pack ships no ornament texture; a flat dark iron colour stands in (no missing-texture look).
            new Spec { Name = "MI_MetalOrnaments", Base = null, Normal = null, Smooth = .45f, Metal = .7f, Tint = new Color(.2f, .2f, .22f) },
            new Spec { Name = "MI_WindowGlass", Base = null, Normal = null, Smooth = .9f, Glass = true, Tint = new Color(.7f, .85f, .9f, .22f) },
            // Equipment-service accent (palette: blue #3F6FB5) for the shop sign; the kit itself ships no sign board.
            new Spec { Name = "Shop_Accent_Equipment", Base = null, Normal = null, Smooth = .2f, Tint = new Color(.247f, .435f, .71f) },
            new Spec { Name = "MI_Vine", Base = "T_VineLeaf", Normal = null, Smooth = .1f, Clip = true, Tint = Color.white },
        };

        [MenuItem("DeepDive/P4.7/Rebuild MegaKit (materials, remaps, prefabs)")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Materials); Directory.CreateDirectory(Prefabs);
            ConfigureTextures();
            var mats = Specs.ToDictionary(s => s.Name, MakeMaterial);
            RemapModels(mats);
            MegaKitSamples.Build(Prefabs);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("P47_MEGAKIT_DONE models=" + Directory.GetFiles(Models, "*.fbx").Length + " materials=" + mats.Count);
        }

        public static void BuildBatch() => Build();

        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Textures, "*.png").Select(p => p.Replace('\\', '/')))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var changed = false;
                if (name.EndsWith("_Normal") && importer.textureType != TextureImporterType.NormalMap) { importer.textureType = TextureImporterType.NormalMap; changed = true; }
                if (name.EndsWith("_Roughness") && importer.sRGBTexture) { importer.sRGBTexture = false; changed = true; }
                if (name == "T_VineLeaf" && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static Material MakeMaterial(Spec s)
        {
            var path = Materials + "/" + s.Name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", s.Tint);
            material.SetFloat("_Smoothness", s.Smooth); material.SetFloat("_Metallic", s.Metal);
            if (s.Base != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + s.Base + ".png"));
            if (s.Normal != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/" + s.Normal + ".png"));
                material.EnableKeyword("_NORMALMAP");
            }
            if (s.Clip) { material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .5f); material.EnableKeyword("_ALPHATEST_ON"); material.SetFloat("_Cull", 0); }
            if (s.Glass)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemapModels(Dictionary<string, Material> mats)
        {
            foreach (var path in Directory.GetFiles(Models, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var changed = false;
                foreach (var id in importer.GetExternalObjectMap().Keys.ToList()) { importer.RemoveRemap(id); changed = true; }
                // With "materials in prefab" the embedded materials are not listed as loadable sub-assets, but the renderers expose their names.
                var names = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>()
                    .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().ToArray();
                foreach (var name in names)
                {
                    if (!mats.TryGetValue(name, out var target)) { Debug.LogError("P47_UNMAPPED " + path + " material=" + name); continue; }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), target); changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }
    }
}
