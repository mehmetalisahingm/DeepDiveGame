using UnityEngine;

namespace DeepDive.World
{
    // #310: how big a fish looks follows the weight the host rolled for it. A fish grows about the same in every
    // direction, so its length goes with the cube root of its weight; the species' mid weight is the model's own size.
    // Pure rules: the host computes it once per spawn and replicates the result, so every view shows the same specimen.
    public static class FishSize
    {
        public const float MinScale = 0.7f, MaxScale = 1.35f;

        public static float ScaleFor(int weightGrams, WeightRange range)
        {
            if (!range.IsValid || weightGrams <= 0) return 1f;
            var mid = (range.MinGrams + range.MaxGrams) * 0.5f;
            return Mathf.Clamp(Mathf.Pow(weightGrams / mid, 1f / 3f), MinScale, MaxScale);
        }
    }
}
