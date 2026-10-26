using UnityEngine;

namespace DeepDive.Network
{
    public static class SurfaceCameraRules
    {
        // Float the visual head through the line as the capsule approaches it. The
        // root/capsule and the host's water classifier remain untouched.
        public static float Lift(LocomotionMode mode,float capsuleTop,float surface)
        {
            if(float.IsNaN(surface)||float.IsInfinity(surface)||
                (mode!=LocomotionMode.Surface&&mode!=LocomotionMode.Underwater))return 0;
            var rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(surface-.45f,surface+.05f,capsuleTop));
            // Fade back to the normal eye before the dry/wading transition, so the
            // cosmetic head lift cannot drop 35 cm when the owner walks up the ramp.
            var exit=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(surface+.05f,surface+.8f,capsuleTop));
            return .35f*rise*exit;
        }
    }
}
