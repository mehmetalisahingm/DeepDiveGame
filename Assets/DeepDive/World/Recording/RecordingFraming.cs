using UnityEngine;

namespace DeepDive.World
{
    // Why a sample was not worth recording. Mirrors FishHitOutcome: the rules say what they
    // decided, so a test (and later the host log) can tell "too far" from "behind a rock".
    public enum RecordingSampleRejection
    {
        None,
        InvalidInput, // a pose or size that is not a usable number (NaN/infinite/zero forward)
        Occluded,     // something solid sits between the camera and the subject
        TooClose,     // inside the near band: the subject overfills or clips the camera
        TooFar,       // past the far band, where water fog makes the shot worthless
        OffFrame,     // outside the framing cone: the subject is not actually in shot
        TooSmall      // in frame but too few pixels to identify the animal
    }

    // One instant of a running recording, as judged by the framing rules. Carries the measured
    // numbers alongside the verdict so tuning does not need a debugger.
    public readonly struct RecordingSample
    {
        public readonly bool IsValid;
        public readonly float Score01;
        public readonly float DistanceMetres;
        public readonly float OffAxisDegrees;
        public readonly float FrameFill;
        public readonly RecordingSampleRejection Rejection;

        private RecordingSample(bool isValid, float score01, float distanceMetres, float offAxisDegrees,
            float frameFill, RecordingSampleRejection rejection)
        {
            IsValid = isValid;
            Score01 = score01;
            DistanceMetres = distanceMetres;
            OffAxisDegrees = offAxisDegrees;
            FrameFill = frameFill;
            Rejection = rejection;
        }

        public static RecordingSample Valid(float score01, float distanceMetres, float offAxisDegrees, float frameFill) =>
            new RecordingSample(true, Mathf.Clamp01(score01), distanceMetres, offAxisDegrees, frameFill,
                RecordingSampleRejection.None);

        // A rejected sample always scores zero: RecordingSession must never be able to bank
        // time or score off a frame the rules refused.
        public static RecordingSample Reject(RecordingSampleRejection rejection, float distanceMetres = 0f,
            float offAxisDegrees = 0f, float frameFill = 0f) =>
            new RecordingSample(false, 0f, distanceMetres, offAxisDegrees, frameFill, rejection);
    }

