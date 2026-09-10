using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // Pure rules object for one collectable catch, so the ordering that matters can be tested
    // without NGO: the catch is consumed only after the bag has actually accepted it.
    //
    // docs/plan/CONTRACTS.md: "Utku'nun av tuketme islemi, Mert eklemeyi kabul etmeden
    // calistirilamaz" and "kapasite yetersizse av yerde kalir".
    public sealed class CatchState
    {
        // docs/plan/CONTRACTS.md: "Her degistirici istek requestId tasir; ev sahibi ayni istegi
        // ikinci kez yeni islem olarak uygulamaz ... daha onceki sonuc dondurulebilir."
        // Request ids are per-player sequences, so the player is part of the key.
        private readonly Dictionary<(ulong Player, ulong Request), PlayerActionResult> handled =
            new Dictionary<(ulong, ulong), PlayerActionResult>();

        public CaptureResult Capture { get; private set; }
        public bool IsClaimed { get; private set; }
        public bool IsArmed { get; private set; }
        public bool IsAvailable => IsArmed && !IsClaimed;

        // Arms the catch once the fish is dead and a valid capture exists.
        public bool Hold(CaptureResult capture)
        {
            if (IsArmed) return false;
            if (string.IsNullOrWhiteSpace(capture.CaptureId) || string.IsNullOrWhiteSpace(capture.DiveId)) return false;
            Capture = capture;
            IsArmed = true;
            return true;
        }

        // The whole pickup rule in one place. `consume` is the only thing that may destroy the
        // catch, and it is set for exactly one outcome: the sink answered Accepted.
        public PlayerActionResult TryClaim(ICatchClaimSink sink, PlayerId player, ulong requestId, out bool consume)
        {
            consume = false;

            // Replay first: a repeated request must never reach the bag a second time, even
            // after a successful claim already emptied this catch.
            if (handled.TryGetValue((player.Value, requestId), out var earlier)) return earlier;

            if (!IsAvailable) return PlayerActionResult.InvalidTarget;

            // No bound authority means nobody can accept the catch. Refuse and leave it on the
            // ground. This is deliberately not remembered: no authority decided anything, so a
            // later attempt (once Composition has bound the sink) is still allowed to succeed.
            if (sink == null) return PlayerActionResult.Rejected;

            var result = sink.TryClaim(player, Capture);
            handled[(player.Value, requestId)] = result;
            if (result != PlayerActionResult.Accepted) return result;

            IsClaimed = true;
            consume = true;
            return result;
        }

        // For reuse across dives ("Bir sonraki dalista eski diveId'ye ait ... av, olay veya
        // islem istegi yeniden kullanilamaz").
        public void Reset()
        {
            handled.Clear();
            Capture = default;
            IsArmed = false;
            IsClaimed = false;
        }
    }
}
