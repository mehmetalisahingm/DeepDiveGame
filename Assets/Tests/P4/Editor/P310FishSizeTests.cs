using System.Linq;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #310: the visible size follows the host's weight, the same rule for every species.
    public sealed class P310FishSizeTests
    {
        private static SpeciesDefinition[] AllSpecies() =>
            AssetDatabase.FindAssets("t:SpeciesDefinition", new[] { "Assets/DeepDive/World/Fish/Species" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

        [Test]
        public void TheMidWeightIsTheModelsOwnSizeAndHeavierIsBigger()
        {
            foreach (var species in AllSpecies())
            {
                var w = species.Weight;
                var mid = (w.MinGrams + w.MaxGrams) / 2;
                Assert.AreEqual(1f, FishSize.ScaleFor(mid, w), 0.01f, species.SpeciesId);
                var previous = 0f;
                for (var grams = w.MinGrams; grams <= w.MaxGrams; grams += Mathf.Max(1, (w.MaxGrams - w.MinGrams) / 20))
                {
                    var scale = FishSize.ScaleFor(grams, w);
                    Assert.Greater(scale, previous, species.SpeciesId + " at " + grams);
                    previous = scale;
                }
            }
        }

        [Test]
        public void LengthGrowsWithTheCubeRootOfWeight()
        {
            var range = new WeightRange(900, 2600);
            // 2.744 times the weight is 1.4 times the length, inside the clamp.
            var wide = new WeightRange(500, 1500);
            Assert.AreEqual(1.4f, FishSize.ScaleFor(1715, wide) / FishSize.ScaleFor(625, wide), 0.01f);
            var smallest = FishSize.ScaleFor(range.MinGrams, range);
            var largest = FishSize.ScaleFor(range.MaxGrams, range);
            Assert.AreEqual(Mathf.Pow(2600f / 900f, 1f / 3f), largest / smallest, 0.01f);
        }

        [Test]
        public void EverySpeciesStaysInsideAReadableBand()
        {
            foreach (var species in AllSpecies())
            {
                var w = species.Weight;
                Assert.GreaterOrEqual(FishSize.ScaleFor(w.MinGrams, w), 0.75f, species.SpeciesId);
                Assert.LessOrEqual(FishSize.ScaleFor(w.MaxGrams, w), 1.2f, species.SpeciesId);
            }
        }

        [Test]
        public void NoWeightMeansTheModelsOwnSize()
        {
            Assert.AreEqual(1f, FishSize.ScaleFor(0, new WeightRange(900, 2600)));
            Assert.AreEqual(1f, FishSize.ScaleFor(1200, new WeightRange(0, 0)));
        }
    }
}
