using UnityEngine;

namespace DeepDive.Composition
{
    public enum ReefKitKind { Coral, Rock, Plant }

    // #217 P4.11-C: the data a reef-kit prefab hands to the habitat placer (Utku) and the camera/player wiring (Mehmet).
    // Presentation only: the prefabs carry no collider, rigidbody or NetworkObject; where a collider is wanted is a
    // placement decision, so the hint below is advice, never a component.
    public sealed class ReefKitPiece : MonoBehaviour
    {
        public const string ResourceFolder = "ReefKit/";
        public string Id;                  // stable id, also the prefab name
        public ReefKitKind Kind;
        public Vector3 Size;               // metres: width (x), height (y), depth (z) of the LOD0 bounds
        public bool WalkThrough;           // true when the piece has an opening a diver can swim through (arch)
        public Vector3 ColliderHint;       // suggested box size (m) for a placement collider; zero = none needed

        public static readonly string[] AllIds =
        {
            "reef-coral-branching", "reef-coral-fan", "reef-coral-mound", "reef-rock-boulder", "reef-rock-arch", "reef-plant-seagrass"
        };

        public static GameObject Load(string id) => Resources.Load<GameObject>(ResourceFolder + id);
    }
}
