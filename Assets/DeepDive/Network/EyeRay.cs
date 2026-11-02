using UnityEngine;

namespace DeepDive.Network
{
    /// <summary>
    /// A ray from a diver's eye that cannot stop on that diver. The capsule's physics top is
    /// 1.62 m above the feet, and SurfaceCameraRules.Lift carries the eye through that height just
    /// under the water line. A ray that starts inside a collider ignores it, but from 1.5 cm above
    /// the top a downward ray runs into the capsule (#219).
    /// </summary>
    public static class EyeRay
    {
        public static bool Raycast(Vector3 origin, Vector3 direction, float range, QueryTriggerInteraction triggers,
            Collider self, out RaycastHit nearest)
        {
            nearest = default;
            var found = false;
            foreach (var hit in Physics.RaycastAll(origin, direction, range, ~0, triggers))
            {
                if (hit.collider == self || (found && hit.distance >= nearest.distance)) continue;
                nearest = hit;
                found = true;
            }
            return found;
        }
    }
}
