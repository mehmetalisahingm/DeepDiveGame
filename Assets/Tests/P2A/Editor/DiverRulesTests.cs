using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P2A.Tests
{
    public class DiverRulesTests
    {
        [Test]
        public void OxygenDrainsOnlyWhileConsumingAndClampsAtZero()
        {
            var vitals = new DiverVitalsState(10f, 100f, 2f, 0.2f);
            vitals.Tick(2f, false);
            Assert.AreEqual(10f, vitals.Oxygen);
            vitals.Tick(2f, true);
            Assert.AreEqual(6f, vitals.Oxygen);
            vitals.Tick(10f, true);
            Assert.AreEqual(0f, vitals.Oxygen);
            Assert.IsTrue(vitals.Passive);
        }

        [Test]
        public void LowOxygenThresholdDoesNotMarkPlayerPassive()
        {
            var vitals = new DiverVitalsState(100f, 100f, 10f, 0.2f);
            vitals.Tick(8f, true);
            Assert.AreEqual(20f, vitals.Oxygen);
            Assert.IsTrue(vitals.LowOxygen);
            Assert.IsFalse(vitals.Passive);
        }

        [Test]
        public void DamageIsHostRuleFriendlyAndResetStartsFreshDive()
        {
            var vitals = new DiverVitalsState(30f, 50f, 1f, 0.2f);
            Assert.IsTrue(vitals.ApplyDamage(20f));
            Assert.AreEqual(30f, vitals.Health);
            Assert.IsTrue(vitals.ApplyDamage(100f));
            Assert.IsTrue(vitals.Passive);
            vitals.Reset();
            Assert.AreEqual(30f, vitals.Oxygen);
            Assert.AreEqual(50f, vitals.Health);
            Assert.IsFalse(vitals.Passive);
        }

        [Test]
        public void ActionGateRejectsReplayAndRateSpam()
        {
            var gate = new ServerActionGate();
            Assert.AreEqual(PlayerActionResult.Accepted, gate.TryAccept(1, PlayerActionKind.Harpoon, 10d, 0.5d));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, gate.TryAccept(1, PlayerActionKind.Harpoon, 10.1d, 0.5d));
            Assert.AreEqual(PlayerActionResult.TooFast, gate.TryAccept(2, PlayerActionKind.Harpoon, 10.2d, 0.5d));
            Assert.AreEqual(PlayerActionResult.Accepted, gate.TryAccept(3, PlayerActionKind.Harpoon, 10.6d, 0.5d));
        }

        [Test]
        public void HarpoonAndPickupHaveIndependentCooldownsButSharedRequestOrder()
        {
            var gate = new ServerActionGate();
            Assert.AreEqual(PlayerActionResult.Accepted, gate.TryAccept(1, PlayerActionKind.Harpoon, 2d, 1d));
            Assert.AreEqual(PlayerActionResult.Accepted, gate.TryAccept(2, PlayerActionKind.Pickup, 2.1d, 1d));
            Assert.AreEqual(PlayerActionResult.DuplicateRequest, gate.TryAccept(1, PlayerActionKind.Pickup, 4d, 0d));
        }

        [Test]
        public void InputBufferRejectsInvalidPitchAndClampsValidPitch()
        {
            var buffer = new ServerInputBuffer();
            Assert.IsFalse(buffer.Accept(new PlayerInputFrame { Sequence = 1, Pitch = float.NaN }, 1d));
            Assert.IsTrue(buffer.Accept(new PlayerInputFrame { Sequence = 1, Pitch = 120f }, 1d));
            Assert.AreEqual(85f, buffer.Read(1d).Pitch);
            Assert.AreEqual(Vector3.zero, buffer.Read(2d).Move);
        }

        [Test]
        public void AcceptedHarpoonResolvesToHitFeedback()
        {
            var feedback = ActionFeedbackRules.Resolve(PlayerActionKind.Harpoon, PlayerActionResult.Accepted);
            Assert.AreEqual(PlayerFeedbackCue.HarpoonHit, feedback.Cue);
            Assert.AreEqual("HIT", feedback.Message);
        }

        [Test]
        public void AcceptedPickupResolvesToCatchSecuredFeedback()
        {
            var feedback = ActionFeedbackRules.Resolve(PlayerActionKind.Pickup, PlayerActionResult.Accepted);
            Assert.AreEqual(PlayerFeedbackCue.PickupAccepted, feedback.Cue);
            Assert.AreEqual("CATCH SECURED", feedback.Message);
        }

        [Test]
        public void InventoryFullFeedbackIsSharedAcrossPickupFailures()
        {
            var feedback = ActionFeedbackRules.Resolve(PlayerActionKind.Pickup, PlayerActionResult.InventoryFull);
            Assert.AreEqual(PlayerFeedbackCue.BagFull, feedback.Cue);
            Assert.AreEqual("BAG FULL", feedback.Message);
        }
    }
}
