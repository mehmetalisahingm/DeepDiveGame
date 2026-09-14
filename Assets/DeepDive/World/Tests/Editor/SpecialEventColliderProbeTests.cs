using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.World.Tests
{
    // Step 6 gate: exercise actual PhysX queries and CharacterController.Move, including
    // a solid-collider control. No layer matrix or scene asset is changed by this probe.
    public sealed class SpecialEventColliderProbeTests
    {
        private SphereCollider sphere;
        private GameObject diverPrefab;
        private GameObject diver;

        [SetUp] public void Setup()
        {
            diverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab");
            Assert.IsNotNull(diverPrefab);
            var target = new GameObject("EventTarget");
            // Far from any loaded level geometry; EditMode scenes share the default physics scene.
            target.transform.position = new Vector3(0, 1000, 0);
            sphere = target.AddComponent<SphereCollider>();
            sphere.radius = 1.5f;
            sphere.isTrigger = false;
        }

        [TearDown] public void Cleanup()
        {
            if (diver != null) Object.DestroyImmediate(diver);
            if (sphere != null) Object.DestroyImmediate(sphere.gameObject);
        }

        [Test] public void ExcludedContactLayerStillAllowsNonTriggerRaycast()
        {
            sphere.excludeLayers = 1 << diverPrefab.layer;
            Physics.SyncTransforms();
            var origin = sphere.transform.position + Vector3.back * 4;
            Assert.IsTrue(Physics.Raycast(origin, Vector3.forward, out var hit,
                8, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreSame(sphere, hit.collider);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ControllerPassesOnlyWhenItsLayerIsExcluded(bool exclude)
        {
            sphere.excludeLayers = exclude ? 1 << diverPrefab.layer : 0;
            var template = diverPrefab.GetComponent<CharacterController>();
            Assert.IsNotNull(template);
            diver = new GameObject("ProbeDiver") { layer = diverPrefab.layer };
            diver.transform.position = sphere.transform.position - template.center + Vector3.back * 4;
            var controller = diver.AddComponent<CharacterController>();
            controller.height = template.height;
            controller.radius = template.radius;
            controller.center = template.center;
            controller.skinWidth = template.skinWidth;
            controller.stepOffset = template.stepOffset;
            controller.slopeLimit = template.slopeLimit;
            controller.minMoveDistance = template.minMoveDistance;
            controller.includeLayers = template.includeLayers;
            controller.excludeLayers = template.excludeLayers;
            Physics.SyncTransforms();
            for (var i = 0; i < 80; i++) controller.Move(Vector3.forward * 0.1f);
            if (exclude) Assert.Greater(diver.transform.position.z, 3.5f, "Excluded event collider blocked the diver");
            else Assert.Less(diver.transform.position.z, -1.5f, "Control must prove the solid sphere blocks movement");
        }
    }
}
