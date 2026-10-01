using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public class WaterRulesTests
    {
        // One sea: footprint +-15 on both axes, surface at y = 8.
        private static readonly WaterBody Sea = Box(new Vector3(0, 4, 0), new Vector3(30, 8, 30));

        private static WaterBody Box(Vector3 center, Vector3 size)
        {
            WaterBody.TryFromBox(center, size, out var body);
            return body;
        }

        // A standing diver measured from the feet: base sits at feet.y, top at feet.y + 2.
        private static WaterProbe Diver(Vector3 feet) =>
            new WaterProbe(feet, new Vector3(0, 1, 0), 0.5f, 2f, Vector3.up);

        private static EnvironmentLocomotion Classify(Vector3 feet) =>
            WaterRules.ClassifyRaw(new[] { Sea }, Diver(feet));

        [Test]
        public void OutsideTheFootprintIsLand()
        {
            Assert.AreEqual(EnvironmentLocomotion.Land, Classify(new Vector3(20, 2, 0)));
            Assert.AreEqual(EnvironmentLocomotion.Land, Classify(new Vector3(0, 2, -40)));
        }

        [Test]
        public void AboveTheSurfaceOverWaterIsLand()
        {
            Assert.AreEqual(EnvironmentLocomotion.Land, Classify(new Vector3(0, 12, 0)));
        }

        [Test]
        public void BaseInWaterHeadOutIsSurface()
        {
            Assert.AreEqual(EnvironmentLocomotion.Surface, Classify(new Vector3(0, 7, 0)));
        }

        [Test]
        public void WholeCapsuleUnderTheSurfaceIsUnderwater()
        {
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(0, 2, 0)));
        }

        [Test]
        public void BaseExactlyOnTheSurfaceIsLand()
        {
            // Feet at 8 puts the base exactly at the water line. Standing on it is not in it.
            Assert.AreEqual(EnvironmentLocomotion.Land, Classify(new Vector3(0, 8, 0)));
        }

        [Test]
        public void TopExactlyOnTheSurfaceIsUnderwater()
        {
            // Feet at 6 puts the top exactly at the water line. Level with it counts as under.
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(0, 6, 0)));
        }

        [Test]
        public void FootExactlyOnTheShoreEdgeIsOverWater()
        {
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(15, 2, 0)));
        }

        [Test]
        public void FootJustOutsideTheEdgeIsLandEvenWhenBodyOverhangsWater()
        {
            // The capsule spans x 14.51..15.51, so most of it is over water. The feet decide.
            Assert.AreEqual(EnvironmentLocomotion.Land, Classify(new Vector3(15.01f, 2, 0)));
        }

        [Test]
        public void FootJustInsideTheEdgeIsWaterEvenWhenBodyOverhangsLand()
        {
            // Mirror of the case above: most of the capsule hangs over land, the feet do not.
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(14.99f, 2, 0)));
        }

        [Test]
        public void StandingOnTheSeaFloorIsUnderwater()
        {
            // There is no floor in the rules, so resting on or just below the bed is wet.
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(0, 0, 0)));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, Classify(new Vector3(0, -0.01f, 0)));
        }

        [Test]
        public void LyingCapsuleUsesItsLowestAndHighestPoints()
        {
            // Same centre, same capsule, different orientation. Standing, the base reaches
            // 7.6 and is wet; lying flat it only reaches 8.1 and is dry.
            var center = new Vector3(0, 8.6f, 0);
            var standing = new WaterProbe(center, Vector3.zero, 0.5f, 2f, Vector3.up);
            var lying = new WaterProbe(center, Vector3.zero, 0.5f, 2f, Vector3.right);

            Assert.AreEqual(EnvironmentLocomotion.Surface, WaterRules.ClassifyRaw(new[] { Sea }, standing));
            Assert.AreEqual(EnvironmentLocomotion.Land, WaterRules.ClassifyRaw(new[] { Sea }, lying));
        }

        [Test]
        public void ShortCapsuleIsTreatedAsASphere()
        {
            // Height below the diameter has no straight section left, so it must classify
            // exactly as the sphere of the same radius.
            var center = new Vector3(0, 8.5f, 0);
            var squashed = new WaterProbe(center, Vector3.zero, 1f, 0.5f, Vector3.up);
            var sphere = new WaterProbe(center, Vector3.zero, 1f, 2f, Vector3.up);

            var squashedResult = WaterRules.ClassifyRaw(new[] { Sea }, squashed);
            Assert.AreEqual(WaterRules.ClassifyRaw(new[] { Sea }, sphere), squashedResult);
            Assert.AreEqual(EnvironmentLocomotion.Surface, squashedResult);
        }

        [TestCase("nan-position")]
        [TestCase("nan-offset")]
        [TestCase("negative-radius")]
        [TestCase("negative-height")]
        [TestCase("zero-up")]
        public void InvalidProbeIsLand(string kind)
        {
            var offset = new Vector3(0, 1, 0);
            var feet = new Vector3(0, 2, 0);
            var probe = kind switch
            {
                "nan-position" => new WaterProbe(new Vector3(float.NaN, 2, 0), offset, 0.5f, 2f, Vector3.up),
                "nan-offset" => new WaterProbe(feet, new Vector3(0, float.NaN, 0), 0.5f, 2f, Vector3.up),
                "negative-radius" => new WaterProbe(feet, offset, -0.5f, 2f, Vector3.up),
                "negative-height" => new WaterProbe(feet, offset, 0.5f, -2f, Vector3.up),
                _ => new WaterProbe(feet, offset, 0.5f, 2f, Vector3.zero)
            };

            // Each of these otherwise sits deep in the sea, so only the rejection makes it dry.
            Assert.AreEqual(EnvironmentLocomotion.Land, WaterRules.ClassifyRaw(new[] { Sea }, probe));
        }

        [Test]
        public void NoWaterIsLand()
        {
            var probe = Diver(new Vector3(0, 2, 0));
            Assert.AreEqual(EnvironmentLocomotion.Land, WaterRules.ClassifyRaw(new WaterBody[0], probe));
            Assert.AreEqual(EnvironmentLocomotion.Land, WaterRules.ClassifyRaw(null, probe));
        }

        [Test]
        public void WettestBodyWins()
        {
            // Same footprint, higher surface. Against the shallow sea alone the head is out.
            var deep = Box(new Vector3(0, 5, 0), new Vector3(30, 10, 30));
            var probe = Diver(new Vector3(0, 7, 0));

            Assert.AreEqual(EnvironmentLocomotion.Surface, WaterRules.ClassifyRaw(new[] { Sea }, probe));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, WaterRules.ClassifyRaw(new[] { Sea, deep }, probe));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, WaterRules.ClassifyRaw(new[] { deep, Sea }, probe));
        }

        [Test]
        public void BodyFromSwimVolumeBoxMatchesItsTopAndSides()
        {
            Assert.IsTrue(WaterBody.TryFromBox(new Vector3(0, 4, 0), new Vector3(30, 8, 30), out var body));
            Assert.AreEqual(8f, body.SurfaceY);
            Assert.AreEqual(-15f, body.MinX);
            Assert.AreEqual(15f, body.MaxX);
            Assert.AreEqual(-15f, body.MinZ);
            Assert.AreEqual(15f, body.MaxZ);
        }

        [TestCase(0f, 8f, 30f)]
        [TestCase(30f, 0f, 30f)]
        [TestCase(30f, 8f, 0f)]
        [TestCase(-30f, 8f, 30f)]
        [TestCase(float.NaN, 8f, 30f)]
        public void InvalidBoxIsRejected(float sizeX, float sizeY, float sizeZ)
        {
            Assert.IsFalse(WaterBody.TryFromBox(
                new Vector3(0, 4, 0), new Vector3(sizeX, sizeY, sizeZ), out _));
        }
    }
}
