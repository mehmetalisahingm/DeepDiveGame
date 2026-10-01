using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // The world half of one dive route (#76): the ordered world-space points a boat follows
    // between the dock and the anchorage. Core's DiveRouteDefinition is the other half and holds
    // ids and times only, because it has to stay free of UnityEngine; the geometry lives here,
    // where it is authored by dragging child transforms in the scene instead of typing numbers.
    //
    // Deliberately not a mover. It answers "where does this route go" and nothing else: Mehmet's
    // host-authoritative movement walks the list and reports arrivals through
    // BoatRouteProgress.ReportArrival, and Mert's BoatTripManager decides what an arrival means.
    // Anything here that drove, timed or gated the boat would be the second trip authority
    // docs/plan/CONTRACTS.md refuses ("uc ayri sandal otoritesi kurulmaz").
    //
    // No NetworkObject and no NetworkBehaviour: a route is authored scenery, identical on every
    // machine, so there is nothing to replicate.
    //
    // The waypoints are children rather than a serialized Vector3 list so that they can be seen,
    // dragged and measured in the scene view - a route that is only inspectable as numbers is a
    // route nobody checks against the geometry it has to miss.
    [DisallowMultipleComponent]
    public sealed class DiveRoutePath : MonoBehaviour
    {
        [SerializeField] private string routeId = "";

        // Rebuilt on demand rather than read per call: the mover reads this while under way, and
        // the points are authored scenery that does not move on its own. Refresh() is the single
        // way to pick up an edit, so nobody can be handed a half-updated route.
        private Vector3[] points = Array.Empty<Vector3>();
        private ReadOnlyCollection<Vector3> view;

        public string RouteId => routeId;

        // Spelled against Mert's constant, never as a literal: the id reaches BoatTripManager's
        // route check and a second copy of the string here could drift from it silently.
        public bool IsContractRoute =>
            string.Equals(routeId, BoatTripIds.NearRouteId, StringComparison.Ordinal);

        // A read-only view rather than the array itself. The list crosses into Mehmet's mover,
        // and a Vector3[] handed out as IReadOnlyList can be cast straight back and written
        // through - which would let a consumer edit the world's route data from the outside.
        public IReadOnlyList<Vector3> Waypoints
        {
            get
            {
                if (view == null) Refresh();
                return view;
            }
        }

        // Two is the fewest points that describe a journey. A route with one point is an
        // authoring accident - the boat would have nowhere to go - and saying so here is what
        // keeps "the route is missing" and "the route is half-authored" separate failures.
        public bool IsUsable => !string.IsNullOrEmpty(routeId) && Waypoints.Count >= 2;

        // Mehmet's frozen nominal for the near route: eight seconds out, eight back. They are the
        // target the trip is written around, not a measurement of this path - his mover derives
        // its own speed from the waypoints it walks. Seconds computed here from the route's length
        // would be a second answer to "how fast does the boat go", which is his.
        public const float NominalOutboundSeconds = 8f;
        public const float NominalInboundSeconds = 8f;

        // The bridge to Core (docs/plan/CONTRACTS.md "Sabit kimlikler"): Core keeps the stable
        // anchor ids and the times, the geometry stays here, and the struct is constructed rather
        // than altered - DiveRouteDefinition is Mert's file and is not edited from this side.
        //
        // The anchor ids are the near route's frozen pair rather than serialized fields. Fields
        // would mean re-saving DiveTestArea to author what the contract already fixes; P4.3's
        // second route is the phase that needs them per path, and it can add them then. It is
        // also why an unknown or half-authored path is refused instead of described: a definition
        // built from one would name a dock and an anchor that its own waypoints never visit.
        public DiveRouteDefinition ToDefinition()
        {
            if (!IsContractRoute || !IsUsable)
                throw new InvalidOperationException(
                    $"P3_ROUTE_NO_DEFINITION object={name} routeId={routeId} waypoints={Waypoints.Count} " +
                    "reason=only a usable near route has a definition");

            return new DiveRouteDefinition(
                routeId,
                DiveRouteAnchors.Dock,
                DiveRouteAnchors.AnchorPoint,
                NominalOutboundSeconds,
                NominalInboundSeconds);
        }

        // Sibling order is route order. It is the order the hierarchy shows, so what an author
        // sees in the scene is what the boat does; the scene script names the children WP_0..WP_n
        // and the scene test pins those names, so a drag in the hierarchy cannot quietly reverse
        // a leg without failing something.
        public void Refresh()
        {
            var count = transform.childCount;
            if (points.Length != count)
            {
                points = new Vector3[count];
                view = new ReadOnlyCollection<Vector3>(points);
            }

            for (var i = 0; i < count; i++) points[i] = transform.GetChild(i).position;

            view ??= new ReadOnlyCollection<Vector3>(points);
        }

        // Editor setup only, like BoatPartAnchor.Configure.
        public void Configure(string id)
        {
            routeId = id ?? string.Empty;
        }

        // Exact id match, and no fallback to "the only route in the scene". Such a fallback would
        // work perfectly until P4.3 adds the second route, and then quietly sail the wrong one -
        // the failure would appear months later as a boat going somewhere nobody asked for.
        //
        // A match is returned even when it is not usable: the caller can then say "route
        // route-near-1 has one waypoint" instead of the useless "route not found".
        public static bool TryFind(string routeId, out DiveRoutePath path)
        {
            path = null;
            if (string.IsNullOrEmpty(routeId)) return false;

            var found = FindObjectsByType<DiveRoutePath>(FindObjectsSortMode.None);
            for (var i = 0; i < found.Length; i++)
            {
                if (!string.Equals(found[i].routeId, routeId, StringComparison.Ordinal)) continue;

                // Two paths claiming one id is ambiguous data, and picking the first would make
                // which one sails depend on scene load order. Refused out loud instead.
                if (path != null)
                {
                    Debug.LogError(
                        $"P3_ROUTE_DUPLICATE_ID routeId={routeId} first={path.name} second={found[i].name} " +
                        "reason=one route id must name exactly one path");
                    path = null;
                    return false;
                }

                path = found[i];
            }

            return path != null;
        }

        private void Awake() => Refresh();

        // No Refresh here: OnValidate runs during deserialization, where reaching into the
        // hierarchy is not always safe. The scene script calls Refresh explicitly after it
        // places the children, and Awake covers the built game.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(routeId) || IsContractRoute) return;
            Debug.LogError(
                $"P3_ROUTE_UNKNOWN_ID object={name} routeId={routeId} " +
                $"reason=must be BoatTripIds.NearRouteId", this);
        }
    }
}
