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

    // EconomySaveStore loads during Awake. The P3 manager's built-in defaults predate P4.3, so an
    // old/new campaign containing camera-advanced/professional would otherwise be filtered before
    // a later system got a chance to contribute those definitions. SceneLoaded runs after all Awake
    // calls but before gameplay Start/first-frame input: apply the P4 catalog and immediately replay
    // that same campaign file once, now against the complete catalog. One live EconomyManager is
    // processed only once; scene changes do not reset or duplicate ownership.
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

                // Re-read only the first time this live manager is seen. No new persistence authority:
                // EconomySaveStore still owns the exact same file and restore validation.
                var save = economy.GetComponent<EconomySaveStore>();
                if (save != null && !save.LoadNow())
                    Debug.LogWarning("P4 camera catalog: campaign reload failed after catalog bind");
            }
        }
    }
}
