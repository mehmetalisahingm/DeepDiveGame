using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DeepDive.EditorTools
{
    // #249 evidence: editor renders of the equipment store prefab alone (not the game scene): street view, interior views, a top-down plan
    // and night views with only the shop's own lights. These are editor previews, not Windows player pictures.
    // -executeMethod DeepDive.EditorTools.P410EquipmentStorePreview.RenderBatch -previewOut <dir>   (needs a graphics device: no -nographics)
    public static class P410EquipmentStorePreview
    {
        private const int W = 1280, H = 720;

        public static void RenderBatch()
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-previewOut");
            var outDir = i >= 0 ? args[i + 1] : "Logs/p410-store";
            Directory.CreateDirectory(outDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P410EquipmentStoreBuilder.PrefabPath);
            var store = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var street = GameObject.CreatePrimitive(PrimitiveType.Cube);
            street.name = "StreetPlane";
            street.transform.position = new Vector3(0f, -0.1f, -6f);
            street.transform.localScale = new Vector3(40f, 0.2f, 12f);
            street.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.42f, 0.4f, 0.36f));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "SideGround";
            ground.transform.position = new Vector3(0f, -0.1f, 4f);
            ground.transform.localScale = new Vector3(40f, 0.2f, 12f);
            ground.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.38f, 0.36f, 0.33f));
            Object.DestroyImmediate(ground);   // the store's own floor is the ground inside; only the street is drawn

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            sun.shadows = LightShadows.Soft;

            Day(sun);
            Shot(outDir, "street-day", new Vector3(5.5f, 1.7f, -13f), new Vector3(0f, 2.8f, 0f), 50f);
            Shot(outDir, "street-door-close", new Vector3(1.5f, 1.65f, -4.2f), new Vector3(0f, 1.4f, 2f), 60f);
            Shot(outDir, "interior-from-door-day", new Vector3(0f, 1.62f, -0.5f), new Vector3(0f, 1.2f, 5.5f), 75f);
            Shot(outDir, "interior-counter-day", new Vector3(-3.2f, 1.62f, 1.0f), new Vector3(3.7f, 1.0f, 4.6f), 75f);
            Shot(outDir, "interior-back-shelves-day", new Vector3(2.4f, 1.62f, 1.2f), new Vector3(-2.2f, 1.2f, 7.5f), 75f);

            Night(sun);
            Shot(outDir, "street-night", new Vector3(5.5f, 1.7f, -13f), new Vector3(0f, 2.8f, 0f), 50f);
            Shot(outDir, "interior-from-door-night", new Vector3(0f, 1.62f, -0.5f), new Vector3(0f, 1.2f, 5.5f), 75f);
            Shot(outDir, "interior-counter-night", new Vector3(-3.2f, 1.62f, 1.0f), new Vector3(3.7f, 1.0f, 4.6f), 75f);

            // Top-down plan: roof, ceiling and the closed storey away.
            Day(sun);
            foreach (var t in store.GetComponentsInChildren<Transform>(true))
                if ((t.name.StartsWith("Roof") || t.name == "Ceiling" || t.name == "SignBoard" || t.name == "SignText") ||
                    (t.parent != null && t.parent.name == "Shell" && t.localPosition.y > P410EquipmentStoreBuilder.Storey - 0.1f))
                    t.gameObject.SetActive(false);
            Plan(outDir, "plan-top-down");
            Debug.Log("P410_STORE_PREVIEW_DONE " + outDir);
        }

        private static Material Flat(Color color)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            return m;
        }

        private static void Day(Light sun)
        {
            sun.enabled = true; sun.intensity = 1.5f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.fog = false;
        }

        private static void Night(Light sun)
        {
            sun.enabled = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.12f);
        }

        private static void Shot(string dir, string name, Vector3 from, Vector3 at, float fov)
        {
            var camObject = new GameObject("PreviewCam");
            var cam = camObject.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.ambientLight.maxColorComponent > 0.3f ? new Color(0.62f, 0.76f, 0.88f) : new Color(0.03f, 0.04f, 0.08f);
            cam.transform.position = from;
            cam.transform.LookAt(at);
            Render(cam, Path.Combine(dir, name + ".png"));
            Object.DestroyImmediate(camObject);
        }

        private static void Plan(string dir, string name)
        {
            var camObject = new GameObject("PlanCam");
            var cam = camObject.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5.2f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.76f, 0.88f);
            cam.transform.position = new Vector3(0f, 20f, 4f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Render(cam, Path.Combine(dir, name + ".png"));
            Object.DestroyImmediate(camObject);
        }

        private static void Render(Camera cam, string path)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t); Object.DestroyImmediate(rt);
        }
    }
}
