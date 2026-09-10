using System;
using UnityEngine;

namespace DeepDive.World
{
    public enum SpeciesClass { Fish, Crustacean, Cephalopod }

    // Weights are grams (docs/plan/CONTRACTS.md: "agirlik gram olarak tanimlanir"). Pure rules
    // struct so the roll can be tested without creating an asset.
    [Serializable]
    public struct WeightRange
    {
        [SerializeField] private int minGrams;
        [SerializeField] private int maxGrams;

        public WeightRange(int minGrams, int maxGrams)
        {
            this.minGrams = minGrams;
            this.maxGrams = maxGrams;
        }

        public int MinGrams => minGrams;
        public int MaxGrams => maxGrams;
        public bool IsValid => minGrams > 0 && maxGrams >= minGrams;

        // Host-side only: the host rolls once and replicates the result, so no shared seed is
        // needed between machines. Inclusive on both ends.
        public int Roll(System.Random random)
        {
            if (!IsValid) return 0;
            if (random == null || minGrams == maxGrams) return minGrams;
            return random.Next(minGrams, maxGrams + 1);
        }
    }

    // Behaviour parameters the swim rules read. Distances are metres and speeds metres/second
    // (docs/plan/CONTRACTS.md: "Mesafe metre, sure saniye"). Invalid inspector values fall back
    // instead of producing a motionless or teleporting fish.
    public readonly struct SwimTuning
    {
        public readonly float SwimSpeed;
        public readonly float FleeSpeed;
        public readonly float FleeRadius;
        public readonly float WanderRadius;

        public SwimTuning(float swimSpeed, float fleeSpeed, float fleeRadius, float wanderRadius)
        {
            SwimSpeed = Positive(swimSpeed, 1.5f);
            FleeSpeed = Positive(fleeSpeed, 3.5f);
            FleeRadius = Finite(fleeRadius) && fleeRadius > 0f ? fleeRadius : 0f;
            WanderRadius = Positive(wanderRadius, 1f);
        }

        private static float Positive(float value, float fallback) =>
            Finite(value) && value > 0f ? value : fallback;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // docs/plan/CONTRACTS.md ortak veri sozlugu: "SpeciesDefinition | Sabit speciesId, sinif,
    // davranis parametreleri, agirlik araligi | Utku -> Mehmet/Mert | P2".
    //
    // Static definition asset: it carries the stable id that goes over the network and into
    // saves, never per-player state ("ScriptableObject nesnesi kalici oyuncu verisi gibi
    // kullanilmaz"). DisplayName is for UI only and is never used as an identity.
    [CreateAssetMenu(menuName = "DeepDive/World/Species Definition", fileName = "Species")]
    public sealed class SpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string speciesId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private SpeciesClass speciesClass = SpeciesClass.Fish;

        [Header("Catch")]
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private WeightRange weight = new WeightRange(400, 1200);

        [Header("Behaviour")]
        // Read through Swim by FishMotion.
        [SerializeField] private float swimSpeed = 1.5f;
        [SerializeField] private float fleeSpeed = 3.5f;
        [SerializeField] private float fleeRadius = 6f;
        [SerializeField] private float wanderRadius = 8f;

        public string SpeciesId => speciesId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? speciesId : displayName;
        public SpeciesClass Class => speciesClass;
        public float MaxHealth => maxHealth;
        public WeightRange Weight => weight;
        public SwimTuning Swim => new SwimTuning(swimSpeed, fleeSpeed, fleeRadius, wanderRadius);

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(speciesId)) { error = "speciesId is empty"; return false; }
            if (maxHealth <= 0f || float.IsNaN(maxHealth) || float.IsInfinity(maxHealth))
            { error = "maxHealth must be a positive number"; return false; }
            if (!weight.IsValid) { error = "weight range must be positive and ordered"; return false; }
            error = "";
            return true;
        }

        public int RollWeightGrams(System.Random random) => weight.Roll(random);
    }
}
