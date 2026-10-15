using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
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
        private const float WalkY = ContinuousTownLayout.WalkSurfaceY;
        private const float WaterSurfaceY = 8.0f;

        // #153: the town's own pieces come from ContinuousTownLayout; only the scene-authored coast is named here.
        private static readonly string[] SceneWalkSurfaces = { "Beach_Platform", "Beach_Wade", "Shore_Ledge", "Shore_Ramp", "Dock_Town" };

        private static readonly string[] WalkSurfaces = SceneWalkSurfaces
            .Concat(ContinuousTownLayout.Slabs.Select(b => b.Name))
            .Concat(ContinuousTownLayout.Fills.Select(b => b.Name)).ToArray();

        private static readonly string[] FillNames = ContinuousTownLayout.Fills.Select(b => b.Name).ToArray();

        private static readonly string[] EdgeNames = ContinuousTownLayout.Edges.Select(b => b.Name).ToArray();

        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
        private static Vector2 HomeLocal(float x, float z) => XZ(ContinuousTownLayout.HomeOrigin) + new Vector2(x, z);
        private static Vector2 Customer(Vector3 shop) => new Vector2(shop.x, shop.z + TownStorefront.CustomerSpot);

        private static readonly Vector2 HomeInside = HomeLocal(0f, -2f);
        private static readonly Vector2 Square = XZ(ContinuousTownLayout.TownSquareCenter) + new Vector2(0f, 5f);

        // Rota 36: house -> side corridor -> along the bed ends -> door -> approach -> square -> street
        // -> coast walk -> beach -> top of the wade -> down the wade into the sea.
        // From the street on, the points sit on the scene-authored coast (beach, wade) and stay written.
        private const int DoorWaypoint = 3;
        private static readonly Vector2[] HomeToWater =
        {
            HomeInside, HomeLocal(-4.7f, -2f), HomeLocal(-4.7f, 5f), XZ(ContinuousTownLayout.HomeRig.Door),
            XZ(ContinuousTownLayout.TownSquareCenter) + new Vector2(0f, -7f), Square, new Vector2(0f, -16f),
            new Vector2(0f, -12.5f), new Vector2(-6.5f, -11.2f), new Vector2(-6.5f, -5f)
        };

        // The branch: beach -> ledge -> dock (scene-authored).
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
                Assert.AreEqual(HomeToWater[DoorWaypoint], new Vector2(door.position.x, door.position.z), "route passes the door anchor");

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
                var walk = Walk(Square, Customer(shop));
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
                    diver.transform.position = ContinuousTownLayout.TownSquareCenter + Vector3.up * 0.5f;
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

        // ---- commit 5: the #143 gaps (plan section 2 a-d) and their fix ------------------------

        [TestCase(-6.5f, -11.3f, 0, TestName = "DiagonalBeachToShopApproachesStayOnGround(wade->slot0)")]
        [TestCase(-12f, -12.5f, 0, TestName = "DiagonalBeachToShopApproachesStayOnGround(hull->slot0)")]
        [TestCase(12.5f, -12.5f, 3, TestName = "DiagonalBeachToShopApproachesStayOnGround(fueltank->slot3)")]
        [TestCase(9f, -9f, 3, TestName = "DiagonalBeachToShopApproachesStayOnGround(ledge->slot3)")]
        public void DiagonalBeachToShopApproachesStayOnGround(float fromX, float fromZ, int slot)
        {
            ContinuousTownScene.With(_ =>
            {
                var shop = ContinuousTownLayout.StorefrontPositions[slot];
                var walk = Walk(new Vector2(fromX, fromZ), Customer(shop));
                Assert.IsEmpty(walk.Failures, Report($"({fromX}, {fromZ}) -> slot {slot}", walk));
                Assert.IsFalse(walk.ReachedSea);
            }, furnitureProxies: true);
        }

        // A recorded #143 regression coordinate (wade -> west shop diagonal), not a layout value: it stays written.
        [Test]
        public void TheOldFallPointAndItsApproachFromTheWadeAreOnGround()
        {
            ContinuousTownScene.With(_ =>
            {
                Assert.IsTrue(Physics.Raycast(new Vector3(-8.4f, RayStartY, -19.7f), Vector3.down, out var hit,
                    RayLength, ~0, QueryTriggerInteraction.Ignore), "the old fall point has no ground");
                Assert.AreEqual(WalkY, hit.point.y, 0.03f);
                var walk = Walk(new Vector2(-6.5f, -11.3f), new Vector2(-8.4f, -19.7f));
                Assert.IsEmpty(walk.Failures, Report("wade -> old fall point", walk));
            });
        }

        [Test]
        public void TheBeachBackEdgeIsClosedWallToWall()
        {
            ContinuousTownScene.With(_ =>
            {
                // The strip is Fill_BeachBack: its beach face, 5cm in, every 0.25m, its back face and 0.1m beyond;
                // across it wall to wall, 5cm in from each end.
                var fill = ContinuousTownLayout.FindBlock("Fill_BeachBack");
                var zs = new List<float> { fill.Max.z, fill.Max.z - 0.05f };
                for (var z = fill.Max.z - SampleSpacing; z >= fill.Min.z - 1e-4f; z -= SampleSpacing) zs.Add(z);
                zs.Add(fill.Min.z - 0.1f);
                var minX = fill.Min.x + 0.05f;
                var maxX = fill.Max.x - 0.05f;
                var xs = Mathf.CeilToInt((maxX - minX) / SampleSpacing);
                var failures = new List<string>();
                foreach (var z in zs)
                {
                    var open = new List<float>();
                    for (var i = 0; i <= xs; i++)
                    {
                        var x = Mathf.Min(minX + i * SampleSpacing, maxX);
                        var hasHit = Physics.Raycast(new Vector3(x, RayStartY, z), Vector3.down, out var hit,
                            RayLength, ~0, QueryTriggerInteraction.Ignore);
                        if (hasHit && Mathf.Abs(hit.point.y - WalkY) <= 0.03f) continue;
                        // South of (or on) the fill's back face a fence counts as closed too.
                        if (hasHit && z <= fill.Min.z + 1e-4f && hit.collider.name.StartsWith("Edge_")) continue;
                        open.Add(x);
                    }
                    if (open.Count > 0)
                        failures.Add($"z {z}: {open.Count} open samples, x {open.First():0.##}..{open.Last():0.##}");
                }
                Assert.IsEmpty(failures, string.Join("\n", failures));
            });
        }

        [Test]
        public void EveryOuterSlabEdgeIsFencedOrDropsIntoWater()
        {
            ContinuousTownScene.With(scene =>
            {
                var failures = new List<string>();
                var samples = 0;
                foreach (var edge in OuterEdges(scene))
                {
                    var count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(edge.From, edge.To) / SampleSpacing));
                    var open = new List<Vector2>();
                    for (var i = 0; i <= count; i++)
                    {
                        samples++;
                        var onEdge = Vector2.Lerp(edge.From, edge.To, (float)i / count);
                        var outside = onEdge + edge.Outward * 0.1f;
                        if (HasWalkGround(outside)) continue;
                        if (edge.Water ? DropsIntoSwimVolume(outside) : FencedAt(onEdge + edge.Outward * 0.5f)) continue;
                        open.Add(outside);
                    }
                    if (open.Count > 0)
                        failures.Add($"{edge.Name} ({(edge.Water ? "water" : "fenced")}): {open.Count}/{count + 1} open, " +
                                     $"first ({open.First().x:0.##}, {open.First().y:0.##}) last ({open.Last().x:0.##}, {open.Last().y:0.##})");
                }
                Assert.Greater(samples, 500);
                Assert.IsEmpty(failures, failures.Count + " open edges\n" + string.Join("\n", failures));
            });
        }

        [Test]
        public void FillsAndEdgesStayOutOfSwimVolumeAndClearOfTheArenaWalls()
        {
            ContinuousTownScene.With(scene =>
            {
                var added = AddedPieces(scene);
                Assert.AreEqual(FillNames.Length + EdgeNames.Length, added.Count, FillNames.Length + " fills + " + EdgeNames.Length + " edges");
                CollectionAssert.AreEquivalent(FillNames.Concat(EdgeNames), added.Select(c => c.name));
                var swim = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SwimVolume>(true))
                    .Single().GetComponent<Collider>().bounds;
                var walls = new[] { "Wall_S", "Wall_W", "Wall_E" }
                    .Select(n => scene.GetRootGameObjects().Single(r => r.name == n).GetComponent<Collider>().bounds)
                    .ToArray();
                foreach (var piece in added)
                {
                    var b = piece.bounds;
                    Assert.IsFalse(piece.isTrigger, piece.name);
                    if (OverlapXZ(b, swim) > 0f)
                        Assert.GreaterOrEqual(b.min.y - swim.max.y, 0.05f - 1e-3f, piece.name + " vs SwimVolume");
                    foreach (var wall in walls)
                        if (OverlapXZ(b, wall) > 0f)
                            Assert.GreaterOrEqual(b.min.y - wall.max.y, 1.0f, piece.name + " vs arena wall");
                }
            });
        }

        [Test]
        public void FillsAreFlushAt8_4WithEveryNeighbourSeam()
        {
            ContinuousTownScene.With(scene =>
            {
                foreach (var name in FillNames)
                {
                    var fill = Piece(scene, name);
                    Assert.AreEqual(WalkY, fill.bounds.max.y, 1e-3f, name + " top");
                    Assert.AreEqual(WalkY - ContinuousTownLayout.FillThickness, fill.bounds.min.y, 1e-3f, name + " bottom");
                }
                // Each seam sampled 0.1m either side; both sides must read the same 8.4.
                var seams = FillSeams(scene);
                foreach (var name in FillNames)
                    Assert.IsTrue(seams.Any(s => s.fill == name), name + " meets no walk surface");
                foreach (var seam in seams)
                {
                    var count = Mathf.CeilToInt(Vector2.Distance(seam.a, seam.b) / SampleSpacing);
                    for (var i = 0; i <= count; i++)
                    {
                        var p = Vector2.Lerp(seam.a, seam.b, (float)i / count);
                        foreach (var side in new[] { -0.1f, 0.1f })
                        {
                            var q = p + seam.across * side;
                            Assert.IsTrue(Physics.Raycast(new Vector3(q.x, RayStartY, q.y), Vector3.down, out var hit,
                                RayLength, ~0, QueryTriggerInteraction.Ignore), $"no ground at seam ({q.x:0.##}, {q.y:0.##})");
                            Assert.AreEqual(WalkY, hit.point.y, 1e-3f, $"seam step at ({q.x:0.##}, {q.y:0.##}) on {hit.collider.name}");
                        }
                    }
                }
            });
        }

        [Test]
        public void NoEdgeSitsOnAWalkableSurfaceOrACustomerPoint()
        {
            ContinuousTownScene.With(scene =>
            {
                var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName);
                var surfaces = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                    .Where(c => !c.isTrigger && IsWalkSurface(c)).ToArray();
                Assert.AreEqual(WalkSurfaces.Length, surfaces.Length, "every walk surface present once");
                foreach (var name in EdgeNames)
                {
                    var edge = Piece(scene, name);
                    Assert.AreSame(root.transform, edge.transform.parent, name + " parent");
                    Assert.AreEqual(WalkY, edge.bounds.min.y, 1e-3f, name + " bottom");
                    Assert.AreEqual(WalkY + ContinuousTownLayout.EdgeHeight, edge.bounds.max.y, 1e-3f, name + " top");
                    if (name.EndsWith("West")) Assert.Less(edge.bounds.center.x, 0f, name + " is not on the west side");
                    if (name.EndsWith("East")) Assert.Greater(edge.bounds.center.x, 0f, name + " is not on the east side");
                    foreach (var surface in surfaces)
                        Assert.LessOrEqual(OverlapXZ(edge.bounds, surface.bounds), 1e-3f,
                            name + " stands on " + surface.name);
                    foreach (var shop in ContinuousTownLayout.StorefrontPositions)
                    {
                        var customer = new Vector3(shop.x, edge.bounds.center.y, Customer(shop).y);
                        Assert.GreaterOrEqual(Vector3.Distance(edge.bounds.ClosestPoint(customer), customer), 1f,
                            name + " crowds the customer point at x " + shop.x);
                    }
                }
            });
        }

        // HomePlot stops at z -42.5 while the house walls and the open facade reach -42.
        [Test]
        public void TheHouseFrontHasFloorAndNoEdgeTouchesTheHouse()
        {
            ContinuousTownScene.With(scene =>
            {
                // Between the side walls (0.125m in from their inner faces), from 0.1m inside the facade line
                // to 5cm short of HomePlot's front.
                var open = new List<string>();
                var minX = ContinuousTownLayout.FindBlock("HomeLeft").Max.x + 0.125f;
                var maxX = ContinuousTownLayout.FindBlock("HomeRight").Min.x - 0.125f;
                var nearZ = ContinuousTownLayout.HomeRig.Door.z - 0.1f;
                var farZ = ContinuousTownLayout.FindBlock("HomePlot").Max.z + 0.05f;
                var xs = Mathf.CeilToInt((maxX - minX) / SampleSpacing);
                var zs = Mathf.CeilToInt((nearZ - farZ) / SampleSpacing);
                for (var i = 0; i <= xs; i++)
                for (var j = 0; j <= zs; j++)
                {
                    var x = Mathf.Lerp(minX, maxX, (float)i / xs);
                    var z = Mathf.Lerp(nearZ, farZ, (float)j / zs);
                    if (!HasWalkGround(new Vector2(x, z))) open.Add($"({x:0.##}, {z:0.##})");
                }
                Assert.IsEmpty(open, open.Count + " samples inside the house front have no floor");

                var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName);
                var house = new List<(string name, Bounds bounds)>();
                var home = new Bounds();
                home.SetMinMax(ContinuousTownLayout.ZoneVolumes.HomeMin, ContinuousTownLayout.ZoneVolumes.HomeMax);
                house.Add(("Zone_Home", home));
                foreach (var wall in new[] { "HomeBack", "HomeLeft", "HomeRight" })
                    house.Add((wall, root.GetComponentsInChildren<Collider>(true).Single(c => c.name == wall).bounds));
                foreach (var name in EdgeNames)
                {
                    var edge = Piece(scene, name).bounds;
                    foreach (var (houseName, bounds) in house)
                        Assert.LessOrEqual(Overlap3D(edge, bounds), 1e-3f, name + " touches " + houseName);
                }
            });
        }

        // #153: a seam is the part of a fill face that another flat walk surface (town slab, other fill, or
        // scene-authored beach/ledge/dock) covers or meets, merged along the face and kept 0.1m in from its ends.
        private static List<(string fill, Vector2 a, Vector2 b, Vector2 across)> FillSeams(Scene scene)
        {
            var flat = ContinuousTownLayout.Slabs.Concat(ContinuousTownLayout.Fills)
                .Select(b => (name: b.Name, min: XZ(b.Min), max: XZ(b.Max)))
                .Concat(new[] { "Beach_Platform", "Shore_Ledge", "Dock_Town" }
                    .Select(n => SceneSurface(scene, n).bounds).Select((b, i) => (name: "scene" + i, min: XZ(b.min), max: XZ(b.max))))
                .ToList();
            const float eps = 1e-3f;
            const float inset = 0.1f;
            var seams = new List<(string, Vector2, Vector2, Vector2)>();
            foreach (var fill in ContinuousTownLayout.Fills)
            foreach (Face face in System.Enum.GetValues(typeof(Face)))
            {
                var edge = FaceEdge(fill.Name, XZ(fill.Min), XZ(fill.Max), face, false);
                var alongX = Mathf.Abs(edge.Outward.y) > 0f;     // north/south faces run along x
                var line = alongX ? edge.From.y : edge.From.x;
                var lo = alongX ? edge.From.x : edge.From.y;
                var hi = alongX ? edge.To.x : edge.To.y;
                var spans = new List<(float lo, float hi)>();
                foreach (var other in flat)
                {
                    if (other.name == fill.Name) continue;
                    var across = alongX ? (other.min.y, other.max.y) : (other.min.x, other.max.x);
                    if (line < across.Item1 - eps || line > across.Item2 + eps) continue;
                    var from = Mathf.Max(lo, alongX ? other.min.x : other.min.y);
                    var to = Mathf.Min(hi, alongX ? other.max.x : other.max.y);
                    if (to - from > eps) spans.Add((from, to));
                }
                foreach (var (from, to) in Merge(spans, eps))
                {
                    if (to - from <= 2f * inset) continue;
                    var a = alongX ? new Vector2(from + inset, line) : new Vector2(line, from + inset);
                    var b = alongX ? new Vector2(to - inset, line) : new Vector2(line, to - inset);
                    seams.Add((fill.Name, a, b, alongX ? Vector2.up : Vector2.right));
                }
            }
            return seams;
        }

        private static IEnumerable<(float lo, float hi)> Merge(List<(float lo, float hi)> spans, float eps)
        {
            var merged = new List<(float lo, float hi)>();
            foreach (var span in spans.OrderBy(s => s.lo))
            {
                if (merged.Count > 0 && span.lo <= merged[merged.Count - 1].hi + eps)
                    merged[merged.Count - 1] = (merged[merged.Count - 1].lo, Mathf.Max(merged[merged.Count - 1].hi, span.hi));
                else
                    merged.Add(span);
            }
            return merged;
        }

        private readonly struct OuterEdge
        {
            public readonly string Name;
            public readonly Vector2 From;
            public readonly Vector2 To;
            public readonly Vector2 Outward;
            public readonly bool Water;

            public OuterEdge(string name, Vector2 from, Vector2 to, Vector2 outward, bool water)
            {
                Name = name;
                From = from;
                To = to;
                Outward = outward;
                Water = water;
            }
        }

        private enum Face { North, South, West, East }

        // #153, plan section 2(c): every face of every town slab and fill must have walk ground outside it or be
        // fenced; interior faces pass on the ground test. The scene-authored coast keeps its written split:
        // the beach ends are fenced, the beach north, ledge east and dock faces drop into the water.
        private static readonly (string piece, Face face)[] SceneFencedFaces =
        {
            ("Beach_Platform", Face.West), ("Beach_Platform", Face.East)
        };

        private static readonly (string piece, Face face)[] SceneWaterFaces =
        {
            ("Beach_Platform", Face.North), ("Dock_Town", Face.North), ("Dock_Town", Face.West),
            ("Dock_Town", Face.East), ("Shore_Ledge", Face.East)
        };

        private static IEnumerable<OuterEdge> OuterEdges(Scene scene)
        {
            foreach (var block in ContinuousTownLayout.Slabs.Concat(ContinuousTownLayout.Fills))
            foreach (Face face in System.Enum.GetValues(typeof(Face)))
                yield return FaceEdge(block.Name, XZ(block.Min), XZ(block.Max), face, false);
            foreach (var (piece, face) in SceneFencedFaces)
            {
                var b = SceneSurface(scene, piece).bounds;
                yield return FaceEdge(piece, XZ(b.min), XZ(b.max), face, false);
            }
            foreach (var (piece, face) in SceneWaterFaces)
            {
                var b = SceneSurface(scene, piece).bounds;
                yield return FaceEdge(piece, XZ(b.min), XZ(b.max), face, true);
            }
        }

        private static OuterEdge FaceEdge(string piece, Vector2 min, Vector2 max, Face face, bool water)
        {
            var name = piece + "." + face;
            switch (face)
            {
                case Face.North: return new OuterEdge(name, new Vector2(min.x, max.y), new Vector2(max.x, max.y), Vector2.up, water);
                case Face.South: return new OuterEdge(name, new Vector2(min.x, min.y), new Vector2(max.x, min.y), Vector2.down, water);
                case Face.West: return new OuterEdge(name, new Vector2(min.x, min.y), new Vector2(min.x, max.y), Vector2.left, water);
                default: return new OuterEdge(name, new Vector2(max.x, min.y), new Vector2(max.x, max.y), Vector2.right, water);
            }
        }

        private static Collider SceneSurface(Scene scene, string name)
        {
            var surface = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>(true))
                .SingleOrDefault(c => c.name == name && !c.isTrigger);
            Assert.IsNotNull(surface, name + " missing");
            return surface;
        }

        private static bool HasWalkGround(Vector2 p) =>
            Physics.Raycast(new Vector3(p.x, RayStartY, p.y), Vector3.down, out var hit, RayLength, ~0,
                QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - WalkY) <= 0.03f;

        private static bool FencedAt(Vector2 p)
        {
            var feet = new Vector3(p.x, WalkY, p.y);
            var lift = Skin + 0.01f;
            return Physics.OverlapCapsule(feet + Vector3.up * (lift + Radius), feet + Vector3.up * (lift + Height - Radius),
                    Radius, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => c.name.StartsWith("Edge_"));
        }

        private static bool DropsIntoSwimVolume(Vector2 p) =>
            Physics.RaycastAll(new Vector3(p.x, RayStartY, p.y), Vector3.down, 40f, ~0, QueryTriggerInteraction.Collide)
                .Any(h => h.collider.GetComponent<SwimVolume>() != null);

        private static List<Collider> AddedPieces(Scene scene)
        {
            var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName);
            return root.GetComponentsInChildren<Collider>(true)
                .Where(c => c.name.StartsWith("Fill_") || c.name.StartsWith("Edge_")).ToList();
        }

        private static Collider Piece(Scene scene, string name)
        {
            var piece = AddedPieces(scene).SingleOrDefault(c => c.name == name);
            Assert.IsNotNull(piece, name + " missing");
            return piece;
        }

        private static float OverlapXZ(Bounds a, Bounds b) =>
            Mathf.Min(Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x),
                Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));

        // Shared faces read 0; only a real interpenetration is positive.
        private static float Overlap3D(Bounds a, Bounds b) =>
            Mathf.Min(OverlapXZ(a, b), Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y));

        // ---- shared walk criterion -------------------------------------------------------------

        internal sealed class WalkResult
        {
            public readonly List<string> Failures = new List<string>();
            public bool ReachedSea;
            public int Samples;
        }

        internal static WalkResult Walk(params Vector2[] waypoints) => WalkWithRadius(Radius, waypoints);

        // #138 PR-2: the home corridors are checked with Radius + Skin, the clearance a real CharacterController
        // needs, because a bare-radius capsule still fits through a gap the controller cannot pass.
        internal static WalkResult WalkWithRadius(float radius, params Vector2[] waypoints)
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

                    var blockers = CapsuleBlockers(hit.point, radius);
                    if (blockers.Count > 0)
                        result.Failures.Add($"capsule ({p.x:0.##}, {p.y:0.##}) hits {string.Join(",", blockers)}");
                }
            }
            return result;
        }

        internal static List<string> CapsuleBlockers(Vector3 feet) => CapsuleBlockers(feet, Radius);

        internal static List<string> CapsuleBlockers(Vector3 feet, float radius)
        {
            var lift = Skin + 0.01f;
            var hits = Physics.OverlapCapsule(
                feet + Vector3.up * (lift + radius),
                feet + Vector3.up * (lift + Height - radius),
                radius, ~0, QueryTriggerInteraction.Ignore);
            return hits.Where(c => !IsWalkSurface(c)).Select(c => c.name).Distinct().OrderBy(n => n).ToList();
        }

        internal static bool IsWalkSurface(Collider collider) => WalkSurfaces.Contains(collider.name);

        internal static string Report(string label, WalkResult walk) =>
            $"{label}: {walk.Failures.Count} bad of {walk.Samples} samples\n" + string.Join("\n", walk.Failures.Take(30));

        // Steers a real CharacterController through the waypoints with Move(step + gravity*dt).
        internal static void Drive(CharacterController cc, IPlayerZoneSource zones, Vector2[] waypoints,
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
