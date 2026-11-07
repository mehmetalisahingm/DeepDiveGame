using DeepDive.Network;
using DeepDive.Core.Contracts;
using NUnit.Framework;

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
