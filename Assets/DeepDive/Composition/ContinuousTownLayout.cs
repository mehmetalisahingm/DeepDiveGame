using System;
using DeepDive.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.Composition
{
    // P4.6-A: a deterministic, non-networked town blockout in the SAME scene as the coast.
    // All clients author exactly the same terrain on load; no additional scene/NetworkObject
    // authority is introduced. The P3 beach, water, routes and authored service IDs stay intact.
    // This is a foundation, not proof of the P4.6 four-player/three-day acceptance.
    public static class ContinuousTownLayout
    {
        public const string SceneName = "DiveTestArea";
        public const string RootName = "P46_ContinuousTown";
        public static readonly Vector3 HomeOrigin = new Vector3(0f, 8.4f, -48f);
        public const float WalkSurfaceY = 8.4f;
        private const float SlabThickness = 0.6f;

        public static readonly Vector3[] StorefrontPositions =
        {
            new Vector3(-10f, WalkSurfaceY, -29f),
            new Vector3(-3.3f, WalkSurfaceY, -29f),
            new Vector3(3.4f, WalkSurfaceY, -29f),
            new Vector3(10.1f, WalkSurfaceY, -29f)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName) Build(scene);
        }

        // Idempotent for reconnects and editor test calls. World layout does not move
        // NetworkPlayer/Boat state; it only positions scenery and existing shop anchors.
        public static void Build(Scene scene)
        {
            if (!scene.IsValid() || scene.name != SceneName) return;
            var root = FindRoot(scene, RootName);
            if (root != null) return;

            // The old south wall ends at y=7 while dry beach is y=8.4, so the
            // sidewalk crosses ABOVE the wall without touching it or altering arena physics.
            // A 0.6m-thick slab has top exactly at 8.4, flush with Beach_Platform.
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var parent = root.transform;
            var sand = MaterialFromScene(scene, "Beach_Platform");
            var building = MaterialFromScene(scene, "Wall_S") ?? sand;

            Slab(parent, "CoastWalk", new Vector2(0f, -16.25f), new Vector2(7.5f, 7.0f), sand);
            Slab(parent, "MainStreet", new Vector2(0f, -22f), new Vector2(7.5f, 13f), sand);
            Slab(parent, "TownSquare", new Vector2(0f, -29f), new Vector2(28f, 14f), sand);
            Slab(parent, "HouseApproach", new Vector2(0f, -40f), new Vector2(7.5f, 12f), sand);
            Slab(parent, "HomePlot", new Vector2(0f, -49f), new Vector2(15f, 13f), sand);

            // Physical facades outside the square's pedestrian area. No collider is placed
            // over the paths or shop customer marks; service range remains host-validated.
            foreach (var x in new[] { -18f, 18f })
            {
                Block(parent, "MainBuilding_" + (x < 0 ? "West" : "East"),
                    new Vector3(x, 11.0f, -28f), new Vector3(7f, 5.2f, 8f), building, true);
                Block(parent, "TownRoof_" + (x < 0 ? "West" : "East"),
                    new Vector3(x, 13.65f, -28f), new Vector3(8f, 0.4f, 9f), sand, false);
            }

            // Home has an open front so the player can enter from the square on foot.
            // All home targets (beds/storage/PC) are positioned by their existing bindings.
            Block(parent, "HomeBack", HomeOrigin + new Vector3(0f, 1.5f, -6f),
                new Vector3(12f, 3f, 0.35f), building, true);
            foreach (var x in new[] { -6f, 6f })
                Block(parent, x < 0 ? "HomeLeft" : "HomeRight",
                    HomeOrigin + new Vector3(x, 1.5f, 0f), new Vector3(0.35f, 3f, 12f), building, true);
            Block(parent, "HomeRoof", HomeOrigin + new Vector3(0f, 3.1f, -0.2f),
                new Vector3(12f, 0.3f, 12f), sand, false);

            var services = FindRoot(scene, "TownServices");
            if (services == null)
            {
                Debug.LogError("P46_TOWN_SERVICES_MISSING: authored NPC anchors required");
                return;
            }
            var anchors = services.GetComponentsInChildren<ServicePointAnchor>(true);
            int moved = 0;
            foreach (var anchor in anchors)
            {
                var id = anchor.Definition.ServiceId;
                int slot = id switch
                {
                    "shop-equipment" => 0,
                    "buyer-fish" => 1,
                    "buyer-recording" => 2,
                    "vendor-vehicle" => 3,
                    _ => -1
                };
                if (slot < 0) continue;
                anchor.transform.SetPositionAndRotation(StorefrontPositions[slot], Quaternion.identity);
                moved++;
            }
            if (moved != StorefrontPositions.Length)
                Debug.LogError($"P46_SHOP_ANCHORS_INCOMPLETE moved={moved}/{StorefrontPositions.Length}");

            Debug.Log($"P46_CONTINUOUS_TOWN_READY shops={moved} home={HomeOrigin} scene={scene.name}");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static Material MaterialFromScene(Scene scene, string objectName)
        {
            var go = FindRoot(scene, objectName);
            return go == null ? null : go.GetComponent<Renderer>()?.sharedMaterial;
        }

        private static void Slab(Transform parent, string name, Vector2 xz, Vector2 size, Material material)
        {
            Block(parent, name,
                new Vector3(xz.x, WalkSurfaceY - SlabThickness / 2f, xz.y),
                new Vector3(size.x, SlabThickness, size.y), material, true);
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            if (!keepCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.Destroy(collider);
            }
        }
    }
}
