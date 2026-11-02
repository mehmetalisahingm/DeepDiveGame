using System.Reflection;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #219: a real CharacterController and the eye heights SurfaceCameraRules.Lift goes through. Red without the
    // self filter: from 1.624-1.632 m a downward ray stopped on the diver's own capsule, 1.4 cm from the eye.
    public sealed class EyeRayTests
    {
        private static readonly Vector3 Feet = new Vector3(500f, 500f, 500f);
        private static readonly Vector3 Down = new Vector3(0.2f, -0.3f, 1f).normalized;
        private const float BadEye = 1.628f;

        private GameObject diver, wall;
        private CharacterController capsule;

        [SetUp]
        public void Build()
        {
            diver = new GameObject("EyeRay_Diver");
            capsule = diver.AddComponent<CharacterController>();
            capsule.height = 1.8f; capsule.radius = 0.35f; capsule.center = new Vector3(0f, 0.9f, 0f); capsule.skinWidth = 0.08f;
            diver.transform.position = Feet;

            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "EyeRay_Target";
            wall.transform.localScale = new Vector3(20f, 20f, 1f);
            wall.transform.position = Feet + new Vector3(0f, 0f, 10f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Clean()
        {
            Object.DestroyImmediate(diver);
            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
        }

        [Test]
        public void ThePlainRayReallyStopsOnTheDiverAtTheBadEyeHeight()
        {
            Assert.IsTrue(Physics.Raycast(Feet + Vector3.up * BadEye, Down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual(capsule, hit.collider, "the fault this helper exists for must still be measurable");
        }

        [Test]
        public void TheEyeRayReachesTheTargetFromEveryEyeHeight()
        {
            for (var height = 1.5f; height <= 1.95f; height += 0.002f)
            {
                Assert.IsTrue(EyeRay.Raycast(Feet + Vector3.up * height, Down, 30f, QueryTriggerInteraction.Ignore, capsule, out var hit), "h=" + height);
                Assert.AreEqual(wall.GetComponent<Collider>(), hit.collider, "h=" + height);
            }
        }

        [Test]
        public void TheEyeRayStillStopsOnOtherSolids()
        {
            var between = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                between.transform.localScale = new Vector3(20f, 20f, 1f);
                between.transform.position = Feet + new Vector3(0f, 0f, 5f);
                Physics.SyncTransforms();
                Assert.IsTrue(EyeRay.Raycast(Feet + Vector3.up * BadEye, Down, 30f, QueryTriggerInteraction.Ignore, capsule, out var hit));
                Assert.AreEqual(between.GetComponent<Collider>(), hit.collider);
                Assert.IsFalse(EyeRay.Raycast(Feet + Vector3.up * BadEye, Vector3.back, 30f, QueryTriggerInteraction.Ignore, capsule, out _), "nothing else behind");
            }
            finally { Object.DestroyImmediate(between); }
        }

        [Test]
        public void ThePickupStackSkipsTheDiverToo()
        {
            var hits = Physics.RaycastAll(Feet + Vector3.up * BadEye, Down, 30f, ~0, QueryTriggerInteraction.Collide);
            Assert.AreEqual(capsule, PickupRaycastRules.SelectFirstPickupOrBlocker((RaycastHit[])hits.Clone()), "without the self filter");
            Assert.AreEqual(wall.GetComponent<Collider>(), PickupRaycastRules.SelectFirstPickupOrBlocker(hits, capsule));
        }

        // Recording's occlusion check takes the eye, not the diver, so it recognises a capsule right at the eye.
        [Test]
        public void TheRecordingEyeIsNotOccludedByItsOwnCapsule()
        {
            var subject = new GameObject("EyeRay_Subject");
            try
            {
                subject.transform.position = Feet + new Vector3(0f, -2f, 8f);
                var component = subject.AddComponent<RecordingSubject>();
                typeof(RecordingSubject).GetField("occluderLayers", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(component, (LayerMask)~0);
                var occluded = typeof(RecordingSubject).GetMethod("IsOccluded", BindingFlags.NonPublic | BindingFlags.Instance);
                var eye = Feet + Vector3.up * BadEye;
                Assert.IsTrue(Physics.Raycast(eye, (subject.transform.position - eye).normalized, out var self, 30f, ~0, QueryTriggerInteraction.Ignore));
                Assert.AreEqual(capsule, self.collider, "the line really crosses the capsule");
                Assert.IsFalse((bool)occluded.Invoke(component, new object[] { eye, subject.transform.position }));

                // Another diver standing in the shot still blocks it.
                var other = new GameObject("EyeRay_OtherDiver");
                try
                {
                    var otherCapsule = other.AddComponent<CharacterController>();
                    otherCapsule.height = 1.8f; otherCapsule.radius = 0.35f; otherCapsule.center = new Vector3(0f, 0.9f, 0f);
                    other.transform.position = Vector3.Lerp(eye, subject.transform.position, 0.5f) - Vector3.up * 0.9f;
                    Physics.SyncTransforms();
                    Assert.IsTrue((bool)occluded.Invoke(component, new object[] { eye, subject.transform.position }));
                }
                finally { Object.DestroyImmediate(other); }
            }
            finally { Object.DestroyImmediate(subject); }
        }
    }
}
