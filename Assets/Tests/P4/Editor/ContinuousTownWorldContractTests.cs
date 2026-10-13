using System;
using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.World;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // DiveTestArea opened additively with the #143 town built on top, exactly as the layout tests do.
    // Optionally adds the runtime-built furniture: the REAL home rig (HomeSceneSetup.EnsureRig on the anchors,
    // #138 PR-2) and stand-in colliders for the four storefront counters, which only appear on a real scene load.
    internal static class ContinuousTownScene
    {
        public const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        public const string ProxyRootName = "Test_P46_RuntimeFurnitureProxies";

        public static void With(Action<Scene> assertions, bool furnitureProxies = false)
        {
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            var opened = !existing.IsValid() || !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existing;
            var shops = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<ServicePointAnchor>(true))
                .ToArray();
            var positions = new Dictionary<ServicePointAnchor, Pose>();
            foreach (var shop in shops)
                positions[shop] = new Pose(shop.transform.position, shop.transform.rotation);
            GameObject proxies = null;
            try
            {
                ContinuousTownLayout.Build(scene);
                if (furnitureProxies) proxies = BuildFurnitureProxies(scene);
                Physics.SyncTransforms();
                assertions(scene);
            }
            finally
            {
                if (proxies != null) Object.DestroyImmediate(proxies);
                foreach (var name in new[] { HomeSceneSetup.RootName, HomePcAnchor.PcName })
                foreach (var leftover in scene.GetRootGameObjects().Where(r => r.name == name).ToArray())
                    Object.DestroyImmediate(leftover);
                foreach (var item in positions)
                    if (item.Key != null)
                        item.Key.transform.SetPositionAndRotation(item.Value.position, item.Value.rotation);
                var root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == ContinuousTownLayout.RootName);
                if (root != null) Object.DestroyImmediate(root);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
                Physics.SyncTransforms();
            }
        }

        // The home rig is the real one (its placement is locked by HomeRigAnchorTests); the counters are
        // written TownStorefront values, not read from it.
        private static GameObject BuildFurnitureProxies(Scene scene)
        {
            HomeSceneSetup.EnsureRig(scene);
            var root = new GameObject(ProxyRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var shop in ContinuousTownLayout.StorefrontPositions)
                Proxy(root, "Counter_" + shop.x, shop + new Vector3(0f, 0.5f, 0.65f), new Vector3(1.8f, 1.0f, 0.5f));
            return root;
        }

        private static void Proxy(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        public static Transform TownChild(Scene scene, string name)
        {
            var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName);
            var child = root.transform.Find(name);
            Assert.IsNotNull(child, ContinuousTownLayout.RootName + "/" + name + " missing");
            return child;
        }
    }

    // #138 PR-1: the anchors and zone volumes bound into the #143 town. Expectations are written
    // numbers; only HomeOrigin and StorefrontPositions are deliberately shared with the layout.
    public sealed class ContinuousTownWorldContractTests
    {
        private const float Tolerance = 0.001f;
        private const float DiverRadius = 0.35f;
        private const float DiverHeight = 1.8f;
        private const float DiverSkin = 0.08f;

        [Test]
        public void AnchorCountsMatchBedsPlayersAndServiceSlots()
        {
            Assert.AreEqual(4, WorldAnchorIds.HomeBedCount);
            Assert.AreEqual(DayIds.BedCount, WorldAnchorIds.HomeBedCount);
            Assert.AreEqual(AdmissionPolicy.MaxPlayers, WorldAnchorIds.HomeBedCount);
            Assert.AreEqual(AdmissionPolicy.MaxPlayers, WorldAnchorIds.HomeSpawnCount);
            Assert.AreEqual(ContinuousTownLayout.StorefrontPositions.Length, WorldAnchorIds.TownServiceSlotCount);
            Assert.AreEqual(4, WorldAnchorIds.TownServiceSlotCount);
        }

        [Test]
        public void EveryWorldAnchorIdIsBuiltExactlyOnceUnderTheTownRoot()
        {
            ContinuousTownScene.With(scene =>
            {
                var anchorsRoot = ContinuousTownScene.TownChild(scene, ContinuousTownLayout.AnchorsName);
                var built = anchorsRoot.GetComponentsInChildren<WorldAnchor>(true);
                Assert.AreEqual(17, built.Length);
                CollectionAssert.AreEquivalent(WorldAnchorIds.All, built.Select(a => a.Id));
                foreach (var id in WorldAnchorIds.All)
                {
                    Assert.IsTrue(WorldAnchors.TryFind(id, out var anchor), "TryFind " + id);
                    Assert.AreSame(anchorsRoot, anchor.transform.parent, id + " parent");
                    Assert.AreEqual(id, anchor.name);
                }
            });
        }

        // #138 PR-2: the beds moved apart for the corridors ({-3,-1,1,3} -> {-3.45,-1.15,1.15,3.45}). The old
        // block that locked HomeRig to the copies inside HomeSceneSetup/HomePcAnchor is gone: the DiveTestArea rig
        // has no copy any more, it reads these anchors (locked by HomeRigAnchorTests), and PrepArea's legacy
        // values are now deliberately different (locked by PrepAreaKeepsItsLegacyRig).
        [Test]
        public void HomeAnchorsSitOnTheWrittenRigPlacement()
        {
            ContinuousTownScene.With(_ =>
            {
                AssertPose(WorldAnchorIds.HomeRoot, new Vector3(0f, 8.4f, -48f), 0f);
                AssertPose(WorldAnchorIds.HomeDoor, new Vector3(0f, 8.4f, -42f), 0f);
                var bedXs = new[] { -3.45f, -1.15f, 1.15f, 3.45f };
                for (var i = 0; i < bedXs.Length; i++)
                    AssertPose(WorldAnchorIds.HomeBed(i), new Vector3(bedXs[i], 8.7f, -44.55f), 180f);
                AssertPose(WorldAnchorIds.HomeStorage, new Vector3(-3.9f, 9.2f, -51.55f), 0f);
                AssertPose(WorldAnchorIds.HomePc, new Vector3(3.9f, 9.0f, -51.55f), 0f);
                AssertPose(WorldAnchorIds.HomeSpawn(0), new Vector3(-1.5f, 8.4f, -49.5f), 0f);
                AssertPose(WorldAnchorIds.HomeSpawn(1), new Vector3(1.5f, 8.4f, -49.5f), 0f);
                AssertPose(WorldAnchorIds.HomeSpawn(2), new Vector3(-1.5f, 8.4f, -47.5f), 0f);
                AssertPose(WorldAnchorIds.HomeSpawn(3), new Vector3(1.5f, 8.4f, -47.5f), 0f);
                AssertPose(WorldAnchorIds.TownSquare, new Vector3(0f, 8.4f, -29f), 0f);
            });
        }

        [Test]
        public void HomeSpawnsAndTheDoorStandOnTheFloorClearOfFurnitureAndWalls()
        {
            ContinuousTownScene.With(_ =>
            {
                var ids = Enumerable.Range(0, WorldAnchorIds.HomeSpawnCount).Select(WorldAnchorIds.HomeSpawn)
                    .Append(WorldAnchorIds.HomeDoor);
                foreach (var id in ids)
                {
                    Assert.IsTrue(WorldAnchors.TryGetPose(id, out var pose), id);
                    var feet = pose.position;
                    Assert.IsTrue(Physics.Raycast(new Vector3(feet.x, 9.8f, feet.z), Vector3.down, out var hit, 2.0f,
                        ~0, QueryTriggerInteraction.Ignore), "no floor under " + id);
                    Assert.AreEqual(8.4f, hit.point.y, 0.03f, "floor height under " + id);
                    var lift = DiverSkin + 0.01f;
                    Assert.IsFalse(Physics.CheckCapsule(
                            feet + Vector3.up * (lift + DiverRadius),
                            feet + Vector3.up * (lift + DiverHeight - DiverRadius),
                            DiverRadius, ~0, QueryTriggerInteraction.Ignore),
                        id + " capsule overlaps furniture or a wall");
                }
            }, furnitureProxies: true);
        }

        [Test]
        public void ServiceSlotsAreTheStorefrontPositionsAndTheirCustomerPointsHaveGround()
        {
            ContinuousTownScene.With(_ =>
            {
                for (var i = 0; i < WorldAnchorIds.TownServiceSlotCount; i++)
                {
                    Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.TownServiceSlot(i), out var pose));
                    Assert.AreEqual(ContinuousTownLayout.StorefrontPositions[i], pose.position, "slot " + i);
                    Assert.AreEqual(-29f, pose.position.z, Tolerance);
                    var customer = pose.position + new Vector3(0f, 0f, TownStorefront.CustomerSpot);
                    Assert.AreEqual(-27.3f, customer.z, Tolerance);
                    Assert.IsTrue(Physics.Raycast(new Vector3(customer.x, 9.8f, customer.z), Vector3.down,
                        out var hit, 2.0f, ~0, QueryTriggerInteraction.Ignore), "no ground at customer point " + i);
                    Assert.AreEqual(8.4f, hit.point.y, 0.03f, "customer point " + i);
                }
            });
        }

        [Test]
        public void ZoneVolumesAreDataAndThereIsStillExactlyOneWaterAuthority()
        {
            ContinuousTownScene.With(scene =>
            {
                var zones = ContinuousTownScene.TownChild(scene, ContinuousTownLayout.ZonesName);
                Assert.AreEqual(0, zones.GetComponentsInChildren<Collider>(true).Length, "a zone carries a collider");
                var volumes = zones.GetComponentsInChildren<WorldZoneVolume>(true);
                CollectionAssert.AreEquivalent(new[] { "Zone_Home", "Zone_Dock", "Zone_Town", "Zone_Coast" },
                    volumes.Select(v => v.name));
                AssertZone(volumes, "Zone_Home", PlayerZone.Home, new Vector3(-5.825f, 7.9f, -53.825f), new Vector3(5.825f, 11.4f, -42f));
                AssertZone(volumes, "Zone_Dock", PlayerZone.Dock, new Vector3(7f, 7.9f, -11f), new Vector3(11f, 13f, -3.5f));
                AssertZone(volumes, "Zone_Town", PlayerZone.Town, new Vector3(-14.75f, 7.9f, -55.8f), new Vector3(14.75f, 13f, -14.75f));
                AssertZone(volumes, "Zone_Coast", PlayerZone.Coast, new Vector3(-15f, 4.5f, -14.75f), new Vector3(15f, 13f, -3.5f));

                var waterFields = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaterField>(true)).ToArray();
                Assert.AreEqual(1, waterFields.Length, "one WaterField");
                Assert.AreEqual(1, waterFields[0].VolumeCount, "one SwimVolume");
                var field = zones.GetComponent<WorldZoneField>();
                Assert.IsNotNull(field);
                Assert.AreSame(waterFields[0], field.Water, "zones read the scene's own WaterField");
                Assert.AreEqual(1, field.Water.Bodies.Count);
                var body = field.Water.Bodies[0];
                Assert.AreEqual(-15f, body.MinX, Tolerance);
                Assert.AreEqual(15f, body.MaxX, Tolerance);
                Assert.AreEqual(-15f, body.MinZ, Tolerance);
                Assert.AreEqual(65f, body.MaxZ, Tolerance);
                Assert.AreEqual(8f, body.SurfaceY, Tolerance, "SwimVolume moved");
            });
        }

        [Test]
        public void TheRealPlacesResolveToTheirZones()
        {
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldZoneField.TryFind(out var zones), "exactly one zone field");
                var spawns = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerSpawnPoint>(true)).ToArray();
                Assert.AreEqual(4, spawns.Length);
                foreach (var spawn in spawns)
                    Assert.AreEqual(PlayerZone.Sea, zones.ResolveZone(spawn.transform.position), "dive spawn " + spawn.Slot);
                Assert.AreEqual(PlayerZone.Coast, zones.ResolveZone(new Vector3(0f, 8.4f, -12.5f)), "beach");
                Assert.AreEqual(PlayerZone.Dock, zones.ResolveZone(new Vector3(9f, 8.4f, -9f)), "DiveExit base");
                Assert.AreEqual(PlayerZone.Town, zones.ResolveZone(new Vector3(0f, 8.4f, -29f)), "square");
                Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeSpawn(0), out var homeSpawn));
                Assert.AreEqual(PlayerZone.Home, zones.ResolveZone(homeSpawn.position), "home-spawn-0");
                Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeDoor, out var door));
                Assert.AreEqual(PlayerZone.Town, zones.ResolveZone(door.position + new Vector3(0f, 0f, 0.5f)), "outside the door");
                Assert.AreEqual(PlayerZone.Unknown, zones.ResolveZone(new Vector3(0f, 8.4f, -70f)), "beyond the plot");
            });
        }

        [Test]
        public void FourDiversAcrossTheRealWorldResolveIndependently()
        {
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldZoneField.TryFind(out var zones));
                Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeSpawn(0), out var home));
                var sea = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerSpawnPoint>(true))
                    .First().transform.position;
                var divers = new[]
                {
                    (feet: home.position, zone: PlayerZone.Home),
                    (feet: new Vector3(0f, 8.4f, -29f), zone: PlayerZone.Town),
                    (feet: new Vector3(0f, 8.4f, -12.5f), zone: PlayerZone.Coast),
                    (feet: sea, zone: PlayerZone.Sea)
                };
                foreach (var order in Permutations(new[] { 0, 1, 2, 3 }))
                {
                    var results = new PlayerZone[divers.Length];
                    foreach (var i in order) results[i] = zones.ResolveZone(divers[i].feet);
                    for (var i = 0; i < divers.Length; i++)
                        Assert.AreEqual(divers[i].zone, results[i], "diver " + i + " order " + string.Join(",", order));
                }
            });
        }

        [Test]
        public void BuildingTwiceKeepsOneAnchorAndZoneSet()
        {
            ContinuousTownScene.With(scene =>
            {
                ContinuousTownLayout.Build(scene);
                var roots = scene.GetRootGameObjects().Where(r => r.name == ContinuousTownLayout.RootName).ToArray();
                Assert.AreEqual(1, roots.Length);
                Assert.AreEqual(17, roots[0].GetComponentsInChildren<WorldAnchor>(true).Length);
                Assert.AreEqual(4, roots[0].GetComponentsInChildren<WorldZoneVolume>(true).Length);
                Assert.AreEqual(1, roots[0].GetComponentsInChildren<WorldZoneField>(true).Length);
                Assert.IsTrue(WorldZoneField.TryFind(out _));
                foreach (var id in WorldAnchorIds.All) Assert.IsTrue(WorldAnchors.TryFind(id, out _), id);
            });
        }

        [Test]
        public void NoAnchorOrZoneCarriesANetworkObjectRendererOrCollider()
        {
            ContinuousTownScene.With(scene =>
            {
                foreach (var name in new[] { ContinuousTownLayout.AnchorsName, ContinuousTownLayout.ZonesName })
                {
                    var subtree = ContinuousTownScene.TownChild(scene, name);
                    Assert.AreEqual(0, subtree.GetComponentsInChildren<NetworkObject>(true).Length, name + " NetworkObject");
                    Assert.AreEqual(0, subtree.GetComponentsInChildren<Renderer>(true).Length, name + " Renderer");
                    Assert.AreEqual(0, subtree.GetComponentsInChildren<Collider>(true).Length, name + " Collider");
                    Assert.AreEqual(0, subtree.GetComponentsInChildren<MeshFilter>(true).Length, name + " MeshFilter");
                }
            });
        }

        private static void AssertPose(string id, Vector3 position, float yaw)
        {
            Assert.IsTrue(WorldAnchors.TryGetPose(id, out var pose), id);
            AssertNear(position, pose.position, id);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(0f, yaw, 0f), pose.rotation), 0.01f, id + " yaw");
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, label + " x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, label + " y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, label + " z");
        }

        private static void AssertZone(WorldZoneVolume[] volumes, string name, PlayerZone zone, Vector3 min, Vector3 max)
        {
            var volume = volumes.Single(v => v.name == name);
            Assert.IsTrue(volume.TryGetEntry(out var entry), name);
            Assert.AreEqual(zone, entry.Zone, name);
            AssertNear(min, entry.Box.Min, name + " min");
            AssertNear(max, entry.Box.Max, name + " max");
        }

        private static IEnumerable<int[]> Permutations(int[] items)
        {
            if (items.Length <= 1)
            {
                yield return items;
                yield break;
            }
            for (var i = 0; i < items.Length; i++)
            {
                var rest = items.Where((_, j) => j != i).ToArray();
                foreach (var tail in Permutations(rest))
                    yield return new[] { items[i] }.Concat(tail).ToArray();
            }
        }
    }
}
