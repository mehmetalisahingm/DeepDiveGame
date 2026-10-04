using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // World position -> depth -> band, read from WaterBody (WaterField.Bodies) only. Plain numbers,
    // no scene: the DiveTestArea water is SwimVolume's box (centre 0,4,0 size 30,8,30, surface 8).
    public class WaterDepthTests
    {
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        [Test]
        public void Depth_IsSurfaceMinusY_InsideTheFootprint()
        {
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(0f, 5f, 0f), out var d), Is.True);
            Assert.That(d, Is.EqualTo(3f));
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(3f, 8.4f, -2f), out d), Is.True);
            Assert.That(d, Is.EqualTo(-0.4f).Within(1e-5f), "above the line is negative, not zero");
        }

        [Test]
        public void Depth_IsRefused_OffTheWaterOrForBadInput()
        {
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(15.01f, 4f, 0f), out _), Is.False);
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(float.NaN, 4f, 0f), out _), Is.False);
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(0f, float.PositiveInfinity, 0f), out _), Is.False);
            Assert.That(WaterDepth.TryDepthAt(null, Vector3.zero, out _), Is.False);
            Assert.That(WaterDepth.TryDepthAt(Array.Empty<WaterBody>(), Vector3.zero, out _), Is.False);
        }

        [Test]
        public void Depth_FootprintEdgeIsInclusive()
        {
            Assert.That(WaterDepth.TryDepthAt(Sea, new Vector3(15f, 4f, -15f), out var d), Is.True);
            Assert.That(d, Is.EqualTo(4f));
        }

        [Test]
        public void Depth_OverlappingBodies_HighestSurfaceWins_WhateverTheOrder()
        {
            var low = new WaterBody(-5f, 5f, -5f, 5f, 6f);
            var high = new WaterBody(-5f, 5f, -5f, 5f, 8f);
            var p = new Vector3(0f, 2f, 0f);
            Assert.That(WaterDepth.TryDepthAt(new[] { low, high }, p, out var a), Is.True);
            Assert.That(WaterDepth.TryDepthAt(new[] { high, low }, p, out var b), Is.True);
            Assert.That(a, Is.EqualTo(6f));
            Assert.That(b, Is.EqualTo(6f));
        }

        [Test]
        public void Classify_ShallowBand_BoundsAreInclusive()
        {
            // Surface (depth 0) and the sea bed of DiveTestArea (y 0 = depth 8) are both shallow.
            AssertBand(new Vector3(0f, 8f, 0f), DepthBandIds.Shallow);
            AssertBand(new Vector3(0f, 4f, 0f), DepthBandIds.Shallow);
            AssertBand(new Vector3(0f, 0f, 0f), DepthBandIds.Shallow);
        }

        [Test]
        public void Classify_RefusesAboveSurface_OffWater_AndPastTheAuthoredBands()
        {
            AssertNoBand(new Vector3(0f, 8.01f, 0f));   // just above the line
            AssertNoBand(new Vector3(20f, 4f, 0f));     // not over water
            // Deeper than 35 m: past the deepest authored band (P4.3), so no band - never a guess
            // that it is "deep".
            AssertNoBand(new Vector3(0f, 8f - DiveDepthBands.DeepMaxDepth - 0.01f, 0f));
        }

        [Test]
        public void Classify_ThresholdsComeFromDiveDepthBands()
        {
            Assert.That(DiveDepthBands.ShallowMinDepth, Is.EqualTo(0f));
            Assert.That(DiveDepthBands.ShallowMaxDepth, Is.EqualTo(8f));
            AssertBand(new Vector3(0f, 8f - DiveDepthBands.ShallowMaxDepth, 0f), DepthBandIds.Shallow);
        }

        [Test]
        public void Classify_IsDeterministic()
        {
            var p = new Vector3(1.37f, 3.21f, -7.9f);
            WaterDepth.TryClassify(Sea, p, out var first);
            for (var i = 0; i < 100; i++)
            {
                WaterDepth.TryClassify(Sea, p, out var again);
                Assert.That(again, Is.SameAs(first));
            }
        }

        [Test]
        public void Classify_DoesNotAllocate()
        {
            IReadOnlyList<WaterBody> sea = Sea;
            var p = new Vector3(2f, 3f, 1f);
            WaterDepth.TryClassify(sea, p, out _); // warm up (JIT)
            var before = GC.GetAllocatedBytesForCurrentThread();
            var hits = 0;
            for (var i = 0; i < 10000; i++)
                if (WaterDepth.TryClassify(sea, p, out _)) hits++;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes (hits {hits})");
        }

        private static void AssertBand(Vector3 p, string expected)
        {
            Assert.That(WaterDepth.TryClassify(Sea, p, out var id), Is.True, $"{p} should classify");
            Assert.That(id, Is.EqualTo(expected));
        }

        private static void AssertNoBand(Vector3 p)
        {
            Assert.That(WaterDepth.TryClassify(Sea, p, out var id), Is.False, $"{p} should not classify");
            Assert.That(id, Is.Empty);
        }
    }
}
