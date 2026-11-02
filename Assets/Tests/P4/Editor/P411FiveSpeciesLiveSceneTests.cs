using System.Linq;
using DeepDive.World;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #218: Checks the COMMITTED scene and real imported models, not just the
    // future authoring plan. Keep the 15 original NGO scene fish identities.
    public sealed class P411FiveSpeciesLiveSceneTests
    {
        [Test]
        public void AuthoredSceneHasThreeHuntableNetworkFishPerDistinctSpecies()
        {
            ContinuousTownScene.With(scene =>
            {
                var actors = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<FishActor>(true)).ToArray();
                Assert.AreEqual(P411FiveSpeciesRoster.Population, actors.Length);
                foreach (var id in P411FiveSpeciesRoster.SpeciesIds)
                {
                    Assert.AreEqual(3, actors.Count(actor => actor.Species != null &&
                        actor.Species.SpeciesId == id), id + " must own exactly three real scene actors");
                }
                Assert.AreEqual(5, actors.Select(actor => actor.Species.SpeciesId).Distinct().Count());
                var ids = actors.Select(actor =>
                {
                    var network = actor.GetComponent<NetworkObject>();
                    Assert.NotNull(network, actor.name + " is not a scene NetworkObject");
                    Assert.AreEqual(1, actor.GetComponentsInChildren<NetworkObject>(true).Length,
                        actor.name + " must not nest a second network identity");
                    Assert.AreNotEqual(0u, network.GlobalObjectIdHash, actor.name + " lost its authored NGO identity");
                    return network.GlobalObjectIdHash;
                }).ToArray();
                Assert.AreEqual(P411FiveSpeciesRoster.Population, ids.Distinct().Count(),
                    "Duplicate NGO scene identifiers after five-species authoring");
            }, false);
        }

        [Test]
        public void AllFiveSpeciesHaveRealMeshAndAnimatedVisualResources()
        {
            foreach (var key in P411FiveSpeciesRoster.VisualNames)
            {
                var prefab = Resources.Load<GameObject>("SuppliedKit/" + key);
                Assert.NotNull(prefab, key + " visual prefab is not imported");
                Assert.IsTrue(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Any(skin => skin.sharedMesh != null), key + " has no animated mesh");
                Assert.IsTrue(prefab.GetComponentsInChildren<Animation>(true)
                    .Any(animation => animation.clip != null), key + " lacks a real imported swim clip");
                Assert.IsEmpty(prefab.GetComponentsInChildren<NetworkObject>(true),
                    key + " must be cosmetic, never an extra gameplay network fish");
            }
        }
    }
}
