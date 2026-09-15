using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class DiverLocomotionTests
    {
        private GameObject diver;
        private Animator animator;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/DeepDive/Player/Character/DiverCharacter.prefab");
            Assert.That(prefab, Is.Not.Null, "The shipped diver prefab must be available.");
            diver = Object.Instantiate(prefab);
            animator = diver.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (diver != null) Object.DestroyImmediate(diver);
        }

        [Test]
        public void LandIdleAndWalkStayStableAndFollowSpeed()
        {
            SetMode(0, 0f);
            AssertStableClip("Dive_Idle");
            SetMode(0, 1f);
            AssertStableClip("Dive_Walk");
            SetMode(0, 0f);
            AssertStableClip("Dive_Idle");
        }

        [Test]
        public void SurfaceSpeedSelectsTreadingOrSwimmingWithoutLeavingSurface()
        {
            SetMode(1, 0f);
            AssertStableClip("Dive_TreadWater");
            SetMode(1, 1f);
            AssertStableClip("Dive_SwimSurface");
            SetMode(1, 0f);
            AssertStableClip("Dive_TreadWater");
        }

        [TestCase(0, 1f, 1, 0f, "Dive_TreadWater")]
        [TestCase(0, 1f, 2, 1f, "Dive_SwimUnderwater")]
        [TestCase(1, 0f, 2, 1f, "Dive_SwimUnderwater")]
        [TestCase(1, 1f, 2, 1f, "Dive_SwimUnderwater")]
        [TestCase(1, 0f, 0, 0f, "Dive_Idle")]
        [TestCase(1, 1f, 0, 1f, "Dive_Walk")]
        [TestCase(2, 1f, 0, 1f, "Dive_Walk")]
        [TestCase(2, 1f, 1, 0f, "Dive_TreadWater")]
        [TestCase(2, 1f, 1, 1f, "Dive_SwimSurface")]
        [TestCase(2, 1f, 3, 0f, "Dive_Idle")]
        [TestCase(2, 1f, 4, 0f, "Dive_Idle")]
        [TestCase(3, 0f, 2, 1f, "Dive_SwimUnderwater")]
        [TestCase(4, 0f, 0, 1f, "Dive_Walk")]
        public void ModeChangesReachTheRequestedClip(int fromMode, float fromSpeed,
            int toMode, float toSpeed, string expectedClip)
        {
            SetMode(fromMode, fromSpeed);
            Advance();
            SetMode(toMode, toSpeed);
            AssertStableClip(expectedClip);
        }

        [Test]
        public void VisualAnimationNeverMovesTheNetworkRoot()
        {
            Assert.That(animator.applyRootMotion, Is.False);
            var position = diver.transform.position;
            var rotation = diver.transform.rotation;
            SetMode(0, 1f);
            Advance();
            SetMode(2, 1f);
            Advance();
            Assert.That(Vector3.Distance(position, diver.transform.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(rotation, diver.transform.rotation), Is.LessThan(0.001f));
        }

        private void SetMode(int mode, float speed)
        {
            animator.SetInteger("LocomotionMode", mode);
            animator.SetFloat("MoveSpeed", speed);
        }

        private void AssertStableClip(string expected)
        {
            for (var sample = 0; sample < 3; sample++)
            {
                Advance();
                Assert.That(animator.IsInTransition(0), Is.False, "The mode must settle instead of retriggering.");
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                Assert.That(clips.Length, Is.GreaterThan(0));
                var dominant = clips.OrderByDescending(clip => clip.weight).First();
                Assert.That(dominant.clip.name, Is.EqualTo(expected));
                Assert.That(dominant.weight, Is.GreaterThan(0.99f));
            }
        }

        private void Advance()
        {
            for (var frame = 0; frame < 40; frame++) animator.Update(1f / 60f);
        }
    }
}
