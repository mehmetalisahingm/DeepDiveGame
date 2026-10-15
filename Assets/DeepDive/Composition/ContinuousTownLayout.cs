using System;
using System.Collections.Generic;
using System.Linq;
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
        public const float SlabThickness = 0.6f;
        // #138 fills stop 5cm above SwimVolume's top (8.0) and 1.05m above the arena walls (7.0).
        public const float FillThickness = 0.35f;
        // Taller than stepOffset (0.3) and nobody can jump, so an edge fence cannot be climbed.
        public const float EdgeHeight = 1.2f;

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

        // #138: the home rig's placement, the single source of the home anchors. Since PR-2 the DiveTestArea
        // rig (HomeSceneSetup beds/storage, HomePcAnchor PC) reads those anchors and keeps no copy; PrepArea
        // keeps its own legacy values. Positions are local to HomeOrigin.
        public static class HomeRig
        {
            // #138 PR-2: 1.05m between beds (>= 2 x (radius + skin) = 0.86) and 1.75m side corridors.
            public static readonly float[] BedXs = { -3.45f, -1.15f, 1.15f, 3.45f };
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
                // Keep >= one diver diameter from the middle beds' exit poses.
                new Vector3(-1.5f, 0f, -0.25f),
                new Vector3(1.5f, 0f, -0.25f)
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

        public enum BlockMaterial { Sand, Building }

        // #153: one built cube, world center and size. Build() creates exactly these rows in list order;
        // tests derive their walk surfaces, edges and seams from them instead of keeping a second copy.
        public readonly struct TownBlock
        {
            public readonly string Name;
            public readonly Vector3 Center;
            public readonly Vector3 Size;
            public readonly BlockMaterial Material;
            public readonly bool HasCollider;

            public TownBlock(string name, Vector3 center, Vector3 size, BlockMaterial material, bool hasCollider)
            {
                Name = name;
                Center = center;
                Size = size;
                Material = material;
                HasCollider = hasCollider;
            }

            public Vector3 Min => Center - Size * 0.5f;
            public Vector3 Max => Center + Size * 0.5f;
        }

        // Walk slabs. The old south wall ends at y=7 while dry beach is y=8.4, so the sidewalk crosses
        // ABOVE the wall without touching it or altering arena physics. A 0.6m-thick slab has top exactly
        // at 8.4, flush with Beach_Platform.
        public static readonly IReadOnlyList<TownBlock> Slabs = new[]
        {
            Slab("CoastWalk", new Vector2(0f, -16.25f), new Vector2(7.5f, 7.0f)),
            // Players approach the west/east shop plots diagonally from the beach. The
            // narrow old street left both approaches above empty space at z=-18..-22.
            Slab("MainStreet", new Vector2(0f, -22f), new Vector2(22f, 13f)),
            Slab("TownSquare", new Vector2(0f, -29f), new Vector2(28f, 14f)),
            Slab("HouseApproach", new Vector2(0f, -40f), new Vector2(7.5f, 12f)),
            Slab("HomePlot", new Vector2(0f, -49f), new Vector2(15f, 13f))
        };

        // Physical facades outside the square's pedestrian area. No collider is placed over the paths or
        // shop customer marks; service range remains host-validated. Home has an open front so the player
        // can enter from the square on foot; all home targets (beds/storage/PC) stand on the anchors built
        // below (HomeSceneSetup).
        public static readonly IReadOnlyList<TownBlock> Structures = new[]
        {
            new TownBlock("MainBuilding_West", new Vector3(-18f, 11.0f, -28f), new Vector3(7f, 5.2f, 8f), BlockMaterial.Building, true),
            new TownBlock("TownRoof_West", new Vector3(-18f, 13.65f, -28f), new Vector3(8f, 0.4f, 9f), BlockMaterial.Sand, false),
            new TownBlock("MainBuilding_East", new Vector3(18f, 11.0f, -28f), new Vector3(7f, 5.2f, 8f), BlockMaterial.Building, true),
            new TownBlock("TownRoof_East", new Vector3(18f, 13.65f, -28f), new Vector3(8f, 0.4f, 9f), BlockMaterial.Sand, false),
            new TownBlock("HomeBack", HomeOrigin + new Vector3(0f, 1.5f, -6f), new Vector3(12f, 3f, 0.35f), BlockMaterial.Building, true),
            new TownBlock("HomeLeft", HomeOrigin + new Vector3(-6f, 1.5f, 0f), new Vector3(0.35f, 3f, 12f), BlockMaterial.Building, true),
            new TownBlock("HomeRight", HomeOrigin + new Vector3(6f, 1.5f, 0f), new Vector3(0.35f, 3f, 12f), BlockMaterial.Building, true),
            new TownBlock("HomeRoof", HomeOrigin + new Vector3(0f, 3.1f, -0.2f), new Vector3(12f, 0.3f, 12f), BlockMaterial.Sand, false)
        };

        // #138 (plan section 4): close the 0.75m strip behind the beach, the street's missing shoulders and
        // the strip in front of the home plot. Additions only: no slab, house, anchor or locked value moves.
        public static readonly IReadOnlyList<TownBlock> Fills = new[]
        {
            Fill("Fill_BeachBack", new Vector2(0f, -15.125f), new Vector2(29.5f, 0.75f)),
            Fill("Fill_StreetWest", new Vector2(-12.5f, -18.75f), new Vector2(3f, 6.5f)),
            Fill("Fill_StreetEast", new Vector2(12.5f, -18.75f), new Vector2(3f, 6.5f)),
            // HomePlot ends at z -42.5 but the house walls and open facade run to -42: floor that strip.
            Fill("Fill_HomeFront", new Vector2(0f, -42.25f), new Vector2(15f, 0.5f))
        };

        // Fences on every outer town edge that drops into the void. The water-side edges (beach north,
        // wade/ramp sides, dock, ledge) stay open on purpose: they step down into the sea.
        public static readonly IReadOnlyList<TownBlock> Edges = BuildEdges();

        private static TownBlock[] BuildEdges()
        {
            var edges = new List<TownBlock>();
            // Written for the west side as (minX, maxX, minZ, maxZ); mirror -1 is the east side.
            foreach (var mirror in new[] { 1f, -1f })
            {
                var side = mirror > 0 ? "West" : "East";
                edges.Add(Edge("Edge_Beach" + side, mirror, -15.05f, -14.75f, -15.8f, -11.0f));
                edges.Add(Edge("Edge_BackFillSouth" + side, mirror, -14.75f, -14.0f, -15.8f, -15.5f));
                edges.Add(Edge("Edge_Town" + side, mirror, -14.3f, -14.0f, -36.3f, -15.5f));
                edges.Add(Edge("Edge_SquareSouth" + side, mirror, -14.3f, -3.75f, -36.3f, -36.0f));
                edges.Add(Edge("Edge_Approach" + side, mirror, -4.05f, -3.75f, -42.0f, -36.0f));
                // Outside the facade line (z -42), so the house front stays open between the walls.
                edges.Add(Edge("Edge_PlotNorth" + side, mirror, -7.8f, -3.75f, -42.0f, -41.7f));
                edges.Add(Edge("Edge_Plot" + side, mirror, -7.8f, -7.5f, -55.8f, -41.7f));
            }
            edges.Add(Edge("Edge_PlotSouth", 1f, -7.8f, 7.8f, -55.8f, -55.5f));
            return edges.ToArray();
        }

        // Build order: slabs, structures, fills, edges.
        public static IEnumerable<TownBlock> AllBlocks => Slabs.Concat(Structures).Concat(Fills).Concat(Edges);

        public static TownBlock FindBlock(string name)
        {
            foreach (var block in AllBlocks)
                if (block.Name == name) return block;
            throw new ArgumentException("no town block " + name, nameof(name));
        }

        private static TownBlock Slab(string name, Vector2 xz, Vector2 size) =>
            new TownBlock(name,
                new Vector3(xz.x, WalkSurfaceY - SlabThickness / 2f, xz.y),
                new Vector3(size.x, SlabThickness, size.y), BlockMaterial.Sand, true);

        private static TownBlock Fill(string name, Vector2 xz, Vector2 size) =>
            new TownBlock(name,
                new Vector3(xz.x, WalkSurfaceY - FillThickness / 2f, xz.y),
                new Vector3(size.x, FillThickness, size.y), BlockMaterial.Sand, true);

        // Stands on the void side of a walk edge, from the walk surface up EdgeHeight.
        private static TownBlock Edge(string name, float mirror, float minX, float maxX, float minZ, float maxZ) =>
            new TownBlock(name,
                new Vector3(mirror * (minX + maxX) / 2f, WalkSurfaceY + EdgeHeight / 2f, (minZ + maxZ) / 2f),
                new Vector3(maxX - minX, EdgeHeight, maxZ - minZ), BlockMaterial.Building, true);

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

            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var parent = root.transform;
            var sand = MaterialFromScene(scene, "Beach_Platform");
            var building = MaterialFromScene(scene, "Wall_S") ?? sand;

            foreach (var block in AllBlocks)
                Block(parent, block.Name, block.Center, block.Size,
                    block.Material == BlockMaterial.Building ? building : sand, block.HasCollider);

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

        // Empty transforms: no collider, renderer or NetworkObject. HomeSceneSetup consumes
        // beds/storage/PC; SessionNetworkAdapter consumes the home spawns for entry and mornings.
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
