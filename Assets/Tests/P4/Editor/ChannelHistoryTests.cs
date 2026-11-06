using System;
using System.Collections.Generic;
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
    // #255: the media archive is bounded but never blocks the channel; a settled publication keeps its record for good when its clip's media
    // is evicted; reload, replay protection and old saves stay safe. The rights and money side is the REAL EconomyManager, the save side the
    // REAL EconomySaveStore on a real file (same rig as ChannelTests).
    public sealed class ChannelHistoryTests
    {
        private static readonly PlayerId Host = new PlayerId(0);
        private static readonly string[] Subjects = { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };

        private static ClipManifest Clip(string id, int quality = 3, string subject = "sea_bass", int day = 1) =>
            new ClipManifest(id, "rec-" + id, "dive-1", day, Host, subject, quality, 12f, "h-" + id, 1000, true, true);

        private sealed class Rig : ChannelAuthority.IRights
        {
            public GameObject Root;
            public EconomyManager Economy;
            public EconomySaveStore Store;
            public ChannelAuthority Channel = new ChannelAuthority();
            public int Day = 1;
            public readonly List<string> Evicted = new List<string>();

            public bool TryClaimForChannel(string recordingId) => Economy.TryClaimRecordingForChannel(recordingId);
            public void ReleaseChannelClaim(string recordingId) => Economy.ReleaseChannelClaim(recordingId);
            public bool CreditChannelIncome(string settleId, int amount) => Economy.CreditChannelIncome(settleId, amount);
        }

        private readonly List<GameObject> roots = new List<GameObject>();
        private string path;

        [SetUp]
        public void Setup() => path = Path.Combine(Path.GetTempPath(), "DeepDive-P255-media", Guid.NewGuid().ToString("N") + ".json");

        [TearDown]
        public void Cleanup()
        {
            foreach (var r in roots) UnityEngine.Object.DestroyImmediate(r);
            roots.Clear();
            DayLock.Bind(null);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private Rig Boot(string file = null)
        {
            var rig = new Rig { Root = new GameObject("media255") };
            roots.Add(rig.Root);
            rig.Root.AddComponent<InventoryManager>();
            rig.Economy = rig.Root.AddComponent<EconomyManager>();
            rig.Store = rig.Root.AddComponent<EconomySaveStore>();
            rig.Store.SetPathForTests(file ?? path);
            rig.Store.LoadNow();
            rig.Channel.Configure(rig, () => rig.Day, () => false, () => rig.Store.SaveNow());
            rig.Store.Media = rig.Channel;
            rig.Channel.OnClipEvicted += rig.Evicted.Add;
            return rig;
        }

        // A full archive whose oldest clip (c0) is published and settled.
        private Rig FullArchiveWithOneSettledClip()
        {
            var rig = Boot();
            for (var i = 0; i < ChannelAuthority.MaxArchivedClips; i++) Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("c" + i)));
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c0", "Ilk", 1).Accepted);
            Assert.AreEqual(1, rig.Channel.SettleThrough(1));
            return rig;
        }

        [Test]
        public void AFullArchiveEvictsTheOldestSettledClipAndKeepsItsPublicationRecord()
        {
            var rig = FullArchiveWithOneSettledClip();
            var before = rig.Channel.Publications().Single();
            var followersBefore = rig.Channel.Followers;
            var balanceBefore = rig.Economy.SharedBalance;
            Assert.Greater(before.Views, 0);

            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("new")), "a full archive no longer refuses while a settled clip can make room");
            Assert.AreEqual(new[] { "c0" }, rig.Evicted.ToArray(), "the host is told to delete exactly that media file");
            Assert.IsFalse(rig.Channel.TryGetClip("c0", out _));
            Assert.AreEqual(ChannelAuthority.MaxArchivedClips, rig.Channel.ClipCount);

            var kept = rig.Channel.Publications().Single();
            Assert.AreEqual(before.PublicationId, kept.PublicationId);
            Assert.AreEqual((before.Views, before.FollowersGained, before.Income, before.SettledId), (kept.Views, kept.FollowersGained, kept.Income, kept.SettledId));
            Assert.AreEqual(followersBefore, rig.Channel.Followers, "eviction never touches the channel's numbers");
            Assert.AreEqual(balanceBefore, rig.Economy.SharedBalance);
        }

        [Test]
        public void AClipThatCouldStillBePublishedOrSettledIsNeverEvicted()
        {
            var rig = Boot();
            for (var i = 0; i < ChannelAuthority.MaxArchivedClips; i++) rig.Channel.Submit(Clip("c" + i));
            Assert.AreEqual(ClipArchiveOutcome.ArchiveFull, rig.Channel.Submit(Clip("over")), "all publishable and unpublished: nothing may be dropped");
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c0", "Ilk", 1).Accepted);
            Assert.AreEqual(ClipArchiveOutcome.ArchiveFull, rig.Channel.Submit(Clip("over2")), "published but not yet settled: still protected");
            Assert.IsEmpty(rig.Evicted);
            Assert.IsTrue(rig.Channel.TryGetClip("c0", out _));
        }

        [Test]
        public void ANeverPublishableClipMakesRoomWhenNothingHasSettled()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("junk", quality: 0));   // not a commercial candidate
            for (var i = 1; i < ChannelAuthority.MaxArchivedClips; i++) rig.Channel.Submit(Clip("c" + i));
            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("new")));
            Assert.AreEqual(new[] { "junk" }, rig.Evicted.ToArray());
        }

        [Test]
        public void AnEvictedClipCanNeverBeArchivedOrPublishedAgain()
        {
            var rig = FullArchiveWithOneSettledClip();
            rig.Channel.Submit(Clip("new"));
            var followers = rig.Channel.Followers;
            var balance = rig.Economy.SharedBalance;

            Assert.AreEqual(ClipArchiveOutcome.AlreadyArchived, rig.Channel.Submit(Clip("c0")), "a retry of the old capture cannot bring it back");
            Assert.IsFalse(rig.Channel.TryPublish(Host, "c0", "again", 2).Accepted);
            Assert.IsTrue(rig.Channel.IsPublished("c0"), "the record still says it was published");
            Assert.AreEqual(0, rig.Channel.SettleThrough(50), "nothing settles twice");
            Assert.AreEqual(followers, rig.Channel.Followers);
            Assert.AreEqual(balance, rig.Economy.SharedBalance);
            Assert.AreEqual(1, rig.Channel.Publications().Count);
        }

        [Test]
        public void SettledHistoryWithoutMediaSurvivesSaveAndReload()
        {
            var rig = FullArchiveWithOneSettledClip();
            rig.Channel.Submit(Clip("new"));
            var publication = rig.Channel.Publications().Single();
            var followers = rig.Channel.Followers;
            var balance = rig.Economy.SharedBalance;
            Assert.IsTrue(rig.Store.SaveNow());

            var second = Boot();   // same file: a fresh host loading the campaign
            Assert.AreEqual(followers, second.Channel.Followers);
            Assert.AreEqual(balance, second.Economy.SharedBalance);
            Assert.AreEqual(ChannelAuthority.MaxArchivedClips, second.Channel.ClipCount);
            var reloaded = second.Channel.Publications().Single();
            Assert.AreEqual((publication.PublicationId, publication.Views, publication.FollowersGained, publication.Income, publication.SettledId),
                (reloaded.PublicationId, reloaded.Views, reloaded.FollowersGained, reloaded.Income, reloaded.SettledId));
            Assert.IsFalse(second.Channel.TryGetClip("c0", out _));
            Assert.AreEqual(ClipArchiveOutcome.AlreadyArchived, second.Channel.Submit(Clip("c0")));
            Assert.AreEqual(0, second.Channel.SettleThrough(99), "no second payout after the reload");
            Assert.AreEqual(balance, second.Economy.SharedBalance);
        }

        [Test]
        public void AMadeUpSettledRecordWithoutItsClipIsStillDropped()
        {
            var rig = Boot();
            var data = new MediaSaveData { Followers = 5 };
            data.Publications.Add(new PublicationSave { ClipId = "ghost", Title = "x", Views = 999999, Income = 99999, SettledId = "settle-not-the-real-one" });
            data.Publications.Add(new PublicationSave { ClipId = "ghost2", Title = "y", Views = 10, SettledId = ChannelIds.SettleId(ChannelIds.PublicationId("ghost2")) });
            Assert.IsTrue(rig.Channel.RestoreMedia(data));
            Assert.AreEqual(new[] { ChannelIds.PublicationId("ghost2") }, rig.Channel.Publications().Select(p => p.PublicationId).ToArray(),
                "only a record carrying the id this clip's settlement would have is accepted as history");
            Assert.AreEqual(5, rig.Channel.Followers);
        }

        [Test]
        public void ASavedCampaignWrittenBeforeEvictionLoadsExactlyAsBefore()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("c1"));
            rig.Channel.TryPublish(Host, "c1", "Ilk", 1);
            rig.Channel.SettleThrough(1);
            var data = rig.Channel.ExportMedia();   // every clip present, as every save before #255

            var second = Boot(Path.Combine(Path.GetTempPath(), "DeepDive-P255-media", Guid.NewGuid().ToString("N") + ".json"));
            Assert.IsTrue(second.Channel.RestoreMedia(data));
            Assert.AreEqual(1, second.Channel.ClipCount);
            Assert.AreEqual(rig.Channel.Followers, second.Channel.Followers);
            var a = rig.Channel.Publications().Single();
            var b = second.Channel.Publications().Single();
            Assert.AreEqual((a.PublicationId, a.Views, a.FollowersGained, a.Income, a.SettledId, a.ResultDay), (b.PublicationId, b.Views, b.FollowersGained, b.Income, b.SettledId, b.ResultDay));
        }

        [Test]
        public void ClientsGetTheClipsAndRecentHistoryButTheSaveKeepsAllOfIt()
        {
            var rig = Boot();
            var day = 0;
            for (var i = 0; i < 40; i++)
            {
                rig.Channel.Submit(Clip("h" + i, subject: Subjects[i % 5]));
                rig.Day = ++day;
                Assert.IsTrue(rig.Channel.TryPublish(Host, "h" + i, "t" + i, (ulong)(i + 1)).Accepted);
                rig.Channel.SettleThrough(day);
            }
            // Force eviction of the early clips: fill the archive past its cap with fresh clips.
            for (var i = 0; i < ChannelAuthority.MaxArchivedClips; i++) rig.Channel.Submit(Clip("f" + i, subject: Subjects[i % 5]));

            var save = rig.Channel.ExportMedia();
            var mirror = rig.Channel.ExportMirror(12);
            Assert.AreEqual(40, save.Publications.Count, "the save keeps every publication");
            Assert.AreEqual(save.Followers, mirror.Followers, "the follower count is exact");
            var withMedia = mirror.Publications.Count(p => mirror.Clips.Any(c => c.ClipId == p.ClipId));
            Assert.LessOrEqual(mirror.Publications.Count, withMedia + 12, "only the 12 most recent media-less records are sent");
            Assert.AreEqual("t39", mirror.Publications.Last().Title, "order is kept, newest last");
        }

        // The host deletes an evicted clip's media only after the save that no longer lists it succeeded: until then the previous save still
        // names the clip, so a failed write or a crash must leave the file in place.
        [Test]
        public void AnEvictedClipsFileIsDeletedOnlyAfterASuccessfulSave()
        {
            var rig = FullArchiveWithOneSettledClip();
            var pending = new PendingMediaDeletes();
            var deleted = new List<string>();
            rig.Channel.OnClipEvicted += pending.Add;

            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("new")));
            Assert.AreEqual(1, pending.Count, "the eviction only queues the file");
            Assert.IsEmpty(deleted, "nothing is deleted before a save succeeded");

            // The save failed: the host does not flush, the file stays queued (and on disk).
            Assert.AreEqual(1, pending.Count);
            Assert.IsEmpty(deleted);

            // The next save succeeded: now the file goes, exactly once.
            Assert.AreEqual(1, pending.Flush(deleted.Add));
            Assert.AreEqual(new[] { "c0" }, deleted.ToArray());
            Assert.AreEqual(0, pending.Flush(deleted.Add), "a second flush deletes nothing");
            Assert.AreEqual(0, pending.Count);
        }

        [Test]
        public void ADeleteThatFailsStaysQueuedAndAClipIsQueuedOnce()
        {
            var pending = new PendingMediaDeletes();
            pending.Add("a");
            pending.Add("a");
            pending.Add("b");
            pending.Add("");
            Assert.AreEqual(2, pending.Count);
            var done = pending.Flush(id => { if (id == "a") throw new IOException("locked"); });
            Assert.AreEqual(1, done);
            Assert.AreEqual(1, pending.Count, "the file that could not be deleted is retried at the next save");
        }

        // The goal through the REAL authority, economy and save: a lively crew (three publishable clips a day, quality 3, every subject new
        // once) with the archive evicting settled clips as it fills, reaches 10,000,000 followers, is never refused a publication, keeps its
        // income on the price scale, and keeps playing after the goal. Day count is a property of these inputs, not a promise.
        [Test]
        public void TheGoalIsReachedThroughTheRealChannelWithoutTheArchiveEverBlockingIt()
        {
            var rig = Boot();
            ulong request = 0;
            var clip = 0;
            var reachedDay = 0;
            var refused = 0;
            for (var day = 1; day <= 120 && reachedDay == 0; day++)
            {
                rig.Day = day;
                for (var k = 0; k < 3; k++, clip++)
                {
                    if (rig.Channel.Submit(Clip("g" + clip, 3, Subjects[clip % 5], day)) != ClipArchiveOutcome.Added) refused++;
                    if (!rig.Channel.TryPublish(Host, "g" + clip, "", ++request).Accepted) refused++;
                }
                rig.Channel.SettleThrough(day);
                if (rig.Channel.Followers >= ChannelGoal.TargetFollowers) reachedDay = day;
            }

            TestContext.Out.WriteLine($"P411_CHANNEL_REAL day={reachedDay} publications={rig.Channel.Publications().Count} clips={rig.Channel.ClipCount} evicted={rig.Evicted.Count} balance={rig.Economy.SharedBalance}");
            Assert.AreEqual(0, refused, "neither the archive nor the publication path ever refused a clip");
            Assert.Greater(reachedDay, 0, "the goal is reached");
            Assert.IsTrue(ChannelGoal.Of(rig.Channel.Followers).Reached);
            Assert.Greater(rig.Evicted.Count, 0, "the archive did fill and evict settled media on the way");
            Assert.LessOrEqual(rig.Channel.ClipCount, ChannelAuthority.MaxArchivedClips);
            Assert.Less(rig.Economy.SharedBalance, 1000000, "income stayed on the game's price scale");

            // Reaching the goal does not end anything: the channel keeps taking clips and growing.
            var atGoal = rig.Channel.Followers;
            rig.Day = reachedDay + 1;
            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("after", 3, "tuna", reachedDay + 1)));
            Assert.IsTrue(rig.Channel.TryPublish(Host, "after", "", ++request).Accepted);
            rig.Channel.SettleThrough(reachedDay + 1);
            Assert.Greater(rig.Channel.Followers, atGoal);
        }
    }
}
