using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeepDive.World.Tests
{
    // The route path and its two anchors are ordered placement and identity, nothing else. These
    // tests are mostly about what they must NOT grow: a trip phase, a mover, a fallback that
    // guesses which route was meant. Where the route actually runs in DiveTestArea, and that it
    // clears the world it has to miss, is pinned by the scene tests; nothing here opens a scene.
    public class DiveRoutePathTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void DestroySpawned()
        {
            // Every test creates real objects in the test scene and TryFind searches that scene,
            // so leftovers from one test would be found by the next.
            foreach (var go in spawned)
                if (go != null)
                    Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private DiveRoutePath NewRoute(string routeId, params Vector3[] waypoints)
        {
            var host = new GameObject("Route_Test");
            spawned.Add(host);

            var path = host.AddComponent<DiveRoutePath>();
            path.Configure(routeId);

            for (var i = 0; i < waypoints.Length; i++)
            {
                var child = new GameObject("WP_" + i);
                child.transform.SetParent(host.transform);
                child.transform.position = waypoints[i];
            }

            path.Refresh();
            return path;
        }

        private RouteAnchor NewAnchor(string anchorId)
        {
            var host = new GameObject("Anchor_Test");
            spawned.Add(host);

            var anchor = host.AddComponent<RouteAnchor>();
            anchor.Configure(anchorId);
            return anchor;
        }

        private static void Validate(Object target) =>
            target.GetType()
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);

        [Test]
        public void WaypointsComeBackInSiblingOrder()
        {
            var path = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(9f, 8f, -0.4f),
                new Vector3(9f, 8f, 3.5f),
                new Vector3(9f, 8f, 8.5f));

            Assert.AreEqual(3, path.Waypoints.Count);
            Assert.AreEqual(new Vector3(9f, 8f, -0.4f), path.Waypoints[0]);
            Assert.AreEqual(new Vector3(9f, 8f, 3.5f), path.Waypoints[1]);
            Assert.AreEqual(new Vector3(9f, 8f, 8.5f), path.Waypoints[2]);
        }

        [Test]
        public void ReorderingTheChildrenReordersTheRoute()
        {
            // The hierarchy is the authoring surface, so what an author sees top to bottom has to
            // be the order the boat sails. If the path sorted by name or by distance instead,
            // this drag would change nothing and the scene view would be lying.
            var path = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(0f, 8f, 0f),
                new Vector3(0f, 8f, 4f),
                new Vector3(0f, 8f, 9f));

            path.transform.GetChild(2).SetSiblingIndex(0);
            path.Refresh();

            Assert.AreEqual(new Vector3(0f, 8f, 9f), path.Waypoints[0], "the dragged child leads now");
            Assert.AreEqual(new Vector3(0f, 8f, 0f), path.Waypoints[1]);
            Assert.AreEqual(new Vector3(0f, 8f, 4f), path.Waypoints[2]);
        }

        [Test]
        public void WaypointsAreWorldSpaceAndFollowTheirParent()
        {
            // Mehmet's mover steers to these numbers directly. If they were local, moving the
            // route's root would leave the boat sailing to coordinates that exist nowhere.
            var locals = new[] { Vector3.zero, new Vector3(0f, 0f, 4f), new Vector3(2f, 0f, 9f) };
            var path = NewRoute(BoatTripIds.NearRouteId, locals);

            var position = new Vector3(5f, 8f, -2f);
            var rotation = Quaternion.Euler(0f, 90f, 0f);
            path.transform.SetPositionAndRotation(position, rotation);
            path.Refresh();

            var expected = Matrix4x4.TRS(position, rotation, Vector3.one);
            for (var i = 0; i < locals.Length; i++)
            {
                var point = expected.MultiplyPoint3x4(locals[i]);
                Assert.AreEqual(point.x, path.Waypoints[i].x, 1e-4f, "world x of waypoint " + i);
                Assert.AreEqual(point.y, path.Waypoints[i].y, 1e-4f, "world y of waypoint " + i);
                Assert.AreEqual(point.z, path.Waypoints[i].z, 1e-4f, "world z of waypoint " + i);
            }

            Assert.AreNotEqual(locals[2], path.Waypoints[2], "the route reported local coordinates");
        }

        [Test]
        public void ARouteWithFewerThanTwoWaypointsIsNotUsable()
        {
            Assert.IsFalse(NewRoute(BoatTripIds.NearRouteId).IsUsable,
                "a route with no waypoints has nowhere to go");
            Assert.IsFalse(NewRoute(BoatTripIds.NearRouteId, new Vector3(9f, 8f, -0.4f)).IsUsable,
                "one waypoint is a place, not a journey");
            Assert.IsTrue(NewRoute(BoatTripIds.NearRouteId,
                    new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f)).IsUsable,
                "two waypoints are a leg and must be usable");

            // Geometry without an id cannot be asked for by name, so it is not a route either.
            Assert.IsFalse(NewRoute("", new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f)).IsUsable,
                "an unnamed path must not read as a usable route");
        }

        [Test]
        public void TryFindMatchesTheRouteIdExactlyAndNeverFallsBackToTheOnlyRoute()
        {
            var near = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f));

            Assert.IsTrue(DiveRoutePath.TryFind(BoatTripIds.NearRouteId, out var found));
            Assert.AreSame(near, found);

            // The only route in the scene is still not "the route you asked for". A fallback here
            // would work until P4.3 adds the second route and then sail the wrong one.
            foreach (var wrong in new[] { "route-far-2", "ROUTE-NEAR-1", "route-near-1 ", "", null })
            {
                Assert.IsFalse(DiveRoutePath.TryFind(wrong, out var miss),
                    "id " + (wrong ?? "null") + " must not match");
                Assert.IsNull(miss, "a refused lookup must not hand back a path anyway");
            }

            // Two paths claiming one id is ambiguous data; picking the first would make which one
            // sails depend on scene load order.
            NewRoute(BoatTripIds.NearRouteId, new Vector3(0f, 8f, 0f), new Vector3(0f, 8f, 5f));
            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_DUPLICATE_ID"));
            Assert.IsFalse(DiveRoutePath.TryFind(BoatTripIds.NearRouteId, out var ambiguous));
            Assert.IsNull(ambiguous);
        }

        [Test]
        public void AnUnknownRouteIdIsReportedAsAnError()
        {
            // A path with a typo looks perfectly fine in the scene while no trip can ever ask for
            // it, so it is a hard error rather than a warning.
            var path = NewRoute("route-near-2", new Vector3(0f, 8f, 0f), new Vector3(0f, 8f, 5f));
            Assert.IsFalse(path.IsContractRoute);

            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_UNKNOWN_ID"));
            Validate(path);

            // The default empty id means "not authored yet" and must stay quiet, or adding the
            // component would spam the console.
            Validate(NewRoute("", new Vector3(0f, 8f, 0f)));
        }

        [TestCase("dock-town-1")]
        [TestCase("anchor-near-1")]
        [TestCase("anchor-reef-1")]
        [TestCase("anchor-deep-1")]
        public void AnchorAcceptsEachContractId(string anchorId)
        {
            var anchor = NewAnchor(anchorId);
            Assert.AreEqual(anchorId, anchor.AnchorId);
            Assert.IsTrue(anchor.IsContractAnchor, anchorId + " must be one of the route ends");
            Validate(anchor);
        }

        [Test]
        public void AnUnknownAnchorIdIsReportedAsAnError()
        {
            var anchor = NewAnchor("dock-town-2");
            Assert.IsFalse(anchor.IsContractAnchor);

            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_UNKNOWN_ANCHOR"));
            Validate(anchor);

            Validate(NewAnchor(""));
            Validate(NewAnchor(null));
        }

        [Test]
        public void TheBoardingPointFallsBackToTheAnchorItself()
        {
            // The dock anchor already sits where a player walks, so it needs no separate boarding
            // spot; the sea anchor is the hull's own position and does. One optional child covers
            // both without minting a third frozen id.
            var dock = NewAnchor(DiveRouteAnchors.Dock);
            dock.transform.position = new Vector3(9f, 8.4f, -3.7f);
            Assert.AreEqual(dock.WorldPosition, dock.BoardingPosition,
                "an anchor with no boarding child boards at itself");

            var sea = NewAnchor(DiveRouteAnchors.AnchorPoint);
            sea.transform.position = new Vector3(9f, 8f, 8.5f);
            var boarding = new GameObject("Boarding");
            boarding.transform.SetParent(sea.transform);
            boarding.transform.position = new Vector3(11f, 8f, 8.5f);
            sea.Configure(DiveRouteAnchors.AnchorPoint, boarding.transform);

            Assert.AreEqual(new Vector3(11f, 8f, 8.5f), sea.BoardingPosition,
                "boarding at sea happens beside the hull, not inside it");
            Assert.AreEqual(new Vector3(9f, 8f, 8.5f), sea.WorldPosition,
                "the anchor itself must stay where the hull sits");
        }

        [Test]
        public void TheFrozenRouteAndAnchorIdsAreTheOnesTheContractNames()
        {
            // Spelled out rather than read from the constants, so a rename shows up as a failing
            // test instead of silently renaming the world anchors too. These strings are shared
            // with Mert's trip state and Mehmet's mover; they are not ours to change alone.
            Assert.AreEqual("route-near-1", BoatTripIds.NearRouteId);
            Assert.AreEqual("dock-town-1", DiveRouteAnchors.Dock);
            Assert.AreEqual("anchor-near-1", DiveRouteAnchors.AnchorPoint);
            // P4.3 (#108): one shared departure and the three sea anchorages, nothing else.
            Assert.AreEqual("anchor-reef-1", DiveRouteAnchors.ReefAnchorPoint);
            Assert.AreEqual("anchor-deep-1", DiveRouteAnchors.DeepAnchorPoint);
            CollectionAssert.AreEquivalent(
                new[] { "dock-town-1", "anchor-near-1", "anchor-reef-1", "anchor-deep-1" }, DiveRouteAnchors.All);
            CollectionAssert.AreEquivalent(
                new[] { "anchor-near-1", "anchor-reef-1", "anchor-deep-1" }, DiveRouteAnchors.AnchorPoints);
            Assert.IsFalse(DiveRouteAnchors.IsAnchorPoint(DiveRouteAnchors.Dock), "the dock is a departure, not an anchorage");
        }

        [Test]
        public void TheDefinitionCarriesTheFrozenRouteAndAnchorIds()
        {
            var path = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(9f, 8f, -0.4f),
                new Vector3(9f, 8f, 3.5f),
                new Vector3(9f, 8f, 8.5f));

            var definition = path.ToDefinition();

            // Both spellings, for the same reason the test above gives: the constant proves the
            // bridge reads the contract, the literal proves the contract itself has not moved
            // under Mert's trip state or Mehmet's mover.
            Assert.AreEqual(BoatTripIds.NearRouteId, definition.RouteId, "the definition's route id");
            Assert.AreEqual("route-near-1", definition.RouteId);
            Assert.AreEqual(DiveRouteAnchors.Dock, definition.DepartureDockAnchor, "the departure anchor");
            Assert.AreEqual("dock-town-1", definition.DepartureDockAnchor);
            Assert.AreEqual(DiveRouteAnchors.AnchorPoint, definition.AnchorPointAnchor, "the anchorage");
            Assert.AreEqual("anchor-near-1", definition.AnchorPointAnchor);

            // Mehmet's frozen nominal, out and back.
            Assert.AreEqual(8f, definition.OutboundSeconds, 0.001f, "the outbound leg's nominal time");
            Assert.AreEqual(8f, definition.InboundSeconds, 0.001f, "the inbound leg's nominal time");
        }

        [Test]
        public void TheDefinitionIsNominalAndDerivesNothingFromTheGeometry()
        {
            // A route four times as long must produce exactly the same numbers. The times are a
            // target Mehmet froze, and his mover is the one that turns waypoints into a speed;
            // seconds computed from the path's length here would be a second answer to that.
            var near = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f)).ToDefinition();
            var far = NewRoute(BoatTripIds.NearRouteId,
                new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 35.2f)).ToDefinition();

            Assert.AreEqual(near.OutboundSeconds, far.OutboundSeconds, "the outbound time followed the geometry");
            Assert.AreEqual(near.InboundSeconds, far.InboundSeconds, "the inbound time followed the geometry");
            Assert.AreEqual(near.DepartureDockAnchor, far.DepartureDockAnchor);
            Assert.AreEqual(near.AnchorPointAnchor, far.AnchorPointAnchor);
        }

        [Test]
        public void NoDefinitionComesFromAnUnknownOrHalfAuthoredRoute()
        {
            // The definition names dock-town-1 and anchor-near-1. Handing those ids to a path that
            // is not the near route, or that has no journey in it, would describe a trip between
            // two anchors its own waypoints never visit.
            foreach (var path in new[]
                     {
                         NewRoute("route-far-2", new Vector3(0f, 8f, 0f), new Vector3(0f, 8f, 5f)),
                         NewRoute("", new Vector3(0f, 8f, 0f), new Vector3(0f, 8f, 5f)),
                         NewRoute(BoatTripIds.NearRouteId, new Vector3(9f, 8f, -0.4f)),
                         NewRoute(BoatTripIds.NearRouteId)
                     })
            {
                var error = Assert.Throws<System.InvalidOperationException>(() => path.ToDefinition(),
                    "routeId=" + path.RouteId + " waypoints=" + path.Waypoints.Count + " produced a definition");
                StringAssert.Contains("P3_ROUTE_NO_DEFINITION", error.Message);
            }
        }

        // --- P4.3 (#108): per-route definitions authored on the path ---------------------------------

        [Test]
        public void TheNearDefinitionIsIdenticalToTheP33Definition()
        {
            // A path configured with its id only - the shape every P3 scene was saved in - must still describe
            // exactly the definition the P3 five-argument contract built: same ids, 8 s / 8 s, a rowboat route.
            var path = NewRoute(BoatTripIds.NearRouteId, new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f));
            var legacy = new DiveRouteDefinition(BoatTripIds.NearRouteId, DiveRouteAnchors.Dock,
                DiveRouteAnchors.AnchorPoint, DiveRoutePath.NominalOutboundSeconds, DiveRoutePath.NominalInboundSeconds);

            var definition = path.ToDefinition();

            Assert.AreEqual(legacy.RouteId, definition.RouteId);
            Assert.AreEqual(legacy.DepartureDockAnchor, definition.DepartureDockAnchor);
            Assert.AreEqual(legacy.AnchorPointAnchor, definition.AnchorPointAnchor);
            Assert.AreEqual(legacy.OutboundSeconds, definition.OutboundSeconds);
            Assert.AreEqual(legacy.InboundSeconds, definition.InboundSeconds);
            Assert.AreEqual(VehicleClass.Rowboat, definition.RequiredVehicleClass);
            Assert.AreEqual(legacy.RequiredVehicleClass, definition.RequiredVehicleClass);
        }

        [TestCase("route-near-1", "anchor-near-1", VehicleClass.Rowboat, 8f)]
        [TestCase("route-reef-1", "anchor-reef-1", VehicleClass.Motorboat, 28f)]
        [TestCase("route-deep-1", "anchor-deep-1", VehicleClass.ResearchBoat, 52f)]
        public void EachRouteCarriesItsOwnAuthoredDefinition(string routeId, string anchorId, VehicleClass required, float seconds)
        {
            var path = NewRoute(routeId, new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 3.5f));
            path.Configure(routeId, DiveRouteAnchors.Dock, anchorId, required, seconds, seconds);

            var definition = path.ToDefinition();

            Assert.AreEqual(routeId, definition.RouteId);
            Assert.AreEqual("dock-town-1", definition.DepartureDockAnchor);
            Assert.AreEqual(anchorId, definition.AnchorPointAnchor);
            Assert.AreEqual(required, definition.RequiredVehicleClass);
            Assert.AreEqual(seconds, definition.OutboundSeconds);
            Assert.AreEqual(seconds, definition.InboundSeconds);
            Assert.AreEqual(required, path.RequiredVehicleClass);
        }

        [Test]
        public void AHalfAuthoredDefinitionIsRefused()
        {
            var waypoints = new[] { new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 3.5f) };
            var broken = new (string Dock, string Anchor, VehicleClass Class, float Out, float In)[]
            {
                (DiveRouteAnchors.Dock, DiveRouteAnchors.Dock, VehicleClass.Motorboat, 28f, 28f),        // the dock as an anchorage
                ("dock-town-2", DiveRouteAnchors.ReefAnchorPoint, VehicleClass.Motorboat, 28f, 28f),      // unknown departure
                (DiveRouteAnchors.Dock, DiveRouteAnchors.ReefAnchorPoint, VehicleClass.None, 28f, 28f),   // no class
                (DiveRouteAnchors.Dock, DiveRouteAnchors.ReefAnchorPoint, (VehicleClass)9, 28f, 28f),     // undefined class
                (DiveRouteAnchors.Dock, DiveRouteAnchors.ReefAnchorPoint, VehicleClass.Motorboat, 0f, 28f),
                (DiveRouteAnchors.Dock, DiveRouteAnchors.ReefAnchorPoint, VehicleClass.Motorboat, 28f, float.NaN)
            };

            foreach (var b in broken)
            {
                var path = NewRoute(BoatTripIds.ReefRouteId, waypoints);
                path.Configure(BoatTripIds.ReefRouteId, b.Dock, b.Anchor, b.Class, b.Out, b.In);
                Assert.IsFalse(path.HasAuthoredDefinition, b.ToString());
                Assert.Throws<System.InvalidOperationException>(() => path.ToDefinition(), b.ToString());
                Assert.IsFalse(DiveRoutePath.TryGetDefinition(BoatTripIds.ReefRouteId, out var none), b.ToString());
                Assert.IsNull(none.RouteId, "a refusal hands back default, not a partial definition");
                var host = path.gameObject;
                spawned.Remove(host);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TryGetDefinitionReadsTheScenePathByExactIdAndNothingElse()
        {
            var near = NewRoute(BoatTripIds.NearRouteId, new Vector3(9f, 8f, -0.4f), new Vector3(9f, 8f, 8.5f));
            var reef = NewRoute(BoatTripIds.ReefRouteId, new Vector3(9f, 8f, -0.4f), new Vector3(3f, 8f, 30f));
            reef.Configure(BoatTripIds.ReefRouteId, DiveRouteAnchors.Dock, DiveRouteAnchors.ReefAnchorPoint,
                VehicleClass.Motorboat, 28f, 28f);

            Assert.IsTrue(DiveRoutePath.TryGetDefinition(BoatTripIds.NearRouteId, out var n));
            Assert.AreEqual(VehicleClass.Rowboat, n.RequiredVehicleClass);
            Assert.IsTrue(DiveRoutePath.TryGetDefinition(BoatTripIds.ReefRouteId, out var r));
            Assert.AreEqual(VehicleClass.Motorboat, r.RequiredVehicleClass);
            Assert.AreEqual(DiveRouteAnchors.ReefAnchorPoint, r.AnchorPointAnchor);

            // Not in the scene, not a contract id, empty: refused, never "the only route there is".
            Assert.IsFalse(DiveRoutePath.TryGetDefinition(BoatTripIds.DeepRouteId, out _), "deep is not authored here");
            Assert.IsFalse(DiveRoutePath.TryGetDefinition("route-far-2", out _));
            Assert.IsFalse(DiveRoutePath.TryGetDefinition("", out _));
            Assert.IsFalse(DiveRoutePath.TryGetDefinition(null, out _));

            // Two paths claiming one id are ambiguous.
            NewRoute(BoatTripIds.NearRouteId, new Vector3(0f, 8f, 0f), new Vector3(0f, 8f, 5f));
            LogAssert.Expect(LogType.Error, new Regex("P3_ROUTE_DUPLICATE_ID"));
            Assert.IsFalse(DiveRoutePath.TryGetDefinition(BoatTripIds.NearRouteId, out _));
            Assert.IsNotNull(near);
        }

        [Test]
        public void ExactlyTheThreeContractRouteIdsAreRoutes()
        {
            Assert.IsTrue(DiveRoutePath.IsContractRouteId(BoatTripIds.NearRouteId));
            Assert.IsTrue(DiveRoutePath.IsContractRouteId(BoatTripIds.ReefRouteId));
            Assert.IsTrue(DiveRoutePath.IsContractRouteId(BoatTripIds.DeepRouteId));
            Assert.IsFalse(DiveRoutePath.IsContractRouteId("route-near-2"));
            Assert.IsFalse(DiveRoutePath.IsContractRouteId("ROUTE-REEF-1"));
            Assert.IsFalse(DiveRoutePath.IsContractRouteId(null));
        }
    }
}
