using DeepDive.Composition;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.Tests.P3A
{
    public sealed class PlayerWaterLocomotionBindingTests
    {
        [Test]
        public void BuildProbe_ConvertsCharacterControllerToWorldCapsule()
        {
            var go = new GameObject("P3_WATER_PROBE_TEST");
            try
            {
                go.transform.position = new Vector3(3f, 2f, -4f);
                go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                go.transform.localScale = new Vector3(2f, 3f, 4f);

                var controller = go.AddComponent<CharacterController>();
                controller.center = new Vector3(0.2f, 0.9f, 0f);
                controller.radius = 0.35f;
                controller.height = 1.8f;

                var probe = PlayerWaterLocomotionBinding.BuildProbe(go.transform, controller);

                AssertVector(go.transform.position, probe.Position);
                AssertVector(go.transform.TransformVector(controller.center), probe.CenterOffsetWorld);
                AssertVector(go.transform.up.normalized, probe.Up);
                Assert.That(probe.Radius, Is.EqualTo(1.4f).Within(0.0001f));
                Assert.That(probe.Height, Is.EqualTo(5.4f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildProbe_ClampsHeightToCapsuleDiameter()
        {
            var go = new GameObject("P3_WATER_PROBE_SHORT_TEST");
            try
            {
                var controller = go.AddComponent<CharacterController>();
                controller.center = Vector3.up;
                controller.radius = 0.8f;
                controller.height = 1f;

                var probe = PlayerWaterLocomotionBinding.BuildProbe(go.transform, controller);

                Assert.That(probe.Radius, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(probe.Height, Is.EqualTo(1.6f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BindingRunsBeforeDefaultPlayerFixedUpdate()
        {
            var attributes = typeof(PlayerWaterLocomotionBinding)
                .GetCustomAttributes(typeof(DefaultExecutionOrder), false);

            Assert.That(attributes, Has.Length.EqualTo(1));
            Assert.That(((DefaultExecutionOrder)attributes[0]).order, Is.LessThan(0));
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f),
                $"expected {expected} but got {actual}");
        }
    }
}
