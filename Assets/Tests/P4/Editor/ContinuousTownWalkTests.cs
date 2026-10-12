using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    // #138 PR-1: walk the real DiveTestArea + #143 town the way a diver's CharacterController would.
    // Every sample on a route, at <= 0.25m spacing, must have:
    //   - a non-trigger floor under it (ray from y 9.8, 2.6m long);
    //   - a step to the previous sample <= stepOffset, or a surface slope <= slopeLimit;
    //   - room for the diver capsule lifted by skin+0.01 (walk surfaces themselves excluded).
    // A sample whose floor is below 8.0 ends the route only if the zone rules call it Sea.
    public sealed class ContinuousTownWalkTests
    {
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";

        // Written values, locked against the prefab by DiverControllerMatchesTheWrittenWalkLimits.
        private const float Radius = 0.35f;
        private const float Height = 1.8f;
        private const float CenterY = 0.9f;
        private const float SlopeLimit = 45f;
        private const float StepOffset = 0.3f;
        private const float Skin = 0.08f;

        private const float SampleSpacing = 0.25f;
        private const float RayStartY = 9.8f;
        private const float RayLength = 2.6f;
        private const float WalkY = 8.4f;
        private const float WaterSurfaceY = 8.0f;

        private static readonly string[] WalkSurfaces =
        {
            "Beach_Platform", "Beach_Wade", "Shore_Ledge", "Shore_Ramp", "Dock_Town",
            "CoastWalk", "MainStreet", "TownSquare", "HouseApproach", "HomePlot"
        };

        private static readonly Vector2 HomeInside = new Vector2(0f, -50f);
        private static readonly Vector2 Square = new Vector2(0f, -24f);
        private const float CustomerZ = -27.3f;

        // Rota 36: house -> side corridor -> door -> approach -> square -> street -> coast walk -> beach
        // -> top of the wade -> down the wade into the sea.
        private static readonly Vector2[] HomeToWater =
        {
            HomeInside, new Vector2(-4.7f, -50f), new Vector2(-4.7f, -43f), new Vector2(0f, -42f),
            new Vector2(0f, -36f), Square, new Vector2(0f, -16f), new Vector2(0f, -12.5f),
            new Vector2(-6.5f, -11.2f), new Vector2(-6.5f, -5f)
        };

        // The branch: beach -> ledge -> dock.
        private static readonly Vector2[] BeachToDock =
        {
            new Vector2(0f, -12.5f), new Vector2(9f, -12.5f), new Vector2(9f, -9f), new Vector2(9f, -4f)
        };

        [Test]
        public void DiverControllerMatchesTheWrittenWalkLimits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, "NetworkDiver prefab missing");
            var controller = prefab.GetComponent<CharacterController>();
            Assert.IsNotNull(controller, "NetworkDiver has no CharacterController");
            Assert.AreEqual(Height, controller.height, 1e-4f);
            Assert.AreEqual(Radius, controller.radius, 1e-4f);
            Assert.AreEqual(new Vector3(0f, CenterY, 0f), controller.center);
            Assert.AreEqual(SlopeLimit, controller.slopeLimit, 1e-4f);
            Assert.AreEqual(StepOffset, controller.stepOffset, 1e-4f);
            Assert.AreEqual(Skin, controller.skinWidth, 1e-4f);
        }

        [Test]
        public void TheHomeToWaterWalkHasGroundHeadroomAndWalkableSteps()
        {
            ContinuousTownScene.With(_ =>
            {
                Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeDoor, out var door));
                Assert.AreEqual(HomeToWater[3], new Vector2(door.position.x, door.position.z), "route passes the door anchor");

                var main = Walk(HomeToWater);
                Assert.IsEmpty(main.Failures, Report("home -> water", main));
                Assert.IsTrue(main.ReachedSea, "the wade must end in the sea");

                var branch = Walk(BeachToDock);
                Assert.IsEmpty(branch.Failures, Report("beach -> dock", branch));
                Assert.IsFalse(branch.ReachedSea, "the dock branch stays dry");
            }, furnitureProxies: true);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryShopCustomerPointIsReachableFromTheSquare(int slot)
        {
            ContinuousTownScene.With(_ =>
            {
                var shop = ContinuousTownLayout.StorefrontPositions[slot];
                var walk = Walk(Square, new Vector2(shop.x, CustomerZ));
                Assert.IsEmpty(walk.Failures, Report("square -> slot " + slot, walk));
                Assert.IsFalse(walk.ReachedSea);
            }, furnitureProxies: true);
        }

        [Test]
        public void CharacterControllerMovedAlongTheRouteNeverDropsBelowTheWalkSurface()
        {
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldZoneField.TryFind(out var zones));
                var diver = new GameObject("Test_P46_WalkingDiver");
                SceneManager.MoveGameObjectToScene(diver, scene);
                try
                {
                    var cc = diver.AddComponent<CharacterController>();
                    cc.height = Height;
                    cc.radius = Radius;
                    cc.center = new Vector3(0f, CenterY, 0f);
                    cc.slopeLimit = SlopeLimit;
                    cc.stepOffset = StepOffset;
                    cc.skinWidth = Skin;

                    // Precondition: Move must actually sweep against the scene in EditMode.
                    diver.transform.position = new Vector3(0f, WalkY + 0.5f, -29f);
                    Physics.SyncTransforms();
                    for (var i = 0; i < 60; i++) cc.Move(Vector3.down * 0.05f);
                    Assert.AreEqual(WalkY, diver.transform.position.y, 0.1f, "CharacterController.Move does not land on the square in EditMode");
                    Assert.IsTrue(cc.isGrounded, "CharacterController.Move reports no ground in EditMode");

                    var failures = new List<string>();
                    Drive(cc, zones, HomeToWater, failures, out var reachedSea);
                    Assert.IsTrue(reachedSea, "the controller must reach the sea down the wade");
                    Drive(cc, zones, BeachToDock, failures, out var dockSea);
                    Assert.IsFalse(dockSea);
                    Assert.IsEmpty(failures, string.Join("\n", failures.Take(20)));
                }
                finally
                {
                    Object.DestroyImmediate(diver);
                }
            }, furnitureProxies: true);
        }

        // ---- shared walk criterion -------------------------------------------------------------

        internal sealed class WalkResult
        {
            public readonly List<string> Failures = new List<string>();
            public bool ReachedSea;
            public int Samples;
        }

        internal static WalkResult Walk(params Vector2[] waypoints)
        {
            Assert.IsTrue(WorldZoneField.TryFind(out var zones), "zone field missing");
            var result = new WalkResult();
            float? previous = null;
            for (var leg = 0; leg + 1 < waypoints.Length; leg++)
            {
                var a = waypoints[leg];
                var b = waypoints[leg + 1];
                var count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / SampleSpacing));
                for (var i = leg == 0 ? 0 : 1; i <= count; i++)
                {
                    var p = Vector2.Lerp(a, b, (float)i / count);
                    result.Samples++;
                    if (!Physics.Raycast(new Vector3(p.x, RayStartY, p.y), Vector3.down, out var hit, RayLength,
                            ~0, QueryTriggerInteraction.Ignore))
                    {
                        result.Failures.Add($"missing ground ({p.x:0.##}, {p.y:0.##})");
                        previous = null;
                        continue;
                    }

                    var y = hit.point.y;
                    if (y < WaterSurfaceY)
                    {
                        if (zones.ResolveZone(hit.point) == PlayerZone.Sea)
                        {
                            result.ReachedSea = true;
                            return result;
                        }
                        result.Failures.Add($"below the walk surface but not sea ({p.x:0.##}, {p.y:0.##}) y={y:0.###} on {hit.collider.name}");
                        previous = null;
                        continue;
                    }

                    if (previous.HasValue && Mathf.Abs(y - previous.Value) > StepOffset &&
                        Vector3.Angle(hit.normal, Vector3.up) > SlopeLimit)
                        result.Failures.Add($"step ({p.x:0.##}, {p.y:0.##}) dy={y - previous.Value:0.###}");
                    previous = y;

                    var blockers = CapsuleBlockers(hit.point);
                    if (blockers.Count > 0)
                        result.Failures.Add($"capsule ({p.x:0.##}, {p.y:0.##}) hits {string.Join(",", blockers)}");
                }
            }
            return result;
        }

        internal static List<string> CapsuleBlockers(Vector3 feet)
        {
            var lift = Skin + 0.01f;
            var hits = Physics.OverlapCapsule(
                feet + Vector3.up * (lift + Radius),
                feet + Vector3.up * (lift + Height - Radius),
                Radius, ~0, QueryTriggerInteraction.Ignore);
            return hits.Where(c => !IsWalkSurface(c)).Select(c => c.name).Distinct().OrderBy(n => n).ToList();
        }

        internal static bool IsWalkSurface(Collider collider) => WalkSurfaces.Contains(collider.name);

        internal static string Report(string label, WalkResult walk) =>
            $"{label}: {walk.Failures.Count} bad of {walk.Samples} samples\n" + string.Join("\n", walk.Failures.Take(30));

        // Steers a real CharacterController through the waypoints with Move(step + gravity*dt).
        private static void Drive(CharacterController cc, IPlayerZoneSource zones, Vector2[] waypoints,
            List<string> failures, out bool reachedSea)
        {
            const float dt = 0.02f;
            const float speed = 4f;
            reachedSea = false;
            var t = cc.transform;
            t.position = new Vector3(waypoints[0].x, WalkY + Skin, waypoints[0].y);
            Physics.SyncTransforms();
            var vy = 0f;
            for (var w = 1; w < waypoints.Length; w++)
            {
                var target = waypoints[w];
                var guard = 0;
                while (true)
                {
                    var here = new Vector2(t.position.x, t.position.z);
                    var toTarget = target - here;
                    if (toTarget.magnitude <= 0.05f) break;
                    if (++guard > 2000)
                    {
                        failures.Add($"stuck at ({here.x:0.##}, {here.y:0.##}) heading to {target}");
                        return;
                    }
                    var step = Vector2.ClampMagnitude(toTarget, speed * dt);
                    vy = cc.isGrounded ? -1f : vy - 9.81f * dt;
                    cc.Move(new Vector3(step.x, vy * dt, step.y));

                    var feet = t.position;
                    if (zones.ResolveZone(feet) == PlayerZone.Sea)
                    {
                        reachedSea = true;
                        return;
                    }
                    if (!Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var ground, 3f, ~0,
                            QueryTriggerInteraction.Ignore))
                        failures.Add($"controller over no ground at ({feet.x:0.##}, {feet.z:0.##}) y={feet.y:0.###}");
                    else if (ground.collider.name == "Beach_Wade" ? feet.y < ground.point.y - 0.05f : feet.y < WalkY - 0.05f)
                        failures.Add($"controller dropped to y={feet.y:0.###} at ({feet.x:0.##}, {feet.z:0.##}) over {ground.collider.name}");

                    var embedded = Physics.OverlapCapsule(
                            feet + Vector3.up * (Radius + 0.02f), feet + Vector3.up * (Height - Radius - 0.02f),
                            Radius - 0.02f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c != cc).Select(c => c.name).Distinct().ToArray();
                    if (embedded.Length > 0)
                        failures.Add($"controller embedded at ({feet.x:0.##}, {feet.z:0.##}) in {string.Join(",", embedded)}");
                }
            }
        }
    }
}
