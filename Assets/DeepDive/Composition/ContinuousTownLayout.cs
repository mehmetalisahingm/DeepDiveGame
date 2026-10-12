using System;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
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

        public const string AnchorsName = "Anchors";
        public const string ZonesName = "Zones";
        public static readonly Vector3 TownSquareCenter = new Vector3(0f, WalkSurfaceY, -29f);

        // #138: the home rig's placement, gathered in one place. HomeSceneSetup/HomePcView still carry
        // their own copies until PR-2 binds them to the anchors; ContinuousTownWorldContractTests locks
        // the two together. Positions are local to HomeOrigin.
        public static class HomeRig
        {
            public static readonly float[] BedXs = { -3f, -1f, 1f, 3f };
            public const float BedLocalY = 0.3f;
            public const float BedLocalZ = 3.45f;
            public const float BedYaw = 180f;
            public static readonly Vector3 BedSize = new Vector3(1.25f, 0.35f, 2.05f);
            public static readonly Vector3 StorageLocal = new Vector3(-3.9f, 0.8f, -3.55f);
            public static readonly Vector3 StorageSize = new Vector3(1.35f, 1.6f, 0.75f);
            public static readonly Vector3 PcLocal = new Vector3(3.9f, 0.6f, -3.55f);
            public static readonly Vector3 PcSize = new Vector3(1.4f, 1.2f, 0.7f);
            // The open facade line (north end of HomeLeft/HomeRight); faces out of the house (+z).
            public static readonly Vector3 DoorLocal = new Vector3(0f, 0f, 6f);
            // The clear middle of the room: >= 1.57m to the beds, >= 1.3m to storage/PC, >= 4m to a wall.
            public static readonly Vector3[] SpawnLocal =
            {
                new Vector3(-1.5f, 0f, -1.5f),
                new Vector3(1.5f, 0f, -1.5f),
                new Vector3(-1.5f, 0f, 0.5f),
                new Vector3(1.5f, 0f, 0.5f)
            };

            public static Vector3 Bed(int index) => HomeOrigin + new Vector3(BedXs[index], BedLocalY, BedLocalZ);
            public static Vector3 Storage => HomeOrigin + StorageLocal;
            public static Vector3 Pc => HomeOrigin + PcLocal;
            public static Vector3 Door => HomeOrigin + DoorLocal;
            public static Vector3 Spawn(int index) => HomeOrigin + SpawnLocal[index];
        }

        // #138 zone volumes (world min/max). Data only: WorldZoneField reads them, physics never does.
        public static class ZoneVolumes
        {
            public static readonly Vector3 HomeMin = new Vector3(-5.825f, 7.9f, -53.825f);
            public static readonly Vector3 HomeMax = new Vector3(5.825f, 11.4f, -42f);
            public static readonly Vector3 DockMin = new Vector3(7f, 7.9f, -11f);
            public static readonly Vector3 DockMax = new Vector3(11f, 13f, -3.5f);
            public static readonly Vector3 TownMin = new Vector3(-14.75f, 7.9f, -55.8f);
            public static readonly Vector3 TownMax = new Vector3(14.75f, 13f, -14.75f);
            public static readonly Vector3 CoastMin = new Vector3(-15f, 4.5f, -14.75f);
            public static readonly Vector3 CoastMax = new Vector3(15f, 13f, -3.5f);
        }

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
            // Players approach the west/east shop plots diagonally from the beach. The
            // narrow old street left both approaches above empty space at z=-18..-22.
            Slab(parent, "MainStreet", new Vector2(0f, -22f), new Vector2(22f, 13f), sand);
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

            BuildAnchors(parent);
            BuildZones(scene, parent);

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

        // Empty transforms: no collider, renderer or NetworkObject. Nothing consumes them yet (PR-2/3).
        private static void BuildAnchors(Transform parent)
        {
            var anchors = new GameObject(AnchorsName).transform;
            anchors.SetParent(parent, false);
            Anchor(anchors, WorldAnchorIds.HomeRoot, HomeOrigin, 0f);
            Anchor(anchors, WorldAnchorIds.HomeDoor, HomeRig.Door, 0f);
            for (var i = 0; i < WorldAnchorIds.HomeBedCount; i++)
                Anchor(anchors, WorldAnchorIds.HomeBed(i), HomeRig.Bed(i), HomeRig.BedYaw);
            Anchor(anchors, WorldAnchorIds.HomeStorage, HomeRig.Storage, 0f);
            Anchor(anchors, WorldAnchorIds.HomePc, HomeRig.Pc, 0f);
            for (var i = 0; i < WorldAnchorIds.HomeSpawnCount; i++)
                Anchor(anchors, WorldAnchorIds.HomeSpawn(i), HomeRig.Spawn(i), 0f);
            Anchor(anchors, WorldAnchorIds.TownSquare, TownSquareCenter, 0f);
            for (var i = 0; i < WorldAnchorIds.TownServiceSlotCount; i++)
                Anchor(anchors, WorldAnchorIds.TownServiceSlot(i), StorefrontPositions[i], 0f);
        }

        private static void Anchor(Transform parent, string id, Vector3 position, float yaw)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<WorldAnchor>().Configure(id);
        }

        // Zone volumes plus the field that reads them. Sea comes only from the scene's one WaterField;
        // without it nothing is Sea, and there is no second water path.
        private static void BuildZones(Scene scene, Transform parent)
        {
            var zones = new GameObject(ZonesName);
            zones.transform.SetParent(parent, false);
            Zone(zones.transform, "Zone_Home", PlayerZone.Home, ZoneVolumes.HomeMin, ZoneVolumes.HomeMax);
            Zone(zones.transform, "Zone_Dock", PlayerZone.Dock, ZoneVolumes.DockMin, ZoneVolumes.DockMax);
            Zone(zones.transform, "Zone_Town", PlayerZone.Town, ZoneVolumes.TownMin, ZoneVolumes.TownMax);
            Zone(zones.transform, "Zone_Coast", PlayerZone.Coast, ZoneVolumes.CoastMin, ZoneVolumes.CoastMax);

            WaterField water = null;
            var waterCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var field in root.GetComponentsInChildren<WaterField>(true))
            {
                water = field;
                waterCount++;
            }
            if (waterCount != 1)
            {
                Debug.LogError($"P46_ZONE_WATER_MISSING fields={waterCount} scene={scene.name}");
                water = null;
            }
            zones.AddComponent<WorldZoneField>().Configure(water);
        }

        private static void Zone(Transform parent, string name, PlayerZone zone, Vector3 min, Vector3 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<WorldZoneVolume>().Configure(zone, max - min);
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
                if (collider != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                    else UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }
    }
}
