using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.6-C: asserts the original real sea -> ramp -> ledge -> pier route still
    // connects to P4.6's walkable town, with no cloned boat position or water authority.
    // Existing BoatTripManager/NetworkBoatController continue to own vehicle movement.
    public static class CoastalHarborLink
    {
        public const string RootName = "P46_HarborWayfinding";
        private const float GapTolerance = 0.06f;
        private const float HeightTolerance = 0.06f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != ContinuousTownLayout.SceneName) return;
            if (!TryValidate(scene, out var error))
            {
                Debug.LogError("P46_HARBOR_LINK_INVALID " + error);
                return;
            }
            BuildMarkers(scene);
        }

        public static bool TryValidate(Scene scene, out string error)
        {
            error = string.Empty;
            if (!scene.IsValid() || scene.name != ContinuousTownLayout.SceneName)
                return Fail("wrong scene", out error);

            var beach = GetCollider(scene, "Beach_Platform");
            var ledge = GetCollider(scene, "Shore_Ledge");
            var dock = GetCollider(scene, "Dock_Town");
            var ramp = GetCollider(scene, "Shore_Ramp");
            var wade = GetCollider(scene, "Beach_Wade");
            var pierAnchor = FindRoot(scene, "Dock_Town_Anchor");
            if (beach == null || ledge == null || dock == null ||
                ramp == null || wade == null || pierAnchor == null)
                return Fail("required authored shore/ledge/ramp/pier/wade/anchor missing", out error);

            // The walk is a continuous dry ground surface. Neither a phase teleport nor a
            // hidden transition can substitute for either of the two physical seams.
            if (Mathf.Abs(beach.bounds.max.z - ledge.bounds.min.z) > GapTolerance ||
                Mathf.Abs(ledge.bounds.max.z - dock.bounds.min.z) > GapTolerance)
                return Fail("beach/ledge/pier have a gap or overlap", out error);
            if (Mathf.Abs(beach.bounds.max.y - ledge.bounds.max.y) > HeightTolerance ||
                Mathf.Abs(ledge.bounds.max.y - dock.bounds.max.y) > HeightTolerance)
                return Fail("beach/ledge/pier have an unwalkable vertical step", out error);

            // The ramp's high-side top edge must be flush with the ledge, while
            // its lower edge is submerged so the diver can exit the water physically.
            var top = ramp.transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
            if (Mathf.Abs(top.x - ledge.bounds.min.x) > 0.08f ||
                Mathf.Abs(top.y - ledge.bounds.max.y) > 0.08f ||
                ramp.bounds.min.y >= 8f)
                return Fail("water ramp is disconnected from dry ledge", out error);
            if (wade.bounds.min.y >= 8f)
                return Fail("beach wade does not reach swimming water", out error);

            var anchor = pierAnchor.transform.position;
            if (anchor.x < dock.bounds.min.x || anchor.x > dock.bounds.max.x ||
                anchor.z < dock.bounds.min.z || anchor.z > dock.bounds.max.z ||
                Mathf.Abs(anchor.y - dock.bounds.max.y) > HeightTolerance)
                return Fail("authoritative pier anchor outside dock surface", out error);

            return true;
        }

        // Decorative pointers only; no collider, NetworkObject, BoatTrip position
        // authority or duplicated persistence data. Both host and clients see the same.
        public static void BuildMarkers(Scene scene)
        {
            if (!TryValidate(scene, out var error))
                throw new InvalidOperationException("P46_HARBOR_LINK_INVALID " + error);
            if (FindRoot(scene, RootName) != null) return;
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var source = FindRoot(scene, "Dock_Town")?.GetComponent<Renderer>()?.sharedMaterial;
            foreach (var z in new[] { -12.9f, -10.5f, -8.1f })
            {
                var mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mark.name = "ToPier_" + z.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                mark.transform.SetParent(root.transform, false);
                mark.transform.position = new Vector3(12f, ContinuousTownLayout.WalkSurfaceY + 0.04f, z);
                mark.transform.localScale = new Vector3(0.28f, 0.04f, 0.28f);
                if (source != null) mark.GetComponent<Renderer>().sharedMaterial = source;
                var col = mark.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(col);
                    else UnityEngine.Object.DestroyImmediate(col);
                }
            }
        }

        private static bool Fail(string reason, out string error)
        {
            error = reason;
            return false;
        }

        private static Collider GetCollider(Scene scene, string name) =>
            FindRoot(scene, name)?.GetComponent<Collider>();

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
    }
}
