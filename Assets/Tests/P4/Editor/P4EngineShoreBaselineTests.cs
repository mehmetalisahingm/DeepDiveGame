using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #186/#188: locks the engine-shore evidence written by DeepDive.Editor.P4EngineShoreAudit against the live scene. The chosen spot is
    // read from the evidence file, not copied here, and its recorded walks are driven again with the real CharacterController
    // (ContinuousTownWalkTests.Drive). Moving a part or the coast fails this test: re-run the audit and update docs/reports/P4-ENGINE-SHORE.md.
    // EditMode geometry only - not a 1/2/4-process or playtest proof.
    public sealed class P4EngineShoreBaselineTests
    {
        private const string EvidencePath = "docs/reports/evidence/p4-engine-shore/engine-shore.json";
        private const float Radius = 0.35f;
        private const float Height = 1.8f;
        private const float Skin = 0.08f;

        [Serializable] private sealed class Evidence
        {
            public Vector3 hull, fuelTank;
            public Candidate[] ranked;
            public string chosen;
            public string[] alternatives;
        }

        [Serializable] private sealed class Candidate
        {
            public string id;
            public Vector3 position;
            public bool pickupHitsPart, walkable;
            public Walk[] walks;
        }

        [Serializable] private sealed class Walk
        {
            public string from;
            public Vector2[] waypoints;
            public bool reached, fell, stuck, enteredSea;
        }

        private static Evidence Load()
        {
            Assert.IsTrue(File.Exists(EvidencePath), EvidencePath + " missing: run DeepDive.Editor.P4EngineShoreAudit.Run");
            var evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(EvidencePath));
            Assert.IsNotNull(evidence.ranked);
            return evidence;
        }

        private static Candidate Chosen(Evidence evidence)
        {
            var chosen = evidence.ranked.SingleOrDefault(c => c.id == evidence.chosen);
            Assert.IsNotNull(chosen, "the chosen spot " + evidence.chosen + " is not in the ranked table");
            return chosen;
        }

        private static IEnumerable<BoatPartAnchor> Parts(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BoatPartAnchor>(true));

        [Test]
        public void TheEngineAnchorStandsOnTheChosenSpotAndTheOtherPartsDidNotMove()
        {
            var evidence = Load();
            var chosen = Chosen(evidence);
            ContinuousTownScene.With(scene =>
            {
                var parts = Parts(scene).ToDictionary(p => p.PartId, p => p.WorldPosition);
                Assert.Less(Vector3.Distance(chosen.position, parts[BoatRepairParts.Engine]), 0.01f, "the engine is not on the audited spot");
                Assert.Less(Vector3.Distance(evidence.hull, parts[BoatRepairParts.Hull]), 0.01f, "the hull moved since the audit");
                Assert.Less(Vector3.Distance(evidence.fuelTank, parts[BoatRepairParts.FuelTank]), 0.01f, "the fuel tank moved since the audit");
            });
        }

        [Test]
        public void TheChosenSpotAndBothAlternativesWereProvenAndAreDistinct()
        {
            var evidence = Load();
            Assert.AreEqual(2, evidence.alternatives.Length, "the report names two alternatives");
            var picked = new[] { evidence.chosen }.Concat(evidence.alternatives).Select(id => evidence.ranked.Single(c => c.id == id)).ToArray();
            foreach (var c in picked)
            {
                Assert.IsTrue(c.pickupHitsPart, c.id + ": the 2.5 m eye ray did not reach the part");
                Assert.IsTrue(c.walkable, c.id + " was not walkable");
                Assert.AreEqual(3, c.walks.Length, c.id);
                foreach (var w in c.walks)
                    Assert.IsTrue(w.reached && !w.fell && !w.stuck && !w.enteredSea, c.id + " from " + w.from);
            }
            for (var i = 0; i < picked.Length; i++)
                for (var j = i + 1; j < picked.Length; j++)
                    Assert.GreaterOrEqual(Vector3.Distance(picked[i].position, picked[j].position), 1.5f, picked[i].id + " / " + picked[j].id);
        }

        // Without the S1 stand-in cradle (a proposal, not in the scene) the recorded paths only get easier, so they must still walk.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TheRecordedWalkToTheEngineStillWalksWithTheRealController(int index)
        {
            var walk = Chosen(Load()).walks[index];
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldZoneField.TryFind(out var zones));
                var diver = new GameObject("Test_EngineShore_" + index);
                SceneManager.MoveGameObjectToScene(diver, scene);
                try
                {
                    var controller = diver.AddComponent<CharacterController>();
                    controller.height = Height;
                    controller.radius = Radius;
                    controller.center = new Vector3(0f, Height * 0.5f, 0f);
                    controller.slopeLimit = 45f;
                    controller.stepOffset = 0.3f;
                    controller.skinWidth = Skin;
                    Physics.SyncTransforms();

                    var failures = new List<string>();
                    ContinuousTownWalkTests.Drive(controller, zones, walk.waypoints, failures, out var reachedSea);
                    Assert.IsEmpty(failures, walk.from + ": " + string.Join("\n", failures.Take(20)));
                    Assert.IsFalse(reachedSea, walk.from + " entered the sea");
                    var end = walk.waypoints[walk.waypoints.Length - 1];
                    Assert.Less(Vector2.Distance(end, new Vector2(diver.transform.position.x, diver.transform.position.z)), 0.2f, walk.from + " did not arrive");
                }
                finally { Object.DestroyImmediate(diver); }
            }, furnitureProxies: true);
        }
    }
}
