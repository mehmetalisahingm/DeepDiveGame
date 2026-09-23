using System;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    /// <summary>
    /// Resolves host-side pickup ray hits without letting ambient trigger volumes (for example
    /// SwimVolume) occlude a real catch/boat-part target. Non-trigger geometry still blocks the
    /// ray, preserving ordinary line-of-sight/occlusion semantics.
    /// </summary>
    public static class PickupRaycastRules
    {
        public static Collider SelectFirstPickupOrBlocker(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0) return null;

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null) continue;

                if (IsPickupTarget(collider)) return collider;

                // Trigger volumes such as SwimVolume describe world state; they must not eat E-pickup.
                if (collider.isTrigger) continue;

                // Real solid world geometry remains a hard occluder.
                return collider;
            }

            return null;
        }

        public static bool IsPickupTarget(Collider collider) =>
            FindTarget<ICatchPickupTarget>(collider) != null ||
            FindTarget<IBoatPartPickupTarget>(collider) != null;

        private static T FindTarget<T>(Collider collider) where T : class
        {
            if (collider == null) return null;
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var behaviour in behaviours)
                if (behaviour is T target) return target;
            return null;
        }
    }
}
