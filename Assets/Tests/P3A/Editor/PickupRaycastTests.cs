using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P3A.Tests
{
    public sealed class PickupRaycastTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        [Test]
        public void NonInteractionTriggerDoesNotBlockBoatPartPickup()
        {
            var volume = CreateBox("SwimVolumeLike", new Vector3(0f, 0f, 1f), isTrigger: true);
            var target = CreateBox("BoatPart", new Vector3(0f, 0f, 2f), isTrigger: true);
            target.AddComponent<BoatPartPickupTargetStub>();
            Physics.SyncTransforms();

            Assert.That(NetworkPlayer.TryResolvePickupHit(Vector3.zero, Vector3.forward, 5f, out var hit), Is.True);
            Assert.That(hit.collider, Is.EqualTo(target.GetComponent<BoxCollider>()));
            Assert.That(hit.collider, Is.Not.EqualTo(volume.GetComponent<BoxCollider>()));
        }

        [Test]
        public void SolidGeometryStillBlocksPickupBehindIt()
        {
            var wall = CreateBox("Wall", new Vector3(0f, 0f, 1f), isTrigger: false);
            var target = CreateBox("BoatPart", new Vector3(0f, 0f, 2f), isTrigger: true);
            target.AddComponent<BoatPartPickupTargetStub>();
            Physics.SyncTransforms();

            Assert.That(NetworkPlayer.TryResolvePickupHit(Vector3.zero, Vector3.forward, 5f, out var hit), Is.True);
            Assert.That(hit.collider, Is.EqualTo(wall.GetComponent<BoxCollider>()));
        }

        [Test]
        public void IrrelevantTriggersAloneDoNotProducePickupHit()
        {
            CreateBox("SwimVolumeLike", new Vector3(0f, 0f, 1f), isTrigger: true);
            Physics.SyncTransforms();

            Assert.That(NetworkPlayer.TryResolvePickupHit(Vector3.zero, Vector3.forward, 5f, out _), Is.False);
        }

        private GameObject CreateBox(string name, Vector3 position, bool isTrigger)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one * 0.5f;
            box.isTrigger = isTrigger;
            return go;
        }
    }

    public sealed class BoatPartPickupTargetStub : MonoBehaviour, IBoatPartPickupTarget
    {
        public string PartId => BoatRepairParts.Engine;
    }
}
