using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // #138: the pure zone rule against SYNTHETIC boxes. They are deliberately written numbers (a frozen copy of
    // the #143 volumes and SwimVolume's footprint) so the rule is tested on its own; they are NOT the live world
    // and do not follow ContinuousTownLayout. The real scene volumes are pinned by the P4
    // ContinuousTownWorldContractTests, which read them from the layout (#153).
    public class PlayerZoneRulesTests
    {
        private static readonly WaterBody[] SyntheticSwimVolume = { new WaterBody(-15f, 15f, -15f, 65f, 8f) };

        private static ZoneVolumeEntry Box(PlayerZone zone, Vector3 min, Vector3 max)
        {
            Assert.IsTrue(ZoneBox.TryCreate(min, max, out var box), zone + " box refused");
            return new ZoneVolumeEntry(zone, box);
        }

        private static readonly ZoneVolumeEntry SyntheticHome =
            Box(PlayerZone.Home, new Vector3(-5.825f, 7.9f, -53.825f), new Vector3(5.825f, 11.4f, -42f));
        private static readonly ZoneVolumeEntry SyntheticDock =
            Box(PlayerZone.Dock, new Vector3(7f, 7.9f, -11f), new Vector3(11f, 13f, -3.5f));
        private static readonly ZoneVolumeEntry SyntheticTown =
            Box(PlayerZone.Town, new Vector3(-14.75f, 7.9f, -55.8f), new Vector3(14.75f, 13f, -14.75f));
        private static readonly ZoneVolumeEntry SyntheticCoast =
            Box(PlayerZone.Coast, new Vector3(-15f, 4.5f, -14.75f), new Vector3(15f, 13f, -3.5f));

        private static readonly ZoneVolumeEntry[] SyntheticWorld = { SyntheticCoast, SyntheticTown, SyntheticDock, SyntheticHome };

        private static PlayerZone Resolve(Vector3 feet) => PlayerZoneRules.Resolve(feet, SyntheticSwimVolume, SyntheticWorld);

        [TestCase(PlayerZone.Home, 0f, 8.4f, -48f)]
        [TestCase(PlayerZone.Town, 0f, 8.4f, -29f)]
        [TestCase(PlayerZone.Coast, 0f, 8.4f, -12.5f)]
        [TestCase(PlayerZone.Dock, 9f, 8.4f, -9f)]
        public void EachZoneResolvesInsideItsOwnBox(PlayerZone expected, float x, float y, float z)
        {
            Assert.AreEqual(expected, Resolve(new Vector3(x, y, z)));
        }

        [Test]
        public void APointOutsideEveryBoxIsUnknown()
        {
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(0f, 8.4f, -70f)));
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(30f, 8.4f, -29f)));
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(0f, 20f, -29f)));
            Assert.AreEqual(PlayerZone.Unknown, PlayerZoneRules.Resolve(new Vector3(0f, 8.4f, -29f), SyntheticSwimVolume, null));
        }

        [Test]
        public void BoxFacesAreInclusive()
        {
            var only = new[] { SyntheticTown };
            Assert.AreEqual(PlayerZone.Town, PlayerZoneRules.Resolve(new Vector3(-14.75f, 7.9f, -55.8f), null, only));
            Assert.AreEqual(PlayerZone.Town, PlayerZoneRules.Resolve(new Vector3(14.75f, 13f, -14.75f), null, only));
            Assert.AreEqual(PlayerZone.Unknown, PlayerZoneRules.Resolve(new Vector3(14.76f, 8.4f, -20f), null, only));
            Assert.AreEqual(PlayerZone.Unknown, PlayerZoneRules.Resolve(new Vector3(0f, 7.89f, -20f), null, only));
        }

        [Test]
        public void HomeWinsInsideTown()
        {
            var feet = new Vector3(0f, 8.4f, -48f);
            Assert.IsTrue(SyntheticTown.Box.Contains(feet));
            Assert.AreEqual(PlayerZone.Home, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticTown, SyntheticHome }));
            Assert.AreEqual(PlayerZone.Home, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticHome, SyntheticTown }));
            // The door line itself belongs to Home; half a metre outside it is the town.
            Assert.AreEqual(PlayerZone.Home, Resolve(new Vector3(0f, 8.4f, -42f)));
            Assert.AreEqual(PlayerZone.Town, Resolve(new Vector3(0f, 8.4f, -41.5f)));
        }

        [Test]
        public void DockWinsInsideCoast()
        {
            var feet = new Vector3(9f, 8.4f, -5f);
            Assert.IsTrue(SyntheticCoast.Box.Contains(feet));
            Assert.AreEqual(PlayerZone.Dock, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticCoast, SyntheticDock }));
            Assert.AreEqual(PlayerZone.Dock, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticDock, SyntheticCoast }));
        }

        [Test]
        public void TownWinsOnTheSharedFaceWithCoast()
        {
            var feet = new Vector3(0f, 8.4f, -14.75f);
            Assert.IsTrue(SyntheticTown.Box.Contains(feet) && SyntheticCoast.Box.Contains(feet));
            Assert.AreEqual(PlayerZone.Town, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticCoast, SyntheticTown }));
            Assert.AreEqual(PlayerZone.Town, PlayerZoneRules.Resolve(feet, null, new[] { SyntheticTown, SyntheticCoast }));
            Assert.AreEqual(PlayerZone.Coast, Resolve(new Vector3(0f, 8.4f, -14.7f)));
        }

        // Feet inside the SyntheticCoast and SyntheticDock boxes but in the water: the water answers first.
        [TestCase(-6.5f, 5.0f, -5f, EnvironmentLocomotion.Underwater)]
        [TestCase(9f, 7.5f, -5f, EnvironmentLocomotion.Surface)]
        public void WaterIsSeaBeforeAnyBox(float x, float y, float z, EnvironmentLocomotion expectedWater)
        {
            var feet = new Vector3(x, y, z);
            Assert.AreEqual(expectedWater, WaterRules.ClassifyRaw(SyntheticSwimVolume, ZoneProbeCapsule.Diver(feet)));
            Assert.IsTrue(SyntheticWorld.Any(v => v.Box.Contains(feet)), "the point must sit inside a box");
            Assert.AreEqual(PlayerZone.Sea, Resolve(feet));
        }

        [Test]
        public void SeaIsExactlyWaterRulesClassifyRawNotLand()
        {
            var checkedPoints = 0;
            for (var x = -16f; x <= 16f; x += 2f)
            for (var z = -20f; z <= 6f; z += 1.5f)
            for (var y = 5.5f; y <= 9.5f; y += 0.25f)
            {
                var feet = new Vector3(x, y, z);
                var wet = WaterRules.ClassifyRaw(SyntheticSwimVolume, ZoneProbeCapsule.Diver(feet)) != EnvironmentLocomotion.Land;
                Assert.AreEqual(wet, Resolve(feet) == PlayerZone.Sea, "at " + feet);
                checkedPoints++;
            }
            Assert.Greater(checkedPoints, 1000);
            // Feet just above the surface are dry: the shore ledge case.
            Assert.AreEqual(PlayerZone.Coast, Resolve(new Vector3(0f, 8.05f, -10f)));
        }

        [Test]
        public void WithoutWaterBodiesNothingIsSea()
        {
            var underwater = new Vector3(-6.5f, 5.0f, -5f);
            Assert.AreEqual(PlayerZone.Coast, PlayerZoneRules.Resolve(underwater, null, SyntheticWorld));
            Assert.AreEqual(PlayerZone.Coast, PlayerZoneRules.Resolve(underwater, new WaterBody[0], SyntheticWorld));
            Assert.AreEqual(PlayerZone.Unknown, PlayerZoneRules.Resolve(new Vector3(0f, -10f, 20f), null, SyntheticWorld));
        }

        [Test]
        public void FourPlayersInFourZonesResolveIndependentlyInAnyOrder()
        {
            var players = new[]
            {
                (feet: new Vector3(1.5f, 8.4f, -49.5f), zone: PlayerZone.Home),
                (feet: new Vector3(0f, 8.4f, -29f), zone: PlayerZone.Town),
                (feet: new Vector3(0f, 8.4f, -12.5f), zone: PlayerZone.Coast),
                (feet: new Vector3(0f, 2f, 20f), zone: PlayerZone.Sea)
            };
            foreach (var order in Permutations(Enumerable.Range(0, players.Length).ToArray()))
            {
                var results = new PlayerZone[players.Length];
                foreach (var i in order) results[i] = Resolve(players[i].feet);
                for (var i = 0; i < players.Length; i++)
                    Assert.AreEqual(players[i].zone, results[i], "player " + i + " order " + string.Join(",", order));
            }
        }

        [Test]
        public void NonFinitePositionIsUnknown()
        {
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(float.NaN, 8.4f, -29f)));
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(0f, float.PositiveInfinity, -29f)));
            Assert.AreEqual(PlayerZone.Unknown, Resolve(new Vector3(0f, 8.4f, float.NegativeInfinity)));
        }

        [Test]
        public void InvalidBoxesAreRefused()
        {
            Assert.IsFalse(ZoneBox.TryCreate(new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 1f), out _), "inverted x");
            Assert.IsFalse(ZoneBox.TryCreate(Vector3.zero, new Vector3(1f, 0f, 1f), out _), "zero height");
            Assert.IsFalse(ZoneBox.TryCreate(new Vector3(float.NaN, 0f, 0f), Vector3.one, out _), "NaN");
            Assert.IsFalse(ZoneBox.TryFromCenterSize(Vector3.zero, new Vector3(1f, -1f, 1f), out _), "negative size");

            var go = new GameObject("ZoneVolume_Invalid");
            try
            {
                var volume = go.AddComponent<WorldZoneVolume>();
                volume.Configure(PlayerZone.Sea, Vector3.one);
                Assert.IsFalse(volume.TryGetEntry(out _), "Sea is never a box zone");
                volume.Configure(PlayerZone.Unknown, Vector3.one);
                Assert.IsFalse(volume.TryGetEntry(out _), "Unknown is never a box zone");
                volume.Configure(PlayerZone.Town, new Vector3(1f, 0f, 1f));
                Assert.IsFalse(volume.TryGetEntry(out _), "degenerate box");
                volume.Configure(PlayerZone.Town, Vector3.one);
                Assert.IsTrue(volume.TryGetEntry(out var entry));
                Assert.AreEqual(PlayerZone.Town, entry.Zone);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static IEnumerable<int[]> Permutations(int[] items)
        {
            if (items.Length <= 1)
            {
                yield return items;
                yield break;
            }
            for (var i = 0; i < items.Length; i++)
            {
                var rest = items.Where((_, j) => j != i).ToArray();
                foreach (var tail in Permutations(rest))
                    yield return new[] { items[i] }.Concat(tail).ToArray();
            }
        }
    }
}
