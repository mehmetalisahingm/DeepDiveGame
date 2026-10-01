using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Day;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.Media;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #102 archive + channel. The rights and money side is the REAL EconomyManager; the save side the REAL
    // EconomySaveStore on a real file; the day close the REAL DayEngine.
    public sealed class ChannelTests
    {
        private static readonly PlayerId Host = new PlayerId(0), Guest = new PlayerId(1);

        private static ClipManifest Clip(string id, PlayerId owner, string recordingId = null, int quality = 3,
            bool ready = true, bool safe = true, string subject = "sea_bass", int day = 1) =>
            new ClipManifest(id, recordingId ?? "rec-" + id, "dive-1", day, owner, subject, quality, 12f, "h-" + id, 1000, ready, safe);

        private sealed class Rig : ChannelAuthority.IRights
        {
            public GameObject Root;
            public EconomyManager Economy;
            public EconomySaveStore Store;
            public ChannelAuthority Channel = new ChannelAuthority();
            public bool Locked;
            public int Day = 1;
            public bool WriteOk = true;
            public readonly List<string> Queued = new List<string>();

            public bool TryClaimForChannel(string recordingId) => Economy.TryClaimRecordingForChannel(recordingId);
            public void ReleaseChannelClaim(string recordingId) => Economy.ReleaseChannelClaim(recordingId);
            public bool CreditChannelIncome(string settleId, int amount) => Economy.CreditChannelIncome(settleId, amount);
        }

        private readonly List<GameObject> roots = new List<GameObject>();
        private string path;

        [SetUp]
        public void Setup() => path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-media", Guid.NewGuid().ToString("N") + ".json");

        [TearDown]
        public void Cleanup()
        {
            foreach (var r in roots) UnityEngine.Object.DestroyImmediate(r);
            roots.Clear();
            DayLock.Bind(null);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private Rig Boot()
        {
            var rig = new Rig { Root = new GameObject("media") };
            roots.Add(rig.Root);
            rig.Root.AddComponent<InventoryManager>();
            rig.Economy = rig.Root.AddComponent<EconomyManager>();
            rig.Store = rig.Root.AddComponent<EconomySaveStore>();
            rig.Store.SetPathForTests(path);
            rig.Store.LoadNow();
            rig.Channel.Configure(rig, () => rig.Day, () => rig.Locked, () => rig.WriteOk && rig.Store.SaveNow(), rig.Queued.Add);
            rig.Store.Media = rig.Channel;
            return rig;
        }

        // ---- archive ------------------------------------------------------------------------------

        [Test]
        public void TheArchiveTakesAClipOnceAndRefusesMalformedOnes()
        {
            var rig = Boot();
            Assert.AreEqual(ClipArchiveOutcome.Added, rig.Channel.Submit(Clip("c1", Host)));
            Assert.AreEqual(ClipArchiveOutcome.AlreadyArchived, rig.Channel.Submit(Clip("c1", Host)), "a capture retry is not a second clip");
            Assert.AreEqual(ClipArchiveOutcome.Invalid, rig.Channel.Submit(new ClipManifest("", "r", "d", 1, Host, "s", 1, 1, "", 0, true, true)));
            Assert.AreEqual(ClipArchiveOutcome.Invalid, rig.Channel.Submit(new ClipManifest("x", "r", "d", 1, Host, "s", 1, 0f, "", 0, true, true)));
            Assert.AreEqual(1, rig.Channel.ClipCount);
        }

        [Test]
        public void TheArchiveHasACeiling()
        {
            var rig = Boot();
            for (var i = 0; i < ChannelAuthority.MaxArchivedClips; i++) rig.Channel.Submit(Clip("c" + i, Host));
            Assert.AreEqual(ClipArchiveOutcome.ArchiveFull, rig.Channel.Submit(Clip("over", Host)));
        }

        // ---- publish ------------------------------------------------------------------------------

        [Test]
        public void TheOwnerPublishesAValidClipOnceAndARepeatCreatesNothing()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("c1", Host));
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "Ilk levrek", 1).Accepted);
            Assert.AreEqual("PublicationAlreadyQueued", rig.Channel.TryPublish(Host, "c1", "baska baslik", 2).ReasonCode,
                "renaming the metadata is not a new right");
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "Ilk levrek", 1).Accepted, "the same request replays its first answer");
            Assert.AreEqual(1, rig.Channel.Publications().Count);
            Assert.AreEqual(ChannelIds.PublicationId("c1"), rig.Channel.Publications()[0].PublicationId);
            Assert.AreEqual("Ilk levrek", rig.Channel.Publications()[0].Title);
            CollectionAssert.AreEqual(new[] { ChannelIds.PublicationId("c1") }, rig.Queued, "the day ledger hears about it once");
        }

        [Test]
        public void SomeoneElsesOrAnInvalidClipCannotBePublished()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("mine", Host));
            rig.Channel.Submit(Clip("empty", Host, quality: 0));
            rig.Channel.Submit(Clip("lost", Host, safe: false));
            rig.Channel.Submit(Clip("half", Host, ready: false));
            rig.Channel.Submit(Clip("noframe", Host, subject: ""));

            Assert.AreEqual("NotOwner", rig.Channel.TryPublish(Guest, "mine", "", 1).ReasonCode);
            Assert.AreEqual("InvalidTarget", rig.Channel.TryPublish(Host, "nope", "", 2).ReasonCode);
            Assert.AreEqual("NotPublishable", rig.Channel.TryPublish(Host, "empty", "", 3).ReasonCode);
            Assert.AreEqual("NotPublishable", rig.Channel.TryPublish(Host, "lost", "", 4).ReasonCode, "not safely returned (D07)");
            Assert.AreEqual("MediaNotReady", rig.Channel.TryPublish(Host, "half", "", 5).ReasonCode);
            Assert.AreEqual("NotPublishable", rig.Channel.TryPublish(Host, "noframe", "", 6).ReasonCode);
            Assert.AreEqual(0, rig.Channel.Publications().Count);
        }

        [Test]
        public void ClosingTheDayRefusesPublishingWithoutCachingTheRefusal()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("c1", Host));
            rig.Locked = true;
            Assert.AreEqual("DayClosing", rig.Channel.TryPublish(Host, "c1", "", 7).ReasonCode);
            rig.Locked = false;
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "", 7).Accepted);
        }

        [Test]
        public void AFailedWriteLeavesNoPublicationAndGivesTheRightBack()
        {
            var rig = Boot();
            rig.Economy.TryQueueRecordingTurnIn(new RecordingResult("rec-c1", "dive-1", Host, "sea_bass", 3, 12f));
            rig.Channel.Submit(Clip("c1", Host));
            rig.WriteOk = false;

            Assert.AreEqual("SaveFailed", rig.Channel.TryPublish(Host, "c1", "", 8).ReasonCode);
            Assert.AreEqual(0, rig.Channel.Publications().Count);
            Assert.IsFalse(rig.Economy.IsChannelClaimed("rec-c1"));
            Assert.AreEqual(1, rig.Economy.PendingCountFor(Host, TurnInKind.Recording), "the NPC candidate is back");

            rig.WriteOk = true;
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "", 8).Accepted, "the same request may be retried");
        }

        // ---- one commercial right (CONTRACTS: NPC or channel, never both) --------------------------

        [Test]
        public void ARecordingPaidAtTheNpcCannotBePublished()
        {
            var rig = Boot();
            rig.Economy.TryQueueRecordingTurnIn(new RecordingResult("rec-c1", "dive-1", Host, "sea_bass", 3, 12f));
            Assert.IsTrue(rig.Economy.TryTurnInRecordings(Host, 1).Accepted);
            rig.Channel.Submit(Clip("c1", Host));
            Assert.AreEqual("RightsConsumed", rig.Channel.TryPublish(Host, "c1", "", 2).ReasonCode);
        }

        [Test]
        public void APublishedRecordingIsWithdrawnFromTheNpcAndCannotBeQueuedAgain()
        {
            var rig = Boot();
            rig.Economy.TryQueueRecordingTurnIn(new RecordingResult("rec-c1", "dive-1", Host, "sea_bass", 3, 12f));
            rig.Channel.Submit(Clip("c1", Host));
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "", 1).Accepted);

            Assert.AreEqual(0, rig.Economy.PendingCountFor(Host, TurnInKind.Recording));
            Assert.AreEqual("NothingToTurnIn", rig.Economy.TryTurnInRecordings(Host, 2).ReasonCode);
            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                rig.Economy.TryQueueRecordingTurnIn(new RecordingResult("rec-c1", "dive-1", Host, "sea_bass", 3, 12f)));
            Assert.AreEqual(0, rig.Economy.SharedBalance);
        }

        // ---- results at the day close ------------------------------------------------------------

        [Test]
        public void ResultsArePaidOnceWhenTheQueuedDayClosesAndNeverAgain()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("c1", Host));
            rig.Channel.TryPublish(Host, "c1", "", 1);
            Assert.AreEqual(0, rig.Channel.SettleThrough(0), "day 0 is before it was queued");

            Assert.AreEqual(1, rig.Channel.SettleThrough(1));
            var p = rig.Channel.Publications()[0];
            ChannelResultRules.Evaluate(3, true, 0, out var views, out var followers, out var income);
            Assert.AreEqual(views, p.Views);
            Assert.AreEqual(followers, p.FollowersGained);
            Assert.AreEqual(income, rig.Economy.SharedBalance);
            Assert.AreEqual(2, p.ResultDay, "the result belongs to the next morning");
            Assert.AreEqual(followers, rig.Channel.Followers);

            Assert.AreEqual(0, rig.Channel.SettleThrough(1), "a retry of the close pays nothing");
            Assert.AreEqual(0, rig.Channel.SettleThrough(5));
            Assert.AreEqual(income, rig.Economy.SharedBalance);
        }

        [Test]
        public void ASecondVideoOfTheSameSubjectIsLessNovel()
        {
            ChannelResultRules.Evaluate(3, true, 0, out var first, out _, out _);
            ChannelResultRules.Evaluate(3, false, 0, out var second, out _, out _);
            Assert.Greater(first, second);
            ChannelResultRules.Evaluate(0, true, 0, out var none, out _, out var noIncome);
            Assert.AreEqual(0, none);
            Assert.AreEqual(0, noIncome);
        }

        [Test]
        public void TheRealDayCloseSettlesInsideItsOwnWriteAndARetryDoesNotPayTwice()
        {
            var rig = Boot();
            rig.Channel.Submit(Clip("c1", Host));
            var engine = new DayEngine();
            var hooks = new CloseHooks { Rig = rig };
            engine.Configure(new OneActive(), hooks);
            rig.Store.Day = engine;
            rig.Channel.Configure(rig, () => engine.DayNumber, () => engine.IsLocked, () => rig.Store.SaveNow(),
                id => engine.RecordPublicationQueued(id));
            Assert.IsTrue(rig.Channel.TryPublish(Host, "c1", "", 1).Accepted);

            hooks.WriteOk = false;                      // the close's write fails once
            engine.BeginClose(DayCloseReason.Midnight);
            var paidOnce = rig.Economy.SharedBalance;
            Assert.Greater(paidOnce, 0);
            Assert.AreEqual(1, engine.DayNumber);
            hooks.WriteOk = true;
            Assert.IsTrue(engine.RetryClose());

            Assert.AreEqual(paidOnce, rig.Economy.SharedBalance, "the retry wrote the same payout, it did not pay again");
            Assert.AreEqual(1, engine.History[0].PublicationsQueued);

            var disk = JsonUtility.FromJson<EconomySaveData>(File.ReadAllText(path));
            Assert.AreEqual(2, disk.Day.DayNumber);
            Assert.AreEqual(paidOnce, disk.SharedBalance);
            Assert.IsFalse(string.IsNullOrEmpty(disk.Media.Publications[0].SettledId), "payout and settle id in the same file");
        }

        private sealed class OneActive : IDayRoster
        {
            public IReadOnlyList<PlayerId> ActivePlayers() => new[] { Host };
        }

        private sealed class CloseHooks : IDayCloseHooks
        {
            public Rig Rig;
            public bool WriteOk = true;
            public void SettleAcceptedActions(string closeId) { }
            public DayDiveClosure CloseOpenDives(string closeId) => DayDiveClosure.None;
            public bool Persist() => WriteOk && Rig.Store.SaveNow();
            public void BeginMorning(int dayNumber) { }
            public void SettleNextDayResults(string closeId, int closingDayNumber) => Rig.Channel.SettleThrough(closingDayNumber);
        }

        // ---- save ---------------------------------------------------------------------------------

        [Test]
        public void ArchivePublicationsRightsAndFollowersComeBackFromTheFileWithoutDuplicates()
        {
            var first = Boot();
            first.Channel.Submit(Clip("c1", Host));
            first.Channel.Submit(Clip("c2", Guest));
            first.Channel.TryPublish(Host, "c1", "Levrek", 1);
            first.Channel.SettleThrough(1);
            Assert.IsTrue(first.Store.SaveNow(), first.Store.LastError);
            var balance = first.Economy.SharedBalance;

            var second = Boot();
            Assert.AreEqual(2, second.Channel.ClipCount);
            Assert.AreEqual(1, second.Channel.Publications().Count);
            Assert.AreEqual(first.Channel.Followers, second.Channel.Followers);
            Assert.AreEqual(balance, second.Economy.SharedBalance);
            Assert.IsTrue(second.Economy.IsChannelClaimed("rec-c1"));

            Assert.AreEqual("PublicationAlreadyQueued", second.Channel.TryPublish(Host, "c1", "", 99).ReasonCode, "no duplicate after load");
            Assert.AreEqual(0, second.Channel.SettleThrough(9), "no second payout after load");
            Assert.AreEqual(balance, second.Economy.SharedBalance);
            Assert.AreEqual(ClipArchiveOutcome.AlreadyArchived, second.Channel.Submit(Clip("c1", Host)));
        }

        [Test]
        public void ATamperedMediaSaveCannotInventOrDoublePublications()
        {
            var rig = Boot();
            var data = new MediaSaveData();
            data.Clips.Add(ClipSave.From(Clip("c1", Host)));
            data.Publications.Add(new PublicationSave { ClipId = "c1", Title = "a" });
            data.Publications.Add(new PublicationSave { ClipId = "c1", Title = "b" });
            data.Publications.Add(new PublicationSave { ClipId = "ghost", Title = "c" });
            Assert.IsTrue(rig.Channel.RestoreMedia(data));
            Assert.AreEqual(1, rig.Channel.Publications().Count);
            Assert.AreEqual(ChannelIds.PublicationId("c1"), rig.Channel.Publications()[0].PublicationId);
        }

        [Test]
        public void OlderSavesLoadWithAnEmptyArchive()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new EconomySaveData { SchemaVersion = 4, SharedBalance = 3 }));
            var rig = Boot();
            Assert.AreEqual(3, rig.Economy.SharedBalance);
            Assert.AreEqual(0, rig.Channel.ClipCount);
        }

        // ---- seams --------------------------------------------------------------------------------

        [Test]
        public void TheArchiveSeamRefusesWhenUnboundAndRoutesWhenBound()
        {
            var rig = Boot();
            Assert.AreEqual(ClipArchiveOutcome.Invalid, ClipArchive.TrySubmit(Clip("c1", Host)));
            Func<ClipManifest, ClipArchiveOutcome> submit = rig.Channel.Submit;
            ClipArchive.Bind(submit);
            Assert.AreEqual(ClipArchiveOutcome.Added, ClipArchive.TrySubmit(Clip("c1", Host)));
            ClipArchive.Unbind(submit);
            Assert.IsFalse(ClipPlayback.CanPlay("c1"), "no player bound: the PC must not pretend to play");
        }
    }
}
