using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DeepDive.World.Tests
{
    public class WaterTrackerTests
    {
        private const float SeaSurface = 8f;

        // The same sea the raw rules are tested against: footprint +-15, surface at y = 8.
        private static readonly WaterBody[] Sea = { Box(new Vector3(0, 4, 0), new Vector3(30, 8, 30)) };

        private static WaterBody Box(Vector3 center, Vector3 size)
        {
            WaterBody.TryFromBox(center, size, out var body);
            return body;
        }

        // A standing diver measured from the feet: base sits at feet.y, top at feet.y + 2.
        private static WaterProbe Diver(float x, float feetY) =>
            new WaterProbe(new Vector3(x, feetY, 0), new Vector3(0, 1, 0), 0.5f, 2f, Vector3.up);

        private static WaterTracker Tracker(float deadband = WaterTracker.DefaultDeadband) =>
            new WaterTracker(Sea, deadband);

        // Trackers already carrying a state, so the deadband has something to hold on to.
        private static WaterTracker OnLand()
        {
            var tracker = Tracker();
            Assert.AreEqual(EnvironmentLocomotion.Land, tracker.Classify(Diver(0, 40f)));
            return tracker;
        }

        private static WaterTracker AtSurface()
        {
            var tracker = Tracker();
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(0, 7f)));
            return tracker;
        }

        private static WaterTracker UnderWater()
        {
            var tracker = Tracker();
            Assert.AreEqual(EnvironmentLocomotion.Underwater, tracker.Classify(Diver(0, 2f)));
            return tracker;
        }

        [Test]
        public void FirstCallHasNoMemory()
        {
            // 0.05 under the surface is inside the deadband, so a remembered Land would keep
            // this dry. With nothing remembered the raw line decides and it is wet.
            var tracker = Tracker();
            Assert.IsFalse(tracker.HasMemory);
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(0, SeaSurface - 0.05f)));
            Assert.IsTrue(tracker.HasMemory);

            var remembering = OnLand();
            Assert.AreEqual(EnvironmentLocomotion.Land, remembering.Classify(Diver(0, SeaSurface - 0.05f)));
        }

        [Test]
        public void LandNeedsTheBaseDeadbandUnderTheSurface()
        {
            var tracker = OnLand();
            Assert.AreEqual(EnvironmentLocomotion.Land, tracker.Classify(Diver(0, SeaSurface - 0.09f)));
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(0, SeaSurface - 0.11f)));
        }

        [Test]
        public void SurfaceNeedsTheBaseDeadbandAboveToBecomeLand()
        {
            var tracker = AtSurface();
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(0, SeaSurface + 0.09f)));
            Assert.AreEqual(EnvironmentLocomotion.Land, tracker.Classify(Diver(0, SeaSurface + 0.11f)));
        }

        [Test]
        public void SurfaceAndUnderwaterHoldAcrossTheHeadLine()
        {
            // Going under is a new claim: the head has to clear the line by a full band.
            var going = AtSurface();
            Assert.AreEqual(EnvironmentLocomotion.Surface, going.Classify(Diver(0, SeaSurface - 2f - 0.09f)));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, going.Classify(Diver(0, SeaSurface - 2f - 0.11f)));

            // Coming back up is the mirror: underwater holds until the head is a band clear.
            var coming = UnderWater();
            Assert.AreEqual(EnvironmentLocomotion.Underwater, coming.Classify(Diver(0, SeaSurface - 2f + 0.09f)));
            Assert.AreEqual(EnvironmentLocomotion.Surface, coming.Classify(Diver(0, SeaSurface - 2f + 0.11f)));
        }

        [Test]
        public void TheShoreEdgeHasTheSameDeadband()
        {
            // Wading in: the feet have to be a band inside the shore line.
            var entering = OnLand();
            Assert.AreEqual(EnvironmentLocomotion.Land, entering.Classify(Diver(15f - 0.09f, 2f)));
            Assert.AreEqual(EnvironmentLocomotion.Underwater, entering.Classify(Diver(15f - 0.11f, 2f)));

            // Wading out: the water holds until the feet are a band past the line.
            var leaving = UnderWater();
            Assert.AreEqual(EnvironmentLocomotion.Underwater, leaving.Classify(Diver(15f + 0.09f, 2f)));
            Assert.AreEqual(EnvironmentLocomotion.Land, leaving.Classify(Diver(15f + 0.11f, 2f)));
        }

        // Deterministic wobble inside the band, four offsets so it is not a single alternation.
        private static float Jitter(int tick)
        {
            switch (tick & 3)
            {
                case 0: return 0.09f;
                case 1: return -0.09f;
                case 2: return 0.045f;
                default: return -0.045f;
            }
        }

        [TestCase("base-line")]
        [TestCase("head-line")]
        [TestCase("shore-edge")]
        public void JitterAtEveryLineNeverFlips(string line)
        {
            var tracker = Tracker();

            // Start exactly on the line, then wobble inside the band for 500 ticks. Whatever
            // the first tick decided has to survive all of them - this is the flip-flop lock.
            var first = tracker.Classify(Probe(line, 0f));
            for (var tick = 0; tick < 500; tick++)
            {
                var result = tracker.Classify(Probe(line, Jitter(tick)));
                Assert.AreEqual(first, result, "flipped on tick " + tick);
            }
        }

        private static WaterProbe Probe(string line, float offset) => line switch
        {
            // Base exactly at the surface.
            "base-line" => Diver(0, SeaSurface + offset),
            // Top exactly at the surface.
            "head-line" => Diver(0, SeaSurface - 2f + offset),
            // Feet exactly on the shore line.
            _ => Diver(15f + offset, 2f)
        };

        [Test]
        public void ATeleportFarPastTheLineIgnoresMemory()
        {
            // The band only holds near the line. A jump well past it wins immediately, with
            // no Reset needed.
            var down = OnLand();
            Assert.AreEqual(EnvironmentLocomotion.Underwater, down.Classify(Diver(0, 0f)));

            var up = UnderWater();
            Assert.AreEqual(EnvironmentLocomotion.Land, up.Classify(Diver(0, 50f)));
        }

        [Test]
        public void ResetClearsTheMemory()
        {
            var tracker = OnLand();
            Assert.IsTrue(tracker.HasMemory);
            Assert.AreEqual(EnvironmentLocomotion.Land, tracker.Classify(Diver(0, SeaSurface - 0.05f)));

            tracker.Reset();
            Assert.IsFalse(tracker.HasMemory);

            // Same place, but judged on raw geometry again now that the memory is gone.
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(Diver(0, SeaSurface - 0.05f)));
        }

        [Test]
        public void NegativeDeadbandIsClampedToZero()
        {
            var tracker = Tracker(-1f);
            Assert.AreEqual(0f, tracker.Deadband);

            // With no band the tracker must answer exactly what the raw rule answers, memory
            // or not. 0.05 under the surface would be held dry by the default band.
            Assert.AreEqual(EnvironmentLocomotion.Land, tracker.Classify(Diver(0, 40f)));
            var probe = Diver(0, SeaSurface - 0.05f);
            Assert.AreEqual(WaterRules.ClassifyRaw(Sea, probe), tracker.Classify(probe));
            Assert.AreEqual(EnvironmentLocomotion.Surface, tracker.Classify(probe));
        }

        [Test]
        public void ClassifyDoesNotAllocate()
        {
            var tracker = Tracker();
            var probe = Diver(0, 2f);
            tracker.Classify(probe);

            // Block body, not an expression body: the constraint needs a TestDelegate and
            // an expression lambda that returns a value binds to Func<T> instead.
            Assert.That(() => { tracker.Classify(probe); }, Is.Not.AllocatingGCMemory());
        }

        // A 1.8 m diver: base at the feet, head 1.8 m above them.
        private static WaterProbe Human(float footDepth) =>
            new WaterProbe(
                new Vector3(0, SeaSurface - footDepth, 0), new Vector3(0, 0.9f, 0), 0.35f, 1.8f, Vector3.up);

        // The depths stay a centimetre clear of the two nominal lines (feet at the surface,
        // head at the surface). Sitting exactly on either one is not a meaningful test at
        // this scale: 0.9, 0.35 and 1.8 are not representable in binary, so the offset
        // round-trip loses about 5e-7 m and a nominal zero is measured as +4.76837158E-07.
        // Instrumented against this very sea, a foot depth of 0.00 reads dBase = +4.8e-7 and
        // classifies Surface, and 1.80 reads dTop = +4.8e-7 and classifies Underwater - each
        // decided by half an ULP rather than by the rule. The rule itself is right; the
        // boundary is simply below float resolution here, so the lock is placed where the
        // answer is unambiguous. 1.79 measures dTop = -0.0100002289 and 1.81 measures
        // dTop = +0.009999752, both four orders of magnitude clear of the noise.
        [TestCase(1.00f, EnvironmentLocomotion.Surface)]
        [TestCase(1.79f, EnvironmentLocomotion.Surface)]
        [TestCase(1.81f, EnvironmentLocomotion.Underwater)]
        [TestCase(2.50f, EnvironmentLocomotion.Underwater)]
        [TestCase(-0.01f, EnvironmentLocomotion.Land)]
        [TestCase(-0.20f, EnvironmentLocomotion.Land)]
        public void HumanScaleFootDepthsClassifyAsExpected(float footDepth, EnvironmentLocomotion expected)
        {
            // Fresh tracker each time: these are the raw lines at human scale, not the band.
            Assert.AreEqual(expected, Tracker().Classify(Human(footDepth)));
        }
    }
}
