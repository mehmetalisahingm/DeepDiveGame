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

namespace DeepDive.World.Editor
{
    // Authors the P3.3 half of DiveTestArea: the jetty a diver walks out on, the berth the boat
    // waits at, the near route it sails, the offshore anchorage it stops at, and the region the
    // map's 0..1 square covers.
    //
    // Separate from DiveTestAreaSetup (P2-B), DiveTestAreaWaterSetup (P3.1) and
    // DiveTestAreaBeachSetup (P3.2) the same way those are separate from each other: its menu
    // path, its log tags and its comments all say P3.3, and phases are tracked file by file.
    //
    // It never destroys anything - not an object, not a component, not a stray waypoint. A
    // missing piece is created and an existing one is retuned in place, so a re-run repairs
    // rather than rewrites and the scene file comes back byte for byte identical (ApplyAndVerify
    // checks exactly that). Where a re-run cannot repair - an extra WP_ child from an older,
    // longer route - it stops and says so instead of quietly sailing through it.
    //
    // Two things it deliberately does NOT do:
    //
    //   - It does not make the jetty a safe return. The narrowed return pad on Shore_Ledge
    //     (Composition's SafeReturnZone) stays the only one; the jetty ends 0.25 m short of it,
    //     so a diver walks off the boat, along the deck and onto the pad. A second zone here
    //     would be a second authority over "this catch is banked".
    //   - It does not build land at the anchorage. The boat stops in real water with the sea bed
    //     8 m below it; a wade shelf out there would turn an offshore stop into a beach and make
    //     the trip pointless.
    //
    // Nothing here opens WaterField. Its volumes array keeps its single SwimVolume and stays the
    // one authority over Land/Surface/Underwater; this script only reads the classification to
    // prove the anchorage is wet.
    public static class DiveTestAreaRouteSetup
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        public const string DockName = "Dock_Town";
        public const string DockAnchorName = "Dock_Town_Anchor";
        public const string RouteName = "Route_Near";
        public const string AnchorName = "Anchor_Near";
        public const string BoardingName = "Boarding";
        public const string RegionName = "DiveRegion";
        public const string WaypointPrefix = "WP_";

        private const string ShoreMaterialPath = "Assets/DeepDive/World/Art/ShoreMaterial.mat";

        // --- The jetty -------------------------------------------------------------------------

        // Flush with Shore_Ledge's north face (z = -7) and with its top (8.4), so stepping
        // between the two is not a step at all. 2 m wide inside the ledge's 4, centred on the
        // pad's own centre line (x = 9), which is what "the dock lands on the return pad" means
        // in geometry rather than in prose.
        public static readonly Vector3 DockCenter = new Vector3(9f, 8.2f, -5.25f);
        public static readonly Vector3 DockSize = new Vector3(2f, 0.4f, 3.5f);

        // The seaward end of the deck, 0.2 m in from its edge: where a player stands to board.
        public static readonly Vector3 DockAnchorPosition = new Vector3(9f, 8.4f, -3.7f);

        // --- The route -------------------------------------------------------------------------

        // WP_0 is the berth, WP_2 is the anchorage, WP_1 is the gate between them: it is what
        // makes the inbound leg approach the jetty dead astern instead of angling in, which is
        // the difference between a boat that berths and a boat that scrapes.
        //
        // All three share one y, and it is checked against SwimVolume's own surface rather than
        // trusted - a route with a step in it would make the hull hop as it passed a waypoint.
        public static readonly Vector3[] WaypointPositions =
        {
            new Vector3(9f, 8f, -0.4f),
            new Vector3(9f, 8f, 3.5f),
            new Vector3(9f, 8f, 8.5f)
        };

        // The anchorage is the last waypoint, not a place near it: Mehmet's mover reports arrival
        // when it reaches the route's end, and Mert's map draws the anchor icon there. Two
        // slightly different points would put the icon where the boat is not.
        public static Vector3 AnchorPosition => WaypointPositions[WaypointPositions.Length - 1];

        // Beside the hull, not inside it. A diver surfaces here to climb aboard; the hull's east
        // side is at x = 10.2, so this is 0.8 m of open water away from it.
        public static readonly Vector3 BoardingPosition = new Vector3(11f, 8f, 8.5f);

