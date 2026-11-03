using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.MapUI;
using DeepDive.World;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #218: the five species id's recording -> encyclopedia -> visual end of the chain, through the real observation authorities (no
    // fake read model). The sale end is P411SpeciesSaleTests (P3 assembly, it needs the session); the real harpoon, pickup and swimming
    // are the integrated -p2-hunt smoke's job.
    public sealed class P411SpeciesChainTests
    {
        private static readonly string[] Ids = { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
        private static readonly string[] Names = { "Levrek", "Ton baligi", "Kirmizi mercan", "Orfoz", "Fener baligi" };
        private static readonly DiveRegionBounds Arena = new DiveRegionBounds(-15f, 15f, -15f, 15f);
        private static readonly WaterBody[] Sea = { new WaterBody(-15f, 15f, -15f, 15f, 8f) };

        private static CaptureResult Capture(int index) =>
            new CaptureResult("cap-" + Ids[index], "dive-1", Ids[index], 700 + index * 100, catchObjectId: (ulong)(index + 1));

        [Test]
        public void TheEncyclopediaNamesAndFilesEverySpeciesAfterSightingRecordingAndCatch()
        {
            var cells = new ExplorationCellAuthority(ExplorationIds.NearRegionId, Arena, Sea);
            var species = new SpeciesObservationAuthority(cells);
            var spots = new[]
            {
                new Vector3(2f, 5f, 2f), new Vector3(-2f, 5f, 12f), new Vector3(-12f, -4f, -12f),
                new Vector3(12f, -4f, -2f), new Vector3(12f, -18f, 12f)
            };
            var bands = new[] { DepthBandIds.Shallow, DepthBandIds.Shallow, DepthBandIds.Reef, DepthBandIds.Reef, DepthBandIds.Deep };
            var feed = new Explorers();
            for (var i = 0; i < Ids.Length; i++) feed.Now.Add(new ExplorerPosition(new PlayerId((ulong)i), spots[i]));
            cells.Tick(feed);
            for (var i = 0; i < Ids.Length; i++)
            {
                species.AcceptSighting(Ids[i], spots[i], 1);
                species.AcceptRecording("rec-" + Ids[i], Ids[i], spots[i], 1);
                species.AcceptCatch(Capture(i), spots[i], 1);
            }

            var entries = EncyclopediaPresenter.Build(species.Snapshot()).ToDictionary(e => e.SpeciesId);
            Assert.AreEqual(5, entries.Count);
            for (var i = 0; i < Ids.Length; i++)
            {
                var e = entries[Ids[i]];
                Assert.AreEqual(Names[i], e.DisplayName, Ids[i]);
                Assert.AreNotEqual("Bilinmeyen tur", e.Category, Ids[i] + " must have a category of its own");
                Assert.IsTrue(e.NameKnown && e.CatchData && e.HabitatKnown, Ids[i]);
                Assert.AreEqual(EncyclopediaEntry.MaxProgressSteps, e.ProgressSteps, Ids[i]);
                CollectionAssert.AreEqual(new[] { bands[i] }, e.HabitatBandIds, Ids[i] + " habitat comes from where it was seen");
            }
            Assert.AreEqual(5, entries.Values.Select(e => e.DisplayName).Distinct().Count(), "five different names");
        }

        [Test]
        public void EverySpeciesHasItsOwnHuntAndDeliveryVisual()
        {
            var paths = Ids.Select(PlayerActionView.FishResource).ToArray();
            CollectionAssert.AllItemsAreNotNull(paths);
            Assert.AreEqual(5, paths.Distinct().Count(), "no two species share a visual");
            foreach (var path in paths) Assert.NotNull(Resources.Load<GameObject>(path), path);
        }

        private sealed class Explorers : IExplorerPositionSource
        {
            public readonly List<ExplorerPosition> Now = new List<ExplorerPosition>();
            public void CollectPositions(List<ExplorerPosition> into) { into.Clear(); into.AddRange(Now); }
        }
    }
}
