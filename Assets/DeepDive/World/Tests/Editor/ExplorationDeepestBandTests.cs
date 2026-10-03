using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // P4.3-B2 (#108): a cell's band is the deepest band any approved diver reached in it (Shallow < Reef < Deep),
    // on the live path and the restore path alike, and HasDiscoveredCellInBand reads it. Plain numbers on the P4.3
    // region (x -15..15, z -15..65, surface y 8); no scene.
    public class ExplorationDeepestBandTests
    {
        private static readonly DiveRegionBounds Region = new DiveRegionBounds(-15f, 15f, -15f, 65f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 65f, 8f) };

        // One cell on the reef shelf (gx 0, gz 6) sampled at three depths.
        private static readonly Vector3 ShallowAt = new Vector3(2f, 6f, 32f);   //  2 m
        private static readonly Vector3 ReefAt = new Vector3(2f, -6f, 32f);     // 14 m
        private static readonly Vector3 DeepAt = new Vector3(2f, -20f, 32f);    // 28 m
        private static readonly string Cell = ExplorationIds.CellId(ExplorationIds.NearRegionId, 0, 6);

        private sealed class Source : IExplorerPositionSource
        {
            private readonly List<Vector3> next = new List<Vector3>();

            public Source Set(params Vector3[] worlds)
            {
                next.Clear();
                next.AddRange(worlds);
                return this;
            }

            public void CollectPositions(List<ExplorerPosition> into)
            {
                into.Clear();
                for (var i = 0; i < next.Count; i++) into.Add(new ExplorerPosition(new PlayerId((ulong)i), next[i]));
            }
        }

        private static ExplorationCellAuthority Fog() =>
            new ExplorationCellAuthority(ExplorationIds.NearRegionId, Region, Sea);

        private static string BandOf(ExplorationCellAuthority fog)
        {
            Assert.That(fog.TryGetCell(ReefAt, out var id, out var band), Is.True);
            Assert.That(id, Is.EqualTo(Cell));
            return band;
        }

        // --- Deepest wins -----------------------------------------------------------------------------------

        [Test]
        public void ShallowThenReefThenDeep_MovesDeeper_EachStepMovesTheRevision_OneDiscovery()
        {
            var fog = Fog();
            var source = new Source();
            var events = new List<string>();

            fog.Tick(source.Set(ShallowAt), events);
            Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Shallow));
            var revision = fog.Revision;

            fog.Tick(source.Set(ReefAt), events);
            Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Reef));
            Assert.That(fog.Revision, Is.EqualTo(revision + 1), "a deeper band is a change the map must see");

            fog.Tick(source.Set(DeepAt), events);
            Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Deep));
            Assert.That(fog.Revision, Is.EqualTo(revision + 2));

            Assert.That(events, Is.EqualTo(new[] { Cell }), "moving deeper is never a second discovery");
        }

        [Test]
        public void DeepThenShallow_NeverMovesBack_AndIsNotAChange()
        {
            var fog = Fog();
            var source = new Source();
            fog.Tick(source.Set(DeepAt));
            var revision = fog.Revision;

            fog.Tick(source.Set(ReefAt));
            fog.Tick(source.Set(ShallowAt));
            fog.Tick(source.Set(new Vector3(2f, 9f, 32f)));   // above the line

            Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Deep));
            Assert.That(fog.Revision, Is.EqualTo(revision), "revision moves only on a real change");
        }

        [Test]
        public void RestoreOrder_DoesNotMatter_TheDeepestAlwaysWins()
        {
            var bands = new[] { DepthBandIds.Shallow, DepthBandIds.Reef, DepthBandIds.Deep, DepthBandIds.Unclassified };
            var orders = new[]
            {
                new[] { 0, 1, 2, 3 }, new[] { 2, 1, 0, 3 }, new[] { 1, 3, 2, 0 }, new[] { 3, 2, 0, 1 }, new[] { 2, 0, 3, 1 }
            };

            foreach (var order in orders)
            {
                var fog = Fog();
                foreach (var i in order) fog.TryRestore(Cell, 0, 6, bands[i]);
                Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Deep), string.Join(",", order));
                Assert.That(fog.IsDiscovered(Cell), Is.True);
            }

            // A live visit before or after the restore lands in the same place.
            var liveFirst = Fog();
            liveFirst.Tick(new Source().Set(ReefAt));
            liveFirst.TryRestore(Cell, 0, 6, DepthBandIds.Shallow);
            var restoreFirst = Fog();
            restoreFirst.TryRestore(Cell, 0, 6, DepthBandIds.Shallow);
            restoreFirst.Tick(new Source().Set(ReefAt));
            Assert.That(BandOf(liveFirst), Is.EqualTo(BandOf(restoreFirst)));
            Assert.That(BandOf(liveFirst), Is.EqualTo(DepthBandIds.Reef));
        }

        [Test]
        public void DuplicateRestore_IsIdempotent()
        {
            var fog = Fog();
            Assert.That(fog.TryRestore(Cell, 0, 6, DepthBandIds.Deep), Is.EqualTo(CellRestoreOutcome.Restored));
            var revision = fog.Revision;

            for (var i = 0; i < 50; i++)
            {
                Assert.That(fog.TryRestore(Cell, 0, 6, DepthBandIds.Deep), Is.EqualTo(CellRestoreOutcome.AlreadyDiscovered));
                Assert.That(fog.TryRestore(Cell, 0, 6, DepthBandIds.Reef), Is.EqualTo(CellRestoreOutcome.AlreadyDiscovered));
            }

            Assert.That(fog.Revision, Is.EqualTo(revision));
            Assert.That(BandOf(fog), Is.EqualTo(DepthBandIds.Deep));
        }

        [Test]
        public void RestoredBand_IsStoredAsTheContractString()
        {
            var fog = Fog();
            var copy = new string(DepthBandIds.Reef.ToCharArray());
            fog.TryRestore(Cell, 0, 6, copy);

            Assert.That(ReferenceEquals(BandOf(fog), DepthBandIds.Reef), Is.True, "the save's string instance is not kept");
        }

        // --- Reef-discovery read ----------------------------------------------------------------------------

        [Test]
        public void HasDiscoveredCellInBand_NothingExplored_IsFalse()
        {
            var fog = Fog();
            foreach (var band in DepthBandIds.All)
                Assert.That(fog.HasDiscoveredCellInBand(band), Is.False, band);
        }

        [Test]
        public void HasDiscoveredCellInBand_ObservedReef_IsTrue()
        {
            var fog = Fog();
            fog.Tick(new Source().Set(ReefAt));
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Reef), Is.True);
        }

        [Test]
        public void HasDiscoveredCellInBand_RestoredReef_IsTrue()
        {
            var fog = Fog();
            fog.TryRestore(Cell, 0, 6, DepthBandIds.Reef);
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Reef), Is.True, "a reloaded campaign keeps its reef");
        }

        [Test]
        public void HasDiscoveredCellInBand_ShallowOnly_IsFalseForReef()
        {
            var fog = Fog();
            fog.Tick(new Source().Set(ShallowAt, new Vector3(0f, 4f, 0f)));
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Shallow), Is.True);
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Reef), Is.False);
        }

        [Test]
        public void HasDiscoveredCellInBand_DeepCell_IsNotAReefCell()
        {
            var fog = Fog();
            fog.Tick(new Source().Set(DeepAt));
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Deep), Is.True);
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Reef), Is.False, "exact band, not at-or-below");
        }

        [Test]
        public void HasDiscoveredCellInBand_UnknownOrUnclassified_IsFalse()
        {
            var fog = Fog();
            fog.Tick(new Source().Set(new Vector3(2f, 9f, 32f)));   // discovered dry: band ""
            Assert.That(fog.HasDiscoveredCellInBand(DepthBandIds.Unclassified), Is.False);
            Assert.That(fog.HasDiscoveredCellInBand(null), Is.False);
            Assert.That(fog.HasDiscoveredCellInBand("Reef"), Is.False);
            Assert.That(fog.HasDiscoveredCellInBand(" "), Is.False);
        }

        [Test]
        public void HasDiscoveredCellInBand_DoesNotAllocate()
        {
            var fog = Fog();
            fog.Tick(new Source().Set(ShallowAt));
            fog.HasDiscoveredCellInBand(DepthBandIds.Reef); // warm up (JIT)

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) fog.HasDiscoveredCellInBand(DepthBandIds.Reef);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0));
        }
    }
}
