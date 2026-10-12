using System.Collections.Generic;
using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    // P4.6-A: exercise the real authored beach + deterministic continuous town topology,
    // rather than asserting coordinates from a disconnected mock map.
    public sealed class ContinuousTownLayoutTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        [Test]
        public void BeachTownSquareAndHouseAreWalkableOnOneUnbrokenHeight()
        {
            WithTown(scene =>
            {
                Physics.SyncTransforms();
                var destinations = new[]
                {
                    new Vector3(0f, 0f, -13.5f),
                    new Vector3(0f, 0f, -18f),
                    new Vector3(0f, 0f, -24f),
                    new Vector3(0f, 0f, -29f),
                    new Vector3(0f, 0f, -35f),
                    new Vector3(0f, 0f, -40f),
                    new Vector3(0f, 0f, -46f),
                    new Vector3(0f, 0f, -50f)
                };
                foreach (var target in destinations)
                {
                    var from = new Vector3(target.x, ContinuousTownLayout.WalkSurfaceY + 1.4f, target.z);
                    Assert.IsTrue(Physics.Raycast(from, Vector3.down, out var hit, 2.0f,
                        ~0, QueryTriggerInteraction.Ignore), "missing walkable terrain at " + target);
                    Assert.AreEqual(ContinuousTownLayout.WalkSurfaceY, hit.point.y, 0.03f,
                        "ground seam/step at " + target);
                    Assert.IsFalse(hit.collider.isTrigger);
                }
            });
        }

        [Test]
        public void EveryShopIsSeparateAndTheExistingServiceIdentityIsRetained()
        {
            WithTown(scene =>
            {
                var anchors = scene.GetRootGameObjects().SelectMany(r =>
                    r.GetComponentsInChildren<ServicePointAnchor>(true)).ToArray();
                Assert.AreEqual(4, anchors.Length);
                var ids = new[] { "shop-equipment", "buyer-fish", "buyer-recording", "vendor-vehicle" };
                for (var i = 0; i < ids.Length; i++)
                {
                    var shop = anchors.Single(a => a.Definition.ServiceId == ids[i]);
                    Assert.AreEqual(ContinuousTownLayout.StorefrontPositions[i], shop.transform.position);
                    Assert.AreEqual(ContinuousTownLayout.WalkSurfaceY, shop.transform.position.y);
                    for (var j = i + 1; j < ids.Length; j++)
                        Assert.Greater(Vector3.Distance(ContinuousTownLayout.StorefrontPositions[i],
                            ContinuousTownLayout.StorefrontPositions[j]), 4f, "storefront overlap");
                }
            });
        }

        [Test]
        public void BuildingTwiceDoesNotDuplicateTownOrChangeAnchors()
        {
            WithTown(scene =>
            {
                var root = scene.GetRootGameObjects().Single(r => r.name == ContinuousTownLayout.RootName);
                var before = root.GetComponentsInChildren<Transform>(true).Length;
                ContinuousTownLayout.Build(scene);
                Assert.AreEqual(1, scene.GetRootGameObjects().Count(r => r.name == ContinuousTownLayout.RootName));
                Assert.AreEqual(before, root.GetComponentsInChildren<Transform>(true).Length);
            });
        }

        private static void WithTown(System.Action<Scene> assertions)
        {
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            var opened = !existing.IsValid() || !existing.isLoaded;
            var scene = opened
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : existing;
            var shops = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<ServicePointAnchor>(true))
                .ToArray();
            var positions = new Dictionary<ServicePointAnchor, Pose>();
            foreach (var shop in shops)
                positions[shop] = new Pose(shop.transform.position, shop.transform.rotation);
            try
            {
                ContinuousTownLayout.Build(scene);
                assertions(scene);
            }
            finally
            {
                foreach (var item in positions)
                    if (item.Key != null) item.Key.transform.SetPositionAndRotation(item.Value.position,
                        item.Value.rotation);
                var root = scene.GetRootGameObjects()
                    .FirstOrDefault(r => r.name == ContinuousTownLayout.RootName);
                if (root != null) Object.DestroyImmediate(root);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
