using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    // #156: a three-player crew reached 2/3 repair parts and could not find the third. The parts are authored scenery (Utku), but the P4.6 town grew
    // new slabs, fills and edge barriers around them, so "reachable on foot" has to be re-proved against the scene the player really gets: the
    // coast scene with ContinuousTownLayout built on top of it (that is what runs on sceneLoaded). This walks a capsule over the real colliders from
    // the wade shelf, the only place a swimmer comes ashore, to each free part.
    public sealed class BoatPartReachabilityTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        // Lift: a walking diver steps over ankle-high ground changes (the slope of the wade shelf), so the swept capsule starts above them; anything taller (a 1.2 m barrier) still blocks.
        private const float Cell = 0.5f, Radius = 0.35f, Height = 1.8f, MaxStep = 0.6f, Lift = 0.5f;

        private Scene scene;
        private Scene previous;
        private bool opened;

        [SetUp]
        public void Open()
        {
            previous = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            opened = !already.IsValid() || !already.isLoaded;
            scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            SceneManager.SetActiveScene(scene);
            ContinuousTownLayout.Build(scene);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Close()
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private BoatPartAnchor[] Parts() => scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoatPartAnchor>(true)).ToArray();

        private static bool Ground(float x, float z, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(x, 14f, z), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore)) return false;
            y = hit.point.y;
            return true;
        }

        private static bool Free(Vector3 feet) =>
            !Physics.CheckCapsule(feet + Vector3.up * (Radius + Lift), feet + Vector3.up * (Height - Radius), Radius, ~0, QueryTriggerInteraction.Ignore);

        // Breadth-first walk over a 0.5 m grid; a step is allowed when the ground changes by at most MaxStep and the capsule sweeps clean.
        private static HashSet<Vector2Int> Walk(Vector3 start, out Dictionary<Vector2Int, float> heights)
        {
            heights = new Dictionary<Vector2Int, float>();
            var seen = new HashSet<Vector2Int>();
            Vector2Int Key(float x, float z) => new Vector2Int(Mathf.RoundToInt(x / Cell), Mathf.RoundToInt(z / Cell));
            var first = Key(start.x, start.z);
            if (!Ground(first.x * Cell, first.y * Cell, out var y0)) return seen;
            heights[first] = y0; seen.Add(first);
            var queue = new Queue<Vector2Int>(); queue.Enqueue(first);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                var here = new Vector3(c.x * Cell, heights[c], c.y * Cell);
                for (var dx = -1; dx <= 1; dx++)
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var n = new Vector2Int(c.x + dx, c.y + dz);
                        if (seen.Contains(n) || n.x * Cell < -30f || n.x * Cell > 30f || n.y * Cell < -60f || n.y * Cell > 6f) continue;
                        if (!Ground(n.x * Cell, n.y * Cell, out var ny) || Mathf.Abs(ny - heights[c]) > MaxStep) continue;
                        var there = new Vector3(n.x * Cell, ny, n.y * Cell);
                        if (!Free(there)) continue;
                        var dir = there - here; dir.y = 0f;
                        if (Physics.CapsuleCast(here + Vector3.up * (Radius + Lift), here + Vector3.up * (Height - Radius), Radius * 0.9f, dir.normalized,
                            dir.magnitude, ~0, QueryTriggerInteraction.Ignore)) continue;
                        seen.Add(n); heights[n] = ny; queue.Enqueue(n);
                    }
            }
            return seen;
        }

        private static bool Reached(HashSet<Vector2Int> reached, Vector3 point, float within)
        {
            var r = Mathf.CeilToInt(within / Cell);
            var cx = Mathf.RoundToInt(point.x / Cell); var cz = Mathf.RoundToInt(point.z / Cell);
            for (var dx = -r; dx <= r; dx++)
                for (var dz = -r; dz <= r; dz++)
                    if (reached.Contains(new Vector2Int(cx + dx, cz + dz)) && new Vector2(dx * Cell, dz * Cell).magnitude <= within) return true;
            return false;
        }

        [Test]
        public void NoFreePartIsBuriedInsideSolidTownGeometry()
        {
            foreach (var part in Parts())
            {
                // The part lies ON some ground (the beach strip, or the wade slope); anything else overlapping it is geometry the town put on top of it.
                Physics.Raycast(part.WorldPosition + Vector3.up * 2f, Vector3.down, out var floor, 4f, ~0, QueryTriggerInteraction.Ignore);
                var hits = Physics.OverlapSphere(part.WorldPosition + Vector3.up * 0.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => h != floor.collider && !h.transform.IsChildOf(part.transform)).Select(h => h.name).ToArray();
                Assert.IsEmpty(hits, $"{part.PartId} at {part.WorldPosition} sits inside: " + string.Join(", ", hits));
            }
        }

        [Test]
        public void EveryFreePartCanBeWalkedUpToFromTheWadeShelfWithTheTownBuilt()
        {
            var parts = Parts();
            Assert.AreEqual(3, parts.Length);
            var engine = parts.First(p => p.PartId == BoatRepairParts.Engine);
            var reached = Walk(engine.WorldPosition, out _);
            Assert.IsTrue(reached.Count > 200, "the shelf walk found almost no ground: " + reached.Count);
            foreach (var part in parts)
                Assert.IsTrue(Reached(reached, part.WorldPosition, 1.5f),
                    $"{part.PartId} at {part.WorldPosition} is not within pickup range of any walkable cell reachable from the wade shelf");
        }
    }
}
