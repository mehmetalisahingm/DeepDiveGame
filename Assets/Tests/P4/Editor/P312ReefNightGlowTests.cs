using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #312: the night glows run from the wade to the arch mouth, each within sight of the next, above the sand, and
    // only at night.
    public sealed class P312ReefNightGlowTests
    {
        [Test]
        public void TheGlowsLeadFromTheShallowsToTheArchMouthOneStepAtATime()
        {
            var points = ReefNightGlow.Points();
            Assert.AreEqual(ReefHabitatVisuals.ArchTrail.Length + 2, points.Length);
            Assert.Less(points[0].z, 5f, "starts by the wade");
            for (var i = 1; i < ReefHabitatVisuals.ArchTrail.Length; i++)
                Assert.Less(Vector3.Distance(points[i - 1], points[i]), 6f, "gap before glow " + i);
            var last = points[ReefHabitatVisuals.ArchTrail.Length - 1];
            for (var i = ReefHabitatVisuals.ArchTrail.Length; i < points.Length; i++)
            {
                Assert.Less(Vector3.Distance(last, points[i]), 7f, "mouth glow " + i + " within reach of the trail's end");
                Assert.Greater(points[i].z, 27f, "at the arch");
            }
            foreach (var p in points)
                Assert.Greater(p.y, SeabedProfile.FloorHeight(p.x, p.z), "above the sand at " + p);
        }

        [Test]
        public void TheGlowIsOffByDayAndOnAtNight()
        {
            Assert.AreEqual(0f, ReefNightGlow.IntensityFor(0f));
            Assert.AreEqual(0f, ReefNightGlow.IntensityFor(0.3f), "not in the late afternoon");
            Assert.AreEqual(ReefNightGlow.NightIntensity, ReefNightGlow.IntensityFor(1f));
            Assert.Greater(ReefNightGlow.IntensityFor(0.6f), 0f, "fading in at dusk");
        }
    }
}
