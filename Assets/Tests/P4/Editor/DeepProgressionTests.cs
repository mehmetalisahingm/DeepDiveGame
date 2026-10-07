using System;
using System.Collections.Generic;
using System.IO;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Inventory;
using DeepDive.MapUI;
using DeepDive.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // P4.4-C (#123): the deep progression chain on the REAL authority, the real campaign save store and its file. The world validator
    // is a test double only because Utku's (#122) does not exist yet; it is the one seam a real world rule plugs into.
    public sealed class DeepProgressionTests
    {
        private static readonly PlayerId Host = new PlayerId(0), Guest = new PlayerId(1);

        private sealed class FakeWorld : IDeepProgressionWorld
        {
            public bool IsValidTrace(string traceId, string cellId, string depthBandId) =>
                traceId.StartsWith("trace-") && depthBandId == DepthBandIds.Deep && cellId.Length > 0;

            public bool IsDiscoveryArea(string arenaId, string cellId, string depthBandId) =>
                arenaId == "arena-1" && depthBandId == DepthBandIds.Deep && cellId.Length > 0;
        }

        private readonly FakeWorld world = new FakeWorld();
        private DeepProgressionAuthority authority;
        private ulong request = 1;
        private string path;

        [SetUp]
        public void Setup()
        {
            authority = new DeepProgressionAuthority();
            DeepProgressionWorld.Bind(world);
            path = Path.Combine(Path.GetTempPath(), "DeepDive-P4-progression", Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void Cleanup()
        {
            DeepProgressionWorld.Unbind(world);
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }

        private static ExplorationSaveData Exploration(bool observation, params (string cell, string band)[] cells)
        {
            var data = new ExplorationSaveData { RegionId = ExplorationIds.NearRegionId };
            if (observation)
                data.Observations.Add(new SpeciesObservationSave { ObservationId = "obs-1", SpeciesId = "sea_bass", CellId = "c0", DepthBandId = DepthBandIds.Shallow });
            foreach (var c in cells) data.DiscoveredCells.Add(new ExplorationCellSave { CellId = c.cell, DepthBandId = c.band });
            return data;
        }

        private void ReachRumor()
        {
            Assert.IsTrue(authority.EvaluateExploration(Exploration(true, ("r0", DepthBandIds.Reef))));
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
        }

        private void ReachTrace()
        {
            ReachRumor();
            for (var i = 1; i <= DeepProgressionIds.RequiredTraceCount; i++)
                Assert.IsTrue(authority.TrySubmitTrace(Host, "trace-" + i, "d" + i, DepthBandIds.Deep, request++).Accepted);
            Assert.AreEqual(DeepProgressionStage.Trace, authority.State.Stage);
        }

        private void ReachDiscovery()
        {
            ReachTrace();
            Assert.IsTrue(authority.TrySubmitDiscovery(Guest, "arena-1", "a0", DepthBandIds.Deep, request++).Accepted);
        }

        // ---- defaults and order ----------------------------------------------------------------------------------

        [Test]
        public void TheDefaultIsClosedAndNothingElseOpensTheBoss()
        {
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);
            Assert.IsFalse(authority.State.BossUnlocked);
            Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, authority.State.TracesRequired);
            Assert.AreEqual(DeepProgressionStage.Locked, BossProgression.State.Stage, "unbound read seam = Locked");
            Assert.IsFalse(BossProgression.IsAvailable(DeepProgressionIds.BossId));
            Assert.IsFalse(BossProgression.IsAvailable("boss-other"));
        }

        [Test]
        public void TheChainCannotSkipAStep()
        {
            Assert.AreEqual("OutOfOrder", authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).ReasonCode, "no trace without a rumor");
            Assert.AreEqual("OutOfOrder", authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, request++).ReasonCode, "no discovery without traces");
            Assert.AreEqual("OutOfOrder", authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-1", request++).ReasonCode, "no completion without the unlock");

            authority.EvaluateExploration(Exploration(true));   // encyclopedia only
            Assert.AreEqual(DeepProgressionStage.Encyclopedia, authority.State.Stage);
            Assert.AreEqual("OutOfOrder", authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).ReasonCode, "a rumor is still missing");

            ReachRumor();
            Assert.AreEqual("OutOfOrder", authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, request++).ReasonCode, "traces are still missing");
            Assert.IsFalse(authority.State.BossUnlocked);
        }

        // ---- derived stages: encyclopedia and rumor ---------------------------------------------------------------

        [Test]
        public void EncyclopediaNeedsARealCountedObservationAndRumorNeedsAReefCell()
        {
            Assert.IsFalse(authority.EvaluateExploration(Exploration(false)));
            Assert.IsFalse(authority.EvaluateExploration(null));
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);

            var junk = Exploration(false);
            junk.Observations.Add(new SpeciesObservationSave { ObservationId = "", SpeciesId = "sea_bass" });
            junk.Observations.Add(new SpeciesObservationSave { ObservationId = "obs-x", SpeciesId = "" });
            Assert.IsFalse(authority.EvaluateExploration(junk), "an observation without ids is not evidence");

            Assert.IsTrue(authority.EvaluateExploration(Exploration(true, ("s0", DepthBandIds.Shallow))));
            Assert.AreEqual(DeepProgressionStage.Encyclopedia, authority.State.Stage, "a shallow cell is no rumor");

            Assert.IsTrue(authority.EvaluateExploration(Exploration(true, ("s0", DepthBandIds.Shallow), ("r0", DepthBandIds.Reef))));
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
        }

        [Test]
        public void BothDerivedStagesCanAdvanceInOneEvaluationButNeverTwiceAndAReplayIsANoOp()
        {
            var evidence = Exploration(true, ("r0", DepthBandIds.Reef));
            var revision = 0;
            Assert.IsTrue(authority.EvaluateExploration(evidence));
            revision = authority.State.Revision;
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);

            Assert.IsFalse(authority.EvaluateExploration(evidence), "the same evidence again produces nothing");
            Assert.IsFalse(authority.EvaluateExploration(Exploration(true, ("r1", DepthBandIds.Reef))));
            Assert.AreEqual(revision, authority.State.Revision);
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
        }

        // ---- host-fed stages: trace and discovery ------------------------------------------------------------------

        [Test]
        public void WithoutAWorldValidatorNothingIsAccepted()
        {
            ReachRumor();
            DeepProgressionWorld.Unbind(world);
            var result = authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++);
            Assert.AreEqual("WorldUnavailable", result.ReasonCode, "fail-closed");
            Assert.AreEqual(0, authority.State.TracesFound);
        }

        [Test]
        public void TheWorldValidatorDecidesWhichContextCounts()
        {
            ReachRumor();
            Assert.AreEqual("InvalidContext", authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Shallow, request++).ReasonCode, "wrong depth");
            Assert.AreEqual("InvalidContext", authority.TrySubmitTrace(Host, "trace-1", "", DepthBandIds.Deep, request++).ReasonCode, "no cell");
            Assert.AreEqual("InvalidContext", authority.TrySubmitTrace(Host, "not-a-trace", "d1", DepthBandIds.Deep, request++).ReasonCode);
            Assert.AreEqual("InvalidTarget", authority.TrySubmitTrace(Host, "", "d1", DepthBandIds.Deep, request++).ReasonCode);
            Assert.AreEqual(0, authority.State.TracesFound, "refusals change nothing");
            Assert.IsTrue(authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).Accepted);
            Assert.AreEqual(1, authority.State.TracesFound);
        }

        [Test]
        public void ATraceCountsOnceWhoeverReportsItAndTheStageOpensOnTheLastRequiredOne()
        {
            ReachRumor();
            var id = request++;
            var first = authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, id);
            var replay = authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, id);
            Assert.IsTrue(first.Accepted);
            Assert.AreEqual(first.Revision, replay.Revision, "a replayed request answers from the cache");
            Assert.AreEqual("AlreadyProcessed", authority.TrySubmitTrace(Guest, "trace-1", "d9", DepthBandIds.Deep, request++).ReasonCode, "the same trace from another player");
            Assert.AreEqual(1, authority.State.TracesFound);
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);

            for (var i = 2; i <= DeepProgressionIds.RequiredTraceCount; i++)
                authority.TrySubmitTrace(Guest, "trace-" + i, "d" + i, DepthBandIds.Deep, request++);
            Assert.AreEqual(DeepProgressionStage.Trace, authority.State.Stage);
            Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, authority.State.TracesFound);
            Assert.AreEqual("AlreadyProcessed", authority.TrySubmitTrace(Host, "trace-extra", "dx", DepthBandIds.Deep, request++).ReasonCode, "all traces are in");
            Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, authority.State.TracesFound);
        }

        [Test]
        public void DiscoveryUnlocksTheBossOnceAndOnlyFromTheRealArena()
        {
            ReachTrace();
            Assert.AreEqual("InvalidContext", authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Reef, request++).ReasonCode);
            Assert.AreEqual("InvalidContext", authority.TrySubmitDiscovery(Host, "arena-9", "a0", DepthBandIds.Deep, request++).ReasonCode);
            Assert.IsFalse(authority.State.BossUnlocked);

            var id = request++;
            Assert.IsTrue(authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, id).Accepted);
            Assert.IsTrue(authority.State.BossUnlocked);
            Assert.AreEqual(DeepProgressionStage.Discovery, authority.State.Stage);
            Assert.IsTrue(authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, id).Accepted, "replay of the accepted request");
            Assert.AreEqual("AlreadyProcessed", authority.TrySubmitDiscovery(Guest, "arena-1", "a0", DepthBandIds.Deep, request++).ReasonCode);
        }

        [Test]
        public void ABossIsCompletedOnceAndOnlyAfterTheUnlock()
        {
            ReachDiscovery();
            Assert.AreEqual("InvalidTarget", authority.TryCompleteBoss("boss-other", "enc-1", request++).ReasonCode);
            Assert.AreEqual("InvalidTarget", authority.TryCompleteBoss(DeepProgressionIds.BossId, "", request++).ReasonCode);
            Assert.IsFalse(authority.State.IsCompleted(DeepProgressionIds.BossId));

            var id = request++;
            Assert.IsTrue(authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-1", id).Accepted);
            Assert.IsTrue(authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-1", id).Accepted, "replay answers from the cache");
            Assert.AreEqual("AlreadyProcessed", authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-2", request++).ReasonCode, "a second encounter id is no second completion");
            Assert.AreEqual(1, authority.State.CompletedBossIds.Count);
            Assert.IsTrue(authority.State.IsCompleted(DeepProgressionIds.BossId));
        }

        // ---- the read seam and the host feed ------------------------------------------------------------------------

        [Test]
        public void TheReadSeamOffersTheBossOnlyAfterTheCompletedDiscoveryAndTheFeedRoutesToTheAuthority()
        {
            Func<DeepProgressionState> read = () => authority.State;
            Func<PlayerId, string, string, string, ulong, TransactionResult> trace = authority.TrySubmitTrace;
            Func<PlayerId, string, string, string, ulong, TransactionResult> discovery = authority.TrySubmitDiscovery;
            Func<string, string, ulong, TransactionResult> complete = authority.TryCompleteBoss;
            try
            {
                Assert.AreEqual("InvalidState", DeepProgressionEvidence.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).ReasonCode, "unbound feed rejects");
                BossProgression.Bind(read);
                DeepProgressionEvidence.Bind(trace, discovery, complete);
                Assert.IsFalse(BossProgression.IsAvailable(DeepProgressionIds.BossId));

                ReachRumor();
                for (var i = 1; i <= DeepProgressionIds.RequiredTraceCount; i++)
                    Assert.IsTrue(DeepProgressionEvidence.TrySubmitTrace(Host, "trace-" + i, "d" + i, DepthBandIds.Deep, request++).Accepted);
                Assert.IsFalse(BossProgression.IsAvailable(DeepProgressionIds.BossId), "traces alone do not unlock it");
                Assert.IsTrue(DeepProgressionEvidence.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, request++).Accepted);
                Assert.IsTrue(BossProgression.IsAvailable(DeepProgressionIds.BossId));
                Assert.IsFalse(BossProgression.IsAvailable("boss-other"));
                Assert.IsFalse(BossProgression.IsCompleted(DeepProgressionIds.BossId));
                Assert.IsTrue(DeepProgressionEvidence.TryCompleteBoss(DeepProgressionIds.BossId, "enc-1", request++).Accepted);
                Assert.IsTrue(BossProgression.IsCompleted(DeepProgressionIds.BossId));
            }
            finally
            {
                BossProgression.Unbind(read);
                DeepProgressionEvidence.Unbind(trace, discovery, complete);
            }
            Assert.IsFalse(BossProgression.IsAvailable(DeepProgressionIds.BossId), "unbound again = closed");
        }

        // ---- failed writes -----------------------------------------------------------------------------------------

        [Test]
        public void AFailedWriteRollsAStepBackAndTheSameRequestCanBeRetried()
        {
            var works = true;
            authority = new DeepProgressionAuthority(() => works);
            ReachRumor();
            works = false;
            var id = request++;
            Assert.AreEqual("SaveFailed", authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, id).ReasonCode);
            Assert.AreEqual(0, authority.State.TracesFound);
            var revision = authority.State.Revision;

            works = true;
            Assert.IsTrue(authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, id).Accepted, "SaveFailed is not cached against the request");
            Assert.AreEqual(1, authority.State.TracesFound);
            Assert.Greater(authority.State.Revision, revision);
        }

        [Test]
        public void AFailedWriteLeavesTheDerivedStagesUnacknowledgedAndTheNextEvaluationRetries()
        {
            var works = false;
            authority = new DeepProgressionAuthority(() => works);
            Assert.IsFalse(authority.EvaluateExploration(Exploration(true, ("r0", DepthBandIds.Reef))));
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);
            works = true;
            Assert.IsTrue(authority.EvaluateExploration(Exploration(true, ("r0", DepthBandIds.Reef))));
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
        }

        // ---- save / load through the real store and file ----------------------------------------------------------

        private sealed class Rig : IDisposable
        {
            public readonly GameObject Root;
            public readonly EconomySaveStore Store;
            public readonly DeepProgressionAuthority Authority;

            public Rig(string path)
            {
                Root = new GameObject("progression-rig");
                Root.AddComponent<InventoryManager>();
                Root.AddComponent<EconomyManager>();
                Store = Root.AddComponent<EconomySaveStore>();
                Store.SetPathForTests(path);
                Assert.IsTrue(Store.LoadNow());
                Authority = new DeepProgressionAuthority(Store.SaveNow);
                Store.Progression = Authority;
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }

        [Test]
        public void ProgressionSurvivesASaveAndReopenWithoutDuplicatesAndTheBossStaysUnlockedAndCompleted()
        {
            string diskJson;
            using (var first = new Rig(path))
            {
                authority = first.Authority;
                ReachDiscovery();
                Assert.IsTrue(authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-1", request++).Accepted);
                diskJson = File.ReadAllText(path);
                var disk = JsonUtility.FromJson<EconomySaveData>(diskJson);
                Assert.AreEqual(EconomySaveData.CurrentSchemaVersion, disk.SchemaVersion);
                Assert.IsTrue(disk.HasProgression);
                Assert.AreEqual((byte)DeepProgressionStage.Discovery, disk.Progression.Stage);
                Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, disk.Progression.TraceIds.Count);
                CollectionAssert.AreEqual(new[] { DeepProgressionIds.BossId }, disk.Progression.CompletedBossIds);
                StringAssert.DoesNotContain("ActiveBoss", diskJson, "the transient encounter state is never persisted");
                StringAssert.DoesNotContain("Encounter", diskJson, "the transient encounter state is never persisted");
            }

            using (var reopened = new Rig(path))
            {
                var state = reopened.Authority.State;
                Assert.AreEqual(DeepProgressionStage.Discovery, state.Stage);
                Assert.IsTrue(state.BossUnlocked);
                Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, state.TracesFound);
                Assert.AreEqual(1, state.CompletedBossIds.Count);

                // Replaying the same evidence after the reopen produces nothing new.
                Assert.IsFalse(reopened.Authority.EvaluateExploration(Exploration(true, ("r0", DepthBandIds.Reef))));
                Assert.AreEqual("AlreadyProcessed", reopened.Authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).ReasonCode);
                Assert.AreEqual("AlreadyProcessed", reopened.Authority.TrySubmitDiscovery(Host, "arena-1", "a0", DepthBandIds.Deep, request++).ReasonCode);
                Assert.AreEqual("AlreadyProcessed", reopened.Authority.TryCompleteBoss(DeepProgressionIds.BossId, "enc-9", request++).ReasonCode);
                Assert.AreEqual(1, reopened.Authority.State.CompletedBossIds.Count);
                Assert.AreEqual(DeepProgressionIds.RequiredTraceCount, reopened.Authority.State.TracesFound);
            }
        }

        [Test]
        public void AMidChainStageAlsoComesBackExactly()
        {
            using (var first = new Rig(path))
            {
                authority = first.Authority;
                ReachRumor();
                Assert.IsTrue(authority.TrySubmitTrace(Host, "trace-1", "d1", DepthBandIds.Deep, request++).Accepted);
            }
            using (var reopened = new Rig(path))
            {
                Assert.AreEqual(DeepProgressionStage.Rumor, reopened.Authority.State.Stage);
                Assert.AreEqual(1, reopened.Authority.State.TracesFound);
                Assert.IsFalse(reopened.Authority.State.BossUnlocked);
            }
        }

        [Test]
        public void AnOlderCampaignFileWithoutProgressionOpensClosed()
        {
            // A v6 file: written before progression existed. Nothing in it mentions the chain.
            var legacy = new EconomySaveData { SchemaVersion = 6, SharedBalance = 321 };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(legacy));
            using (var rig = new Rig(path))
            {
                Assert.AreEqual(DeepProgressionStage.Locked, rig.Authority.State.Stage);
                Assert.AreEqual(0, rig.Authority.State.TracesFound);
                Assert.IsFalse(rig.Authority.State.BossUnlocked);
                Assert.AreEqual(321, rig.Root.GetComponent<EconomyManager>().SharedBalance, "the rest of the old file still loads");
            }
        }

        // ---- tampering: fail-closed -------------------------------------------------------------------------------

        private static DeepProgressionSaveData Saved(byte stage, string enc = "enc:o", string rumor = "rumor:r", int traces = 3, string discovery = "discovery:arena-1", params string[] completed)
        {
            var data = new DeepProgressionSaveData { Stage = stage, EncyclopediaEvidenceId = enc, RumorEvidenceId = rumor, DiscoveryEvidenceId = discovery };
            for (var i = 1; i <= traces; i++) data.TraceIds.Add("trace-" + i);
            data.CompletedBossIds.AddRange(completed);
            return data;
        }

        [Test]
        public void AValidFullRecordRestoresAsIs()
        {
            Assert.IsTrue(authority.RestoreProgression(Saved(4, completed: DeepProgressionIds.BossId)));
            Assert.AreEqual(DeepProgressionStage.Discovery, authority.State.Stage);
            Assert.IsTrue(authority.State.IsCompleted(DeepProgressionIds.BossId));
        }

        [Test]
        public void ATamperedStageWithoutItsEvidenceFallsBackToTheHighestBackedStageAndNeverUnlocksTheBoss()
        {
            // stage says Discovery but nothing backs it
            authority.RestoreProgression(new DeepProgressionSaveData { Stage = 4 });
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);
            Assert.IsFalse(authority.State.BossUnlocked);

            // encyclopedia and rumor backed, only two traces, discovery id forged
            authority.RestoreProgression(Saved(4, traces: DeepProgressionIds.RequiredTraceCount - 1));
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
            Assert.IsFalse(authority.State.BossUnlocked);

            // every trace but no discovery evidence
            authority.RestoreProgression(Saved(4, discovery: ""));
            Assert.AreEqual(DeepProgressionStage.Trace, authority.State.Stage);
            Assert.IsFalse(authority.State.BossUnlocked);

            // a rumor without the encyclopedia step behind it
            authority.RestoreProgression(Saved(2, enc: ""));
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);
        }

        [Test]
        public void TamperedDetailsAreCleanedNotTrusted()
        {
            // an absurd stage byte is clamped to the last stage, which still needs its evidence
            authority.RestoreProgression(Saved(200));
            Assert.AreEqual(DeepProgressionStage.Discovery, authority.State.Stage);

            // duplicate / blank trace ids count once; too few distinct ones cannot reach the Trace stage
            var data = Saved(4, traces: 0);
            data.TraceIds.AddRange(new[] { "trace-1", "trace-1", " ", "", "trace-2" });
            authority.RestoreProgression(data);
            Assert.AreEqual(DeepProgressionStage.Rumor, authority.State.Stage);
            Assert.AreEqual(2, authority.State.TracesFound);

            // a completion with no unlock behind it is dropped
            authority.RestoreProgression(Saved(2, completed: DeepProgressionIds.BossId));
            Assert.AreEqual(0, authority.State.CompletedBossIds.Count);

            // an unknown boss id is dropped even on a fully unlocked record
            authority.RestoreProgression(Saved(4, completed: "boss-forged"));
            Assert.AreEqual(DeepProgressionStage.Discovery, authority.State.Stage);
            Assert.AreEqual(0, authority.State.CompletedBossIds.Count);
        }

        [Test]
        public void ARecordFromAnUnknownVersionOrNoRecordAtAllIsTheClosedDefault()
        {
            authority.RestoreProgression(Saved(4));
            Assert.IsTrue(authority.State.BossUnlocked);

            var future = Saved(4);
            future.Version = DeepProgressionIds.CurrentSaveVersion + 5;
            Assert.IsTrue(authority.RestoreProgression(future));
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage, "a version this build does not understand grants nothing");

            authority.RestoreProgression(Saved(4));
            Assert.IsTrue(authority.RestoreProgression(null));
            Assert.AreEqual(DeepProgressionStage.Locked, authority.State.Stage);
        }

        // ---- the words the player sees -----------------------------------------------------------------------------

        [Test]
        public void ThePlayerTextNamesTheNextActionNeverAPlace()
        {
            var lines = new List<string>();
            foreach (var stage in new[] { DeepProgressionStage.Locked, DeepProgressionStage.Encyclopedia, DeepProgressionStage.Rumor, DeepProgressionStage.Trace, DeepProgressionStage.Discovery })
            {
                var line = DeepProgressionPresenter.Line(new DeepProgressionState(stage, 1, DeepProgressionIds.RequiredTraceCount, null, 0));
                Assert.IsNotEmpty(line);
                StringAssert.DoesNotContain("arena", line);
                StringAssert.DoesNotContain("trace-", line);
                StringAssert.DoesNotContain("boss-", line);
                lines.Add(line);
            }
            CollectionAssert.AllItemsAreUnique(lines);
            StringAssert.Contains("1/" + DeepProgressionIds.RequiredTraceCount, lines[2], "the rumor stage shows the trace count");
            StringAssert.Contains("tamamlandi", DeepProgressionPresenter.Line(new DeepProgressionState(DeepProgressionStage.Discovery, 3, 3, new[] { DeepProgressionIds.BossId }, 0)));
        }
    }
}
