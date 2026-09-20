using System.Linq;
using System.Reflection;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // The shallow band and the component that measures depth against the scene's water line.
    // Pure: no scene is opened here. The band's relationship to DiveTestArea - that its surface
    // matches SwimVolume's top, that the fish stays inside it, and that deleting it changes no
    // water classification at all - is pinned by the beach scene tests instead.
    public class DepthBandTests
    {
        private GameObject host;
        private DiveDepthBandSet bands;

        [SetUp]
        public void CreateBandSet()
        {
            host = new GameObject("DepthBands");
            bands = host.AddComponent<DiveDepthBandSet>();
        }

        [TearDown]
        public void DestroyBandSet()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        // --- The band itself ----------------------------------------------------------------

        [Test]
        public void ShallowBandIsTheDocumentedZeroToEightRange()
        {
            Assert.AreEqual("shallow", DiveDepthBands.Shallow.Id, "id");
            Assert.AreEqual(0f, DiveDepthBands.Shallow.MinDepth, "min depth");
            Assert.AreEqual(8f, DiveDepthBands.Shallow.MaxDepth, "max depth");
            Assert.IsTrue(DiveDepthBands.Shallow.IsValid);
        }

        [Test]
        public void ShallowIsTheOnlyBandToday()
        {
            // P4.4 adds the deeper cuts. Until then a second band appearing here would change
            // what "shallow" means for every reader without anyone having to say so.
            Assert.AreEqual(1, DiveDepthBands.All.Count, "band count");
            Assert.AreEqual(DiveDepthBands.ShallowId, DiveDepthBands.All[0].Id);
        }

        [Test]
        public void ContainsIsInclusiveAtBothEnds()
        {
            // A diver exactly on the water line is in shallow water, and so is one on the sea
            // bed of this arena. Neither end is a rounding error away from belonging nowhere.
            Assert.IsTrue(DiveDepthBands.Shallow.Contains(0f), "water line");
            Assert.IsTrue(DiveDepthBands.Shallow.Contains(8f), "sea bed");
            Assert.IsTrue(DiveDepthBands.Shallow.Contains(4f), "mid water");
        }

        [Test]
        public void ContainsRejectsJustOutside()
        {
            Assert.IsFalse(DiveDepthBands.Shallow.Contains(-0.0001f), "above the water line");
            Assert.IsFalse(DiveDepthBands.Shallow.Contains(8.0001f), "below the band");
        }

        [Test]
        public void ContainsRejectsNaNAndInfinity()
        {
            Assert.IsFalse(DiveDepthBands.Shallow.Contains(float.NaN), "NaN");
            Assert.IsFalse(DiveDepthBands.Shallow.Contains(float.PositiveInfinity), "+inf");
            Assert.IsFalse(DiveDepthBands.Shallow.Contains(float.NegativeInfinity), "-inf");
        }

        [Test]
        public void TryFindReturnsShallowInsideAndFailsOutside()
        {
            Assert.IsTrue(DiveDepthBands.TryFind(4f, out var found), "4 m is shallow");
            Assert.AreEqual(DiveDepthBands.ShallowId, found.Id);

            Assert.IsFalse(DiveDepthBands.TryFind(9f, out var missed), "9 m is past the band");
            Assert.IsFalse(missed.IsValid, "a failed find must not leak a usable band");
        }

        [TestCase(5f, 5f, TestName = "zero width")]
        [TestCase(8f, 2f, TestName = "max before min")]
        [TestCase(-1f, 8f, TestName = "negative min")]
        [TestCase(0f, float.NaN, TestName = "NaN max")]
        public void InvalidBandsAreRejectedAndContainNothing(float min, float max)
        {
            var band = new DepthBand("broken", min, max);
            Assert.IsFalse(band.IsValid, "IsValid");

            // The point of returning false from Contains rather than trusting the caller to
            // check IsValid: a malformed band is empty, not universal.
            Assert.IsFalse(band.Contains(min), "min");
            Assert.IsFalse(band.Contains(4f), "mid");
        }

        [Test]
        public void AnEmptyIdIsNotAValidBand()
        {
            Assert.IsFalse(new DepthBand("", 0f, 8f).IsValid);
            Assert.IsFalse(new DepthBand(null, 0f, 8f).IsValid);
        }

        [Test]
        public void DepthBandCarriesNoUnityType()
        {
            // The band is the pure layer, like WaterBody's numbers rather than its collider.
            // A UnityEngine type creeping in would make it need a scene to be reasoned about.
            foreach (var field in typeof(DepthBand).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Assert.AreNotEqual("UnityEngine", field.FieldType.Namespace, field.Name);
        }

        // --- Depth measured against the scene's water line -----------------------------------

        [Test]
        public void DepthAtDerivesDepthFromTheSurface()
        {
            Assert.AreEqual(8f, bands.SurfaceY, "default surface");
            Assert.AreEqual(0f, bands.DepthAt(8f), 0.0001f, "the water line is depth zero");
            Assert.AreEqual(8f, bands.DepthAt(0f), 0.0001f, "the sea bed is 8 m down");
            Assert.AreEqual(-1f, bands.DepthAt(9f), 0.0001f, "above the surface is negative");
        }

        [Test]
        public void AboveTheSurfaceIsNotInTheShallowBand()
        {
            // The beach platform's top sits at y = 8.4. Standing on it is being on the beach,
            // not being in shallow water, and the band has to say so.
            Assert.IsFalse(bands.TryClassify(8.4f, out _), "dry sand");

            // The foot of the wade slope, 0.8 m under the line, is.
            Assert.IsTrue(bands.TryClassify(7.2f, out var band), "the wade foot");
            Assert.AreEqual(DiveDepthBands.ShallowId, band.Id);
        }

        [Test]
        public void TheWholeArenaColumnIsShallow()
        {
            // DiveTestArea's sea bed is at y = 0 and its surface at y = 8, so every point in
            // the water is inside the one band. This is what makes "the base fish is shallow"
            // true by construction rather than by placement.
            for (var y = 0f; y <= 8f; y += 0.5f)
                Assert.IsTrue(bands.TryClassify(y, out _), "y=" + y + " must be shallow");
        }

        [Test]
        public void NonFiniteSurfaceFallsBackToTheDefault()
        {
            bands.ConfigureSurface(float.NaN);
            Assert.AreEqual(DiveDepthBandSet.DefaultSurfaceY, bands.SurfaceY, "NaN");

            bands.ConfigureSurface(float.PositiveInfinity);
            Assert.AreEqual(DiveDepthBandSet.DefaultSurfaceY, bands.SurfaceY, "+inf");

            bands.ConfigureSurface(12.5f);
            Assert.AreEqual(12.5f, bands.SurfaceY, "a real water line is kept");
        }

        // --- It is not a water authority -----------------------------------------------------

        [Test]
        public void DiveDepthBandSetHasExactlyOneSerializedField()
        {
            var serialized = typeof(DiveDepthBandSet)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                           BindingFlags.DeclaredOnly)
                .Where(f => f.IsPublic || f.IsDefined(typeof(SerializeField), false))
                .ToArray();

            Assert.AreEqual(1, serialized.Length,
                "the band set must hold only the water line: " + string.Join(", ", serialized.Select(f => f.Name)));
            Assert.AreEqual("surfaceY", serialized[0].Name);
        }

        [Test]
        public void DiveDepthBandSetIsNotAWaterField()
        {
            // IWaterField is the only thing PlayerWaterLocomotionBinding will accept as a
            // tracker. Not implementing it is what makes a second water authority impossible
            // rather than merely unintended.
            Assert.IsFalse(typeof(IWaterField).IsAssignableFrom(typeof(DiveDepthBandSet)),
                "the band set must not be usable as a water field");
            Assert.IsFalse(typeof(DiveDepthBandSet).IsAssignableFrom(typeof(WaterField)),
                "and WaterField must not be one of these either");
        }

        [Test]
        public void DiveDepthBandSetNeverNamesEnvironmentLocomotion()
        {
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var locomotion = typeof(EnvironmentLocomotion);

            foreach (var field in typeof(DiveDepthBandSet).GetFields(flags))
                Assert.AreNotEqual(locomotion, field.FieldType, "field " + field.Name);

            foreach (var property in typeof(DiveDepthBandSet).GetProperties(flags))
                Assert.AreNotEqual(locomotion, property.PropertyType, "property " + property.Name);

            foreach (var method in typeof(DiveDepthBandSet).GetMethods(flags))
            {
                Assert.AreNotEqual(locomotion, method.ReturnType, "returns of " + method.Name);
                foreach (var parameter in method.GetParameters())
                    Assert.AreNotEqual(locomotion, parameter.ParameterType,
                        method.Name + "(" + parameter.Name + ")");
            }
        }
    }
}
