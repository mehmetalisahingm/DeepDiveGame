using DeepDive.Core.Contracts;

namespace DeepDive.Economy
{
    // P4.3-C catalog extension. EconomyManager remains the purchase/ownership authority;
    // this file only defines the two upper camera entries and their initial economy tuning.
    public static class P4EquipmentCatalog
    {
        public const string CameraAdvancedId = "camera-advanced";
        public const string CameraProfessionalId = "camera-professional";

        // Initial progression values. These are economy tuning values, not World capability rules.
        // Basic remains EconomyManager.CameraBasicId at 150 credits.
        public const int CameraAdvancedPrice = 400;
        public const int CameraProfessionalPrice = 900;

        public static readonly EquipmentDefinition CameraAdvanced =
            new EquipmentDefinition(CameraAdvancedId, "camera", 2, CameraAdvancedPrice);

        public static readonly EquipmentDefinition CameraProfessional =
            new EquipmentDefinition(CameraProfessionalId, "camera", 3, CameraProfessionalPrice);

        public static void Apply(EconomyManager economy)
        {
            if (economy == null) return;
            economy.AddToCatalog(CameraAdvanced);
            economy.AddToCatalog(CameraProfessional);
        }
    }
}
