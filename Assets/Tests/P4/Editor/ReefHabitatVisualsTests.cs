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
        // What the kit's frame-time probe measured (72 pieces); a placement past it would need measuring again.
        private const int MeasuredPieces = 72;

        [Test]
        public void SpotsStandOnTheFloorInsideTheWalls()
        {
            var spots = ReefHabitatVisuals.Spots;
            Assert.LessOrEqual(spots.Count, MeasuredPieces);
            foreach (var spot in spots)
            {
                Assert.Greater(spot.Position.x, -14.75f, spot.PrefabId);
                Assert.Less(spot.Position.x, 14.75f, spot.PrefabId);
                Assert.Less(spot.Position.z, 64.75f, spot.PrefabId);
                Assert.AreEqual(SeabedProfile.FloorHeight(spot.Position.x, spot.Position.z) - ReefHabitatVisuals.BedOverlap,
                    spot.Position.y, 1e-3f, spot.PrefabId + " at " + spot.Position);
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

        [Test]
        public void ProposedFiveSpeciesHomesStayClearOfPlacedReefKitCenters()
        {
            // Home positions only; the wander and flee volumes are P219SeabedTests' job.
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
                var prefab = ReefKitPiece.Load(spot.PrefabId);
                Assert.NotNull(prefab, spot.PrefabId);
                Assert.IsTrue(prefab.GetComponentsInChildren<MeshFilter>(true)
                    .Any(filter => filter.sharedMesh != null && filter.sharedMesh.vertexCount >= 3),
                    spot.PrefabId + " needs actual mesh geometry, not an empty placeholder");
                Assert.IsTrue(prefab.GetComponentsInChildren<MeshRenderer>(true)
                    .Any(renderer => renderer.sharedMaterial != null),
                    spot.PrefabId + " needs a material-backed mesh");
            }
        }

        // Every solid in the profile has a visual there, at its scale, so nobody swims into an invisible box.
        [Test]
        public void EverySolidHasItsVisual()
        {
            foreach (var solid in SeabedProfile.Solids)
            {
                var (x, z) = solid.KitId == ReefHabitatVisuals.ArchId
                    ? (ReefHabitatVisuals.ArchX, ReefHabitatVisuals.ArchZ) : (solid.X, solid.Z);
                Assert.IsTrue(ReefHabitatVisuals.Spots.Any(s => s.PrefabId == solid.KitId && Mathf.Approximately(s.Scale, solid.Scale) &&
                    Mathf.Approximately(s.Position.x, x) && Mathf.Approximately(s.Position.z, z)), $"{solid.KitId} at {solid.X},{solid.Z}");
            }
        }

        // Mert's hint boxes scaled by the placement scale, so a collider never gets out of step with its piece.
        [Test]
        public void SolidBoxesFollowTheKitsColliderHints()
        {
            foreach (var solid in SeabedProfile.Solids.Where(s => s.KitId != ReefHabitatVisuals.ArchId))
            {
                var hint = ReefKitPiece.Load(solid.KitId).GetComponent<ReefKitPiece>().ColliderHint;
                Assert.Greater(hint.sqrMagnitude, 0f, solid.KitId);
                Assert.That((hint * solid.Scale - solid.Size).magnitude, Is.LessThan(0.15f), $"{solid.KitId} at scale {solid.Scale}");
            }
        }

        // The real arch mesh at its placement: an upright diver fits through the opening the boxes leave. Mert's kit test
        // proves 1 x 1 m at scale 1; this is the 1.8 m capsule at the scale it stands at.
        [Test]
        public void AnUprightDiverFitsThroughTheRealArch()
        {
            var prefab = ReefKitPiece.Load(ReefHabitatVisuals.ArchId);
            var instance = Object.Instantiate(prefab);
            try
            {
                instance.transform.position = new Vector3(100f, 100f, 100f);
                instance.transform.localScale = Vector3.one * ReefHabitatVisuals.ArchScale;
                var filter = instance.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(f => f.sharedMesh.vertexCount).First();
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                Physics.SyncTransforms();
                // On the bed, swimming through the opening along z.
                for (var z = -1.5f; z <= 1.5f; z += 0.1f)
                {
                    var feet = instance.transform.position + new Vector3(0f, 0.1f, z);
                    var hits = Physics.OverlapCapsule(feet + Vector3.up * 0.35f, feet + Vector3.up * 1.45f, 0.35f, ~0, QueryTriggerInteraction.Ignore);
                    Assert.IsFalse(hits.Contains(collider), "the arch closes on the capsule at z " + z);
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
