using System;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    // The pure day/night curve. No scene and no atmosphere: only CampaignDayState in, DaylightState out.
    public class DaylightModelTests
    {
        private const float Eps = 1e-5f;

        private static CampaignDayState Day(int minute, DayPhase phase = DayPhase.Running, int seed = 0) =>
            new CampaignDayState(1, DayIds.DayId(1), minute, phase, null, null, seed, 0);

        [Test]
        public void Midday_IsFullDay()
        {
            var s = DaylightModel.Evaluate(Day(12 * 60));
            Assert.That(s.DaylightFactor, Is.EqualTo(1f));
            Assert.That(s.NightFactor, Is.EqualTo(0f));
            Assert.That(s.AmbientMultiplier, Is.EqualTo(1f).Within(Eps));
            Assert.That(s.FogColorMultiplier, Is.EqualTo(1f).Within(Eps));
        }

        [Test]
        public void Night_IsFullNight_ButNotBlack()
        {
            var s = DaylightModel.Evaluate(Day(23 * 60));
            Assert.That(s.DaylightFactor, Is.EqualTo(0f));
            Assert.That(s.NightFactor, Is.EqualTo(1f));
            Assert.That(s.AmbientMultiplier, Is.EqualTo(DaylightModel.NightAmbient).Within(Eps));
            Assert.That(s.FogColorMultiplier, Is.EqualTo(DaylightModel.NightFogColor).Within(Eps));
            Assert.That(s.SunIntensityMultiplier, Is.GreaterThan(0f));
        }

        [Test]
        public void DayStart_And_Midnight_AreTheCampaignBounds()
        {
            Assert.That(DaylightModel.DaylightFactorAt(DayIds.DayStartMinute), Is.EqualTo(1f));
            Assert.That(DaylightModel.DaylightFactorAt(DayIds.DayEndMinute), Is.EqualTo(0f));
        }

        [Test]
        public void RampEdges_AreExact()
        {
            Assert.That(DaylightModel.DaylightFactorAt(DaylightModel.DuskStartMinute), Is.EqualTo(1f));
            Assert.That(DaylightModel.DaylightFactorAt(DaylightModel.DuskEndMinute), Is.EqualTo(0f));
            Assert.That(DaylightModel.DaylightFactorAt(DaylightModel.DawnStartMinute), Is.EqualTo(0f));
            Assert.That(DaylightModel.DaylightFactorAt(DaylightModel.DawnEndMinute), Is.EqualTo(1f));
        }

        [Test]
        public void Dusk_FallsMonotonically_ThroughStrictlyPartialLight()
        {
            var previous = 1f;
            for (var m = DaylightModel.DuskStartMinute + 1; m < DaylightModel.DuskEndMinute; m++)
            {
                var f = DaylightModel.DaylightFactorAt(m);
                Assert.That(f, Is.GreaterThan(0f).And.LessThan(1f), $"minute {m}");
                Assert.That(f, Is.LessThanOrEqualTo(previous), $"minute {m}");
                previous = f;
            }
        }

        [Test]
        public void Dawn_RisesMonotonically_ThroughStrictlyPartialLight()
        {
            var previous = 0f;
            for (var m = DaylightModel.DawnStartMinute + 1; m < DaylightModel.DawnEndMinute; m++)
            {
                var f = DaylightModel.DaylightFactorAt(m);
                Assert.That(f, Is.GreaterThan(0f).And.LessThan(1f), $"minute {m}");
                Assert.That(f, Is.GreaterThanOrEqualTo(previous), $"minute {m}");
                previous = f;
            }
        }

        [Test]
        public void OutOfRangeClock_ClampsToNight_NeverWrapsToMorning()
        {
            Assert.That(DaylightModel.DaylightFactorAt(DayIds.DayEndMinute + 600), Is.EqualTo(0f));
            Assert.That(DaylightModel.DaylightFactorAt(-100), Is.EqualTo(0f));
        }

        [Test]
        public void MorningPhase_IsLitAsDayStart_WhateverTheClock()
        {
            var morning = DaylightModel.Evaluate(Day(DayIds.DayEndMinute, DayPhase.Morning));
            Assert.That(morning.DaylightFactor, Is.EqualTo(1f));
        }

        [Test]
        public void ClosingAndSummary_KeepTheClockTheyClosedAt()
        {
            Assert.That(DaylightModel.Evaluate(Day(DayIds.DayEndMinute, DayPhase.Closing)).DaylightFactor, Is.EqualTo(0f));
            Assert.That(DaylightModel.Evaluate(Day(DayIds.DayEndMinute, DayPhase.Summary)).DaylightFactor, Is.EqualTo(0f));
            Assert.That(DaylightModel.Evaluate(Day(14 * 60, DayPhase.Summary)).DaylightFactor, Is.EqualTo(1f));
        }

        [Test]
        public void SameState_GivesIdenticalResult()
        {
            for (var seed = -50; seed <= 50; seed += 7)
            for (var m = 0; m <= DayIds.DayEndMinute; m += 37)
            {
                var a = DaylightModel.Evaluate(Day(m, DayPhase.Running, seed));
                var b = DaylightModel.Evaluate(Day(m, DayPhase.Running, seed));
                Assert.That(b.DaylightFactor, Is.EqualTo(a.DaylightFactor));
                Assert.That(b.SunIntensityMultiplier, Is.EqualTo(a.SunIntensityMultiplier));
                Assert.That(b.AmbientMultiplier, Is.EqualTo(a.AmbientMultiplier));
                Assert.That(b.FogColorMultiplier, Is.EqualTo(a.FogColorMultiplier));
            }
        }

        [Test]
        public void Seed_DimsOnlyTheSun_WithinTheOvercastLimit()
        {
            var noon = 12 * 60;
            var seen = false;
            for (var seed = 0; seed < 200; seed++)
            {
                var s = DaylightModel.Evaluate(Day(noon, DayPhase.Running, seed));
                Assert.That(s.DaylightFactor, Is.EqualTo(1f));
                Assert.That(s.AmbientMultiplier, Is.EqualTo(1f).Within(Eps));
                Assert.That(s.FogColorMultiplier, Is.EqualTo(1f).Within(Eps));
                Assert.That(s.SunIntensityMultiplier,
                    Is.InRange(1f - DaylightModel.MaxOvercast - Eps, 1f + Eps));
                if (s.SunIntensityMultiplier < 1f - Eps) seen = true;
            }
            Assert.That(seen, Is.True, "no seed produced any overcast");
        }

        [Test]
        public void Overcast_IsAFixedFunctionOfTheSeed()
        {
            // Seed 0 hashes to 0 (a fresh campaign starts clear); other seeds differ from each other.
            Assert.That(DaylightModel.Overcast01(0), Is.EqualTo(0f));
            Assert.That(DaylightModel.Overcast01(17), Is.EqualTo(DaylightModel.Overcast01(17)));
            Assert.That(DaylightModel.Overcast01(17), Is.InRange(0f, 1f));
            Assert.That(DaylightModel.Overcast01(17), Is.Not.EqualTo(DaylightModel.Overcast01(18)));
        }

        [Test]
        public void Evaluate_DoesNotAllocate()
        {
            var day = Day(20 * 60, DayPhase.Running, 42);
            DaylightModel.Evaluate(day); // warm up (JIT)
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = 0f;
            for (var i = 0; i < 10000; i++) sum += DaylightModel.Evaluate(day).DaylightFactor;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes (sum {sum})");
        }

        [Test]
        public void ShallowBandId_IsTheCoreDepthBandId()
        {
            Assert.That(DiveDepthBands.ShallowId, Is.EqualTo(DepthBandIds.Shallow));
            Assert.That(DiveDepthBands.Shallow.Id, Is.EqualTo(DepthBandIds.Shallow));
        }
    }
}
