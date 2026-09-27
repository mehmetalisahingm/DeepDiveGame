using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // The exploration fog's host state: coordinate cell ids on the fixed 5 m world grid, ids that
    // survive the region growing, first-entry discovery, idempotent revisits, out-of-region refusal,
    // the depth band per cell, and the read-only snapshot. A fake IExplorerPositionSource stands in
    // for Mehmet's approved feed.
    public class ExplorationCellAuthorityTests
    {
        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);

        // Asymmetric, off-origin and off the 5 m lines, so an x/z swap, a dropped offset or a
        // missing partial edge cell cannot pass.
        private static readonly DiveRegionBounds Skewed = new DiveRegionBounds(-4f, 12f, 3f, 9f);

        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        private sealed class FakeSource : IExplorerPositionSource
        {
            public readonly List<ExplorerPosition> Next = new List<ExplorerPosition>(8);
            public int Calls;

            public void CollectPositions(List<ExplorerPosition> into)
            {
                Calls++;
                into.Clear();
                for (var i = 0; i < Next.Count; i++) into.Add(Next[i]);
            }

            public FakeSource Set(params Vector3[] worlds)
            {
                Next.Clear();
                for (var i = 0; i < worlds.Length; i++) Next.Add(new ExplorerPosition(new PlayerId((ulong)i), worlds[i]));
                return this;
            }
        }

        private static ExplorationCellAuthority NearRegion(IReadOnlyList<WaterBody> water = null) =>
            new ExplorationCellAuthority(ExplorationIds.NearRegionId, Arena, water ?? Sea);

        private static string Id(int gx, int gz) => ExplorationIds.CellId(ExplorationIds.NearRegionId, gx, gz);

        // ---- cell id / layout ----

        [Test]
        public void CellId_IsRegionPlusGridCoordinates()
        {
            Assert.That(ExplorationIds.CellId("region-near-1", 2, -3), Is.EqualTo("region-near-1:gx2:gz-3"));
            Assert.That(ExplorationIds.CellSizeMetres, Is.EqualTo(5f));
        }

        [Test]
        public void Layout_SamePosition_SameCell_EveryTime()
        {
            var layout = new ExplorationCellLayout("r", Arena);
            var p = new Vector3(3.3f, 2f, -7.1f);
            Assert.That(layout.TryCell(p, out var x0, out var z0), Is.True);
            Assert.That((x0, z0), Is.EqualTo((0, -2)));
            for (var i = 0; i < 100; i++)
            {
                Assert.That(layout.TryCell(p, out var x, out var z), Is.True);
                Assert.That((x, z), Is.EqualTo((x0, z0)));
            }
        }

        [Test]
        public void Layout_CellsAreWorldGridSquares_AnchoredAtTheWorldOrigin()
        {
            // 5 m squares from world 0,0: gx = floor(x / 5), gz = floor(z / 5), whatever the region.
            var arena = new ExplorationCellLayout("r", Arena);
            AssertCell(arena, new Vector3(-15f, 0f, -15f), -3, -3);
            AssertCell(arena, new Vector3(-9f, 0f, -14f), -2, -3);
            AssertCell(arena, new Vector3(-14f, 0f, -9f), -3, -2);
            AssertCell(arena, new Vector3(0f, 0f, 0f), 0, 0);
            AssertCell(arena, new Vector3(-0.01f, 0f, 4.99f), -1, 0);

            var skewed = new ExplorationCellLayout("r", Skewed);
            AssertCell(skewed, new Vector3(8.5f, 0f, 5.9f), 1, 1);
            AssertCell(skewed, new Vector3(-4f, 0f, 3f), -1, 0);
        }

        [Test]
        public void Layout_GridRange_IsDerivedFromTheFootprint_NotAConstant()
        {
            AssertGrid(new ExplorationCellLayout("r", Arena).Grid, -3, -3, 6, 6);                                      // 30 x 30
            AssertGrid(new ExplorationCellLayout("r", new DiveRegionBounds(-20f, 20f, -20f, 20f)).Grid, -4, -4, 8, 8); // 40 x 40
            // Skewed -4..12 x 3..9 is off the 5 m lines: partial edge cells, gx -1..2, gz 0..1.
            AssertGrid(new ExplorationCellLayout("r", Skewed).Grid, -1, 0, 4, 2);
        }

        [Test]
        public void ExpandingTheRegionKeepsExistingCellIds()
        {
            var small = NearRegion(); // -15..15
            var grown = new ExplorationCellAuthority(ExplorationIds.NearRegionId,
                new DiveRegionBounds(-20f, 20f, -20f, 20f), Sea);
            var eastOnly = new ExplorationCellAuthority(ExplorationIds.NearRegionId,
                new DiveRegionBounds(-15f, 25f, -15f, 15f), Sea);

            // Every position of the old 30 x 30 world keeps its id when the world becomes 40 x 40,
            // grown on all sides or only to one side.
            for (var x = -14.9f; x < 15f; x += 0.7f)
            for (var z = -14.9f; z < 15f; z += 0.7f)
            {
                var p = new Vector3(x, 4f, z);
                Assert.That(small.TryGetCellId(p, out var before), Is.True, $"{p}");
                Assert.That(grown.TryGetCellId(p, out var after), Is.True, $"{p}");
                Assert.That(eastOnly.TryGetCellId(p, out var east), Is.True, $"{p}");
                Assert.That(after, Is.EqualTo(before), $"{p}");
                Assert.That(east, Is.EqualTo(before), $"{p}");
            }

            // The old cells all still exist; growth only adds new ones around them.
            var oldIds = new HashSet<string>();
            foreach (var c in small.Snapshot().Cells) oldIds.Add(c.CellId);
            var newIds = new HashSet<string>();
            foreach (var c in grown.Snapshot().Cells) newIds.Add(c.CellId);
            Assert.That(oldIds.Count, Is.EqualTo(36));
            Assert.That(newIds.Count, Is.EqualTo(64));
            Assert.That(oldIds.IsSubsetOf(newIds), Is.True);

            // A cell discovered in the small world has the id the grown world opens for the same spot.
            var events = new List<string>();
            small.Tick(new FakeSource().Set(new Vector3(-12f, 4f, 7f)), events);
            grown.Tick(new FakeSource().Set(new Vector3(-12f, 4f, 7f)), events);
            Assert.That(events, Is.EqualTo(new[] { Id(-3, 1), Id(-3, 1) }));
            Assert.That(grown.IsDiscovered(events[0]), Is.True);
        }

        [Test]
        public void Layout_MaxEdgeIsInsideTheLastCell_NotAPhantomCell()
        {
            var layout = new ExplorationCellLayout("r", Arena);
            AssertCell(layout, new Vector3(15f, 0f, 15f), 2, 2);
        }

        [Test]
        public void Layout_Y_DoesNotChooseTheCell()
        {
            var layout = new ExplorationCellLayout("r", Arena);
            AssertCell(layout, new Vector3(2f, -50f, 2f), 0, 0);
            AssertCell(layout, new Vector3(2f, 50f, 2f), 0, 0);
        }

        [Test]
        public void Layout_OutOfRegion_IsRefused_NotClamped()
        {
            var layout = new ExplorationCellLayout("r", Arena);
            foreach (var p in new[]
                     {
                         new Vector3(15.01f, 0f, 0f), new Vector3(-15.01f, 0f, 0f),
                         new Vector3(0f, 0f, 15.01f), new Vector3(0f, 0f, -100f),
                         new Vector3(float.NaN, 0f, 0f), new Vector3(0f, 0f, float.NegativeInfinity)
                     })
            {
                Assert.That(layout.TryCell(p, out var x, out var z), Is.False, $"{p}");
                Assert.That((x, z), Is.EqualTo((0, 0)));
            }
        }

        // ---- map projection (derived from the canonical grid, never stored) ----

        [Test]
        public void Projection_IsTheCellCentreOfTheGrid()
        {
            var grid = new ExplorationCellLayout("r", Arena).Grid; // gx/gz -3..2
            AssertCentre(grid, -3, -3, 0.5f / 6f, 0.5f / 6f);
            AssertCentre(grid, 0, 0, 3.5f / 6f, 3.5f / 6f);
            AssertCentre(grid, 2, -1, 5.5f / 6f, 2.5f / 6f);

            var skewed = new ExplorationCellLayout("r", Skewed).Grid; // gx -1..2, gz 0..1
            AssertCentre(skewed, -1, 0, 0.125f, 0.25f);
            AssertCentre(skewed, 2, 1, 0.875f, 0.75f);
        }

        [Test]
        public void Projection_RefusesCellsOutsideTheGrid_NotClamped()
        {
            var grid = new ExplorationCellLayout("r", Arena).Grid;
            foreach (var (gx, gz) in new[] { (-4, 0), (3, 0), (0, -4), (0, 3) })
            {
                Assert.That(ExplorationMapProjection.TryCellCenter(grid, gx, gz, out var u, out var v), Is.False, $"{gx},{gz}");
                Assert.That((u, v), Is.EqualTo((0f, 0f)));
            }
            Assert.That(ExplorationMapProjection.TryCellCenter(default, 0, 0, out _, out _), Is.False);
        }

        [Test]
        public void Projection_OnAnAlignedRegion_LandsOnTheCellsWorldCentre_BeforeAndAfterGrowth()
        {
            // Same cell, two region sizes: u/v differ (they are normalized to each grid) but both map
            // back through DiveRegionBounds to the same world point - the cell's centre. No second
            // world->map algorithm: the grid is derived from the bounds and the bounds invert it.
            var regions = new[] { Arena, new DiveRegionBounds(-20f, 20f, -20f, 20f) };
            foreach (var bounds in regions)
            {
                var fog = new ExplorationCellAuthority("r", bounds, Sea);
                foreach (var cell in fog.Snapshot().Cells)
                {
                    Assert.That(ExplorationMapProjection.TryCellCenter(fog.Grid, cell, out var u, out var v), Is.True);
                    Assert.That(bounds.TryMapToWorld(new Vector2(u, v), out var world), Is.True);
                    Assert.That(world.x, Is.EqualTo((cell.GridX + 0.5f) * ExplorationIds.CellSizeMetres).Within(1e-4f));
                    Assert.That(world.y, Is.EqualTo((cell.GridZ + 0.5f) * ExplorationIds.CellSizeMetres).Within(1e-4f));

                    // ...and that world point is in this very cell.
                    Assert.That(fog.TryGetCellId(new Vector3(world.x, 4f, world.y), out var back), Is.True);
                    Assert.That(back, Is.EqualTo(cell.CellId));
                }
            }
        }

        [Test]
        public void Projection_DoesNotAllocate()
        {
            var grid = new ExplorationCellLayout("r", Arena).Grid;
            ExplorationMapProjection.TryCellCenter(grid, 0, 0, out _, out _); // warm up (JIT)
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = 0f;
            for (var i = 0; i < 10000; i++)
                if (ExplorationMapProjection.TryCellCenter(grid, i % 6 - 3, 0, out var u, out _)) sum += u;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes (sum {sum})");
        }

        // ---- DepthBandId semantics (frozen) ----

        [Test]
        public void DepthBandId_IsAKnownIdOrUnclassified_NeverNullOrWhitespace()
        {
            Assert.That(DepthBandIds.Unclassified, Is.EqualTo(""));
            Assert.That(DepthBandIds.IsValidOrUnclassified(""), Is.True);
            Assert.That(DepthBandIds.IsValidOrUnclassified(DepthBandIds.Shallow), Is.True);
            Assert.That(DepthBandIds.IsValidOrUnclassified(DepthBandIds.Reef), Is.True, "reserved, valid to load");
            Assert.That(DepthBandIds.IsValidOrUnclassified(null), Is.False);
            Assert.That(DepthBandIds.IsValidOrUnclassified(" "), Is.False);
            Assert.That(DepthBandIds.IsValidOrUnclassified("\t"), Is.False);
            Assert.That(DepthBandIds.IsValidOrUnclassified("Shallow"), Is.False, "ids are exact, not case-folded");
            Assert.That(DepthBandIds.IsKnown(DepthBandIds.Unclassified), Is.False, "unclassified is not a band");

            Assert.That(new ExplorationCellState("c", 0, 0, true, null).DepthBandId, Is.EqualTo(DepthBandIds.Unclassified));
        }

        [Test]
        public void Snapshot_OnlyEverHoldsValidBandIds()
        {
            var fog = NearRegion();
            var source = new FakeSource();
            fog.Tick(source.Set(new Vector3(0f, 4f, 0f), new Vector3(-12f, -5f, -12f), new Vector3(12f, 9f, 12f)));
            foreach (var cell in fog.Snapshot().Cells)
                Assert.That(DepthBandIds.IsValidOrUnclassified(cell.DepthBandId), Is.True, cell.CellId);
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
            Assert.That(CellOf(fog, -3, -3).DepthBandId, Is.EqualTo(DepthBandIds.Unclassified), "13 m: no guessed band");
            Assert.That(CellOf(fog, 2, 2).DepthBandId, Is.EqualTo(DepthBandIds.Unclassified), "above the line");
        }

        // ---- authority ----

        [Test]
        public void Constructor_RefusesInvalidLayout()
        {
            Assert.Throws<ArgumentException>(() => new ExplorationCellAuthority("r", new DiveRegionBounds(1f, -1f, 0f, 1f), Sea));
            Assert.Throws<ArgumentException>(() => new ExplorationCellAuthority("", Arena, Sea));
        }

        [Test]
        public void FirstEntry_DiscoversTheCell_Once()
        {
            var fog = NearRegion();
            var source = new FakeSource().Set(new Vector3(0f, 4f, 0f));
            var events = new List<string>();

            Assert.That(fog.Tick(source, events), Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(new[] { Id(0, 0) }));
            Assert.That(fog.IsDiscovered(0, 0), Is.True);
            Assert.That(fog.DiscoveredCount, Is.EqualTo(1));
        }

        [Test]
        public void Revisit_IsNotASecondDiscovery()
        {
            var fog = NearRegion();
            var source = new FakeSource().Set(new Vector3(0f, 4f, 0f));
            var events = new List<string>();
            fog.Tick(source, events);
            var revision = fog.Revision;

            // Same spot, another spot in the same cell, and two players in it at once.
            source.Set(new Vector3(0f, 4f, 0f));
            Assert.That(fog.Tick(source, events), Is.EqualTo(0));
            source.Set(new Vector3(4.9f, 1f, 4.9f), new Vector3(0.1f, 7f, 0.1f));
            Assert.That(fog.Tick(source, events), Is.EqualTo(0));

            Assert.That(events, Is.EqualTo(new[] { Id(0, 0) }), "one discovery result, ever");
            Assert.That(fog.Revision, Is.EqualTo(revision), "a revisit changes nothing");
            Assert.That(fog.TryObserve(new Vector3(0f, 4f, 0f), out var index), Is.False);
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void TwoPlayers_EnteringTheSameNewCellInOneTick_DiscoverItOnce()
        {
            var fog = NearRegion();
            var events = new List<string>();
            Assert.That(fog.Tick(new FakeSource().Set(new Vector3(1f, 4f, 1f), new Vector3(2f, 4f, 2f)), events),
                Is.EqualTo(1));
            Assert.That(events, Is.EqualTo(new[] { Id(0, 0) }));
        }

        [Test]
        public void OutOfRegionPosition_OpensNothing()
        {
            var fog = NearRegion();
            var events = new List<string>();
            Assert.That(fog.Tick(new FakeSource().Set(new Vector3(40f, 4f, 0f), new Vector3(0f, 4f, -15.5f)), events),
                Is.EqualTo(0));
            Assert.That(events, Is.Empty);
            Assert.That(fog.DiscoveredCount, Is.EqualTo(0));
            Assert.That(fog.Revision, Is.EqualTo(0));
        }

        [Test]
        public void DiscoveryFlow_AcrossTicks_OpensEachCellOnce()
        {
            var fog = NearRegion();
            var source = new FakeSource();
            var events = new List<string>();

            // A diver swims west->east along z = -12 (gz -3), through all six columns, and back.
            for (var x = -14.5f; x <= 14.5f; x += 1f) { source.Set(new Vector3(x, 4f, -12f)); fog.Tick(source, events); }
            for (var x = 14.5f; x >= -14.5f; x -= 1f) { source.Set(new Vector3(x, 4f, -12f)); fog.Tick(source, events); }

            Assert.That(events, Is.EqualTo(new[] { Id(-3, -3), Id(-2, -3), Id(-1, -3), Id(0, -3), Id(1, -3), Id(2, -3) }));
            Assert.That(fog.DiscoveredCount, Is.EqualTo(6));
            Assert.That(source.Calls, Is.EqualTo(60));
        }

        [Test]
        public void DiscoveryIsDeterministic_SameInputs_SameState()
        {
            var a = NearRegion();
            var b = NearRegion();
            var path = new[]
            {
                new Vector3(-13f, 4f, -13f), new Vector3(7f, 2f, 11f), new Vector3(7.5f, 9f, 11f),
                new Vector3(99f, 0f, 0f), new Vector3(14f, 1f, -3f)
            };
            var source = new FakeSource();
            foreach (var p in path) { source.Set(p); a.Tick(source); b.Tick(source); }

            var sa = a.Snapshot();
            var sb = b.Snapshot();
            Assert.That(sa.Revision, Is.EqualTo(sb.Revision));
            for (var i = 0; i < sa.Cells.Count; i++)
            {
                Assert.That(sa.Cells[i].CellId, Is.EqualTo(sb.Cells[i].CellId));
                Assert.That(sa.Cells[i].Discovered, Is.EqualTo(sb.Cells[i].Discovered));
                Assert.That(sa.Cells[i].DepthBandId, Is.EqualTo(sb.Cells[i].DepthBandId));
            }
        }

        [Test]
        public void Cell_RecordsTheDepthBand_FromTheWaterBodies()
        {
            var fog = NearRegion();
            fog.Tick(new FakeSource().Set(new Vector3(0f, 4f, 0f)));
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
        }

        [Test]
        public void Cell_FirstEnteredDry_LearnsItsBandOnTheFirstWetVisit_WithoutASecondDiscovery()
        {
            var fog = NearRegion();
            var events = new List<string>();
            var source = new FakeSource();

            fog.Tick(source.Set(new Vector3(0f, 8.5f, 0f)), events); // on the beach, above the line
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.Empty);
            var revision = fog.Revision;

            fog.Tick(source.Set(new Vector3(0f, 4f, 0f)), events);
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
            Assert.That(fog.Revision, Is.EqualTo(revision + 1), "the band fill is a change the map must see");
            Assert.That(events, Is.EqualTo(new[] { Id(0, 0) }), "but it is not a second discovery");

            // Never overwritten afterwards.
            fog.Tick(source.Set(new Vector3(0f, 8.5f, 0f)), events);
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.EqualTo(DepthBandIds.Shallow));
        }

        [Test]
        public void Cell_DeeperThanAnyAuthoredBand_HasNoBand_NotAGuessedOne()
        {
            // y -5 under a surface at 8 is 13 m down. Reef/deep have no metres yet, so no band.
            var fog = NearRegion();
            fog.Tick(new FakeSource().Set(new Vector3(0f, -5f, 0f)));
            Assert.That(fog.IsDiscovered(0, 0), Is.True);
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.Empty);
            Assert.That(DepthBandIds.IsKnown(DepthBandIds.Reef), Is.True, "reef stays a reserved id");
            Assert.That(DepthBandIds.IsKnown(DepthBandIds.Deep), Is.True, "deep stays a reserved id");
        }

        [Test]
        public void NoWater_CellsStillOpen_WithAnEmptyBand()
        {
            var fog = new ExplorationCellAuthority("r", Arena, Array.Empty<WaterBody>());
            fog.Tick(new FakeSource().Set(new Vector3(0f, 4f, 0f)));
            Assert.That(fog.IsDiscovered(0, 0), Is.True);
            Assert.That(CellOf(fog, 0, 0).DepthBandId, Is.Empty);
        }

        [Test]
        public void Snapshot_ListsEveryCell_WithCoordinateIds()
        {
            var s = NearRegion().Snapshot();
            Assert.That(s.Grid.RegionId, Is.EqualTo(ExplorationIds.NearRegionId));
            AssertGrid(s.Grid, -3, -3, 6, 6);
            Assert.That(s.Cells.Count, Is.EqualTo(36));
            Assert.That(s.Species, Is.Empty);
            var i = 0;
            for (var gz = -3; gz <= 2; gz++)
            for (var gx = -3; gx <= 2; gx++)
            {
                var cell = s.Cells[i++];
                Assert.That(cell.CellId, Is.EqualTo(Id(gx, gz)));
                Assert.That((cell.GridX, cell.GridZ), Is.EqualTo((gx, gz)));
                Assert.That(cell.Discovered, Is.False);
                Assert.That(cell.DepthBandId, Is.Empty);
            }
        }

        [Test]
        public void Snapshot_IsReused_UntilTheRevisionMoves()
        {
            var fog = NearRegion();
            IExplorationReadModel read = fog;
            var first = read.Snapshot();
            Assert.That(read.Snapshot().Cells, Is.SameAs(first.Cells));

            fog.Tick(new FakeSource().Set(new Vector3(0f, 4f, 0f)));
            var second = read.Snapshot();
            Assert.That(second.Cells, Is.Not.SameAs(first.Cells));
            Assert.That(second.Revision, Is.EqualTo(read.Revision));
            Assert.That(Find(first, Id(0, 0)).Discovered, Is.False, "an earlier snapshot is not mutated");
            Assert.That(Find(second, Id(0, 0)).Discovered, Is.True);
        }

        [Test]
        public void Tick_DoesNotAllocate()
        {
            var fog = NearRegion();
            var events = new List<string>(64);
            var source = new FakeSource();

            // Warm up (JIT) on a path that both opens and revisits cells.
            fog.Tick(source.Set(new Vector3(-14f, 4f, -14f), new Vector3(14f, 4f, 14f)), events);
            fog.Tick(source, events);

            source.Set(new Vector3(0f, 4f, 0f), new Vector3(-7f, 3f, 6f), new Vector3(40f, 4f, 0f));
            var before = GC.GetAllocatedBytesForCurrentThread();
            var opened = 0;
            for (var i = 0; i < 10000; i++) opened += fog.Tick(source, events);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.EqualTo(0), $"allocated {allocated} bytes");
            Assert.That(opened, Is.EqualTo(2), "only the first tick opens the two new cells");
        }

        private static ExplorationCellState CellOf(ExplorationCellAuthority fog, int gx, int gz) =>
            Find(fog.Snapshot(), ExplorationIds.CellId(fog.Grid.RegionId, gx, gz));

        private static ExplorationCellState Find(ExplorationSnapshot snapshot, string cellId)
        {
            foreach (var c in snapshot.Cells)
                if (c.CellId == cellId) return c;
            Assert.Fail($"no cell {cellId}");
            return default;
        }

        private static void AssertCell(ExplorationCellLayout layout, Vector3 p, int gx, int gz)
        {
            Assert.That(layout.TryCell(p, out var x, out var z), Is.True, $"{p} should be inside");
            Assert.That((x, z), Is.EqualTo((gx, gz)), $"{p}");
        }

        private static void AssertCentre(ExplorationGrid grid, int gx, int gz, float u, float v)
        {
            Assert.That(ExplorationMapProjection.TryCellCenter(grid, gx, gz, out var pu, out var pv), Is.True, $"{gx},{gz}");
            Assert.That(pu, Is.EqualTo(u).Within(1e-6f), $"u {gx},{gz}");
            Assert.That(pv, Is.EqualTo(v).Within(1e-6f), $"v {gx},{gz}");
        }

        private static void AssertGrid(ExplorationGrid grid, int minX, int minZ, int columns, int rows) =>
            Assert.That((grid.MinGridX, grid.MinGridZ, grid.Columns, grid.Rows), Is.EqualTo((minX, minZ, columns, rows)));
    }
}
