using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class BossTrailVisualsTests
    {
        [Test]
        public void OnePieceStandsOnTheBedAtEveryTrailPoint()
        {
            Assert.AreEqual(DeepEncounterWorld.TrailPoints.Length, BossTrailVisuals.PieceIds.Length);
            for (var i = 0; i < BossTrailVisuals.PieceIds.Length; i++)
            {
                var at = BossTrailVisuals.PositionOf(i);
                Assert.AreEqual(SeabedProfile.FloorHeight(at.x, at.z) - BossTrailVisuals.BedOverlap, at.y, 1e-4f, "piece " + i);
            }
        }

        [Test]
        public void NightBeaconsStayAboveExistingTrailWithoutMovingEvidence()
        {
            Assert.AreEqual(6, BossTrailVisuals.PieceIds.Length);
            for (var i = 0; i < BossTrailVisuals.PieceIds.Length; i++)
            {
                var point = BossTrailVisuals.BeaconPosition(i);
                Assert.AreEqual(BossTrailVisuals.PositionOf(i).x, point.x);
                Assert.AreEqual(BossTrailVisuals.PositionOf(i).z, point.z);
                Assert.Greater(point.y, SeabedProfile.FloorHeight(point.x, point.z));
            }
            Assert.LessOrEqual(BossTrailVisuals.BeaconRange, 3f);
        }
        [TestCase(0f, false)] [TestCase(.3f, false)] [TestCase(.6f, true)] [TestCase(1f, true)]
        public void WreckGlowUsesOnlyBoundedNightIntensity(float night, bool visible)
        {
            var root = new GameObject("wreck light");
            try
            {
                var light = root.AddComponent<Light>();
                BossTrailVisuals.ApplyBeacon(light, night);
                Assert.AreEqual(visible, light.enabled);
                Assert.That(light.intensity, Is.InRange(0f, BossTrailVisuals.BeaconNightIntensity));
                BossTrailVisuals.ApplyBeacon(light, 0f);
                Assert.IsFalse(light.enabled);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TheTrailReusesColliderFreeShoreKitPieces()
        {
            foreach (var id in BossTrailVisuals.PieceIds)
            {
                var prefab = ShoreKitPiece.Load(id);
                Assert.NotNull(prefab, id);
                Assert.NotNull(prefab.GetComponent<ShoreKitPiece>(), id);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), id + " must not block the swim line");
            }
        }

        // The trail is decor: no piece may sit on a trace or the arena, where it would hide the evidence marker.
        [Test]
        public void NoPieceCoversATraceOrTheArena()
        {
            for (var i = 0; i < BossTrailVisuals.PieceIds.Length; i++)
            {
                var at = BossTrailVisuals.PositionOf(i);
                foreach (var trace in DeepEncounterWorld.TracePositions)
                    Assert.Greater(Vector3.Distance(at, trace), DeepEncounterWorld.TraceRadius + 1f, "piece " + i);
                Assert.Greater(Vector3.Distance(at, DeepEncounterWorld.ArenaPosition), DeepEncounterWorld.ArenaRadius + 1f, "piece " + i);
            }
        }
    }
}
