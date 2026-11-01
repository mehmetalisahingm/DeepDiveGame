using System.Collections.Generic;
using System.Linq;
using DeepDive.World.Editor;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // This tests the proposed authoring plan, not a claim that the five models
    // have already been imported or that the scene's authoring step was run.
    public sealed class P411FiveSpeciesAuthoringPlanTests
    {
        [Test]
        public void FiveSpeciesEachHaveThreeDedicatedNetworkSceneSlots()
        {
            Assert.AreEqual(15, P411FiveSpeciesSceneSetup.Population);
            Assert.AreEqual(15, P411FiveSpeciesSceneSetup.Homes.Length);
            Assert.AreEqual(5, P411FiveSpeciesSceneSetup.SpeciesIds.Distinct().Count());
            var expected = new[] { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
            CollectionAssert.AreEqual(expected, P411FiveSpeciesSceneSetup.SpeciesIds);
            for (var i = 0; i < 15; i++)
                Assert.AreEqual(expected[i / 3], P411FiveSpeciesSceneSetup.SpeciesForSlot(i));
        }

        [Test]
        public void HomesRemainWithinExistingCoastReefAndDeepWaterColumns()
        {
            var homes = P411FiveSpeciesSceneSetup.Homes;
            for (var i = 0; i < homes.Length; i++)
            {
                Assert.Greater(homes[i].x, -14.75f);
                Assert.Less(homes[i].x, 14.75f);
                Assert.Greater(homes[i].z, -15f);
                Assert.Less(homes[i].z, 65f);
                Assert.Less(homes[i].y, 8f);
                if (i < 6)
                {
                    Assert.GreaterOrEqual(homes[i].y, 0f, "Shallow species y");
                    Assert.Less(homes[i].z, 15f);
                }
                else if (i < 12)
                {
                    Assert.Greater(homes[i].y, -12f, "Reef species depth");
                    Assert.GreaterOrEqual(homes[i].z, 19f);
                    Assert.LessOrEqual(homes[i].z, 40f);
                }
                else
                {
                    Assert.Less(homes[i].y, -12f, "Deep species depth");
                    Assert.GreaterOrEqual(homes[i].z, 46f);
                }
            }
        }
    }
}
