using Unity.Netcode;
using UnityEngine;

namespace DeepDive.World
{
    // The host-side clock for one special event, and nothing else. Every decision it looks like
    // it is making belongs to SpecialEventCycle: this component builds the cycle from the
    // definition asset, ticks it on the server, and mirrors one bool to the clients.
    //
    // Host-only, like FishActor's AI and RecordingSubject's sampling. A client never runs the
    // schedule or the window - it only reads Active - which is what stops a client deciding that
    // the event is on screen and therefore filmable.
    //
    // Lives on the same NetworkObject as a RecordingSubject with an explicit subjectId
    // ("event_bioluminescence") and no FishActor: aiming at the event resolves the subject the
    // same way aiming at the fish does, and RecordingSubject picks this component up through
    // IRecordingWindow, so no wiring is needed beyond putting the two together.
    //
    // It never despawns anything. Mehmet, 14 September 2026: the visual may close, but the
    // target object is not destroyed while a recording is still open, and despawning a
    // RecordingSubject runs its AbortAll and burns every open take. The event object is
    // scene-placed and simply lives out the dive with Active false; the dive's own teardown is
    // what removes it, by which point any open take is void under the dive rules anyway.
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(RecordingSubject))]
    public sealed class SpecialEventRunner : NetworkBehaviour, IRecordingWindow
    {
        [Tooltip("The event's static definition: id, 45 s trigger delay, 20 s window, quality table, signal.")]
        [SerializeField] private RecordingEventDefinition definition;

        // Server writes, everyone reads. SpecialEventPresenter is the only consumer: it is what
        // turns the particles, the light and the start sound on and off.
        public readonly NetworkVariable<bool> Active = new NetworkVariable<bool>();

        // Built in Awake, so it exists before any OnNetworkSpawn and RecordingSubject can pick
        // this component up as its window without caring which spawned first. Inert on a client:
        // only FixedUpdate advances it, and that is server-only.
        private SpecialEventCycle cycle;

        // Read by the scene test and the setup script; the running state is host-side only.
        public RecordingEventDefinition Definition => definition;

        // IRecordingWindow, the seam RecordingSession consults. Null cycle - a misconfigured
        // event - reads as shut, so nothing can be filmed rather than everything.
        public bool IsOpen => cycle != null && cycle.IsOpen;

        private void Awake()
        {
            // A missing or invalid definition is a setup fault, not a decision about a dive: the
            // cycle is left unbuilt and the event simply never appears, loudly.
            if (definition == null)
            {
                Debug.LogError($"P3_EVENT_INVALID object={name} reason=definition is not assigned", this);
                return;
            }
            if (!definition.IsValid(out var error))
            {
                Debug.LogError($"P3_EVENT_INVALID object={name} id={definition.EventId} reason={error}", this);
                return;
            }

            cycle = new SpecialEventCycle(definition.CreateSchedule(), definition.CreateWindow());
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            cycle?.Reset();
            Active.Value = false;
        }

        // The object is going away, so the appearance is over. The takes themselves are the
        // subject's business - RecordingSubject.OnNetworkDespawn is what drops them, and that is
        // exactly why this component never despawns anything on its own.
        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            cycle?.End();
        }

        // Host-only, and the only place the event's clock moves. Two lines of body: advance the
        // cycle with the host's own delta, and publish whether the event is on screen.
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || cycle == null) return;

            cycle.Tick(DiveContext.Source, Time.fixedDeltaTime);
            if (Active.Value != cycle.IsOpen) Active.Value = cycle.IsOpen;
        }
    }
}
