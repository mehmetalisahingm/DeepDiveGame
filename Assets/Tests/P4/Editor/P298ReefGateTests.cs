using System.Linq;
using DeepDive.Composition;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #298: the reef arch's south mouth is open to a diver arriving level, and the shore-to-arch trail is decor only.
    public sealed class P298ReefGateTests
    {
        private const float StandingDiver = 1.8f + 0.16f;

        private static Bounds ArchTop => SeabedProfile.Solids.Single(s => s.KitId == ReefHabitatVisuals.ArchId && s.Lift > 0f).Box;

        [Test]
        public void TheLastFourMetresBeforeTheSouthMouthLeaveRoomForAStandingDiver()
        {
            var top = ArchTop;
            for (var z = top.min.z - 4f; z <= top.min.z + 0.01f; z += 0.25f)
                Assert.Greater(top.min.y - SeabedProfile.FloorHeight(SeabedProfile.ArchAxisX, z), StandingDiver, "z=" + z);
        }

        [Test]
        public void TheGullyOnlyTouchesTheArchApproach()
        {
            Assert.AreEqual(0f, SeabedProfile.ArchApproachCut(SeabedProfile.ArchAxisX - 3f, 26f), "west of the gully");
            Assert.AreEqual(0f, SeabedProfile.ArchApproachCut(SeabedProfile.ArchAxisX + 3f, 26f), "east of the gully");
            Assert.AreEqual(0f, SeabedProfile.ArchApproachCut(SeabedProfile.ArchAxisX, 18f), "the shallow approach");
            Assert.AreEqual(1f, SeabedProfile.ArchApproachCut(SeabedProfile.ArchAxisX, 26f), "the mouth");
            // The cut only scales the first outcrop (x -8, z 24, radius 4.5); the fish homes over it keep their floor.
            foreach (var home in P411FiveSpeciesRoster.Homes)
                if (new Vector2(home.x + 8f, home.z - 24f).magnitude < 4.5f)
                    Assert.AreEqual(0f, SeabedProfile.ArchApproachCut(home.x, home.z), "home " + home);
        }

        [Test]
        public void TheArchTrailRunsFromTheShallowsToTheMouthWithinFogReach()
        {
            var trail = ReefHabitatVisuals.ArchTrail;
            Assert.Less(trail[0].Position.z, 5f, "starts by the wade");
            Assert.Greater(trail[trail.Length - 1].Position.z, ArchTop.min.z - 6f, "ends at the mouth");
            for (var i = 1; i < trail.Length; i++)
                Assert.Less(Vector3.Distance(trail[i - 1].Position, trail[i].Position), 6f, "gap before piece " + i);
            foreach (var spot in trail)
                Assert.IsTrue(ReefHabitatVisuals.Spots.Contains(spot), "placed with the reef " + spot.Position);
        }
    }
}
