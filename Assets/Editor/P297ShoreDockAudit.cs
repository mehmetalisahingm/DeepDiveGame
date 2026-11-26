using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // #297 (Utku): read-only measurement of the shore -> jetty walk on the real DiveTestArea scene. Four real-size
    // CharacterControllers walk at once from the beach gate onto the 2 m Dock_Town and back, the way four players
    // gather for the boat; a second pass sends two out while two come back. Also maps the step height along the
    // beach-platform edge from the ramp to the jetty root. Nothing is moved and the scene is closed unsaved.
    public static class P297ShoreDockAudit
    {
        public const string OutputPath = "docs/reports/evidence/p297-shore-dock/shore-dock.json";
        private const float Dt = 0.02f;

        [Serializable] public sealed class Walker { public string id; public Vector3 start, end; public bool arrived; public float seconds; public int blockedFrames; public float minClearance, maxY; }
        [Serializable] public sealed class Pass { public string id; public bool allArrived; public float seconds; public List<Walker> walkers = new List<Walker>(); }
        [Serializable] public sealed class Edge { public float x, z, platformY, outsideY; public string outsideCollider; public float step; }
        [Serializable] public sealed class Audit
        {
            public string note = "Real scene, real controller values, scene closed unsaved. Walkers steer straight at waypoints and never give way.";
            public float radius, height, stepOffset, walkSpeed;
            public Vector3 dockMin, dockMax, ledgeMin, ledgeMax, platformMax;
            public List<Pass> passes = new List<Pass>();
            public List<Edge> platformEdge = new List<Edge>();
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var audit = new Audit
            {
                radius = diver.radius, height = diver.height, stepOffset = diver.stepOffset,
                walkSpeed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue
            };
            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in All<Collider>(scene).Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();
                var dock = Find(scene, "Dock_Town").bounds;
                var ledge = Find(scene, "Shore_Ledge").bounds;
                var platform = Find(scene, "Beach_Platform").bounds;
                audit.dockMin = dock.min; audit.dockMax = dock.max; audit.ledgeMin = ledge.min; audit.ledgeMax = ledge.max;
                audit.platformMax = platform.max;

                // Four players leave the beach gate for the jetty end, each to its own spot on the 2 m pier.
                var gate = new Vector2(0f, -13.5f);
                var outbound = new List<(string, Vector2[])>();
                for (var i = 0; i < 4; i++)
                {
                    var start = gate + new Vector2(-1.5f + i * 1f, 0f);
                    var spot = new Vector2(dock.center.x + (i % 2 == 0 ? -0.45f : 0.45f), dock.max.z - 0.5f - (i / 2) * 1.0f);
                    outbound.Add(("out-" + i, new[] { start, new Vector2(7.5f + i * 0.4f, -12.6f), new Vector2(spot.x, -9f), spot }));
                }
                audit.passes.Add(Walk("four-out", outbound, diver, audit.walkSpeed));

                // Two come back while two go out on the same narrow walkway.
                var mixed = new List<(string, Vector2[])>
                {
                    ("back-0", new[] { new Vector2(dock.center.x - 0.45f, dock.max.z - 0.5f), new Vector2(dock.center.x - 0.45f, -9f), new Vector2(7.6f, -12.6f), gate }),
                    ("back-1", new[] { new Vector2(dock.center.x + 0.45f, dock.max.z - 1.5f), new Vector2(dock.center.x + 0.45f, -9f), new Vector2(8.4f, -12.6f), gate + Vector2.right }),
                    ("out-2", new[] { gate + new Vector2(-1f, 0f), new Vector2(8.0f, -12.6f), new Vector2(dock.center.x - 0.45f, -9f), new Vector2(dock.center.x - 0.45f, dock.max.z - 0.5f) }),
                    ("out-3", new[] { gate + new Vector2(2f, 0f), new Vector2(8.8f, -12.6f), new Vector2(dock.center.x + 0.45f, -9f), new Vector2(dock.center.x + 0.45f, dock.max.z - 1.5f) })
                };
                audit.passes.Add(Walk("two-back-two-out", mixed, diver, audit.walkSpeed));

                // #297 option A: walk off the beach straight north across the seam onto the ramp, then up to the ledge.
                foreach (var x in new[] { 5.0f, 5.5f, 6.0f, 6.5f })
                    audit.passes.Add(Walk("beach-to-ramp-side-x" + x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                        new List<(string, Vector2[])> { ("a", new[] { new Vector2(x, -13f), new Vector2(x, -9.8f), new Vector2(8.6f, -9.2f) }) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("four-beach-to-ramp-side", new List<(string, Vector2[])>
                {
                    ("s0", new[] { new Vector2(5.2f, -13.2f), new Vector2(5.2f, -9.9f), new Vector2(8.3f, -9.6f) }),
                    ("s1", new[] { new Vector2(6.2f, -13.2f), new Vector2(6.2f, -10.1f), new Vector2(9.1f, -9.0f) }),
                    ("s2", new[] { new Vector2(5.2f, -14.4f), new Vector2(5.2f, -10.2f), new Vector2(8.0f, -8.4f) }),
                    ("s3", new[] { new Vector2(6.2f, -14.4f), new Vector2(6.2f, -10.4f), new Vector2(10.3f, -10.4f) })
                }, diver, audit.walkSpeed));
                // And back: from the ledge down the ramp's upper part and up the apron onto the beach.
                audit.passes.Add(Walk("ramp-side-to-beach", new List<(string, Vector2[])> { ("a", new[] { new Vector2(8.6f, -9.2f), new Vector2(5.5f, -9.8f), new Vector2(5.5f, -13f) }) }, diver, audit.walkSpeed));

                for (var x = 0f; x <= 12.01f; x += 0.5f)
                {
                    var z = platform.max.z;
                    var inside = Hit(new Vector3(x, 20f, z - 0.3f), out _);
                    var outside = Hit(new Vector3(x, 20f, z + 0.3f), out var name);
                    audit.platformEdge.Add(new Edge { x = x, z = z, platformY = inside, outsideY = outside, outsideCollider = name, step = inside - outside });
                }

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P297_SHORE_DOCK_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Also used by P297CradleShiftAudit.
        internal static Pass Walk(string id, List<(string Id, Vector2[] Path)> routes, CharacterController template, float speed)
        {
            var pass = new Pass { id = id };
            var walkers = new List<(Walker Report, CharacterController Cc, Vector2[] Path)>();
            foreach (var (walkerId, path) in routes)
            {
                var go = new GameObject("Audit_P297_" + walkerId);
                var cc = go.AddComponent<CharacterController>();
                cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
                cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
                var groundY = Hit(new Vector3(path[0].x, 20f, path[0].y), out _);
                go.transform.position = new Vector3(path[0].x, groundY + template.skinWidth, path[0].y);
                walkers.Add((new Walker { id = walkerId, start = go.transform.position, minClearance = float.MaxValue }, cc, path));
            }
            Physics.SyncTransforms();
            try
            {
                var next = new int[walkers.Count];
                for (var i = 0; i < next.Length; i++) next[i] = 1;
                var frames = 0;
                const int budget = 1500; // 30 s
                while (frames < budget && Enumerable.Range(0, walkers.Count).Any(i => next[i] < walkers[i].Path.Length))
                {
                    for (var i = 0; i < walkers.Count; i++)
                    {
                        var (report, cc, path) = walkers[i];
                        if (next[i] >= path.Length) continue;
                        var here = new Vector2(cc.transform.position.x, cc.transform.position.z);
                        var step = Vector2.ClampMagnitude(path[next[i]] - here, speed * Dt);
                        cc.Move(new Vector3(step.x, -2f * Dt, step.y));
                        var moved = Vector2.Distance(here, new Vector2(cc.transform.position.x, cc.transform.position.z));
                        if (moved < step.magnitude * 0.5f) report.blockedFrames++;
                        report.maxY = Mathf.Max(report.maxY, cc.transform.position.y);
                        for (var j = 0; j < walkers.Count; j++)
                            if (j != i) report.minClearance = Mathf.Min(report.minClearance,
                                Vector3.Distance(cc.transform.position, walkers[j].Cc.transform.position) - 2f * template.radius);
                        if (Vector2.Distance(new Vector2(cc.transform.position.x, cc.transform.position.z), path[next[i]]) < 0.2f)
                        {
                            next[i]++;
                            if (next[i] >= path.Length) { report.arrived = true; report.seconds = frames * Dt; }
                        }
                    }
                    frames++;
                }
                foreach (var (report, cc, _) in walkers) { report.end = cc.transform.position; pass.walkers.Add(report); }
                pass.allArrived = walkers.All(w => w.Report.arrived);
                pass.seconds = frames * Dt;
            }
            finally
            {
                foreach (var w in walkers) Object.DestroyImmediate(w.Cc.gameObject);
                Physics.SyncTransforms();
            }
            return pass;
        }

        private static Collider Find(Scene scene, string name) =>
            All<Collider>(scene).First(c => c.name == name);

        private static float Hit(Vector3 from, out string name)
        {
            name = "(none)";
            if (!Physics.Raycast(from, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)) return float.NaN;
            name = hit.collider.name;
            return hit.point.y;
        }

        internal static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
