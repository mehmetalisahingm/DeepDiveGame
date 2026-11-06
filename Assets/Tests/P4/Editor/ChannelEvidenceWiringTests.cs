using System;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Media;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #255 x #254: the host-verified specimen travels with the clip (manifest, clip save) and the day-close payout reads it. Only a
    // film-and-catch of the same animal earns more, once per animal; everything else (film only, no proof, old clips) is today's rule.
    // Rights and money are the REAL EconomyManager, the save the REAL EconomySaveStore on a file (same rig as ChannelHistoryTests).
    public sealed class ChannelEvidenceWiringTests
    {
        private static readonly PlayerId Host = new PlayerId(0);

        private static ClipManifest Clip(string id, RecordingSpecimenProof specimen, int quality = 3, string subject = "grouper", int day = 1) =>
            new ClipManifest(id, "rec-" + id, "dive-1", day, Host, subject, quality, 12f, "h-" + id, 1000, true, true, default, specimen);

        private sealed class Rig : ChannelAuthority.IRights
        {
            public GameObject Root;
            public EconomyManager Economy;
            public EconomySaveStore Store;
            public ChannelAuthority Channel = new ChannelAuthority();
            public int Day = 1;

            public bool TryClaimForChannel(string recordingId) => Economy.TryClaimRecordingForChannel(recordingId);
            public void ReleaseChannelClaim(string recordingId) => Economy.ReleaseChannelClaim(recordingId);
            public bool CreditChannelIncome(string settleId, int amount) => Economy.CreditChannelIncome(settleId, amount);
        }

        private readonly System.Collections.Generic.List<GameObject> roots = new System.Collections.Generic.List<GameObject>();
        private string path;

        [SetUp]
        public void Setup() => path = Path.Combine(Path.GetTempPath(), "DeepDive-P255-evidence", Guid.NewGuid().ToString("N") + ".json");

        [TearDown]
        public void Cleanup()
        {
            foreach (var r in roots) UnityEngine.Object.DestroyImmediate(r);
            roots.Clear();
            DayLock.Bind(null);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private static (int Min, int Max) GrouperRange(string species) => species == "grouper" ? (2000, 6000) : (0, 0);

        private Rig Boot(Func<string, (int Min, int Max)> range = null, string file = null)
        {
            var rig = new Rig { Root = new GameObject("evidence255") };
            roots.Add(rig.Root);
            rig.Root.AddComponent<InventoryManager>();
            rig.Economy = rig.Root.AddComponent<EconomyManager>();
            rig.Store = rig.Root.AddComponent<EconomySaveStore>();
            rig.Store.SetPathForTests(file ?? path);
            rig.Store.LoadNow();
            rig.Channel.Configure(rig, () => rig.Day, () => false, () => rig.Store.SaveNow(), null, range);
            rig.Store.Media = rig.Channel;
            return rig;
        }

        // A channel with its own empty campaign file: the compared cases must not inherit each other's followers.
        private Rig Fresh(Func<string, (int Min, int Max)> range = null) =>
            Boot(range, Path.Combine(Path.GetTempPath(), "DeepDive-P255-evidence", Guid.NewGuid().ToString("N") + ".json"));

        // Submit, publish and settle one clip on its own day; returns the publication.
        private static PublicationSave Run(Rig rig, ClipManifest clip, ulong request)
        {
            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(clip));
            rig.Day++;
            Assert.IsTrue(rig.Channel.TryPublish(Host, clip.ClipId, "", request).Accepted);
            rig.Channel.SettleThrough(rig.Day);
            return rig.Channel.Publications().Single(p => p.ClipId == clip.ClipId);
        }

        [Test]
        public void TheSpecimenProofSurvivesTheClipSaveAndOldSavesLoadAsNone()
        {
            var manifest = Clip("c1", RecordingSpecimenProof.FilmedAndCaught("sp-1", 4100, "cap-1"));
            var save = ClipSave.From(manifest);
            Assert.AreEqual((byte)SpecimenProofKind.FilmAndCatch, save.SpecimenKind);
            var back = JsonUtility.FromJson<ClipSave>(JsonUtility.ToJson(save)).ToManifest();
            Assert.AreEqual(SpecimenProofKind.FilmAndCatch, back.Specimen.Kind);
            Assert.AreEqual(("sp-1", 4100, "cap-1"), (back.Specimen.SpecimenId, back.Specimen.WeightGrams, back.Specimen.CaptureId));

            var filmOnly = JsonUtility.FromJson<ClipSave>(JsonUtility.ToJson(ClipSave.From(Clip("c2", RecordingSpecimenProof.Filmed("sp-2", 900))))).ToManifest();
            Assert.AreEqual(SpecimenProofKind.FilmOnly, filmOnly.Specimen.Kind);

            // A save written before #255: none of the specimen fields exist in the JSON.
            var old = JsonUtility.FromJson<ClipSave>("{\"ClipId\":\"old\",\"RecordingId\":\"rec-old\",\"DiveId\":\"d\",\"DayNumber\":1,\"OwnerPlayerId\":0,\"SubjectId\":\"sea_bass\",\"Quality\":2,\"DurationSeconds\":9,\"ContentHash\":\"h\",\"SizeBytes\":5,\"MediaReady\":true,\"SafeReturned\":true}").ToManifest();
            Assert.AreEqual(SpecimenProofKind.None, old.Specimen.Kind);
            Assert.AreEqual(string.Empty, old.Specimen.SpecimenId);

            // A half record never becomes half a proof.
            var half = new ClipSave { ClipId = "h", RecordingId = "r", DurationSeconds = 1, SpecimenKind = (byte)SpecimenProofKind.FilmAndCatch, SpecimenWeightGrams = 500 };
            Assert.AreEqual(SpecimenProofKind.None, half.ToManifest().Specimen.Kind);
        }

        [Test]
        public void TheManifestBuilderCarriesTheProofFromTheRecordingResult()
        {
            var proof = RecordingSpecimenProof.FilmedAndCaught("sp-7", 5200, "cap-7");
            var result = new RecordingResult("rec-7", "dive-1", Host, "grouper", 3, 9f, proof);
            Assert.IsTrue(RecordingClipFile.TryBuildManifest(result, 2, 9f, new string('a', 64), 4096, out var manifest));
            Assert.AreEqual(SpecimenProofKind.FilmAndCatch, manifest.Specimen.Kind);
            Assert.AreEqual("sp-7", manifest.Specimen.SpecimenId);
            Assert.AreEqual(5200, manifest.Specimen.WeightGrams);

            var plain = new RecordingResult("rec-8", "dive-1", Host, "grouper", 3, 9f);
            Assert.IsTrue(RecordingClipFile.TryBuildManifest(plain, 2, 9f, new string('b', 64), 4096, out var none));
            Assert.AreEqual(SpecimenProofKind.None, none.Specimen.Kind);
        }

        [Test]
        public void AFilmAndCatchOfTheSameAnimalEarnsMoreViewsAndTheResultRecordsWhy()
        {
            var big = Run(Fresh(GrouperRange), Clip("big", RecordingSpecimenProof.FilmedAndCaught("sp-big", 6000, "cap-big")), 1);
            var small = Run(Fresh(GrouperRange), Clip("small", RecordingSpecimenProof.FilmedAndCaught("sp-small", 2000, "cap-small")), 1);
            var filmOnly = Run(Fresh(GrouperRange), Clip("film", RecordingSpecimenProof.Filmed("sp-film", 6000)), 1);
            var noProof = Run(Fresh(GrouperRange), Clip("none", default), 1);

            Assert.AreEqual(187, big.EvidencePercent, "the biggest possible animal: 150% size x 125% match");
            Assert.AreEqual(125, small.EvidencePercent, "the smallest: only the match bonus");
            Assert.AreEqual(100, filmOnly.EvidencePercent, "a big animal filmed but not brought home is not a big catch");
            Assert.AreEqual(100, noProof.EvidencePercent);
            Assert.AreEqual("sp-big", big.SpecimenId);

            ChannelResultRules.Evaluate(3, true, 0, out var plainViews, out _, out _);
            Assert.AreEqual(plainViews, filmOnly.Views, "film only = today's rule");
            Assert.AreEqual(plainViews, noProof.Views);
            Assert.Greater(big.Views, small.Views);
            Assert.Greater(small.Views, plainViews);
            ChannelResultRules.Evaluate(3, true, 0, 187, out var expectedBig, out var expectedFollowers, out var expectedIncome);
            Assert.AreEqual((expectedBig, expectedFollowers, expectedIncome), (big.Views, big.FollowersGained, big.Income));
        }

        [Test]
        public void WithoutAKnownWeightRangeOnlyTheMatchBonusApplies()
        {
            var publication = Run(Boot(), Clip("c", RecordingSpecimenProof.FilmedAndCaught("sp", 6000, "cap")), 1);
            Assert.AreEqual(125, publication.EvidencePercent);
        }

        [Test]
        public void OneAnimalEarnsTheBonusOnceEvenFromAnotherClipAndAfterAReload()
        {
            var rig = Boot(GrouperRange);
            var first = Run(rig, Clip("a", RecordingSpecimenProof.FilmedAndCaught("sp-1", 5000, "cap-1")), 1);
            var again = Run(rig, Clip("b", RecordingSpecimenProof.FilmedAndCaught("sp-1", 5000, "cap-1"), day: 2), 2);
            var other = Run(rig, Clip("c", RecordingSpecimenProof.FilmedAndCaught("sp-2", 5000, "cap-2"), day: 3), 3);
            Assert.Greater(first.EvidencePercent, 100);
            Assert.AreEqual(100, again.EvidencePercent, "the same animal again: no second bonus");
            Assert.Greater(other.EvidencePercent, 100, "another animal still earns its own");

            Assert.IsTrue(rig.Store.SaveNow());
            var second = Boot(GrouperRange);   // the same campaign file, a fresh host
            var afterReload = Run(second, Clip("d", RecordingSpecimenProof.FilmedAndCaught("sp-1", 5000, "cap-1"), day: 4), 4);
            Assert.AreEqual(100, afterReload.EvidencePercent, "the paid bonus is remembered across a reload");
            var kept = second.Channel.Publications().Single(p => p.ClipId == "a");
            Assert.AreEqual((first.EvidencePercent, first.SpecimenId, first.Views), (kept.EvidencePercent, kept.SpecimenId, kept.Views));
        }

        [Test]
        public void ASettlementRunTwiceNeverPaysTheEvidenceTwice()
        {
            var rig = Boot(GrouperRange);
            var publication = Run(rig, Clip("a", RecordingSpecimenProof.FilmedAndCaught("sp-1", 6000, "cap-1")), 1);
            var balance = rig.Economy.SharedBalance;
            var followers = rig.Channel.Followers;
            Assert.AreEqual(0, rig.Channel.SettleThrough(rig.Day + 5));
            Assert.AreEqual(balance, rig.Economy.SharedBalance);
            Assert.AreEqual(followers, rig.Channel.Followers);
            Assert.AreEqual(publication.Views, rig.Channel.Publications().Single().Views);
        }
    }
}
