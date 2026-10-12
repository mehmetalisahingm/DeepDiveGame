using System.Linq;
using DeepDive.Composition;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    public sealed class CoastalHarborLinkTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        [Test]
        public void RealBeachRampAndPierHaveAContinuousWalkableContact()
        {
            WithScene(scene =>
            {
                Assert.IsTrue(CoastalHarborLink.TryValidate(scene, out var error), error);
                var roots = scene.GetRootGameObjects();
                var beach = roots.Single(x => x.name == "Beach_Platform").GetComponent<Collider>();
                var ledge = roots.Single(x => x.name == "Shore_Ledge").GetComponent<Collider>();
                var dock = roots.Single(x => x.name == "Dock_Town").GetComponent<Collider>();
                Assert.IsFalse(beach.isTrigger);
                Assert.IsFalse(ledge.isTrigger);
                Assert.IsFalse(dock.isTrigger);
                Assert.AreEqual(8.4f, dock.bounds.max.y, 0.06f);
                Assert.AreEqual(beach.bounds.max.z, ledge.bounds.min.z, 0.06f);
                Assert.AreEqual(ledge.bounds.max.z, dock.bounds.min.z, 0.06f);
            });
        }

        [Test]
        public void PierPointersDoNotIntroduceBoatOrCollisionAuthority()
        {
            WithScene(scene =>
            {
                CoastalHarborLink.BuildMarkers(scene);
                CoastalHarborLink.BuildMarkers(scene);
                var root = scene.GetRootGameObjects().Single(x => x.name == CoastalHarborLink.RootName);
                Assert.AreEqual(3, root.GetComponentsInChildren<Renderer>().Length);
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>().Length);
                Assert.IsNull(root.GetComponent<Unity.Netcode.NetworkObject>());
                Assert.IsNull(root.GetComponent<DeepDive.Trip.BoatTripManager>());
                Object.DestroyImmediate(root);
            });
        }

        private static void WithScene(System.Action<Scene> assertions)
        {
            var previous = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            var opened = !already.IsValid() || !already.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            try { assertions(scene); }
            finally
            {
                var root = scene.GetRootGameObjects().FirstOrDefault(x => x.name == CoastalHarborLink.RootName);
                if (root != null) Object.DestroyImmediate(root);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
