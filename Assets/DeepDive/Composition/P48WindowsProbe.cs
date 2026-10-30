using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Core.Contracts;
using DeepDive.World;
using UnityEngine;

namespace DeepDive.Composition
{
    // #189/#217 acceptance probe for a real Windows player. Inert unless the player is started with `-p48-probe <outDir>`: then, once
    // the DiveTestArea scene (with its runtime town) is up, it places the authored repair worksite at the cradle/drop anchors and a
    // block of reef-kit pieces, renders the worksite by day, dusk and night with CoastalAtmosphere's own formulas (no ocean, no
    // network), measures screen luminance of the repaired parts against the broken hull, and times frames with and without the kit.
    // Nothing here ships a behaviour: no scene references this type and the entry point returns when the switch is absent.
    public static class P48WindowsProbe
    {
        private static readonly int[] Minutes = { 12 * 60, 20 * 60 + 15, 21 * 60, 23 * 60 };
        private static readonly string[] Names = { "day-1200", "dusk-2015", "night-2100", "night-2300" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, "-p48-probe");
            if (i < 0 || i + 1 >= args.Length) return;
            var host = new GameObject("P48WindowsProbe");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Runner>().Begin(args[i + 1]);
        }

        private sealed class Runner : MonoBehaviour
        {
            private string output;
            private readonly StringBuilder log = new StringBuilder();

            public void Begin(string outDir) { output = outDir; Directory.CreateDirectory(outDir); StartCoroutine(Run()); }

            private static string F(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);

            private IEnumerator Run()
            {
                var ok = false;
                var csv = new StringBuilder("time,view,frame_mean,hull_mean,parts_mean,lantern_peak,parts_vs_hull\n");
                var frames = new StringBuilder("scenario,time,kit_pieces,frames,mean_ms,p95_ms,max_ms,fps_mean\n");
                IEnumerator body = Measure(csv, frames);
                while (true)
                {
                    object current;
                    try { if (!body.MoveNext()) { ok = true; break; } current = body.Current; }
                    catch (Exception e) { log.AppendLine("PROBE_EXCEPTION " + e); break; }
                    yield return current;
                }
                File.WriteAllText(Path.Combine(output, "night-player.csv"), csv.ToString());
                File.WriteAllText(Path.Combine(output, "frametime-player.csv"), frames.ToString());
                File.WriteAllText(Path.Combine(output, "probe.log"), log.ToString());
                Debug.Log((ok ? "P48_PROBE_DONE " : "P48_PROBE_FAILED ") + output);
                Application.Quit(ok ? 0 : 1);
            }

