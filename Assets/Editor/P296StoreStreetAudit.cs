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
    // #296 (Utku): read-only walk from the square and from the beach gate to the equipment store's door on the real scene
    // with the real diver controller: one each way, two meeting, four arriving at the door together, and what stands in
    // the way (colliders within a body width of the straight line). Walkers never give way.
    public static class P296StoreStreetAudit
    {
        public const string OutputPath = "docs/reports/evidence/p296-store-street/store-street.json";
        private const float Dt = 0.02f;

        [Serializable] public sealed class Walker { public string id; public Vector3 start, end; public bool arrived; public float seconds, metres; public int blockedFrames; public float minClearance, lowestY; }
        [Serializable] public sealed class Pass { public string id; public bool allArrived; public float seconds; public List<Walker> walkers = new List<Walker>(); }
        [Serializable] public sealed class Push { public string id; public Vector3 from, end; public bool heldByFence, fell; public string fence; }
        [Serializable] public sealed class Audit
        {
            public string note = "Real scene, real controller values, scene closed unsaved. Walkers keep right and never give way.";
            public float radius, walkSpeed; public List<string> obstaclesNearRoute = new List<string>();
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

                Vector2 Xz(Vector3 p) => new Vector2(p.x, p.z);
                var door = Xz(ContinuousTownLayout.EquipmentStoreDoorOutside);
                var square = new Vector2(0f, -29f);
                var gate = new Vector2(0f, -15.8f);
                // The side street as the layout defines it (#296); the earlier measurement took the corner-hugging line.
                var fromSquare = ContinuousTownLayout.StoreStreet.Select(Xz).ToArray();
                var fromGate = new[] { gate, new Vector2(-5f, -18.2f), door };
                audit.passes.Add(Walk("square-to-door", new List<(string, Vector2[])> { ("a", fromSquare) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("door-to-square", new List<(string, Vector2[])> { ("a", fromSquare.Reverse().ToArray()) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("gate-to-door", new List<(string, Vector2[])> { ("a", fromGate) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("two-meet", new List<(string, Vector2[])> { ("in", KeepRight(fromSquare, 0.5f)), ("out", KeepRight(fromSquare.Reverse().ToArray(), 0.5f)) }, diver, audit.walkSpeed));
                // Four arrive at the door at once: two from the square, two from the beach, each to its own spot in front of it.
                audit.passes.Add(Walk("four-to-door", new List<(string, Vector2[])>
                {
                    ("sq-0", EndAt(fromSquare, door + new Vector2(0.6f, 0.2f))), ("sq-1", Delay(EndAt(KeepRight(fromSquare, 0.8f), door + new Vector2(1.4f, 0.9f)), square + new Vector2(1f, -1f))),
                    ("gate-0", EndAt(fromGate, door + new Vector2(-0.6f, 0.4f))), ("gate-1", Delay(EndAt(KeepRight(fromGate, 0.8f), door + new Vector2(-1.3f, 1.1f)), gate + new Vector2(-1f, 0.4f)))
                }, diver, audit.walkSpeed));
                // Then the four go in through the door one behind the other and come out again.
                var inside = Xz(ContinuousTownLayout.EquipmentStoreDoorInside);
                audit.passes.Add(Walk("in-and-out", new List<(string, Vector2[])> { ("in", new[] { door, inside }), ("out", new[] { inside + new Vector2(0f, -1f), door + new Vector2(0.9f, 0.6f) }) }, diver, audit.walkSpeed));
                foreach (var c in All<Collider>(scene).Where(c => c.enabled && !c.isTrigger))
                {
                    var b = c.bounds;
                    if (b.max.y < ContinuousTownLayout.WalkSurfaceY + 0.1f || b.min.y > ContinuousTownLayout.WalkSurfaceY + 1.9f) continue;
                    foreach (var path in new[] { fromSquare, fromGate })
                        for (var i = 1; i < path.Length; i++)
                            for (var t = 0f; t <= 1f; t += 0.05f)
                            {
                                var p = Vector2.Lerp(path[i - 1], path[i], t);
                                var q = new Vector3(p.x, ContinuousTownLayout.WalkSurfaceY + 0.9f, p.y);
                                if (Vector3.Distance(b.ClosestPoint(q), q) < 0.8f) { var tag = c.name + " " + b.min.ToString("0.0") + ".." + b.max.ToString("0.0"); if (!audit.obstaclesNearRoute.Contains(tag)) audit.obstaclesNearRoute.Add(tag); }
                            }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P296_STORE_STREET_WRITTEN " + OutputPath);
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

        private static Vector2[] EndAt(Vector2[] path, Vector2 end) { var copy = (Vector2[])path.Clone(); copy[copy.Length - 1] = end; return copy; }

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
                var go = new GameObject("Audit_P296_" + walkerId);
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
