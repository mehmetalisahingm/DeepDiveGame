using UnityEngine;

namespace DeepDive.Composition
{
    public enum ShoreKitKind { Dock, Rock, Tree, Prop }

    // #171 P4.7-C: the data a shore-kit prefab hands to the shore placer (Utku) and the player/camera wiring (Mehmet).
    // Presentation only: the prefabs carry no collider, rigidbody or NetworkObject; where a collider is wanted is a
    // placement decision, so the hint below is advice, never a component.
    public sealed class ShoreKitPiece : MonoBehaviour
    {
        public const string ResourceFolder = "ShoreKit/";
        public string Id;                  // stable id, also the prefab name
        public ShoreKitKind Kind;
        public Vector3 Size;               // metres: width (x), height (y), depth (z) of the visible bounds
        public float SurfaceY;             // local y of the plank top for a walkable dock, else 0 (the piece stands on y = 0)
        public bool Walkable;             // true when the top surface is meant to be walked on (dock planks)
        public Vector3 ColliderHint;       // suggested box size (m) for a placement collider; zero = none needed

        public static readonly string[] AllIds =
        {
            "shore-dock", "shore-dock-pole", "shore-dock-broken",
            "shore-rock-1", "shore-rock-2", "shore-rock-3", "shore-rock-4", "shore-rock-5",
            "shore-palm-1", "shore-palm-2", "shore-palm-3",
            "shore-prop-barrel", "shore-prop-bucket", "shore-prop-bucket-fishes", "shore-prop-anchor", "shore-prop-chest"
        };

        public static GameObject Load(string id) => Resources.Load<GameObject>(ResourceFolder + id);
    }
}
