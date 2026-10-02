using System;
using DeepDive.Core.Contracts;
using NUnit.Framework;

namespace DeepDive.World.Tests
{
    // The production light adaptor, kept apart from the pure rule tests: this is the only fixture
    // that binds the shared day (DayLock), and it always unbinds it again.
    public class RecordingLightTests
    {
        private Func<CampaignDayState> provider;

        private sealed class SilentSource : IRecordingLightSource
        {
            public bool TryGetDaylight(out DaylightState daylight)
            {
                daylight = default;
                return false;
            }
        }

        [TearDown]
        public void UnbindDay()
        {
            if (provider != null) DayLock.UnbindState(provider);
            provider = null;
        }

        private void BindDay(int clockMinute)
        {
            var state = new CampaignDayState(1, "day-1", clockMinute, DayPhase.Running,
                Array.Empty<PlayerId>(), Array.Empty<PlayerId>(), 0, 1);
            provider = () => state;
            DayLock.BindState(provider, () => null);
        }

        private static void AssertDaylight(DaylightState light)
        {
            Assert.AreEqual(1f, light.DaylightFactor);
            Assert.AreEqual(1f, light.AmbientMultiplier);
        }

        [Test]
        public void ANullSourceIsTheDaylightFallback()
        {
            var light = RecordingLight.Resolve(null, out var mode);

            AssertDaylight(light);
            Assert.AreEqual(LightSourceMode.Fallback, mode);
            Assert.AreEqual(LightSourceMode.Fallback, RecordingLight.LastResolvedMode);
        }

        [Test]
        public void ASourceWithNoAnswerIsTheDaylightFallback()
        {
            var light = RecordingLight.Resolve(new SilentSource(), out var mode);

            AssertDaylight(light);
            Assert.AreEqual(LightSourceMode.Fallback, mode);
        }

        [Test]
        public void CampaignDayFollowsTheHostDay()
        {
            Assume.That(RecordingLight.IsDayProviderBound, Is.False, "another test left the day bound");
            AssertDaylight(RecordingLight.Resolve(RecordingLight.CampaignDay, out var unbound));
            Assert.AreEqual(LightSourceMode.Fallback, unbound);

            BindDay(22 * 60);
            Assert.IsTrue(RecordingLight.IsDayProviderBound);
            var night = RecordingLight.Resolve(RecordingLight.CampaignDay, out var bound);
            Assert.AreEqual(LightSourceMode.CampaignDay, bound);
            Assert.AreEqual(LightSourceMode.CampaignDay, RecordingLight.LastResolvedMode);
            Assert.AreEqual(0f, night.DaylightFactor);
            Assert.AreEqual(DaylightModel.NightAmbient, night.AmbientMultiplier);

            DayLock.UnbindState(provider);
            provider = null;
            Assert.IsFalse(RecordingLight.IsDayProviderBound);
            AssertDaylight(RecordingLight.Resolve(RecordingLight.CampaignDay, out var after));
            Assert.AreEqual(LightSourceMode.Fallback, after);
        }
    }
}
