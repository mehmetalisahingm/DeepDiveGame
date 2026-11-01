using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
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
