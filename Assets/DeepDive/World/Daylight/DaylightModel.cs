using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.World
{
    // What the light should be at one moment of the shared day. Multipliers, not colours or
    // absolute values: the atmosphere that owns the sun/fog/ambient keeps its own base look and
    // scales it by these, so this type never becomes a second owner of RenderSettings.
    //
    // World-only on purpose, not a Core contract: nobody outside the presentation reads it (Mert's
    // day UI shows the clock, not the light), and it is derived - never stored, synced or saved.
    public readonly struct DaylightState
    {
        // 1 = full day, 0 = full night. NightFactor is always 1 - DaylightFactor.
        public readonly float DaylightFactor;
        public readonly float SunIntensityMultiplier;
        public readonly float AmbientMultiplier;
        public readonly float FogColorMultiplier;

        public DaylightState(float daylightFactor, float sunIntensityMultiplier, float ambientMultiplier,
            float fogColorMultiplier)
        {
            DaylightFactor = daylightFactor;
            SunIntensityMultiplier = sunIntensityMultiplier;
            AmbientMultiplier = ambientMultiplier;
            FogColorMultiplier = fogColorMultiplier;
        }

        public float NightFactor => 1f - DaylightFactor;
    }

    // The pure day/night curve (P4.1 "Gun-gece isigi"). Input is only the host's CampaignDayState -
    // the clock every client already displays - so every machine computes the same light from the
    // same state and nothing about the light is sent over the network. Deterministic, no clock of
    // its own, no Random, no allocation.
    public static class DaylightModel
    {
        // Working times, minutes since midnight like DayIds. The campaign day runs 08:00-00:00, so
        // in play only the dusk ramp is reached; the dawn ramp keeps the curve total over 00:00-24:00.
        public const int DawnStartMinute = 5 * 60 + 30;   // 05:30 still night
        public const int DawnEndMinute = 7 * 60;          // 07:00 full day
        public const int DuskStartMinute = 19 * 60 + 30;  // 19:30 still full day
        public const int DuskEndMinute = 21 * 60;         // 21:00 full night

        // Night floors: night is dark, not black - a diver must still read the scene without a lamp.
        public const float NightSunIntensity = 0.08f;
        public const float NightAmbient = 0.25f;
        public const float NightFogColor = 0.2f;

        // The day's weather dims only the sun, and only a little; calm/windy weather itself is P4.5.
        public const float MaxOvercast = 0.15f;

        public static DaylightState Evaluate(in CampaignDayState day) =>
            Evaluate(day.ClockMinute, day.Phase, day.WeatherSeed);

        public static DaylightState Evaluate(int clockMinute, DayPhase phase, int weatherSeed)
        {
            // Morning is the new day being prepared: it is lit as the day's start, whatever the clock
            // held when the close finished. Closing/Summary keep the clock they closed at.
            var minute = phase == DayPhase.Morning ? DayIds.DayStartMinute : clockMinute;
            var daylight = DaylightFactorAt(minute);
            var overcast = Overcast01(weatherSeed) * MaxOvercast;

            return new DaylightState(
                daylight,
                Mathf.Lerp(NightSunIntensity, 1f, daylight) * (1f - overcast),
                Mathf.Lerp(NightAmbient, 1f, daylight),
                Mathf.Lerp(NightFogColor, 1f, daylight));
        }

        // Out-of-range minutes are clamped to the day's 00:00-24:00, never wrapped: a clock past the
        // end is still the night the day closes in, not the next morning.
        public static float DaylightFactorAt(int clockMinute)
        {
            var m = Mathf.Clamp(clockMinute, 0, DayIds.DayEndMinute);
            if (m <= DawnStartMinute || m >= DuskEndMinute) return 0f;
            if (m >= DawnEndMinute && m <= DuskStartMinute) return 1f;
            return m < DawnEndMinute
                ? Mathf.SmoothStep(0f, 1f, (m - DawnStartMinute) / (float)(DawnEndMinute - DawnStartMinute))
                : Mathf.SmoothStep(1f, 0f, (m - DuskStartMinute) / (float)(DuskEndMinute - DuskStartMinute));
        }

        // 0..1 from the seed alone (an integer hash, not System.Random), so the same seed gives the
        // same value on every machine and every run.
        public static float Overcast01(int weatherSeed)
        {
            unchecked
            {
                var h = (uint)weatherSeed * 0x9E3779B1u;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
