using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #188 madde 5 / #189: how readable is the repair worksite by day, at dusk and at night? The real DiveTestArea is opened with
    // the runtime town built on top (as P48RepairAreaAudit does), the authored worksite (broken hull + three R01 parts + drop
    // trestle) is placed at the cradle/drop anchors exactly as BoatRepairNetworkBinding places it, and the sun/ambient/fog/background
    // are set with CoastalAtmosphere's own formulas from DaylightModel at fixed clock minutes. Fixed cameras: the crew's eye behind the
    // stern, the walk in from the town, and a wide shot. Measures screen luminance of the parts, the lantern and the whole frame.
    // Read-only: the scene is closed unsaved. It is a scene render, not a Windows build run.
    // Building the runtime town re-saves the Town/Resources/DevVisuals materials on disk; `git checkout -- Assets/DeepDive/Town/Resources/DevVisuals` after a run.
    public static class P48RepairNightAudit
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string Art = "Assets/DeepDive/Art/Resources/RepairSet/";
        private static readonly int[] Minutes = { 12 * 60, 20 * 60 + 15, 21 * 60, 23 * 60 };
        private static readonly string[] Names = { "day-1200", "dusk-2015", "night-2100", "night-2300" };

        // -executeMethod DeepDive.Editor.P48RepairNightAudit.RunBatch -nightOut <dir> [-nightLantern <0|1>] [-nightParts <0|1>]
        public static void RunBatch()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string key, string fallback) { var i = Array.IndexOf(args, key); return i >= 0 ? args[i + 1] : fallback; }
            var output = Arg("-nightOut", "Logs/p48-night"); var lantern = Arg("-nightLantern", "1") == "1"; var parts = Arg("-nightParts", "1") == "1";
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var csv = new StringBuilder("time,view,frame_mean,hull_mean,parts_mean,lantern_peak,parts_vs_hull\n");
            try
            {
                SceneManager.SetActiveScene(scene);
                ContinuousTownLayout.Build(scene);
                CoastalHarborLink.BuildMarkers(scene);
                foreach (var service in Object.FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None)) TownStorefront.Build(service);

                if (!WorldAnchors.TryGetPose(RepairAnchorIds.Cradle, out var cradle)) throw new InvalidOperationException("no cradle anchor");
                WorldAnchors.TryFind(RepairAnchorIds.Drop, out var drop);
                var site = new GameObject("AuditSite"); site.transform.SetPositionAndRotation(cradle.position, cradle.rotation);
                Spawn("DamagedRowboat", site.transform, Vector3.zero);
                var slotObjects = new GameObject[3];
                if (parts)
                {
                    var names = new[] { "RepairedHullPatch", "RepairedSeatSupport", "RepairedOarlock" };
                    for (var i = 0; i < 3; i++) slotObjects[i] = Spawn(names[i], site.transform, BoatRepairNetworkBinding.SlotLocal(i));
                }
                var trestle = Spawn("DropTrestle", drop.transform, Vector3.zero);
                if (!lantern) foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Lantern" && t.IsChildOf(site.transform) || t.name == "Lantern" && t.IsChildOf(trestle.transform)).ToArray()) t.gameObject.SetActive(false);

                var sun = RenderSettings.sun ?? Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                var views = Views(cradle, site.transform);
                for (var m = 0; m < Minutes.Length; m++)
                {
                    var daylight = DaylightModel.Evaluate(Minutes[m], DayPhase.Running, 0);
                    Apply(daylight, sun);
                    foreach (var glow in Object.FindObjectsByType<RepairLanternGlow>(FindObjectsSortMode.None)) glow.Refresh();
                    foreach (var view in views)
                    {
                        var path = Path.Combine(output, Names[m] + "-" + view.Name + ".png");
                        var s = Shoot(view, site, slotObjects, path);
                        csv.AppendLine(string.Join(",", Names[m], view.Name, F(s.frame), F(s.hull), F(s.parts), F(s.lantern), F(s.parts / Mathf.Max(.0001f, s.hull))));
                    }
                }
                File.WriteAllText(Path.Combine(output, "night-audit.csv"), csv.ToString());
                Debug.Log("P48_NIGHT_AUDIT_DONE " + output + "\n" + csv);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        private static string F(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);

        private static GameObject Spawn(string prefab, Transform parent, Vector3 local)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + prefab + ".prefab"), parent);
            go.transform.localPosition = local; go.transform.localRotation = Quaternion.identity;
            return go;
        }

        // CoastalAtmosphere.ApplyTo for a camera above water on a calm day.
        private static void Apply(DaylightState d, Light sun)
        {
            RenderSettings.fog = true; RenderSettings.fogDensity = .009f;
            RenderSettings.fogColor = Scale(new Color(.54f, .72f, .78f), d.FogColorMultiplier);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Scale(new Color(.64f, .69f, .72f), d.AmbientMultiplier);
            if (sun == null) return;
            sun.color = new Color(1f, .89f, .73f); sun.intensity = 1.6f * d.SunIntensityMultiplier; sun.transform.rotation = Quaternion.Euler(48, -32, 0);
        }
        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        private struct View { public string Name; public Vector3 Position, Target; public float Fov; }

        private static View[] Views(Pose cradle, Transform site)
        {
            var back = cradle.position + cradle.rotation * new Vector3(0f, 1.65f, -3.2f);                   // crew eye behind the stern
            var slots = site.TransformPoint(BoatRepairNetworkBinding.SlotLocal(1));
            return new[]
            {
                new View { Name = "crew", Position = back, Target = slots, Fov = 60f },
                new View { Name = "approach", Position = cradle.position + new Vector3(-9f, 1.7f, 4.5f), Target = cradle.position + Vector3.up * .8f, Fov = 60f },
                new View { Name = "wide", Position = cradle.position + new Vector3(-6f, 5.5f, -7f), Target = cradle.position + Vector3.up * .5f, Fov = 50f }
            };
        }

        private struct Stats { public float frame, hull, parts, lantern; }

        private static Stats Shoot(View view, GameObject site, GameObject[] slotObjects, string path)
        {
            const int w = 960, h = 540;
            var camObject = new GameObject("AuditCam"); var cam = camObject.AddComponent<Camera>();
            cam.transform.position = view.Position; cam.transform.LookAt(view.Target); cam.fieldOfView = view.Fov;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Scale(new Color(.43f, .66f, .78f), DaylightScale());
            cam.nearClipPlane = .1f; cam.farClipPlane = 200f;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            cam.Render(); cam.Render(); // second frame: the first is not lit like the rest
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = null;
            var px = tex.GetPixels32();

            float Lum(Color32 c) => (.2126f * c.r + .7152f * c.g + .0722f * c.b) / 255f;
            var stats = new Stats();
            double sum = 0; foreach (var p in px) sum += Lum(p); stats.frame = (float)(sum / px.Length);

            Rect Box(Renderer[] rs)
            {
                var xs = new System.Collections.Generic.List<Vector3>();
                foreach (var r in rs) { var b = r.bounds; foreach (var sx in new[] { -1, 1 }) foreach (var sy in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) xs.Add(cam.WorldToScreenPoint(b.center + Vector3.Scale(b.extents, new Vector3(sx, sy, sz)))); }
                xs = xs.Where(v => v.z > 0).ToList();
                if (xs.Count == 0) return Rect.zero;
                var x0 = Mathf.Clamp(xs.Min(v => v.x), 0, w - 1); var x1 = Mathf.Clamp(xs.Max(v => v.x), 0, w - 1);
                var y0 = Mathf.Clamp(xs.Min(v => v.y), 0, h - 1); var y1 = Mathf.Clamp(xs.Max(v => v.y), 0, h - 1);
                return Rect.MinMaxRect(x0, y0, x1, y1);
            }
            float MeanIn(Rect r) { if (r.width < 2 || r.height < 2) return 0; double s = 0; var n = 0; for (var y = (int)r.yMin; y < (int)r.yMax; y++) for (var x = (int)r.xMin; x < (int)r.xMax; x++) { s += Lum(px[y * w + x]); n++; } return n > 0 ? (float)(s / n) : 0; }
            float PeakIn(Rect r) { float mx = 0; for (var y = (int)r.yMin; y < (int)r.yMax; y++) for (var x = (int)r.xMin; x < (int)r.xMax; x++) mx = Mathf.Max(mx, Lum(px[y * w + x])); return mx; }

            var partRenderers = slotObjects.Where(o => o != null).SelectMany(o => o.GetComponentsInChildren<Renderer>()).Where(r => r.gameObject.activeInHierarchy).ToArray();
            var hullRoot = site.transform.Find("DamagedRowboat");
            var hullRenderers = hullRoot.GetComponentsInChildren<Renderer>().Where(r => r.gameObject.activeInHierarchy && !r.transform.IsChildOf(hullRoot.Find("WorkLantern"))).ToArray();
            var lanterns = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Glass" && t.gameObject.activeInHierarchy).Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
            if (partRenderers.Length > 0) stats.parts = MeanIn(Box(partRenderers));
            stats.hull = MeanIn(Box(hullRenderers));
            if (lanterns.Length > 0) stats.lantern = lanterns.Select(l => PeakIn(Box(new[] { l }))).Max();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt); Object.DestroyImmediate(camObject);
            return stats;
        }

        private static float DaylightScale() => RenderSettings.fogColor.g > 0 ? RenderSettings.fogColor.g / .72f : 1f;
    }
}
