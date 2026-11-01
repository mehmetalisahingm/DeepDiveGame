using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DeepDive.Economy;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #218 data-only milestone. World population and distinctive animated visuals
    // require a separate playable implementation; these tests must not imply five fish spawn.
    public sealed class P411FiveSpeciesDefinitionTests
    {
        private const string Root = "Assets/DeepDive/World/Fish/Species/";

        [Test]
        public void FiveStableSpeciesAssetsHaveUniqueValidIdentities()
        {
            var files = new[] { "SeaBass", "Tuna", "RedSnapper", "Grouper", "Lanternfish" };
            var expectedIds = new[] { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
            var species = files.Select(file => AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(Root + file + ".asset")).ToArray();
            for (var i = 0; i < species.Length; i++)
            {
                Assert.NotNull(species[i], files[i]);
                Assert.AreEqual(expectedIds[i], species[i].SpeciesId);
                Assert.IsTrue(species[i].IsValid(out var error), files[i] + ": " + error);
            }
            Assert.AreEqual(5, species.Select(x => x.SpeciesId).Distinct().Count());
            Assert.AreEqual(2000, species[3].Weight.MinGrams);
            Assert.AreEqual(6000, species[3].Weight.MaxGrams);
            Assert.AreEqual(300, species[4].Weight.MinGrams);
            Assert.AreEqual(900, species[4].Weight.MaxGrams);
        }

        [Test]
        public void NewPricesAreSpeciesSpecificAndLegacyPricesUnchanged()
        {
            var host = new GameObject("P411_species_economy");
            try
            {
                var economy = host.AddComponent<EconomyManager>();
                var field = typeof(EconomyManager).GetField("_priceBySpeciesId", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);
                var prices = (Dictionary<string, int>)field.GetValue(economy);
                Assert.AreEqual(120, prices["sea_bass"]);
                Assert.AreEqual(120, prices["tuna"]);
                Assert.AreEqual(120, prices["red_snapper"]);
                Assert.AreEqual(180, prices["grouper"]);
                Assert.AreEqual(320, prices["lanternfish"]);
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
