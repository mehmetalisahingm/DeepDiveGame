using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #218 evidence: the three fish already in the game beside the Quaternius Cute Fish candidates for the missing species, all scaled
    // to the same 1 m length, in one lit view and one backlit silhouette view (how a fish reads at a distance). Editor render only.
    // -executeMethod DeepDive.EditorTools.P411SpeciesPreview.PreviewBatch -previewOut <dir>
    // Needs Anglerfish/Tang/Humphead/Turbot/Swordfish.fbx copied from the CC0 Cute Fish pack into Art/Supplied/Fish first (not committed).
    public static class P411SpeciesPreview
    {
        private static readonly string[] Names = { "CoralGrouper", "Tuna", "RedSnapper", "Anglerfish", "Tang", "Humphead", "Turbot", "Swordfish" };
        private const string Folder = "Assets/DeepDive/Art/Supplied/Fish/";

        public static void PreviewBatch()
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-previewOut");
            var outDir = i >= 0 ? args[i + 1] : "Logs/p411-species";
            Directory.CreateDirectory(outDir);
            AssetDatabase.Refresh();
            foreach (var name in Names)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
                if (importer.importAnimation && importer.animationType == ModelImporterAnimationType.Legacy) continue;
                importer.importCameras = importer.importLights = importer.addCollider = false;
                importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Legacy;
                importer.SaveAndReimport();
            }
            // The repo's own fish pipeline already solves the legacy-rig scale and pivot problems (SuppliedKitImport.Make), so the candidates
            // are built through it into temporary Cand_* prefabs, which this preview deletes again.
            var make = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("DeepDive.World.Editor.SuppliedKitImport")).First(t => t != null)
                .GetMethod("Make", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Directory.CreateDirectory("Assets/DeepDive/Art/Resources/SuppliedKit/Materials");
            var log = new System.Text.StringBuilder("fish,clips,swim_s,fast_s,death_s\n");
            var root = new GameObject("PreviewRoot");
            var fish = new GameObject[Names.Length];
            var temp = new System.Collections.Generic.List<string>();
            for (var k = 0; k < Names.Length; k++)
            {
                var path = Folder + Names[k] + ".fbx";
                make.Invoke(null, new object[] { "Fish/" + Names[k], "Cand_" + Names[k], new Vector3(.3f, .5f, 1.05f), true, false });
                var prefabPath = "Assets/DeepDive/Art/Resources/SuppliedKit/Cand_" + Names[k] + ".prefab"; temp.Add(prefabPath);
                var holder = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), root.transform);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
                AnimationClip Clip(string key) => clips.FirstOrDefault(c => c.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
                var swim = Clip("Swimming_Impulse"); var fast = Clip("Swimming_Fast"); var death = Clip("Death");
                var model = holder.GetComponentInChildren<Animation>();
                foreach (var r in holder.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
                if (swim != null && model != null) swim.SampleAnimation(model.gameObject, swim.length * .3f);
                holder.transform.rotation = Quaternion.Euler(0, -90f, 0); holder.transform.position = new Vector3(k * .5f, 0, 0);   // length is along z: turn side-on to the camera
                fish[k] = holder;
                string L(AnimationClip c) => c == null ? "none" : c.length.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                log.AppendLine($"{Names[k]},{clips.Length},{L(swim)},{L(fast)},{L(death)}");
            }
            File.WriteAllText(Path.Combine(outDir, "species-candidates.csv"), log.ToString());
            Render(root, fish, Path.Combine(outDir, "species-lineup-lit.png"), false);
            Render(root, fish, Path.Combine(outDir, "species-lineup-silhouette.png"), true);
            Object.DestroyImmediate(root);
            foreach (var p in temp) AssetDatabase.DeleteAsset(p);
            Debug.Log("P411_SPECIES_PREVIEW_DONE " + outDir + "\n" + log);
        }

        private static void Render(GameObject root, GameObject[] fish, string output, bool silhouette)
        {
            const int w = 1600, h = 420;
            var cam = new GameObject("cam").AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = silhouette ? new Color(.62f, .78f, .86f) : new Color(.18f, .42f, .52f);
            cam.orthographic = true;
            var cx = (fish.Length - 1) * .25f; cam.orthographicSize = (cx + .4f) * h / w; cam.transform.position = new Vector3(cx, 0, -8); cam.transform.LookAt(new Vector3(cx, 0, 0));
            var sun = new GameObject("sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(30, -35, 0); sun.intensity = 1.4f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .6f, .62f);
            Material dark = null;
            if (silhouette)
            {
                dark = new Material(Shader.Find("Universal Render Pipeline/Unlit")); dark.SetColor("_BaseColor", new Color(.05f, .09f, .12f));
                foreach (var go in fish) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => dark).ToArray();
            }
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(output, t.EncodeToPNG());
            Object.DestroyImmediate(t); Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(sun.gameObject);
        }
    }
}
