using System;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // The camera tier rules on plain numbers. No global is read or bound anywhere in this
    // fixture: light arrives as a DaylightState or a light01, exactly as the rules take it.
    public class RecordingCameraRulesTests
    {
        private const float Fov = 60f;
        private const float FishRadius = 0.55f;   // the fish in DiveTestArea
        private const float LargeRadius = 3f;     // never frame-fill limited inside the reach tested
        private const int Noon = 12 * 60;
        private const int Night = 22 * 60;
        private const float Tolerance = 0.00001f;

        private static readonly RecordingTuning Default = RecordingTuning.Default;

        // The bioluminescence event's authored framing: wider than the default far edge.
        private static readonly RecordingTuning Event = new RecordingTuning(3f, 16f, 22f, 0.08f, 0.45f, 0.4f);

        private static RecordingSample Ahead(float distance, float radius, CameraTier tier, float light01,
            RecordingTuning framing, bool occluded = false) =>
            RecordingCameraRules.Sample(Vector3.zero, Vector3.forward, Fov, new Vector3(0f, 0f, distance), radius,
                occluded, framing, tier, light01);

        private static RecordingSample Ahead(float distance, float radius, CameraTier tier, float light01 = 1f) =>
            Ahead(distance, radius, tier, light01, Default);

        private static RecordingSample At(float degreesOffAxis, float distance, float radius, CameraTier tier,
            float light01, RecordingTuning framing, bool occluded = false)
        {
            var direction = Quaternion.Euler(0f, degreesOffAxis, 0f) * Vector3.forward;
            return RecordingCameraRules.Sample(Vector3.zero, Vector3.forward, Fov, direction * distance, radius,
                occluded, framing, tier, light01);
        }

        private static float LightAt(int clockMinute, float depth, int weatherSeed = 0) =>
            RecordingCameraRules.Light01(DaylightModel.Evaluate(clockMinute, DayPhase.Running, weatherSeed), depth);

        private static void AssertSameFraming(RecordingTuning expected, RecordingTuning actual)
        {
            Assert.AreEqual(expected.MinDistanceMetres, actual.MinDistanceMetres);
            Assert.AreEqual(expected.MaxDistanceMetres, actual.MaxDistanceMetres);
            Assert.AreEqual(expected.MaxOffAxisDegrees, actual.MaxOffAxisDegrees);
            Assert.AreEqual(expected.MinFrameFill, actual.MinFrameFill);
            Assert.AreEqual(expected.IdealFrameFill, actual.IdealFrameFill);
            Assert.AreEqual(expected.CenteringWeight, actual.CenteringWeight);
        }

        private static void AssertSameSample(RecordingSample expected, RecordingSample actual, string pose)
        {
            Assert.AreEqual(expected.IsValid, actual.IsValid, pose);
            Assert.AreEqual(expected.Rejection, actual.Rejection, pose);
            Assert.AreEqual(expected.Score01, actual.Score01, pose);
            Assert.AreEqual(expected.DistanceMetres, actual.DistanceMetres, pose);
            Assert.AreEqual(expected.OffAxisDegrees, actual.OffAxisDegrees, pose);
            Assert.AreEqual(expected.FrameFill, actual.FrameFill, pose);
        }

        // --- tiers -------------------------------------------------------------------------

        [Test]
        public void NoneIsTreatedAsBasic()
        {
            Assert.AreEqual(CameraTier.Basic, RecordingCameraRules.Normalize(CameraTier.None));
            Assert.IsFalse(RecordingCameraRules.HasExtendedOptics(CameraTier.None));
            Assert.AreEqual(RecordingCameraRules.MinLight01(CameraTier.Basic),
                RecordingCameraRules.MinLight01(CameraTier.None));
            AssertSameFraming(RecordingCameraRules.TuningFor(Default, CameraTier.Basic),
                RecordingCameraRules.TuningFor(Default, CameraTier.None));
        }

        [Test]
        public void AnUndefinedTierFallsBackToBasic()
        {
            var corrupt = (CameraTier)99;

            Assert.AreEqual(CameraTier.Basic, RecordingCameraRules.Normalize(corrupt));
            Assert.IsFalse(RecordingCameraRules.HasExtendedOptics(corrupt));
            Assert.AreEqual(RecordingCameraRules.BasicMinLight, RecordingCameraRules.MinLight01(corrupt));
            AssertSameFraming(Default, RecordingCameraRules.TuningFor(Default, corrupt));
        }

        // --- optical reach -----------------------------------------------------------------

        [TestCase(1.5f, 14f)]
        [TestCase(3f, 16f)]
        public void BasicKeepsTheFramingExactly(float minDistance, float maxDistance)
        {
            var framing = new RecordingTuning(minDistance, maxDistance, 22f, 0.08f, 0.45f, 0.4f);

            AssertSameFraming(framing, RecordingCameraRules.TuningFor(framing, CameraTier.Basic));
            AssertSameFraming(framing, RecordingCameraRules.TuningFor(framing, CameraTier.None));
            Assert.AreEqual(maxDistance, RecordingCameraRules.MaxDistanceFor(framing, CameraTier.Basic));
        }

        [TestCase(CameraTier.Advanced)]
        [TestCase(CameraTier.Professional)]
        public void ExtendedOpticsChangeOnlyReachAndFrameFill(CameraTier tier)
        {
            var extended = RecordingCameraRules.TuningFor(Default, tier);

            Assert.AreEqual(Default.MaxDistanceMetres + RecordingCameraRules.ExtendedReachMetres,
                extended.MaxDistanceMetres);
            Assert.AreEqual(20f, extended.MaxDistanceMetres);
            Assert.AreEqual(Default.MinFrameFill * RecordingCameraRules.OpticalFrameFillFactor, extended.MinFrameFill);

            // Everything the score is computed from stays the subject's own.
            Assert.AreEqual(Default.MinDistanceMetres, extended.MinDistanceMetres);
            Assert.AreEqual(Default.MaxOffAxisDegrees, extended.MaxOffAxisDegrees);
            Assert.AreEqual(Default.IdealFrameFill, extended.IdealFrameFill);
            Assert.AreEqual(Default.CenteringWeight, extended.CenteringWeight);

            Assert.AreEqual(22f, RecordingCameraRules.MaxDistanceFor(Event, tier),
                "the event keeps its wider far edge at every tier");
        }

        [Test]
        public void OpticalReachNeverShrinksAsTheTierRises()
        {
            var basic = RecordingCameraRules.TuningFor(Default, CameraTier.Basic);
            var advanced = RecordingCameraRules.TuningFor(Default, CameraTier.Advanced);
            var professional = RecordingCameraRules.TuningFor(Default, CameraTier.Professional);

            Assert.LessOrEqual(basic.MaxDistanceMetres, advanced.MaxDistanceMetres);
            Assert.LessOrEqual(advanced.MaxDistanceMetres, professional.MaxDistanceMetres);
            Assert.GreaterOrEqual(basic.MinFrameFill, advanced.MinFrameFill);
            Assert.GreaterOrEqual(advanced.MinFrameFill, professional.MinFrameFill);
        }

        [TestCase(CameraTier.Basic, 14f)]
        [TestCase(CameraTier.Advanced, 20f)]
        [TestCase(CameraTier.Professional, 20f)]
        public void EachTierAcceptsAtItsMaxAndRefusesJustPast(CameraTier tier, float maxDistance)
        {
            Assert.IsTrue(Ahead(maxDistance, LargeRadius, tier).IsValid, "exactly at the far edge is in reach");

            var past = Ahead(maxDistance + 0.01f, LargeRadius, tier);
            Assert.IsFalse(past.IsValid);
            Assert.AreEqual(RecordingSampleRejection.TooFar, past.Rejection);
        }

        [Test]
        public void ANormalFishInsideBasicsReachIsTooSmallForBasicAndInShotForAdvanced()
        {
            // 13 m is inside Basic's 14 m far edge, so the frame-fill floor is what refuses it:
            // 0.55 / (13 * tan 30) = 0.073 < 0.08, but >= 0.056 for the extended optics.
            Assert.AreEqual(RecordingSampleRejection.TooSmall, Ahead(13f, FishRadius, CameraTier.Basic).Rejection,
                "the base camera cannot resolve this fish even inside its own far edge");
            Assert.IsTrue(Ahead(13f, FishRadius, CameraTier.Advanced).IsValid, "the extended optics resolve it");
        }

        [Test]
        public void ANormalFishAtFifteenMetresIsOutOfReachForBasicAndInShotForAdvanced()
        {
            // Past 14 m the far edge refuses first (Evaluate checks reach before frame fill).
            Assert.AreEqual(RecordingSampleRejection.TooFar, Ahead(15f, FishRadius, CameraTier.Basic).Rejection);

            var advanced = Ahead(15f, FishRadius, CameraTier.Advanced);
            Assert.IsTrue(advanced.IsValid, "reach and frame fill both extend, so the fish is in shot");
            Assert.AreEqual(RecordingSampleRejection.None, advanced.Rejection);
        }

        [Test]
        public void BasicsMinFrameFillIsTheTablesOwn()
        {
            Assert.AreEqual(Default.MinFrameFill, RecordingCameraRules.MinFrameFillFor(Default, CameraTier.Basic));
            Assert.AreEqual(Default.MinFrameFill, RecordingCameraRules.MinFrameFillFor(Default, CameraTier.None));
            Assert.AreEqual(Event.MinFrameFill, RecordingCameraRules.MinFrameFillFor(Event, CameraTier.Basic));
        }

        [Test]
        public void ExtendedFrameFillThresholdIsInclusive()
        {
            const float distance = 10f;
            const float radius = 0.5f;
            var fill = RecordingFraming.FrameFill(distance, radius, Fov);

            // Find a framing whose extended floor lands exactly on this fill, so the test sits on
            // the boundary rather than near it.
            var found = false;
            var framing = Default;
            var start = BitConverter.ToInt32(BitConverter.GetBytes(fill / RecordingCameraRules.OpticalFrameFillFactor), 0);
            for (var step = -16; step <= 16 && !found; step++)
            {
                var floor = BitConverter.ToSingle(BitConverter.GetBytes(start + step), 0);
                framing = new RecordingTuning(1.5f, 14f, 22f, floor, 0.45f, 0.4f);
                found = RecordingCameraRules.MinFrameFillFor(framing, CameraTier.Advanced) == fill;
            }
            Assume.That(found, "no float floor maps exactly onto the measured fill");

            Assert.IsTrue(Ahead(distance, radius, CameraTier.Advanced, 1f, framing).IsValid,
                "a fill exactly on the extended floor is in shot");
            Assert.AreEqual(RecordingSampleRejection.TooSmall,
                Ahead(distance + 0.05f, radius, CameraTier.Advanced, 1f, framing).Rejection);
        }

        [Test]
        public void TheSamePoseScoresTheSameForEveryTier()
        {
            float[] distances = { 2f, 5f, 8f, 11f };
            float[] angles = { 0f, 8f, 15f };

            foreach (var distance in distances)
            foreach (var angle in angles)
            {
                var basic = At(angle, distance, FishRadius, CameraTier.Basic, 1f, Default);
                Assume.That(basic.IsValid, $"pose {distance} m / {angle} deg should be valid for Basic");

                foreach (var tier in new[] { CameraTier.Advanced, CameraTier.Professional })
                {
                    var other = At(angle, distance, FishRadius, tier, 1f, Default);
                    Assert.IsTrue(other.IsValid);
                    Assert.AreEqual(basic.Score01, other.Score01,
                        $"{tier} at {distance} m / {angle} deg must score exactly like Basic");
                }
            }
        }

        // --- light -------------------------------------------------------------------------

        [TestCase(0f)]
        [TestCase(4f)]
        [TestCase(8f)]
        public void DepthFactorIsFullThroughTheShallowBand(float depth)
        {
            Assert.AreEqual(DiveDepthBands.ShallowMaxDepth, RecordingCameraRules.LitDepthMetres);
            Assert.AreEqual(1f, RecordingCameraRules.DepthFactor(depth));
        }

        [TestCase(20f, 1f - 0.8f * 12f / 27f)]
        [TestCase(35f, 0.2f)]
        [TestCase(100f, 0.2f)]
        public void DepthFactorFallsLinearlyToTheDeepFloor(float depth, float expected)
        {
            Assert.AreEqual(expected, RecordingCameraRules.DepthFactor(depth), Tolerance);
        }

        [Test]
        public void AboveTheSurfaceCountsAsTheSurface()
        {
            Assert.AreEqual(1f, RecordingCameraRules.DepthFactor(-2f));
        }

        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(0f, float.NaN)]
        public void NonFiniteInputsFailOpenToFullLight(float depth, float ambient)
        {
            var daylight = new DaylightState(1f, 1f, ambient, 1f);

            Assert.AreEqual(1f, RecordingCameraRules.Light01(daylight, depth));
            Assert.IsTrue(RecordingCameraRules.IsLitEnough(CameraTier.Basic, float.NaN),
                "a light that is not a number is a broken read, not darkness");
        }

        [Test]
        public void WeatherDoesNotDimTheCamera()
        {
            int[] seeds = { 0, 1, 12345, -7 };
            foreach (var minute in new[] { 12 * 60, 20 * 60 })
            {
                var expected = LightAt(minute, 4f, seeds[0]);
                foreach (var seed in seeds)
                    Assert.AreEqual(expected, LightAt(minute, 4f, seed), $"minute {minute} seed {seed}");
            }
        }

        [Test]
        public void LightThresholdIsInclusive()
        {
            var framed = Ahead(5f, FishRadius, CameraTier.Basic);
            Assume.That(framed.IsValid);

            Assert.IsTrue(RecordingCameraRules.IsLitEnough(CameraTier.Basic, RecordingCameraRules.BasicMinLight));
            Assert.IsTrue(RecordingCameraRules.ApplyLight(framed, CameraTier.Basic, 0.5f).IsValid);
            Assert.AreEqual(RecordingSampleRejection.TooDark,
                RecordingCameraRules.ApplyLight(framed, CameraTier.Basic, 0.49f).Rejection);
        }

        [Test]
        public void BasicIsNeverTooDarkInShallowWaterBeforeDusk()
        {
            // The P3 -Record smoke: DiveTestArea's whole column is 0-8 m and the day starts at 08:00.
            for (var minute = DayIds.DayStartMinute; minute <= DaylightModel.DuskStartMinute; minute += 15)
            for (var depth = 0f; depth <= 8f; depth += 0.5f)
            {
                var light = LightAt(minute, depth, 3);
                foreach (var tier in new[] { CameraTier.None, CameraTier.Basic })
                    Assert.IsTrue(Ahead(5f, FishRadius, tier, light).IsValid, $"minute {minute} depth {depth}");
            }

            var morning = RecordingCameraRules.Light01(DaylightModel.Evaluate(0, DayPhase.Morning, 0), 8f);
            Assert.IsTrue(Ahead(5f, FishRadius, CameraTier.Basic, morning).IsValid,
                "Morning is lit as the day's start whatever the clock held");
        }

        [Test]
        public void AtNightBasicIsTooDarkAndProfessionalIsNot()
        {
            var light = LightAt(Night, 2f);
            Assert.AreEqual(DaylightModel.NightAmbient, light, Tolerance);

            Assert.AreEqual(RecordingSampleRejection.TooDark, Ahead(5f, FishRadius, CameraTier.Basic, light).Rejection);
            Assert.AreEqual(RecordingSampleRejection.TooDark, Ahead(5f, FishRadius, CameraTier.None, light).Rejection);
            Assert.AreEqual(RecordingSampleRejection.TooDark, Ahead(5f, FishRadius, CameraTier.Advanced, light).Rejection,
                "the first upgrade is reach, not light");
            Assert.IsTrue(Ahead(5f, FishRadius, CameraTier.Professional, light).IsValid);
        }

        [Test]
        public void DeepBasicIsTooDarkAndProfessionalIsNot()
        {
            var light = LightAt(Noon, 30f);
            Assert.AreEqual(1f - 0.8f * 22f / 27f, light, Tolerance);

            Assert.AreEqual(RecordingSampleRejection.TooDark, Ahead(5f, FishRadius, CameraTier.Basic, light).Rejection);
            Assert.IsTrue(Ahead(5f, FishRadius, CameraTier.Professional, light).IsValid);
        }

        [Test]
        public void BasicInDaylightIsExactlyTodaysEvaluator()
        {
            var poses = new (string Name, float Angle, float Distance, float Radius, bool Occluded)[]
            {
                ("valid", 0f, 5f, FishRadius, false),
                ("valid off-centre", 12f, 7f, FishRadius, false),
                ("occluded", 0f, 5f, FishRadius, true),
                ("too close", 0f, 0.5f, FishRadius, false),
                ("too far", 0f, 20f, LargeRadius, false),
                ("off frame", 40f, 5f, FishRadius, false),
                ("too small", 0f, 10f, 0.05f, false),
                ("invalid", 0f, 5f, float.NaN, false)
            };

            foreach (var framing in new[] { Default, Event })
            foreach (var tier in new[] { CameraTier.None, CameraTier.Basic })
            foreach (var pose in poses)
            {
                var direction = Quaternion.Euler(0f, pose.Angle, 0f) * Vector3.forward;
                var subject = direction * pose.Distance;
                var today = RecordingFraming.Evaluate(Vector3.zero, Vector3.forward, Fov, subject, pose.Radius,
                    pose.Occluded, framing);
                var now = RecordingCameraRules.Sample(Vector3.zero, Vector3.forward, Fov, subject, pose.Radius,
                    pose.Occluded, framing, tier, 1f);
                AssertSameSample(today, now, $"{pose.Name} ({tier}, far edge {framing.MaxDistanceMetres})");
            }
        }

        [Test]
        public void ProfessionalPassesTheWorstDesignedCase()
        {
            var worst = LightAt(Night, RecordingCameraRules.DarkDepthMetres);
            Assert.AreEqual(0.05f, worst, Tolerance);

            Assert.IsTrue(Ahead(5f, FishRadius, CameraTier.Professional, worst).IsValid);
            Assert.IsTrue(Ahead(5f, FishRadius, CameraTier.Professional, LightAt(Night, 100f)).IsValid);
        }

        [Test]
        public void ReachIsJudgedBeforeLight()
        {
            var night = LightAt(Night, 2f);

            Assert.AreEqual(RecordingSampleRejection.TooFar, Ahead(18f, LargeRadius, CameraTier.Basic, night).Rejection);
            Assert.AreEqual(RecordingSampleRejection.TooDark, Ahead(18f, LargeRadius, CameraTier.Advanced, night).Rejection);
            Assert.IsTrue(Ahead(18f, LargeRadius, CameraTier.Professional, night).IsValid);
        }

        [TestCase(RecordingSampleRejection.Occluded)]
        [TestCase(RecordingSampleRejection.TooFar)]
        [TestCase(RecordingSampleRejection.OffFrame)]
        public void AnAlreadyRejectedSampleKeepsItsOwnReason(RecordingSampleRejection reason)
        {
            RecordingSample dark;
            switch (reason)
            {
                case RecordingSampleRejection.Occluded:
                    dark = Ahead(5f, FishRadius, CameraTier.Basic, 0f, Default, occluded: true);
                    break;
                case RecordingSampleRejection.TooFar:
                    dark = Ahead(30f, LargeRadius, CameraTier.Basic, 0f);
                    break;
                default:
                    dark = At(40f, 5f, FishRadius, CameraTier.Basic, 0f, Default);
                    break;
            }

            Assert.IsFalse(dark.IsValid);
            Assert.AreEqual(reason, dark.Rejection, "the reason the diver can act on wins over darkness");
        }

        [Test]
        public void TooDarkScoresZeroButKeepsTheFramingNumbers()
        {
            var framed = At(10f, 6f, FishRadius, CameraTier.Basic, 1f, Default);
            Assume.That(framed.IsValid);

            var dark = RecordingCameraRules.ApplyLight(framed, CameraTier.Basic, 0.1f);

            Assert.IsFalse(dark.IsValid);
            Assert.AreEqual(RecordingSampleRejection.TooDark, dark.Rejection);
            Assert.AreEqual(0f, dark.Score01);
            Assert.AreEqual(framed.DistanceMetres, dark.DistanceMetres);
            Assert.AreEqual(framed.OffAxisDegrees, dark.OffAxisDegrees);
            Assert.AreEqual(framed.FrameFill, dark.FrameFill);
        }

        [Test]
        public void TooDarkIsAppendedWithoutRenumbering()
        {
            Assert.AreEqual(0, (int)RecordingSampleRejection.None);
            Assert.AreEqual(1, (int)RecordingSampleRejection.InvalidInput);
            Assert.AreEqual(2, (int)RecordingSampleRejection.Occluded);
            Assert.AreEqual(3, (int)RecordingSampleRejection.TooClose);
            Assert.AreEqual(4, (int)RecordingSampleRejection.TooFar);
            Assert.AreEqual(5, (int)RecordingSampleRejection.OffFrame);
            Assert.AreEqual(6, (int)RecordingSampleRejection.TooSmall);
            Assert.AreEqual(7, (int)RecordingSampleRejection.TooDark);
        }
    }
}
