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

        // Unity keeps a managed wrapper for a prefab whose source asset disappeared. Returning
        // it directly lets callers pass a MissingReference to Instantiate. The implicit Unity
        // object check treats that state as null, which is exactly what the runtime fallback needs.
        [SerializeField] private GameObject firstPersonArmsPrefab;
        public GameObject FirstPersonArmsPrefab => ExistingOrNull(firstPersonArmsPrefab);
        public GameObject ThirdPersonRigPrefab => ExistingOrNull(thirdPersonRigPrefab);
        public GameObject HarpoonPropPrefab => ExistingOrNull(harpoonPropPrefab);
        public GameObject CameraPropPrefab => ExistingOrNull(cameraPropPrefab);

        private static GameObject ExistingOrNull(GameObject value) => value ? value : null;
    }
}