            private IEnumerator Measure(StringBuilder csv, StringBuilder frames)
            {
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                log.AppendLine($"gpu={SystemInfo.graphicsDeviceName} api={SystemInfo.graphicsDeviceType} cpu={SystemInfo.processorType} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} unity={Application.unityVersion}");
                yield return new WaitForSecondsRealtime(2f);
                yield return null;
                foreach (var atmosphere in FindObjectsByType<CoastalAtmosphere>(FindObjectsSortMode.None)) atmosphere.enabled = false;

                if (!WorldAnchors.TryGetPose(RepairAnchorIds.Cradle, out var cradle)) throw new InvalidOperationException("no cradle anchor (town not built?)");
                WorldAnchors.TryFind(RepairAnchorIds.Drop, out var drop);
                var site = new GameObject("ProbeSite").transform; site.SetPositionAndRotation(cradle.position, cradle.rotation);
                BoatRepairSetArt.AttachDamagedHull(site);
                var slots = new GameObject[3];
                for (var i = 0; i < 3; i++)
                {
                    slots[i] = BoatRepairSetArt.Part(i, site);
                    if (slots[i] != null) slots[i].transform.localPosition = BoatRepairNetworkBinding.SlotLocal(i);
                }
                var bench = drop != null ? BoatRepairSetArt.AttachTrestle(drop.transform) : null;

                var cameraObject = new GameObject("ProbeCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = .1f; camera.farClipPlane = 200f; camera.clearFlags = CameraClearFlags.SolidColor;
                foreach (var other in Camera.allCameras) if (other != camera) other.enabled = false;
                var sun = RenderSettings.sun != null ? RenderSettings.sun : FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);

                var views = new[]
                {
                    ("crew", cradle.position + cradle.rotation * new Vector3(0f, 1.65f, -3.2f), site.TransformPoint(BoatRepairNetworkBinding.SlotLocal(1)), 60f),
                    ("approach", cradle.position + new Vector3(-9f, 1.7f, 4.5f), cradle.position + Vector3.up * .8f, 60f),
                    ("wide", cradle.position + new Vector3(-6f, 5.5f, -7f), cradle.position + Vector3.up * .5f, 50f)
                };

                for (var m = 0; m < Minutes.Length; m++)
                {
                    Apply(DaylightModel.Evaluate(Minutes[m], DayPhase.Running, 0), sun, camera);
                    foreach (var glow in FindObjectsByType<RepairLanternGlow>(FindObjectsSortMode.None)) glow.Refresh();
                    foreach (var (name, position, target, fov) in views)
                    {
                        camera.transform.position = position; camera.transform.LookAt(target); camera.fieldOfView = fov;
                        yield return null; yield return null;
                        // ScreenCapture is not in the project modules: render the same camera through the player pipeline into a texture.
                        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
                        camera.Render(); camera.Render();
                        RenderTexture.active = rt;
                        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                        RenderTexture.active = null; camera.targetTexture = null; Destroy(rt);
                        var s = Stats(tex, camera, site, slots);
                        File.WriteAllBytes(Path.Combine(output, Names[m] + "-" + name + ".png"), tex.EncodeToPNG());
                        Destroy(tex);
                        csv.AppendLine(string.Join(",", Names[m], name, F(s.frame), F(s.hull), F(s.parts), F(s.lantern), F(s.parts / Mathf.Max(.0001f, s.hull))));
                    }
                }

                // Frame time of the repair worksite (hull, parts, trestle and both lanterns): wide view, with and without, alternated so drift shows.
                camera.transform.position = views[2].Item2; camera.transform.LookAt(views[2].Item3); camera.fieldOfView = views[2].Item4;
                foreach (var minute in new[] { 0, 3 })
                {
                    Apply(DaylightModel.Evaluate(Minutes[minute], DayPhase.Running, 0), sun, camera);
                    foreach (var scenario in new[] { "worksite-off", "worksite-on", "worksite-off", "worksite-on" })
                    {
                        var on = scenario == "worksite-on";
                        site.gameObject.SetActive(on); if (bench != null) bench.SetActive(on);
                        for (var w = 0; w < 30; w++) yield return null;
                        var samples = new List<float>(400);
                        for (var f = 0; f < 400; f++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000f); }
                        samples.Sort();
                        var mean = samples.Average();
                        frames.AppendLine(string.Join(",", scenario, Names[minute], 0, samples.Count, F(mean), F(samples[(int)(samples.Count * .95f)]), F(samples[samples.Count - 1]), F(1000f / mean)));
                    }
                }
                site.gameObject.SetActive(true); if (bench != null) bench.SetActive(true);

