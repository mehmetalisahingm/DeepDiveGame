using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Network
{
    public enum BossEncounterPhase : byte
    {
        Locked = 0,
        Available = 1,
        Active = 2,
        Completed = 3
    }

    // P4.4-A (#121): transient, host-owned encounter rules.
    // Progression/save remain Mert's authority; world placement/arena validity remain Utku's.
    // This object owns only the live encounter phase + health and consumes the existing HarpoonHit contract.
    public sealed class BossEncounterAuthority
    {
        private readonly HashSet<(ulong Player, ulong Request)> appliedHits =
            new HashSet<(ulong Player, ulong Request)>();

        public string BossId { get; }
        public string EncounterId { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public BossEncounterPhase Phase { get; private set; } = BossEncounterPhase.Locked;
        public int Revision { get; private set; }

        public bool DefeatedPendingPersistence =>
            Phase == BossEncounterPhase.Active && Health <= 0f;

        public BossEncounterAuthority(string bossId, string encounterId, float maxHealth)
        {
            BossId = string.IsNullOrWhiteSpace(bossId) ? DeepProgressionIds.BossId : bossId;
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? "encounter-deep-1" : encounterId;
            MaxHealth = Finite(maxHealth) && maxHealth > 0f ? maxHealth : 1f;
            Health = MaxHealth;
        }

        // The progression read-model is the gate. Active encounters are transient and are not
        // cancelled merely because a provider is briefly rebound; scene/area shutdown calls Abort.
        public bool RefreshProgression(bool unlocked, bool completed)
        {
            if (completed)
                return Set(BossEncounterPhase.Completed, 0f);

            if (Phase == BossEncounterPhase.Active)
                return false;

            return Set(unlocked ? BossEncounterPhase.Available : BossEncounterPhase.Locked, MaxHealth);
        }

        public PlayerActionResult TryActivate(string encounterId)
        {
            if (!string.Equals(encounterId, EncounterId, StringComparison.Ordinal))
                return PlayerActionResult.InvalidTarget;
            if (Phase == BossEncounterPhase.Active)
                return PlayerActionResult.Accepted;
            if (Phase != BossEncounterPhase.Available)
                return PlayerActionResult.InvalidState;

            Phase = BossEncounterPhase.Active;
            Health = MaxHealth;
            Revision++;
            return PlayerActionResult.Accepted;
        }

        public PlayerActionResult TryApplyHarpoonHit(HarpoonHit hit)
        {
            var key = (hit.PlayerId.Value, hit.RequestId);
            if (appliedHits.Contains(key))
                return PlayerActionResult.DuplicateRequest;
            if (Phase != BossEncounterPhase.Active || Health <= 0f)
                return PlayerActionResult.InvalidState;
            if (!Finite(hit.Damage) || hit.Damage <= 0f)
                return PlayerActionResult.Rejected;

            appliedHits.Add(key);
            Health = Math.Max(0f, Health - hit.Damage);
            Revision++;
            return PlayerActionResult.Accepted;
        }

        // Called only after DeepProgressionEvidence accepted (or already had) the completion.
        public bool MarkCompletionPersisted()
        {
            if (!DefeatedPendingPersistence) return false;
            Phase = BossEncounterPhase.Completed;
            Health = 0f;
            Revision++;
            return true;
        }

        // A save failure must not leave a zero-health "completed-looking" transient boss.
        // One more valid host-authoritative hit can retry the completion write.
        public bool RestoreAfterFailedCompletion()
        {
            if (!DefeatedPendingPersistence) return false;
            Health = Math.Min(MaxHealth, 1f);
            Revision++;
            return true;
        }

        // Leaving/tearing down an unfinished encounter never persists active health. A later
        // legitimate activation starts cleanly from MaxHealth.
        public bool Abort()
        {
            if (Phase != BossEncounterPhase.Active) return false;
            Phase = BossEncounterPhase.Available;
            Health = MaxHealth;
            Revision++;
            return true;
        }

        private bool Set(BossEncounterPhase phase, float health)
        {
            if (Phase == phase && Math.Abs(Health - health) <= 0.0001f)
                return false;
            Phase = phase;
            Health = health;
            Revision++;
            return true;
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
