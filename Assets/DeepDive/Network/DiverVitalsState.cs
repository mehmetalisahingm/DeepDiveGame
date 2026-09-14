using System;

namespace DeepDive.Network
{
    // Pure rules object so oxygen/health authority can be tested without a live NGO session.
    public sealed class DiverVitalsState
    {
        public float MaxOxygen { get; private set; }
        public float MaxHealth { get; }
        public float OxygenDrainPerSecond { get; }
        public float LowOxygenFraction { get; }
        public float Oxygen { get; private set; }
        public float Health { get; private set; }
        public bool Passive => Oxygen <= 0f || Health <= 0f;
        public bool LowOxygen => !Passive && Oxygen <= MaxOxygen * LowOxygenFraction;

        public DiverVitalsState(float maxOxygen, float maxHealth, float oxygenDrainPerSecond, float lowOxygenFraction)
        {
            MaxOxygen = Positive(maxOxygen, 120f);
            MaxHealth = Positive(maxHealth, 100f);
            OxygenDrainPerSecond = Positive(oxygenDrainPerSecond, 1f);
            LowOxygenFraction = Clamp(lowOxygenFraction, 0.05f, 0.9f);
            Reset();
        }

        // Equipment is applied outside an active dive. Recomputing from the base/loadout value
        // instead of adding deltas makes duplicate loadout notifications idempotent.
        public bool SetMaxOxygen(float maxOxygen, bool refill)
        {
            var next = Positive(maxOxygen, MaxOxygen);
            if (Math.Abs(next - MaxOxygen) <= 0.001f) return false;
            MaxOxygen = next;
            if (refill) Oxygen = MaxOxygen;
            else Oxygen = Math.Min(Oxygen, MaxOxygen);
            return true;
        }

        public void Reset()
        {
            Oxygen = MaxOxygen;
            Health = MaxHealth;
        }

        public void Tick(float seconds, bool consumesOxygen)
        {
            if (!consumesOxygen || Passive || !Finite(seconds) || seconds <= 0f) return;
            Oxygen = Math.Max(0f, Oxygen - OxygenDrainPerSecond * seconds);
        }

        public bool ApplyDamage(float amount)
        {
            if (Passive || !Finite(amount) || amount <= 0f) return false;
            var before = Health;
            Health = Math.Max(0f, Health - amount);
            return Health != before;
        }

        private static float Positive(float value, float fallback) => Finite(value) && value > 0f ? value : fallback;
        private static float Clamp(float value, float min, float max) => !Finite(value) ? min : Math.Max(min, Math.Min(max, value));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
