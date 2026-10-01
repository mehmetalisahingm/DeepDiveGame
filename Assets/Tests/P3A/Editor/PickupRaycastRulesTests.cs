using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class BoatPartPickupTargetStub : MonoBehaviour, IBoatPartPickupTarget
    {
        public string PartId => "boat-part-engine";
    }

    public sealed class CatchPickupTargetStub : MonoBehaviour, ICatchPickupTarget
    {
        public PlayerActionResult TryPickup(PlayerId playerId, ulong requestId) => PlayerActionResult.Accepted;
    }

    public sealed class PickupRaycastRulesTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        [Test]
        public void AmbientTriggerBeforeBoatPartDoesNotOccludePickup()
        {
            CreateCollider("SwimVolume", 0.75f, isTrigger: true);
            var target = CreateCollider("BoatPart_Engine", 1.75f, isTrigger: true);
            target.gameObject.AddComponent<BoatPartPickupTargetStub>();
            Physics.SyncTransforms();

            var hits = Physics.RaycastAll(Vector3.zero, Vector3.forward, 3f, ~0, QueryTriggerInteraction.Collide);
            var selected = PickupRaycastRules.SelectFirstPickupOrBlocker(hits);

            Assert.AreSame(target, selected);
            Assert.IsTrue(PickupRaycastRules.IsPickupTarget(selected));
        }

        [Test]
        public void SolidGeometryBeforeBoatPartStillBlocksPickup()
        {
            var wall = CreateCollider("Wall", 0.75f, isTrigger: false);
            var target = CreateCollider("BoatPart_Engine", 1.75f, isTrigger: true);
            target.gameObject.AddComponent<BoatPartPickupTargetStub>();
            Physics.SyncTransforms();

            var hits = Physics.RaycastAll(Vector3.zero, Vector3.forward, 3f, ~0, QueryTriggerInteraction.Collide);
            var selected = PickupRaycastRules.SelectFirstPickupOrBlocker(hits);

            Assert.AreSame(wall, selected);
            Assert.IsFalse(PickupRaycastRules.IsPickupTarget(selected));
        }

        [Test]
        public void CatchTriggerRemainsAValidPickupTarget()
        {
            CreateCollider("AmbientTrigger", 0.75f, isTrigger: true);
            var target = CreateCollider("Catch", 1.75f, isTrigger: true);
            target.gameObject.AddComponent<CatchPickupTargetStub>();
            Physics.SyncTransforms();

            var hits = Physics.RaycastAll(Vector3.zero, Vector3.forward, 3f, ~0, QueryTriggerInteraction.Collide);
            var selected = PickupRaycastRules.SelectFirstPickupOrBlocker(hits);

            Assert.AreSame(target, selected);
            Assert.IsTrue(PickupRaycastRules.IsPickupTarget(selected));
        }

        private BoxCollider CreateCollider(string name, float z, bool isTrigger)
        {
            var go = new GameObject(name);
            created.Add(go);
            go.transform.position = new Vector3(0f, 0f, z);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.5f, 0.5f, 0.5f);
            collider.isTrigger = isTrigger;
            return collider;
        }
    }
}
