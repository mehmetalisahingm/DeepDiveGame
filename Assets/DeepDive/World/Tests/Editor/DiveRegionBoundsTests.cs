using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // Guards the pure world<->map transform: the arithmetic, the edges, and the part that is
    // easiest to break by accident - the refusal to clamp. No scene, no component, no Unity
    // object: plain numbers in and plain numbers out, the way WaterRulesTests is.
    public class DiveRegionBoundsTests
    {
        // DiveTestArea's arena, the region P3.3 actually ships. SwimVolume's footprint is exactly
        // this (centre 0,4,0 size 30,8,30), the walls' inner faces sit at +-14.75 and
        // Beach_Platform ends at +-14.75, so every reachable world position is inside it and a
        // refusal always means a real mistake rather than a tight boundary.
        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);

        // A second, deliberately asymmetric region: 16 wide, 6 deep, centred nowhere near the
        // origin. The arena is symmetric about the origin on both axes, so on its own it would
        // still pass if the transform swapped x and z or dropped an offset. This one would not.
        private static readonly DiveRegionBounds Skewed = new DiveRegionBounds(-4f, 12f, 3f, 9f);

        private const float Tolerance = 1e-4f;

        [Test]
        public void CornersAndCentreMapToTheUnitSquare()
        {
            foreach (var region in new[] { Arena, Skewed })
            {
                AssertMaps(region, region.MinX, region.MinZ, 0f, 0f);
                AssertMaps(region, region.MaxX, region.MinZ, 1f, 0f);
                AssertMaps(region, region.MinX, region.MaxZ, 0f, 1f);
                AssertMaps(region, region.MaxX, region.MaxZ, 1f, 1f);
                AssertMaps(region,
                    (region.MinX + region.MaxX) * 0.5f,
                    (region.MinZ + region.MaxZ) * 0.5f,
                    0.5f, 0.5f);
            }
        }

        [Test]
        public void WorldToMapAndBackReturnsTheSamePointAcrossTheRegion()
        {
            foreach (var region in new[] { Arena, Skewed })
            {
                for (var i = 0; i <= 8; i++)
                {
                    for (var k = 0; k <= 8; k++)
                    {
                        var x = Mathf.Lerp(region.MinX, region.MaxX, i / 8f);
                        var z = Mathf.Lerp(region.MinZ, region.MaxZ, k / 8f);

                        Assert.IsTrue(region.TryWorldToMap(new Vector3(x, 8f, z), out var map),
                            "a point inside the region must convert: " + x + ", " + z);
                        Assert.IsTrue(region.TryMapToWorld(map, out var back),
                            "the map point it just produced must convert back: " + map);

                        Assert.AreEqual(x, back.x, Tolerance, "world x drifted at " + map);
                        Assert.AreEqual(z, back.y, Tolerance, "world z drifted at " + map);
                    }
                }
            }
        }

        [Test]
        public void MapToWorldAndBackReturnsTheSameNormalizedPoint()
        {
            foreach (var region in new[] { Arena, Skewed })
            {
                for (var i = 0; i <= 8; i++)
                {
                    for (var k = 0; k <= 8; k++)
                    {
                        var map = new Vector2(i / 8f, k / 8f);

                        Assert.IsTrue(region.TryMapToWorld(map, out var world),
                            "a map point inside the unit square must convert: " + map);
                        Assert.IsTrue(region.TryWorldToMap(new Vector3(world.x, -3f, world.y), out var back),
                            "the world point it just produced must convert back: " + world);

                        Assert.AreEqual(map.x, back.x, Tolerance, "mapX drifted at " + map);
                        Assert.AreEqual(map.y, back.y, Tolerance, "mapZ drifted at " + map);
                    }
                }
            }
        }

        [Test]
        public void HeightIsIgnoredSoTwoDepthsAtOneSpotShareOneMapPoint()
        {
            // The boat on the surface and a diver on the sea bed under it are the same place on
            // the map. If y ever reached the arithmetic, these three would drift apart.
            Assert.IsTrue(Arena.TryWorldToMap(new Vector3(9f, 8f, 8.5f), out var surface));
            Assert.IsTrue(Arena.TryWorldToMap(new Vector3(9f, 0.1f, 8.5f), out var seabed));
            Assert.IsTrue(Arena.TryWorldToMap(new Vector3(9f, -250f, 8.5f), out var absurd));

            Assert.AreEqual(surface, seabed, "depth changed the map point");
            Assert.AreEqual(surface, absurd, "an out-of-world height changed the map point");
        }

        [Test]
        public void WorldXDrivesMapXAndWorldZDrivesMapZ()
        {
            // Skewed is 16 wide and 6 deep, so one metre of world x and one metre of world z
            // produce different map steps. A transform that swapped the axes would be caught by
            // the step sizes even where the signs happened to agree.
            Assert.IsTrue(Skewed.TryWorldToMap(new Vector3(4f, 0f, 6f), out var centre));
            Assert.IsTrue(Skewed.TryWorldToMap(new Vector3(5f, 0f, 6f), out var east));
            Assert.IsTrue(Skewed.TryWorldToMap(new Vector3(4f, 0f, 7f), out var north));

            Assert.AreEqual(1f / 16f, east.x - centre.x, Tolerance, "one metre of world x");
            Assert.AreEqual(0f, east.y - centre.y, Tolerance, "moving east must not move mapZ");

            Assert.AreEqual(1f / 6f, north.y - centre.y, Tolerance, "one metre of world z");
            Assert.AreEqual(0f, north.x - centre.x, Tolerance, "moving north must not move mapX");
        }

        [Test]
        public void APointOutsideTheRegionIsRejectedAndNeverClampedToTheEdge()
        {
            // Each sample sits just past one edge with the other axis three quarters across, so a
            // clamped answer would be (0, 0.75), (1, 0.75), (0.75, 0) or (0.75, 1) - none of them
            // default. Asserting the out value is what actually catches a silent clamp; the bool
            // alone would still pass if the coordinate had been filled in first.
            const float offset = 7.5f;
            var samples = new[]
            {
                new Vector3(-15.01f, 8f, offset),
                new Vector3(15.01f, 8f, offset),
                new Vector3(offset, 8f, -15.01f),
                new Vector3(offset, 8f, 15.01f),
                new Vector3(-400f, 8f, 900f)
            };

            foreach (var sample in samples)
            {
                Assert.IsFalse(Arena.Contains(sample.x, sample.z), "outside the region: " + sample);
                Assert.IsFalse(Arena.TryWorldToMap(sample, out var map),
                    "a point outside the region must be refused: " + sample);
                Assert.AreEqual(Vector2.zero, map,
                    "the refused point at " + sample + " came back as a coordinate, not as default");
            }
        }

        [Test]
        public void AMapPointOutsideTheUnitSquareIsRejected()
        {
            var samples = new[]
            {
                new Vector2(-0.01f, 0.5f),
                new Vector2(1.01f, 0.5f),
                new Vector2(0.5f, -0.01f),
                new Vector2(0.5f, 1.01f),
                new Vector2(2f, 2f)
            };

            foreach (var sample in samples)
            {
                Assert.IsFalse(Arena.TryMapToWorld(sample, out var world),
                    "a map point outside the unit square must be refused: " + sample);
                Assert.AreEqual(Vector2.zero, world,
                    "the refused map point " + sample + " came back as a world position");
            }
        }

        [Test]
        public void NonFiniteInputIsRejectedRatherThanMapped()
        {
            foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.IsFalse(Arena.TryWorldToMap(new Vector3(bad, 8f, 0f), out _), "world x " + bad);
                Assert.IsFalse(Arena.TryWorldToMap(new Vector3(0f, 8f, bad), out _), "world z " + bad);
                Assert.IsFalse(Arena.TryMapToWorld(new Vector2(bad, 0.5f), out _), "map x " + bad);
                Assert.IsFalse(Arena.TryMapToWorld(new Vector2(0.5f, bad), out _), "map z " + bad);

                // A broken height is not this type's to police: y is never read, so validating it
                // would claim an authority over depth that belongs to DiveDepthBandSet.
                Assert.IsTrue(Arena.TryWorldToMap(new Vector3(0f, bad, 0f), out var map),
                    "a bad height must not break a conversion that never reads it");
                Assert.AreEqual(new Vector2(0.5f, 0.5f), map);
            }
        }

        [Test]
        public void DegenerateOrInvertedBoundsRejectEveryConversion()
        {
            var broken = new[]
            {
                new DiveRegionBounds(5f, 5f, -15f, 15f),        // zero width
                new DiveRegionBounds(-15f, 15f, 4f, 4f),        // zero depth
                new DiveRegionBounds(15f, -15f, -15f, 15f),     // inverted x
                new DiveRegionBounds(-15f, 15f, 15f, -15f),     // inverted z
                new DiveRegionBounds(float.NaN, 15f, -15f, 15f),
                new DiveRegionBounds(-15f, float.PositiveInfinity, -15f, 15f)
            };

            foreach (var region in broken)
            {
                Assert.IsFalse(region.IsValid, "these bounds must not read as valid");
                Assert.IsFalse(region.TryWorldToMap(new Vector3(5f, 8f, 0f), out var map),
                    "an invalid region must not convert a world point");
                Assert.AreEqual(Vector2.zero, map);
                Assert.IsFalse(region.TryMapToWorld(new Vector2(0.5f, 0.5f), out var world),
                    "an invalid region must not convert a map point");
                Assert.AreEqual(Vector2.zero, world);
            }
        }

        [Test]
        public void TheRegionEdgeIsInsideAndMapsToExactlyZeroAndOne()
        {
            // dock-town-1 and anchor-near-1 are authored by hand, and a wall-hugging anchor that
            // lands exactly on the boundary must not read as "outside the world". Exact equality
            // rather than a tolerance: the endpoints are the one place the arithmetic has to land
            // on the number itself, or an icon would sit a pixel off the map's own edge.
            Assert.IsTrue(Arena.Contains(-15f, 0f));
            Assert.IsTrue(Arena.Contains(15f, 0f));
            Assert.IsTrue(Arena.Contains(0f, -15f));
            Assert.IsTrue(Arena.Contains(0f, 15f));

            AssertMapsExactly(Arena, -15f, 0f, 0f, 0.5f);
            AssertMapsExactly(Arena, 15f, 0f, 1f, 0.5f);
            AssertMapsExactly(Arena, 0f, -15f, 0.5f, 0f);
            AssertMapsExactly(Arena, 0f, 15f, 0.5f, 1f);

            Assert.IsTrue(Arena.TryMapToWorld(new Vector2(0f, 1f), out var corner));
            Assert.AreEqual(-15f, corner.x, "map 0 must land exactly on the region's west edge");
            Assert.AreEqual(15f, corner.y, "map 1 must land exactly on the region's north edge");
        }

        private static void AssertMaps(
            DiveRegionBounds region, float worldX, float worldZ, float mapX, float mapZ)
        {
            Assert.IsTrue(region.TryWorldToMap(new Vector3(worldX, 8f, worldZ), out var map),
                "expected " + worldX + ", " + worldZ + " to be inside the region");
            Assert.AreEqual(mapX, map.x, Tolerance, "mapX at " + worldX + ", " + worldZ);
            Assert.AreEqual(mapZ, map.y, Tolerance, "mapZ at " + worldX + ", " + worldZ);
        }

        private static void AssertMapsExactly(
            DiveRegionBounds region, float worldX, float worldZ, float mapX, float mapZ)
        {
            Assert.IsTrue(region.TryWorldToMap(new Vector3(worldX, 8f, worldZ), out var map));
            Assert.AreEqual(mapX, map.x, "mapX at " + worldX + ", " + worldZ);
            Assert.AreEqual(mapZ, map.y, "mapZ at " + worldX + ", " + worldZ);
        }
    }
}
