using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    public enum FishHitOutcome
    {
        Applied,     // damage landed, fish still alive
        Killed,      // this hit took the fish to zero
        Duplicate,   // same player + same requestId already applied
        AlreadyDead, // nothing left to kill
        Ignored      // damage was not a usable number (NaN/infinite/non-positive)
    }

    // Pure rules object so fish health authority can be tested without a live NGO session,
    // mirroring Mehmet's DiverVitalsState. Health and the death decision live on the host
    // (docs/plan/CONTRACTS.md: "Sonuc doguran canli ... degisikliklerini ev sahibi dogrular").
    public sealed class FishHealth
    {
        // docs/plan/CONTRACTS.md: "Her degistirici istek requestId tasir; ev sahibi ayni istegi
        // ikinci kez yeni islem olarak uygulamaz". Mehmet's ServerActionGate already filters
        // replays per diver; this is the same guard on the side that owns the health value.
        // Request ids are per-player sequences, so the key must include the player.
        private readonly HashSet<(ulong Player, ulong Request)> applied = new HashSet<(ulong, ulong)>();

        public float MaxHealth { get; }
        public float Health { get; private set; }
        public bool IsDead => Health <= 0f;

        public FishHealth(float maxHealth)
        {
            MaxHealth = Finite(maxHealth) && maxHealth > 0f ? maxHealth : 1f;
            Reset();
        }

        // Damage arrives already validated by the host (P2-A verifies the shot, range and
        // cooldown), so the amount is applied as given and never re-judged or capped here.
        // The only rejected values are ones that would corrupt the health number itself.
        public FishHitOutcome ApplyDamage(PlayerId player, ulong requestId, float damage)
        {
            var key = (player.Value, requestId);
            if (applied.Contains(key)) return FishHitOutcome.Duplicate;
            if (IsDead) return FishHitOutcome.AlreadyDead;
            if (!Finite(damage) || damage <= 0f) return FishHitOutcome.Ignored;

            applied.Add(key);
            Health = Health - damage;
            if (Health < 0f) Health = 0f;
            return IsDead ? FishHitOutcome.Killed : FishHitOutcome.Applied;
        }

        public void Reset()
        {
            Health = MaxHealth;
            applied.Clear();
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
