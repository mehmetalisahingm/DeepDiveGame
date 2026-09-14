using System.Linq;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepDive.World.Tests
{
    public sealed class DiveTestAreaEventSceneTests
    {
        private Scene scene;
        private bool opened;
        private SpecialEventRunner runner;
        [SetUp] public void Setup()
        {
            const string path = "Assets/DeepDive/World/Scenes/DiveTestArea.unity";
            scene = SceneManager.GetSceneByPath(path);
            opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var events = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SpecialEventRunner>(true)).ToArray();
            Assert.AreEqual(1, events.Length);
            runner = events[0];
            Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup() { if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
        [Test] public void EventHasTheAgreedIdentityScheduleAndStableNetworkObject()
        {
            Assert.AreEqual("event_bioluminescence", runner.Definition.EventId);
            Assert.AreEqual(45, runner.Definition.TriggerDelaySeconds);
            Assert.AreEqual(20, runner.Definition.WindowSeconds);
            Assert.AreEqual(new Vector3(-9, 3.5f, -2), runner.transform.position);
            Assert.AreEqual(runner.Definition.EventId, runner.GetComponent<RecordingSubject>().ExpectedSubjectId);
            Assert.IsNull(runner.GetComponent<FishActor>());
            var hash = new SerializedObject(runner.GetComponent<NetworkObject>()).FindProperty("GlobalObjectIdHash").longValue;
            Assert.AreNotEqual(0, hash);
            var otherHashes = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<NetworkObject>(true))
                .Select(x => new SerializedObject(x).FindProperty("GlobalObjectIdHash").longValue).ToArray();
            Assert.AreEqual(otherHashes.Length, otherHashes.Distinct().Count());
        }
        [Test] public void PlaceholderIsVisibleWhenSignalledAndDoesNotInventAudio()
        {
            var presenter = new SerializedObject(runner.GetComponent<SpecialEventPresenter>());
            var particles = presenter.FindProperty("glowParticles").objectReferenceValue as ParticleSystem;
            var light = presenter.FindProperty("glowLight").objectReferenceValue as Light;
            Assert.IsNotNull(particles); Assert.IsNotNull(light);
            Assert.IsNotNull(particles.GetComponent<ParticleSystemRenderer>().sharedMaterial);
            Assert.AreNotEqual("Hidden/InternalErrorShader", particles.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name);
            Assert.IsFalse(particles.main.playOnAwake);
            Assert.IsFalse(light.enabled);
            Assert.IsNull(presenter.FindProperty("startAudio").objectReferenceValue);
        }
        [Test] public void ColliderIsRaycastableButExcludesDiverContacts()
        {
            var hull = runner.GetComponent<SphereCollider>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab");
            Assert.IsFalse(hull.isTrigger);
            Assert.AreEqual(runner.Definition.SubjectRadiusMetres, hull.radius);
            Assert.AreNotEqual(0, hull.excludeLayers.value & (1 << prefab.layer));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EverySpawnCanAimAndEarnBronzeAfterFourSeconds(int slot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab");
            var camera = prefab.GetComponentInChildren<Camera>(true);
            var spawn = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PlayerSpawnPoint>(true)).Single(x => x.Slot == slot);
            var eye = spawn.transform.TransformPoint(prefab.transform.InverseTransformPoint(camera.transform.position));
            var target = runner.transform.position;
            var direction = (target - eye).normalized;
            Assert.IsTrue(Physics.Raycast(eye, direction, out var hit, 22, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(runner.GetComponent<SphereCollider>(), hit.collider, "A wall or another target blocks the camera ray");
            var sample = RecordingFraming.Evaluate(eye, direction, camera.fieldOfView, target,
                runner.Definition.SubjectRadiusMetres, false, runner.Definition.Quality.Framing);
            Assert.IsTrue(sample.IsValid, sample.Rejection.ToString());
            Assert.GreaterOrEqual((int)runner.Definition.Quality.Evaluate(sample.Score01, 4), 1);
        }
    }
}
