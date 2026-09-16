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

        public GameObject ThirdPersonRigPrefab => thirdPersonRigPrefab;
        public GameObject HarpoonPropPrefab => harpoonPropPrefab;
        public GameObject CameraPropPrefab => cameraPropPrefab;
    }
}
