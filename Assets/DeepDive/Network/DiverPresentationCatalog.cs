using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    [CreateAssetMenu(menuName = "DeepDive/P3/Diver Presentation Catalog", fileName = ResourceName)]
    public sealed class DiverPresentationCatalog : ScriptableObject
    {
        public const string ResourceName = "DiverPresentationCatalog";

        [SerializeField] private GameObject thirdPersonRigPrefab;
        [SerializeField] private GameObject harpoonPropPrefab;
        [SerializeField] private GameObject cameraPropPrefab;
        [SerializeField] private GameObject cameraAdvancedPropPrefab;
        [SerializeField] private GameObject cameraProfessionalPropPrefab;
        [Header("Optional P4.8 repair art; unassigned uses the mechanical prototype")]
        [SerializeField] private GameObject repairPlankPrefab;
        [SerializeField] private GameObject repairHatchetPrefab;
        [SerializeField] private AudioClip repairImpactClip;
        [SerializeField] private GameObject repairPlacedPlankPrefab;
        [SerializeField] private GameObject repairNailPrefab;
        public GameObject RepairPlankPrefab => ExistingOrNull(repairPlankPrefab);
        public GameObject RepairHatchetPrefab => ExistingOrNull(repairHatchetPrefab);
        public AudioClip RepairImpactClip => repairImpactClip ? repairImpactClip : null;
        public GameObject RepairPlacedPlankPrefab => ExistingOrNull(repairPlacedPlankPrefab);
        public GameObject RepairNailPrefab => ExistingOrNull(repairNailPrefab);

        // Unity keeps a managed wrapper for a prefab whose source asset disappeared. Returning
        // it directly lets callers pass a MissingReference to Instantiate. The implicit Unity
        // object check treats that state as null, which is exactly what the runtime fallback needs.
        [SerializeField] private GameObject firstPersonArmsPrefab;
        public GameObject FirstPersonArmsPrefab => ExistingOrNull(firstPersonArmsPrefab);
        public GameObject ThirdPersonRigPrefab => ExistingOrNull(thirdPersonRigPrefab);
        public GameObject HarpoonPropPrefab => ExistingOrNull(harpoonPropPrefab);
        public GameObject CameraPropPrefab => ExistingOrNull(cameraPropPrefab);
        public GameObject CameraAdvancedPropPrefab => ExistingOrNull(cameraAdvancedPropPrefab);
        public GameObject CameraProfessionalPropPrefab => ExistingOrNull(cameraProfessionalPropPrefab);

        // Presentation-only fallback: ownership/tier still comes from NetworkPlayer. Missing art for
        // an upper tier never downgrades gameplay capability; it only reuses the basic prop until
        // the dedicated prefab is assigned in the catalog.
        public GameObject ResolveCameraPropPrefab(CameraTier tier)
        {
            if (tier == CameraTier.Professional && CameraProfessionalPropPrefab != null)
                return CameraProfessionalPropPrefab;
            if (tier == CameraTier.Advanced && CameraAdvancedPropPrefab != null)
                return CameraAdvancedPropPrefab;
            return CameraPropPrefab;
        }

        private static GameObject ExistingOrNull(GameObject value) => value ? value : null;
    }
}