    // The framing thresholds, as a plain struct so the maths is testable without an asset -
    // the same split as SpeciesDefinition -> SwimTuning. Distances are metres and angles
    // degrees (docs/plan/CONTRACTS.md: "Mesafe metre, sure saniye"). Invalid inspector values
    // fall back to a usable default instead of producing a camera that accepts or refuses
    // everything.
    public readonly struct RecordingTuning
    {
        public readonly float MinDistanceMetres;
        public readonly float MaxDistanceMetres;
        public readonly float MaxOffAxisDegrees;
        public readonly float MinFrameFill;
        public readonly float IdealFrameFill;
        public readonly float CenteringWeight;

        public RecordingTuning(float minDistanceMetres, float maxDistanceMetres, float maxOffAxisDegrees,
            float minFrameFill, float idealFrameFill, float centeringWeight)
        {
            MinDistanceMetres = Positive(minDistanceMetres, 1.5f);
            var maxDistance = Positive(maxDistanceMetres, 14f);
            // An inverted band would reject every distance, which reads in game as a broken
            // camera. Widening to the near edge keeps at least one usable distance.
            MaxDistanceMetres = maxDistance > MinDistanceMetres ? maxDistance : MinDistanceMetres;
            MaxOffAxisDegrees = Clamp(maxOffAxisDegrees, 1f, 89f, 22f);
            MinFrameFill = Clamp(minFrameFill, 0f, 1f, 0.08f);
            var ideal = Clamp(idealFrameFill, 0f, 1f, 0.45f);
            // The ideal must be reachable and stricter than the floor, otherwise the size score
            // would sit at 1 for a subject the gate barely let through.
            IdealFrameFill = ideal > MinFrameFill ? ideal : Mathf.Min(1f, MinFrameFill + 0.01f);
            CenteringWeight = Clamp(centeringWeight, 0f, 1f, 0.4f);
        }

        public static RecordingTuning Default => new RecordingTuning(1.5f, 14f, 22f, 0.08f, 0.45f, 0.4f);

        private static float Positive(float value, float fallback) =>
            IsFinite(value) && value > 0f ? value : fallback;

        private static float Clamp(float value, float min, float max, float fallback) =>
            IsFinite(value) && value >= min && value <= max ? value : fallback;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Decides whether the camera is actually looking at the subject well enough for the shot to
    // count, and how good that instant is. Pure maths, no NGO and no scene: the host samples the
    // world (camera pose, occlusion linecast) and hands the numbers in.
    //
    // docs/plan/CONTRACTS.md keeps quality thresholds on Utku's side and prices on Mert's, so
    // nothing here ever produces a credit value - only a 0..1 score the tier table reads.
    public static class RecordingFraming
    {
        private const float Epsilon = 0.0001f;
        private const float MinFovDegrees = 1f;
        private const float MaxFovDegrees = 179f;
        private const float FallbackFovDegrees = 60f;

        // Order matters: the cheapest and most absolute rejections come first, so the reported
        // rejection is the one a player could actually act on (get closer, move around the rock).
        public static RecordingSample Evaluate(Vector3 eye, Vector3 forward, float verticalFovDegrees,
            Vector3 subject, float subjectRadius, bool occluded, RecordingTuning tuning)
        {
            if (!IsFinite(eye) || !IsFinite(forward) || !IsFinite(subject) ||
                !IsFinite(verticalFovDegrees) || !IsFinite(subjectRadius) || subjectRadius <= 0f)
                return RecordingSample.Reject(RecordingSampleRejection.InvalidInput);

            var toSubject = subject - eye;
            var distance = toSubject.magnitude;

            // Occlusion is resolved by the host before calling in. A subject nobody can see has
            // no meaningful framing, so no framing numbers are reported for it.
            if (occluded) return RecordingSample.Reject(RecordingSampleRejection.Occluded, distance);

            if (distance <= Epsilon || distance < tuning.MinDistanceMetres)
                return RecordingSample.Reject(RecordingSampleRejection.TooClose, distance);
            if (distance > tuning.MaxDistanceMetres)
                return RecordingSample.Reject(RecordingSampleRejection.TooFar, distance);

            var forwardLength = forward.magnitude;
            if (forwardLength <= Epsilon)
                return RecordingSample.Reject(RecordingSampleRejection.InvalidInput, distance);

            var offAxis = Vector3.Angle(forward / forwardLength, toSubject / distance);
            // The tuning cone is a quality threshold, but it can never be more generous than the
            // lens: past the half-FOV the subject is literally off screen. Vertical half-FOV is
            // the conservative edge of a widescreen frame, which is the direction to err in.
            var maxOffAxis = Mathf.Min(tuning.MaxOffAxisDegrees, HalfFov(verticalFovDegrees));
            if (offAxis > maxOffAxis)
                return RecordingSample.Reject(RecordingSampleRejection.OffFrame, distance, offAxis);

            var fill = FrameFill(distance, subjectRadius, verticalFovDegrees);
            if (fill < tuning.MinFrameFill)
                return RecordingSample.Reject(RecordingSampleRejection.TooSmall, distance, offAxis, fill);

            // Centred and large both read as a deliberate shot. Weighted rather than multiplied
            // so one merely-decent axis cannot zero an otherwise good frame.
            var centering = maxOffAxis <= Epsilon ? 1f : 1f - Mathf.Clamp01(offAxis / maxOffAxis);
            var size = Mathf.Clamp01(fill / tuning.IdealFrameFill);
            var score = centering * tuning.CenteringWeight + size * (1f - tuning.CenteringWeight);
            return RecordingSample.Valid(score, distance, offAxis, fill);
        }

        // How much of the frame's half-height the subject's radius spans, 0..1. This is the
        // "big enough to identify" measure; it falls off with distance on its own, which is why
        // quality drops smoothly across the distance band without a second curve.
        public static float FrameFill(float distance, float subjectRadius, float verticalFovDegrees)
        {
            if (!IsFinite(distance) || !IsFinite(subjectRadius) || distance <= Epsilon || subjectRadius <= 0f)
                return 0f;
            var halfHeight = distance * Mathf.Tan(HalfFov(verticalFovDegrees) * Mathf.Deg2Rad);
            if (halfHeight <= Epsilon) return 0f;
            return Mathf.Clamp01(subjectRadius / halfHeight);
        }

        private static float HalfFov(float verticalFovDegrees)
        {
            var fov = IsFinite(verticalFovDegrees) && verticalFovDegrees > 0f
                ? Mathf.Clamp(verticalFovDegrees, MinFovDegrees, MaxFovDegrees)
                : FallbackFovDegrees;
            return fov * 0.5f;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }
}
