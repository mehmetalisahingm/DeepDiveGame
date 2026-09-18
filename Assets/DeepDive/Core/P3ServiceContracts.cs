namespace DeepDive.Core.Contracts
{
    public enum ServicePointType : byte
    {
        None = 0,
        EquipmentShop = 1,
        FishBuyer = 2,
        RecordingBuyer = 3
    }

    // P3.2 service data owned by progression/economy and consumed by the player interaction layer.
    // WorldAnchor is a stable anchor id; the runtime Transform lives on ServicePointAnchor so Core
    // remains free of UnityEngine types.
    public readonly struct ServicePointDefinition
    {
        public readonly string ServiceId;
        public readonly ServicePointType ServiceType;
        public readonly string WorldAnchor;
        public readonly float InteractionDistance;
        public readonly string CatalogId;

        public ServicePointDefinition(string serviceId, ServicePointType serviceType, string worldAnchor,
            float interactionDistance, string catalogId)
        {
            ServiceId = serviceId ?? string.Empty;
            ServiceType = serviceType;
            WorldAnchor = worldAnchor ?? string.Empty;
            InteractionDistance = interactionDistance;
            CatalogId = catalogId ?? string.Empty;
        }
    }
}
