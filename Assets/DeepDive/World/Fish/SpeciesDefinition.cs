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
        // Declared now so the shape is agreed; the AI step (next P2-B slice) is what reads them.
        [SerializeField] private float swimSpeed = 1.5f;
        [SerializeField] private float fleeSpeed = 3.5f;
        [SerializeField] private float fleeRadius = 6f;
        [SerializeField] private float wanderRadius = 8f;

        public string SpeciesId => speciesId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? speciesId : displayName;
        public SpeciesClass Class => speciesClass;
        public float MaxHealth => maxHealth;
        public WeightRange Weight => weight;
        public float SwimSpeed => swimSpeed;
        public float FleeSpeed => fleeSpeed;
        public float FleeRadius => fleeRadius;
        public float WanderRadius => wanderRadius;

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
