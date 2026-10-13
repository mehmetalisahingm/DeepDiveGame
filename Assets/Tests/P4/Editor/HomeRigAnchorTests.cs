using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeepDive.P4.Tests
{
    // #138 PR-2: the DiveTestArea home rig stands on the world anchors, fails closed without them, keeps PrepArea's
    // legacy rig untouched, and leaves a corridor a real diver can walk to every bed, the storage and the PC.
    // Expectations are written numbers local to HomeOrigin (#153); only the anchors themselves are read from the layout.
    public sealed class HomeRigAnchorTests
    {
        private const string PrepAreaPath = "Assets/DeepDive/World/Scenes/PrepArea.unity";
        private const float Tolerance = 0.001f;

        // Written diver values, locked against the prefab by ContinuousTownWalkTests.DiverControllerMatchesTheWrittenWalkLimits.
        private const float Radius = 0.35f;
        private const float Height = 1.8f;
        private const float CenterY = 0.9f;
        private const float SlopeLimit = 45f;
        private const float StepOffset = 0.3f;
        private const float Skin = 0.08f;
        // What a CharacterController really needs on each side: its radius plus the skin it keeps from colliders.
        private const float Clearance = Radius + Skin;
        private const float MinBedGap = 2f * Clearance;
        private const float MinSideCorridor = 1.75f;
        // HomeLeft/HomeRight inner faces, local to HomeOrigin (wall at +-6, 0.35 thick).
        private const float WallInnerX = 5.825f;

        private static readonly float[] BedXs = { -3.45f, -1.15f, 1.15f, 3.45f };
        private static readonly Vector3 BedScale = new Vector3(1.25f, 0.35f, 2.05f);
        private static readonly Vector3 StorageScale = new Vector3(1.35f, 1.6f, 0.75f);
        private static readonly Vector3 PcScale = new Vector3(1.4f, 1.2f, 0.7f);

        private static Vector3 Home(float x, float y, float z) => ContinuousTownLayout.HomeOrigin + new Vector3(x, y, z);
        private static Vector2 HomeXZ(float x, float z) =>
            new Vector2(ContinuousTownLayout.HomeOrigin.x + x, ContinuousTownLayout.HomeOrigin.z + z);

        // Where a diver stands to use the storage / PC: 0.675 / 0.7m in front of the face looking into the room.
        private static readonly Vector2 StorageStand = HomeXZ(-3.9f, -2.5f);
        private static readonly Vector2 PcStand = HomeXZ(3.9f, -2.5f);
        private const float SideCorridorX = 4.95f;
        private const float PcRange = 3.25f;

        private static IEnumerable<string> HomeRigAnchorIds()
        {
            for (var i = 0; i < WorldAnchorIds.HomeBedCount; i++) yield return WorldAnchorIds.HomeBed(i);
            yield return WorldAnchorIds.HomeStorage;
            yield return WorldAnchorIds.HomePc;
        }

        // ---- placement ----------------------------------------------------------------------------------

        [Test]
        public void RigIsBuiltInTheGivenAdditiveSceneOnTheAnchorPoses()
        {
            ContinuousTownScene.With(scene =>
            {
                var active = SceneManager.GetActiveScene();
                Assert.AreNotEqual(scene, active, "precondition: DiveTestArea is additive, not active");

                HomeSceneSetup.EnsureRig(scene);

                Assert.AreEqual(active, SceneManager.GetActiveScene(), "active scene changed");
                var root = RootIn(scene, HomeSceneSetup.RootName);
                Assert.IsNotNull(root, "rig not built in the given scene");
                Assert.IsTrue(HomeRigAnchors.TryResolve(scene, out var poses, out var missing), string.Join(",", missing));

                var anchors = root.GetComponentsInChildren<HomeInteractionAnchor>(true);
                for (var i = 0; i < DayIds.BedCount; i++)
                {
                    var bed = anchors.Single(a => a.Kind == HomeInteractionKind.Bed && a.BedId == DayIds.Beds[i]);
                    Assert.AreEqual("HomeBed_" + i, bed.name);
                    Assert.AreEqual(scene, bed.gameObject.scene, bed.name + " scene");
                    AssertOnPose(bed.transform, poses.Beds[i], bed.name);
                    AssertPose(bed.transform, Home(BedXs[i], 0.3f, 3.45f), 180f, bed.name);
                    AssertNear(BedScale, bed.transform.lossyScale, bed.name + " scale");
                }

                var storage = anchors.Single(a => a.Kind == HomeInteractionKind.Storage);
                Assert.AreEqual(scene, storage.gameObject.scene);
                AssertOnPose(storage.transform, poses.Storage, "storage");
                AssertPose(storage.transform, Home(-3.9f, 0.8f, -3.55f), 0f, "storage");
                AssertNear(StorageScale, storage.transform.lossyScale, "storage scale");

                var pcs = Object.FindObjectsByType<HomePcAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, pcs.Length, "exactly one PC across all scenes");
                Assert.AreEqual(scene, pcs[0].gameObject.scene, "PC scene");
                AssertOnPose(pcs[0].transform, poses.Pc, "pc");
                AssertPose(pcs[0].transform, Home(3.9f, 0.6f, -3.55f), 0f, "pc");
                AssertNear(PcScale, pcs[0].transform.lossyScale, "pc scale");
            });
        }

        [Test]
        public void EnsuringTwiceKeepsOneRigAndOnePc()
        {
            ContinuousTownScene.With(scene =>
            {
                HomeSceneSetup.EnsureRig(scene);
                HomeSceneSetup.EnsureRig(scene);
                // The parameterless overload (HomePlayerInteractionBinding's morning call) uses the active scene.
                SceneManager.SetActiveScene(scene);
                HomeSceneSetup.EnsureRig();

                Assert.AreEqual(1, scene.GetRootGameObjects().Count(r => r.name == HomeSceneSetup.RootName), "rig roots");
                var anchors = Object.FindObjectsByType<HomeInteractionAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(4, anchors.Count(a => a.Kind == HomeInteractionKind.Bed), "beds");
                Assert.AreEqual(1, anchors.Count(a => a.Kind == HomeInteractionKind.Storage), "storage");
                Assert.AreEqual(1, Object.FindObjectsByType<HomePcAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "PCs");
            });
        }

        // Simulates HomeSceneSetup's sceneLoaded running before ContinuousTownLayout's.
        [Test]
        public void RigBuildsTheTownItselfWhenItRunsBeforeTheLayout()
        {
            ContinuousTownScene.With(scene =>
            {
                Object.DestroyImmediate(RootIn(scene, ContinuousTownLayout.RootName));
                Assert.IsFalse(WorldAnchors.TryFind(WorldAnchorIds.HomePc, out _), "precondition: no anchors");

                HomeSceneSetup.EnsureRig(scene);

                var town = RootIn(scene, ContinuousTownLayout.RootName);
                Assert.IsNotNull(town, "EnsureRig did not build the town first");
                Assert.AreEqual(17, town.GetComponentsInChildren<WorldAnchor>(true).Length);
                Assert.IsNotNull(RootIn(scene, HomeSceneSetup.RootName), "rig missing");
                Assert.IsTrue(HomeRigAnchors.TryResolve(scene, out var poses, out _));
                var bed0 = Object.FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None)
                    .Single(a => a.BedId == DayIds.Bed0);
                AssertOnPose(bed0.transform, poses.Beds[0], "bed0");
            });
        }

        // ---- fail closed --------------------------------------------------------------------------------

        [TestCaseSource(nameof(HomeRigAnchorIds))]
        public void MissingHomeAnchorFailsClosed(string id)
        {
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldAnchors.TryFind(id, out var anchor), "precondition " + id);
                anchor.gameObject.SetActive(false);

                LogAssert.Expect(LogType.Error, new Regex(HomeSceneSetup.AnchorsMissingCode + ".*" + Regex.Escape(id)));
                HomeSceneSetup.EnsureRig(scene);

                AssertNothingBuilt(scene);
            });
        }

        [Test]
        public void DuplicatedHomeAnchorFailsClosed()
        {
            ContinuousTownScene.With(scene =>
            {
                Assert.IsTrue(WorldAnchors.TryFind(WorldAnchorIds.HomePc, out var anchor));
                Object.Instantiate(anchor.gameObject, anchor.transform.parent);

                LogAssert.Expect(LogType.Error, new Regex(HomeSceneSetup.AnchorsMissingCode + ".*" + WorldAnchorIds.HomePc));
                HomeSceneSetup.EnsureRig(scene);

                AssertNothingBuilt(scene);
            });
        }

        [Test]
        public void AnchorFromAnotherSceneCountsAsMissing()
        {
            ContinuousTownScene.With(scene =>
            {
                // Any other loaded scene will do; PrepArea is a saved scene, so no untitled-scene restrictions apply.
                var existing = SceneManager.GetSceneByPath(PrepAreaPath);
                var opened = !existing.IsValid() || !existing.isLoaded;
                var other = opened ? EditorSceneManager.OpenScene(PrepAreaPath, OpenSceneMode.Additive) : existing;
                try
                {
                    Assert.IsTrue(WorldAnchors.TryFind(WorldAnchorIds.HomeStorage, out var anchor));
                    anchor.transform.SetParent(null, true);
                    SceneManager.MoveGameObjectToScene(anchor.gameObject, other);
                    Assert.IsTrue(WorldAnchors.TryFind(WorldAnchorIds.HomeStorage, out _), "precondition: still findable");

                    LogAssert.Expect(LogType.Error, new Regex(HomeSceneSetup.AnchorsMissingCode + ".*" + WorldAnchorIds.HomeStorage));
                    HomeSceneSetup.EnsureRig(scene);

                    AssertNothingBuilt(scene);
                }
                finally
                {
                    var moved = other.GetRootGameObjects().Where(r => r.name == WorldAnchorIds.HomeStorage).ToArray();
                    foreach (var go in moved) Object.DestroyImmediate(go);
                    if (opened) EditorSceneManager.CloseScene(other, true);
                }
            });
        }

        // ---- PrepArea regression ------------------------------------------------------------------------

        [Test]
        public void PrepAreaKeepsItsLegacyRig()
        {
            var existing = SceneManager.GetSceneByPath(PrepAreaPath);
            var opened = !existing.IsValid() || !existing.isLoaded;
            var prep = opened ? EditorSceneManager.OpenScene(PrepAreaPath, OpenSceneMode.Additive) : existing;
            try
            {
                Assert.AreEqual("PrepArea", prep.name);
                HomeSceneSetup.EnsureRig(prep);
                HomePcAnchor.EnsureLegacyPc(prep);

                Assert.IsNull(RootIn(prep, ContinuousTownLayout.RootName), "PrepArea must not build the town");
                var root = RootIn(prep, HomeSceneSetup.RootName);
                Assert.IsNotNull(root, "legacy rig missing");
                AssertNear(Vector3.zero, root.transform.position, "legacy root");
                var anchors = root.GetComponentsInChildren<HomeInteractionAnchor>(true);
                var legacyXs = new[] { -3f, -1f, 1f, 3f };
                for (var i = 0; i < legacyXs.Length; i++)
                {
                    var bed = anchors.Single(a => a.BedId == DayIds.Beds[i]);
                    AssertPose(bed.transform, new Vector3(legacyXs[i], 0.3f, 3.45f), 180f, bed.name);
                    AssertNear(BedScale, bed.transform.lossyScale, bed.name + " scale");
                }
                var storage = anchors.Single(a => a.Kind == HomeInteractionKind.Storage);
                AssertPose(storage.transform, new Vector3(-3.9f, 0.8f, -3.55f), 0f, "storage");
                var pc = RootIn(prep, HomePcAnchor.PcName);
                Assert.IsNotNull(pc, "legacy PC missing");
                Assert.IsNotNull(pc.GetComponent<HomePcAnchor>());
                AssertPose(pc.transform, new Vector3(3.9f, 0.6f, -3.55f), 0f, "pc");
            }
            finally
            {
                foreach (var name in new[] { HomeSceneSetup.RootName, HomePcAnchor.PcName })
                foreach (var leftover in prep.GetRootGameObjects().Where(r => r.name == name).ToArray())
                    Object.DestroyImmediate(leftover);
                if (opened) EditorSceneManager.CloseScene(prep, true);
            }
        }

        // ---- corridors and walks ------------------------------------------------------------------------

        [Test]
        public void BedGapsAreWideEnoughForTheDiver()
        {
            var xs = ContinuousTownLayout.HomeRig.BedXs;
            var width = ContinuousTownLayout.HomeRig.BedSize.x;
            for (var i = 0; i + 1 < xs.Length; i++)
                Assert.GreaterOrEqual(xs[i + 1] - xs[i] - width, MinBedGap - 1e-4f, $"HomeRig gap {i}-{i + 1}");
            Assert.GreaterOrEqual(xs[0] - width / 2f + WallInnerX, MinSideCorridor - 1e-4f, "HomeRig west corridor");
            Assert.GreaterOrEqual(WallInnerX - (xs[xs.Length - 1] + width / 2f), MinSideCorridor - 1e-4f, "HomeRig east corridor");

            ContinuousTownScene.With(scene =>
            {
                var beds = RootIn(scene, HomeSceneSetup.RootName).GetComponentsInChildren<HomeInteractionAnchor>(true)
                    .Where(a => a.Kind == HomeInteractionKind.Bed)
                    .Select(a => a.GetComponent<Collider>().bounds).OrderBy(b => b.center.x).ToArray();
                Assert.AreEqual(4, beds.Length);
                for (var i = 0; i + 1 < beds.Length; i++)
                    Assert.GreaterOrEqual(beds[i + 1].min.x - beds[i].max.x, MinBedGap - 1e-3f, $"rig gap {i}-{i + 1}");
                var left = ContinuousTownScene.TownChild(scene, "HomeLeft").GetComponent<Collider>().bounds;
                var right = ContinuousTownScene.TownChild(scene, "HomeRight").GetComponent<Collider>().bounds;
                Assert.GreaterOrEqual(beds[0].min.x - left.max.x, MinSideCorridor - 1e-3f, "rig west corridor");
                Assert.GreaterOrEqual(right.min.x - beds[3].max.x, MinSideCorridor - 1e-3f, "rig east corridor");
            }, furnitureProxies: true);
        }

        // Red before the #138 corridor fix: the old 0.75m gap fits a bare 0.35 capsule but not a controller.
        [TestCase(1)]
        [TestCase(2)]
        public void MiddleCorridorReachesTheInnerBedsFromTheDoor(int bed)
        {
            ContinuousTownScene.With(scene =>
            {
                var exit = BedExit(bed);
                var route = new[] { Door(), HomeXZ(0f, 5f), new Vector2(HomeXZ(0f, 0f).x, exit.y), exit };
                AssertWalkable(scene, "door -> middle corridor -> bed " + bed, route);
                AssertBedInRange(bed);
            }, furnitureProxies: true);
        }

        [TestCase("bed0-west")]
        [TestCase("bed3-east")]
        [TestCase("storage")]
        [TestCase("pc")]
        [TestCase("spawn0")]
        [TestCase("spawn1")]
        [TestCase("spawn2")]
        [TestCase("spawn3")]
        public void EveryHomeTargetIsWalkableFromTheDoorAndBack(string target)
        {
            ContinuousTownScene.With(scene =>
            {
                var door = Door();
                Vector2[] route;
                switch (target)
                {
                    case "bed0-west":
                    case "bed3-east":
                    {
                        var bed = target == "bed0-west" ? 0 : 3;
                        var side = bed == 0 ? -1f : 1f;
                        var exit = BedExit(bed);
                        route = new[] { door, HomeXZ(side * SideCorridorX, 5.2f), new Vector2(HomeXZ(side * SideCorridorX, 0f).x, exit.y), exit };
                        AssertBedInRange(bed);
                        break;
                    }
                    case "storage":
                    case "pc":
                    {
                        var stand = target == "storage" ? StorageStand : PcStand;
                        var side = Mathf.Sign(stand.x);
                        route = new[] { door, HomeXZ(side * SideCorridorX, 5.2f), HomeXZ(side * SideCorridorX, -1.5f), stand };
                        var anchorId = target == "storage" ? WorldAnchorIds.HomeStorage : WorldAnchorIds.HomePc;
                        Assert.IsTrue(WorldAnchors.TryGetPose(anchorId, out var pose));
                        Assert.LessOrEqual(Vector3.Distance(new Vector3(stand.x, ContinuousTownLayout.HomeOrigin.y, stand.y), pose.position),
                            target == "storage" ? HomePlayerInteractionBinding.InteractionRange : PcRange, target + " out of range");
                        break;
                    }
                    default:
                    {
                        var index = int.Parse(target.Substring("spawn".Length));
                        Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeSpawn(index), out var spawn));
                        route = new[] { new Vector2(spawn.position.x, spawn.position.z), HomeXZ(0f, 1f), HomeXZ(0f, 5f), door };
                        break;
                    }
                }
                AssertWalkable(scene, target, route);
            }, furnitureProxies: true);
        }

        // ---- helpers ------------------------------------------------------------------------------------

        // Both criteria: the clearance capsule (radius + skin) at every 0.25m sample, and a real
        // CharacterController driven along the same route.
        private static void AssertWalkable(Scene scene, string label, Vector2[] route)
        {
            var walk = ContinuousTownWalkTests.WalkWithRadius(Clearance, route);
            Assert.IsEmpty(walk.Failures, ContinuousTownWalkTests.Report(label + " (clearance r=" + Clearance + ")", walk));
            Assert.IsFalse(walk.ReachedSea);

            Assert.IsTrue(WorldZoneField.TryFind(out var zones));
            var diver = new GameObject("Test_P46_HomeDiver");
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
                var failures = new List<string>();
                ContinuousTownWalkTests.Drive(cc, zones, route, failures, out var sea);
                Assert.IsEmpty(failures, label + " (controller)\n" + string.Join("\n", failures.Take(20)));
                Assert.IsFalse(sea);
                var end = route[route.Length - 1];
                Assert.AreEqual(end.x, diver.transform.position.x, 0.1f, label + " controller end x");
                Assert.AreEqual(end.y, diver.transform.position.z, 0.1f, label + " controller end z");
            }
            finally
            {
                Object.DestroyImmediate(diver);
            }
        }

        private static Vector2 Door()
        {
            Assert.IsTrue(WorldAnchors.TryGetPose(WorldAnchorIds.HomeDoor, out var door));
            return new Vector2(door.position.x, door.position.z);
        }

        private static HomeInteractionAnchor RealBed(int index) =>
            Object.FindObjectsByType<HomeInteractionAnchor>(FindObjectsSortMode.None)
                .Single(a => a.Kind == HomeInteractionKind.Bed && a.BedId == DayIds.Beds[index]);

        // The real ExitPose: local (0, 0.15, 1.45) is applied through the bed's scale, then yaw 180.
        private static Vector2 BedExit(int index)
        {
            var exit = RealBed(index).ExitPose.position;
            Assert.AreEqual(ContinuousTownLayout.HomeOrigin.z + 0.4775f, exit.z, Tolerance, "bed " + index + " exit z");
            return new Vector2(exit.x, exit.z);
        }

        private static void AssertBedInRange(int index)
        {
            var bed = RealBed(index);
            var exit = bed.ExitPose.position;
            var feet = new Vector3(exit.x, ContinuousTownLayout.HomeOrigin.y, exit.z);
            Assert.LessOrEqual(Vector3.Distance(feet, bed.WorldPosition), HomePlayerInteractionBinding.InteractionRange,
                "bed " + index + " exit out of interaction range");
        }

        private static void AssertNothingBuilt(Scene scene)
        {
            Assert.IsNull(RootIn(scene, HomeSceneSetup.RootName), "rig was built");
            Assert.AreEqual(0, Object.FindObjectsByType<HomeInteractionAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "bed/storage built");
            Assert.AreEqual(0, Object.FindObjectsByType<HomePcAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "PC built");
        }

        private static GameObject RootIn(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(r => r.name == name);

        private static void AssertOnPose(Transform actual, Pose expected, string label)
        {
            AssertNear(expected.position, actual.position, label + " vs anchor");
            Assert.AreEqual(0f, Quaternion.Angle(expected.rotation, actual.rotation), 0.01f, label + " rotation vs anchor");
        }

        private static void AssertPose(Transform actual, Vector3 position, float yaw, string label)
        {
            AssertNear(position, actual.position, label);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(0f, yaw, 0f), actual.rotation), 0.01f, label + " yaw");
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, label + " x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, label + " y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, label + " z");
        }
    }
}
