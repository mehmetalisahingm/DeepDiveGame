using System.Collections.Generic;

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

        // Metres below the surface. 0 is the water line; larger is deeper. Both ends are
        // inclusive, which is what makes a diver exactly on the water line read as shallow.
        public readonly float MinDepth;
        public readonly float MaxDepth;

        public DepthBand(string id, float minDepth, float maxDepth)
        {
            Id = id ?? string.Empty;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
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
        public bool Contains(float depth) => IsValid && MinDepth <= depth && depth <= MaxDepth;

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }

    // The campaign's depth cuts. One today: the whole of DiveTestArea is shallow, because its
    // sea bed sits at y = 0 and SwimVolume's surface at y = 8, so the entire column is 0-8 m.
    //
    // A compile-time constant rather than a ScriptableObject asset. SpeciesDefinition is an
    // asset because there are many species and a designer tunes each one; there is exactly one
    // band here and its boundary is a contract number that P4.4 will extend in code alongside
    // the new species and boss gating. An asset would add a silent-drift path with no review.
    public static class DiveDepthBands
    {
        public const string ShallowId = "shallow";
        public const float ShallowMinDepth = 0f;
        public const float ShallowMaxDepth = 8f;

        public static readonly DepthBand Shallow =
            new DepthBand(ShallowId, ShallowMinDepth, ShallowMaxDepth);

        public static readonly IReadOnlyList<DepthBand> All = new[] { Shallow };

        // First match wins. With one band the order cannot matter; when P4.4 adds the deeper
        // cuts they are expected to be disjoint, and the test that pins All's contents is
        // where a future overlap has to be argued for rather than slipped in.
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
