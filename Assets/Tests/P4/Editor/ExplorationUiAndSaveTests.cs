using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.MapUI;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #90 map fog, encyclopedia, exploration save and the client mirror, all against Utku's REAL authorities
    // (#94) - no fake read model - and, for the save, the real EconomySaveStore writing a real file.
    public sealed class ExplorationUiAndSaveTests
    {
        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };
        private static readonly Vector3 ShallowSpot = new Vector3(2f, 5f, 2f);    // 3 m under the surface
        private static readonly Vector3 OtherSpot = new Vector3(-12f, 5f, -12f);

        private sealed class Explorers : IExplorerPositionSource
        {
            public readonly List<ExplorerPosition> Now = new List<ExplorerPosition>();
            public void CollectPositions(List<ExplorerPosition> into) { into.Clear(); into.AddRange(Now); }
        }

        private static (ExplorationCellAuthority cells, SpeciesObservationAuthority species) World()
        {
            var cells = new ExplorationCellAuthority(ExplorationIds.NearRegionId, Arena, Sea);
            return (cells, new SpeciesObservationAuthority(cells));
        }

        private static void Visit(ExplorationCellAuthority cells, Vector3 at)
        {
            var feed = new Explorers();
            feed.Now.Add(new ExplorerPosition(new PlayerId(0), at));
            cells.Tick(feed);
        }

        // ---- map fog ------------------------------------------------------------------------------

        [Test]
        public void FogCoversEveryCellAndOpensOnlyVisitedOnes()
        {
            var (cells, species) = World();
            var closed = ExplorationMapPresenter.BuildFog(species.Snapshot());
            Assert.AreEqual(36, closed.Count, "6 x 6 cells over the -15..15 arena");
            Assert.AreEqual(0, ExplorationMapPresenter.DiscoveredCount(closed));

            Visit(cells, ShallowSpot);
            var fog = ExplorationMapPresenter.BuildFog(species.Snapshot());
            Assert.AreEqual(1, ExplorationMapPresenter.DiscoveredCount(fog));
            var open = fog.First(t => t.Discovered);
            Assert.AreEqual(DepthBandIds.Shallow, open.DepthBandId);
            Assert.AreEqual(1f / 6f, open.Width, 1e-5f);
        }

        [Test]
        public void FogTilesSitWhereTheRegionTransformPutsTheVisitedPoint()
        {
            // The tile is placed by Utku's grid projection; the region transform (DiveRegionField/DiveRegionBounds)
            // must agree for the arena, so a fog hole and a player icon at the same place line up.
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            var tile = ExplorationMapPresenter.BuildFog(species.Snapshot()).First(t => t.Discovered);
            Assert.IsTrue(Arena.TryWorldToMap(ShallowSpot, out var map));
            Assert.LessOrEqual(Mathf.Abs(map.x - tile.CenterU), tile.Width * 0.5f);
            Assert.LessOrEqual(Mathf.Abs(map.y - tile.CenterV), tile.Height * 0.5f);
        }

        [Test]
        public void AnInvalidGridDrawsNothingRatherThanGuessing()
        {
            var empty = new ExplorationSnapshot(default, new[] { new ExplorationCellState("x", 0, 0, true, "") },
                Array.Empty<SpeciesDiscoveryState>(), 1);
            Assert.AreEqual(0, ExplorationMapPresenter.BuildFog(empty).Count);
        }

        // ---- encyclopedia -------------------------------------------------------------------------

        [Test]
        public void EncyclopediaListsOnlyCountedSpeciesAndOpensPagesByEvidence()
        {
            var (cells, species) = World();
            Assert.AreEqual(0, EncyclopediaPresenter.Build(species.Snapshot()).Count, "nothing counted, nothing listed");
            Visit(cells, ShallowSpot);   // the cell learns its band from a real wet visit; an observation stamps that band

            species.AcceptSighting("sea_bass", ShallowSpot, 1);
            var seen = EncyclopediaPresenter.Build(species.Snapshot())[0];
            Assert.IsTrue(seen.Silhouette);
            Assert.IsFalse(seen.NameKnown);
            Assert.AreEqual("???", seen.DisplayName, "a sighting alone does not name it");
            Assert.IsFalse(seen.CatchData);
            Assert.IsFalse(seen.HabitatKnown);
            Assert.AreEqual(1, seen.ProgressSteps);

            species.AcceptRecording("rec-1", "sea_bass", ShallowSpot, 1);
            var recorded = EncyclopediaPresenter.Build(species.Snapshot())[0];
            Assert.IsTrue(recorded.NameKnown);
            Assert.AreEqual("Levrek", recorded.DisplayName);
            Assert.IsTrue(recorded.HabitatKnown);
            CollectionAssert.AreEqual(new[] { DepthBandIds.Shallow }, recorded.HabitatBandIds);

            species.AcceptCatch(new CaptureResult("cap-1", "dive-1", "sea_bass", 900, 1), ShallowSpot, 1);
            var full = EncyclopediaPresenter.Build(species.Snapshot())[0];
            Assert.IsTrue(full.CatchData);
            Assert.AreEqual(EncyclopediaEntry.MaxProgressSteps, full.ProgressSteps);
        }

        [Test]
        public void ACatchAloneOpensSilhouetteAndCatchDataButNotTheName()
        {
            var (_, species) = World();
            species.AcceptCatch(new CaptureResult("cap-1", "dive-1", "sea_bass", 900, 1), ShallowSpot, 1);
            var e = EncyclopediaPresenter.Build(species.Snapshot())[0];
            Assert.IsTrue(e.Silhouette);
            Assert.IsTrue(e.CatchData);
            Assert.IsFalse(e.NameKnown);
        }

        [Test]
        public void AnUnknownSpeciesIdFallsBackToItsIdOnceNamed()
        {
            var (_, species) = World();
            species.AcceptRecording("rec-x", "mystery_eel", ShallowSpot, 1);
            var e = EncyclopediaPresenter.Build(species.Snapshot())[0];
            Assert.AreEqual("mystery_eel", e.DisplayName);
            Assert.AreEqual("Bilinmeyen tur", e.Category);
        }

        // ---- save ---------------------------------------------------------------------------------

        [Test]
        public void ExportCarriesDiscoveredCellsAndObservationsOnly()
        {
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            species.AcceptSighting("sea_bass", ShallowSpot, 2);
            var data = new ExplorationPersistenceAdapter(cells, species).ExportExploration();

            Assert.AreEqual(ExplorationIds.NearRegionId, data.RegionId);
            Assert.AreEqual(1, data.DiscoveredCells.Count, "undiscovered cells are not written");
            Assert.AreEqual(DepthBandIds.Shallow, data.DiscoveredCells[0].DepthBandId);
            Assert.AreEqual(1, data.Observations.Count);
            Assert.AreEqual(SpeciesObservationAuthority.SightingId("sea_bass"), data.Observations[0].ObservationId);
            Assert.AreEqual(2, data.Observations[0].DayNumber);
        }

        [Test]
        public void ObservationsComeBackThroughTheAuthorityAndAReplayCountsNothingTwice()
        {
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            species.AcceptSighting("sea_bass", ShallowSpot, 1);
            species.AcceptCatch(new CaptureResult("cap-1", "dive-1", "sea_bass", 900, 1), ShallowSpot, 1);
            var saved = JsonUtility.FromJson<ExplorationSaveData>(JsonUtility.ToJson(
                new ExplorationPersistenceAdapter(cells, species).ExportExploration()));

            var (freshCells, freshSpecies) = World();
            var adapter = new ExplorationPersistenceAdapter(freshCells, freshSpecies);
            Assert.IsTrue(adapter.RestoreExploration(saved));
            Assert.IsTrue(adapter.RestoreExploration(saved), "restoring twice is harmless");

            Assert.AreEqual(2, freshSpecies.Observations.Count);
            Assert.IsTrue(freshSpecies.TryGetSpecies("sea_bass", out var state));
            Assert.IsTrue(state.Sighted && state.Caught && !state.Recorded);
            Assert.AreEqual(SpeciesObservationOutcome.AlreadyCounted,
                freshSpecies.AcceptCatch(new CaptureResult("cap-1", "dive-1", "sea_bass", 900, 1), ShallowSpot, 1),
                "the same catch after a reload is not a second observation");
        }

        [Test]
        public void SavedCellsAreNeverLostEvenBeforeTheAuthorityCanRestoreThem()
        {
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            Visit(cells, OtherSpot);
            var saved = new ExplorationPersistenceAdapter(cells, species).ExportExploration();

            var (freshCells, freshSpecies) = World();
            var adapter = new ExplorationPersistenceAdapter(freshCells, freshSpecies);
            adapter.RestoreExploration(saved);
            Assert.AreEqual(2, adapter.UnrestoredCells, "reported, not faked into the authority");

            Visit(freshCells, ShallowSpot);   // one of them is really visited again
            var again = adapter.ExportExploration();
            Assert.AreEqual(2, again.DiscoveredCells.Count, "the other saved cell is carried forward, not dropped");
            Assert.AreEqual(1, adapter.UnrestoredCells);
        }

        [Test]
        public void TamperedOrForeignSavesAreRefusedOrFiltered()
        {
            var (cells, species) = World();
            var adapter = new ExplorationPersistenceAdapter(cells, species);
            Assert.IsFalse(adapter.RestoreExploration(new ExplorationSaveData { RegionId = "region-far-9" }));

            var data = new ExplorationSaveData { RegionId = ExplorationIds.NearRegionId };
            data.DiscoveredCells.Add(new ExplorationCellSave { CellId = "cell-bogus", GridX = 0, GridZ = 0 });
            data.DiscoveredCells.Add(new ExplorationCellSave { CellId = ExplorationIds.CellId(ExplorationIds.NearRegionId, 99, 99), GridX = 99, GridZ = 99 });
            data.DiscoveredCells.Add(new ExplorationCellSave { CellId = ExplorationIds.CellId(ExplorationIds.NearRegionId, 0, 0), GridX = 0, GridZ = 0, DepthBandId = " " });
            data.Observations.Add(new SpeciesObservationSave { ObservationId = "x", SpeciesId = "sea_bass", RegionId = "region-far-9", CellId = "c" });
            Assert.IsTrue(adapter.RestoreExploration(data));
            Assert.AreEqual(0, adapter.UnrestoredCells, "id/coordinate mismatch, out of grid and a whitespace band are dropped");
            Assert.AreEqual(0, species.Observations.Count, "the authority refuses a foreign-region observation");
        }

        [Test]
        public void ExplorationRidesInTheSameCampaignFileAndSurvivesALateBind()
        {
            var path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-explore", Guid.NewGuid().ToString("N") + ".json");
            var roots = new List<GameObject>();
            try
            {
                var (cells, species) = World();
                Visit(cells, ShallowSpot);
                species.AcceptSighting("sea_bass", ShallowSpot, 1);

                var store = Boot(path, roots, out var economy);
                store.Exploration = new ExplorationPersistenceAdapter(cells, species);
                economy.TryRestore(new EconomySaveData { SharedBalance = 40 });
                Assert.IsTrue(store.SaveNow(), store.LastError);

                var onDisk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
                Assert.AreEqual(EconomySaveData.CurrentSchemaVersion, onDisk.SchemaVersion);
                Assert.IsTrue(onDisk.HasExploration);
                Assert.AreEqual(40, onDisk.SharedBalance, "money and exploration in one write");

                // A fresh boot: the store reads the file first, the exploration host binds later.
                var store2 = Boot(path, roots, out _);
                var (c2, s2) = World();
                store2.Exploration = new ExplorationPersistenceAdapter(c2, s2);
                Assert.AreEqual(1, s2.Observations.Count);

                // A boot where exploration never binds must still carry it forward on the next save.
                var store3 = Boot(path, roots, out _);
                Assert.IsTrue(store3.SaveNow(), store3.LastError);
                Assert.AreEqual(1, JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path)).Exploration.Observations.Count);
            }
            finally
            {
                foreach (var r in roots) UnityEngine.Object.DestroyImmediate(r);
                foreach (var suffix in new[] { "", ".bak", ".tmp" })
                    if (File.Exists(path + suffix)) File.Delete(path + suffix);
            }
        }

        [Test]
        public void V3FilesLoadWithNoExploration()
        {
            var path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-explore", Guid.NewGuid().ToString("N") + ".json");
            var roots = new List<GameObject>();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new EconomySaveData { SchemaVersion = 3, SharedBalance = 7 }));
                var store = Boot(path, roots, out var economy);
                var (c, s) = World();
                store.Exploration = new ExplorationPersistenceAdapter(c, s);
                Assert.AreEqual(7, economy.SharedBalance);
                Assert.AreEqual(0, s.Observations.Count);
            }
            finally
            {
                foreach (var r in roots) UnityEngine.Object.DestroyImmediate(r);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static EconomySaveStore Boot(string path, List<GameObject> roots, out EconomyManager economy)
        {
            var root = new GameObject("explore-save");
            roots.Add(root);
            root.AddComponent<InventoryManager>();
            economy = root.AddComponent<EconomyManager>();
            var store = root.AddComponent<EconomySaveStore>();
            store.SetPathForTests(path);
            store.LoadNow();
            return store;
        }

        // ---- client mirror ------------------------------------------------------------------------

        [Test]
        public void TheMirrorWireRebuildsTheSameSnapshotAClientWouldRender()
        {
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            species.AcceptRecording("rec-1", "sea_bass", ShallowSpot, 1);
            var host = species.Snapshot();

            Assert.IsTrue(ExplorationMirror.TryDecode(ExplorationMirror.Encode(host), out var client));
            Assert.AreEqual(host.Revision, client.Revision);
            Assert.AreEqual(host.Grid.Columns, client.Grid.Columns);
            Assert.AreEqual(host.Grid.MinGridX, client.Grid.MinGridX);
            Assert.AreEqual(ExplorationMapPresenter.DiscoveredCount(ExplorationMapPresenter.BuildFog(host)),
                ExplorationMapPresenter.DiscoveredCount(ExplorationMapPresenter.BuildFog(client)));
            var a = EncyclopediaPresenter.Build(host)[0];
            var b = EncyclopediaPresenter.Build(client)[0];
            Assert.AreEqual(a.DisplayName, b.DisplayName);
            CollectionAssert.AreEqual(a.HabitatBandIds, b.HabitatBandIds);
        }

        [Test]
        public void TheMirrorWireCarriesNoPositions()
        {
            var (cells, species) = World();
            Visit(cells, ShallowSpot);
            species.AcceptSighting("sea_bass", ShallowSpot, 1);
            var json = System.Text.Encoding.UTF8.GetString(ExplorationMirror.Encode(species.Snapshot()));
            StringAssert.DoesNotContain("\"x\"", json);
            StringAssert.DoesNotContain("Position", json);
            StringAssert.DoesNotContain(ShallowSpot.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ",", json);
        }

        [Test]
        public void GarbageOnTheWireIsRejected()
        {
            Assert.IsFalse(ExplorationMirror.TryDecode(System.Text.Encoding.UTF8.GetBytes("not json"), out _));
        }
    }
}
