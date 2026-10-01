using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Economy
{
    // P4.3-C catalog extension. EconomyManager remains the purchase/ownership authority;
    // this file only contributes the two upper camera definitions that its public catalog seam accepts.
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

    // EconomySaveStore loads during Awake. The P3 manager's built-in defaults predate P4.3, so a
    // campaign containing camera-advanced/professional would otherwise be filtered before a later
    // system got a chance to contribute those definitions. SceneLoaded is still before gameplay
    // input: apply the P4 catalog and replay the SAME campaign file once against the complete catalog.
    // No second persistence source is created and each live manager is processed only once.
    public static class P4EquipmentCatalogBootstrap
    {
        private static readonly HashSet<int> ConfiguredManagers = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            ConfiguredManagers.Clear();
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToLiveManagers();

        public static void ApplyToLiveManagers()
        {
            var managers = Object.FindObjectsByType<EconomyManager>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < managers.Length; i++)
            {
                var economy = managers[i];
                if (economy == null || !ConfiguredManagers.Add(economy.GetInstanceID())) continue;

                P4EquipmentCatalog.Apply(economy);

                var save = economy.GetComponent<EconomySaveStore>();
                if (save != null && !save.LoadNow())
                    Debug.LogWarning("P4 camera catalog: campaign reload failed after catalog bind");
            }
        }
    }
}
