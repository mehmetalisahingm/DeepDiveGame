using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Network;
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
    // the beach gate (where the town comes down onto the sand) and from the wade top (where a swimmer comes ashore) to each free part.
    // Since Mehmet's #186 decision every free part lies on dry land: the engine left the wade slope, so none needs a swim (#188/#156).
    public sealed class BoatPartReachabilityTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        // Lift: a walking diver steps over ankle-high ground changes (the slope of the wade shelf), so the swept capsule starts above them; anything taller (a 1.2 m barrier) still blocks.
        private const float Cell = 0.5f, Radius = 0.35f, Height = 1.8f, MaxStep = 0.6f, Lift = 0.5f;
        // The SwimVolume's top is y 8.0; dry sand starts above it (P4RouteAudit and P4EngineShoreAudit use the same 8.01).
        private const float DryY = 8.01f;
        // Beach_Wade's dry top, on the slope just north of the platform edge: where a swimmer who climbed the shelf stands.
        private static readonly Vector2 WadeTop = new Vector2(-6.5f, -10.75f);

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

        private Vector2 Gate()
        {
            var gate = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Beach_TownGate");
            return new Vector2(gate.position.x, gate.position.z);
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
        // minGround keeps the walk on ground at or above that height (the dry walk never steps below the water line).
        private static HashSet<Vector2Int> Walk(Vector3 start, out Dictionary<Vector2Int, float> heights, float minGround = float.MinValue)
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
                        if (!Ground(n.x * Cell, n.y * Cell, out var ny) || ny < minGround || Mathf.Abs(ny - heights[c]) > MaxStep) continue;
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

        // #186: every free part lies on dry land - on a walk surface above the water line, outside the SwimVolume, in the Coast zone -
        // so finding one never needs a swim, a dive or a Surface-classified diver.
        [Test]
        public void EveryFreePartLiesOnDryLand()
        {
            Assert.IsTrue(WorldZoneField.TryFind(out var zones));
            var swim = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SwimVolume>(true)).Single().GetComponent<Collider>();
            var parts = Parts();
            Assert.AreEqual(3, parts.Length);
            foreach (var part in parts)
            {
                var at = part.WorldPosition;
                Assert.IsTrue(Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var floor, 4f, ~0, QueryTriggerInteraction.Ignore),
                    part.PartId + " has no ground under it");
                Assert.GreaterOrEqual(floor.point.y, DryY, $"{part.PartId} at {at} lies on {floor.collider.name} below the water line");
                Assert.AreEqual(floor.point.y, at.y, 0.01f, $"{part.PartId} must rest on {floor.collider.name}, not float or sink");
                Assert.Greater((swim.ClosestPoint(at) - at).sqrMagnitude, 1e-6f, $"{part.PartId} at {at} is inside the SwimVolume");
                Assert.AreEqual(PlayerZone.Coast, zones.ResolveZone(at), $"{part.PartId} at {at} is not on the coast");
            }
        }

        [Test]
        public void EveryFreePartCanBeWalkedUpToFromTheBeachGateWithTheTownBuilt() => AllPartsReachedFrom(Gate(), "the beach gate");

        [Test]
        public void EveryFreePartCanBeWalkedUpToFromTheWadeTopWithTheTownBuilt() => AllPartsReachedFrom(WadeTop, "the wade top");

        // Dry the whole way: the walk only steps on cells above the water line, so a part it reaches needs no swim.
        private void AllPartsReachedFrom(Vector2 start, string label)
        {
            var parts = Parts();
            Assert.AreEqual(3, parts.Length);
            Assert.IsTrue(Ground(start.x, start.y, out var y), label + " has no ground");
            var reached = DryWalk(new Vector3(start.x, y, start.y));
            Assert.IsTrue(reached.Count > 200, $"the walk from {label} found almost no ground: " + reached.Count);
            foreach (var part in parts)
                Assert.IsTrue(Reached(reached, part.WorldPosition, 1.5f),
                    $"{part.PartId} at {part.WorldPosition} is not within pickup range of any dry cell reachable from {label}");
        }

        private static HashSet<Vector2Int> DryWalk(Vector3 start)
        {
            return Walk(start, out _, DryY);
        }

        // #186/#156: a campaign saved before the move holds 2/3 with the engine missing. Saves carry part ids only - never a position - so the
        // same save opens onto the moved anchor: the guide names the engine, points from the beach gate at where it now lies, a crew walks
        // there on dry land, and finding it completes the repair exactly once.
        [Test]
        public void ACampaignSavedWithoutTheEngineFindsItOnDryLandAtItsNewPlace()
        {
            CollectionAssert.IsEmpty(typeof(EconomySaveData).GetFields().Where(f => f.FieldType == typeof(Vector3) || f.FieldType == typeof(Vector2))
                .Select(f => f.Name).ToArray(), "the save must stay position-free");

            var root = new GameObject("engine-shore-save");
            try
            {
                root.AddComponent<InventoryManager>();
                var economy = root.AddComponent<EconomyManager>();
                Assert.IsTrue(economy.TryRestore(new EconomySaveData
                {
                    BoatPartIds = new List<string> { BoatRepairParts.Hull, BoatRepairParts.FuelTank }
                }));
                Assert.AreEqual(BoatRepairStatus.InProgress, economy.BoatRepair.Status);
                var mask = economy.BoatRepair.CompletedPartIds.Aggregate(0, (m, id) => m | BoatRepairGuide.Bit(id));
                Assert.AreEqual(new[] { BoatRepairParts.Engine }, BoatRepairGuide.Missing(mask).ToArray());

                var engine = Parts().Single(p => p.PartId == BoatRepairParts.Engine).WorldPosition;
                var gate = Gate();
                var line = BoatRepairGuide.Line(BoatRepairParts.Engine, gate.x, gate.y, engine.x, engine.z);
                Assert.AreEqual($"Motor: {BoatRepairGuide.Compass(engine.x - gate.x, engine.z - gate.y)}, " +
                                $"{Mathf.RoundToInt(Vector2.Distance(gate, new Vector2(engine.x, engine.z)))} m", line);
                Assert.IsTrue(Ground(gate.x, gate.y, out var y));
                Assert.IsTrue(Reached(DryWalk(new Vector3(gate.x, y, gate.y)), engine, 1.5f), "the engine is not walkable on dry land from the gate");

                Assert.IsTrue(economy.TryContributeBoatPart(new PlayerId(1), BoatRepairParts.Engine, BoatPartSource.Found, 1).Accepted);
                Assert.AreEqual(BoatRepairStatus.Repaired, economy.BoatRepair.Status);
                Assert.AreEqual("AlreadyProcessed",
                    economy.TryContributeBoatPart(new PlayerId(2), BoatRepairParts.Engine, BoatPartSource.Found, 1).ReasonCode);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
