using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    public static class ServiceInteractionRules
    {
        public static PlayerActionResult Validate(ServicePointDefinition definition, Vector3 playerPosition,
            Vector3 anchorPosition, bool hasClearLineOfSight)
        {
            if (!IsValid(definition) || !hasClearLineOfSight)
                return PlayerActionResult.InvalidTarget;

            var distance = definition.InteractionDistance;
            var maxDistanceSquared = distance * distance;
            return (anchorPosition - playerPosition).sqrMagnitude <= maxDistanceSquared
                ? PlayerActionResult.Accepted
                : PlayerActionResult.InvalidTarget;
        }

        public static bool IsValid(ServicePointDefinition definition) =>
            !string.IsNullOrWhiteSpace(definition.ServiceId) &&
            definition.ServiceType != ServicePointType.None &&
            !string.IsNullOrWhiteSpace(definition.WorldAnchor) &&
            !float.IsNaN(definition.InteractionDistance) &&
            !float.IsInfinity(definition.InteractionDistance) &&
            definition.InteractionDistance > 0f;
    }
}
