using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // Visuals only; established colliders and interaction anchors remain authoritative.
    public static class SuppliedTownArt
    {
        public static bool Attach(Transform parent, string key, Vector3 localPosition, Quaternion rotation, bool hideParent = false)
        {
            if (parent.Find("Supplied_" + key) != null) return true;
            var prefab = Resources.Load<GameObject>("SuppliedKit/" + key);
            if (prefab == null) return false;
            var visual = Object.Instantiate(prefab, parent, false);
            visual.name = "Supplied_" + key;
            // Cancel the primitive target's scale; art is authored in metres.
            var scale = parent.lossyScale;
            visual.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = rotation;
            if (hideParent && parent.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
            return true;
        }

        public static void Town(Transform root)
        {
            foreach (var side in new[] { "West", "East" })
            {
                var block = root.Find("MainBuilding_" + side);
                if (block == null) continue;
                Attach(block, "TownBuilding", new Vector3(0f, -0.5f, 0f), Quaternion.identity, true);
                var roof = root.Find("TownRoof_" + side);
                if (roof != null && roof.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
            }
        }

        public static void Home(Transform root)
        {
            var floor = ContinuousTownLayout.HomeOrigin;
            var decor = new GameObject("SuppliedHomeDecor").transform;
            decor.SetParent(root, false);
            decor.position = floor;
            Attach(decor, "Bookshelf", new Vector3(-4.9f, 0f, -4.8f), Quaternion.identity);
            Attach(decor, "Couch", new Vector3(0f, 0f, -5f), Quaternion.identity);
            // Back corner behind the PC: (5.3, 0, 4.8) stood in the east side corridor (P4-WADE-UNDERSIDE).
            Attach(decor, "Plant", new Vector3(5.3f, 0f, -5f), Quaternion.identity);
            Attach(decor, "DeskLight", new Vector3(4.35f, 1.2f, -3.55f), Quaternion.identity);
        }
    }
}
