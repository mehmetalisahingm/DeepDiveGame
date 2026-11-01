using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeepDive.EditorTools
{
    // Renders a prefab (or an ad-hoc list of kit parts) from four angles into one PNG for visual review.
    // Usage: -executeMethod DeepDive.EditorTools.P47MegaKitPreview.Run -previewPrefab <path|HOME|SHOP> -previewOut <png>
    public static class P47MegaKitPreview
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key) { var i = Array.IndexOf(args, key); return i >= 0 ? args[i + 1] : null; }
            var path = Arg("-previewPrefab"); var output = Arg("-previewOut");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)UnityEngine.Object.Instantiate(asset);
            Render(go, output);
            Debug.Log("P47_PREVIEW_DONE " + output);
        }

        public static void Render(GameObject go, string output)
        {
            const int tile = 512;
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds);
            var human = GameObject.CreatePrimitive(PrimitiveType.Cube); human.transform.localScale = new Vector3(.5f, 1.8f, .3f);
            human.transform.position = new Vector3(b.max.x + 1f, .9f, b.min.z - 1f);
            human.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.9f, .25f, .2f) };
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.localScale = new Vector3(60, .02f, 60); ground.transform.position = new Vector3(0, -.01f, 0);
            ground.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.78f, .72f, .55f) };
            var camObject = new GameObject("cam"); var cam = camObject.AddComponent<Camera>();
            var rt = new RenderTexture(tile, tile, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.62f, .78f, .9f); cam.fieldOfView = 30f;
            var light = new GameObject("sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0); light.intensity = 1.4f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .55f, .6f);
            var yaws = new[] { 20f, -35f, 160f, 90f };
            var sheet = new Texture2D(tile * 2, tile * 2, TextureFormat.RGB24, false);
            var center = b.center; var radius = Mathf.Max(b.extents.magnitude, 2.2f);
            for (var i = 0; i < 4; i++)
            {
                var dir = Quaternion.Euler(18f, yaws[i], 0f) * Vector3.back;
                cam.transform.position = center + dir * (radius / Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad) * 1.05f);
                cam.transform.LookAt(center); cam.Render();
                RenderTexture.active = rt;
                var t = new Texture2D(tile, tile, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, tile, tile), 0, 0); t.Apply();
                sheet.SetPixels((i % 2) * tile, (1 - i / 2) * tile, tile, tile, t.GetPixels()); UnityEngine.Object.DestroyImmediate(t);
            }
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            foreach (var o in new UnityEngine.Object[] { camObject, light.gameObject, ground, human, go }) UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
