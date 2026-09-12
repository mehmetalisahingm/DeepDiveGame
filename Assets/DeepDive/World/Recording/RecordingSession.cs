using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.World
{
    // What one finished take was worth. Not a RecordingResult yet: a result is only minted at
    // settlement, once the dive is over and we know who actually surfaced (RecordingLedger).
    //
    // Named "take" rather than "evaluation" on purpose: DeepDive.Core.Contracts already has a
    // static RecordingEvaluation (P3-A's Composition seam), and DeepDive.Composition references
    // both assemblies, so the shorter name there would be ambiguous the moment a file imports
    // both namespaces.
    public readonly struct RecordingTake
    {
        public readonly string DiveId;
        public readonly PlayerId PlayerId;
        public readonly string SubjectId;
        public readonly int Quality;
        public readonly float ValidSeconds;
        public readonly float Score01;

        public RecordingTake(string diveId, PlayerId playerId, string subjectId, int quality,
            float validSeconds, float score01)
        {
            DiveId = diveId;
            PlayerId = playerId;
            SubjectId = subjectId;
            Quality = quality;
            ValidSeconds = validSeconds;
            Score01 = score01;
        }

        // Quality 0 means the shot was taken but earns nothing. Such a take is still a valid
        // outcome of a stop call; it simply never reaches the ledger.
        public bool IsPayable =>
            Quality > RecordingQuality.NoPayout &&
            !string.IsNullOrWhiteSpace(DiveId) &&
            !string.IsNullOrWhiteSpace(SubjectId);
    }

    // The recording authority for one subject, holding at most one open take per player (two
    // divers may film the same fish at once, each from their own camera). Pure rules object like
    // CatchState, so the ordering that matters is testable without NGO.
    //
    // The duration is measured here and nowhere else: Tick is fed the host's own delta time, and
    // there is deliberately no path by which a caller can declare how long it recorded. That is
    // the whole point of splitting start and stop into two calls - docs/plan/CONTRACTS.md,
    // "Sonuc doguran ... degisikliklerini ev sahibi dogrular".
    public sealed class RecordingSession
    {
        // One player's in-progress take, as opposed to the finished RecordingTake handed back by
        // TryStop. Score is time-weighted so a long steady shot beats a long shot that was only
        // briefly well framed.
        private sealed class OpenTake
        {
            public string DiveId;
            public float ValidSeconds;
            public float WeightedScore;

            public float Score01 => ValidSeconds > 0f ? WeightedScore / ValidSeconds : 0f;
        }

        // docs/plan/CONTRACTS.md: "Her degistirici istek requestId tasir; ev sahibi ayni istegi
        // ikinci kez yeni islem olarak uygulamaz ... daha onceki sonuc dondurulebilir." Start
        // and stop share one map because request ids are a single per-player sequence.
        private readonly Dictionary<(ulong Player, ulong Request), PlayerActionResult> handled =
            new Dictionary<(ulong, ulong), PlayerActionResult>();

        private readonly Dictionary<ulong, OpenTake> open = new Dictionary<ulong, OpenTake>();
        private readonly IReadOnlyList<QualityTier> tiers;

        public RecordingSession(string subjectId, IReadOnlyList<QualityTier> tiers)
        {
            SubjectId = subjectId;
            this.tiers = tiers;
        }

        // The species/event id that ends up in RecordingResult.SubjectId, so two fish of the
        // same species are the same subject: the payout rule is per species, not per animal.
        public string SubjectId { get; }

        public int OpenTakeCount => open.Count;

        public bool IsRecording(PlayerId player) => open.ContainsKey(player.Value);

        // Host-side progress read, for HUD feedback later. Zero for a player with no open take.
        public float ValidSecondsFor(PlayerId player) =>
            open.TryGetValue(player.Value, out var openTake) ? openTake.ValidSeconds : 0f;

        public float Score01For(PlayerId player) =>
            open.TryGetValue(player.Value, out var openTake) ? openTake.Score01 : 0f;

        // Accepted opens a take stamped with the live dive id. Nothing about the shot is judged
        // here: a player may start filming a badly framed subject, they just bank no time.
        public PlayerActionResult TryStart(IDiveContext dive, PlayerId player, ulong requestId)
        {
            if (handled.TryGetValue((player.Value, requestId), out var earlier)) return earlier;

            // A subject with no id could never produce a payable result, and silently recording
            // one would waste the player's dive. Not remembered: this is a setup fault, not a
            // decision about the request (same reasoning as CatchState's unbound sink).
            if (string.IsNullOrWhiteSpace(SubjectId)) return PlayerActionResult.InvalidTarget;

            // No live dive means no dive id to stamp, and Mert rejects anything carrying a stale
            // one. Also not remembered, so the same request may succeed once a dive is running.
            if (dive == null || !dive.IsDiveActive) return PlayerActionResult.InvalidState;
            var diveId = dive.CurrentDiveId;
            if (string.IsNullOrWhiteSpace(diveId)) return PlayerActionResult.InvalidState;

            if (open.ContainsKey(player.Value))
                return Remember(player, requestId, PlayerActionResult.InvalidState);

            open[player.Value] = new OpenTake { DiveId = diveId };
            return Remember(player, requestId, PlayerActionResult.Accepted);
        }

        // Called every host tick while a take is open. Only frames the framing rules accepted
        // add to the duration, so time spent swimming away or staring at a rock is not paid for.
        public void Tick(PlayerId player, float deltaTime, RecordingSample sample)
        {
            if (!open.TryGetValue(player.Value, out var openTake)) return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f) return;
            if (!sample.IsValid) return;

            openTake.ValidSeconds += deltaTime;
            openTake.WeightedScore += sample.Score01 * deltaTime;
        }

        // Accepted means the stop was processed, not that money was earned: the take may carry
        // Quality 0, and a replayed requestId returns the earlier result with an empty take.
        // Either way the caller MUST check RecordingTake.IsPayable before letting the ledger or
        // the economy see it - that check is the second half of the double-payment guard.
        public PlayerActionResult TryStop(IDiveContext dive, PlayerId player, ulong requestId,
            out RecordingTake take)
        {
            take = default;
            if (handled.TryGetValue((player.Value, requestId), out var earlier)) return earlier;

            if (!open.TryGetValue(player.Value, out var openTake))
                return Remember(player, requestId, PlayerActionResult.InvalidState);

            // A take that outlived its dive is void. Dropping it here is what stops a recording
            // started last dive from being cashed in during the next one ("Bir sonraki dalista
            // eski diveId'ye ait ... istek yeniden kullanilamaz").
            var liveDiveId = dive != null && dive.IsDiveActive ? dive.CurrentDiveId : "";
            open.Remove(player.Value);
            if (string.IsNullOrWhiteSpace(liveDiveId) || liveDiveId != openTake.DiveId)
                return Remember(player, requestId, PlayerActionResult.InvalidState);

            var quality = RecordingQuality.Evaluate(tiers, openTake.Score01, openTake.ValidSeconds);
            take = new RecordingTake(openTake.DiveId, player, SubjectId, quality,
                openTake.ValidSeconds, openTake.Score01);
            return Remember(player, requestId, PlayerActionResult.Accepted);
        }

        // Drops an open take without producing anything: the subject died, despawned, or the
        // recorder's camera went away. Silent by design - there is no half-recording to pay for.
        public void Abort(PlayerId player) => open.Remove(player.Value);

        public void AbortAll() => open.Clear();

        // For reuse across dives, matching CatchState.Reset: old request ids must not block the
        // new dive, and no take may survive into it.
        public void Reset()
        {
            handled.Clear();
            open.Clear();
        }

        private PlayerActionResult Remember(PlayerId player, ulong requestId, PlayerActionResult result)
        {
            handled[(player.Value, requestId)] = result;
            return result;
        }
    }
}
