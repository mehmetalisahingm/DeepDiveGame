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
    // #302 (Utku): read-only walk of the west shore walk on the real scene with the real diver controller: beach gate to
    // the terrace and back, two walkers passing (one up, one down), four at once (two each way), and walkers pushing at
    // every rail. Walkers keep to the right and steer straight at their points; they never give way.
    public static class P302WestShoreAudit
    {
        public const string OutputPath = "docs/reports/evidence/p302-west-shore/west-shore.json";
        private const float Dt = 0.02f;

        [Serializable] public sealed class Walker { public string id; public Vector3 start, end; public bool arrived; public float seconds, metres; public int blockedFrames; public float minClearance, lowestY; }
        [Serializable] public sealed class Pass { public string id; public bool allArrived; public float seconds; public List<Walker> walkers = new List<Walker>(); }
        [Serializable] public sealed class Push { public string id; public Vector3 from, end; public bool heldByFence, fell; public string fence; }
        [Serializable] public sealed class Audit
        {
            public string note = "Real scene, real controller values, scene closed unsaved. Walkers keep right and never give way.";
            public float radius, walkSpeed, walkWidth, rampSlopeDegrees, terraceRise;
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
                walkWidth = WestShoreWalk.WalkMaxX - WestShoreWalk.WalkMinX,
                rampSlopeDegrees = WestShoreWalk.RampSlopeDegrees,
                terraceRise = WestShoreWalk.TerraceY - WestShoreWalk.BeachY
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
                // From the beach gate round the repair parts' west end, through the opening, up the ramp, to the rail.
                var up = new[] { new Vector2(0f, -13.5f), new Vector2(-9f, -13.6f), new Vector2(-13.5f, -13.3f), Xz(WestShoreWalk.LandingPoint),
                    Xz(WestShoreWalk.RampFoot), Xz(WestShoreWalk.RampTop), Xz(WestShoreWalk.TerraceMiddle), Xz(WestShoreWalk.ViewPoint) };
                var down = up.Reverse().ToArray();
                audit.passes.Add(Walk("one-up", new List<(string, Vector2[])> { ("a", up) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("one-down", new List<(string, Vector2[])> { ("a", down) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("two-pass-up-and-down", new List<(string, Vector2[])> { ("up", KeepRight(up, 0.6f)), ("down", KeepRight(down, 0.6f)) }, diver, audit.walkSpeed));
                audit.passes.Add(Walk("four-two-each-way", new List<(string, Vector2[])>
                {
                    ("up-0", KeepRight(up, 0.45f)), ("up-1", Delay(EndAt(KeepRight(up, 1.05f), new Vector2(-17.6f, 1.0f)), up[0] + new Vector2(1.5f, 0.8f))),
                    ("down-0", KeepRight(down, 0.45f)), ("down-1", Delay(KeepRight(down, 1.05f), down[0] + new Vector2(-1.2f, 0.6f)))
                }, diver, audit.walkSpeed));

                foreach (var (id, from, to) in new[]
                         {
                             ("landing-to-south", Xz(WestShoreWalk.LandingPoint), new Vector2(-16.5f, -18f)),
                             ("landing-to-west", Xz(WestShoreWalk.LandingPoint), new Vector2(-23f, -13.25f)),
                             ("ramp-to-west", new Vector2(-16.5f, -8f), new Vector2(-23f, -8f)),
                             ("ramp-to-east", new Vector2(-16.5f, -8f), new Vector2(-10f, -8f)),
                             ("terrace-to-west", Xz(WestShoreWalk.TerraceMiddle), new Vector2(-25f, -1f)),
                             ("terrace-to-north", Xz(WestShoreWalk.TerraceMiddle), new Vector2(-17.5f, 7f)),
                             ("terrace-to-east-sea", Xz(WestShoreWalk.TerraceMiddle), new Vector2(-10f, -1f)),
                             ("terrace-south-west", Xz(WestShoreWalk.TerraceMiddle), new Vector2(-19.5f, -8f))
                         })
                    audit.pushes.Add(PushAt(id, from, to, diver, audit.walkSpeed));

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllText(OutputPath, JsonUtility.ToJson(audit, true));
                Debug.Log("P302_WEST_SHORE_WRITTEN " + OutputPath);
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

        // The same path with its last point moved (a second walker stops at its own spot on the terrace).
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
                var go = new GameObject("Audit_P302_" + walkerId);
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
