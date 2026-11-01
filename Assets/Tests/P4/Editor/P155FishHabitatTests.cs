using System.Collections.Generic;
using System.Linq;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // P4 #155: going deeper must show a different fish. The three authored species keep their own habitat in the
    // real scene: sea bass on the coast (shallow band), red snapper over the reef shelf (reef band), tuna in open
    // water past the reef edge. Scenery clearance of every wander envelope is P4WadeUndersideTests' job. Not a
    // five-species or 1/2/4-player visibility acceptance.
    public sealed class P155FishHabitatTests
    {
        private const float SurfaceY = DiveDepthBandSet.DefaultSurfaceY;
        // DiveTestAreaReefDeepSetup (editor-only, not referenced by this assembly): reef drop-off, shelf and deep drop-off.
        private const float ReefDropOffStartZ = 15f, ReefShelfStartZ = 19f, DeepDropOffStartZ = 40f;

        [Test]
        public void EachSpeciesKeepsItsOwnHabitat()
        {
            ContinuousTownScene.With(scene =>
            {
                var fish = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true)).ToArray();
                Assert.AreEqual(15, fish.Length);
                var failures = new List<string>();
                var counts = fish.GroupBy(f => f.Species.SpeciesId).ToDictionary(g => g.Key, g => g.Count());
                // Accept only one of two whole-scene layouts during migration.
                // Never allow a partially imported hybrid to pass as five species.
                var fiveSpecies = counts.Count == 5 && P411FiveSpeciesRoster.SpeciesIds.All(id =>
                    counts.TryGetValue(id, out var total) && total == 3);
                var legacy = counts.Count == 3 && new[] { "sea_bass", "tuna", "red_snapper" }.All(id =>
                    counts.TryGetValue(id, out var total) && total == 5);
                Assert.IsTrue(legacy || fiveSpecies, "Expected 5/5/5 legacy or 3/3/3/3/3 complete species scene");
                foreach (var f in fish)
                {
                    var home = f.transform.position;
                    var depth = SurfaceY - home.y;
                    var r = f.Species.Swim.WanderRadius;
                    switch (f.Species.SpeciesId)
                    {
                        case "sea_bass":
                            if (depth + r * 0.5f > DiveDepthBands.ShallowMaxDepth || home.z > ReefDropOffStartZ)
                                failures.Add($"{f.name} sea bass at {home} leaves the coast shallows");
                            break;
                        case "red_snapper":
                            if (depth - r * 0.5f < DiveDepthBands.ReefMinDepth || depth + r * 0.5f > DiveDepthBands.ReefMaxDepth ||
                                home.z - r < ReefShelfStartZ - 1f || home.z > DeepDropOffStartZ)
                                failures.Add($"{f.name} red snapper at {home} is not over the reef shelf in the reef band");
                            break;
                        case "tuna":
                            if (fiveSpecies)
                            {
                                if (depth + r * .5f > DiveDepthBands.ShallowMaxDepth || home.z > ReefDropOffStartZ)
                                    failures.Add($"{f.name} tuna at {home} leaves the newly authored shallow coast");
                            }
                            else
                            {
                                if (depth - r * .5f < DiveDepthBands.ReefMinDepth || home.z - r < DeepDropOffStartZ)
                                    failures.Add($"{f.name} tuna at {home} is not in open water past the reef edge");
                                if (Vector3.Distance(home, DeepEncounterWorld.ArenaPosition) < 12f)
                                    failures.Add($"{f.name} tuna at {home} sits by the boss arena");
                            }
                            break;
                        case "grouper":
                            if (!fiveSpecies || depth - r * .5f < DiveDepthBands.ReefMinDepth ||
                                depth + r * .5f > DiveDepthBands.ReefMaxDepth ||
                                home.z < ReefShelfStartZ || home.z > DeepDropOffStartZ)
                                failures.Add($"{f.name} grouper at {home} leaves the reef band");
                            break;
                        case "lanternfish":
                            if (!fiveSpecies || depth - r * .5f < DiveDepthBands.ReefMaxDepth ||
                                home.z < 46f || home.z >= 65f)
                                failures.Add($"{f.name} lanternfish at {home} leaves the deep basin");
                            if (Vector3.Distance(home, DeepEncounterWorld.ArenaPosition) < 12f)
                                failures.Add($"{f.name} lanternfish at {home} enters the boss arena");
                            break;
                        default:
                            failures.Add($"{f.name} has unexpected species {f.Species.SpeciesId}");
                            break;
                    }
                }
                Assert.AreEqual(fiveSpecies ? 3 : 5, counts["sea_bass"]);
                Assert.AreEqual(fiveSpecies ? 3 : 5, counts["red_snapper"]);
                Assert.AreEqual(fiveSpecies ? 3 : 5, counts["tuna"]);
                if (fiveSpecies)
                {
                    Assert.AreEqual(3, counts["grouper"]);
                    Assert.AreEqual(3, counts["lanternfish"]);
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            }, false);
        }
    }
}
