using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    // Guards the part of DiveTestArea that P3-B owns: the RecordingSubject on the fish and the
    // quality table behind it. Separate from DiveTestAreaSceneTests, which guards P2-B's half of
    // the same scene - the dive scene is shared ground and Unity YAML merges badly
    // (docs/plan/WORKFLOW.md: "Ayni ana sahne/prefab uzerinde ayni anda calisilmaz"), so each
    // phase keeps its own guard and a failure names the owner.
    //
    // Everything asserted here is produced by DiveTestAreaRecordingSetup.Apply; if this fails,
    // running that menu item again is the repair, not hand-editing the scene.
    public class DiveTestAreaRecordingSceneTests
    {
        private const string ScenePath = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        private const string QualityPath =
            "Assets/DeepDive/World/Recording/Quality/DefaultRecordingQuality.asset";

        private Scene scene;
        private Scene previousActive;
        private bool opened;

        [SetUp]
        public void OpenDiveScene()
        {
            previousActive = SceneManager.GetActiveScene();
            var already = SceneManager.GetSceneByPath(ScenePath);
            opened = !already.IsValid() || !already.isLoaded;
            scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : already;
        }

        [TearDown]
        public void CloseDiveScene()
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var found = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<T>(true));
            return found;
        }

        private RecordingSubject SingleSubject()
        {
            var subjects = FindAll<RecordingSubject>(scene);
            subjects.RemoveAll(subject => subject.GetComponent<FishActor>() == null);
            Assert.AreEqual(1, subjects.Count, "DiveTestArea must hold exactly one fish RecordingSubject");
            return subjects[0];
        }

        private static SerializedProperty Property(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.IsNotNull(property, $"serialized property '{propertyName}' is missing on {target.name}");
            return property;
        }

        private static RecordingQualityTable TableOn(RecordingSubject subject)
        {
            var table = Property(subject, "quality").objectReferenceValue as RecordingQualityTable;
            Assert.IsNotNull(table, "RecordingSubject.quality is not assigned; the fish cannot be filmed");
            return table;
        }

        // The diver's own camera decides the framing maths, so the band assertions below read
        // the real prefab FOV instead of assuming 60 degrees.
        private static float DiverCameraFov()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, $"network diver prefab is missing at {DiverPrefabPath}");
            var camera = prefab.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(camera, "NetworkDiver prefab has no view camera");
            return camera.fieldOfView;
        }

        [Test]
        public void TheFishIsTheOneFilmableSubject()
        {
            var subject = SingleSubject();
            Assert.IsTrue(subject.TryGetComponent<FishActor>(out var fish),
                "the recording subject must ride the fish, so aiming at the fish resolves it");
            Assert.IsNotNull(fish.Species, "FishActor.species is not assigned");
        }

        // P3-A's RecordingNetworkBridge resolves a target by walking the aimed-at NetworkObject
        // with GetComponentsInChildren<MonoBehaviour>() and keeping the first IRecordingTarget.
        // This is that exact lookup: if it stops finding the subject, recording silently dies.
        [Test]
        public void TheBridgeLookupFindsTheSubjectOnTheFishNetworkObject()
        {
            var subject = SingleSubject();
            Assert.IsTrue(subject.TryGetComponent<NetworkObject>(out var networkObject),
                "the subject must share the fish's NetworkObject; no target id travels");

            IRecordingTarget found = null;
            foreach (var behaviour in networkObject.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IRecordingTarget target) { found = target; break; }

            Assert.IsNotNull(found, "the bridge's MonoBehaviour scan finds no IRecordingTarget");
            Assert.AreSame(subject, found, "the bridge would resolve something other than the subject");
        }

        // Left empty in the scene on purpose: the id then falls back to the species asset, so
        // there is one source of truth for what this subject is filmed as and for what Mert is
        // asked to price. A hand-typed id here would drift from SeaBass.asset unnoticed.
        [Test]
        public void TheSubjectIdFallsBackToTheFishSpecies()
        {
            var subject = SingleSubject();
            Assert.IsEmpty(Property(subject, "subjectId").stringValue,
                "subjectId must stay empty so the species asset stays authoritative");
            Assert.AreEqual("sea_bass", subject.ExpectedSubjectId);
        }

        [Test]
        public void TheQualityTableIsTheSharedAssetAndItIsConsistent()
        {
            var subject = SingleSubject();
            var table = TableOn(subject);
            Assert.AreEqual(QualityPath, AssetDatabase.GetAssetPath(table),
                "the subject must use the shared table, not a private copy");
            Assert.IsTrue(table.IsValid(out var error), error);
        }

        // The ladder shape is Mert's pricing input (docs/plan/CONTRACTS.md: "Utku kaliteyi, Mert
        // krediyi belirler"), so a quiet retune on this side would move his prices under him.
        // Locked like the three-hit kill is: change it deliberately, in both places.
        [Test]
        public void TheQualityLadderStaysOnTheAgreedFourTiers()
        {
            var table = TableOn(SingleSubject());
            Assert.AreEqual(4, table.TierCount);

            var expected = new[]
            {
                ("Bronze", 0.25f, 2f),
                ("Silver", 0.45f, 4f),
                ("Gold", 0.60f, 6f),
                ("Platinum", 0.80f, 8f)
            };
            for (var i = 0; i < expected.Length; i++)
            {
                var tier = table.Tiers[i];
                Assert.AreEqual(expected[i].Item1, tier.Name, $"tier {i} name drifted");
                Assert.AreEqual(expected[i].Item2, tier.MinScore01, 0.0001f, $"tier {i} score drifted");
                Assert.AreEqual(expected[i].Item3, tier.MinValidSeconds, 0.0001f, $"tier {i} duration drifted");
            }
        }

        // RecordingFraming.FrameFill measures the subject's radius against the frame half
        // height, so for a fish filmed side-on the useful figure is its half length. Derived
        // from the collider the harpoon already uses, and re-derived here: re-shaping the fish
        // in DiveTestAreaSetup must not leave the camera judging the old size.
        [Test]
        public void TheSubjectRadiusStillMatchesTheFishHull()
        {
            var subject = SingleSubject();
            Assert.IsTrue(subject.TryGetComponent<CapsuleCollider>(out var hull),
                "the fish hull is missing, so there is nothing to size the shot against");
            var expected = Mathf.Max(hull.height * 0.5f, hull.radius);
            Assert.AreEqual(expected, Property(subject, "subjectRadiusMetres").floatValue, 0.001f,
                "subjectRadiusMetres drifted from the fish hull; re-run the P3 setup");
            Assert.AreEqual(Vector3.one, subject.transform.localScale,
                "a scaled fish would make the collider-derived radius wrong");
        }

        // Everything solid in the dive sits on Default today. The mask is asserted against the
        // layer the arena walls actually use, so moving the geometry to a new layer fails loudly
        // instead of making every shot count through walls. Also pinned as not-everything, so
        // nobody widens it back to ~0 and re-includes whatever layer is added next.
        [Test]
        public void TheOccluderMaskCoversTheArenaGeometryAndNothingMore()
        {
            var subject = SingleSubject();
            var mask = Property(subject, "occluderLayers").intValue;

            GameObject wall = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Wall_N") { wall = root; break; }
            Assert.IsNotNull(wall, "Wall_N is missing from DiveTestArea");

            Assert.AreNotEqual(0, mask & (1 << wall.layer),
                "the arena walls are not in the occluder mask, so shots would count through them");
            Assert.AreNotEqual(~0, mask, "the mask must name its layers, not accept everything");
        }

        // The band check that justifies the radius and the ladder. A dead-centre shot from
        // inside the arena has to be worth something, and a shot from beyond the band has to be
        // refused - with the real table, the real radius and the real camera FOV. This is the
        // test that catches a subject radius so small that nothing is ever filmable.
        [Test]
        public void ACentredShotFromTheMiddleOfTheArenaEarnsAtLeastBronze()
        {
            var subject = SingleSubject();
            var table = TableOn(subject);
            var radius = Property(subject, "subjectRadiusMetres").floatValue;
            var fov = DiverCameraFov();

            // 5 m: the distance the fish starts fleeing from (SeaBass.fleeRadius), so this is
            // about as close as a steady shot gets.
            var eye = subject.transform.position - Vector3.forward * 5f;
            var sample = RecordingFraming.Evaluate(eye, Vector3.forward, fov,
                subject.transform.position, radius, false, table.Framing);

            Assert.IsTrue(sample.IsValid, $"a centred 5 m shot was refused: {sample.Rejection}");
            Assert.Greater(table.Evaluate(sample.Score01, 4f), RecordingQuality.NoPayout,
                $"a centred 5 m shot held for 4 s earns nothing (score {sample.Score01:0.000})");
        }

        [Test]
        public void AShotFromBeyondTheBandIsRefused()
        {
            var subject = SingleSubject();
            var table = TableOn(subject);
            var radius = Property(subject, "subjectRadiusMetres").floatValue;
            var fov = DiverCameraFov();

            var eye = subject.transform.position - Vector3.forward * 13f;
            var sample = RecordingFraming.Evaluate(eye, Vector3.forward, fov,
                subject.transform.position, radius, false, table.Framing);

            Assert.IsFalse(sample.IsValid, "a 13 m shot counted; the band no longer closes");
        }

        // docs/plan/PHASES.md P3 technical finish: "Gorus disindaki veya engel arkasindaki
        // hedeften gecerli cekim gelmiyor". The occlusion flag is resolved by the subject from
        // the scene, but the rule it feeds is asserted here against the scene's own table.
        [Test]
        public void AnOccludedShotNeverCountsHoweverGoodTheFraming()
        {
            var subject = SingleSubject();
            var table = TableOn(subject);
            var radius = Property(subject, "subjectRadiusMetres").floatValue;

            var eye = subject.transform.position - Vector3.forward * 3f;
            var sample = RecordingFraming.Evaluate(eye, Vector3.forward, DiverCameraFov(),
                subject.transform.position, radius, true, table.Framing);

            Assert.IsFalse(sample.IsValid);
            Assert.AreEqual(RecordingSampleRejection.Occluded, sample.Rejection);
            Assert.AreEqual(0f, sample.Score01);
        }

        // A diver should be able to point the camera and get a usable shot without first
        // swimming across the arena, otherwise the recording half of the dive is untestable in
        // the time a tube lasts. Asserted from a real spawn, through the real eye offset and the
        // real framing rules - so moving the fish or the spawns out of the band fails here.
        [Test]
        public void TheSubjectIsAlreadyFilmableFromADiveSpawn()
        {
            var subject = SingleSubject();
            var table = TableOn(subject);
            var radius = Property(subject, "subjectRadiusMetres").floatValue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, $"network diver prefab is missing at {DiverPrefabPath}");
            var camera = prefab.GetComponentInChildren<Camera>(true);
            Assert.IsNotNull(camera, "NetworkDiver prefab has no view camera");
            var eyeOffset = prefab.transform.InverseTransformPoint(camera.transform.position);

            var spawns = FindAll<PlayerSpawnPoint>(scene);
            Assert.AreEqual(4, spawns.Count, "DiveTestArea must expose four player spawn points");

            var target = subject.transform.position;
            var filmable = 0;
            var reasons = new List<string>();
            foreach (var spawn in spawns)
            {
                var eye = spawn.transform.TransformPoint(eyeOffset);
                // Aimed straight at the fish: this asks whether the distance and size work out,
                // not whether the player can aim.
                var sample = RecordingFraming.Evaluate(eye, (target - eye).normalized,
                    camera.fieldOfView, target, radius, false, table.Framing);
                if (sample.IsValid) filmable++;
                else reasons.Add($"slot {spawn.Slot}: {sample.Rejection} at {sample.DistanceMetres:0.0}m");
            }

            Assert.Greater(filmable, 0,
                "the subject is outside the framing band from every dive spawn - " +
                string.Join("; ", reasons));
        }
    }
}
