using System.Collections.Generic;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    public class SpecialEventPresentationTests
    {
        // Feeds one Active value per frame and keeps every cue that was not None.
        private static List<SpecialEventCue> Frames(SpecialEventPresentation presentation, params bool[] active)
        {
            var cues = new List<SpecialEventCue>();
            foreach (var value in active)
            {
                var cue = presentation.Observe(value);
                if (cue != SpecialEventCue.None) cues.Add(cue);
            }
            return cues;
        }

        [Test]
        public void FirstSightOfAQuietEventWipesTheScene()
        {
            var presentation = new SpecialEventPresentation();

            Assert.AreEqual(SpecialEventCue.HideAtOnce, presentation.Observe(false));
            Assert.IsFalse(presentation.IsShown);
        }

        // Mehmet's window is 20 s; a diver joining at second 12 sees the glow, but hearing the
        // start sound then would announce a beginning that is long past.
        [Test]
        public void ALateJoinerSeesTheGlowWithoutTheStartSound()
        {
            var presentation = new SpecialEventPresentation();

            Assert.AreEqual(SpecialEventCue.ShowSilently, presentation.Observe(true));
            Assert.IsTrue(presentation.IsShown);
        }

        [Test]
        public void TheEventStartingWhileWatchedPlaysTheStartSound()
        {
            var presentation = new SpecialEventPresentation();
            presentation.Observe(false);

            Assert.AreEqual(SpecialEventCue.ShowWithStartSound, presentation.Observe(true));
            Assert.IsTrue(presentation.IsShown);
        }

        [Test]
        public void TheEventEndingWhileWatchedFadesOut()
        {
            var presentation = new SpecialEventPresentation();
            presentation.Observe(false);
            presentation.Observe(true);

            Assert.AreEqual(SpecialEventCue.Fade, presentation.Observe(false));
            Assert.IsFalse(presentation.IsShown);
        }

        // Update feeds the same value every frame; only a change may do anything, or the start
        // sound would restart sixty times a second.
        [Test]
        public void AHeldValueCuesNothing()
        {
            var quiet = new SpecialEventPresentation();
            quiet.Observe(false);
            Assert.AreEqual(SpecialEventCue.None, quiet.Observe(false));
            Assert.AreEqual(SpecialEventCue.None, quiet.Observe(false));

            var shown = new SpecialEventPresentation();
            shown.Observe(false);
            shown.Observe(true);
            Assert.AreEqual(SpecialEventCue.None, shown.Observe(true));
            Assert.AreEqual(SpecialEventCue.None, shown.Observe(true));

            var lateJoiner = new SpecialEventPresentation();
            lateJoiner.Observe(true);
            Assert.AreEqual(SpecialEventCue.None, lateJoiner.Observe(true));
        }

        [Test]
        public void AWholeDiveCuesEachEdgeExactlyOnce()
        {
            var presentation = new SpecialEventPresentation();

            var cues = Frames(presentation,
                false, false, false,
                true, true, true, true, true,
                false, false, false);

            CollectionAssert.AreEqual(new[]
            {
                SpecialEventCue.HideAtOnce,
                SpecialEventCue.ShowWithStartSound,
                SpecialEventCue.Fade
            }, cues);
        }

        [Test]
        public void ALateJoinerStillSeesTheEventEnd()
        {
            var presentation = new SpecialEventPresentation();

            var cues = Frames(presentation, true, true, false, false);

            CollectionAssert.AreEqual(new[] { SpecialEventCue.ShowSilently, SpecialEventCue.Fade }, cues);
        }

        // The runner resets its cycle when a new dive starts, so Active goes true again in the
        // next dive while this machine never stopped watching: that is a real start.
        [Test]
        public void TheNextDivesEventPlaysTheStartSoundAgain()
        {
            var presentation = new SpecialEventPresentation();
            Frames(presentation, false, true, false);

            Assert.AreEqual(SpecialEventCue.ShowWithStartSound, presentation.Observe(true));
        }

        // The runner stopped being spawned while the glow was up - a despawn or a dropped
        // session. There is no Active left to believe, so the glow must not linger.
        [Test]
        public void ForgettingAShownEventHidesItAtOnce()
        {
            var presentation = new SpecialEventPresentation();
            Frames(presentation, false, true);

            Assert.AreEqual(SpecialEventCue.HideAtOnce, presentation.Forget());
            Assert.IsFalse(presentation.IsShown);
        }

        // Update calls Forget every frame until the runner spawns, so it must be free when there
        // is nothing on screen.
        [Test]
        public void ForgettingWithNothingOnScreenCuesNothing()
        {
            var fresh = new SpecialEventPresentation();
            Assert.AreEqual(SpecialEventCue.None, fresh.Forget());
            Assert.AreEqual(SpecialEventCue.None, fresh.Forget());

            var quiet = new SpecialEventPresentation();
            quiet.Observe(false);
            Assert.AreEqual(SpecialEventCue.None, quiet.Forget());

            var shown = new SpecialEventPresentation();
            shown.Observe(true);
            shown.Forget();
            Assert.AreEqual(SpecialEventCue.None, shown.Forget(), "the glow was already hidden once");
        }

        // A diver who reconnects in the middle of the event is a late joiner again, not a second
        // start of the event.
        [Test]
        public void AfterForgettingTheNextSightIsFirstSightAgain()
        {
            var midEvent = new SpecialEventPresentation();
            Frames(midEvent, false, true);
            midEvent.Forget();
            Assert.AreEqual(SpecialEventCue.ShowSilently, midEvent.Observe(true));

            var afterEvent = new SpecialEventPresentation();
            Frames(afterEvent, false, true);
            afterEvent.Forget();
            Assert.AreEqual(SpecialEventCue.HideAtOnce, afterEvent.Observe(false));
        }
    }
}
