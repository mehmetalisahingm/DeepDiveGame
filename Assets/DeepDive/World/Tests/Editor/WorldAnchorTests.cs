using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // #138: the anchor ids are the contract and TryFind is fail-closed - one active ordinal match
    // across every loaded scene, or nothing. Where the real anchors stand is the P4 layout test's job.
    public class WorldAnchorTests
    {
        private const string DiveTestAreaPath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<Scene> openedScenes = new List<Scene>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            foreach (var scene in openedScenes)
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            openedScenes.Clear();
        }

        private WorldAnchor NewAnchor(string id, Vector3 position = default)
        {
            var go = new GameObject("Anchor_" + id);
            spawned.Add(go);
            go.transform.position = position;
            var anchor = go.AddComponent<WorldAnchor>();
            anchor.Configure(id);
            return anchor;
        }

        [Test]
        public void WorldAnchorIdsAreUniqueNonBlankAndKebabCase()
        {
            var ids = WorldAnchorIds.All;
            Assert.AreEqual(17, ids.Count);
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "duplicate anchor id");
            var kebab = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");
            foreach (var id in ids)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(id));
                StringAssert.IsMatch(kebab.ToString(), id);
            }

            Assert.AreEqual(4, WorldAnchorIds.HomeBedCount);
            Assert.AreEqual(4, WorldAnchorIds.HomeSpawnCount);
            Assert.AreEqual(4, WorldAnchorIds.TownServiceSlotCount);
            Assert.AreEqual("home-bed-3", WorldAnchorIds.HomeBed(3));
            Assert.AreEqual("home-spawn-0", WorldAnchorIds.HomeSpawn(0));
            Assert.AreEqual("town-service-slot-2", WorldAnchorIds.TownServiceSlot(2));
            Assert.AreEqual("", WorldAnchorIds.HomeBed(4));
            Assert.AreEqual("", WorldAnchorIds.HomeSpawn(-1));
            Assert.AreEqual("", WorldAnchorIds.TownServiceSlot(4));
        }

        [Test]
        public void PlayerZoneByteValuesAreFrozen()
        {
            Assert.AreEqual(0, (byte)PlayerZone.Unknown);
            Assert.AreEqual(1, (byte)PlayerZone.Home);
            Assert.AreEqual(2, (byte)PlayerZone.Town);
            Assert.AreEqual(3, (byte)PlayerZone.Coast);
            Assert.AreEqual(4, (byte)PlayerZone.Dock);
            Assert.AreEqual(5, (byte)PlayerZone.Sea);
            Assert.AreEqual(6, System.Enum.GetValues(typeof(PlayerZone)).Length);
        }

        [Test]
        public void TryFindReturnsTheSingleAnchorWithThatId()
        {
            var anchor = NewAnchor("test-anchor-single", new Vector3(1f, 2f, 3f));
            NewAnchor("test-anchor-other");

            Assert.IsTrue(WorldAnchors.TryFind("test-anchor-single", out var found));
            Assert.AreSame(anchor, found);
            Assert.IsTrue(WorldAnchors.TryGetPose("test-anchor-single", out var pose));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), pose.position);
        }

        [Test]
        public void TryFindFailsClosedWhenTheIdIsDuplicated()
        {
            NewAnchor("test-anchor-dup");
            NewAnchor("test-anchor-dup");

            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-dup", out var found));
            Assert.IsNull(found);
            Assert.IsFalse(WorldAnchors.TryGetPose("test-anchor-dup", out _));
            Assert.IsFalse(WorldAnchorRules.TrySelectUnique(new[] { "a", "b", "a" }, "a", out var index));
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void TryFindFailsForMissingNullOrBlankId()
        {
            NewAnchor("test-anchor-present");
            NewAnchor("");

            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-absent", out _));
            Assert.IsFalse(WorldAnchors.TryFind(null, out _));
            Assert.IsFalse(WorldAnchors.TryFind("", out _));
            Assert.IsFalse(WorldAnchors.TryFind("   ", out _));
            Assert.IsFalse(WorldAnchorRules.TrySelectUnique(null, "a", out _));
        }

        [Test]
        public void TryFindIsOrdinalAndCaseSensitive()
        {
            NewAnchor("test-anchor-case");

            Assert.IsTrue(WorldAnchors.TryFind("test-anchor-case", out _));
            Assert.IsFalse(WorldAnchors.TryFind("Test-Anchor-Case", out _));
            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-case ", out _));
            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-cas", out _));
        }

        [Test]
        public void TryFindSearchesEveryLoadedSceneWithoutLookingAtSceneNames()
        {
            // Two loaded scenes: the active one and DiveTestArea opened additively, as the P4 tests do.
            var active = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(DiveTestAreaPath);
            var other = existing.IsValid() && existing.isLoaded
                ? existing
                : EditorSceneManager.OpenScene(DiveTestAreaPath, OpenSceneMode.Additive);
            if (!(existing.IsValid() && existing.isLoaded)) openedScenes.Add(other);
            Assert.AreNotEqual(active, other);

            var inActive = NewAnchor("test-anchor-scene-a");
            SceneManager.MoveGameObjectToScene(inActive.gameObject, active);
            var inOther = NewAnchor("test-anchor-scene-b");
            SceneManager.MoveGameObjectToScene(inOther.gameObject, other);

            Assert.IsTrue(WorldAnchors.TryFind("test-anchor-scene-a", out var a));
            Assert.AreSame(inActive, a);
            Assert.IsTrue(WorldAnchors.TryFind("test-anchor-scene-b", out var b));
            Assert.AreSame(inOther, b);

            // The same id in two scenes is still a duplicate: scenes are not namespaces.
            var twin = NewAnchor("test-anchor-scene-a");
            SceneManager.MoveGameObjectToScene(twin.gameObject, other);
            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-scene-a", out _));
        }

        [Test]
        public void TryFindIgnoresInactiveAnchors()
        {
            var inactive = NewAnchor("test-anchor-inactive");
            inactive.gameObject.SetActive(false);
            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-inactive", out _));

            var active = NewAnchor("test-anchor-inactive");
            Assert.IsTrue(WorldAnchors.TryFind("test-anchor-inactive", out var found),
                "an inactive twin must not turn a single active anchor into a duplicate");
            Assert.AreSame(active, found);

            var parent = new GameObject("InactiveParent");
            spawned.Add(parent);
            var child = NewAnchor("test-anchor-under-inactive");
            child.transform.SetParent(parent.transform);
            parent.SetActive(false);
            Assert.IsFalse(WorldAnchors.TryFind("test-anchor-under-inactive", out _));
        }
    }
}
