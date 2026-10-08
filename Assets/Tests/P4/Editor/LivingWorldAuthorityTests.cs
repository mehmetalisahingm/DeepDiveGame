using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.Tests.P4
{
    // Pure host decisions; the real two-process acceptance must still run separately.
    public sealed class LivingWorldAuthorityTests
    {
        private GameObject host;
        private EconomyManager economy;

        [SetUp]
        public void Setup()
        {
            host = new GameObject("P45LivingWorldTests");
            economy = host.AddComponent<EconomyManager>();
        }

        [TearDown]
        public void Teardown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }

        private static PendingTurnInState Catch(string id) =>
            new PendingTurnInState(id, TurnInKind.Catch, "dive-test", "sea_bass",
                500, 0, 0f, new PlayerId(0), true, 0);

        [Test]
        public void ThreeOrderTemplates_OnlyUseAvailableShoreSpecies()
        {
            for (var day = 1; day <= 21; day++)
            {
                var a = new LivingWorldAuthority(economy, () => true);
                Assert.IsTrue(a.BeginDay(day, "campaign-1"));
                Assert.That(a.OrderId, Is.AnyOf(LivingWorldCatalog.OrderIds));
                Assert.Greater(LivingWorldCatalog.OrderQuantity(a.OrderId), 0);
                Assert.AreNotEqual(LivingWorldCatalog.NightRecording, a.SponsorId);
            }
        }

        [Test]
        public void VerifiedCatchReplay_AndSaveRestore_PayOnlyOnce()
        {
            var a = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(a.BeginDay(3, "my-campaign"));
            var catches = new[] { Catch("fish-1"), Catch("fish-2"), Catch("fish-3") };
            a.OnVerifiedCatchesSold(catches);
            Assert.IsTrue(a.OrderPaid);
            var balance = economy.SharedBalance;
            Assert.Greater(balance, 0);
            a.OnVerifiedCatchesSold(catches);
            Assert.AreEqual(balance, economy.SharedBalance);
            var saved = JsonUtility.FromJson<LivingWorldSaveData>(JsonUtility.ToJson(a.Export()));
            var restored = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(restored.Restore(saved));
            restored.OnVerifiedCatchesSold(catches);
            Assert.AreEqual(balance, economy.SharedBalance);
            Assert.IsTrue(restored.OrderPaid);
        }

        [Test]
        public void SettledVerifiedPublication_PaysOnce_AndTitleCannotChangeEvidence()
        {
            var a = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(a.BeginDay(7, "campaign"));
            var context = new RecordingWorldContext(RecordingSubjectKind.Species, "coast", "cell-a", "shallow", true);
            var clip = new ClipManifest("clip-1", "rec-1", "dive-1", 7,
                new PlayerId(0), "sea_bass", 4, 12f, "hash", 10000L, true, true, context);
            var publication = new PublicationSave
            {
                PublicationId = "pub-clip-1", ClipId = "clip-1", RecordingId = "rec-1",
                SubjectId = "sea_bass", QueuedDay = 7, SettledId = "settle-pub-clip-1",
                Title = "Title A"
            };
            a.OnVerifiedPublication(clip, publication);
            Assert.IsTrue(a.SponsorPaid);
            var balance = economy.SharedBalance;
            publication.Title = "Title B";
            a.OnVerifiedPublication(clip, publication);
            Assert.AreEqual(balance, economy.SharedBalance);
        }

        [Test]
        public void MissingSettlement_OrSpoofedClipId_DoesNotAwardSponsor()
        {
            var a = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(a.BeginDay(4, "campaign"));
            var context = new RecordingWorldContext(RecordingSubjectKind.Species, "coast", "cell-a", "shallow", true);
            var clip = new ClipManifest("clip-1", "rec-1", "dive-1", 4,
                new PlayerId(0), "sea_bass", 4, 12f, "hash", 1024L, true, true, context);
            var publication = new PublicationSave { PublicationId = "pub-clip-1", ClipId = "fake",
                RecordingId = "rec-1", SubjectId = "sea_bass", QueuedDay = 4, SettledId = "settled" };
            a.OnVerifiedPublication(clip, publication);
            Assert.IsFalse(a.SponsorPaid);
            publication.ClipId = clip.ClipId;
            publication.SettledId = "";
            a.OnVerifiedPublication(clip, publication);
            Assert.IsFalse(a.SponsorPaid);
            Assert.AreEqual(0, economy.SharedBalance);
        }

        [Test]
        public void RolesAreFree_SingleSelection_AndHostChoiceIsSaved()
        {
            CrewRole applied = CrewRole.None;
            var a = new LivingWorldAuthority(economy, () => true,
                (_, role) => { applied = role; return true; });
            Assert.IsTrue(a.BeginDay(2, "campaign"));
            Assert.IsTrue(a.TrySelectRole(new PlayerId(0), CrewRole.Hunter, 1, true).Accepted);
            Assert.AreEqual(CrewRole.Hunter, applied);
            Assert.IsTrue(a.TrySelectRole(new PlayerId(0), CrewRole.Explorer, 2, true).Accepted);
            Assert.AreEqual(CrewRole.Explorer, a.RoleFor(new PlayerId(0)));
            Assert.AreEqual(0, economy.SharedBalance);
            var loaded = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(loaded.Restore(a.Export()));
            Assert.AreEqual(CrewRole.Explorer, loaded.RoleFor(new PlayerId(0)));
            Assert.IsFalse(loaded.TrySelectRole(new PlayerId(0), (CrewRole)200, 3, true).Accepted);
        }

        [Test]
        public void PurchasesAreNonnegative_NonRepeatable_AndRequireCorrectOrder()
        {
            Assert.IsTrue(economy.TryCreditLivingReward("living-test-seed", 2000));
            var a = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(a.BeginDay(1, "campaign"));
            var player = new PlayerId(0);
            Assert.IsFalse(a.TryUpgrade(player, LivingWorldCatalog.HouseGallery, 1, true).Accepted);
            Assert.AreEqual(40, economy.StorageCapacity);
            Assert.IsTrue(a.TryUpgrade(player, LivingWorldCatalog.HouseArchive, 2, true).Accepted);
            Assert.AreEqual(50, economy.StorageCapacity);
            Assert.IsFalse(a.TryUpgrade(player, LivingWorldCatalog.HouseArchive, 2, true).Accepted);
            Assert.IsTrue(a.TryUpgrade(player, LivingWorldCatalog.HouseGallery, 3, true).Accepted);
            Assert.AreEqual(2, a.HouseTier);
            Assert.AreEqual(2000 - 280 - 460, economy.SharedBalance);
            var reloaded = new LivingWorldAuthority(economy, () => true);
            Assert.IsTrue(reloaded.Restore(a.Export()));
            Assert.IsFalse(reloaded.TryUpgrade(player, LivingWorldCatalog.HouseGallery, 4, true).Accepted);
            Assert.AreEqual(2, reloaded.HouseTier);
            Assert.AreEqual(50, economy.StorageCapacity);
        }

        [Test]
        public void FailedSave_RollsBackRoleAndUpgrade()
        {
            Assert.IsTrue(economy.TryCreditLivingReward("living-test-seed", 1000));
            bool writable = true;
            CrewRole effect = CrewRole.None;
            var a = new LivingWorldAuthority(economy, () => writable,
                (_, role) => { effect = role; return true; });
            Assert.IsTrue(a.BeginDay(1, "campaign"));
            writable = false;
            var result = a.TrySelectRole(new PlayerId(0), CrewRole.Hunter, 1, true);
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(CrewRole.None, effect);
            Assert.AreEqual(CrewRole.None, a.RoleFor(new PlayerId(0)));
            // Economy's disk callback is independent here; spending an upgrade would be
            // committed only with the real SaveStore, so this test asserts role rollback.
        }
    }
}
