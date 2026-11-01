using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #219: the natural sea bed's maths and the bounds a fish swims in. FishMotion is the real host rule; the threats
    // are the worst push a diver can give, a straight line at swim speed that ignores obstacles. These are EditMode
    // replays of the rule, not playtest proof. The mesh and the scene are the setup's tests.
    public sealed class P219SeabedTests
    {
        private const float Dt = 0.02f;
        private const float SwimSpeed = 3f;
        private const float SurfaceY = DiveDepthBandSet.DefaultSurfaceY;

        private static readonly (string Name, Vector3 Offset)[] StandingThreats =
        {
            ("north", Vector3.forward), ("south", Vector3.back), ("east", Vector3.right), ("west", Vector3.left),
            ("above", Vector3.up), ("below", Vector3.down)
        };

        private static readonly (string Name, Vector3 Offset)[] Chasers =
        {
            ("south", new Vector3(0f, 0f, -3f)), ("above", new Vector3(0f, 3f, 0f)), ("shore-above", new Vector3(0f, 2f, -2f))
        };

        private sealed class BoxWater : IWaterBounds
        {
            private readonly Bounds box;
            public BoxWater(Bounds box) => this.box = box;
            public bool Contains(Vector3 point) => box.Contains(point);
        }

        private static readonly IWaterBounds Water =
            new BoxWater(new Bounds(new Vector3(0f, -9.5f, 25f), new Vector3(30f, 35f, 80f)));

        // ---- the floor --------------------------------------------------------------------------------

        [Test]
        public void TheFloorKeepsTheBandsTheRegionAlreadyHas()
        {
            Assert.That(Band(SeabedProfile.FloorHeight(0f, 10f)), Is.EqualTo(DiveDepthBands.ShallowId), "the shallows are shallow");
            Assert.That(Band(SeabedProfile.FloorHeight(3f, 30f)), Is.EqualTo(DiveDepthBands.ReefId), "the reef anchorage bed is reef");
            Assert.That(Band(SeabedProfile.FloorHeight(0f, 56f)), Is.EqualTo(DiveDepthBands.DeepId), "the deep anchorage bed is deep");
        }

        [Test]
        public void TheFloorStaysInsideTheWaterAndUnderTheWalkableCoast()
        {
            for (var x = -15f; x <= 15f; x += 0.5f)
            for (var z = -15f; z <= 65f; z += 0.5f)
            {
                var y = SeabedProfile.FloorHeight(x, z);
                Assert.GreaterOrEqual(y, SurfaceY - DiveDepthBands.DeepMaxDepth + 1f, $"too deep at {x},{z}");
                if (z <= 15f) Assert.GreaterOrEqual(y, 0f, $"opens under the beach platform (y 0) at {x},{z}");
                // The apron swells 0.05 m round the toe tops; a walkable surface would need feet at the water line.
                Assert.LessOrEqual(y, SeabedProfile.ToeTopY + 0.06f, $"higher than the wade foot at {x},{z}");
            }
        }

        [Test]
        public void TheBerthKeepsItsDepth()
        {
            for (var z = -0.4f; z <= 8.5f; z += 0.5f)
                Assert.LessOrEqual(SeabedProfile.FloorHeight(9f, z), SeabedProfile.BerthApronY + 0.06f, "z=" + z);
        }

        // The old drop-offs ran 63 and 72 degrees across the whole 30 m. Only the outcrops may stay steep, and they
        // end within their radius; the x 0 and x 5 lines have none.
        [Test]
        public void TheOpenSlopesAreNoLongerWalls()
        {
            foreach (var x in new[] { 0f, 5f })
                for (var z = -4f; z < 65f; z += 0.5f)
                {
                    var rise = Mathf.Abs(SeabedProfile.FloorHeight(x, z + 0.5f) - SeabedProfile.FloorHeight(x, z));
                    var degrees = Mathf.Atan2(rise, 0.5f) * Mathf.Rad2Deg;
                    Assert.LessOrEqual(degrees, 50f, $"x={x} z={z}");
                }
        }

        // Boss arena and target, traces, and the keel of every route (all three end over this water).
        [Test]
        public void TheFloorLeavesTheBossVolumesAndTheRoutesOpen()
        {
            var failures = new List<string>();
            void Sphere(string id, Vector3 c, float r)
            {
                for (var dx = -r; dx <= r; dx += 0.5f)
                for (var dz = -r; dz <= r; dz += 0.5f)
                    if (SeabedProfile.FloorHeight(c.x + dx, c.z + dz) > c.y - r) failures.Add($"{id} at {dx},{dz}");
            }

            Sphere("arena", DeepEncounterWorld.ArenaPosition, DeepEncounterWorld.ArenaRadius);
            Sphere("boss", DeepEncounterWorld.BossPosition, 2.4f);
            for (var i = 0; i < DeepEncounterWorld.TracePositions.Length; i++)
                Sphere(DeepEncounterWorld.TraceIds[i], DeepEncounterWorld.TracePositions[i], DeepEncounterWorld.TraceRadius);

            var keel = SurfaceY - 0.5f - 5f;
            foreach (var (from, to) in new[]
                     {
                         (new Vector3(9f, 8f, 3.5f), new Vector3(9f, 8f, 8.5f)), (new Vector3(9f, 8f, 3.5f), new Vector3(6f, 8f, 14f)),
                         (new Vector3(6f, 8f, 14f), new Vector3(3f, 8f, 30f)), (new Vector3(6f, 8f, 14f), new Vector3(3f, 8f, 36f)),
                         (new Vector3(3f, 8f, 36f), new Vector3(0f, 8f, 56f))
                     })
                for (var t = 0f; t <= 1f; t += 0.05f)
                {
                    var p = Vector3.Lerp(from, to, t);
                    if (SeabedProfile.FloorHeight(p.x, p.z) > keel) failures.Add($"keel at {p}");
                }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // The -OceanVideo development route, swum as written with the capsule's skin.
        [Test]
        public void TheOceanVideoRouteStaysAboveTheFloor()
        {
            for (var slot = 0; slot < 4; slot++)
            {
                var x = -11f + slot * 1.3f;
                var stops = new[] { new Vector3(-6.5f + slot, 6.3f, -5f), new Vector3(x, 2f, 9f), new Vector3(x, -4f, 30f), new Vector3(x, -16f, 53f) };
                for (var leg = 0; leg + 1 < stops.Length; leg++)
                    for (var t = 0f; t <= 1f; t += 0.02f)
                    {
                        var p = Vector3.Lerp(stops[leg], stops[leg + 1], t);
                        Assert.Greater(p.y, SeabedProfile.FloorHeight(p.x, p.z) + 0.08f, $"slot {slot} leg {leg} at {p}");
                        foreach (var solid in SeabedProfile.Solids)
                        {
                            // The upright capsule's column, feet to head.
                            var box = solid.Box;
                            box.Expand(new Vector3(0.7f, 1.8f, 0.7f));
                            Assert.IsFalse(box.Contains(p + Vector3.up * 0.9f), $"slot {slot} leg {leg} at {p} hits {solid.KitId} at {solid.X},{solid.Z}");
                        }
                    }
            }
        }

        [Test]
        public void SolidsSitOnTheBedAndClearTheBossAndTheAnchorages()
        {
            foreach (var solid in SeabedProfile.Solids)
            {
                var box = solid.Box;
                Assert.Less(box.min.y, SeabedProfile.FloorHeight(solid.X, solid.Z) + solid.Lift, solid.KitId + " floats");
                foreach (var keep in DeepEncounterWorld.TracePositions.Append(DeepEncounterWorld.ArenaPosition).Append(DeepEncounterWorld.BossPosition))
                    Assert.Greater(box.SqrDistance(keep), 2.4f * 2.4f, solid.KitId + " by " + keep);
                Assert.Less(box.max.y, SurfaceY - 1f, solid.KitId + " reaches a hull");
            }
        }

        // The diver fits under the arch's top span: 1.8 m capsule + skin both ways, feet on the bed.
        [Test]
        public void TheArchOpeningFitsAnUprightDiver()
        {
            var arch = SeabedProfile.Solids.Where(s => s.KitId == "reef-rock-arch").ToArray();
            Assert.AreEqual(3, arch.Length);
            var top = arch.Single(s => s.Lift > 0f).Box;
            var feet = arch.Where(s => s.Lift == 0f).Select(s => s.Box).OrderBy(b => b.center.x).ToArray();
            Assert.Greater(feet[1].min.x - feet[0].max.x, 0.7f + 0.16f, "width");
            Assert.Greater(top.min.y - SeabedProfile.FloorHeight(top.center.x, top.center.z), 1.8f + 0.16f, "height");
        }

        // ---- the bounds -------------------------------------------------------------------------------

        [Test]
        public void HabitatRefusesEverythingOutsideItsHabitat()
        {
            var home = new Vector3(5f, -4f, 30f);
            var habitat = new HabitatWaterBounds(Water, home);
            Assert.IsTrue(habitat.Contains(home), "home");
            Assert.IsFalse(habitat.Contains(new Vector3(5f, SeabedProfile.FloorHeight(5f, 30f) + 0.3f, 30f)), "too close to the floor");
            Assert.IsFalse(habitat.Contains(home + Vector3.up * (HabitatWaterBounds.LeashVertical + 0.1f)), "vertical leash");
            Assert.IsFalse(habitat.Contains(home + Vector3.forward * (HabitatWaterBounds.LeashFlat + 0.1f)), "flat leash");

            var wallHome = new Vector3(14f, -4f, 30f);
            Assert.IsFalse(new HabitatWaterBounds(Water, wallHome).Contains(new Vector3(14.5f, -4f, 30f)), "the east wall");
            Assert.IsFalse(new HabitatWaterBounds(Water, new Vector3(0f, -9f, 60f)).Contains(new Vector3(0f, -9f, 64.5f)), "the north wall");

            var solid = SeabedProfile.Solids[0].Box;
            var above = solid.center + Vector3.up * (solid.extents.y + 0.1f);
            Assert.IsFalse(new HabitatWaterBounds(Water, above + Vector3.up * 2f).Contains(above), "a solid kit piece");
            Assert.IsFalse(new HabitatWaterBounds(new BoxWater(new Bounds(home, Vector3.one)), home).Contains(home + Vector3.right * 2f), "the water itself");
        }

        // Every scene fish, every push, from wherever it is authored: never under the floor, in a solid or past the
        // leash, and it keeps moving. Read from the scene so the fish rows cannot move without this test seeing it.
        [Test]
        public void NoPushTakesAFishThroughTheFloorOrOutOfItsHabitat()
        {
            WithFish(fish =>
            {
                var failures = new List<string>();
                foreach (var (name, home, tuning) in fish)
                    foreach (var run in Pushes(home, tuning, new HabitatWaterBounds(Water, home), name))
                    {
                        if (run.BelowFloor > 0) failures.Add($"{run.Label}: {run.BelowFloor} frames under the floor");
                        if (run.InSolid > 0) failures.Add($"{run.Label}: {run.InSolid} frames in a solid");
                        if (run.Stalled > run.Frames / 10) failures.Add($"{run.Label}: stalled {run.Stalled}/{run.Frames}");
                        if (run.Furthest > HabitatWaterBounds.LeashFlat + HabitatWaterBounds.LeashVertical * 0.5f)
                            failures.Add($"{run.Label}: {run.Furthest:0.0} m from home");
                    }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            });
        }

        // Red without the bounds: why FishActor does not use the bare water box any more.
        [Test]
        public void TheBareWaterBoxLetsAChasedFishThroughTheFloor()
        {
            WithFish(fish =>
            {
                var runs = fish.SelectMany(f => Pushes(f.Home, f.Tuning, Water, f.Name)).ToArray();
                Assert.Greater(runs.Sum(r => r.BelowFloor), 0, "the water box used to be the only bound");
                Assert.Greater(runs.Max(r => r.Furthest), 30f, "a chased fish used to leave its habitat");
            });
        }

        // ---- helpers ----------------------------------------------------------------------------------

        private sealed class Run
        {
            public string Label;
            public int Frames, BelowFloor, InSolid, Stalled;
            public float Furthest;
        }

        private static IEnumerable<Run> Pushes(Vector3 home, SwimTuning tuning, IWaterBounds bounds, string name)
        {
            var seed = name.Aggregate(17, (h, c) => h * 31 + c);
            foreach (var (label, offset) in StandingThreats)
            {
                var threat = home + offset * 2f;
                yield return Swim($"{name} standing {label}", home, tuning, bounds, seed, 20f, _ => threat);
            }

            foreach (var (label, offset) in Chasers)
            {
                var chaser = home + offset;
                yield return Swim($"{name} chased {label}", home, tuning, bounds, seed, 40f, fish =>
                {
                    chaser = Vector3.MoveTowards(chaser, fish, SwimSpeed * Dt);
                    return chaser;
                });
            }
        }

        private static Run Swim(string label, Vector3 home, SwimTuning tuning, IWaterBounds bounds, int seed, float seconds, Func<Vector3, Vector3> threatAt)
        {
            var motion = new FishMotion(tuning, home, new System.Random(seed));
            var run = new Run { Label = label };
            var threats = new List<Vector3> { Vector3.zero };
            var at = home;
            var solids = SeabedProfile.Solids.Select(s => s.Box).ToArray();
            for (var frame = Mathf.RoundToInt(seconds / Dt); frame > 0; frame--)
            {
                threats[0] = threatAt(at);
                var next = motion.Step(at, threats, Dt, bounds);
                if (next == at) run.Stalled++;
                at = next;
                run.Frames++;
                run.Furthest = Mathf.Max(run.Furthest, Vector3.Distance(at, home));
                if (at.y < SeabedProfile.FloorHeight(at.x, at.z)) run.BelowFloor++;
                if (solids.Any(b => b.Contains(at))) run.InSolid++;
            }
            return run;
        }

        private static void WithFish(Action<(string Name, Vector3 Home, SwimTuning Tuning)[]> body)
        {
            ContinuousTownScene.With(scene =>
            {
                var fish = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FishActor>(true))
                    .OrderBy(f => f.name, StringComparer.Ordinal)
                    .Select(f => (f.name, f.transform.position, f.Species.Swim)).ToArray();
                Assert.AreEqual(15, fish.Length);
                body(fish);
            }, false);
        }

        private static string Band(float y)
        {
            Assert.IsTrue(DiveDepthBands.TryFind(SurfaceY - y, out var band), "y=" + y);
            return band.Id;
        }
    }
}
