using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class ReefHabitatVisualsTests
    {
        [Test]
        public void SpotsUseExistingReefAndDeepFloorWithoutEnteringFishHabitatRows()
        {
            var spots = ReefHabitatVisuals.Spots;
            Assert.AreEqual(10, spots.Count);
            Assert.AreEqual(6, spots.Count(x => Mathf.Approximately(x.Position.y, -8f)));
            Assert.AreEqual(4, spots.Count(x => Mathf.Approximately(x.Position.y, -26f)));
            foreach (var spot in spots)
            {
                Assert.Greater(spot.Position.x, -14f);
                Assert.Less(spot.Position.x, 14f);
                Assert.IsTrue(spot.Position.z >= 19f && spot.Position.z <= 65f);
                if (Mathf.Approximately(spot.Position.y, -8f))
                {
                    Assert.Less(spot.Position.x, -8f, "Reef visuals must not overwrite the eastern snapper row");
                    Assert.LessOrEqual(spot.Position.z, 40f);
                }
                else
                {
                    Assert.Greater(spot.Position.x, 8f, "Deep visuals must not overwrite the western tuna row");
                    Assert.GreaterOrEqual(spot.Position.z, 46f);
                }
            }
        }

        [Test]
        public void ProposedFiveSpeciesHomesStayClearOfPlacedReefKitCenters()
        {
            // The pending #218 authoring step moves 3 grouper into the WESTERN
            // reef, which the older three-species placement test did not model.
            // This protects home positions, not the full AI wander envelope or
            // rendered-mesh clearance; those still require real gameplay review.
            foreach (var spot in ReefHabitatVisuals.Spots)
                foreach (var home in P411FiveSpeciesRoster.Homes)
                    Assert.Greater(Vector3.Distance(spot.Position, home), 3.0f,
                        $"Fish home {home} would visually overlap {spot.PrefabId} at {spot.Position}");
        }

        [Test]
        public void EveryPlacedReefPrefabContainsRealRenderableMesh()
        {
            foreach (var spot in ReefHabitatVisuals.Spots)
            {
                var prefab=ReefKitPiece.Load(spot.PrefabId);
                Assert.NotNull(prefab, spot.PrefabId);
                Assert.IsTrue(prefab.GetComponentsInChildren<MeshFilter>(true)
                    .Any(filter=>filter.sharedMesh!=null && filter.sharedMesh.vertexCount>=3),
                    spot.PrefabId+" needs actual mesh geometry, not an empty placeholder");
                Assert.IsTrue(prefab.GetComponentsInChildren<MeshRenderer>(true)
                    .Any(renderer=>renderer.sharedMaterial!=null),
                    spot.PrefabId+" needs a material-backed mesh");
            }
        }

        [Test]
        public void AllSpotsReuseSafeReefKitPrefabsOnly()
        {
            var allowed = new HashSet<string>(ReefKitPiece.AllIds);
            foreach (var spot in ReefHabitatVisuals.Spots)
            {
                Assert.IsTrue(allowed.Contains(spot.PrefabId), spot.PrefabId);
                var prefab = ReefKitPiece.Load(spot.PrefabId);
                Assert.NotNull(prefab, spot.PrefabId);
                Assert.NotNull(prefab.GetComponent<ReefKitPiece>(), spot.PrefabId);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), spot.PrefabId);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Unity.Netcode.NetworkObject>(true), spot.PrefabId);
            }
        }
    }
}
