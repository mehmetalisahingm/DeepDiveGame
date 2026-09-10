using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class FishMotionTests
    {
        // A plain box stands in for the scene's SwimVolume.
        private sealed class BoxWater : IWaterBounds
        {
            private readonly Bounds bounds;
            public BoxWater(Vector3 center, Vector3 size) => bounds = new Bounds(center, size);
            public bool Contains(Vector3 point) => bounds.Contains(point);
        }

        private static readonly IWaterBounds BigWater = new BoxWater(Vector3.zero, new Vector3(200, 200, 200));

        private static SwimTuning Tuning(float swim = 1.5f, float flee = 3.5f, float fleeRadius = 6f,
            float wanderRadius = 8f) => new SwimTuning(swim, flee, fleeRadius, wanderRadius);

        private static FishMotion Motion(SwimTuning tuning, Vector3 home, int seed = 1) =>
            new FishMotion(tuning, home, new System.Random(seed));

        private static readonly List<Vector3> NoThreats = new List<Vector3>();

        [Test]
        public void FishSwimsAwayFromADiverInsideTheFleeRadius()
        {
            var motion = Motion(Tuning(), Vector3.zero);
            var diver = new Vector3(2f, 0f, 0f);

            var next = motion.Step(Vector3.zero, new List<Vector3> { diver }, 0.2f, BigWater);

            Assert.IsTrue(motion.IsFleeing);
            Assert.Greater(Vector3.Distance(next, diver), Vector3.Distance(Vector3.zero, diver));
            Assert.Less(next.x, 0f, "the fish should move opposite the diver");
        }

        [Test]
        public void TheNearestDiverIsTheOneTheFishRunsFrom()
        {
            var motion = Motion(Tuning(), Vector3.zero);
            var near = new Vector3(1f, 0f, 0f);
            var far = new Vector3(0f, 0f, 5f);

            var next = motion.Step(Vector3.zero, new List<Vector3> { far, near }, 0.2f, BigWater);

            Assert.IsTrue(motion.IsFleeing);
            Assert.Less(next.x, 0f, "it should flee the diver one metre away, not the one five away");
        }

        [Test]
        public void ADiverOutsideTheFleeRadiusIsIgnored()
        {
            var motion = Motion(Tuning(fleeRadius: 6f), Vector3.zero);

            motion.Step(Vector3.zero, new List<Vector3> { new Vector3(0f, 0f, 20f) }, 0.2f, BigWater);

            Assert.IsFalse(motion.IsFleeing);
        }

        [Test]
        public void FleeingCoversMoreGroundThanWandering()
        {
            const float step = 0.2f;
            var wander = Motion(Tuning(swim: 1.5f, flee: 3.5f), Vector3.zero);
            var flee = Motion(Tuning(swim: 1.5f, flee: 3.5f), Vector3.zero);

            var wandered = Vector3.Distance(Vector3.zero, wander.Step(Vector3.zero, NoThreats, step, BigWater));
            var fled = Vector3.Distance(Vector3.zero,
                flee.Step(Vector3.zero, new List<Vector3> { new Vector3(1f, 0f, 0f) }, step, BigWater));

            Assert.AreEqual(1.5f * step, wandered, 0.001f);
            Assert.AreEqual(3.5f * step, fled, 0.001f);
            Assert.Greater(fled, wandered);
        }

        [Test]
        public void AFleeingFishNeverLeavesTheWater()
        {
            var water = new BoxWater(Vector3.zero, new Vector3(20, 20, 20));
            var motion = Motion(Tuning(flee: 6f), Vector3.zero);
            var position = new Vector3(8f, 0f, 0f);

            // A diver parked outside the wall keeps pushing the fish at the boundary.
            var chasing = new List<Vector3> { new Vector3(20f, 0f, 0f) };
            for (var i = 0; i < 200; i++)
            {
                position = motion.Step(position, chasing, 0.05f, water);
                Assert.IsTrue(water.Contains(position), $"left the water at step {i}: {position}");
            }
        }

        [Test]
        public void AFishHeadingAtTheBoundaryTurnsBackInstead()
        {
            var water = new BoxWater(Vector3.zero, new Vector3(20, 20, 20));
            var motion = Motion(Tuning(flee: 4f), Vector3.zero);
            var position = new Vector3(9.5f, 0f, 0f);

            // Diver on the inside, so fleeing straight would take the fish through the wall.
            var next = motion.Step(position, new List<Vector3> { new Vector3(7f, 0f, 0f) }, 0.2f, water);

            Assert.IsTrue(water.Contains(next));
            Assert.LessOrEqual(next.x, position.x, "it should turn back inward rather than push out");
        }

        [Test]
        public void WanderingStaysAroundHome()
        {
            var home = new Vector3(3f, -4f, 2f);
            var motion = Motion(Tuning(swim: 2f, wanderRadius: 5f), home);
            var position = home;

            for (var i = 0; i < 600; i++)
            {
                position = motion.Step(position, NoThreats, 0.05f, BigWater);
                Assert.LessOrEqual(Vector3.Distance(position, home), 5f + 1f,
                    $"drifted too far from home at step {i}: {position}");
            }
        }

        [Test]
        public void WanderTargetsStayInsideTheWater()
        {
            // Home sits near the edge, so a naive roll would land targets outside.
            var water = new BoxWater(Vector3.zero, new Vector3(10, 10, 10));
            var home = new Vector3(4f, 0f, 4f);
            var motion = Motion(Tuning(swim: 2f, wanderRadius: 8f), home);
            var position = home;

            for (var i = 0; i < 400; i++)
            {
                position = motion.Step(position, NoThreats, 0.05f, water);
                Assert.IsTrue(water.Contains(motion.WanderTarget),
                    $"wander target outside water at step {i}: {motion.WanderTarget}");
                Assert.IsTrue(water.Contains(position));
            }
        }

        [Test]
        public void AStoppedClockDoesNotMoveTheFish()
        {
            var motion = Motion(Tuning(), Vector3.zero);
            var threats = new List<Vector3> { new Vector3(1f, 0f, 0f) };

            Assert.AreEqual(Vector3.zero, motion.Step(Vector3.zero, threats, 0f, BigWater));
            Assert.AreEqual(Vector3.zero, motion.Step(Vector3.zero, threats, -1f, BigWater));
            Assert.AreEqual(Vector3.zero, motion.Step(Vector3.zero, threats, float.NaN, BigWater));
        }

        [Test]
        public void BrokenPositionsAndThreatsCannotProduceANaNFish()
        {
            var motion = Motion(Tuning(), Vector3.zero);
            var broken = new List<Vector3> { new Vector3(float.NaN, 0f, 0f), new Vector3(1f, 0f, 0f) };

            var next = motion.Step(Vector3.zero, broken, 0.2f, BigWater);
            Assert.IsFalse(float.IsNaN(next.x) || float.IsNaN(next.y) || float.IsNaN(next.z));
            Assert.IsTrue(motion.IsFleeing, "the usable threat should still be seen");

            // A broken position is handed back untouched rather than turned into movement.
            // Compared component-wise on purpose: Vector3 equality is false for NaN operands.
            var fromBroken = new Vector3(float.NaN, 0f, 0f);
            var unchanged = motion.Step(fromBroken, broken, 0.2f, BigWater);
            Assert.IsTrue(float.IsNaN(unchanged.x));
            Assert.AreEqual(0f, unchanged.y);
            Assert.AreEqual(0f, unchanged.z);
        }

        [Test]
        public void ADiverStandingExactlyOnTheFishStillGivesAnEscapeHeading()
        {
            var motion = Motion(Tuning(), Vector3.zero);

            var next = motion.Step(Vector3.zero, new List<Vector3> { Vector3.zero }, 0.2f, BigWater);

            Assert.IsTrue(motion.IsFleeing);
            Assert.AreNotEqual(Vector3.zero, next);
            Assert.IsFalse(float.IsNaN(next.x) || float.IsNaN(next.y) || float.IsNaN(next.z));
        }

        [Test]
        public void TheSameSeedProducesTheSameWander()
        {
            var first = Motion(Tuning(), Vector3.zero, seed: 42);
            var second = Motion(Tuning(), Vector3.zero, seed: 42);
            var a = Vector3.zero;
            var b = Vector3.zero;

            for (var i = 0; i < 50; i++)
            {
                a = first.Step(a, NoThreats, 0.1f, BigWater);
                b = second.Step(b, NoThreats, 0.1f, BigWater);
            }

            Assert.AreEqual(a, b);
        }

        [Test]
        public void NearestThreatSearchHonoursTheRadius()
        {
            var threats = new List<Vector3> { new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 10f) };

            Assert.IsTrue(FishMotion.TryFindNearestThreat(Vector3.zero, threats, 5f, out var nearest));
            Assert.AreEqual(new Vector3(0f, 0f, 3f), nearest);

            Assert.IsFalse(FishMotion.TryFindNearestThreat(Vector3.zero, threats, 1f, out _));
            Assert.IsFalse(FishMotion.TryFindNearestThreat(Vector3.zero, threats, 0f, out _));
            Assert.IsFalse(FishMotion.TryFindNearestThreat(Vector3.zero, null, 5f, out _));
            Assert.IsFalse(FishMotion.TryFindNearestThreat(Vector3.zero, new List<Vector3>(), 5f, out _));
        }

        [Test]
        public void FleeDirectionPointsAwayAndIsAlwaysUsable()
        {
            var away = FishMotion.FleeDirection(Vector3.zero, new Vector3(0f, 0f, 4f));
            Assert.AreEqual(new Vector3(0f, 0f, -1f), away);
            Assert.AreEqual(1f, away.magnitude, 0.001f);
            Assert.AreEqual(1f, FishMotion.FleeDirection(Vector3.zero, Vector3.zero).magnitude, 0.001f);
        }
    }

    public class SwimTuningTests
    {
        [Test]
        public void InvalidSpeedsFallBackAndFleeRadiusMayBeDisabled()
        {
            var broken = new SwimTuning(0f, float.NaN, float.NegativeInfinity, -3f);
            Assert.AreEqual(1.5f, broken.SwimSpeed);
            Assert.AreEqual(3.5f, broken.FleeSpeed);
            Assert.AreEqual(0f, broken.FleeRadius, "a non-positive flee radius simply disables fleeing");
            Assert.AreEqual(1f, broken.WanderRadius);

            var good = new SwimTuning(2f, 5f, 7f, 9f);
            Assert.AreEqual(2f, good.SwimSpeed);
            Assert.AreEqual(5f, good.FleeSpeed);
            Assert.AreEqual(7f, good.FleeRadius);
            Assert.AreEqual(9f, good.WanderRadius);
        }

        [Test]
        public void AZeroFleeRadiusMeansTheFishNeverFlees()
        {
            var motion = new FishMotion(new SwimTuning(1.5f, 3.5f, 0f, 8f), Vector3.zero, new System.Random(1));

            motion.Step(Vector3.zero, new List<Vector3> { new Vector3(0.1f, 0f, 0f) }, 0.2f, null);

            Assert.IsFalse(motion.IsFleeing);
        }
    }
}