                // Frame time: the same busy shore view with and without a block of reef pieces, alternated so drift shows.
                UnityEngine.Random.InitState(217);
                var kit = new GameObject("ProbeReefKit").transform;
                var count = 0;
                var centre = cradle.position + cradle.rotation * new Vector3(0f, 0f, 10f);
                foreach (var id in ReefKitPiece.AllIds)
                {
                    var prefab = ReefKitPiece.Load(id);
                    if (prefab == null) throw new InvalidOperationException("missing " + id);
                    for (var n = 0; n < 12; n++)
                    {
                        var p = centre + new Vector3(UnityEngine.Random.Range(-16f, 16f), 0f, UnityEngine.Random.Range(-6f, 14f));
                        if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var hit, 80f)) p.y = hit.point.y;
                        var piece = Instantiate(prefab, p, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), kit);
                        piece.transform.localScale *= UnityEngine.Random.Range(.8f, 1.4f);
                        count++;
                    }
                }
                log.AppendLine("kit_pieces=" + count);
                camera.transform.position = centre + new Vector3(0f, 3f, -14f); camera.transform.LookAt(centre + Vector3.up); camera.fieldOfView = 60f;
                foreach (var minute in new[] { 0, 3 })
                {
                    Apply(DaylightModel.Evaluate(Minutes[minute], DayPhase.Running, 0), sun, camera);
                    foreach (var scenario in new[] { "without-kit", "with-kit", "without-kit", "with-kit" })
                    {
                        kit.gameObject.SetActive(scenario == "with-kit");
                        for (var w = 0; w < 30; w++) yield return null;
                        var samples = new List<float>(400);
                        for (var f = 0; f < 400; f++) { yield return null; samples.Add(Time.unscaledDeltaTime * 1000f); }
                        samples.Sort();
                        var mean = samples.Average();
                        frames.AppendLine(string.Join(",", scenario, Names[minute], scenario == "with-kit" ? count : 0, samples.Count, F(mean), F(samples[(int)(samples.Count * .95f)]), F(samples[samples.Count - 1]), F(1000f / mean)));
                    }
                }
            }

            private static void Apply(DaylightState d, Light sun, Camera camera)
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .009f;
                RenderSettings.fogColor = Scale(new Color(.54f, .72f, .78f), d.FogColorMultiplier);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Scale(new Color(.64f, .69f, .72f), d.AmbientMultiplier);
                camera.backgroundColor = Scale(new Color(.43f, .66f, .78f), d.FogColorMultiplier);
                if (sun == null) return;
                sun.color = new Color(1f, .89f, .73f); sun.intensity = 1.6f * d.SunIntensityMultiplier; sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }

            private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

            private struct Stat { public float frame, hull, parts, lantern; }

            private static Stat Stats(Texture2D tex, Camera cam, Transform site, GameObject[] slots)
            {
                int w = tex.width, h = tex.height; var px = tex.GetPixels32();
                float Lum(Color32 c) => (.2126f * c.r + .7152f * c.g + .0722f * c.b) / 255f;
                var stats = new Stat();
                double sum = 0; foreach (var p in px) sum += Lum(p); stats.frame = (float)(sum / px.Length);

                Rect Box(IEnumerable<Renderer> rs)
                {
                    var pts = new List<Vector3>();
                    foreach (var r in rs) { var b = r.bounds; foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) pts.Add(cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3(sx, sy, sz)))); }
                    pts = pts.Where(v => v.z > 0).ToList();
                    if (pts.Count == 0) return Rect.zero;
                    return Rect.MinMaxRect(Mathf.Clamp(pts.Min(v => v.x), 0, w - 1), Mathf.Clamp(pts.Min(v => v.y), 0, h - 1), Mathf.Clamp(pts.Max(v => v.x), 0, w - 1), Mathf.Clamp(pts.Max(v => v.y), 0, h - 1));
                }
                float MeanIn(Rect r) { if (r.width < 2 || r.height < 2) return 0; double s = 0; var n = 0; for (var y = (int)r.yMin; y < (int)r.yMax; y++) for (var x = (int)r.xMin; x < (int)r.xMax; x++) { s += Lum(px[y * w + x]); n++; } return n > 0 ? (float)(s / n) : 0; }
                float PeakIn(Rect r) { float mx = 0; for (var y = (int)r.yMin; y < (int)r.yMax; y++) for (var x = (int)r.xMin; x < (int)r.xMax; x++) mx = Mathf.Max(mx, Lum(px[y * w + x])); return mx; }

                var parts = slots.Where(o => o != null).SelectMany(o => o.GetComponentsInChildren<Renderer>()).Where(r => r.gameObject.activeInHierarchy).ToArray();
                var hullRoot = site.Find("DamagedRowboat");
                var lanternRoot = hullRoot != null ? hullRoot.Find("WorkLantern") : null;
                var hull = hullRoot.GetComponentsInChildren<Renderer>().Where(r => r.gameObject.activeInHierarchy && (lanternRoot == null || !r.transform.IsChildOf(lanternRoot))).ToArray();
                var glass = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Glass" && t.gameObject.activeInHierarchy).Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
                if (parts.Length > 0) stats.parts = MeanIn(Box(parts));
                stats.hull = MeanIn(Box(hull));
                if (glass.Length > 0) stats.lantern = glass.Select(g => PeakIn(Box(new[] { g }))).Max();
                return stats;
            }
        }
    }
}
