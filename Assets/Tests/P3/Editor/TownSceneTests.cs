using System.Linq;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.P3.Tests
{
    // Guards the P3.2-C town: three physical NPC service points in PrepArea, the scene the session loads for
    // Lobby/Prep/Return (DiveTestArea is Dive only, where service interaction is refused). Produced by
    // TownServiceSetup.Apply; if this fails, re-running that menu item is the repair. Expectations are written
    // out here, not imported from the setup script, so the guard cannot follow the script into a mistake.
    public class TownSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/PrepArea.unity";
        private const float RoomHalfExtent = 4.9f;       // the P1 collision check keeps players inside this
        private const float EyeHeight = 1.55f;
        private static readonly Vector3 DiveEntry = new Vector3(0f, 1f, 4f);

        private Scene scene;
        private Scene previousActive;
        private bool opened;

        [SetUp]
        public void OpenScene()
        {
            previousActive = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            opened = !already.IsValid() || !already.isLoaded;
            scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
            SceneManager.SetActiveScene(scene);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void CloseScene()
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private ServicePointAnchor[] Anchors() =>
            scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ServicePointAnchor>(true)).ToArray();

        [Test]
        public void SceneHasExactlyOneAnchorPerCatalogService()
        {
            var anchors = Anchors();
            Assert.AreEqual(TownServiceCatalog.All.Count, anchors.Length,
                "run DeepDive/P3.2/Prep area: town service NPCs");
            foreach (var expected in TownServiceCatalog.All)
            {
                var matches = anchors.Where(a => a.Definition.ServiceId == expected.ServiceId).ToArray();
                Assert.AreEqual(1, matches.Length, expected.ServiceId);
                var actual = matches[0].Definition;
                Assert.AreEqual(expected.ServiceType, actual.ServiceType, expected.ServiceId);
                Assert.AreEqual(expected.WorldAnchor, actual.WorldAnchor, expected.ServiceId);
                Assert.AreEqual(expected.InteractionDistance, actual.InteractionDistance, 0.001f, expected.ServiceId);
                Assert.IsTrue(TownServiceCatalog.Matches(actual), expected.ServiceId);
                Assert.IsTrue(ServiceInteractionRules.IsValid(actual), expected.ServiceId);
            }
        }

        [Test]
        public void ServicesLiveInThePhaseSceneThatAllowsThem()
        {
            // Service interaction is refused while DiveActive, and Dive is the only phase that loads
            // DiveTestArea. An NPC there could never be used.
            var dive = SceneManager.GetSceneByPath("Assets/DeepDive/World/Scenes/DiveTestArea.unity");
            if (dive.IsValid() && dive.isLoaded)
                Assert.IsEmpty(dive.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ServicePointAnchor>(true)));
            Assert.AreEqual("PrepArea", scene.name);
        }

        [Test]
        public void EveryNpcStandsInsideTheRoomOnTheFloorAtDistinctSpotsClearOfTheDiveEntry()
        {
            var anchors = Anchors();
            foreach (var anchor in anchors)
            {
                var p = anchor.WorldPosition;
                Assert.Less(Mathf.Abs(p.x), RoomHalfExtent, anchor.name);
                Assert.Less(Mathf.Abs(p.z), RoomHalfExtent, anchor.name);
                Assert.AreEqual(0f, p.y, 0.01f, anchor.name + " stands on the floor");
                Assert.Greater(Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(DiveEntry.x, 0f, DiveEntry.z)),
                    2f, anchor.name + " must not block the dive entry");
            }
            for (var i = 0; i < anchors.Length; i++)
                for (var j = i + 1; j < anchors.Length; j++)
                    Assert.Greater(Vector3.Distance(anchors[i].WorldPosition, anchors[j].WorldPosition), 1.5f,
                        $"{anchors[i].name} and {anchors[j].name} overlap");
        }

        [Test]
        public void EveryNpcCanBeAimedAtAndResolvesToItsOwnAnchor()
        {
            foreach (var anchor in Anchors())
            {
                // A player standing 1.8 m into the room, looking at the wall the NPCs face away from.
                var origin = anchor.WorldPosition + new Vector3(1.8f, EyeHeight, 0f);
                Assert.IsTrue(Physics.Raycast(origin, Vector3.left, out var hit, 3.25f, ~0,
                    QueryTriggerInteraction.Collide), anchor.name + " has no collider to aim at");
                var found = hit.collider.GetComponentInParent<ServicePointAnchor>();
                Assert.IsNotNull(found, anchor.name);
                Assert.AreEqual(anchor.Definition.ServiceId, found.Definition.ServiceId);
            }
        }

        [Test]
        public void EachServiceIsInRangeFromInFrontOfItAndOutOfRangeFromTheDiveEntry()
        {
            foreach (var anchor in Anchors())
            {
                var infront = anchor.WorldPosition + new Vector3(1.8f, 0f, 0f);
                Assert.AreEqual(PlayerActionResult.Accepted,
                    ServiceInteractionRules.Validate(anchor.Definition, infront, anchor.WorldPosition, true), anchor.name);
                Assert.AreEqual(PlayerActionResult.InvalidTarget,
                    ServiceInteractionRules.Validate(anchor.Definition, new Vector3(0f, 0f, 4f), anchor.WorldPosition, true),
                    anchor.name + " must not be reachable from the dive entry");
            }
        }
    }
}
