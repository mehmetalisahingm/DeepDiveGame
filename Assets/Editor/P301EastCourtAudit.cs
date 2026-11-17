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
    // #301 (Utku): read-only walk of the east neighbourhood on the real scene with the real diver controller: the loop
    // both ways, two walkers meeting head-on, four (two each way) at once, and walkers pushing at the outer fences.
    // Walkers keep to the right of their lane and steer straight at their points; they never give way.
    public static class P301EastCourtAudit
    {
        public const string OutputPath = "docs/reports/evidence/p301-east-court/east-court.json";
        private const float Dt = 0.02f;

        [Serializable] public sealed class Walker { public string id; public Vector3 start, end; public bool arrived; public float seconds, metres; public int blockedFrames; public float minClearance, lowestY; }
        [Serializable] public sealed class Pass { public string id; public bool allArrived; public float seconds; public List<Walker> walkers = new List<Walker>(); }
        [Serializable] public sealed class Push { public string id; public Vector3 from, end; public bool heldByFence, fell; public string fence; }
        [Serializable] public sealed class Audit
        {
            public string note = "Real scene, real controller values, scene closed unsaved. Walkers keep right and never give way.";
            public float radius, walkSpeed, laneWidth, courtWidth;
            public List<Pass> passes = new List<Pass>();
            public List<Push> pushes = new List<Push>();
        }

        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(P4RouteAudit.DiverPath);
            var diver = prefab.GetComponent<CharacterController>();
            var audit = new Audit
            {
                radius = diver.radius,
                walkSpeed = new SerializedObject(prefab.GetComponent<NetworkPlayer>()).FindProperty("walkSpeed").floatValue,
                laneWidth = ContinuousTownLayout.EastCourt.LaneNorthMaxZ - ContinuousTownLayout.EastCourt.LaneNorthMinZ,
                courtWidth = ContinuousTownLayout.EastCourt.CourtMaxX - ContinuousTownLayout.EastCourt.CourtMinX
            };
            var scene = EditorSceneManager.OpenScene(P4RouteAudit.ScenePath, OpenSceneMode.Additive);
            try
            {
                ContinuousTownLayout.Build(scene);
                Physics.SyncTransforms();
                foreach (var moving in All<Collider>(scene).Where(c => c.GetComponentInParent<Unity.Netcode.NetworkObject>() != null))
                    moving.enabled = false;
                Physics.SyncTransforms();

                var loop = new[] { new Vector3(9f, 0f, -22.5f) }.Concat(ContinuousTownLayout.EastCourt.Loop).Concat(new[] { new Vector3(9f, 0f, -33.75f) })
                    .Select(p => new Vector2(p.x, p.z)).ToArray();
                var back = loop.Reverse().ToArray();
                audit.passes.Add(Walk("one-out-north-back-south", new List<(string, Vector2[])> { ("a", loop) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("one-out-south-back-north", new List<(string, Vector2[])> { ("a", back) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("two-meet-head-on", new List<(string, Vector2[])> { ("cw", KeepRight(loop, 0.7f)), ("ccw", KeepRight(back, 0.7f)) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("four-two-each-way", new List<(string, Vector2[])>
                {
                    ("cw-0", KeepRight(loop, 0.45f)), ("cw-1", Delay(KeepRight(loop, 1.0f), loop[0] + new Vector2(-1.2f, 0.9f))),
                    ("ccw-0", KeepRight(back, 0.45f)), ("ccw-1", Delay(KeepRight(back, 1.0f), back[0] + new Vector2(-1.2f, -0.9f)))
                }, diver, audit.walkSpeed));

                // Walk straight at the void beyond each outer fence.
                foreach (var (id, from, to) in new[]
                         {
                             ("north-lane-to-north", new Vector2(18f, -22.5f), new Vector2(18f, -17f)),
                             ("court-to-east", new Vector2(24f, -28f), new Vector2(31f, -28f)),
                             ("south-lane-to-south", new Vector2(18f, -33.75f), new Vector2(18f, -39f)),
                             ("court-corner-north-east", new Vector2(24f, -23f), new Vector2(29f, -18f))
                         })
                    audit.pushes.Add(PushAt(id, from, to, diver, audit.walkSpeed));

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P301_EAST_COURT_WRITTEN " + OutputPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // The same path shifted sideways to the walker's right by `offset` metres on every leg.
        private static Vector2[] KeepRight(Vector2[] path, float offset)
        {
            var shifted = new Vector2[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                var a = path[Mathf.Max(0, i - 1)];
                var b = path[Mathf.Min(path.Length - 1, i + 1)];
                var dir = (b - a).normalized;
                shifted[i] = path[i] + new Vector2(dir.y, -dir.x) * offset;
            }
            return shifted;
        }

        // A second walker starting a little behind the first.
        private static Vector2[] Delay(Vector2[] path, Vector2 start) => new[] { start }.Concat(path).ToArray();

        private static Push PushAt(string id, Vector2 from, Vector2 to, CharacterController template, float speed)
        {
            var pass = Walk(id, new List<(string, Vector2[])> { (id, new[] { from, to }) }, template, speed, 400);
            var w = pass.walkers[0];
            var push = new Push { id = id, from = w.start, end = w.end, fell = w.lowestY < w.start.y - 1f };
            push.heldByFence = !w.arrived && !push.fell;
            if (Physics.Raycast(w.end + Vector3.up * 0.9f, new Vector3(to.x - from.x, 0f, to.y - from.y).normalized, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                push.fence = hit.collider.name;
            return push;
        }

        private static Pass Walk(string id, List<(string Id, Vector2[] Path)> routes, CharacterController template, float speed, int budget = 2500)
        {
            var pass = new Pass { id = id };
            var walkers = new List<(Walker Report, CharacterController Cc, Vector2[] Path)>();
            foreach (var (walkerId, path) in routes)
            {
                var go = new GameObject("Audit_P301_" + walkerId);
                var cc = go.AddComponent<CharacterController>();
                cc.radius = template.radius; cc.height = template.height; cc.center = template.center;
                cc.skinWidth = template.skinWidth; cc.stepOffset = template.stepOffset; cc.slopeLimit = template.slopeLimit;
                var groundY = Hit(new Vector3(path[0].x, 20f, path[0].y));
                go.transform.position = new Vector3(path[0].x, groundY + template.skinWidth, path[0].y);
                walkers.Add((new Walker { id = walkerId, start = go.transform.position, minClearance = float.MaxValue, lowestY = go.transform.position.y }, cc, path));
            }
            Physics.SyncTransforms();
            try
            {
                var next = new int[walkers.Count];
                for (var i = 0; i < next.Length; i++) next[i] = 1;
                var frames = 0;
                while (frames < budget && Enumerable.Range(0, walkers.Count).Any(i => next[i] < walkers[i].Path.Length))
                {
                    for (var i = 0; i < walkers.Count; i++)
                    {
                        var (report, cc, path) = walkers[i];
                        if (next[i] >= path.Length) continue;
                        var here = new Vector2(cc.transform.position.x, cc.transform.position.z);
                        var step = Vector2.ClampMagnitude(path[next[i]] - here, speed * Dt);
                        cc.Move(new Vector3(step.x, -2f * Dt, step.y));
                        var now = new Vector2(cc.transform.position.x, cc.transform.position.z);
                        var moved = Vector2.Distance(here, now);
                        report.metres += moved;
                        report.lowestY = Mathf.Min(report.lowestY, cc.transform.position.y);
                        if (moved < step.magnitude * 0.5f) report.blockedFrames++;
                        for (var j = 0; j < walkers.Count; j++)
                            if (j != i) report.minClearance = Mathf.Min(report.minClearance,
                                Vector3.Distance(cc.transform.position, walkers[j].Cc.transform.position) - 2f * template.radius);
                        if (Vector2.Distance(now, path[next[i]]) < 0.25f)
                        {
                            next[i]++;
                            if (next[i] >= path.Length) { report.arrived = true; report.seconds = frames * Dt; }
                        }
                    }
                    frames++;
                }
                foreach (var (report, cc, _) in walkers)
                {
                    report.end = cc.transform.position;
                    if (report.minClearance == float.MaxValue) report.minClearance = -1f;
                    pass.walkers.Add(report);
                }
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

        private static float Hit(Vector3 from) =>
            Physics.Raycast(from, Vector3.down, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : float.NaN;

        private static IEnumerable<T> All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }
}
