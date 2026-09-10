namespace DeepDive.World
{
    // The only thing World needs to know about a running dive. Kept as an interface so this
    // module never references Mert's DeepDive.Session directly (docs/plan/CONTRACTS.md:
    // "Alan modulleri ortak sozlesmelere bagimlidir; birbirlerinin ic siniflarini ... aramaz").
    //
    // The dive id matters: Mert's InventoryManager.TryAddCatch rejects a CaptureResult whose
    // DiveId does not match the session's current dive, so a catch produced without a live
    // dive is worthless. CaptureBuilder refuses to build one instead of emitting a bad id.
    public interface IDiveContext
    {
        bool IsDiveActive { get; }
        string CurrentDiveId { get; }
    }

    // Host-side binding point. Composition owns the wiring (SessionManager.State -> here);
    // World only reads it. Left unbound the world simply produces no captures, which is the
    // safe direction: nothing is consumed and nothing invalid reaches Mert's bag.
    public static class DiveContext
    {
        public static IDiveContext Source { get; private set; }

        public static bool IsDiveActive =>
            Source != null && Source.IsDiveActive && !string.IsNullOrWhiteSpace(Source.CurrentDiveId);

        public static string CurrentDiveId => IsDiveActive ? Source.CurrentDiveId : string.Empty;

        public static void Bind(IDiveContext source) => Source = source;

        // Composition must call this when the session ends: a stale dive id outliving its
        // session would stamp the next dive's catches with the previous diveId.
        public static void Unbind() => Source = null;
    }
}
