using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // Physical scene seam for P3.2 NPC/service interactions. It contains no prices, money,
    // quest state or save logic; those stay in the progression/economy layer.
    [DisallowMultipleComponent]
    public sealed class ServicePointAnchor : MonoBehaviour
    {
        [SerializeField] private string serviceId = "";
        [SerializeField] private ServicePointType serviceType = ServicePointType.EquipmentShop;
        [SerializeField] private string worldAnchor = "";
        [SerializeField, Min(0.25f)] private float interactionDistance = 2.5f;
        [SerializeField] private string catalogId = "";

        public Vector3 WorldPosition => transform.position;
        public ServicePointDefinition Definition => new ServicePointDefinition(
            serviceId,
            serviceType,
            string.IsNullOrWhiteSpace(worldAnchor) ? serviceId : worldAnchor,
            interactionDistance,
            catalogId);

        public void Configure(string id, ServicePointType type, string anchorId, float maxInteractionDistance,
            string catalog)
        {
            serviceId = id ?? string.Empty;
            serviceType = type;
            worldAnchor = anchorId ?? string.Empty;
            interactionDistance = Mathf.Max(0.25f, maxInteractionDistance);
            catalogId = catalog ?? string.Empty;
        }

        private void OnValidate()
        {
            if (float.IsNaN(interactionDistance) || float.IsInfinity(interactionDistance))
                interactionDistance = 2.5f;
            interactionDistance = Mathf.Max(0.25f, interactionDistance);
        }
    }
}
