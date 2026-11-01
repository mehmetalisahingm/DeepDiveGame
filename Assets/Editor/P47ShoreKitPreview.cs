using System;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #171 evidence: editor render of the shore kit, a lineup of every piece beside a 1.8 m reference and a small dock-and-beach
    // group by day and at night, lit with CoastalAtmosphere's above-water formulas from DaylightModel. Not a Windows build image.
    // -executeMethod DeepDive.EditorTools.P47ShoreKitPreview.PreviewBatch -previewMode <singles|group-day|group-night> -previewOut <png>
    public static class P47ShoreKitPreview
    {
        public static void PreviewBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key, string fallback) { var i = Array.IndexOf(args, key); return i >= 0 ? args[i + 1] : fallback; }
            var mode = Arg("-previewMode", "singles"); var output = Arg("-previewOut", "Logs/p47-shore-preview.png");
            var root = new GameObject("PreviewRoot");
            GameObject Spawn(string id, Vector3 at, float yaw, float scale = 1f)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(ShoreKitPiece.Load(id), root.transform);
                go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
                return go;
            }
            Vector3[] cams, targets; float[] fovs;
            if (mode == "singles")
            {
                var x = 0f; var centres = new float[16]; var k = 0;
                foreach (var id in ShoreKitPiece.AllIds)
                {
                    var size = ShoreKitPiece.Load(id).GetComponent<ShoreKitPiece>().Size; var step = Mathf.Max(size.x, size.z) + .5f;
                    x += step * .5f; centres[k++] = x; Spawn(id, new Vector3(x, 0f, 0f), 25f); x += step * .5f;
                }
                cams = new Vector3[4]; targets = new Vector3[4]; fovs = new[] { 50f, 50f, 50f, 50f };
                for (var g = 0; g < 4; g++)
                {
                    var c = (centres[g * 4] + centres[g * 4 + 3]) * .5f; var span = centres[g * 4 + 3] - centres[g * 4] + 4f;
                    cams[g] = new Vector3(c, 1.5f + span * .12f, -span * .85f); targets[g] = new Vector3(c, .8f, 0f);
                }
            }
            else
            {
                Spawn("shore-dock", new Vector3(0, -.5f, 0), 0f); Spawn("shore-dock", new Vector3(0, -.5f, -2.66f), 0f); Spawn("shore-dock-broken", new Vector3(0, -.5f, -5.32f), 0f);
                foreach (var sx in new[] { -1.2f, 1.2f }) { Spawn("shore-dock-pole", new Vector3(sx, -.5f, -1.3f), 0f); Spawn("shore-dock-pole", new Vector3(sx, -.5f, -4f), 0f); }
                Spawn("shore-prop-barrel", new Vector3(2.6f, 0, 1.2f), 0f); Spawn("shore-prop-bucket", new Vector3(1.9f, 0, 1.9f), 30f); Spawn("shore-prop-bucket-fishes", new Vector3(-1.8f, 0, 1.5f), -20f);
                Spawn("shore-prop-chest", new Vector3(-3f, 0, .8f), 15f); Spawn("shore-prop-anchor", new Vector3(3.6f, 0, 2.6f), 70f);
                Spawn("shore-rock-2", new Vector3(-4.8f, 0, 2.8f), 20f, 1.8f); Spawn("shore-rock-3", new Vector3(4.8f, 0, 3.5f), 80f, 2f); Spawn("shore-rock-4", new Vector3(-6f, 0, 1f), 140f, 1.6f);
                Spawn("shore-rock-1", new Vector3(5.6f, 0, .8f), 0f, 1.5f); Spawn("shore-rock-5", new Vector3(-2.2f, 0, 3.6f), 0f, 1.4f);
                Spawn("shore-palm-1", new Vector3(-6.5f, 0, 5f), 0f); Spawn("shore-palm-2", new Vector3(6.5f, 0, 6f), 90f);
                cams = new[] { new Vector3(-3.5f, 2.2f, -9f), new Vector3(0, 1.65f, 9f), new Vector3(2.5f, 1.3f, 5.5f), new Vector3(9f, 3.5f, -3f) };
                targets = new[] { new Vector3(0, .3f, 1f), new Vector3(0, .3f, -3f), new Vector3(2.8f, .3f, 1.5f), new Vector3(0, .6f, 1f) };
                fovs = new[] { 55f, 60f, 50f, 50f };
            }
            Render(root, output, cams, targets, fovs, mode == "singles", mode == "group-night");
            Debug.Log("P47_SHORE_PREVIEW_DONE " + output);
        }

        private static void Render(GameObject root, string output, Vector3[] cams, Vector3[] targets, float[] fovs, bool singles, bool night)
        {
            const int w = 800, h = 520;
            var d = DaylightModel.Evaluate(night ? 21 * 60 : 12 * 60, DayPhase.Running, 0);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .009f;
            RenderSettings.fogColor = Scale(new Color(.54f, .72f, .78f), d.FogColorMultiplier);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Scale(new Color(.64f, .69f, .72f), d.AmbientMultiplier);
            var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Material Flat(Color c) => new Material(lit) { color = c };
            GameObject Box(string n, Vector3 scale, Vector3 at, Color c) { var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = n; b.transform.localScale = scale; b.transform.position = at; b.GetComponent<Renderer>().sharedMaterial = Flat(c); Object.DestroyImmediate(b.GetComponent<Collider>()); return b; }
            var sand = new Color(.76f, .68f, .5f);
            var props = new[]
            {
                singles ? Box("floor", new Vector3(120, .1f, 40), new Vector3(30, -.05f, 8), sand)
                        : Box("sand", new Vector3(40, .1f, 20), new Vector3(0, -.05f, 8.7f), sand),
                singles ? null : Box("water", new Vector3(40, .1f, 20), new Vector3(0, -.45f, -11.3f), new Color(.1f, .35f, .45f))
            };
            var human = singles ? GameObject.CreatePrimitive(PrimitiveType.Capsule) : null;
            if (human != null) { human.transform.localScale = new Vector3(.5f, .9f, .5f); human.transform.position = new Vector3(-.4f, .9f, -1.6f); human.GetComponent<Renderer>().sharedMaterial = Flat(new Color(.9f, .3f, .25f)); Object.DestroyImmediate(human.GetComponent<Collider>()); }
            var camObject = new GameObject("cam"); var cam = camObject.AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Scale(new Color(.43f, .66f, .78f), d.FogColorMultiplier);
            var sun = new GameObject("sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, .89f, .73f);
            sun.intensity = 1.6f * d.SunIntensityMultiplier; sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            var sheet = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
            cam.transform.position = new Vector3(0, 3, -8); cam.transform.LookAt(Vector3.zero); cam.Render();   // warm-up frame: the first is not lit like the rest
            for (var k = 0; k < 4; k++)
            {
                cam.transform.position = cams[k]; cam.transform.LookAt(targets[k]); cam.fieldOfView = fovs[k]; cam.Render(); cam.Render();
                RenderTexture.active = rt;
                var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
                sheet.SetPixels((k % 2) * w, (1 - k / 2) * h, w, h, t.GetPixels()); Object.DestroyImmediate(t);
            }
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            foreach (var o in props.Where(p => p != null).Cast<Object>().Concat(new Object[] { camObject, sun.gameObject, human, root })) if (o != null) Object.DestroyImmediate(o);
        }

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    }
}
