using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class RecordingQualityTests
    {
        // Mirrors the shipped ladder closely enough to be realistic, but written out here so a
        // retune of the asset cannot quietly change what these tests claim.
        private static readonly QualityTier[] Ladder =
        {
            new QualityTier("Bronze", 0.25f, 2f),
            new QualityTier("Silver", 0.5f, 4f),
            new QualityTier("Gold", 0.7f, 6f),
            new QualityTier("Platinum", 0.85f, 9f)
        };

        [Test]
        public void TheHighestClearedTierWins()
        {
            Assert.AreEqual(4, RecordingQuality.Evaluate(Ladder, 0.9f, 12f));
            Assert.AreEqual(3, RecordingQuality.Evaluate(Ladder, 0.75f, 7f));
            Assert.AreEqual(2, RecordingQuality.Evaluate(Ladder, 0.6f, 5f));
            Assert.AreEqual(1, RecordingQuality.Evaluate(Ladder, 0.3f, 3f));
        }

        [Test]
        public void BothScoreAndDurationMustClearTheTier()
        {
            // A beautiful shot held too briefly falls back to whatever tier its time allows.
            Assert.AreEqual(2, RecordingQuality.Evaluate(Ladder, 0.95f, 5f));
            // And a long shot that never looked good does the same on the score axis.
            Assert.AreEqual(1, RecordingQuality.Evaluate(Ladder, 0.3f, 30f));
        }

        [Test]
        public void AShotBelowTheBottomTierEarnsNothing()
        {
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, 0.2f, 30f));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, 0.95f, 1f));
            Assert.AreEqual(0, RecordingQuality.NoPayout, "quality 0 is the agreed no-payout value");
        }

        [Test]
        public void LandingExactlyOnAThresholdCounts()
        {
            Assert.AreEqual(1, RecordingQuality.Evaluate(Ladder, 0.25f, 2f));
            Assert.AreEqual(4, RecordingQuality.Evaluate(Ladder, 0.85f, 9f));
        }

        [Test]
        public void AnUnrecordedOrNonsensicalTakeEarnsNothingInsteadOfThrowing()
        {
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, 0.9f, 0f));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, 0.9f, -5f));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, float.NaN, 10f));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(Ladder, 0.9f, float.PositiveInfinity));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(null, 0.9f, 10f));
            Assert.AreEqual(RecordingQuality.NoPayout, RecordingQuality.Evaluate(new QualityTier[0], 0.9f, 10f));
        }

        [Test]
        public void OneMalformedTierIsSkippedRatherThanMakingEveryShotWorthless()
        {
            var withBadRow = new[]
            {
                new QualityTier("Bronze", 0.25f, 2f),
                new QualityTier("Broken", float.NaN, 4f)
            };

            Assert.AreEqual(1, RecordingQuality.Evaluate(withBadRow, 0.9f, 10f));
        }

        [Test]
        public void AnAscendingLadderIsAcceptedAndADescendingOneIsReported()
        {
            Assert.IsTrue(RecordingQuality.AreAscending(Ladder, out var error), error);

            var descending = new[]
            {
                new QualityTier("Gold", 0.7f, 6f),
                new QualityTier("Bronze", 0.25f, 2f)
            };
            Assert.IsFalse(RecordingQuality.AreAscending(descending, out error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void AnEmptyOrMalformedLadderIsReportedRatherThanAccepted()
        {
            Assert.IsFalse(RecordingQuality.AreAscending(null, out var error));
            Assert.IsNotEmpty(error);

            Assert.IsFalse(RecordingQuality.AreAscending(new QualityTier[0], out error));
            Assert.IsNotEmpty(error);

            var zeroDuration = new[] { new QualityTier("Bronze", 0.25f, 0f) };
            Assert.IsFalse(RecordingQuality.AreAscending(zeroDuration, out error),
                "a tier reachable in zero seconds would pay for a tap of the record button");
        }
    }

    public class RecordingQualityTableTests
    {
        private RecordingQualityTable table;

        [SetUp]
        public void CreateTable() => table = ScriptableObject.CreateInstance<RecordingQualityTable>();

        [TearDown]
        public void DestroyTable() => UnityEngine.Object.DestroyImmediate(table);

        [Test]
        public void TheShippedDefaultsAreAValidTable()
        {
            Assert.IsTrue(table.IsValid(out var error), error);
            Assert.Greater(table.TierCount, 0);
        }

        [Test]
        public void TheDefaultFramingGatesAreAUsableBand()
        {
            var framing = table.Framing;

            Assert.Greater(framing.MaxDistanceMetres, framing.MinDistanceMetres);
            Assert.Greater(framing.MaxOffAxisDegrees, 0f);
            Assert.Greater(framing.IdealFrameFill, framing.MinFrameFill);
        }

        [Test]
        public void TheTableGradesThroughTheSameRulesAsTheStaticEvaluator()
        {
            Assert.AreEqual(RecordingQuality.Evaluate(table.Tiers, 0.6f, 5f), table.Evaluate(0.6f, 5f));
            Assert.AreEqual(RecordingQuality.NoPayout, table.Evaluate(0f, 0f));
        }

        [Test]
        public void TheTopTierIndexIsTheTierCountSoQualityStaysWithinTheAgreedRange()
        {
            // Quality is the tier index + 1, and 0 means no payout. Mert prices 1..TierCount.
            Assert.AreEqual(table.TierCount, table.Evaluate(1f, 3600f));
        }
    }
}
