using UnityEngine;

namespace DeepDive.World
{
    // A named place in the world (#138): an empty transform carrying one WorldAnchorIds id.
    // Identity only - no collider, renderer or NetworkObject. Whoever needs the place looks it up
    // by id through WorldAnchors instead of hard-coding a coordinate.
    [DisallowMultipleComponent]
    public sealed class WorldAnchor : MonoBehaviour
    {
        [SerializeField] private string id = "";

        public string Id => id ?? "";

        public void Configure(string anchorId)
        {
            id = anchorId ?? "";
        }
    }
}
