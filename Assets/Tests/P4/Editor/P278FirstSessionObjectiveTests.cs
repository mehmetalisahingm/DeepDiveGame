using DeepDive.Network;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class P278FirstSessionObjectiveTests
    {
        [Test]
        public void FirstSessionHasClearNextStepWithoutFakingEconomicProgress()
        {
            StringAssert.Contains("Hazirlan",
                FirstSessionObjectiveRules.Next(false, false, false, false, false));
            StringAssert.Contains("Kasaya",
                FirstSessionObjectiveRules.Next(false, true, false, false, false));
            StringAssert.Contains("KAYIT",
                FirstSessionObjectiveRules.Next(false, false, true, true, false));
            StringAssert.Contains("ZIPKIN",
                FirstSessionObjectiveRules.Next(false, false, false, true, false));
            StringAssert.Contains("TOPLAMA",
                FirstSessionObjectiveRules.Next(false, false, false, false, true));
        }

        [Test]
        public void FirstPickupCelebrationDoesNotRepeatOnLaterCatches()
        {
            Assert.IsTrue(FirstSessionObjectiveRules.ShouldCelebratePickup(false, PlayerFeedbackCue.PickupAccepted));
            Assert.IsFalse(FirstSessionObjectiveRules.ShouldCelebratePickup(true, PlayerFeedbackCue.PickupAccepted));
            Assert.IsFalse(FirstSessionObjectiveRules.ShouldCelebratePickup(false, PlayerFeedbackCue.HarpoonHit));
        }

        [Test]
        public void FirstSessionPanelDoesNotCoverTheDefaultMapOrVitals()
        {
            foreach (var width in new[] { 1920f, 1280f, 640f, 600f, 480f })
            {
                var panel = FirstSessionObjectiveRules.ObjectivePanelRect(width);
                var map = new Rect(width - 240f, 20f, 220f, 220f);
                var vitals = new Rect(20f, 20f, 230f, 115f);
                Assert.IsFalse(panel.Overlaps(map), "onboarding cannot hide the map at " + width);
                Assert.IsFalse(panel.Overlaps(vitals), "onboarding cannot hide vitals at " + width);
                Assert.Greater(panel.width, 180f);
            }
        }

        [Test]
        public void SafetyAndCurrentActionsOverrideOldSuccessHint()
        {
            StringAssert.Contains("DALIS BITTI",
                FirstSessionObjectiveRules.Next(true, true, true, true, true));
            StringAssert.Contains("Kasaya",
                FirstSessionObjectiveRules.Next(false, true, false, true, true));
            StringAssert.Contains("KAYIT",
                FirstSessionObjectiveRules.Next(false, false, true, true, true));
        }
    }
}
