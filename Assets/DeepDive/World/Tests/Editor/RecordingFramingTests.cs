using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class RecordingFramingTests
    {
        private const float Fov = 60f;
        private const float SubjectRadius = 0.5f;

        private static readonly Vector3 Eye = Vector3.zero;
        private static readonly Vector3 Forward = Vector3.forward;

        private static RecordingTuning Tuning => RecordingTuning.Default;

        // Straight ahead at the given distance, so tests only vary what they mean to vary.
        private static RecordingSample Ahead(float distance, bool occluded = false) =>
            RecordingFraming.Evaluate(Eye, Forward, Fov, new Vector3(0f, 0f, distance), SubjectRadius,
                occluded, Tuning);

        // Rotates the subject off the lens axis by the given angle, keeping the distance fixed.
        private static RecordingSample OffAxis(float degrees, float distance = 5f)
        {
            var direction = Quaternion.Euler(0f, degrees, 0f) * Vector3.forward;
            return RecordingFraming.Evaluate(Eye, Forward, Fov, direction * distance, SubjectRadius,
                false, Tuning);
        }

        [Test]
        public void AWellFramedSubjectInTheBandIsValidAndScoresAboveZero()
        {
            var sample = Ahead(5f);

            Assert.IsTrue(sample.IsValid);
            Assert.AreEqual(RecordingSampleRejection.None, sample.Rejection);
            Assert.Greater(sample.Score01, 0f);
            Assert.LessOrEqual(sample.Score01, 1f);
            Assert.AreEqual(5f, sample.DistanceMetres, 0.001f);
            Assert.AreEqual(0f, sample.OffAxisDegrees, 0.001f);
        }

        [Test]
        public void OcclusionRejectsEvenAPerfectlyFramedSubject()
        {
            Assert.IsTrue(Ahead(5f).IsValid, "the same shot is valid when nothing blocks it");

            var blocked = Ahead(5f, occluded: true);
            Assert.IsFalse(blocked.IsValid);
            Assert.AreEqual(RecordingSampleRejection.Occluded, blocked.Rejection);
        }

        [Test]
        public void DistanceBandRejectsBothEnds()
        {
            Assert.AreEqual(RecordingSampleRejection.TooClose, Ahead(1f).Rejection);
            Assert.AreEqual(RecordingSampleRejection.TooFar, Ahead(20f).Rejection);
            Assert.IsTrue(Ahead(5f).IsValid, "between the two edges the shot counts");
        }

        [Test]
        public void ASubjectOutsideTheFramingConeIsRejected()
        {
            Assert.IsTrue(OffAxis(10f).IsValid);
            Assert.AreEqual(RecordingSampleRejection.OffFrame, OffAxis(40f).Rejection);
        }

        [Test]
        public void ANarrowLensTightensTheConeBelowTheTuningThreshold()
        {
            // 15 degrees is inside the 22 degree tuning cone but outside a 20 degree lens, where
            // half the frame is only 10 degrees. The subject is literally off screen, so the
            // lens has to win over the threshold.
            var direction = Quaternion.Euler(0f, 15f, 0f) * Vector3.forward;
            var wide = RecordingFraming.Evaluate(Eye, Forward, 60f, direction * 5f, SubjectRadius, false, Tuning);
            var narrow = RecordingFraming.Evaluate(Eye, Forward, 20f, direction * 5f, SubjectRadius, false, Tuning);

            Assert.IsTrue(wide.IsValid);
            Assert.IsFalse(narrow.IsValid);
            Assert.AreEqual(RecordingSampleRejection.OffFrame, narrow.Rejection);
        }

        [Test]
        public void ASubjectTooSmallToIdentifyIsRejectedEvenWhenCentredAndInRange()
        {
            var sample = RecordingFraming.Evaluate(Eye, Forward, Fov, new Vector3(0f, 0f, 13f), 0.05f,
                false, Tuning);

            Assert.IsFalse(sample.IsValid);
            Assert.AreEqual(RecordingSampleRejection.TooSmall, sample.Rejection);
        }

        [Test]
        public void CentringTheSubjectScoresHigherThanLettingItDriftToTheEdge()
        {
            Assert.Greater(OffAxis(0f).Score01, OffAxis(15f).Score01);
        }

        [Test]
        public void GettingCloserScoresHigherBecauseTheSubjectFillsMoreOfTheFrame()
        {
            var near = Ahead(3f);
            var far = Ahead(9f);

            Assert.IsTrue(near.IsValid);
            Assert.IsTrue(far.IsValid);
            Assert.Greater(near.FrameFill, far.FrameFill);
            Assert.Greater(near.Score01, far.Score01);
        }

        [Test]
        public void EveryRejectionScoresZeroSoNoTimeCanBeBankedOffIt()
        {
            var rejected = new[]
            {
                Ahead(5f, occluded: true), Ahead(1f), Ahead(20f), OffAxis(40f),
                RecordingFraming.Evaluate(Eye, Forward, Fov, new Vector3(0f, 0f, 13f), 0.05f, false, Tuning)
            };

            foreach (var sample in rejected)
            {
                Assert.IsFalse(sample.IsValid, $"{sample.Rejection} must not be a valid sample");
                Assert.AreEqual(0f, sample.Score01, $"{sample.Rejection} must score nothing");
            }
        }

        [Test]
        public void UnusableInputIsRefusedInsteadOfProducingANaNScore()
        {
            var nan = new Vector3(float.NaN, 0f, 5f);
            Assert.AreEqual(RecordingSampleRejection.InvalidInput,
                RecordingFraming.Evaluate(Eye, Forward, Fov, nan, SubjectRadius, false, Tuning).Rejection);
            Assert.AreEqual(RecordingSampleRejection.InvalidInput,
                RecordingFraming.Evaluate(Eye, Vector3.zero, Fov, new Vector3(0f, 0f, 5f), SubjectRadius, false, Tuning).Rejection);
            Assert.AreEqual(RecordingSampleRejection.InvalidInput,
                RecordingFraming.Evaluate(Eye, Forward, Fov, new Vector3(0f, 0f, 5f), 0f, false, Tuning).Rejection);
            Assert.AreEqual(RecordingSampleRejection.InvalidInput,
                RecordingFraming.Evaluate(Eye, Forward, float.PositiveInfinity, new Vector3(0f, 0f, 5f), SubjectRadius, false, Tuning).Rejection);
        }

        [Test]
        public void FrameFillGrowsAsTheSubjectApproachesAndNeverExceedsTheFrame()
        {
            Assert.Greater(RecordingFraming.FrameFill(2f, 0.5f, Fov), RecordingFraming.FrameFill(8f, 0.5f, Fov));
            Assert.AreEqual(1f, RecordingFraming.FrameFill(0.5f, 40f, Fov), 0.0001f);
            Assert.AreEqual(0f, RecordingFraming.FrameFill(0f, 0.5f, Fov));
            Assert.AreEqual(0f, RecordingFraming.FrameFill(5f, float.NaN, Fov));
        }

        [Test]
        public void TuningFallsBackInsteadOfProducingACameraThatRefusesEverything()
        {
            var garbage = new RecordingTuning(float.NaN, -3f, 500f, -1f, 5f, float.PositiveInfinity);

            Assert.Greater(garbage.MinDistanceMetres, 0f);
            Assert.Greater(garbage.MaxDistanceMetres, garbage.MinDistanceMetres);
            Assert.Greater(garbage.MaxOffAxisDegrees, 0f);
            Assert.Greater(garbage.IdealFrameFill, garbage.MinFrameFill,
                "a size score of 1 must not be reachable at the rejection floor");
            Assert.GreaterOrEqual(garbage.CenteringWeight, 0f);
            Assert.LessOrEqual(garbage.CenteringWeight, 1f);
        }

        [Test]
        public void AnInvertedDistanceBandIsWidenedRatherThanLeftUnusable()
        {
            var inverted = new RecordingTuning(10f, 2f, 22f, 0.08f, 0.45f, 0.4f);

            Assert.AreEqual(10f, inverted.MinDistanceMetres, 0.0001f);
            Assert.GreaterOrEqual(inverted.MaxDistanceMetres, inverted.MinDistanceMetres);
        }
    }
}
