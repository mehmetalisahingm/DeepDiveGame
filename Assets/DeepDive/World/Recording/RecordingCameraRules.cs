using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // What a camera tier changes about a shot (P4.3, docs/plan/CONTRACTS.md "Kamera, derinlik ve
    // rota gelisimi"): optical reach and low-light capability, and nothing else. A better camera
    // sees further and in less light; it never grades a frame higher, never pays more, and owns no
    // price or ownership rule - Mert's catalog and Mehmet's bridge decide which tier a diver holds.
    //
    // Pure and static like RecordingFraming: no scene, no clock, no global. Light arrives as the
    // DaylightState and depth the caller already resolved (RecordingLight / WaterDepth), so every
    // rule here is testable with plain numbers.
    public static class RecordingCameraRules
    {
        // Optical reach of the Advanced and Professional bodies. Added to the framing's own far
        // edge rather than replacing it, so a subject whose framing is authored wider (the
        // bioluminescence event, 16 m) keeps that difference at every tier.
        public const float ExtendedReachMetres = 6f;

        // The same bodies resolve a smaller subject: the frame-fill floor scales by this. Without
        // it the extended reach would be unreachable for an ordinary fish, which fails the
        // frame-fill gate long before the far edge (r = 0.55 m at 60 degrees: ~11.9 m).
        public const float OpticalFrameFillFactor = 0.7f;

        // Light reaching the subject. The shallow band is fully lit - it is the water every P3
        // shot was taken in - and below it the light falls linearly to a floor at the bottom of
        // the deep cut (docs/plan/GAMEPLAY_LOOP.md draft: reef 8-20 m, deep 20-35 m).
        public const float LitDepthMetres = DiveDepthBands.ShallowMaxDepth;
        public const float DarkDepthMetres = 35f;
        public const float DeepLightFloor = 0.2f;

        // The least light each tier films in. Basic and Advanced share one floor: the first
        // upgrade is reach. Professional films through the whole designed range - the darkest
        // designed case is night at 35 m, 0.25 * 0.2 = 0.05 - with a margin under it.
        public const float BasicMinLight = 0.5f;
        public const float AdvancedMinLight = 0.5f;
        public const float ProfessionalMinLight = 0.04f;

        // Same tolerance the tier ladder uses: a light that lands a hair under an exact floor
        // through float arithmetic still counts as reaching it.
        private const float Epsilon = 0.0001f;

        // None is Basic: legacy callers build a candidate without a tier, and whether a diver
        // owns a camera at all is the bridge's invariant, not a World refusal. An undefined
        // value is Basic too, so a corrupt tier can never unlock more than the base camera.
        public static CameraTier Normalize(CameraTier tier) =>
            tier == CameraTier.Advanced || tier == CameraTier.Professional ? tier : CameraTier.Basic;

        public static bool HasExtendedOptics(CameraTier tier) => Normalize(tier) != CameraTier.Basic;

        public static float MaxDistanceFor(RecordingTuning framing, CameraTier tier) =>
            HasExtendedOptics(tier) ? framing.MaxDistanceMetres + ExtendedReachMetres : framing.MaxDistanceMetres;

        public static float MinFrameFillFor(RecordingTuning framing, CameraTier tier) =>
            HasExtendedOptics(tier) ? framing.MinFrameFill * OpticalFrameFillFactor : framing.MinFrameFill;

        // The framing a take of this tier is judged by. Basic returns the subject's own framing
        // untouched, which is what keeps every pre-P4.3 shot exactly as it was. The other tiers
        // move only the far edge and the frame-fill floor; the near edge, the cone, the ideal
        // fill and the centring weight - everything the score is computed from - stay the same.
        public static RecordingTuning TuningFor(RecordingTuning framing, CameraTier tier)
        {
            if (!HasExtendedOptics(tier)) return framing;
            return new RecordingTuning(framing.MinDistanceMetres, MaxDistanceFor(framing, tier),
                framing.MaxOffAxisDegrees, MinFrameFillFor(framing, tier), framing.IdealFrameFill,
                framing.CenteringWeight);
        }

        // 1 through the shallow band (and above the surface), DeepLightFloor at and below
        // DarkDepthMetres, linear in between. A depth that is not a number reads as lit: it can
        // only come from a broken host read, and a new refusal must not be what breaks old shots.
        public static float DepthFactor(float depthMetres)
        {
            if (!IsFinite(depthMetres) || depthMetres <= LitDepthMetres) return 1f;
            if (depthMetres >= DarkDepthMetres) return DeepLightFloor;
            return Mathf.Lerp(1f, DeepLightFloor,
                (depthMetres - LitDepthMetres) / (DarkDepthMetres - LitDepthMetres));
        }

        // 0..1 light on the subject. The day term is DaylightModel's ambient multiplier: it is
        // what the scene's ambient is scaled by, it already carries the night floor, and unlike
        // the sun it ignores the day's overcast - weather is P4.5, and must not quietly lock a
        // Basic camera out of a cloudy noon.
        public static float Light01(in DaylightState daylight, float depthMetres)
        {
            var ambient = daylight.AmbientMultiplier;
            var surface = IsFinite(ambient) ? Mathf.Clamp01(ambient) : 1f;
            return surface * DepthFactor(depthMetres);
        }

        public static float MinLight01(CameraTier tier)
        {
            switch (Normalize(tier))
            {
                case CameraTier.Professional: return ProfessionalMinLight;
                case CameraTier.Advanced: return AdvancedMinLight;
                default: return BasicMinLight;
            }
        }

        public static bool IsLitEnough(CameraTier tier, float light01) =>
            !IsFinite(light01) || light01 + Epsilon >= MinLight01(tier);

        // Darkness is judged after the framing, never instead of it: only a frame the framing
        // accepted can become TooDark, so a frame that is dark and also blocked, out of reach or
        // off frame reports the reason the diver can act on. The measured numbers travel along,
        // and the score drops to zero like every other refusal.
        public static RecordingSample ApplyLight(RecordingSample framed, CameraTier tier, float light01)
        {
            if (!framed.IsValid || IsLitEnough(tier, light01)) return framed;
            return RecordingSample.Reject(RecordingSampleRejection.TooDark, framed.DistanceMetres,
                framed.OffAxisDegrees, framed.FrameFill);
        }

        // One host sample for one diver: the framing at the take's tier, then the light gate.
        public static RecordingSample Sample(Vector3 eye, Vector3 forward, float verticalFovDegrees,
            Vector3 subject, float subjectRadius, bool occluded, RecordingTuning framing, CameraTier tier,
            float light01)
        {
            var framed = RecordingFraming.Evaluate(eye, forward, verticalFovDegrees, subject, subjectRadius,
                occluded, TuningFor(framing, tier));
            return ApplyLight(framed, tier, light01);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
