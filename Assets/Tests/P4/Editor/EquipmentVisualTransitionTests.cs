using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;

namespace DeepDive.P4.Tests
{
    public sealed class EquipmentVisualTransitionTests
    {
        private static EquipmentVisualTransition Harpoon()
        {var view=new EquipmentVisualTransition();view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,0);return view;}

        [Test]
        public void LateJoinStartsInAcceptedPoseWithoutReplayingAnOldSwitch()
        {
            var view=new EquipmentVisualTransition();view.Tick(HeldEquipmentMode.Camera,CameraTier.Professional,.016f);
            Assert.That(view.Displayed,Is.EqualTo(HeldEquipmentMode.Camera));
            Assert.That(view.Tier,Is.EqualTo(CameraTier.Professional));
            Assert.That(view.IsSwitching,Is.False);
        }
        [Test]
        public void OldEquipmentLowersBeforeTheNewOneAppearsThenRaises()
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.07f);
            Assert.That(view.Displayed,Is.EqualTo(HeldEquipmentMode.Harpoon));Assert.That(view.Lowering,Is.EqualTo(.5f).Within(.001f));
            view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.07f);
            Assert.That(view.Displayed,Is.EqualTo(HeldEquipmentMode.Camera));Assert.That(view.Lowering,Is.EqualTo(1).Within(.001f));
            view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.14f);
            Assert.That(view.Lowering,Is.Zero);Assert.That(view.IsSwitching,Is.False);
        }
        [Test]
        public void RapidReturnToCurrentEquipmentReversesFromTheExistingPose()
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.07f);
            view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,0);
            Assert.That(view.Lowering,Is.EqualTo(.5f).Within(.001f));
            view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,.035f);
            Assert.That(view.Lowering,Is.GreaterThan(0).And.LessThan(.5f));
            Assert.That(view.Displayed,Is.EqualTo(HeldEquipmentMode.Harpoon));
        }
        [Test]
        public void RetargetWhileRaisingDoesNotSnapBackToRest()
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.21f);
            var previous=view.Lowering;view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,0);
            Assert.That(view.Lowering,Is.EqualTo(previous));
            view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,.4f);
            Assert.That(view.Displayed,Is.EqualTo(HeldEquipmentMode.Harpoon));Assert.That(view.IsSwitching,Is.False);
        }
        [TestCase(HeldEquipmentMode.None)]
        [TestCase(HeldEquipmentMode.Camera)]
        [TestCase(HeldEquipmentMode.RepairPart)]
        [TestCase(HeldEquipmentMode.RepairHammer)]
        public void RecordingPassiveAndRepairPriorityClearTheTransientPose(HeldEquipmentMode mode)
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.07f);
            view.Tick(mode,CameraTier.Basic,0,immediate:true);
            Assert.That(view.Displayed,Is.EqualTo(mode));Assert.That(view.IsSwitching,Is.False);
        }
        [Test]
        public void RepairNeverQueuesBehindAnOrdinarySwitch()
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.07f);
            view.Tick(HeldEquipmentMode.RepairPart,CameraTier.Basic,0);
            Assert.That(view.IsSwitching,Is.False);
            view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Basic,0);
            Assert.That(view.IsSwitching,Is.False);
        }
        [Test]
        public void CameraTierChangesUseTheSameSwitchButUnrelatedTierChangesDoNot()
        {
            var view=Harpoon();view.Tick(HeldEquipmentMode.Harpoon,CameraTier.Professional,.1f);
            Assert.That(view.IsSwitching,Is.False);
            view.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.5f);
            view.Tick(HeldEquipmentMode.Camera,CameraTier.Professional,.07f);
            Assert.That(view.Tier,Is.EqualTo(CameraTier.Basic));Assert.That(view.IsSwitching,Is.True);
            view.Tick(HeldEquipmentMode.Camera,CameraTier.Professional,.3f);
            Assert.That(view.Tier,Is.EqualTo(CameraTier.Professional));
        }
        [Test]
        public void OneSlowFrameAndManyShortFramesReachTheSamePose()
        {
            var fast=Harpoon();var slow=Harpoon();
            for(var i=0;i<20;i++)fast.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.02f);
            slow.Tick(HeldEquipmentMode.Camera,CameraTier.Basic,.4f);
            Assert.That(fast.Displayed,Is.EqualTo(slow.Displayed));Assert.That(fast.Lowering,Is.EqualTo(slow.Lowering));
        }
    }
}
