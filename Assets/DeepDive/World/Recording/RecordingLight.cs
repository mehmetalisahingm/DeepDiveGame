using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // Where the light a recording is judged in comes from. An interface so a scene or a test can
    // supply its own; false means "no answer", which reads as daylight.
    public interface IRecordingLightSource
    {
        bool TryGetDaylight(out DaylightState daylight);
    }

    // How the last light read was answered, for the host log and the tests.
    public enum LightSourceMode
    {
        CampaignDay, // a source answered: in production, the host's campaign day
        Fallback     // nothing answered: the start-of-day light every pre-P4.1 scene was lit by
    }

    // The production adaptor between the shared day and the recording rules. It is the only
    // place in recording that reads a global: RecordingCameraRules takes the DaylightState this
    // resolves and never looks anything up itself.
    //
    // No new clock or light authority: DayManager binds DayLock.StateProvider on the host, and
    // it is the same CampaignDayState EconomyPlayerSync replicates and CoastalAtmosphere renders.
    public static class RecordingLight
    {
        // The light of the campaign day's first minute, before any dusk ramp.
        public static DaylightState Fallback =>
            DaylightModel.Evaluate(DayIds.DayStartMinute, DayPhase.Running, 0);

        public static readonly IRecordingLightSource CampaignDay = new DayLockLightSource();

        public static bool IsDayProviderBound => DayLock.StateProvider != null;

        // Host-side observation of the latest Resolve. Not read by any rule.
        public static LightSourceMode LastResolvedMode { get; private set; } = LightSourceMode.Fallback;

        public static DaylightState Resolve(IRecordingLightSource source) => Resolve(source, out _);

        public static DaylightState Resolve(IRecordingLightSource source, out LightSourceMode mode)
        {
            DaylightState daylight;
            if (source != null && source.TryGetDaylight(out daylight))
            {
                mode = LightSourceMode.CampaignDay;
            }
            else
            {
                daylight = Fallback;
                mode = LightSourceMode.Fallback;
            }

            LastResolvedMode = mode;
            return daylight;
        }

        private sealed class DayLockLightSource : IRecordingLightSource
        {
            public bool TryGetDaylight(out DaylightState daylight)
            {
                var state = DayLock.StateProvider;
                if (state == null)
                {
                    daylight = default;
                    return false;
                }

                daylight = DaylightModel.Evaluate(state());
                return true;
            }
        }
    }
}
