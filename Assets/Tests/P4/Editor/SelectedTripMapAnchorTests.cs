using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DeepDive.P4.Tests
{
    public sealed class SelectedTripMapAnchorTests
    {
        private const string WorldScene = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        [Test]
        public void EveryAuthoredBoatRouteUsesItsOwnAnchorageOnTheMap()
        {
            var prior = SceneManager.GetActiveScene();
            var loaded = SceneManager.GetSceneByPath(WorldScene);
            var opened = !loaded.IsValid() || !loaded.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Additive) : loaded;
            try
            {
                Assert.AreEqual(DiveRouteAnchors.AnchorPoint,
                    BoatMapView.ResolveAnchorForRoute(BoatTripIds.NearRouteId));
                Assert.AreEqual(DiveRouteAnchors.ReefAnchorPoint,
                    BoatMapView.ResolveAnchorForRoute(BoatTripIds.ReefRouteId));
                Assert.AreEqual(DiveRouteAnchors.DeepAnchorPoint,
                    BoatMapView.ResolveAnchorForRoute(BoatTripIds.DeepRouteId));
                Assert.AreEqual(DiveRouteAnchors.AnchorPoint,
                    BoatMapView.ResolveAnchorForRoute(string.Empty), "legacy default before first trip");
                Assert.AreEqual(string.Empty,
                    BoatMapView.ResolveAnchorForRoute("route-invalid-unknown"),
                    "unknown route must not guess the near anchorage");
            }
            finally
            {
                if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
