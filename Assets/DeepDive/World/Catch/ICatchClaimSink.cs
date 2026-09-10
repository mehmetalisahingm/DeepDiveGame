using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // The bag side of the pickup chain, seen from World. Composition implements it against
    // Mert's InventoryManager.TryAddCatch and maps InventoryActionResult onto the shared
    // PlayerActionResult; World never references DeepDive.Inventory itself.
    //
    // docs/plan/CONTRACTS.md "Av alma": "Utku'nun av kaydi ile Mert'in canta kapasitesi
    // birlikte dogrulanir. Avi alinmis isaretleme ve cantaya ekleme tek mantiksal islem olarak
    // tamamlanir; kapasite yetersizse av yerde kalir."
    //
    // Only PlayerActionResult.Accepted means the catch entered a bag. Every other value leaves
    // the catch on the ground, untouched.
    public interface ICatchClaimSink
    {
        PlayerActionResult TryClaim(PlayerId player, CaptureResult capture);
    }

    // Host-side binding point, same shape as DiveContext. Composition binds it; World reads it.
    // Unbound means no authority can accept a catch, so pickups are refused and nothing is
    // consumed - never the other way around.
    public static class CatchClaim
    {
        public static ICatchClaimSink Sink { get; private set; }

        public static bool IsBound => Sink != null;

        public static void Bind(ICatchClaimSink sink) => Sink = sink;

        public static void Unbind() => Sink = null;
    }
}