        // --- The region ------------------------------------------------------------------------

        public const float RegionExtent = 15f;
        public const string RegionId = "region-near-1";

        // --- The hull the route has to fit ------------------------------------------------------

        // The starting boat, frozen with Mehmet: hull 2.4 x 5.0, clearance envelope 2.90 x 5.50.
        // The hull halves are used where a real gap is measured (does the berthed boat touch the
        // jetty); the envelope halves are used for the swept corridor, where the margin belongs.
        public const float HullHalfWidth = 1.2f;
        public const float HullHalfLength = 2.5f;
        public const float EnvelopeHalfWidth = 1.45f;
        public const float EnvelopeHalfLength = 2.75f;

        // How deep below the waterline the hull sits. Anything whose top is under this cannot
        // touch the boat, which is what keeps the sea bed and the arena walls (top y = 7) out of
        // the obstacle set; the walls are covered by the arena inset rule instead.
        public const float HullDraft = 0.5f;

        // Turning happens at interior waypoints, and a turning hull sweeps its circumscribed
        // circle. WP_0 is exempt by design: the boat does not pivot at the berth, it leaves on a
        // fixed heading, and the gap that matters there is the measured stern gap below - which
        // is 0.60 m and would fail a 3.11 m circle test that does not apply to it.
        public static float TurnClearance =>
            Mathf.Sqrt(EnvelopeHalfWidth * EnvelopeHalfWidth + EnvelopeHalfLength * EnvelopeHalfLength);

        // The berthed hull must not touch the jetty, and must not sit so close that a player
        // cannot tell boat from deck. The authored berth gives 0.60 m.
        public const float MinBerthGap = 0.5f;

        // Room between a waypoint's hull and the arena's inner wall face.
        public const float WallMargin = 1f;
        public const float ArenaInnerExtent = 14.75f;

        // --- Copied numbers ---------------------------------------------------------------------

        // NetworkDiver's CharacterController, copied because DeepDive.World.Editor does not
        // reference the prefab and should not start. Used only to ask WaterField what a diver
        // floating at the anchorage would classify as.
        private const float DiverRadius = 0.35f;
        private const float DiverHeight = 1.8f;
        private const float DiverCentreY = 0.9f;

        // The narrowed return pad, as PR #70 left it and PR #78 kept it. Copied as plain numbers
        // because this assembly does not reference DeepDive.Composition; re-stated here so that
        // moving the pad fails this script at the moment it is run, rather than silently letting
        // the jetty grow into a second safe return.
        private static readonly Vector3 ExitCenter = new Vector3(9f, 9.3f, -9f);
        private static readonly Vector3 ExitSize = new Vector3(3.5f, 2f, 3.5f);

        private const float SeaBedY = 0f;

        [MenuItem("DeepDive/P3.3/Dive test area: dock, route and anchor")]
        public static void Apply()
        {
            // Reused, never created. P3.1 authors this material; if it is missing then the shore
            // this jetty attaches to is missing too, and creating a second one would hide that.
            var shoreMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShoreMaterialPath);
            if (shoreMaterial == null)
                throw new InvalidOperationException(
                    $"P3_ROUTE_NO_SHORE_MATERIAL path={ShoreMaterialPath} " +
                    "reason=run DeepDive/P3.1/Dive test area first");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var swimBox = RequireSwimVolume();
            var surfaceY = SurfaceOf(swimBox);

            var field = UnityEngine.Object.FindFirstObjectByType<WaterField>();
            if (field == null) throw new InvalidOperationException("P3_ROUTE_NO_WATERFIELD");

            var exitBox = RequireExitZone(scene);
            RequireRoot(scene, DiveTestAreaWaterSetup.LedgeName,
                "P3_ROUTE_NO_SHORE reason=run DeepDive/P3.1/Dive test area first");
            RequireRoot(scene, DiveTestAreaBeachSetup.PlatformName,
                "P3_ROUTE_NO_BEACH reason=run DeepDive/P3.2/Dive test area first");

            var dock = EnsureDock(scene, shoreMaterial);
            var dockAnchor = EnsureAnchor(scene, DockAnchorName, DiveRouteAnchors.Dock, DockAnchorPosition);
            var route = EnsureRoute(scene);
            var seaAnchor = EnsureSeaAnchor(scene);
            var region = EnsureRegion(scene);

