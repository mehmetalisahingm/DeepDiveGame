using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // P4.3-B2 (#108): the three depth cuts - shallow 0-8 m (frozen), reef (8, 20), deep 20-35 m, nothing past 35 -
    // their edges, their single home in DiveDepthBands, and the camera rule that is tied to them. The shallow band's
    // own tests (DepthBandTests) are untouched; this file pins what P4.3 added on top of them.
    public class DiveDepthBandsTests
    {
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 65f, 8f) };

        // --- Band edges ----------------------------------------------------------------------

        [TestCase(0f, DepthBandIds.Shallow, TestName = "0 m is shallow")]
        [TestCase(8f, DepthBandIds.Shallow, TestName = "8 m is shallow (frozen, shallow is inclusive)")]
        [TestCase(8.0001f, DepthBandIds.Reef, TestName = "just past 8 m is reef")]
        [TestCase(19.999f, DepthBandIds.Reef, TestName = "just short of 20 m is reef")]
        [TestCase(20f, DepthBandIds.Deep, TestName = "20 m is deep")]
        [TestCase(35f, DepthBandIds.Deep, TestName = "35 m is deep")]
        [TestCase(35.001f, "", TestName = "just past 35 m has no band")]
        [TestCase(float.NaN, "", TestName = "NaN has no band")]
        [TestCase(float.PositiveInfinity, "", TestName = "infinity has no band")]
        [TestCase(-0.01f, "", TestName = "above the water line has no band")]
        public void BandOfDepth(float depth, string expected)
        {
            var found = DiveDepthBands.TryFind(depth, out var band);

            Assert.That(found, Is.EqualTo(expected.Length > 0), "found");
            Assert.That(found ? band.Id : string.Empty, Is.EqualTo(expected));
        }

        [Test]
        public void BandsAreDisjointAtEveryBoundary()
        {
            // Every edge and a hair either side of it belongs to at most one band; 8.0 is the one shared point and
            // the list order gives it to shallow, which BandOfDepth pins.
            foreach (var edge in new[] { 0f, 8f, 20f, 35f })
            foreach (var depth in new[] { edge - 0.0001f, edge + 0.0001f, edge == 8f ? 7.9999f : edge })
            {
                var owners = DiveDepthBands.All.Count(b => b.Contains(depth));
                Assert.That(owners, Is.LessThanOrEqualTo(1), "depth " + depth + " belongs to " + owners + " bands");
            }

            Assert.That(DiveDepthBands.Reef.Contains(DiveDepthBands.ReefMaxDepth), Is.False, "reef's bottom is open");
            Assert.That(DiveDepthBands.Deep.Contains(DiveDepthBands.DeepMinDepth), Is.True, "deep's top is closed");
        }

        [Test]
        public void EachEdgeIsThePreviousBandsEdge()
        {
            Assert.That(DiveDepthBands.ReefMinDepth, Is.EqualTo(DiveDepthBands.ShallowMaxDepth));
            Assert.That(DiveDepthBands.DeepMinDepth, Is.EqualTo(DiveDepthBands.ReefMaxDepth));
            Assert.That(DiveDepthBands.ReefMaxDepth, Is.EqualTo(20f));
            Assert.That(DiveDepthBands.DeepMaxDepth, Is.EqualTo(35f));
            Assert.That(DiveDepthBands.ReefId, Is.EqualTo(DepthBandIds.Reef));
            Assert.That(DiveDepthBands.DeepId, Is.EqualTo(DepthBandIds.Deep));
        }

        [Test]
        public void TheOldThreeArgumentBandIsStillClosedAtTheBottom()
        {
            var band = new DepthBand("x", 0f, 8f);
            Assert.That(band.MaxInclusive, Is.True);
            Assert.That(band.Contains(8f), Is.True);
            Assert.That(new DepthBand("x", 0f, 8f, false).Contains(8f), Is.False);
        }

        // --- One home for the metres ------------------------------------------------------------

        // A metre value for a depth edge is written as a number exactly once, in DepthBand.cs. Every other depth
        // constant in DeepDive's code must name one of those (the camera's Lit/Dark edges do); a second literal 35
        // or 20 somewhere would be a copy that can drift. Source text, because a const alias and a copied literal
        // compile to the same metadata.
        [Test]
        public void DepthEdgeMetresAreWrittenOnlyInDiveDepthBands()
        {
            var literal = new Regex(@"const\s+float\s+(\w*(Depth|Dark|Lit)\w*)\s*=\s*-?\d+(\.\d+)?f\s*;");
            var root = Path.Combine(Application.dataPath, "DeepDive");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Replace('\\', '/').Contains("/Tests/"))
                .Where(p => Path.GetFileName(p) != "DepthBand.cs")
                .SelectMany(p => literal.Matches(File.ReadAllText(p)).Cast<Match>()
                    .Select(m => Path.GetFileName(p) + ": " + m.Value))
                .ToArray();

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test]
        public void TheCameraLightEdgesAreTheBandEdges()
        {
            Assert.That(RecordingCameraRules.LitDepthMetres, Is.EqualTo(DiveDepthBands.ShallowMaxDepth));
            Assert.That(RecordingCameraRules.DarkDepthMetres, Is.EqualTo(DiveDepthBands.DeepMaxDepth));
        }

        // Measured from the formula before DarkDepthMetres was bound to DeepMaxDepth (both 35): the binding must not
        // move a single value. lerp(1, 0.2, (d - 8) / 27).
        [TestCase(0f, 1f)]
        [TestCase(8f, 1f)]
        [TestCase(14f, 0.8222222f)]
        [TestCase(20f, 0.6444444f)]
        [TestCase(27.5f, 0.4222222f)]
        [TestCase(35f, 0.2f)]
        [TestCase(40f, 0.2f)]
        [TestCase(float.NaN, 1f)]
        public void DepthFactorIsUnchanged(float depth, float expected)
        {
            Assert.That(RecordingCameraRules.DepthFactor(depth), Is.EqualTo(expected).Within(1e-5f));
        }

        // --- Through the water the world actually reads ----------------------------------------

        [TestCase(-4f, DepthBandIds.Reef, TestName = "12 m over water is reef")]
        [TestCase(-12f, DepthBandIds.Deep, TestName = "20 m over water is deep")]
        [TestCase(-26f, DepthBandIds.Deep, TestName = "34 m over water is deep (the basin floor)")]
        public void WaterDepthClassifiesReefAndDeep(float y, string expected)
        {
            Assert.That(WaterDepth.TryClassify(Sea, new Vector3(0f, y, 40f), out var id), Is.True);
            Assert.That(id, Is.EqualTo(expected));
        }

        [Test]
        public void TheOriginalArenaColumnIsShallowById()
        {
            // The P3 arena (sea bed y = 0, surface 8) stays shallow by id, not merely "some band".
            for (var y = 0f; y <= 8f; y += 0.5f)
            {
                Assert.That(WaterDepth.TryClassify(Sea, new Vector3(0f, y, 0f), out var id), Is.True, "y=" + y);
                Assert.That(id, Is.EqualTo(DepthBandIds.Shallow), "y=" + y);
            }
        }
    }
}
