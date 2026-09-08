using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace DeepDive.P1.Tests
{
    public class NetworkRulesTests
    {
        [Test] public void FourConcurrentReservationsRejectFifthAndReuseFreedSlot()
        {
            var policy = new AdmissionPolicy();
            for (ulong id = 0; id < 4; id++)
            { Assert.IsTrue(policy.TryReserve(id, true, true, out var slot, out _)); Assert.AreEqual((int)id, slot); }
            Assert.IsFalse(policy.TryReserve(4, true, true, out _, out var reason));
            Assert.AreEqual("RoomFull", reason);
            policy.Release(2);
            Assert.IsTrue(policy.TryReserve(5, true, true, out var reused, out _));
            Assert.AreEqual(2, reused); Assert.AreEqual(4, policy.Count);
        }
        [Test] public void ClosedDiveAndInvalidProtocolDoNotConsumeCapacity()
        {
            var policy = new AdmissionPolicy();
            Assert.IsFalse(policy.TryReserve(1, false, true, out _, out var phase));
            Assert.AreEqual("WrongPhase", phase);
            Assert.IsFalse(policy.TryReserve(2, true, false, out _, out var protocol));
            Assert.AreEqual("ProtocolMismatch", protocol); Assert.AreEqual(0, policy.Count);
        }
        [Test] public void SameConnectionDoesNotReserveTwiceAndNewSessionClearsIds()
        {
            var policy = new AdmissionPolicy();
            policy.TryReserve(0, true, true, out _, out _);
            policy.TryReserve(0, true, true, out _, out _);
            Assert.AreEqual(1, policy.Count);
            policy.Clear(); Assert.AreEqual(0, policy.Count); Assert.AreEqual(-1, policy.SlotOf(0));
            Assert.AreEqual(new PlayerId(0), new PlayerId(0));
        }
        [Test] public void InvalidInputCannotPoisonPositionOrConsumeSequence()
        {
            var buffer = new ServerInputBuffer();
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = 1, Move = new Vector3(float.NaN, 0, 0) }, 1));
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = 1, Yaw = float.PositiveInfinity }, 1));
            Assert.IsTrue(buffer.Accept(new PlayerInputFrame { Sequence = 1, Move = Vector3.one * 50, Yaw = 721 }, 1));
            Assert.That(buffer.Read(1).Move.magnitude, Is.EqualTo(1).Within(0.001));
            Assert.That(buffer.Read(1).Yaw, Is.EqualTo(1).Within(0.001));
        }
        [Test] public void ReorderedDuplicateAndStaleInputDoNotExtendMovement()
        {
            var buffer = new ServerInputBuffer();
            Assert.IsTrue(buffer.Accept(new PlayerInputFrame { Sequence = 5, Move = Vector3.forward }, 1));
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = 4, Move = Vector3.back }, 1.2));
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = 5, Move = Vector3.back }, 1.2));
            Assert.AreEqual(Vector3.forward, buffer.Read(1.2).Move);
            Assert.AreEqual(Vector3.zero, buffer.Read(1.26).Move);
        }
        [Test] public void SequenceWrapAndTeleportInputClearWork()
        {
            var buffer = new ServerInputBuffer();
            Assert.IsTrue(buffer.Accept(new PlayerInputFrame { Sequence = uint.MaxValue }, 0));
            Assert.IsTrue(buffer.Accept(new PlayerInputFrame { Sequence = 0, Move = Vector3.up }, 0));
            buffer.ClearMotion(); Assert.AreEqual(Vector3.zero, buffer.Read(0).Move);
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = uint.MaxValue }, 0));
        }
        [UnityTest] public IEnumerator RotatedWaterVolumeUsesLocalShapeAndUnregisters()
        {
            yield return new EnterPlayMode();
            var water = new GameObject("test-water");
            try
            {
                var box = water.AddComponent<BoxCollider>(); box.size = new Vector3(2, 2, 10);
                water.AddComponent<SwimVolume>();
                water.transform.SetPositionAndRotation(new Vector3(10, 0, 10), Quaternion.Euler(0, 45, 0));
                Assert.IsTrue(SwimVolume.Contains(water.transform.TransformPoint(new Vector3(0, 0, 4))));
                Assert.IsFalse(SwimVolume.Contains(water.transform.TransformPoint(new Vector3(2, 0, 0))));
                water.SetActive(false); Assert.IsFalse(SwimVolume.Contains(water.transform.position));
            }
            finally { Object.DestroyImmediate(water); }
            yield return new ExitPlayMode();
        }
    }
}
