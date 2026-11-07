using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Core.Contracts;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.Composition
{
    // #249/#259/#171 acceptance probe for a real Windows player: it renders the built town (street, the equipment store inside and out, the
    // shared house inside and out, the shore) by day, dusk and night from fixed cameras and writes the pictures plus a small CSV of renderer
    // counts and frame times. Inert unless the player is started with `-town-look <outDir>`; like P48WindowsProbe no scene references it and no
    // network is involved (the day is driven by DaylightModel directly). Build it with TownLookProbeBuild.
    public static class TownLookProbe
    {
        private static readonly int[] Minutes = { 12 * 60, 20 * 60 + 15, 21 * 60 + 30 };
        private static readonly string[] Names = { "day", "dusk", "night" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-town-look");
            if (i < 0 || i + 1 >= args.Length) return;
            var host = new GameObject("TownLookProbe");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Runner>().Begin(args[i + 1]);
        }

        private readonly struct Shot
        {
            public readonly string Name; public readonly Vector3 From, To; public readonly float Fov; public readonly bool NightToo;
            public Shot(string name, Vector3 from, Vector3 to, float fov, bool nightToo = true) { Name = name; From = from; To = to; Fov = fov; NightToo = nightToo; }
        }

        private sealed class Runner : MonoBehaviour
        {
            private string output;
            private readonly StringBuilder log = new StringBuilder();

            public void Begin(string outDir) { output = outDir; Directory.CreateDirectory(outDir); StartCoroutine(Run()); }

            private IEnumerator Run()
            {
                var ok = false;
                var body = Shoot();
                while (true)
                {
                    object current;
                    try { if (!body.MoveNext()) { ok = true; break; } current = body.Current; }
                    catch (Exception e) { log.AppendLine("PROBE_EXCEPTION " + e); break; }
                    yield return current;
                }
                File.WriteAllText(Path.Combine(output, "probe.log"), log.ToString());
                Debug.Log((ok ? "TOWNLOOK_DONE " : "TOWNLOOK_FAILED ") + output);
                Application.Quit(ok ? 0 : 1);
            }

            private static Vector3 H(float x, float y, float z) => ContinuousTownLayout.HomeOrigin + new Vector3(x, y, z);

            private IEnumerator Shoot()
            {
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(2.5f);
                yield return null;
                foreach (var atmosphere in FindObjectsByType<CoastalAtmosphere>(FindObjectsSortMode.None)) atmosphere.enabled = false;

                var store = ContinuousTownLayout.StorefrontPositions[0];
                const float G = ContinuousTownLayout.WalkSurfaceY;
                var camera = new GameObject("ProbeCamera").AddComponent<Camera>();
                camera.nearClipPlane = .08f; camera.farClipPlane = 250f; camera.clearFlags = CameraClearFlags.SolidColor;
                foreach (var other in Camera.allCameras) if (other != camera) other.enabled = false;
                var sun = RenderSettings.sun != null ? RenderSettings.sun : FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);

                var shots = new List<Shot>
                {
                    new Shot("house-front", new Vector3(0f, G + 2.2f, -35.5f), H(0f, 1.6f, 0f), 62f),
                    new Shot("house-corner", new Vector3(11f, G + 3.6f, -37f), H(-1f, 1.8f, 1f), 58f),
                    new Shot("house-inside-door", H(0f, 1.65f, 5.6f), H(0f, 1.2f, -4f), 70f),
                    new Shot("house-inside-back", H(0f, 1.65f, -5.3f), H(0f, 1.1f, 4.5f), 72f),
                    new Shot("house-inside-west", H(-4.6f, 1.65f, 0.5f), H(5f, 1.3f, -3.6f), 72f),
                    new Shot("house-inside-pc", H(0.8f, 1.6f, -0.8f), H(3.9f, 1.4f, -3.6f), 62f),
                    new Shot("house-inside-east", H(4.7f, 1.65f, 1f), H(-5f, 1.3f, -3.4f), 72f),
                    new Shot("store-front", new Vector3(store.x, G + 1.8f, -15.5f), new Vector3(store.x, G + 2.0f, -25f), 62f),
                    new Shot("store-corner", new Vector3(-1.5f, G + 4.2f, -15.5f), new Vector3(store.x, G + 1.8f, -24f), 58f),
                    new Shot("store-inside", new Vector3(store.x, G + 1.65f, -22.1f), new Vector3(store.x, G + 1.3f, -28.5f), 72f),
                    new Shot("store-till", new Vector3(store.x - 0.9f, G + 1.65f, -26.4f), new Vector3(store.x + 2f, G + 1.2f, -22.2f), 72f),
                    new Shot("store-shelves", new Vector3(store.x + 3.4f, G + 1.65f, -23.2f), new Vector3(store.x - 4.2f, G + 1.3f, -27.5f), 72f),
                    new Shot("street-north", new Vector3(0f, G + 1.8f, -17f), new Vector3(0f, G + 2.2f, -34f), 68f),
                    new Shot("street-south", new Vector3(0f, G + 1.8f, -38f), new Vector3(0f, G + 2.0f, -18f), 68f),
                    new Shot("town-overview", new Vector3(0f, 16f, -4f), new Vector3(0f, 8.4f, -30f), 64f),
                    new Shot("town-west", new Vector3(-24f, 9f, -20f), new Vector3(-6f, 9f, -33f), 60f),
                    new Shot("beach", new Vector3(4f, G + 3.5f, -2f), new Vector3(0f, G, -17f), 64f),
                };

                // Development states: before (plot markers) and after (everything built)
                var visuals = FindObjectsByType<DevelopmentVisual>(FindObjectsSortMode.None);
                log.AppendLine($"renderers={FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} developmentVisuals={visuals.Length} store={(FindFirstObjectByType<EquipmentStoreLayout>() != null)} house={(GameObject.Find(HomeDressing.RootName) != null)}");

                for (var state = 0; state < 2; state++)
                {
                    foreach (var visual in visuals) SetBuilt(visual, state == 1);
                    var suffix = state == 0 ? "" : "-built";
                    for (var m = 0; m < Minutes.Length; m++)
                    {
                        var d = DaylightModel.Evaluate(Minutes[m], DayPhase.Running, 0);
                        Apply(d, sun, camera);
                        foreach (var lighting in FindObjectsByType<DressingLighting>(FindObjectsSortMode.None)) { lighting.enabled = false; lighting.Apply(d.NightFactor); }   // no network here: its own poll would read "day"
                        foreach (var shot in shots)
                        {
                            if (state == 1 && !(shot.Name.StartsWith("house-inside") || shot.Name.StartsWith("store-inside") || shot.Name == "street-north" || shot.Name == "house-front")) continue;
                            if (m > 0 && !shot.NightToo) continue;
                            camera.transform.position = shot.From; camera.transform.LookAt(shot.To); camera.fieldOfView = shot.Fov;
                            yield return null; yield return null;
                            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
                            camera.Render(); camera.Render();
                            RenderTexture.active = rt;
                            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                            RenderTexture.active = null; camera.targetTexture = null; Destroy(rt);
                            File.WriteAllBytes(Path.Combine(output, shot.Name + suffix + "-" + Names[m] + ".jpg"), tex.EncodeToJPG(86));
                            Destroy(tex);
                        }
                    }
                }

                // Frame time of the whole lit town by day, from the overview camera
                var overview = shots.First(s => s.Name == "town-overview");
                camera.transform.position = overview.From; camera.transform.LookAt(overview.To); camera.fieldOfView = overview.Fov;
                Apply(DaylightModel.Evaluate(Minutes[0], DayPhase.Running, 0), sun, camera);
                for (var w = 0; w < 30; w++) yield return null;
                var samples = new List<float>(300);
                for (var f = 0; f < 300; f++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000f); }
                samples.Sort();
                log.AppendLine($"overview_frames={samples.Count} mean_ms={samples.Average():0.00} p95_ms={samples[(int)(samples.Count * .95f)]:0.00} max_ms={samples[samples.Count - 1]:0.00}");
                // The #278 success card (IMGUI is not in the camera render, so an outside screen grab takes it): markers tell the grabber when to look
                var demo = new GameObject("SuccessCardDemo").AddComponent<SuccessFeedbackView>();
                SuccessFeedbackRules.TryBuild(ServicePointType.FishBuyer, true, 240, 3, 360, 0, true, out var firstCard);
                demo.Show(firstCard);
                yield return new WaitForSecondsRealtime(1.4f);
                File.WriteAllText(Path.Combine(output, "card-first.ready"), "1");
                yield return new WaitForSecondsRealtime(2.5f);
                SuccessFeedbackRules.TryBuild(ServicePointType.RecordingBuyer, true, 90, 1, 450, 1, false, out var plainCard);
                demo.Show(plainCard);
                yield return new WaitForSecondsRealtime(1.4f);
                File.WriteAllText(Path.Combine(output, "card-plain.ready"), "1");
                yield return new WaitForSecondsRealtime(2.5f);
                log.AppendLine($"gpu={SystemInfo.graphicsDeviceName} api={SystemInfo.graphicsDeviceType} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            }

            private static void SetBuilt(DevelopmentVisual visual, bool built)
            {
                var b = visual.transform.Find("Built"); var p = visual.transform.Find("Plot");
                if (b != null) b.gameObject.SetActive(built);
                if (p != null) p.gameObject.SetActive(!built);
                visual.enabled = false;   // stop its own poll from undoing the forced state
            }

            private static void Apply(DaylightState d, Light sun, Camera camera)
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .004f;
                RenderSettings.fogColor = Scale(new Color(.54f, .72f, .78f), d.FogColorMultiplier);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Scale(new Color(.64f, .69f, .72f), d.AmbientMultiplier);
                camera.backgroundColor = Scale(new Color(.43f, .66f, .78f), d.FogColorMultiplier);
                if (sun == null) return;
                sun.color = new Color(1f, .89f, .73f); sun.intensity = 1.6f * d.SunIntensityMultiplier; sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }

            private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        }
    }
}
