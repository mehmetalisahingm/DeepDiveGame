using System.Linq;
using DeepDive.Network;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #189 P4.8-C: lift / seat / drop sounds follow host-published repair state only, and the three clips are real, distinct and bound.
    public sealed class RepairCarrySoundTests
    {
        [TestCase(false, false, true, false, false, RepairCue.Lift, TestName = "hands become busy -> lift")]
        [TestCase(true, false, true, true, false, RepairCue.Seat, TestName = "board goes onto its slot -> seat")]
        [TestCase(true, false, false, false, false, RepairCue.Drop, TestName = "carried board released -> drop")]
        [TestCase(true, true, false, false, false, RepairCue.Drop, TestName = "placed board released (Q) -> drop")]
        [TestCase(true, true, false, false, true, RepairCue.None, TestName = "completing stroke frees the hands: impact, no drop")]
        [TestCase(true, false, true, false, false, RepairCue.None, TestName = "still carrying: silence")]
        [TestCase(true, true, true, true, false, RepairCue.None, TestName = "still hammering: silence (impact is stroke-driven)")]
        [TestCase(false, false, false, false, false, RepairCue.None, TestName = "idle: silence")]
        [TestCase(false, false, false, false, true, RepairCue.None, TestName = "stroke while idle never makes a drop")]
        public void CuesFollowTheHostPublishedStateEdges(bool wasBusy, bool wasPlaced, bool busy, bool placed, bool struck, RepairCue expected) =>
            Assert.AreEqual(expected, RepairCarryView.CueFor(wasBusy, wasPlaced, busy, placed, struck));

        private static AudioClip[] Clips()
        {
            var catalog = Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            return new[] { catalog.RepairLiftClip, catalog.RepairSeatClip, catalog.RepairDropClip, catalog.RepairImpactClip };
        }

        [Test]
        public void CatalogBindsFourDistinctRealClips()
        {
            var clips = Clips();
            foreach (var clip in clips) Assert.IsNotNull(clip);
            Assert.AreEqual(4, clips.Select(c => c.GetInstanceID()).Distinct().Count());
            Assert.That(clips[0].length, Is.InRange(.15f, .5f)); Assert.That(clips[1].length, Is.InRange(.12f, .4f)); Assert.That(clips[2].length, Is.InRange(.2f, .6f));
            // Different shapes: the drop is the longest and the lowest-pitched, the seat knock is shorter than the lift.
            Assert.Greater(clips[2].length, clips[0].length); Assert.Greater(clips[0].length, clips[1].length);
        }

        [Test]
        public void ClipsAreAudibleBoundedAndFreeOfClicks()
        {
            foreach (var clip in Clips().Take(3))
            {
                var data = new float[clip.samples * clip.channels];
                Assert.IsTrue(clip.GetData(data, 0), clip.name);
                var peak = data.Max(Mathf.Abs);
                Assert.That(peak, Is.InRange(.15f, 1f), clip.name + " peak");
                Assert.IsFalse(data.Any(float.IsNaN), clip.name);
                Assert.Less(Mathf.Abs(data[data.Length - 1]), .01f, clip.name + " ends silent (no click)");
            }
        }
    }
}
