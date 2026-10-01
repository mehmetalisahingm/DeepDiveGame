using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeepDive.World.Tests
{
    // The region component is the scene's mount for DiveRegionBounds: authored extents, a lookup
    // and two ways of refusing a point outside them. The arithmetic itself is pinned by
    // DiveRegionBoundsTests; what matters here is that the mount does not add a clamp, a guess or
    // a second answer. Where the region actually sits in DiveTestArea is a scene test's job.
    public class DiveRegionFieldTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void DestroySpawned()
        {
            foreach (var go in spawned)
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private DiveRegionField NewRegion(string id = "region-near-1", float extent = 15f)
        {
            var host = new GameObject("DiveRegion_Test");
            spawned.Add(host);

            var region = host.AddComponent<DiveRegionField>();
            region.Configure(id, -extent, extent, -extent, extent);
            return region;
        }

        private static void Validate(DiveRegionField target) =>
            typeof(DiveRegionField)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);

        [Test]
        public void AFreshRegionCarriesTheArenaExtents()
        {
            // The default is DiveTestArea's arena, so a component dropped into the scene by hand
            // is already the right region rather than a degenerate one that refuses everything.
            var host = new GameObject("DiveRegion_Default");
            spawned.Add(host);
            var region = host.AddComponent<DiveRegionField>();

            Assert.AreEqual(-15f, region.Bounds.MinX);
            Assert.AreEqual(15f, region.Bounds.MaxX);
            Assert.AreEqual(-15f, region.Bounds.MinZ);
            Assert.AreEqual(15f, region.Bounds.MaxZ);
            Assert.IsTrue(region.Bounds.IsValid);
            Validate(region);
        }

        [Test]
        public void TheMountConvertsThroughTheBoundsAndAddsNothing()
        {
            var region = NewRegion();

            Assert.IsTrue(region.TryWorldToMap(new Vector3(9f, 8f, 8.5f), out var map));
            Assert.AreEqual(0.8f, map.x, 1e-4f, "the anchorage's mapX");
            Assert.AreEqual(0.7833f, map.y, 1e-3f, "the anchorage's mapZ");

            Assert.IsTrue(region.TryMapToWorld(map, out var back));
            Assert.AreEqual(9f, back.x, 1e-3f);
            Assert.AreEqual(8.5f, back.y, 1e-3f);
        }

        [Test]
        public void APointOutsideTheRegionIsRefusedWithoutAClamp()
        {
            var region = NewRegion();

            Assert.IsFalse(region.TryWorldToMap(new Vector3(15.01f, 8f, 7.5f), out var map));
            Assert.AreEqual(Vector2.zero, map, "the refused point came back as a coordinate");
            Assert.IsFalse(region.TryMapToWorld(new Vector2(1.01f, 0.5f), out var world));
            Assert.AreEqual(Vector2.zero, world);
        }

        [Test]
        public void TheCheckedFormThrowsWhereTheBoolFormWouldReturnFalse()
        {
            // The scene script authors the dock and the anchorage by hand; one of them outside
            // its own region is a mistake to stop the save on, not a false to step over.
            var region = NewRegion();

            Assert.DoesNotThrow(() => region.WorldToMapChecked(new Vector3(9f, 8f, 8.5f)));

            var error = Assert.Throws<InvalidOperationException>(
                () => region.WorldToMapChecked(new Vector3(0f, 8f, 15.5f)));
            StringAssert.Contains("P3_ROUTE_OUT_OF_REGION", error.Message);
            StringAssert.Contains("region-near-1", error.Message);
        }

        [Test]
        public void InvalidExtentsAreReportedAndRefuseEveryConversion()
        {
            var region = NewRegion();
            region.Configure("region-near-1", 15f, -15f, -15f, 15f);   // inverted x

            Assert.IsFalse(region.Bounds.IsValid);
            Assert.IsFalse(region.TryWorldToMap(new Vector3(0f, 8f, 0f), out _));
            Assert.IsFalse(region.TryMapToWorld(new Vector2(0.5f, 0.5f), out _));

            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_REGION_INVALID"));
            Validate(region);
        }

        [Test]
        public void TwoRegionsInOneSceneAreRefusedRatherThanPickedBetween()
        {
            var only = NewRegion();
            Assert.IsTrue(DiveRegionField.TryFind(out var found));
            Assert.AreSame(only, found);

            // A second mount is a second answer to "where is this on the map", and choosing one
            // would make the map depend on scene load order.
            NewRegion("region-near-1", 20f);
            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_DUPLICATE_REGION"));
            Assert.IsFalse(DiveRegionField.TryFind(out var ambiguous));
            Assert.IsNull(ambiguous);
        }
    }
}
