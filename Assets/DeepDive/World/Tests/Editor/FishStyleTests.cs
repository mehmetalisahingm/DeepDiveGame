using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // #311: four swim styles that read differently, all still inside the water and host-side only.
    public sealed class FishStyleTests
    {
        private sealed class AnyWater : IWaterBounds
        {
            public bool Contains(Vector3 point) => true;
        }

        private static readonly IWaterBounds Water = new AnyWater();
        private static readonly List<Vector3> NoThreats = new List<Vector3>();

        private static FishMotion Motion(SwimStyle style, Vector3 home, int seed = 7, float wander = 6f) =>
            new FishMotion(new SwimTuning(1.2f, 3.2f, 5f, wander, style), home, new System.Random(seed));

        [Test]
        public void ASchoolDriftsTowardsItsMatesAndALoneWandererDoesNot()
        {
            var mates = new List<Vector3> { new Vector3(6f, 0f, 0f), new Vector3(6f, 0f, 1f) };
            var company = new FishCompany(mates, false, Vector3.zero);
            float MeanX(SwimStyle style)
            {
                var sum = 0f;
                for (var seed = 0; seed < 40; seed++)
                {
                    var motion = Motion(style, Vector3.zero, seed);
                    var at = Vector3.zero;
                    for (var i = 0; i < 200; i++) at = motion.Step(at, NoThreats, 0.05f, Water, 1f, company);
                    sum += at.x;
                }
                return sum / 40f;
            }
            Assert.Greater(MeanX(SwimStyle.School), 2f, "the school pulls it over");
            Assert.Less(Mathf.Abs(MeanX(SwimStyle.Wanderer)), 1.5f, "a wanderer stays round its home");
        }

        [Test]
        public void ASchoolBoltsWhenAMateBoltsButAWandererDoesNot()
        {
            var diver = new Vector3(-20f, 0f, 0f);
            var company = new FishCompany(new List<Vector3> { new Vector3(-2f, 0f, 0f) }, true, diver);
            var school = Motion(SwimStyle.School, Vector3.zero);
            var next = school.Step(Vector3.zero, NoThreats, 0.1f, Water, 1f, company);
            Assert.IsTrue(school.IsFleeing);
            Assert.Greater(next.x, 0.3f, "away from the mate's diver at flee speed");
            var wanderer = Motion(SwimStyle.Wanderer, Vector3.zero);
            wanderer.Step(Vector3.zero, NoThreats, 0.1f, Water, 1f, company);
            Assert.IsFalse(wanderer.IsFleeing);
        }

        [Test]
        public void ARunningSchoolmateDoesNotPassTheAlarmOn()
        {
            var diver = new Vector3(-20f, 0f, 0f);
            var school = Motion(SwimStyle.School, Vector3.zero);
            school.Step(Vector3.zero, NoThreats, 0.1f, Water, 1f, new FishCompany(new List<Vector3> { new Vector3(-2f, 0f, 0f) }, true, diver));
            Assert.IsTrue(school.IsFleeing, "runs with the school");
            Assert.IsFalse(school.SeesThreat, "but does not raise the alarm itself");
            var seer = Motion(SwimStyle.School, Vector3.zero);
            seer.Step(Vector3.zero, new List<Vector3> { new Vector3(2f, 0f, 0f) }, 0.1f, Water);
            Assert.IsTrue(seer.SeesThreat, "a fish that sees the diver does");
        }

        [Test]
        public void ALurkerRunsHomeAndHoldsThereWhileTheDiverIsNear()
        {
            var home = Vector3.zero;
            var lurker = Motion(SwimStyle.Lurker, home);
            var at = new Vector3(1.5f, 0f, 0f);
            var diver = new List<Vector3> { new Vector3(-3f, 0f, 0f) };
            for (var i = 0; i < 40; i++) at = lurker.Step(at, diver, 0.05f, Water);
            Assert.Less(Vector3.Distance(at, home), FishMotion.LurkerHideDistance + 0.01f, "back at its rock, towards the diver");
            Assert.IsTrue(lurker.Hiding);
            var held = lurker.Step(at, diver, 0.05f, Water);
            Assert.AreEqual(at, held, "holds while hiding");
            // A wanderer in the same spot runs the other way.
            var wanderer = Motion(SwimStyle.Wanderer, home);
            Assert.Greater(wanderer.Step(new Vector3(1.5f, 0f, 0f), diver, 0.05f, Water).x, 1.5f);
        }

        [Test]
        public void ACruiserHeadsForTheFarSideOfItsRangeAndHoldsItsHeadingLonger()
        {
            float MeanReach(SwimStyle style)
            {
                var reach = new List<float>();
                for (var seed = 0; seed < 30; seed++)
                {
                    var motion = Motion(style, Vector3.zero, seed);
                    var at = Vector3.zero;
                    var last = motion.WanderTarget;
                    for (var i = 0; i < 600; i++)
                    {
                        at = motion.Step(at, NoThreats, 0.05f, Water);
                        if (motion.WanderTarget != last) { last = motion.WanderTarget; reach.Add(new Vector2(last.x, last.z).magnitude); }
                    }
                }
                return reach.Average();
            }
            Assert.Greater(MeanReach(SwimStyle.Cruiser), MeanReach(SwimStyle.Wanderer) * 1.2f);
            Assert.Greater(FishMotion.RepathSecondsFor(SwimStyle.Cruiser), FishMotion.RepathSecondsFor(SwimStyle.Wanderer));
        }

        [Test]
        public void SchoolmatesKeepABodyLengthApart()
        {
            var school = Motion(SwimStyle.School, Vector3.zero);
            var mate = new Vector3(0.3f, 0f, 0f);
            var company = new FishCompany(new List<Vector3> { mate }, false, Vector3.zero);
            var at = Vector3.zero;
            for (var i = 0; i < 20; i++) at = school.Step(at, NoThreats, 0.05f, Water, 1f, company);
            Assert.Greater(Vector3.Distance(at, mate), 0.3f);
        }

        [Test]
        public void AtLeastThreeSpeciesSwimDifferently()
        {
            var styles = AssetDatabase.FindAssets("t:SpeciesDefinition", new[] { "Assets/DeepDive/World/Fish/Species" })
                .Select(g => AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .ToDictionary(s => s.SpeciesId, s => s.Swim.Style);
            Assert.AreEqual(SwimStyle.School, styles["sea_bass"]);
            Assert.AreEqual(SwimStyle.Cruiser, styles["tuna"]);
            Assert.AreEqual(SwimStyle.Lurker, styles["grouper"]);
            Assert.GreaterOrEqual(styles.Values.Distinct().Count(), 3);
        }
    }
}
