using UnityEngine;

namespace DeepDive.World
{
    // The one world<->map transform for a dive region (docs/plan/CONTRACTS.md "Harita ve kalici
    // kesif": "Mehmet onayli oyuncu/aktif tekne konumunu, Utku dunya-harita donusumunu/kesif
    // olayini, Mert UI ve kayit katmanini saglar"). Mert's MapIcon already carries normalized
    // MapX/MapZ, so this is the only place raw world coordinates ever become map coordinates and
    // the UI never has to repeat the arithmetic.
    //
    // Pure on purpose: a readonly struct with no MonoBehaviour, no scene lookup, no logging and
    // no Unity object of its own, so the whole conversion is testable against plain numbers the
    // way WaterBody is. The region's actual extents are scene data and belong to the DiveRegionField
    // component; this type is only the arithmetic and holds exactly the four numbers it needs.
    //
    // Y is not part of the transform. WorldToMap takes a Vector3 because callers hold world
    // positions, and never reads its y; MapToWorld returns a Vector2 rather than a Vector3, so no
    // caller can be handed a height this type invented. A map coordinate answers "where in the
    // region", never "how deep" - depth is DiveDepthBandSet's question and the two stay unrelated.
    //
    // Out of region is refused, never clamped. Clamping would produce a coordinate that looks
    // approved while nothing in the world occupies it, and approved positions are Mehmet's
    // ("Mehmet onayli oyuncu/aktif tekne konumu"); pinning a stray icon to the border under this
    // layer's name would be exactly the guess the map contract forbids. Callers get false and
    // decide for themselves - if the map screen wants a stray icon pinned to its edge, that clamp
    // belongs in the UI where a reader can see it happen.
    public readonly struct DiveRegionBounds
    {
        // Footprint edges in world XZ, the same four-float shape WaterBody uses and for the same
        // reason: no reader has to remember that a Vector2's y would be holding a z.
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public DiveRegionBounds(float minX, float maxX, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        // Inverted or zero-width bounds are refused rather than normalized. A region authored back
        // to front is a mistake in the scene, and quietly swapping its ends would map every icon
        // to the mirror image of where it belongs - a wrong answer is worse than no answer here.
        // WaterBody.TryFromBox refuses a degenerate box for the same reason.
        public bool IsValid =>
            IsFinite(MinX) && IsFinite(MaxX) && IsFinite(MinZ) && IsFinite(MaxZ) &&
            MaxX > MinX && MaxZ > MinZ;

        // Inclusive on all four edges: a dock or an anchor authored exactly on the boundary is
        // inside the region, not a rejection. NaN compares false against everything, so a bad
        // coordinate drops out here and never reaches the division below.
        public bool Contains(float worldX, float worldZ) =>
            worldX >= MinX && worldX <= MaxX && worldZ >= MinZ && worldZ <= MaxZ;

        // worldX -> mapX and worldZ -> mapZ, both 0..1 across the region. The out value on a
        // refusal is default, not the nearest edge: a caller that ignores the bool gets an
        // obviously unset coordinate rather than a plausible wrong one.
        public bool TryWorldToMap(Vector3 world, out Vector2 map)
        {
            map = default;
            if (!IsValid) return false;
            if (!Contains(world.x, world.z)) return false;

            map = new Vector2(
                (world.x - MinX) / (MaxX - MinX),
                (world.z - MinZ) / (MaxZ - MinZ));
            return true;
        }

        // The inverse, back to world XZ only. LerpUnclamped rather than Lerp: Lerp would silently
        // fold an out-of-range map coordinate onto the border, which is the one behaviour this
        // whole type exists to refuse - and the range check above has already rejected it, so the
        // unclamped form can never actually extrapolate.
        public bool TryMapToWorld(Vector2 map, out Vector2 worldXZ)
        {
            worldXZ = default;
            if (!IsValid) return false;
            if (!IsUnit(map.x) || !IsUnit(map.y)) return false;

            worldXZ = new Vector2(
                Mathf.LerpUnclamped(MinX, MaxX, map.x),
                Mathf.LerpUnclamped(MinZ, MaxZ, map.y));
            return true;
        }

        private static bool IsUnit(float value) => IsFinite(value) && value >= 0f && value <= 1f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
