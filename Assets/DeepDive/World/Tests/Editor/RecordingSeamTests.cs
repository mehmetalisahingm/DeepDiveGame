using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class RecordingClaimBindingTests
    {
        private sealed class FakeEconomy : IRecordingSink
        {
            public PlayerActionResult TryClaim(RecordingResult result) => PlayerActionResult.Accepted;
        }

        [TearDown]
        public void ClearBinding() => RecordingClaim.Unbind();

        [Test]
        public void SinkStartsUnboundAndBindingIsReversible()
        {
            RecordingClaim.Unbind();
            Assert.IsFalse(RecordingClaim.IsBound);
            Assert.IsNull(RecordingClaim.Sink);

            RecordingClaim.Bind(new FakeEconomy());
            Assert.IsTrue(RecordingClaim.IsBound);

            RecordingClaim.Unbind();
            Assert.IsFalse(RecordingClaim.IsBound);
            Assert.IsNull(RecordingClaim.Sink);
        }
    }

    public class RecorderViewsTests
    {
        private static readonly PlayerId Alice = new PlayerId(0);
        private static readonly PlayerId Bob = new PlayerId(1);

        private sealed class FakeCamera : IRecorderView
        {
            public bool IsActive { get; set; } = true;
            public Vector3 EyePosition { get; set; }
            public Vector3 EyeForward { get; set; } = Vector3.forward;
            public float VerticalFieldOfViewDegrees { get; set; } = 60f;
        }

        [SetUp]
        [TearDown]
        public void ClearBindings() => RecorderViews.UnbindAll();

        [Test]
        public void AnUnboundPlayerHasNoCameraAndCannotRecord()
        {
            Assert.IsFalse(RecorderViews.IsBound(Alice));
            Assert.IsFalse(RecorderViews.TryGetActive(Alice, out var view));
            Assert.IsNull(view);
        }

        [Test]
        public void EachDiverGetsTheirOwnCamera()
        {
            var aliceCamera = new FakeCamera { EyePosition = new Vector3(1f, 0f, 0f) };
            var bobCamera = new FakeCamera { EyePosition = new Vector3(2f, 0f, 0f) };

            RecorderViews.Bind(Alice, aliceCamera);
            RecorderViews.Bind(Bob, bobCamera);

            Assert.AreEqual(2, RecorderViews.Count);
            Assert.IsTrue(RecorderViews.TryGetActive(Alice, out var found));
            Assert.AreSame(aliceCamera, found);
            Assert.IsTrue(RecorderViews.TryGetActive(Bob, out found));
            Assert.AreSame(bobCamera, found);
        }

        [Test]
        public void ADiverWhoCannotFilmIsTreatedAsHavingNoCamera()
        {
            var camera = new FakeCamera { IsActive = false };
            RecorderViews.Bind(Alice, camera);

            Assert.IsTrue(RecorderViews.IsBound(Alice), "the binding is still there");
            Assert.IsFalse(RecorderViews.TryGetActive(Alice, out var view), "but it cannot be filmed through");
            Assert.IsNull(view);

            camera.IsActive = true;
            Assert.IsTrue(RecorderViews.TryGetActive(Alice, out view));
            Assert.AreSame(camera, view);
        }

        [Test]
        public void BindingNullClearsThePlayerInsteadOfStoringAHole()
        {
            RecorderViews.Bind(Alice, new FakeCamera());
            RecorderViews.Bind(Alice, null);

            Assert.IsFalse(RecorderViews.IsBound(Alice));
            Assert.IsFalse(RecorderViews.TryGetActive(Alice, out _));
        }

        [Test]
        public void RebindingReplacesTheEarlierCamera()
        {
            var first = new FakeCamera();
            var second = new FakeCamera();

            RecorderViews.Bind(Alice, first);
            RecorderViews.Bind(Alice, second);

            Assert.AreEqual(1, RecorderViews.Count);
            Assert.IsTrue(RecorderViews.TryGetActive(Alice, out var view));
            Assert.AreSame(second, view);
        }

        [Test]
        public void UnbindingOneDiverLeavesTheOthers()
        {
            RecorderViews.Bind(Alice, new FakeCamera());
            RecorderViews.Bind(Bob, new FakeCamera());

            RecorderViews.Unbind(Alice);
            Assert.IsFalse(RecorderViews.IsBound(Alice));
            Assert.IsTrue(RecorderViews.IsBound(Bob));
        }

        [Test]
        public void UnbindAllClearsEveryCameraSoNoneSurvivesTheSession()
        {
            RecorderViews.Bind(Alice, new FakeCamera());
            RecorderViews.Bind(Bob, new FakeCamera());

            RecorderViews.UnbindAll();
            Assert.AreEqual(0, RecorderViews.Count);
        }

        // The seam exists so the framing rules can read a live pose every tick rather than a
        // copy taken at start: a diver who turns away must stop banking time immediately.
        [Test]
        public void TheFramingRulesSeeTheCameraMoveWithoutRebinding()
        {
            var camera = new FakeCamera { EyePosition = Vector3.zero, EyeForward = Vector3.forward };
            RecorderViews.Bind(Alice, camera);
            var subject = new Vector3(0f, 0f, 5f);

            Assert.IsTrue(RecorderViews.TryGetActive(Alice, out var view));
            var facing = RecordingFraming.Evaluate(view.EyePosition, view.EyeForward,
                view.VerticalFieldOfViewDegrees, subject, 0.5f, false, RecordingTuning.Default);
            Assert.IsTrue(facing.IsValid);

            camera.EyeForward = Vector3.back;
            var turnedAway = RecordingFraming.Evaluate(view.EyePosition, view.EyeForward,
                view.VerticalFieldOfViewDegrees, subject, 0.5f, false, RecordingTuning.Default);
            Assert.IsFalse(turnedAway.IsValid);
            Assert.AreEqual(RecordingSampleRejection.OffFrame, turnedAway.Rejection);
        }
    }

    // What a subject is filmed as decides who gets paid for it, so the fallback rule is pulled
    // out of the component and tested on its own rather than only through a spawned fish.
    public class RecordingSubjectIdTests
    {
        [Test]
        public void AnExplicitIdWins()
        {
            // How a scripted event gets filmed without a FishActor of its own.
            Assert.AreEqual("kelp_bloom", RecordingSubject.ResolveSubjectId("kelp_bloom", "sea_bass"));
        }

        [Test]
        public void WithoutAnExplicitIdTheSpeciesIsUsed()
        {
            Assert.AreEqual("sea_bass", RecordingSubject.ResolveSubjectId("", "sea_bass"));
            Assert.AreEqual("sea_bass", RecordingSubject.ResolveSubjectId(null, "sea_bass"));
            Assert.AreEqual("sea_bass", RecordingSubject.ResolveSubjectId("   ", "sea_bass"));
        }

        // An id with stray whitespace would key the ledger and Mert's price lookup differently
        // from the same id typed cleanly, so both sides are trimmed.
        [Test]
        public void SurroundingWhitespaceIsTrimmedFromEitherSource()
        {
            Assert.AreEqual("kelp_bloom", RecordingSubject.ResolveSubjectId("  kelp_bloom \n", null));
            Assert.AreEqual("sea_bass", RecordingSubject.ResolveSubjectId(null, " sea_bass "));
        }

        // Empty is the misconfigured case: RecordingSession refuses to open a take on it and
        // RecordingTake.IsPayable is false, so nothing unidentifiable can reach the economy.
        [Test]
        public void WithNeitherSourceTheSubjectIsUnidentifiable()
        {
            Assert.AreEqual("", RecordingSubject.ResolveSubjectId(null, null));
            Assert.AreEqual("", RecordingSubject.ResolveSubjectId("", ""));
            Assert.AreEqual("", RecordingSubject.ResolveSubjectId("  ", "  "));
        }
    }
}
