using System.Linq;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #310 (Mehmet's starting targets): a mid-weight sea bass, tuna or red snapper is drawn 0.45 m long and a grouper
    // 0.65 m; the lanternfish keeps its model's size.
    public sealed class P310FishLengthTests
    {
        [Test]
        public void MidWeightFishAreDrawnAtTheirSpeciesReferenceLength()
        {
            ContinuousTownScene.With(scene =>
            {
                var fishes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true)).ToArray();
                Assert.AreEqual(15, fishes.Length);
                foreach (var fish in fishes)
                {
                    var reference = fish.Species.ReferenceLengthMetres;
                    var drawn = fish.DrawnLength() * fish.ModelLengthFactor();
                    if (reference > 0f) Assert.AreEqual(reference, drawn, 0.01f, fish.name + " " + fish.Species.SpeciesId);
                    else Assert.AreEqual(1f, fish.ModelLengthFactor(), 1e-4f, fish.name);
                }
                var byId = fishes.GroupBy(f => f.Species.SpeciesId).ToDictionary(g => g.Key, g => g.First().Species.ReferenceLengthMetres);
                Assert.AreEqual(0.45f, byId["sea_bass"], 1e-4f);
                Assert.AreEqual(0.45f, byId["tuna"], 1e-4f);
                Assert.AreEqual(0.45f, byId["red_snapper"], 1e-4f);
                Assert.AreEqual(0.65f, byId["grouper"], 1e-4f);
            }, false);
        }
    }
}
