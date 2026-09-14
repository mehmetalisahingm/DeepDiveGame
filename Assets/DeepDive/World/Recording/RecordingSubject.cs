using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.World
{
    // What RecordingDirector needs from a filmable thing, and nothing more - the same narrowing
    // IRecorderView does for the camera. An interface so the director's rules can be tested
    // against a plain fake instead of a spawned NetworkObject.
    //
    // It extends Core's IRecordingTarget so a subject is automatically the marker P3-A's
    // RecordingNetworkBridge scans for; Core's own interface stays empty on purpose, which is
    // why the subject id lives here in World rather than travelling in RecordingCandidate.
    public interface IRecordingSubject : IRecordingTarget
    {
        // The species/event id that ends up in RecordingResult.SubjectId. Empty means the
        // subject is misconfigured and can never produce a payable take.
        string SubjectId { get; }

        PlayerActionResult TryStartTake(PlayerId player, ulong requestId);

        // Accepted means the stop was processed, not that anything was earned: the take may
        // carry Quality 0, and a replayed requestId returns the earlier result with an empty
        // take. The caller MUST check RecordingTake.IsPayable before anything reaches the
        // ledger or the economy - RecordingDirector is the one place that does this.
        PlayerActionResult TryStopTake(PlayerId player, ulong requestId, out RecordingTake take);
    }

    // One filmable creature or event, and the recording authority for it. Shares its
    // NetworkObject with FishActor/CatchObject the same way the catch does: the thing a diver
    // aims at is the component that judges the shot, so no target id has to travel.
    //
    // P3-A's RecordingNetworkBridge resolves the aimed-at NetworkObject on the host with
    // GetComponentsInChildren<MonoBehaviour>() and keeps the first one that is an
    // IRecordingTarget. NetworkBehaviour is a MonoBehaviour, so this component is found by that
    // scan while still getting IsServer/IsSpawned for the host-only guards below.
    //
    // Host-only, like FishActor's AI: the session, the occlusion linecast and the per-tick
    // sampling all run on the server. A client never measures its own recording, which is what
    // stops a client from declaring how long or how well it filmed.
    //
    // Payability: this component only ever produces a RecordingTake. Whether that take is worth
    // anything is RecordingTake.IsPayable, and acting on it is RecordingDirector's job - see
    // TryStopTake above. Nothing here pays anybody.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class RecordingSubject : NetworkBehaviour, IRecordingSubject
    {
        [Tooltip("Species/event id sent to Mert as RecordingResult.SubjectId. " +
                 "Left empty a FishActor on the same object supplies its species id.")]
        [SerializeField] private string subjectId;

        [Tooltip("Framing gates and the quality tier ladder. Without it this subject cannot be filmed.")]
        [SerializeField] private RecordingQualityTable quality;

        [Tooltip("Roughly how large the subject is, in metres, for the frame-fill measure.")]
        [SerializeField] private float subjectRadiusMetres = 0.35f;

        [Tooltip("What the camera should be pointed at. Left empty this object's own position is used.")]
        [SerializeField] private Transform framingAnchor;

        [Tooltip("Which layers can block the shot. Divers and subjects must stay out of this mask, " +
                 "otherwise a recorder's own body or the subject itself reads as an obstacle.")]
        [SerializeField] private LayerMask occluderLayers = ~0;

        // Host-only state; all of it stays null on a client.
        private RecordingSession session;
        private RecordingTuning tuning;
        private readonly List<ulong> recorders = new List<ulong>();
        private readonly RaycastHit[] occlusionBuffer = new RaycastHit[8];
        private string resolvedSubjectId = "";

        public string SubjectId => resolvedSubjectId;

        // What SubjectId will become once the host spawns this subject. Readable in the editor
        // too, so DiveTestAreaRecordingSetup can log the id it just wired and the scene test can
        // assert it without a live NetworkManager.
        public string ExpectedSubjectId => ResolveSubjectId(subjectId, SpeciesIdOnThisObject());

        // Host-side reads for HUD feedback later, and for the tests that drive a spawned subject.
        public int OpenTakeCount => session == null ? 0 : session.OpenTakeCount;

        public bool IsRecording(PlayerId player) => session != null && session.IsRecording(player);

        public float ValidSecondsFor(PlayerId player) => session == null ? 0f : session.ValidSecondsFor(player);

        public float Score01For(PlayerId player) => session == null ? 0f : session.Score01For(player);

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            // A missing or inconsistent table is a setup fault, not a decision about a request:
            // the session is left unbuilt and every start is refused with InvalidTarget rather
            // than silently recording shots that could never be graded.
            if (quality == null)
            {
                Debug.LogError($"P3_SUBJECT_INVALID object={name} reason=quality table is not assigned", this);
                return;
            }
            if (!quality.IsValid(out var error))
            {
                Debug.LogError($"P3_SUBJECT_INVALID object={name} reason={error}", this);
                return;
            }

            resolvedSubjectId = ExpectedSubjectId;
            if (string.IsNullOrWhiteSpace(resolvedSubjectId))
            {
                Debug.LogError($"P3_SUBJECT_INVALID object={name} reason=subjectId is empty", this);
                return;
            }

            tuning = quality.Framing;

            // A subject that is only filmable part of the time carries its window on the same
            // object - SpecialEventRunner. A fish has none, so this is null and the session
            // treats the subject as always filmable. Same idiom as reading the species off a
            // FishActor beside us: what this object is made of decides how it behaves, and no
            // wiring step can be forgotten.
            session = new RecordingSession(resolvedSubjectId, quality.Tiers,
                GetComponent<IRecordingWindow>());
        }

        // A despawned subject cannot be filmed any further, and there is no half-recording to
        // pay for. Dropping the open takes here is the "subject died or despawned" case in
        // RecordingSession.Abort; the bridge's own target check turns the next stop into
        // InvalidTarget, so nothing is left waiting on a gone fish.
        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            session?.AbortAll();
            recorders.Clear();
        }

        // The payout rule is per species, not per animal, so the id is the species id: two fish
        // of the same kind are the same subject. The explicit inspector value wins, which is how
        // a scripted event gets filmed without a FishActor of its own.
        //
        // Static and taking plain strings so the rule is testable on its own: what a subject is
        // filmed as decides who gets paid for it, and that is too important to be reachable only
        // through a spawned NetworkObject. Empty means misconfigured, and TryStartTake refuses.
        public static string ResolveSubjectId(string explicitId, string speciesId)
        {
            if (!string.IsNullOrWhiteSpace(explicitId)) return explicitId.Trim();
            return string.IsNullOrWhiteSpace(speciesId) ? "" : speciesId.Trim();
        }

        private string SpeciesIdOnThisObject()
        {
            var fish = GetComponent<FishActor>();
            var species = fish == null ? null : fish.Species;
            return species == null ? null : species.SpeciesId;
        }

        public PlayerActionResult TryStartTake(PlayerId player, ulong requestId)
        {
            // IsSpawned first: reading IsServer off an unspawned behaviour depends on a live
            // NetworkManager. Only the host opens a take.
            if (!IsSpawned || !IsServer) return PlayerActionResult.Rejected;
            if (session == null) return PlayerActionResult.InvalidTarget;

            var result = session.TryStart(DiveContext.Source, player, requestId);
            SyncRecorder(player);
            return result;
        }

        public PlayerActionResult TryStopTake(PlayerId player, ulong requestId, out RecordingTake take)
        {
            take = default;
            if (!IsSpawned || !IsServer) return PlayerActionResult.Rejected;
            if (session == null) return PlayerActionResult.InvalidTarget;

            var result = session.TryStop(DiveContext.Source, player, requestId, out take);
            SyncRecorder(player);
            return result;
        }

        // The session is the single source of truth for who has an open take; this list only
        // exists so FixedUpdate has something to walk. Derived rather than incremented so a
        // replayed request - which returns the remembered result without changing anything -
        // cannot leave a phantom recorder behind.
        private void SyncRecorder(PlayerId player)
        {
            if (session.IsRecording(player))
            {
                if (!recorders.Contains(player.Value)) recorders.Add(player.Value);
            }
            else
            {
                recorders.Remove(player.Value);
            }
        }

        // Host-only, and the only place a recording's duration grows: the host's own fixed delta
        // is what gets banked, so no caller can declare how long it filmed. Runs per open take
        // because two divers may film the same subject at once, each from their own camera.
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || session == null || recorders.Count == 0) return;

            var deltaTime = Time.fixedDeltaTime;
            var subject = framingAnchor != null ? framingAnchor.position : transform.position;

            for (var i = recorders.Count - 1; i >= 0; i--)
            {
                var player = new PlayerId(recorders[i]);
                if (!session.IsRecording(player))
                {
                    recorders.RemoveAt(i);
                    continue;
                }

                // A diver whose camera went away banks no time but does not fail retroactively -
                // the take stays open and resumes if the camera comes back (IRecorderView).
                if (!RecorderViews.TryGetActive(player, out var view)) continue;

                var eye = view.EyePosition;
                var sample = RecordingFraming.Evaluate(eye, view.EyeForward, view.VerticalFieldOfViewDegrees,
                    subject, subjectRadiusMetres, IsOccluded(eye, subject), tuning);
                session.Tick(player, deltaTime, sample);
            }
        }

        // Solid geometry between the camera and the subject makes the shot worthless. Hits on
        // this subject's own hierarchy are skipped: its collider sits between the eye and its
        // centre by definition, so counting it would refuse every shot. Anything else in the
        // mask blocks. A buffer this size cannot realistically fill with self-hits alone, and a
        // full buffer already contains a blocker, so overflow needs no separate case.
        private bool IsOccluded(Vector3 eye, Vector3 subject)
        {
            var toSubject = subject - eye;
            var distance = toSubject.magnitude;
            if (distance <= 0.0001f) return false;

            var hits = Physics.RaycastNonAlloc(eye, toSubject / distance, occlusionBuffer, distance,
                occluderLayers, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hits; i++)
            {
                var hit = occlusionBuffer[i].collider;
                if (hit == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }
    }
}
