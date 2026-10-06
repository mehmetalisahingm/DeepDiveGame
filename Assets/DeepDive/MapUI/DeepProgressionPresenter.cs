using DeepDive.Core.Contracts;

namespace DeepDive.MapUI
{
    // P4.4-C (#123): the player-facing words for the deep progression chain. Pure: stage + counts in, text out. It receives only
    // the stage and the trace COUNT, so nothing hidden (a trace point, the arena, the boss) can leak through it before the host
    // has unlocked it; the hints name the next kind of action, never a place.
    public static class DeepProgressionPresenter
    {
        public static string Line(in DeepProgressionState state)
        {
            switch (state.Stage)
            {
                case DeepProgressionStage.Locked:
                    return "DERIN: henuz bir ipucu yok";
                case DeepProgressionStage.Encyclopedia:
                    return "DERIN: ansiklopedi acildi - resife ulas";
                case DeepProgressionStage.Rumor:
                    return $"DERIN: soylenti duyuldu - izleri ara ({state.TracesFound}/{state.TracesRequired})";
                case DeepProgressionStage.Trace:
                    return "DERIN: tum izler bulundu - derinligi kesfet";
                default:
                    return state.IsCompleted(DeepProgressionIds.BossId)
                        ? "DERIN: kesif tamam - karsilasma tamamlandi"
                        : "DERIN: kesif tamam - karsilasma acik";
            }
        }
    }
}
