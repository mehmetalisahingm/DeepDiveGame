using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // A slice of the water column, measured as depth below the surface rather than as a world
    // Y range (docs/plan/CONTRACTS.md "Canlilar ve dunya | Utku | ... derinlik kesimleri").
    //
    // Depth, not Y, because that is the language the design speaks: "0-8 m", P4.4's three
    // depth cuts, the encyclopedia's habitat/depth entry. A band written in world Y would have
    // to be re-authored for every scene whose surface sits somewhere else; a band written in
    // depth survives the move. The surface it is measured from belongs to DiveDepthBandSet.
    //
    // This is NOT a water volume and it does not classify locomotion. It answers "how deep is
    // this" and nothing else; the answer type is a DepthBand, never an EnvironmentLocomotion.
    // The seam that decides Land/Surface/Underwater is IWaterField and it is untouched here.
    public readonly struct DepthBand
    {
        // Stable and kebab-cased to match the ids Mehmet locked for the boat parts, so a save
        // or an encyclopedia entry can reference a band by name later without a migration.
        public readonly string Id;

        // Metres below the surface. 0 is the water line; larger is deeper. MinDepth is always
        // inclusive, which is what makes a diver exactly on the water line read as shallow.
        public readonly float MinDepth;
        public readonly float MaxDepth;

        // Whether MaxDepth itself belongs to the band. True for the original shape (shallow is
        // 0-8 with 8 included). P4.3's reef stops just short of 20 so that 20 m is deep, not
        // reef - a half-open edge, rather than a list order that would have to be remembered.
        public readonly bool MaxInclusive;

        public DepthBand(string id, float minDepth, float maxDepth)
            : this(id, minDepth, maxDepth, true)
        {
        }

        public DepthBand(string id, float minDepth, float maxDepth, bool maxInclusive)
        {
            Id = id ?? string.Empty;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
            MaxInclusive = maxInclusive;
        }

        public bool IsValid =>
            !string.IsNullOrEmpty(Id) &&
            IsFinite(MinDepth) && IsFinite(MaxDepth) &&
            MinDepth >= 0f && MaxDepth > MinDepth;

        // A malformed band contains nothing rather than containing everything, the same way a
        // malformed WaterBody simply never reads as wet: a caller that forgets to check
        // IsValid gets "not in this band", which is the safe answer.
        //
        // NaN and the infinities fall out of the comparisons on their own - every comparison
        // against NaN is false, and an infinite depth is outside any finite band.
        public bool Contains(float depth) =>
            IsValid && MinDepth <= depth && (MaxInclusive ? depth <= MaxDepth : depth < MaxDepth);

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    // The campaign's depth cuts (P4.3 #108, GAMEPLAY_LOOP draft): shallow 0-8 m, reef (8, 20),
    // deep 20-35 m, and nothing deeper. The original arena (sea bed y = 0, surface y = 8) is all
    // shallow; the reef shelf and the deep basin north of it are where the other two are measured.
    //
    // Compile-time constants rather than a ScriptableObject asset. SpeciesDefinition is an asset
    // because there are many species and a designer tunes each one; these boundaries are
    // contract numbers that other rules (the camera's light falloff) are tied to, and an asset
    // would add a silent-drift path with no review. This is the ONE place the metres live.
    public static class DiveDepthBands
    {
        // The ids live in Core's DepthBandIds so a cell, an observation and this band can never
        // spell them two ways; World only adds the metre ranges.
        public const string ShallowId = DepthBandIds.Shallow;
        public const float ShallowMinDepth = 0f;
        public const float ShallowMaxDepth = 8f;

        // Each edge is the previous band's edge by name, never a second literal. 8.0 m itself is
        // shallow (frozen, shallow is inclusive and listed first); 20.0 m is deep (reef is
        // half-open at its bottom); 35 m is still deep and anything past it has no band.
        public const string ReefId = DepthBandIds.Reef;
        public const float ReefMinDepth = ShallowMaxDepth;
        public const float ReefMaxDepth = 20f;

        public const string DeepId = DepthBandIds.Deep;
        public const float DeepMinDepth = ReefMaxDepth;
        public const float DeepMaxDepth = 35f;

        public static readonly DepthBand Shallow =
            new DepthBand(ShallowId, ShallowMinDepth, ShallowMaxDepth);

        public static readonly DepthBand Reef =
            new DepthBand(ReefId, ReefMinDepth, ReefMaxDepth, false);

        public static readonly DepthBand Deep =
            new DepthBand(DeepId, DeepMinDepth, DeepMaxDepth);

        // Shallowest first, the order DepthBandIds.All already uses.
        public static readonly IReadOnlyList<DepthBand> All = new[] { Shallow, Reef, Deep };

        // First match wins. The only shared point is 8.0 m (shallow's inclusive bottom, reef's
        // inclusive top), and listing shallow first is what keeps it shallow; reef's bottom is
        // half-open, so 20 m is deep whatever the order. The disjointness test pins both.
        public static bool TryFind(float depth, out DepthBand band)
        {
            for (var i = 0; i < All.Count; i++)
            {
                if (!All[i].Contains(depth)) continue;
                band = All[i];
                return true;
            }

            band = default;
            return false;
        }
    }
}
