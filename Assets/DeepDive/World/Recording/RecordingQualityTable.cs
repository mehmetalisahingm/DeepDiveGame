using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // One quality step. Serializable so the table can be edited in the inspector, with a public
    // constructor so the tier rules stay testable without an asset (WeightRange does the same).
    //
    // Name is for UI only and is never an identity: what crosses to Mert is the tier index.
    [Serializable]
    public struct QualityTier
    {
        [SerializeField] private string name;
        [SerializeField] private float minScore01;
        [SerializeField] private float minValidSeconds;

        public QualityTier(string name, float minScore01, float minValidSeconds)
        {
            this.name = name;
            this.minScore01 = minScore01;
            this.minValidSeconds = minValidSeconds;
        }

        public string Name => string.IsNullOrWhiteSpace(name) ? "tier" : name;
        public float MinScore01 => minScore01;
        public float MinValidSeconds => minValidSeconds;

        public bool IsValid =>
            Finite(minScore01) && minScore01 >= 0f && minScore01 <= 1f &&
            Finite(minValidSeconds) && minValidSeconds > 0f;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Turns a finished recording into the single int that crosses the module boundary.
    //
    // docs/plan/CONTRACTS.md: "RecordingResult | ... kalite ... | Utku -> Mert/Mehmet". The
    // shared struct types Quality as an int, and this is the agreed meaning of that int:
    // 0 means the shot did not earn anything, 1..N are the tiers of this table in ascending
    // order. Sending a raw 0..100 score instead would push the thresholds onto Mert, and the
    // thresholds are explicitly Utku's ("Kalite esikleri bende, fiyatlar Mert'te").
    public static class RecordingQuality
    {
        public const int NoPayout = 0;

        // Float comparisons on accumulated time land a hair under the threshold often enough
        // that an exact >= would fail a recording the player genuinely held long enough.
        private const float Epsilon = 0.0001f;

        // Highest tier first: a shot that clears Gold also clears Bronze, and the best match is
        // the one that should be paid. Malformed tiers are skipped rather than throwing, so one
        // bad inspector row cannot make every recording worthless.
        public static int Evaluate(IReadOnlyList<QualityTier> tiers, float score01, float validSeconds)
        {
            if (tiers == null || tiers.Count == 0) return NoPayout;
            if (!Finite(score01) || !Finite(validSeconds) || validSeconds <= 0f) return NoPayout;

            for (var i = tiers.Count - 1; i >= 0; i--)
            {
                var tier = tiers[i];
                if (!tier.IsValid) continue;
                if (score01 + Epsilon < tier.MinScore01) continue;
                if (validSeconds + Epsilon < tier.MinValidSeconds) continue;
                return i + 1;
            }
            return NoPayout;
        }

        // Evaluate walks from the top down, so an out-of-order table would silently award a low
        // tier for a great shot. Checked by RecordingQualityTable.IsValid rather than sorted
        // behind the designer's back: a table that does not mean what it looks like is a bug.
        public static bool AreAscending(IReadOnlyList<QualityTier> tiers, out string error)
        {
            error = "";
            if (tiers == null || tiers.Count == 0)
            {
                error = "at least one quality tier is required";
                return false;
            }

            for (var i = 0; i < tiers.Count; i++)
            {
                if (!tiers[i].IsValid)
                {
                    error = $"tier {i} needs a 0..1 minScore01 and a positive minValidSeconds";
                    return false;
                }
                if (i == 0) continue;
                if (tiers[i].MinScore01 < tiers[i - 1].MinScore01 ||
                    tiers[i].MinValidSeconds < tiers[i - 1].MinValidSeconds)
                {
                    error = $"tier {i} is easier than tier {i - 1}; tiers must ascend";
                    return false;
                }
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // The thresholds half of P3-B, as a static definition asset (SpeciesDefinition pattern):
    // framing gates for the live sampling plus the tier ladder for the final grade. It carries
    // no per-player state, so it is never persistent player data.
    [CreateAssetMenu(menuName = "DeepDive/World/Recording Quality Table", fileName = "RecordingQuality")]
    public sealed class RecordingQualityTable : ScriptableObject
    {
        [Header("Framing gates")]
        // Read through Framing by the host sampler; see RecordingTuning for the fallbacks.
        [Tooltip("Closer than this the subject overfills the frame and the shot is refused.")]
        [SerializeField] private float minDistanceMetres = 1.5f;
        [Tooltip("Past this the water fog makes the shot worthless.")]
        [SerializeField] private float maxDistanceMetres = 14f;
        [Tooltip("How far off the centre of the lens the subject may sit and still count.")]
        [SerializeField] private float maxOffAxisDegrees = 22f;
        [Tooltip("Smallest share of the frame half-height the subject may span to be identifiable.")]
        [SerializeField] private float minFrameFill = 0.08f;
        [Tooltip("Share of the frame half-height that scores full marks for size.")]
        [SerializeField] private float idealFrameFill = 0.45f;
        [Tooltip("How much of the score is centring rather than size.")]
        [SerializeField] private float centeringWeight = 0.4f;

        [Header("Quality tiers (ascending; index + 1 is the Quality sent to Mert)")]
        [SerializeField]
        private QualityTier[] tiers =
        {
            new QualityTier("Bronze", 0.25f, 2f),
            new QualityTier("Silver", 0.5f, 4f),
            new QualityTier("Gold", 0.7f, 6f),
            new QualityTier("Platinum", 0.85f, 9f)
        };

        public RecordingTuning Framing => new RecordingTuning(minDistanceMetres, maxDistanceMetres,
            maxOffAxisDegrees, minFrameFill, idealFrameFill, centeringWeight);

        public IReadOnlyList<QualityTier> Tiers => tiers ?? Array.Empty<QualityTier>();

        public int TierCount => tiers == null ? 0 : tiers.Length;

        public int Evaluate(float score01, float validSeconds) =>
            RecordingQuality.Evaluate(Tiers, score01, validSeconds);

        public bool IsValid(out string error)
        {
            var framing = Framing;
            // Framing values self-correct, so the only thing worth refusing here is a band the
            // designer clearly did not mean: the struct widened it instead of rejecting it.
            if (framing.MaxDistanceMetres <= framing.MinDistanceMetres)
            {
                error = "distance band must be positive and ordered";
                return false;
            }
            return RecordingQuality.AreAscending(Tiers, out error);
        }
    }
}
