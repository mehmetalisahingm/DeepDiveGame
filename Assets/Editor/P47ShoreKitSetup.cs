using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Composition;
using UnityEditor;
using UnityEngine;

namespace DeepDive.EditorTools
{
    // #171 P4.7-C: rebuilds the shore kit from the user-supplied Quaternius Pirate Kit (CC0): the atlas URP material, the importer
    // remaps and the metre-scale prefabs under Resources/ShoreKit. Re-runnable; nothing here touches a scene.
    public static class P47ShoreKitSetup
    {
        public const string Root = "Assets/DeepDive/Art/Supplied/PirateKit";
        public const string PrefabFolder = "Assets/DeepDive/Art/Resources/ShoreKit";
        private const string Models = Root + "/Models", AtlasPath = Root + "/Textures/Atlas_Pirate.png", MaterialPath = Root + "/Materials/MI_PirateAtlas.mat";

        private struct Piece { public string Id, Model; public ShoreKitKind Kind; public bool Walkable; }

        private static readonly Piece[] Pieces =
        {
            new Piece { Id = "shore-dock", Model = "Environment_Dock", Kind = ShoreKitKind.Dock, Walkable = true },
            new Piece { Id = "shore-dock-pole", Model = "Environment_Dock_Pole", Kind = ShoreKitKind.Dock },
            new Piece { Id = "shore-dock-broken", Model = "Environment_Dock_Broken", Kind = ShoreKitKind.Dock, Walkable = true },
            new Piece { Id = "shore-rock-1", Model = "Environment_Rock_1", Kind = ShoreKitKind.Rock },
            new Piece { Id = "shore-rock-2", Model = "Environment_Rock_2", Kind = ShoreKitKind.Rock },
            new Piece { Id = "shore-rock-3", Model = "Environment_Rock_3", Kind = ShoreKitKind.Rock },
            new Piece { Id = "shore-rock-4", Model = "Environment_Rock_4", Kind = ShoreKitKind.Rock },
            new Piece { Id = "shore-rock-5", Model = "Environment_Rock_5", Kind = ShoreKitKind.Rock },
            new Piece { Id = "shore-palm-1", Model = "Environment_PalmTree_1", Kind = ShoreKitKind.Tree },
            new Piece { Id = "shore-palm-2", Model = "Environment_PalmTree_2", Kind = ShoreKitKind.Tree },
            new Piece { Id = "shore-palm-3", Model = "Environment_PalmTree_3", Kind = ShoreKitKind.Tree },
            new Piece { Id = "shore-prop-barrel", Model = "Prop_Barrel", Kind = ShoreKitKind.Prop },
            new Piece { Id = "shore-prop-bucket", Model = "Prop_Bucket", Kind = ShoreKitKind.Prop },
            new Piece { Id = "shore-prop-bucket-fishes", Model = "Prop_Bucket_Fishes", Kind = ShoreKitKind.Prop },
            new Piece { Id = "shore-prop-anchor", Model = "Prop_Anchor", Kind = ShoreKitKind.Prop },
            new Piece { Id = "shore-prop-chest", Model = "Prop_Chest_Closed", Kind = ShoreKitKind.Prop }
        };

        [MenuItem("DeepDive/P4.7/Rebuild shore kit (material, remaps, prefabs)")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(PrefabFolder);
            ConfigureAtlas();
            var material = MakeMaterial();
            Remap(material);
            var report = new StringBuilder("id,model,tris,renderers,min_x,min_y,min_z,max_x,max_y,max_z,materials\n");
            foreach (var piece in Pieces) { report.AppendLine(Measure(piece)); BuildPrefab(piece); }
            File.WriteAllText("Logs/p47-shorekit-measure.csv", report.ToString());
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("P47_SHOREKIT_DONE models=" + Pieces.Length + "\n" + report);
        }

        public static void BuildBatch() => Build();

        private static void ConfigureAtlas()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            // Flat-colour palette: point sampling and no mips, so neighbouring swatches never bleed into each other.
            if (importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp) return;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static Material MakeMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .1f); material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Remap(Material atlas)
        {
            foreach (var piece in Pieces)
            {
                var path = Models + "/" + piece.Model + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var changed = false;
                foreach (var id in importer.GetExternalObjectMap().Keys.ToList()) { importer.RemoveRemap(id); changed = true; }
                var names = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>()
                    .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().ToArray();
                foreach (var name in names) { importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), atlas); changed = true; }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void BuildPrefab(Piece piece)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/" + piece.Model + ".fbx");
            var root = new GameObject(piece.Id);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
                model.name = "Model"; // the FBX root transform (Blender axis fix) stays as imported
                var renderers = root.GetComponentsInChildren<Renderer>();
                var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
                var data = root.AddComponent<ShoreKitPiece>();
                data.Id = piece.Id; data.Kind = piece.Kind; data.Walkable = piece.Walkable; data.Size = b.size;
                data.SurfaceY = piece.Walkable ? DeckY(model) : 0f;
                data.ColliderHint = piece.Kind == ShoreKitKind.Dock
                    ? (piece.Walkable ? new Vector3(b.size.x, .25f, b.size.z) : new Vector3(.4f, b.size.y, .4f))
                    : piece.Kind == ShoreKitKind.Tree ? new Vector3(.45f, b.size.y, .45f) : b.size;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + piece.Id + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        // Local y of the walking surface: the height carrying most of the upward-facing triangle area.
        private static float DeckY(GameObject model)
        {
            var filter = model.GetComponentInChildren<MeshFilter>();
            var mesh = filter.sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
            var area = new Dictionary<int, float>();
            for (var i = 0; i < t.Length; i += 3)
            {
                var a = filter.transform.TransformPoint(v[t[i]]); var b = filter.transform.TransformPoint(v[t[i + 1]]); var c = filter.transform.TransformPoint(v[t[i + 2]]);
                var n = Vector3.Cross(b - a, c - a);
                if (n.normalized.y < .95f) continue;
                var key = Mathf.RoundToInt(((a.y + b.y + c.y) / 3f) * 20f);
                area[key] = (area.TryGetValue(key, out var m) ? m : 0f) + n.magnitude * .5f;
            }
            return area.Count == 0 ? 0f : area.OrderByDescending(kv => kv.Value).First().Key / 20f;
        }

        private static string Measure(Piece piece)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/" + piece.Model + ".fbx");
            var instance = (GameObject)Object.Instantiate(asset);
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>();
                var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
                var tris = instance.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
                var mats = string.Join("|", renderers.SelectMany(r => r.sharedMaterials).Select(m => m != null ? m.name : "null").Distinct());
                string F(float v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                return $"{piece.Id},{piece.Model},{tris},{renderers.Length},{F(b.min.x)},{F(b.min.y)},{F(b.min.z)},{F(b.max.x)},{F(b.max.y)},{F(b.max.z)},{mats}";
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
