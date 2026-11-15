using System.Linq;
using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #303: the shallow wreck stays clear of what was there, its hull is the one box divers and fish both respect, and
    // the rest of the set is collider-free decor from approved kits.
    public sealed class P303ShallowWreckTests
    {
        private static Bounds Hull => SeabedProfile.WreckSolids.Single().Box;

        [Test]
        public void TheWreckSitsOnShallowSandUnderTheBoatsAndAwayFromTheFish()
        {
            var hull = Hull;
            Assert.Greater(SeabedProfile.FloorHeight(SeabedProfile.WreckX, SeabedProfile.WreckZ), 0f, "shallow sand");
            // The keel test's lowest keel (P219SeabedTests): surface 8, 0.5 draught, 5 m clear.
            Assert.Less(hull.max.y, 8f - 0.5f - 5f, "under every keel");
            foreach (var home in P411FiveSpeciesRoster.Homes)
                Assert.Greater(Vector3.Distance(home, hull.ClosestPoint(home)), 4f, "fish home " + home);
            foreach (var spot in ReefHabitatVisuals.Spots)
                Assert.Greater(Vector3.Distance(spot.Position, hull.ClosestPoint(spot.Position)), 3f, spot.PrefabId + " at " + spot.Position);
            Assert.Less(hull.min.y, SeabedProfile.FloorHeight(SeabedProfile.WreckX, SeabedProfile.WreckZ), "set into the sand");
        }

        [Test]
        public void TheFishBoundsSteerRoundTheHull()
        {
            var water = new HabitatWaterBounds(null, P411FiveSpeciesRoster.Homes[0]);
            Assert.IsFalse(water.Contains(Hull.center + Vector3.up * 0.4f), "inside the hull");
            Assert.IsTrue(water.Contains(new Vector3(Hull.center.x, Hull.max.y + 1.5f, Hull.center.z)), "above it");
        }

        [Test]
        public void TheSetUsesApprovedKitsWithoutCollidersOrNetworkObjects()
        {
            foreach (var prefab in ShallowWreckVisuals.Props.Select(p => ShoreKitPiece.Load(p.Id))
                         .Append(Resources.Load<GameObject>(BoatRepairSetArt.DamagedHull)))
            {
                Assert.NotNull(prefab);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), prefab.name);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Unity.Netcode.NetworkObject>(true), prefab.name);
            }
            foreach (var prop in ShallowWreckVisuals.Props)
                Assert.AreEqual(SeabedProfile.FloorHeight(prop.X, prop.Z) - ShallowWreckVisuals.BedOverlap, prop.Position.y, 1e-4f, prop.Id);
        }
    }
}
