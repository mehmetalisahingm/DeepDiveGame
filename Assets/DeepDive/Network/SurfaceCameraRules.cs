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
            return .35f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(surface-.45f,surface+.05f,capsuleTop));
        }
    }
}
