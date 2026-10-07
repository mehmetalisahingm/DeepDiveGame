using System.Collections.Generic;
using System.Linq;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.World.Tests
{
    public sealed class DeepEncounterWorldTests
    {
        private static readonly DiveRegionBounds Bounds = new DiveRegionBounds(-15f, 15f, -15f, 65f);
        private const string Region = ExplorationIds.NearRegionId;

        [Test]
        public void ExactlyThreeStableDistinctTracePointsAreAuthored()
        {
            Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, DeepEncounterWorld.TraceIds.Length);
            Assert.AreEqual(DeepEncounterWorld.TraceIds.Length, DeepEncounterWorld.TracePositions.Length);
            CollectionAssert.AllItemsAreUnique(DeepEncounterWorld.TraceIds);

            var cells = new HashSet<string>();
            for (var i = 0; i < DeepEncounterWorld.TracePositions.Length; i++)
            {
                Assert.IsTrue(Bounds.Contains(DeepEncounterWorld.TracePositions[i].x, DeepEncounterWorld.TracePositions[i].z));
                var cell = DeepEncounterWorld.ExpectedCellId(DeepEncounterWorld.TracePositions[i], Region, Bounds);
                Assert.IsNotEmpty(cell);
                Assert.IsTrue(cells.Add(cell), "each trace must occupy a distinct exploration cell");
            }
        }

        [Test]
        public void AuthoredTracesAndArenaAreInDeepWater()
        {
            var water = new List<WaterBody> { new WaterBody(-15f, 15f, -15f, 65f, 8f) };
            foreach (var position in DeepEncounterWorld.TracePositions)
            {
                Assert.IsTrue(WaterDepth.TryClassify(water, position, out var band));
                Assert.AreEqual(DepthBandIds.Deep, band, position.ToString());
            }

            Assert.IsTrue(WaterDepth.TryClassify(water, DeepEncounterWorld.ArenaPosition, out var arenaBand));
            Assert.AreEqual(DepthBandIds.Deep, arenaBand);
            Assert.IsTrue(Bounds.Contains(DeepEncounterWorld.ArenaPosition.x, DeepEncounterWorld.ArenaPosition.z));
            Assert.IsTrue(Bounds.Contains(DeepEncounterWorld.BossPosition.x, DeepEncounterWorld.BossPosition.z));
        }

        [Test]
        public void TraceValidationRequiresExactIdCellAndDeepBand()
        {
            var traceId = DeepEncounterWorld.TraceIds[0];
            var cell = DeepEncounterWorld.ExpectedCellId(DeepEncounterWorld.TracePositions[0], Region, Bounds);

            Assert.IsTrue(DeepEncounterWorld.IsValidTraceContext(traceId, cell, DepthBandIds.Deep, Region, Bounds));
            Assert.IsFalse(DeepEncounterWorld.IsValidTraceContext(traceId, cell, DepthBandIds.Reef, Region, Bounds));
            Assert.IsFalse(DeepEncounterWorld.IsValidTraceContext("forged-trace", cell, DepthBandIds.Deep, Region, Bounds));

            var otherCell = DeepEncounterWorld.ExpectedCellId(DeepEncounterWorld.TracePositions[1], Region, Bounds);
            Assert.IsFalse(DeepEncounterWorld.IsValidTraceContext(traceId, otherCell, DepthBandIds.Deep, Region, Bounds));
        }

        [Test]
        public void ArenaValidationRequiresExactArenaCellAndDeepBand()
        {
            var cell = DeepEncounterWorld.ExpectedCellId(DeepEncounterWorld.ArenaPosition, Region, Bounds);

            Assert.IsTrue(DeepEncounterWorld.IsValidArenaContext(DeepEncounterWorld.ArenaId, cell, DepthBandIds.Deep, Region, Bounds));
            Assert.IsFalse(DeepEncounterWorld.IsValidArenaContext("fake-arena", cell, DepthBandIds.Deep, Region, Bounds));
            Assert.IsFalse(DeepEncounterWorld.IsValidArenaContext(DeepEncounterWorld.ArenaId, cell, DepthBandIds.Reef, Region, Bounds));

            var traceCell = DeepEncounterWorld.ExpectedCellId(DeepEncounterWorld.TracePositions[0], Region, Bounds);
            Assert.IsFalse(DeepEncounterWorld.IsValidArenaContext(DeepEncounterWorld.ArenaId, traceCell, DepthBandIds.Deep, Region, Bounds));
        }

        [Test]
        public void NoProgressionContractContainsRawWorldCoordinates()
        {
            var state = new DeepProgressionState(DeepProgressionStage.Trace, 2, 3, null, 7);
            Assert.AreEqual(DeepProgressionStage.Trace, state.Stage);
            Assert.AreEqual(2, state.TracesFound);
            Assert.IsFalse(state.BossUnlocked);
            Assert.IsFalse(typeof(DeepProgressionState).GetFields().Any(field => field.FieldType == typeof(Vector3)),
                "shared progression state must never leak trace/arena/boss world coordinates");
        }
    }
}
