using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.World.Editor
{
    // Authors the P4.3-B2 (#108) half of DiveTestArea: the reef shelf and the deep basin north of the P3 arena,
    // the reef and deep anchorages, the two routes that reach them, the water and walls grown to hold them, and the
    // north surface lid. Same region (DiveTestAreaRegion), same single SwimVolume, same water line.
    //
    // Like the P3.1/P3.3 scripts it never destroys anything - not an object, not a component. A missing piece is
    // created and an existing one is retuned in place, so a second run leaves the scene byte for byte identical
    // (ApplyAndVerify checks exactly that). Objects other scripts own are retuned only where this phase has to:
    // SwimVolume (transform only - one water authority, grown, never a second volume) and the P1 arena walls
    // (moved/stretched to enclose the new water). The near route, the jetty and the beach are read, never written.
    //
    // Depth profile (surface y = 8; DiveDepthBands: shallow 0-8, reef (8,20), deep 20-35):
    //   z -15..15  P3 arena, sea bed y 0   ->  8 m  shallow (untouched)
    //   z  15..19  reef drop-off, y 0 -> -8
    //   z  19..40  reef shelf, y -8         -> 16 m  reef; three reef heads up to y -3 (11 m)
    //   z  40..46  deep drop-off, y -8 -> -26
    //   z  46..65  deep basin, y -26        -> 34 m  deep (1 m inside the 35 m edge; nothing reachable is deeper)
    public static class DiveTestAreaReefDeepSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        public const string ReefRouteName = "Route_Reef";
        public const string DeepRouteName = "Route_Deep";
        public const string ReefAnchorName = "Anchor_Reef";
        public const string DeepAnchorName = "Anchor_Deep";
        public const string ReefDropOffName = "Reef_DropOff";
        public const string ReefShelfName = "Reef_Shelf";
        public const string ReefHeadPrefix = "Reef_Head_";
        public const string DeepDropOffName = "Deep_DropOff";
        public const string DeepBasinName = "Deep_Basin";
        public const string NorthSurfaceName = "WaterSurface_North";

        private const string ShoreMaterialPath = "Assets/DeepDive/World/Art/ShoreMaterial.mat";

        // --- Water -----------------------------------------------------------------------------------

        public const float SurfaceY = 8f;

        // The one SwimVolume, grown in place: footprint = the region, top still y 8, bottom y -27 (35 m) so the
        // whole basin column is inside it for SwimVolume.Contains (fish bounds, the legacy locomotion fallback).
        public static readonly Vector3 SwimCenter = new Vector3(0f, -9.5f, 25f);
        public static readonly Vector3 SwimSize = new Vector3(30f, 35f, 80f);

        // --- Walls (P1 greybox, retuned) ------------------------------------------------------------------

        // Same 0.5 m thickness and top y 7 as before; they now reach y -27 and enclose z -15..65. Wall_S is untouched.
        private const float WallTopY = 7f;
        private const float WallBottomY = -27f;
        private const float WallThickness = 0.5f;
        public static readonly Vector3 WallNorthCenter = new Vector3(0f, (WallTopY + WallBottomY) * 0.5f, DiveTestAreaRegion.MaxZ);
        public static readonly Vector3 WallNorthSize = new Vector3(30f, WallTopY - WallBottomY, WallThickness);
        public static readonly Vector3 WallEastCenter = new Vector3(15f, (WallTopY + WallBottomY) * 0.5f, 25f);
        public static readonly Vector3 WallWestCenter = new Vector3(-15f, (WallTopY + WallBottomY) * 0.5f, 25f);
        public static readonly Vector3 WallSideSize = new Vector3(WallThickness, WallTopY - WallBottomY, 80f);

        // --- Sea bed --------------------------------------------------------------------------------------

        public const float FloorThickness = 1f;
        public const float FloorWidth = 30f;
        public const float ReefShelfTopY = -8f;
        public const float DeepBasinTopY = -26f;
        public const float ReefDropOffStartZ = 15f;
        public const float ReefShelfStartZ = 19f;
        public const float DeepDropOffStartZ = 40f;
        public const float DeepBasinStartZ = 46f;
        public const float ArenaSeaBedY = 0f;

        // Reef heads: scenery on the shelf, tops 5 m above it (11 m deep, still reef), all clear of the reef
        // anchorage's water column and far below any hull.
        public static readonly Vector3[] ReefHeadCenters =
        {
            new Vector3(-8f, -5.5f, 24f),
            new Vector3(-5f, -5.5f, 35f),
            new Vector3(10f, -5.5f, 22f)
        };

        public static readonly Vector3[] ReefHeadSizes =
        {
            new Vector3(4f, 5f, 4f),
            new Vector3(3f, 5f, 5f),
            new Vector3(3f, 5f, 3f)
        };

        // --- North surface lid -----------------------------------------------------------------------------

        // P2's WaterSurface covers x/z -30..30. This one covers z 30..70 at the same height, so "up" reads as the
        // surface over the reef and deep anchorages too. Renderer only: no collider, no shadow.
        public static readonly Vector3 NorthSurfaceCenterXZ = new Vector3(0f, 0f, 50f);
        public static readonly Vector3 NorthSurfaceScale = new Vector3(6f, 1f, 4f);

        // --- Anchorages and routes ---------------------------------------------------------------------------

        public static readonly Vector3 ReefAnchorPosition = new Vector3(3f, SurfaceY, 30f);
        public static readonly Vector3 DeepAnchorPosition = new Vector3(0f, SurfaceY, 56f);

        // Beside the widest hull that may sail the route (the research vessel, half width 2.2), 0.8 m clear.
        public const float BoardingOffsetX = 3f;

        // WP_0 / WP_1 are the near route's berth and harbour gate, shared, so every vehicle docks at one place.
        public static readonly Vector3[] ReefWaypoints =
        {
            new Vector3(9f, SurfaceY, -0.4f),
            new Vector3(9f, SurfaceY, 3.5f),
            new Vector3(6f, SurfaceY, 14f),
            ReefAnchorPosition
        };

        public static readonly Vector3[] DeepWaypoints =
        {
            new Vector3(9f, SurfaceY, -0.4f),
            new Vector3(9f, SurfaceY, 3.5f),
            new Vector3(6f, SurfaceY, 14f),
            new Vector3(3f, SurfaceY, 36f),
            DeepAnchorPosition
        };

        // Base (vehicle-independent) times, authored here; the vehicle's speed factor is #107's. Written at the near
        // route's own base pace (8.900 m in 8 s = 1.1125 m/s): reef 31.099 m -> 27.95 -> 28 s, deep 57.248 m ->
        // 51.46 -> 52 s. The derivation is this comment, not code - World does not compute a boat's speed.
        public const float ReefBaseSeconds = 28f;
        public const float DeepBaseSeconds = 52f;

        // --- The largest hull, from Mehmet's own rule --------------------------------------------------------

        // Clearance envelope = hull + 0.25 m each side, the margin P3.3 froze for the rowboat (2.4 -> 2.9).
        public const float EnvelopeMargin = 0.25f;
        public const float WallMargin = 1f;
        public const float WallInnerX = 14.75f;
        public const float WallInnerMinZ = -14.75f;
        public const float WallInnerMaxZ = DiveTestAreaRegion.MaxZ - WallThickness * 0.5f;

        // The jetty's seaward end (Dock_Town centre z -5.25, length 3.5).
        private const float JettyEndZ = -3.5f;

        private const float DiverRadius = 0.35f;
        private const float DiverHeight = 1.8f;
        private const float DiverCentreY = 0.9f;

        [MenuItem("DeepDive/P4.3/Dive test area: reef and deep")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var swimBox = RequireSwimVolume();
            var field = Object.FindFirstObjectByType<WaterField>();
            if (field == null) throw new InvalidOperationException("P4_REEFDEEP_NO_WATERFIELD reason=run DeepDive/P3.1 first");

            var nearRoute = RequireRoot(scene, DiveTestAreaRouteSetup.RouteName,
                "P4_REEFDEEP_NO_NEAR_ROUTE reason=run DeepDive/P3.3 first").GetComponent<DiveRoutePath>();
            if (nearRoute == null) throw new InvalidOperationException("P4_REEFDEEP_NEAR_ROUTE_NO_PATH");
            nearRoute.Refresh();
            var dockAnchor = RequireRoot(scene, DiveTestAreaRouteSetup.DockAnchorName,
                "P4_REEFDEEP_NO_DOCK reason=run DeepDive/P3.3 first").GetComponent<RouteAnchor>();
            var floorMaterial = RequireRoot(scene, "SeaFloor", "P4_REEFDEEP_NO_SEAFLOOR")
                .GetComponent<Renderer>().sharedMaterial;
            var reefMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShoreMaterialPath);
            if (reefMaterial == null)
                throw new InvalidOperationException($"P4_REEFDEEP_NO_SHORE_MATERIAL path={ShoreMaterialPath}");
            var surfaceTemplate = RequireRoot(scene, "WaterSurface", "P4_REEFDEEP_NO_WATERSURFACE reason=run DeepDive/P2 first");

            RetuneSwimVolume(swimBox);
            RetuneWall(scene, "Wall_N", WallNorthCenter, WallNorthSize);
            RetuneWall(scene, "Wall_E", WallEastCenter, WallSideSize);
            RetuneWall(scene, "Wall_W", WallWestCenter, WallSideSize);

            EnsureSlope(scene, ReefDropOffName, ReefDropOffStartZ, ArenaSeaBedY, ReefShelfStartZ, ReefShelfTopY, floorMaterial);
            EnsureSlab(scene, ReefShelfName, ReefShelfStartZ, DeepDropOffStartZ, ReefShelfTopY, floorMaterial);
            EnsureSlope(scene, DeepDropOffName, DeepDropOffStartZ, ReefShelfTopY, DeepBasinStartZ, DeepBasinTopY, floorMaterial);
            EnsureSlab(scene, DeepBasinName, DeepBasinStartZ, DiveTestAreaRegion.MaxZ, DeepBasinTopY, floorMaterial);
            for (var i = 0; i < ReefHeadCenters.Length; i++)
                EnsureCube(scene, ReefHeadPrefix + i, ReefHeadCenters[i], ReefHeadSizes[i], Quaternion.identity, reefMaterial);

            EnsureNorthSurface(scene, surfaceTemplate);

            var reefAnchor = EnsureSeaAnchor(scene, ReefAnchorName, DiveRouteAnchors.ReefAnchorPoint, ReefAnchorPosition);
            var deepAnchor = EnsureSeaAnchor(scene, DeepAnchorName, DiveRouteAnchors.DeepAnchorPoint, DeepAnchorPosition);
            var reefRoute = EnsureRoute(scene, ReefRouteName, BoatTripIds.ReefRouteId, ReefWaypoints,
                DiveRouteAnchors.ReefAnchorPoint, VehicleClass.Motorboat, ReefBaseSeconds);
            var deepRoute = EnsureRoute(scene, DeepRouteName, BoatTripIds.DeepRouteId, DeepWaypoints,
                DiveRouteAnchors.DeepAnchorPoint, VehicleClass.ResearchBoat, DeepBaseSeconds);
            var region = EnsureRegion(scene);

            field.RebuildVolumes();
            Physics.SyncTransforms();

            VerifyWaterAuthority(scene, field, swimBox);
            VerifyNearRouteUntouched(nearRoute);
            foreach (var route in new[] { reefRoute, deepRoute })
            {
                VerifySharedHarbourExit(route, nearRoute);
                VerifyOneWaterLine(route);
                VerifyLargestHullClearsTheWorld(scene, route);
            }

            VerifyEndsAtAnchor(reefRoute, reefAnchor);
            VerifyEndsAtAnchor(deepRoute, deepAnchor);
            VerifyOpenWater(scene, field, reefAnchor, ReefShelfTopY, DepthBandIds.Reef);
            VerifyOpenWater(scene, field, deepAnchor, DeepBasinTopY, DepthBandIds.Deep);
            VerifyBandsAreMeasurable(field);
            VerifyRegion(region, swimBox, dockAnchor, nearRoute, reefRoute, deepRoute, reefAnchor, deepAnchor);
            LogBerthConstraint(nearRoute);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P4_REEFDEEP_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"P4_DIVEAREA_REEFDEEP_READY region={region.RegionId} " +
                $"bounds=x[{DiveTestAreaRegion.MinX},{DiveTestAreaRegion.MaxX}] z[{DiveTestAreaRegion.MinZ},{DiveTestAreaRegion.MaxZ}] " +
                $"reef={BoatTripIds.ReefRouteId}/{reefRoute.Waypoints.Count}wp/{ReefBaseSeconds}s " +
                $"deep={BoatTripIds.DeepRouteId}/{deepRoute.Waypoints.Count}wp/{DeepBaseSeconds}s " +
                $"swim={SwimCenter}/{SwimSize} waterVolumes={field.VolumeCount}");
        }

        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P4_REEFDEEP_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P4_REEFDEEP_SETUP_IDEMPOTENT");
        }

        // --- Placement ---------------------------------------------------------------------------------------

        private static void RetuneSwimVolume(BoxCollider swimBox)
        {
            // Transform only. The collider stays a unit box and SwimVolume's component is not touched.
            swimBox.transform.SetPositionAndRotation(SwimCenter, Quaternion.identity);
            swimBox.transform.localScale = SwimSize;
            if (swimBox.center != Vector3.zero || swimBox.size != Vector3.one)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_SWIMVOLUME_SHAPE center={swimBox.center} size={swimBox.size} reason=expected a unit box");
        }

        private static void RetuneWall(Scene scene, string name, Vector3 center, Vector3 size)
        {
            var wall = RequireRoot(scene, name, "P4_REEFDEEP_NO_WALL");
            if (wall.GetComponent<BoxCollider>() == null)
                throw new InvalidOperationException($"P4_REEFDEEP_WALL_NO_BOX wall={name}");
            wall.transform.SetPositionAndRotation(center, Quaternion.identity);
            wall.transform.localScale = size;
        }

        // A flat slab whose top is at topY, spanning z [fromZ, toZ] across the arena width.
        private static void EnsureSlab(Scene scene, string name, float fromZ, float toZ, float topY, Material material)
        {
            EnsureCube(scene, name,
                new Vector3(0f, topY - FloorThickness * 0.5f, (fromZ + toZ) * 0.5f),
                new Vector3(FloorWidth, FloorThickness, toZ - fromZ),
                Quaternion.identity, material);
        }

        // A tilted slab whose top face runs exactly from (fromZ, fromY) to (toZ, toY).
        private static void EnsureSlope(Scene scene, string name, float fromZ, float fromY, float toZ, float toY,
            Material material)
        {
            var run = toZ - fromZ;
            var drop = fromY - toY;
            var length = Mathf.Sqrt(run * run + drop * drop);
            var rotation = Quaternion.Euler(Mathf.Atan2(drop, run) * Mathf.Rad2Deg, 0f, 0f);
            var topMid = new Vector3(0f, (fromY + toY) * 0.5f, (fromZ + toZ) * 0.5f);
            var centre = topMid - rotation * Vector3.up * (FloorThickness * 0.5f);
            EnsureCube(scene, name, centre, new Vector3(FloorWidth, FloorThickness, length), rotation, material);
        }

        private static GameObject EnsureCube(Scene scene, string name, Vector3 center, Vector3 size,
            Quaternion rotation, Material material)
        {
            var piece = FindRoot(scene, name);
            if (piece == null)
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = name;
                SceneManager.MoveGameObjectToScene(piece, scene);
            }

            piece.layer = 0;
            piece.transform.SetPositionAndRotation(center, rotation);
            piece.transform.localScale = size;

            var box = Ensure<BoxCollider>(piece);
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = Vector3.one;

            var renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            RequireNotNetworked(piece, name);
            return piece;
        }

        // A renderer-only lid built from P2's own surface mesh and material - no primitive, so there is no collider to
        // remove, and nothing here destroys anything.
        private static void EnsureNorthSurface(Scene scene, GameObject template)
        {
            var templateFilter = template.GetComponent<MeshFilter>();
            var templateRenderer = template.GetComponent<MeshRenderer>();
            if (templateFilter == null || templateRenderer == null)
                throw new InvalidOperationException("P4_REEFDEEP_WATERSURFACE_NOT_A_MESH");

            var lid = EnsureBareRoot(scene, NorthSurfaceName);
            lid.transform.SetPositionAndRotation(
                new Vector3(NorthSurfaceCenterXZ.x, template.transform.position.y, NorthSurfaceCenterXZ.z),
                Quaternion.Euler(180f, 0f, 0f));
            lid.transform.localScale = NorthSurfaceScale;

            var filter = Ensure<MeshFilter>(lid);
            filter.sharedMesh = templateFilter.sharedMesh;
            var renderer = Ensure<MeshRenderer>(lid);
            renderer.sharedMaterial = templateRenderer.sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (lid.GetComponent<Collider>() != null)
                throw new InvalidOperationException("P4_REEFDEEP_LID_HAS_COLLIDER reason=the surface must never block a diver");
            RequireNotNetworked(lid, NorthSurfaceName);
        }

        private static RouteAnchor EnsureSeaAnchor(Scene scene, string name, string anchorId, Vector3 position)
        {
            var root = EnsureBareRoot(scene, name);
            root.transform.SetPositionAndRotation(position, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var boarding = EnsureChild(root, DiveTestAreaRouteSetup.BoardingName);
            boarding.transform.position = position + new Vector3(BoardingOffsetX, 0f, 0f);
            boarding.transform.rotation = Quaternion.identity;
            boarding.transform.localScale = Vector3.one;

            var anchor = Ensure<RouteAnchor>(root);
            anchor.Configure(anchorId, boarding.transform);
            EditorUtility.SetDirty(anchor);

            if (!anchor.IsContractAnchor || !DiveRouteAnchors.IsAnchorPoint(anchorId))
                throw new InvalidOperationException($"P4_REEFDEEP_UNKNOWN_ANCHOR object={name} anchorId={anchorId}");

            RequireNotNetworked(root, name);
            return anchor;
        }

        private static DiveRoutePath EnsureRoute(Scene scene, string name, string routeId, Vector3[] waypoints,
            string anchorId, VehicleClass requiredClass, float baseSeconds)
        {
            var root = EnsureBareRoot(scene, name);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var path = Ensure<DiveRoutePath>(root);
            path.Configure(routeId, DiveRouteAnchors.Dock, anchorId, requiredClass, baseSeconds, baseSeconds);

            if (root.transform.childCount > waypoints.Length)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_EXTRA_WAYPOINT object={name} children={root.transform.childCount} " +
                    $"expected={waypoints.Length} reason=remove the extra waypoint by hand");

            for (var i = 0; i < waypoints.Length; i++)
            {
                var waypoint = EnsureChild(root, DiveTestAreaRouteSetup.WaypointPrefix + i);
                waypoint.transform.position = waypoints[i];
                waypoint.transform.rotation = Quaternion.identity;
                waypoint.transform.localScale = Vector3.one;
                waypoint.transform.SetSiblingIndex(i);
            }

            path.Refresh();
            EditorUtility.SetDirty(path);

            if (!path.IsContractRoute || !path.IsUsable || !path.HasAuthoredDefinition)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_ROUTE_UNUSABLE routeId={path.RouteId} waypoints={path.Waypoints.Count}");

            RequireNotNetworked(root, name);
            return path;
        }

        private static DiveRegionField EnsureRegion(Scene scene)
        {
            var root = EnsureBareRoot(scene, DiveTestAreaRouteSetup.RegionName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var region = Ensure<DiveRegionField>(root);
            region.Configure(DiveTestAreaRegion.RegionId, DiveTestAreaRegion.MinX, DiveTestAreaRegion.MaxX,
                DiveTestAreaRegion.MinZ, DiveTestAreaRegion.MaxZ);
            EditorUtility.SetDirty(region);

            if (!region.Bounds.IsValid) throw new InvalidOperationException("P4_REEFDEEP_REGION_INVALID");
            RequireNotNetworked(root, DiveTestAreaRouteSetup.RegionName);
            return region;
        }

        // --- Author-time guards -----------------------------------------------------------------------------

        // One water authority, grown: exactly one volume and one body, its footprint the region, its top the
        // water line every depth band is measured from - and the P3.2 band set still agrees with it.
        private static void VerifyWaterAuthority(Scene scene, WaterField field, BoxCollider swimBox)
        {
            if (Object.FindObjectsByType<SwimVolume>(FindObjectsSortMode.None).Length != 1 ||
                field.VolumeCount != 1 || field.Bodies.Count != 1)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_WATER_AUTHORITY_CHANGED volumes={field.VolumeCount} bodies={field.Bodies.Count} " +
                    "reason=expected exactly one SwimVolume, grown in place");

            var body = field.Bodies[0];
            if (!Mathf.Approximately(body.SurfaceY, SurfaceY) ||
                !Mathf.Approximately(body.MinX, DiveTestAreaRegion.MinX) || !Mathf.Approximately(body.MaxX, DiveTestAreaRegion.MaxX) ||
                !Mathf.Approximately(body.MinZ, DiveTestAreaRegion.MinZ) || !Mathf.Approximately(body.MaxZ, DiveTestAreaRegion.MaxZ))
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_WATER_FOOTPRINT surfaceY={body.SurfaceY} x[{body.MinX},{body.MaxX}] z[{body.MinZ},{body.MaxZ}]");

            var bottom = BoundsOf(swimBox).min.y;
            if (bottom > SurfaceY - DiveDepthBands.DeepMaxDepth + 0.001f)
                throw new InvalidOperationException($"P4_REEFDEEP_WATER_TOO_SHALLOW bottom={bottom}");

            var bands = Object.FindObjectsByType<DiveDepthBandSet>(FindObjectsSortMode.None);
            if (bands.Length != 1 || !Mathf.Approximately(bands[0].SurfaceY, SurfaceY))
                throw new InvalidOperationException("P4_REEFDEEP_BANDSET_SURFACE reason=DiveDepthBandSet must still read y 8");
        }

        private static void VerifyNearRouteUntouched(DiveRoutePath near)
        {
            var expected = DiveTestAreaRouteSetup.WaypointPositions;
            if (near.RouteId != BoatTripIds.NearRouteId || near.Waypoints.Count != expected.Length)
                throw new InvalidOperationException("P4_REEFDEEP_NEAR_ROUTE_CHANGED");
            for (var i = 0; i < expected.Length; i++)
                if ((near.Waypoints[i] - expected[i]).sqrMagnitude > 1e-6f)
                    throw new InvalidOperationException($"P4_REEFDEEP_NEAR_ROUTE_CHANGED index={i} at={near.Waypoints[i]}");
            var definition = near.ToDefinition();
            if (definition.RequiredVehicleClass != VehicleClass.Rowboat ||
                definition.AnchorPointAnchor != DiveRouteAnchors.AnchorPoint ||
                !Mathf.Approximately(definition.OutboundSeconds, DiveRoutePath.NominalOutboundSeconds))
                throw new InvalidOperationException("P4_REEFDEEP_NEAR_DEFINITION_CHANGED");
        }

        private static void VerifySharedHarbourExit(DiveRoutePath route, DiveRoutePath near)
        {
            for (var i = 0; i < 2; i++)
                if ((route.Waypoints[i] - near.Waypoints[i]).sqrMagnitude > 1e-6f)
                    throw new InvalidOperationException(
                        $"P4_REEFDEEP_HARBOUR_EXIT routeId={route.RouteId} index={i} at={route.Waypoints[i]} " +
                        $"near={near.Waypoints[i]} reason=every route leaves from the one berth and gate");
        }

        private static void VerifyOneWaterLine(DiveRoutePath route)
        {
            for (var i = 0; i < route.Waypoints.Count; i++)
                if (!Mathf.Approximately(route.Waypoints[i].y, SurfaceY))
                    throw new InvalidOperationException(
                        $"P4_REEFDEEP_WAYPOINT_OFF_WATER routeId={route.RouteId} index={i} y={route.Waypoints[i].y}");
        }

        private static void VerifyEndsAtAnchor(DiveRoutePath route, RouteAnchor anchor)
        {
            var last = route.Waypoints[route.Waypoints.Count - 1];
            if ((last - anchor.WorldPosition).sqrMagnitude > 1e-4f || route.ToDefinition().AnchorPointAnchor != anchor.AnchorId)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_ANCHOR_MISMATCH routeId={route.RouteId} last={last} anchor={anchor.AnchorId}@{anchor.WorldPosition}");
        }

        // The research vessel may sail every route (activeClass >= required), so every route is swept with its
        // envelope. Leg 0 (berth -> gate) is the shared near-route leg whose berth depends on the hull: that is the
        // measured constraint LogBerthConstraint reports and #107's mover applies, not something a route can fix.
        private static void VerifyLargestHullClearsTheWorld(Scene scene, DiveRoutePath route)
        {
            var envelope = LargestEnvelope(out var halfWidth, out var halfLength);
            var obstacles = Obstacles(scene);
            var turn = Mathf.Sqrt(halfWidth * halfWidth + halfLength * halfLength);

            for (var leg = 1; leg + 1 < route.Waypoints.Count; leg++)
            {
                var corners = SweptEnvelope(route.Waypoints[leg], route.Waypoints[leg + 1], halfWidth, halfLength);
                foreach (var obstacle in obstacles)
                    if (Overlaps(corners, obstacle.Value))
                        throw new InvalidOperationException(
                            $"P4_REEFDEEP_LEG_BLOCKED routeId={route.RouteId} leg={leg} obstacle={obstacle.Key} hull={envelope}");

                foreach (var corner in corners)
                    if (Mathf.Abs(corner.x) > WallInnerX - WallMargin ||
                        corner.z < WallInnerMinZ + WallMargin || corner.z > WallInnerMaxZ - WallMargin)
                        throw new InvalidOperationException(
                            $"P4_REEFDEEP_LEG_NEAR_WALL routeId={route.RouteId} leg={leg} corner={corner} hull={envelope}");
            }

            for (var i = 1; i + 1 < route.Waypoints.Count; i++)
                foreach (var obstacle in obstacles)
                {
                    var gap = FlatDistanceToBounds(route.Waypoints[i], obstacle.Value);
                    if (gap < turn)
                        throw new InvalidOperationException(
                            $"P4_REEFDEEP_TURN_BLOCKED routeId={route.RouteId} index={i} obstacle={obstacle.Key} gap={gap} need={turn}");
                }
        }

        // Open water at the anchorage, measured the P3.3 way, against this anchorage's own sea bed: surface just
        // under the line, underwater mid-column and on the bottom, nothing solid in the largest hull's footprint
        // above the bed, and the bed itself in the band the anchorage is for.
        private static void VerifyOpenWater(Scene scene, WaterField field, RouteAnchor anchor, float seaBedY,
            string expectedBottomBand)
        {
            var tracker = field.CreateTracker();
            var at = anchor.WorldPosition;

            var floating = Classify(tracker, new Vector3(at.x, SurfaceY - 1f, at.z));
            if (floating != EnvironmentLocomotion.Surface)
                throw new InvalidOperationException($"P4_REEFDEEP_ANCHOR_NOT_WATER anchor={anchor.AnchorId} classified={floating}");

            foreach (var feet in new[] { (SurfaceY + seaBedY) * 0.5f, seaBedY + 0.1f })
            {
                var state = Classify(tracker, new Vector3(at.x, feet, at.z));
                if (state != EnvironmentLocomotion.Underwater)
                    throw new InvalidOperationException(
                        $"P4_REEFDEEP_ANCHOR_NOT_WATER anchor={anchor.AnchorId} feetY={feet} classified={state}");
            }

            var boarding = Classify(tracker, new Vector3(anchor.BoardingPosition.x, SurfaceY - 1f, anchor.BoardingPosition.z));
            if (boarding != EnvironmentLocomotion.Surface)
                throw new InvalidOperationException($"P4_REEFDEEP_BOARDING_NOT_WATER anchor={anchor.AnchorId} classified={boarding}");

            LargestEnvelope(out var halfWidth, out var halfLength);
            var column = new Bounds(new Vector3(at.x, 0f, at.z), new Vector3(halfWidth * 2f, 1f, halfLength * 2f));
            foreach (var collider in SolidColliders(scene))
            {
                var bounds = collider.bounds;
                if (bounds.max.y <= seaBedY + 0.01f) continue;
                if (!FlatIntersects(column, bounds)) continue;
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_ANCHOR_HAS_LAND anchor={anchor.AnchorId} collider={collider.name} bounds={bounds}");
            }

            if (!WaterDepth.TryClassify(field.Bodies, new Vector3(at.x, seaBedY + 0.1f, at.z), out var band) ||
                band != expectedBottomBand)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_BOTTOM_BAND anchor={anchor.AnchorId} band={band} expected={expectedBottomBand}");
        }

        // All three bands can be reached in one descent at the deep anchorage, and nothing reachable is past 35 m.
        private static void VerifyBandsAreMeasurable(WaterField field)
        {
            var x = DeepAnchorPosition.x;
            var z = DeepAnchorPosition.z;
            var expected = new (float Y, string Band)[]
            {
                (SurfaceY - 4f, DepthBandIds.Shallow),
                (SurfaceY - 14f, DepthBandIds.Reef),
                (SurfaceY - 25f, DepthBandIds.Deep),
                (DeepBasinTopY, DepthBandIds.Deep)
            };

            foreach (var (y, band) in expected)
                if (!WaterDepth.TryClassify(field.Bodies, new Vector3(x, y, z), out var found) || found != band)
                    throw new InvalidOperationException($"P4_REEFDEEP_BAND_NOT_MEASURABLE y={y} found={found} expected={band}");

            if (SurfaceY - DeepBasinTopY > DiveDepthBands.DeepMaxDepth)
                throw new InvalidOperationException($"P4_REEFDEEP_BASIN_TOO_DEEP depth={SurfaceY - DeepBasinTopY}");
            if (SurfaceY - ReefShelfTopY >= DiveDepthBands.ReefMaxDepth || SurfaceY - ReefShelfTopY <= DiveDepthBands.ReefMinDepth)
                throw new InvalidOperationException($"P4_REEFDEEP_SHELF_NOT_REEF depth={SurfaceY - ReefShelfTopY}");
        }

        private static void VerifyRegion(DiveRegionField region, BoxCollider swimBox, RouteAnchor dockAnchor,
            DiveRoutePath near, DiveRoutePath reef, DiveRoutePath deep, RouteAnchor reefAnchor, RouteAnchor deepAnchor)
        {
            var water = BoundsOf(swimBox);
            foreach (var corner in new[]
                     {
                         new Vector3(water.min.x, 0f, water.min.z), new Vector3(water.max.x, 0f, water.min.z),
                         new Vector3(water.min.x, 0f, water.max.z), new Vector3(water.max.x, 0f, water.max.z)
                     })
                if (!region.Bounds.Contains(corner.x, corner.z))
                    throw new InvalidOperationException($"P4_REEFDEEP_REGION_TOO_SMALL corner={corner}");

            foreach (var route in new[] { near, reef, deep })
                foreach (var point in route.Waypoints)
                    region.WorldToMapChecked(point);
            foreach (var anchor in new[] { dockAnchor, reefAnchor, deepAnchor })
            {
                region.WorldToMapChecked(anchor.WorldPosition);
                region.WorldToMapChecked(anchor.BoardingPosition);
            }
        }

        // H (#107, Mehmet): the shared berth WP_0 fits the rowboat; a longer hull docked there would reach into the
        // jetty. Reported as the measured numbers the mover needs, never "fixed" here by moving the frozen berth.
        private static void LogBerthConstraint(DiveRoutePath near)
        {
            var berth = near.Waypoints[0];
            foreach (BoatHullKind kind in Enum.GetValues(typeof(BoatHullKind)))
            {
                if (!BoatHullSeatRules.TryGetDimensions(kind, out var hull)) continue;
                var stern = berth.z - hull.Length * 0.5f;
                var needCentreZ = JettyEndZ + DiveTestAreaRouteSetup.MinBerthGap + hull.Length * 0.5f;
                Debug.Log(
                    $"P4_ROUTE_BERTH_CONSTRAINT hull={kind} length={hull.Length} berthZ={berth.z} sternZ={stern} " +
                    $"jettyEndZ={JettyEndZ} minGap={DiveTestAreaRouteSetup.MinBerthGap} needCentreZ>={needCentreZ} " +
                    $"fitsSharedBerth={berth.z >= needCentreZ - 0.0001f}");
            }
        }

        // --- Geometry --------------------------------------------------------------------------------------------

        private static string LargestEnvelope(out float halfWidth, out float halfLength)
        {
            if (!BoatHullSeatRules.TryGetDimensions(BoatHullKind.ResearchVessel, out var hull))
                throw new InvalidOperationException("P4_REEFDEEP_NO_RESEARCH_HULL");
            halfWidth = hull.Width * 0.5f + EnvelopeMargin;
            halfLength = hull.Length * 0.5f + EnvelopeMargin;
            return $"{halfWidth * 2f}x{halfLength * 2f}";
        }

        // Solid colliders that reach a floating hull (top above the waterline minus the frozen P3.3 draft). The sea
        // bed, the reef heads and the walls (top y 7) fall out; the walls are covered by the lateral check instead.
        private static Dictionary<string, Bounds> Obstacles(Scene scene)
        {
            var obstacles = new Dictionary<string, Bounds>();
            foreach (var collider in SolidColliders(scene))
            {
                if (collider.bounds.max.y <= SurfaceY - DiveTestAreaRouteSetup.HullDraft) continue;
                var key = collider.name;
                for (var i = 2; obstacles.ContainsKey(key); i++) key = collider.name + "#" + i;
                obstacles[key] = collider.bounds;
            }

            return obstacles;
        }

        private static Vector3[] SweptEnvelope(Vector3 from, Vector3 to, float halfWidth, float halfLength)
        {
            var direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
            var length = direction.magnitude;
            if (length <= Mathf.Epsilon)
                throw new InvalidOperationException($"P4_REEFDEEP_ZERO_LEG from={from} to={to}");

            var forward = direction / length;
            var right = new Vector3(forward.z, 0f, -forward.x);
            var centre = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);
            var reach = length * 0.5f + halfLength;
            return new[]
            {
                centre - forward * reach - right * halfWidth,
                centre - forward * reach + right * halfWidth,
                centre + forward * reach + right * halfWidth,
                centre + forward * reach - right * halfWidth
            };
        }

        private static bool Overlaps(Vector3[] corners, Bounds bounds)
        {
            var axes = new[]
            {
                (corners[1] - corners[0]).normalized, (corners[3] - corners[0]).normalized, Vector3.right, Vector3.forward
            };
            var box = new[]
            {
                new Vector3(bounds.min.x, 0f, bounds.min.z), new Vector3(bounds.max.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.max.z), new Vector3(bounds.min.x, 0f, bounds.max.z)
            };

            foreach (var axis in axes)
            {
                Project(corners, axis, out var minA, out var maxA);
                Project(box, axis, out var minB, out var maxB);
                if (maxA < minB || maxB < minA) return false;
            }

            return true;
        }

        private static void Project(Vector3[] points, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var point in points)
            {
                var value = point.x * axis.x + point.z * axis.z;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        private static float FlatDistanceToBounds(Vector3 point, Bounds bounds)
        {
            var dx = Mathf.Max(bounds.min.x - point.x, 0f, point.x - bounds.max.x);
            var dz = Mathf.Max(bounds.min.z - point.z, 0f, point.z - bounds.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Bounds BoundsOf(BoxCollider box) =>
            new Bounds(box.transform.TransformPoint(box.center), Vector3.Scale(box.size, box.transform.lossyScale));

        private static bool FlatIntersects(Bounds a, Bounds b) =>
            a.min.x < b.max.x && b.min.x < a.max.x && a.min.z < b.max.z && b.min.z < a.max.z;

        private static IEnumerable<Collider> SolidColliders(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (!collider.isTrigger)
                        yield return collider;
        }

        private static EnvironmentLocomotion Classify(IWaterField tracker, Vector3 feet) =>
            tracker.Classify(new WaterProbe(feet, new Vector3(0f, DiverCentreY, 0f), DiverRadius, DiverHeight, Vector3.up));

        // --- Lookup ----------------------------------------------------------------------------------------------

        private static BoxCollider RequireSwimVolume()
        {
            var swims = Object.FindObjectsByType<SwimVolume>(FindObjectsSortMode.None);
            if (swims.Length != 1)
                throw new InvalidOperationException($"P4_REEFDEEP_SWIMVOLUME count={swims.Length} reason=expected exactly one");
            var box = swims[0].GetComponent<BoxCollider>();
            if (box == null) throw new InvalidOperationException("P4_REEFDEEP_SWIMVOLUME_NO_BOX");
            return box;
        }

        // Explicit null check: Unity's fake-null GetComponent result is not C# null, so `??` never adds.
        private static T Ensure<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static GameObject EnsureBareRoot(Scene scene, string name)
        {
            var root = FindRoot(scene, name);
            if (root == null)
            {
                root = new GameObject(name);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            root.layer = 0;
            return root;
        }

        private static GameObject EnsureChild(GameObject parent, string name)
        {
            var matches = new List<Transform>();
            for (var i = 0; i < parent.transform.childCount; i++)
            {
                var child = parent.transform.GetChild(i);
                if (child.name == name) matches.Add(child);
            }

            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"P4_REEFDEEP_DUPLICATE_CHILD parent={parent.name} name={name} count={matches.Count}");
            if (matches.Count == 1)
            {
                matches[0].gameObject.layer = 0;
                return matches[0].gameObject;
            }

            var created = new GameObject(name) { layer = 0 };
            created.transform.SetParent(parent.transform, true);
            return created;
        }

        private static GameObject RequireRoot(Scene scene, string name, string failure)
        {
            var root = FindRoot(scene, name);
            if (root == null) throw new InvalidOperationException($"{failure} missing={name}");
            return root;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"P4_REEFDEEP_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }

        private static void RequireNotNetworked(GameObject piece, string name)
        {
            if (piece.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException($"P4_REEFDEEP_NETWORKED object={name}");
        }
    }
}
