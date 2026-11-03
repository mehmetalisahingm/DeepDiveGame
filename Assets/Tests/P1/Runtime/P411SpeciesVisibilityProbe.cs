using System;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.P1.Lab
{
    // #218 acceptance evidence, observation only: in every real process of an integrated run, which fish species the
    // scene shows (spawned FishActor, enabled renderers) and, in a process with a graphics device, a near and a far
    // picture of one real fish of each species from the live scene. It changes no state and judges nothing.
    // -p411-species <dir>
    public sealed class P411SpeciesVisibilityProbe : MonoBehaviour
    {
        private static readonly string[] Species = { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
        private string dir;
        private bool finished;
        private double readyAt = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Arg("-p1-integrated").Length == 0 || Arg("-p411-species").Length == 0) return;
            if (FindFirstObjectByType<P411SpeciesVisibilityProbe>() != null) return;
            var go = new GameObject("P411SpeciesVisibilityProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<P411SpeciesVisibilityProbe>().dir = Arg("-p411-species");
        }

        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
        }

        private void LateUpdate()
        {
            if (finished) return;
            var fish = FindObjectsByType<FishActor>(FindObjectsSortMode.None).Where(f => f != null && f.IsSpawned && f.Species != null).ToArray();
            if (fish.Length < 15) { readyAt = -1; return; }
            if (readyAt < 0) readyAt = Time.realtimeSinceStartupAsDouble;
            if (Time.realtimeSinceStartupAsDouble - readyAt < 2.0) return;   // let the live motion and the late joiners settle
            finished = true;
            Directory.CreateDirectory(dir);
            var role = Arg("-p1-integrated");
            var tag = role + "-" + System.Diagnostics.Process.GetCurrentProcess().Id;
            var sb = new StringBuilder("{\n  \"role\": \"" + role + "\", \"pid\": " + System.Diagnostics.Process.GetCurrentProcess().Id +
                ", \"graphics\": " + (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null ? "true" : "false") +
                ", \"spawnedFish\": " + fish.Length + ",\n  \"species\": [\n");
            for (var i = 0; i < Species.Length; i++)
            {
                var of = fish.Where(f => f.Species.SpeciesId == Species[i]).ToArray();
                var visible = of.Count(f => f.GetComponentsInChildren<Renderer>(false).Any(r => r.enabled));
                sb.Append("    { \"id\": \"" + Species[i] + "\", \"spawned\": " + of.Length + ", \"withVisibleRenderer\": " + visible + " }" + (i < Species.Length - 1 ? "," : "") + "\n");
            }
            sb.Append("  ]\n}\n");
            File.WriteAllText(Path.Combine(dir, "species-" + tag + ".json"), sb.ToString());
            Debug.Log("P411_SPECIES_VISIBILITY role=" + role + " spawned=" + fish.Length + " counts=" +
                      string.Join(",", Species.Select(id => id + ":" + fish.Count(f => f.Species.SpeciesId == id))));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                foreach (var id in Species)
                {
                    var one = fish.FirstOrDefault(f => f.Species.SpeciesId == id);
                    if (one == null) continue;
                    Shoot(one, 2.5f, Path.Combine(dir, tag + "-" + id + "-near.png"));
                    Shoot(one, 9f, Path.Combine(dir, tag + "-" + id + "-far.png"));
                }
            Debug.Log("P411_SPECIES_PICTURES_DONE " + tag);
        }

        // One real fish, seen from its side at the given distance, with the scene's own light, fog and post settings.
        private static void Shoot(FishActor fish, float distance, string path)
        {
            const int w = 960, h = 540;
            var renderers = fish.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var side = Vector3.Cross(Vector3.up, fish.transform.forward).normalized;
            var camObject = new GameObject("P411SpeciesCamera");
            var cam = camObject.AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.fieldOfView = 50f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f;
            cam.transform.position = bounds.center + side * distance + Vector3.up * 0.15f;
            cam.transform.LookAt(bounds.center);
            cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Destroy(t); Destroy(rt); Destroy(camObject);
        }
    }
}
