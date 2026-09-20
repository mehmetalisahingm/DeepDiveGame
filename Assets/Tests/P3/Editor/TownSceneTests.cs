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
    // Guards the P3.2-C town: three physical NPC service points on the DiveTestArea beach. Since PR #65 Prep, Dive and
    // Return all run in DiveTestArea (PrepArea is the lobby only) and service interaction is gated by phase, so the
    // NPCs have to stand where the divers walk back out of the water. Produced by TownServiceSetup.Apply; if this
    // fails, re-running that menu item is the repair. Expectations are written out here, not imported from the setup
    // script, so the guard cannot follow the script into a mistake. The beach object names are Utku's (#61).
    public class TownSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string LobbyScenePath = "Assets/DeepDive/World/Scenes/PrepArea.unity";
        private const float EyeHeight = 1.55f;

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

        private GameObject Root(string name)
        {
            var found = scene.GetRootGameObjects().FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(found, $"{name} missing from DiveTestArea (run the beach setup, then the town setup)");
            return found;
        }

        // The standing spot in front of an NPC: NPCs face north (+z), so a player is 1.8 m north of them.
        private static Vector3 InFront(ServicePointAnchor anchor) => anchor.WorldPosition + new Vector3(0f, 0f, 1.8f);

        [Test]
        public void SceneHasExactlyOneAnchorPerCatalogService()
        {
            var anchors = Anchors();
            Assert.AreEqual(TownServiceCatalog.All.Count, anchors.Length,
                "run DeepDive/P3.2/Dive test area: town service NPCs");
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
        public void TheLobbySceneNoLongerHoldsAnyService()
        {
            // The NPCs moved out of PrepArea (lobby only since PR #65). A second copy there would be a second
            // ServicePointAnchor per service id and an unreachable one.
            var lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.IsEmpty(lobby.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<ServicePointAnchor>(true)));
                Assert.IsNull(lobby.GetRootGameObjects().FirstOrDefault(x => x.name == "TownServices"));
            }
            finally { EditorSceneManager.CloseScene(lobby, true); }
        }

        [Test]
        public void EveryNpcStandsOnTheBeachStripAndClearOfTheGateAndTheFreeParts()
        {
            var platform = Root("Beach_Platform").GetComponent<Collider>();
            var gate = Root("Beach_TownGate").transform.position;
            var parts = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith("BoatPart_")).Select(t => t.position).ToArray();
            Assert.AreEqual(3, parts.Length, "the three free boat parts are Utku's (#61)");

            var anchors = Anchors();
            foreach (var anchor in anchors)
            {
                var p = anchor.WorldPosition;
                var b = platform.bounds;
                Assert.Greater(p.x, b.min.x + 0.5f, anchor.name);
                Assert.Less(p.x, b.max.x - 0.5f, anchor.name);
                Assert.Greater(p.z, b.min.z + 0.4f, anchor.name + " needs room behind it");
                Assert.Less(p.z, b.max.z - 0.4f, anchor.name);
                Assert.AreEqual(b.max.y, p.y, 0.01f, anchor.name + " stands on the strip top");
                Assert.Greater(Vector3.Distance(p, gate), 2.5f, anchor.name + " must not block the town gate");
                foreach (var part in parts)
                    Assert.Greater(Vector3.Distance(p, part), 2f, anchor.name + " must not sit on a free boat part");
            }
            for (var i = 0; i < anchors.Length; i++)
                for (var j = i + 1; j < anchors.Length; j++)
                    Assert.Greater(Vector3.Distance(anchors[i].WorldPosition, anchors[j].WorldPosition), 1.5f,
                        $"{anchors[i].name} and {anchors[j].name} overlap");
        }

        [Test]
        public void EveryNpcHasWalkableGroundInFrontAndTheWadeLaneStaysClear()
        {
            var platform = Root("Beach_Platform").GetComponent<Collider>();
            foreach (var anchor in Anchors())
            {
                var spot = InFront(anchor);
                Assert.IsTrue(Physics.Raycast(spot + Vector3.up * 2f, Vector3.down, out var hit, 4f, ~0,
                    QueryTriggerInteraction.Ignore), anchor.name + ": no ground where a customer stands");
                Assert.AreSame(platform, hit.collider, anchor.name + ": customer stands on the beach strip");
            }
            // Divers wade out at x -9.5..-3.5 (Utku's shelf); every NPC is east of the lane.
            foreach (var anchor in Anchors())
                Assert.Greater(anchor.WorldPosition.x, -3.5f + 1f, anchor.name + " must not narrow the wade lane");
        }

        [Test]
        public void EveryNpcCanBeAimedAtAndResolvesToItsOwnAnchor()
        {
            foreach (var anchor in Anchors())
            {
                // A customer 1.8 m north of the NPC, eye at head height, looking south at it.
                var origin = InFront(anchor) + new Vector3(0f, EyeHeight, 0f);
                Assert.IsTrue(Physics.Raycast(origin, Vector3.back, out var hit, 3.25f, ~0,
                    QueryTriggerInteraction.Collide), anchor.name + " has no collider to aim at");
                var found = hit.collider.GetComponentInParent<ServicePointAnchor>();
                Assert.IsNotNull(found, anchor.name);
                Assert.AreEqual(anchor.Definition.ServiceId, found.Definition.ServiceId);
            }
        }

        [Test]
        public void EachServiceIsInRangeFromInFrontOfItAndOutOfRangeFromTheDiveArea()
        {
            foreach (var anchor in Anchors())
            {
                Assert.AreEqual(PlayerActionResult.Accepted,
                    ServiceInteractionRules.Validate(anchor.Definition, InFront(anchor), anchor.WorldPosition, true), anchor.name);
                // The middle of the pool, where a dive actually happens.
                Assert.AreEqual(PlayerActionResult.InvalidTarget,
                    ServiceInteractionRules.Validate(anchor.Definition, new Vector3(0f, 4f, 0f), anchor.WorldPosition, true),
                    anchor.name + " must not be reachable from the dive area");
            }
        }
    }
}