            VerifyTheReturnPadIsUntouched(exitBox);
            VerifyTheWaypointsRideOneWaterLine(route, surfaceY);
            VerifyTheRouteEndsAtTheRealDockAndAnchor(route, dockAnchor, seaAnchor);
            VerifyNoHullSweepTouchesTheWorld(scene, route, surfaceY);
            VerifyTheBerthedHullClearsTheJetty(dock);
            VerifyEveryWaypointStaysInsideTheWalls(route);
            VerifyNothingReachesTheReturnPad(exitBox, dock, route);
            VerifyTheAnchorageIsRealWater(scene, field, surfaceY);
            VerifyTheRegionCoversTheWater(region, swimBox, route, dockAnchor, seaAnchor);
            VerifyWaterAuthorityUntouched(field, swimBox);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"P3_ROUTE_SAVE_FAILED scene={ScenePath}");
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"P3_DIVEAREA_ROUTE_READY dock={DockCenter} size={DockSize} " +
                $"dockAnchor={DockAnchorPosition} route={BoatTripIds.NearRouteId} " +
                $"waypoints={route.Waypoints.Count} berth={WaypointPositions[0]} " +
                $"anchor={AnchorPosition} boarding={BoardingPosition} " +
                $"region={RegionId} extent={RegionExtent} surfaceY={surfaceY} " +
                $"waterVolumes={field.VolumeCount}");
        }

        // Byte-idempotence is the real contract: DiveTestArea is shared ground, Unity YAML merges
        // badly, and a second run that renumbered fileIDs would turn a repair into a conflict.
        public static void ApplyAndVerify()
        {
            Apply();
            var bytes = File.ReadAllBytes(ScenePath);
            Apply();
            if (!bytes.SequenceEqual(File.ReadAllBytes(ScenePath)))
                throw new InvalidOperationException("P3_ROUTE_SETUP_NOT_IDEMPOTENT");
            Debug.Log("P3_ROUTE_SETUP_IDEMPOTENT");
        }

        // --- Placement ---------------------------------------------------------------------------

        private static GameObject EnsureDock(UnityEngine.SceneManagement.Scene scene, Material material)
        {
            var piece = FindRoot(scene, DockName);
            if (piece == null)
            {
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = DockName;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(piece, scene);
            }

            piece.layer = 0;
            piece.transform.SetPositionAndRotation(DockCenter, Quaternion.identity);
            piece.transform.localScale = DockSize;

            // Solid, not a trigger: the whole point is that a player walks on it.
            var box = piece.GetComponent<BoxCollider>() ?? piece.AddComponent<BoxCollider>();
            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = Vector3.one;

            var renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            RequireNotNetworked(piece, DockName);
            return piece;
        }

        private static RouteAnchor EnsureAnchor(
            UnityEngine.SceneManagement.Scene scene, string name, string anchorId, Vector3 position)
        {
            var root = EnsureBareRoot(scene, name);
            root.transform.SetPositionAndRotation(position, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var anchor = root.GetComponent<RouteAnchor>() ?? root.AddComponent<RouteAnchor>();
            anchor.Configure(anchorId);
            EditorUtility.SetDirty(anchor);

            if (!anchor.IsContractAnchor)
                throw new InvalidOperationException(
                    $"P3_ROUTE_UNKNOWN_ANCHOR object={name} anchorId={anchorId}");

            RequireNotNetworked(root, name);
            return anchor;
        }

        // The sea anchor carries a boarding child; the dock anchor does not need one, because the
        // deck spot it already sits on is where a player stands.
        private static RouteAnchor EnsureSeaAnchor(UnityEngine.SceneManagement.Scene scene)
        {
            var anchor = EnsureAnchor(scene, AnchorName, DiveRouteAnchors.AnchorPoint, AnchorPosition);

            var boarding = EnsureChild(anchor.gameObject, BoardingName);
            boarding.transform.position = BoardingPosition;
            boarding.transform.rotation = Quaternion.identity;
            boarding.transform.localScale = Vector3.one;

            anchor.Configure(DiveRouteAnchors.AnchorPoint, boarding.transform);
            EditorUtility.SetDirty(anchor);
            return anchor;
        }

        private static DiveRoutePath EnsureRoute(UnityEngine.SceneManagement.Scene scene)
        {
            var root = EnsureBareRoot(scene, RouteName);

            // At the origin with no rotation, so a child's local position reads as its world
            // position in the inspector. The path reports world space either way; this only
            // makes the authored numbers the same numbers the documents quote.
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var path = root.GetComponent<DiveRoutePath>() ?? root.AddComponent<DiveRoutePath>();
            path.Configure(BoatTripIds.NearRouteId);

            // Nothing here destroys a child, so a leftover WP_3 from a longer route would quietly
            // become a fourth leg. Refusing is the repair a human has to make by hand.
            if (root.transform.childCount > WaypointPositions.Length)
                throw new InvalidOperationException(
                    $"P3_ROUTE_EXTRA_WAYPOINT object={RouteName} children={root.transform.childCount} " +
                    $"expected={WaypointPositions.Length} reason=remove the extra waypoint by hand");

            for (var i = 0; i < WaypointPositions.Length; i++)
            {
                var waypoint = EnsureChild(root, WaypointPrefix + i);
                waypoint.transform.position = WaypointPositions[i];
                waypoint.transform.rotation = Quaternion.identity;
                waypoint.transform.localScale = Vector3.one;

                // Sibling order is route order, so the order has to be authored, not hoped for.
                waypoint.transform.SetSiblingIndex(i);
            }

            path.Refresh();
            EditorUtility.SetDirty(path);

            if (!path.IsContractRoute || !path.IsUsable)
                throw new InvalidOperationException(
                    $"P3_ROUTE_UNUSABLE routeId={path.RouteId} waypoints={path.Waypoints.Count}");

            RequireNotNetworked(root, RouteName);
            return path;
        }

        private static DiveRegionField EnsureRegion(UnityEngine.SceneManagement.Scene scene)
        {
            var root = EnsureBareRoot(scene, RegionName);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var region = root.GetComponent<DiveRegionField>() ?? root.AddComponent<DiveRegionField>();
            region.Configure(RegionId, -RegionExtent, RegionExtent, -RegionExtent, RegionExtent);
            EditorUtility.SetDirty(region);

            if (!region.Bounds.IsValid)
                throw new InvalidOperationException($"P3_ROUTE_REGION_INVALID extent={RegionExtent}");

            RequireNotNetworked(root, RegionName);
            return region;
        }

        // Creates the child only when it is missing, and never renames or removes one, so a
        // re-run leaves the scene's fileIDs alone.
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
                    $"P3_ROUTE_DUPLICATE_CHILD parent={parent.name} name={name} count={matches.Count}");

            if (matches.Count == 1)
            {
                matches[0].gameObject.layer = 0;
                return matches[0].gameObject;
            }

            var created = new GameObject(name);
            created.layer = 0;
            created.transform.SetParent(parent.transform, true);
            return created;
        }

        // --- Author-time guards -------------------------------------------------------------------

        // The pad is Composition's and is not ours to move; this says out loud that it has not
        // moved under us, because every jetty number above is measured from it.
        private static void VerifyTheReturnPadIsUntouched(BoxCollider exitBox)
        {
            var centre = exitBox.transform.TransformPoint(exitBox.center);
            var size = Vector3.Scale(exitBox.size, exitBox.transform.lossyScale);

            if ((centre - ExitCenter).sqrMagnitude > 1e-4f || (size - ExitSize).sqrMagnitude > 1e-4f)
                throw new InvalidOperationException(
                    $"P3_ROUTE_RETURN_PAD_MOVED centre={centre} size={size} " +
                    $"expected centre={ExitCenter} size={ExitSize} " +
                    "reason=the jetty is aligned to this pad and must be re-measured");
        }

        // One water line for the whole route, and it is SwimVolume's, not a number typed twice.
        private static void VerifyTheWaypointsRideOneWaterLine(DiveRoutePath route, float surfaceY)
        {
            for (var i = 0; i < route.Waypoints.Count; i++)
            {
                if (Mathf.Approximately(route.Waypoints[i].y, surfaceY)) continue;
                throw new InvalidOperationException(
                    $"P3_ROUTE_WAYPOINT_OFF_WATER index={i} y={route.Waypoints[i].y} surfaceY={surfaceY} " +
                    "reason=a step in the route would make the hull hop");
            }
        }

        // The route has to end where the trip says it ends. The anchorage is the last waypoint
        // exactly; the dock anchor is on the deck above the berth, so it is checked in XZ within
        // one hull length - a player standing there can reach the boat.
        private static void VerifyTheRouteEndsAtTheRealDockAndAnchor(
            DiveRoutePath route, RouteAnchor dockAnchor, RouteAnchor seaAnchor)
        {
            var last = route.Waypoints[route.Waypoints.Count - 1];
            if ((last - seaAnchor.WorldPosition).sqrMagnitude > 1e-4f)
                throw new InvalidOperationException(
                    $"P3_ROUTE_ANCHOR_MISMATCH last={last} anchor={seaAnchor.WorldPosition}");

            var berth = route.Waypoints[0];
            var reach = FlatDistance(berth, dockAnchor.BoardingPosition);
            if (reach > HullHalfLength + MinBerthGap + HullHalfLength)
                throw new InvalidOperationException(
                    $"P3_ROUTE_BERTH_OUT_OF_REACH berth={berth} dock={dockAnchor.BoardingPosition} " +
                    $"distance={reach} reason=nobody can board from there");
        }

        // The corridor test. Every leg's swept envelope, aligned to the leg, must miss every
        // solid collider that reaches the hull's depth; every interior waypoint must also clear
        // one circumscribed radius, because a hull turning there sweeps a circle rather than a
        // box. WP_0 is exempt from the circle - see TurnClearance.
        private static void VerifyNoHullSweepTouchesTheWorld(
            UnityEngine.SceneManagement.Scene scene, DiveRoutePath route, float surfaceY)
        {
            var obstacles = Obstacles(scene, surfaceY);

            for (var leg = 0; leg + 1 < route.Waypoints.Count; leg++)
            {
                var corners = SweptEnvelope(route.Waypoints[leg], route.Waypoints[leg + 1]);
                foreach (var obstacle in obstacles)
                {
                    if (!Overlaps(corners, obstacle.Value)) continue;
                    throw new InvalidOperationException(
                        $"P3_ROUTE_LEG_BLOCKED leg={leg} from={route.Waypoints[leg]} " +
                        $"to={route.Waypoints[leg + 1]} obstacle={obstacle.Key} " +
                        "reason=the hull would strike it");
                }
            }

            for (var i = 1; i + 1 < route.Waypoints.Count; i++)
            {
                foreach (var obstacle in obstacles)
                {
                    var gap = FlatDistanceToBounds(route.Waypoints[i], obstacle.Value);
                    if (gap >= TurnClearance) continue;
                    throw new InvalidOperationException(
                        $"P3_ROUTE_TURN_BLOCKED index={i} at={route.Waypoints[i]} " +
                        $"obstacle={obstacle.Key} gap={gap} need={TurnClearance} " +
                        "reason=a hull turning here sweeps a circle, not a box");
                }
            }
        }

        // The one place the real hull is measured rather than the envelope: the berth is meant to
        // be close to the jetty, and how close is the number that decides whether a player can
        // step aboard or the boat grinds against the deck.
        private static void VerifyTheBerthedHullClearsTheJetty(GameObject dock)
        {
            var berth = WaypointPositions[0];
            var hullMinZ = berth.z - HullHalfLength;
            var dockMaxZ = dock.transform.position.z + dock.transform.localScale.z * 0.5f;
            var gap = hullMinZ - dockMaxZ;

            if (gap < MinBerthGap)
                throw new InvalidOperationException(
                    $"P3_ROUTE_BERTH_TOO_CLOSE gap={gap} need={MinBerthGap} " +
                    $"hullSternZ={hullMinZ} jettyEndZ={dockMaxZ}");
        }

        private static void VerifyEveryWaypointStaysInsideTheWalls(DiveRoutePath route)
        {
            var limit = ArenaInnerExtent - EnvelopeHalfLength - WallMargin;
            foreach (var waypoint in route.Waypoints)
            {
                if (Mathf.Abs(waypoint.x) <= limit && Mathf.Abs(waypoint.z) <= limit) continue;
                throw new InvalidOperationException(
                    $"P3_ROUTE_WAYPOINT_NEAR_WALL at={waypoint} limit={limit} " +
                    "reason=the hull would reach the arena wall");
            }
        }

        // The jetty must stop short of the pad and the boat must never sit in it: a crew that
        // banked its catch by parking the boat would never have to walk ashore at all.
        private static void VerifyNothingReachesTheReturnPad(
            BoxCollider exitBox, GameObject dock, DiveRoutePath route)
        {
            var pad = BoundsOf(exitBox);

            var deck = new Bounds(dock.transform.position, dock.transform.localScale);
            if (FlatIntersects(deck, pad))
                throw new InvalidOperationException(
                    $"P3_ROUTE_DOCK_IN_RETURN_PAD deck={deck} pad={pad} " +
                    "reason=the jetty would become a second safe return");

            var berth = WaypointPositions[0];
            var hull = new Bounds(
                new Vector3(berth.x, berth.y, berth.z),
                new Vector3(HullHalfWidth * 2f, 1f, HullHalfLength * 2f));
            if (FlatIntersects(hull, pad))
                throw new InvalidOperationException(
                    $"P3_ROUTE_BERTH_IN_RETURN_PAD hull={hull} pad={pad} " +
                    "reason=sitting in the boat would bank the catch");

            foreach (var waypoint in route.Waypoints)
            {
                if (!FlatContains(pad, waypoint)) continue;
                throw new InvalidOperationException(
                    $"P3_ROUTE_WAYPOINT_IN_RETURN_PAD at={waypoint}");
            }
        }

        // "Real water" measured three ways, because the anchorage is the one place where a well
        // meant piece of scenery would quietly turn the voyage into a paddle: the classifier says
        // wet just under the surface and underwater below it, nothing walkable stands in the
        // hull's footprint, and a diver on the sea bed there is still underwater.
        private static void VerifyTheAnchorageIsRealWater(
            UnityEngine.SceneManagement.Scene scene, WaterField field, float surfaceY)
        {
            var tracker = field.CreateTracker();

            var floating = Classify(tracker, new Vector3(AnchorPosition.x, surfaceY - 1f, AnchorPosition.z));
            if (floating != EnvironmentLocomotion.Surface)
                throw new InvalidOperationException(
                    $"P3_ROUTE_ANCHOR_NOT_WATER at=surface-1 classified={floating} expected=Surface");

            foreach (var feet in new[] { surfaceY * 0.5f, SeaBedY + 0.1f })
            {
                var deep = Classify(tracker, new Vector3(AnchorPosition.x, feet, AnchorPosition.z));
                if (deep != EnvironmentLocomotion.Underwater)
                    throw new InvalidOperationException(
                        $"P3_ROUTE_ANCHOR_NOT_WATER feetY={feet} classified={deep} expected=Underwater");
            }

            var boarding = Classify(tracker, new Vector3(BoardingPosition.x, surfaceY - 1f, BoardingPosition.z));
            if (boarding != EnvironmentLocomotion.Surface)
                throw new InvalidOperationException(
                    $"P3_ROUTE_BOARDING_NOT_WATER classified={boarding} expected=Surface");

            // Nothing solid may stand in the anchorage's own footprint. A shelf here would be the
            // "wade at the anchor" this script exists to refuse.
            var column = new Bounds(
                new Vector3(AnchorPosition.x, 0f, AnchorPosition.z),
                new Vector3(HullHalfWidth * 2f, 1f, HullHalfLength * 2f));

            foreach (var collider in SolidColliders(scene))
            {
                var bounds = collider.bounds;
                if (bounds.max.y <= SeaBedY + 0.01f) continue;      // the sea bed itself
                if (!FlatIntersects(column, bounds)) continue;
                throw new InvalidOperationException(
                    $"P3_ROUTE_ANCHOR_HAS_LAND collider={collider.name} bounds={bounds} " +
                    "reason=the anchorage must be open water, not a wade shelf");
            }
        }

        private static void VerifyTheRegionCoversTheWater(
            DiveRegionField region, BoxCollider swimBox, DiveRoutePath route,
            RouteAnchor dockAnchor, RouteAnchor seaAnchor)
        {
            var water = BoundsOf(swimBox);
            var corners = new[]
            {
                new Vector3(water.min.x, 0f, water.min.z),
                new Vector3(water.max.x, 0f, water.min.z),
                new Vector3(water.min.x, 0f, water.max.z),
                new Vector3(water.max.x, 0f, water.max.z)
            };

            foreach (var corner in corners)
            {
                if (region.Bounds.Contains(corner.x, corner.z)) continue;
                throw new InvalidOperationException(
                    $"P3_ROUTE_REGION_TOO_SMALL corner={corner} region={region.RegionId} " +
                    "reason=water outside the region has no place on the map");
            }

            // Throws P3_ROUTE_OUT_OF_REGION on the first point that does not belong.
            foreach (var waypoint in route.Waypoints) region.WorldToMapChecked(waypoint);
            region.WorldToMapChecked(dockAnchor.WorldPosition);
            region.WorldToMapChecked(dockAnchor.BoardingPosition);
            region.WorldToMapChecked(seaAnchor.WorldPosition);
            region.WorldToMapChecked(seaAnchor.BoardingPosition);
        }

        // Nothing above writes WaterField, and this says so out loud rather than trusting it.
        private static void VerifyWaterAuthorityUntouched(WaterField field, BoxCollider swimBox)
        {
            if (field.VolumeCount != 1 || field.Bodies.Count != 1)
                throw new InvalidOperationException(
                    $"P3_ROUTE_WATER_AUTHORITY_CHANGED volumes={field.VolumeCount} " +
                    $"bodies={field.Bodies.Count} reason=expected exactly one SwimVolume");

            if (!Mathf.Approximately(field.Bodies[0].SurfaceY, SurfaceOf(swimBox)))
                throw new InvalidOperationException(
                    $"P3_ROUTE_WATER_AUTHORITY_CHANGED surfaceY={field.Bodies[0].SurfaceY} " +
                    $"expected={SurfaceOf(swimBox)}");
        }

        // --- Geometry ------------------------------------------------------------------------------

        // The swept envelope of a hull travelling from one waypoint to the next: the leg's own
        // box, widened by the envelope's half width and extended by its half length past both
        // ends, since the hull's bow and stern reach beyond the point it steers to.
        private static Vector3[] SweptEnvelope(Vector3 from, Vector3 to)
        {
            var direction = new Vector3(to.x - from.x, 0f, to.z - from.z);
            var length = direction.magnitude;
            if (length <= Mathf.Epsilon)
                throw new InvalidOperationException(
                    $"P3_ROUTE_ZERO_LEG from={from} to={to} reason=two waypoints share a place");

            var forward = direction / length;
            var right = new Vector3(forward.z, 0f, -forward.x);
            var centre = new Vector3((from.x + to.x) * 0.5f, 0f, (from.z + to.z) * 0.5f);
            var halfLength = length * 0.5f + EnvelopeHalfLength;

            return new[]
            {
                centre - forward * halfLength - right * EnvelopeHalfWidth,
                centre - forward * halfLength + right * EnvelopeHalfWidth,
                centre + forward * halfLength + right * EnvelopeHalfWidth,
                centre + forward * halfLength - right * EnvelopeHalfWidth
            };
        }

        // Separating-axis test in XZ between the swept box and a world-axis bounds. Four axes are
        // enough for two rectangles: both of the box's, and both world axes.
        private static bool Overlaps(Vector3[] corners, Bounds bounds)
        {
            var axes = new[]
            {
                (corners[1] - corners[0]).normalized,
                (corners[3] - corners[0]).normalized,
                Vector3.right,
                Vector3.forward
            };

            var boxCorners = new[]
            {
                new Vector3(bounds.min.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.min.z),
                new Vector3(bounds.max.x, 0f, bounds.max.z),
                new Vector3(bounds.min.x, 0f, bounds.max.z)
            };

            foreach (var axis in axes)
            {
                Project(corners, axis, out var minA, out var maxA);
                Project(boxCorners, axis, out var minB, out var maxB);
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

        // Every solid collider that reaches the hull's depth, by name. The sea bed (top y = 0)
        // and the arena walls (top y = 7) fall out here: neither can touch a hull that floats
        // between 7.5 and the surface, and the walls are covered by the arena inset rule instead.
        private static Dictionary<string, Bounds> Obstacles(
            UnityEngine.SceneManagement.Scene scene, float surfaceY)
        {
            var obstacles = new Dictionary<string, Bounds>();
            foreach (var collider in SolidColliders(scene))
            {
                if (collider.bounds.max.y <= surfaceY - HullDraft) continue;
                var key = collider.name;
                for (var i = 2; obstacles.ContainsKey(key); i++) key = collider.name + "#" + i;
                obstacles[key] = collider.bounds;
            }

            return obstacles;
        }

        private static IEnumerable<Collider> SolidColliders(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (!collider.isTrigger)
                        yield return collider;
        }

        private static EnvironmentLocomotion Classify(IWaterField tracker, Vector3 feet) =>
            tracker.Classify(new WaterProbe(
                feet, new Vector3(0f, DiverCentreY, 0f), DiverRadius, DiverHeight, Vector3.up));

        private static float FlatDistance(Vector3 a, Vector3 b) =>
            Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));

        private static float FlatDistanceToBounds(Vector3 point, Bounds bounds)
        {
            var dx = Mathf.Max(bounds.min.x - point.x, 0f, point.x - bounds.max.x);
            var dz = Mathf.Max(bounds.min.z - point.z, 0f, point.z - bounds.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool FlatIntersects(Bounds a, Bounds b) =>
            a.min.x < b.max.x && b.min.x < a.max.x && a.min.z < b.max.z && b.min.z < a.max.z;

        private static bool FlatContains(Bounds bounds, Vector3 point) =>
            point.x >= bounds.min.x && point.x <= bounds.max.x &&
            point.z >= bounds.min.z && point.z <= bounds.max.z;

        private static Bounds BoundsOf(BoxCollider box) =>
            new Bounds(
                box.transform.TransformPoint(box.center),
                Vector3.Scale(box.size, box.transform.lossyScale));

        private static float SurfaceOf(BoxCollider box) =>
            box.transform.TransformPoint(box.center).y +
            Vector3.Scale(box.size, box.transform.lossyScale).y * 0.5f;

        // --- Lookup ---------------------------------------------------------------------------------

        private static BoxCollider RequireSwimVolume()
        {
            var swim = UnityEngine.Object.FindFirstObjectByType<SwimVolume>();
            if (swim == null)
                throw new InvalidOperationException(
                    $"P3_ROUTE_NO_SWIMVOLUME scene={ScenePath} reason=run DeepDive/P2/Dive test area first");
            var box = swim.GetComponent<BoxCollider>();
            if (box == null)
                throw new InvalidOperationException($"P3_ROUTE_SWIMVOLUME_NO_BOX object={swim.name}");
            return box;
        }

        private static BoxCollider RequireExitZone(UnityEngine.SceneManagement.Scene scene)
        {
            var exit = RequireRoot(scene, "DiveExit", "P3_ROUTE_NO_DIVEEXIT");
            var box = exit.GetComponent<BoxCollider>();
            if (box == null) throw new InvalidOperationException("P3_ROUTE_DIVEEXIT_NO_BOX");
            return box;
        }

        private static GameObject EnsureBareRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var root = FindRoot(scene, name);
            if (root == null)
            {
                root = new GameObject(name);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            }

            root.layer = 0;
            return root;
        }

        private static GameObject RequireRoot(
            UnityEngine.SceneManagement.Scene scene, string name, string failure)
        {
            var root = FindRoot(scene, name);
            if (root == null) throw new InvalidOperationException($"{failure} missing={name}");
            return root;
        }

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(x => x.name == name).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException(
                    $"P3_ROUTE_DUPLICATE_ROOT name={name} count={matches.Length}");
            return matches.Length == 1 ? matches[0] : null;
        }

        // Scenery and markers only. A NetworkObject would need a GlobalObjectIdHash and would
        // drag the route into the netcode surface for nothing: the boat that sails it is
        // Mehmet's networked prefab, the path it follows is not.
        private static void RequireNotNetworked(GameObject piece, string name)
        {
            if (piece.GetComponent<NetworkObject>() != null)
                throw new InvalidOperationException($"P3_ROUTE_NETWORKED object={name}");
        }
    }
}
