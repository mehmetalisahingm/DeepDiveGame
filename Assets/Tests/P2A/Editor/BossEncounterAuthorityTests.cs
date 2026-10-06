using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;

namespace DeepDive.Tests
{
    public sealed class BossEncounterAuthorityTests
    {
        private static readonly PlayerId Host = new PlayerId(0);
        private static readonly PlayerId Guest = new PlayerId(1);

        [Test]
        public void ProgressionGateControlsLockedAvailableActiveCompletedLifecycle()
        {
            var boss = NewBoss();
            Assert.AreEqual(BossEncounterPhase.Locked, boss.Phase);
            Assert.AreEqual(PlayerActionResult.InvalidState, boss.TryActivate("encounter-deep-1"));

            Assert.IsTrue(boss.RefreshProgression(unlocked: true, completed: false));
            Assert.AreEqual(BossEncounterPhase.Available, boss.Phase);
            Assert.AreEqual(PlayerActionResult.InvalidTarget, boss.TryActivate("wrong-encounter"));
            Assert.AreEqual(PlayerActionResult.Accepted, boss.TryActivate("encounter-deep-1"));
            Assert.AreEqual(BossEncounterPhase.Active, boss.Phase);

            Assert.IsTrue(boss.RefreshProgression(unlocked: true, completed: true));
            Assert.AreEqual(BossEncounterPhase.Completed, boss.Phase);
            Assert.AreEqual(0f, boss.Health);
            Assert.AreEqual(PlayerActionResult.InvalidState, boss.TryActivate("encounter-deep-1"));
        }

        [Test]
        public void ExistingHarpoonContractDamagesOnlyAnActiveBossAndReplayIsIdempotent()
        {
            var boss = NewBoss(maxHealth: 5f);
            boss.RefreshProgression(true, false);
            Assert.AreEqual(PlayerActionResult.InvalidState,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 1, 2f)));

            boss.TryActivate("encounter-deep-1");
            Assert.AreEqual(PlayerActionResult.Accepted,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 1, 2f)));
            Assert.AreEqual(3f, boss.Health);

            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 1, 2f)));
            Assert.AreEqual(3f, boss.Health, "replayed hit cannot deal damage twice");

            Assert.AreEqual(PlayerActionResult.Accepted,
                boss.TryApplyHarpoonHit(new HarpoonHit(Guest, 1, 1f)),
                "request ids are player-scoped");
            Assert.AreEqual(2f, boss.Health);
        }

        [Test]
        public void DefeatIsNotCompletedUntilPersistenceAcknowledgesIt()
        {
            var boss = NewBoss(maxHealth: 2f);
            boss.RefreshProgression(true, false);
            boss.TryActivate("encounter-deep-1");

            Assert.AreEqual(PlayerActionResult.Accepted,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 10, 2f)));
            Assert.IsTrue(boss.DefeatedPendingPersistence);
            Assert.AreEqual(BossEncounterPhase.Active, boss.Phase);

            Assert.IsTrue(boss.MarkCompletionPersisted());
            Assert.AreEqual(BossEncounterPhase.Completed, boss.Phase);
            Assert.AreEqual(0f, boss.Health);
            Assert.IsFalse(boss.MarkCompletionPersisted(), "completion is one-way");
        }

        [Test]
        public void FailedCompletionWriteLeavesOneHealthSoANewHitCanRetry()
        {
            var boss = NewBoss(maxHealth: 8f);
            boss.RefreshProgression(true, false);
            boss.TryActivate("encounter-deep-1");
            boss.TryApplyHarpoonHit(new HarpoonHit(Host, 21, 99f));

            Assert.IsTrue(boss.RestoreAfterFailedCompletion());
            Assert.AreEqual(BossEncounterPhase.Active, boss.Phase);
            Assert.AreEqual(1f, boss.Health);
            Assert.AreEqual(PlayerActionResult.DuplicateRequest,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 21, 1f)));
            Assert.AreEqual(PlayerActionResult.Accepted,
                boss.TryApplyHarpoonHit(new HarpoonHit(Guest, 22, 1f)));
            Assert.IsTrue(boss.DefeatedPendingPersistence);
        }

        [Test]
        public void AbortNormalizesTransientStateWithoutPersistingHealth()
        {
            var boss = NewBoss(maxHealth: 7f);
            boss.RefreshProgression(true, false);
            boss.TryActivate("encounter-deep-1");
            boss.TryApplyHarpoonHit(new HarpoonHit(Host, 30, 3f));
            Assert.AreEqual(4f, boss.Health);

            Assert.IsTrue(boss.Abort());
            Assert.AreEqual(BossEncounterPhase.Available, boss.Phase);
            Assert.AreEqual(7f, boss.Health);
            Assert.IsFalse(boss.Abort());

            Assert.AreEqual(PlayerActionResult.Accepted, boss.TryActivate("encounter-deep-1"));
            Assert.AreEqual(7f, boss.Health, "a new encounter never restores transient old health");
        }

        [Test]
        public void InvalidDamageNeverChangesEncounterState()
        {
            var boss = NewBoss(maxHealth: 3f);
            boss.RefreshProgression(true, false);
            boss.TryActivate("encounter-deep-1");
            var revision = boss.Revision;

            Assert.AreEqual(PlayerActionResult.Rejected,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 40, 0f)));
            Assert.AreEqual(PlayerActionResult.Rejected,
                boss.TryApplyHarpoonHit(new HarpoonHit(Host, 41, float.NaN)));
            Assert.AreEqual(3f, boss.Health);
            Assert.AreEqual(revision, boss.Revision);
        }

        [Test]
        public void ActiveEncounterDoesNotRegressWhenProgressionProviderIsTemporarilyRebound()
        {
            var boss = NewBoss();
            boss.RefreshProgression(true, false);
            boss.TryActivate("encounter-deep-1");

            Assert.IsFalse(boss.RefreshProgression(false, false));
            Assert.AreEqual(BossEncounterPhase.Active, boss.Phase);
            Assert.IsTrue(boss.Abort());
            Assert.IsTrue(boss.RefreshProgression(false, false));
            Assert.AreEqual(BossEncounterPhase.Locked, boss.Phase);
        }

        private static BossEncounterAuthority NewBoss(float maxHealth = 6f) =>
            new BossEncounterAuthority(DeepProgressionIds.BossId, "encounter-deep-1", maxHealth);
    }
}
