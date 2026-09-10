using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class FishHealthTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        [Test]
        public void DamageAccumulatesAndTheKillingHitIsReportedOnce()
        {
            var fish = new FishHealth(30f);
            Assert.AreEqual(FishHitOutcome.Applied, fish.ApplyDamage(Alice, 1, 10f));
            Assert.AreEqual(20f, fish.Health);
            Assert.IsFalse(fish.IsDead);

            Assert.AreEqual(FishHitOutcome.Killed, fish.ApplyDamage(Alice, 2, 25f));
            Assert.AreEqual(0f, fish.Health);
            Assert.IsTrue(fish.IsDead);
        }

        [Test]
        public void HostVerifiedDamageIsAppliedAsGivenAndNeverGoesBelowZero()
        {
            var fish = new FishHealth(30f);
            Assert.AreEqual(FishHitOutcome.Killed, fish.ApplyDamage(Alice, 1, 9999f));
            Assert.AreEqual(0f, fish.Health);
        }

        [Test]
        public void ReplayedRequestFromTheSamePlayerAppliesOnlyOnce()
        {
            var fish = new FishHealth(30f);
            Assert.AreEqual(FishHitOutcome.Applied, fish.ApplyDamage(Alice, 7, 10f));
            Assert.AreEqual(FishHitOutcome.Duplicate, fish.ApplyDamage(Alice, 7, 10f));
            Assert.AreEqual(20f, fish.Health);
        }

        [Test]
        public void RequestIdsAreScopedPerPlayerSoTwoDiversMayShareANumber()
        {
            var fish = new FishHealth(30f);
            Assert.AreEqual(FishHitOutcome.Applied, fish.ApplyDamage(Alice, 1, 10f));
            Assert.AreEqual(FishHitOutcome.Applied, fish.ApplyDamage(Bob, 1, 10f));
            Assert.AreEqual(10f, fish.Health);
        }

        [Test]
        public void HitsOnADeadFishAreRejectedAndCannotReviveIt()
        {
            var fish = new FishHealth(10f);
            Assert.AreEqual(FishHitOutcome.Killed, fish.ApplyDamage(Alice, 1, 10f));
            Assert.AreEqual(FishHitOutcome.AlreadyDead, fish.ApplyDamage(Bob, 2, 5f));
            Assert.AreEqual(0f, fish.Health);
            Assert.IsTrue(fish.IsDead);
        }

        [Test]
        public void UnusableDamageNumbersAreIgnoredWithoutTouchingHealth()
        {
            var fish = new FishHealth(30f);
            Assert.AreEqual(FishHitOutcome.Ignored, fish.ApplyDamage(Alice, 1, float.NaN));
            Assert.AreEqual(FishHitOutcome.Ignored, fish.ApplyDamage(Alice, 2, float.PositiveInfinity));
            Assert.AreEqual(FishHitOutcome.Ignored, fish.ApplyDamage(Alice, 3, 0f));
            Assert.AreEqual(FishHitOutcome.Ignored, fish.ApplyDamage(Alice, 4, -5f));
            Assert.AreEqual(30f, fish.Health);
            Assert.IsFalse(fish.IsDead);
        }

        [Test]
        public void ResetRestoresHealthAndForgetsHandledRequests()
        {
            var fish = new FishHealth(30f);
            fish.ApplyDamage(Alice, 1, 30f);
            Assert.IsTrue(fish.IsDead);

            fish.Reset();
            Assert.AreEqual(30f, fish.Health);
            Assert.IsFalse(fish.IsDead);
            Assert.AreEqual(FishHitOutcome.Applied, fish.ApplyDamage(Alice, 1, 10f));
        }

        [Test]
        public void InvalidMaxHealthFallsBackInsteadOfCreatingABornDeadFish()
        {
            Assert.AreEqual(1f, new FishHealth(0f).MaxHealth);
            Assert.AreEqual(1f, new FishHealth(float.NaN).MaxHealth);
            Assert.IsFalse(new FishHealth(-5f).IsDead);
        }
    }

    public class WeightRangeTests
    {
        [Test]
        public void RollStaysInsideTheInclusiveRange()
        {
            var range = new WeightRange(400, 1200);
            var random = new System.Random(1234);
            for (var i = 0; i < 200; i++)
            {
                var grams = range.Roll(random);
                Assert.GreaterOrEqual(grams, 400);
                Assert.LessOrEqual(grams, 1200);
            }
        }

        [Test]
        public void SameSeedRollsTheSameWeight()
        {
            var range = new WeightRange(400, 1200);
            Assert.AreEqual(range.Roll(new System.Random(7)), range.Roll(new System.Random(7)));
        }

        [Test]
        public void SinglePointRangeAndInvalidRangeAreHandled()
        {
            Assert.AreEqual(500, new WeightRange(500, 500).Roll(new System.Random(1)));
            Assert.IsFalse(new WeightRange(1200, 400).IsValid);
            Assert.AreEqual(0, new WeightRange(1200, 400).Roll(new System.Random(1)));
            Assert.IsFalse(new WeightRange(0, 100).IsValid);
        }
    }

    public class CaptureBuilderTests
    {
        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        private static FakeDive ActiveDive(string diveId = "dive-1") =>
            new FakeDive { IsDiveActive = true, CurrentDiveId = diveId };

        [Test]
        public void CaptureCarriesTheLiveDiveIdAndSpeciesFields()
        {
            Assert.IsTrue(CaptureBuilder.TryCreate(ActiveDive(), "reef-bass", 850, 42, out var capture));
            Assert.AreEqual("dive-1", capture.DiveId);
            Assert.AreEqual("reef-bass", capture.SpeciesId);
            Assert.AreEqual(850, capture.WeightGrams);
            Assert.AreEqual(42ul, capture.CatchObjectId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(capture.CaptureId));
            // Quality belongs to P3 recording evaluation; P2 must not invent a payout input.
            Assert.IsNull(capture.Quality);
        }

        [Test]
        public void NoCaptureIsBuiltWithoutALiveDive()
        {
            Assert.IsFalse(CaptureBuilder.TryCreate(null, "reef-bass", 850, 42, out _));
            Assert.IsFalse(CaptureBuilder.TryCreate(new FakeDive { IsDiveActive = false, CurrentDiveId = "dive-1" },
                "reef-bass", 850, 42, out _));
            Assert.IsFalse(CaptureBuilder.TryCreate(new FakeDive { IsDiveActive = true, CurrentDiveId = "" },
                "reef-bass", 850, 42, out _));
            Assert.IsFalse(CaptureBuilder.TryCreate(new FakeDive { IsDiveActive = true, CurrentDiveId = "   " },
                "reef-bass", 850, 42, out _));
        }

        [Test]
        public void MissingSpeciesOrWeightProducesNoCapture()
        {
            Assert.IsFalse(CaptureBuilder.TryCreate(ActiveDive(), "", 850, 42, out _));
            Assert.IsFalse(CaptureBuilder.TryCreate(ActiveDive(), "reef-bass", 0, 42, out _));
            Assert.IsFalse(CaptureBuilder.TryCreate(ActiveDive(), "reef-bass", -1, 42, out _));
        }

        [Test]
        public void EveryCaptureGetsItsOwnId()
        {
            CaptureBuilder.TryCreate(ActiveDive(), "reef-bass", 850, 42, out var first);
            CaptureBuilder.TryCreate(ActiveDive(), "reef-bass", 850, 42, out var second);
            Assert.AreNotEqual(first.CaptureId, second.CaptureId);
        }
    }

    public class DiveContextTests
    {
        [TearDown]
        public void ClearBinding() => DiveContext.Unbind();

        private sealed class FakeDive : IDiveContext
        {
            public bool IsDiveActive { get; set; }
            public string CurrentDiveId { get; set; } = "";
        }

        [Test]
        public void UnboundContextReportsNoDiveInsteadOfThrowing()
        {
            DiveContext.Unbind();
            Assert.IsFalse(DiveContext.IsDiveActive);
            Assert.AreEqual("", DiveContext.CurrentDiveId);
        }

        [Test]
        public void BoundContextExposesTheDiveIdOnlyWhileTheDiveIsActive()
        {
            var dive = new FakeDive { IsDiveActive = true, CurrentDiveId = "dive-9" };
            DiveContext.Bind(dive);
            Assert.IsTrue(DiveContext.IsDiveActive);
            Assert.AreEqual("dive-9", DiveContext.CurrentDiveId);

            dive.IsDiveActive = false;
            Assert.IsFalse(DiveContext.IsDiveActive);
            Assert.AreEqual("", DiveContext.CurrentDiveId);
        }
    }

    public class SpeciesDefinitionTests
    {
        [Test]
        public void FreshDefinitionIsInvalidUntilItGetsAnId()
        {
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                Assert.IsFalse(species.IsValid(out var error));
                Assert.IsTrue(error.Contains("speciesId"));
            }
            finally
            {
                Object.DestroyImmediate(species);
            }
        }
    }
}
