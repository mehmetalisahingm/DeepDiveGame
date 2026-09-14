using UnityEngine;

namespace DeepDive.World
{
    // The special event as the divers see and hear it: the plankton glow, the light and the
    // start sound, switched by SpecialEventRunner.Active and by nothing else.
    //
    // Pure presentation. It does not know whether the event can be filmed, how long is left or
    // who is recording - that is SpecialEventCycle and RecordingSubject, on the host. The host
    // reads the same Active the clients read rather than the cycle sitting beside it, so there
    // is one path and one window on every screen (Mehmet, 14 September 2026: everyone sees the
    // same window).
    //
    // Read every frame, the way NetworkPlayer drives its low-oxygen loop, not through
    // OnValueChanged: a client joining while the event is on gets Active as its initial sync, and
    // OnValueChanged does not fire for that. What a change means - including "joined late, no
    // start sound" - is SpecialEventPresentation's call, where it is tested.
    //
    // A plain MonoBehaviour on purpose: it sends nothing and owns no network state, so it takes
    // no NetworkBehaviour slot on the event's NetworkObject.
    //
    // Every field may be empty. Mert delivers the particle, the light and the start clip later
    // (the clip goes on the RecordingEventDefinition), and until then this runs silently. An
    // empty field here is a decision, not a setup fault, so it is skipped without a log - unlike
    // RecordingSubject's missing quality table, which is logged. Filling the fields in is the
    // whole integration step; no code changes.
    [RequireComponent(typeof(SpecialEventRunner))]
    public sealed class SpecialEventPresenter : MonoBehaviour
    {
        [Header("Signal (Mert fills these in; empty stays silent)")]
        [Tooltip("The plankton glow. Started and stopped only; its authored colours are kept.")]
        [SerializeField] private ParticleSystem glowParticles;

        [Tooltip("The blue/turquoise light. Takes the event definition's signal colour when shown.")]
        [SerializeField] private Light glowLight;

        [Tooltip("Plays the event definition's start clip once when the event begins.")]
        [SerializeField] private AudioSource startAudio;

        private readonly SpecialEventPresentation presentation = new SpecialEventPresentation();
        private SpecialEventRunner runner;

        private void Awake()
        {
            runner = GetComponent<SpecialEventRunner>();
        }

        // After every Awake on the object, so a particle system or audio source saved with
        // playOnAwake - Unity's default for both - is already running and is stopped here. The
        // event starts dark on the host and on every client, whatever the assets were saved as.
        private void Start()
        {
            Apply(SpecialEventCue.HideAtOnce, glowParticles, glowLight, startAudio, null);
        }

        private void Update()
        {
            var cue = runner.IsSpawned ? presentation.Observe(runner.Active.Value) : presentation.Forget();
            if (cue != SpecialEventCue.None) Apply(cue, glowParticles, glowLight, startAudio, runner.Definition);
        }

        // What each cue does to the scene. Static and handed the components, so the empty-field
        // rule is testable without spawning anything - the same reason
        // RecordingSubject.ResolveSubjectId is static. Every reference is checked on its own and
        // skipped when missing, and nothing here logs.
        public static void Apply(SpecialEventCue cue, ParticleSystem particles, Light light, AudioSource audio,
            RecordingEventDefinition definition)
        {
            switch (cue)
            {
                case SpecialEventCue.ShowWithStartSound:
                    Show(particles, light, definition);
                    var clip = definition == null ? null : definition.StartClip;
                    // Checked here rather than left to Unity: PlayOneShot with a null clip warns.
                    if (audio != null && clip != null) audio.PlayOneShot(clip);
                    break;

                case SpecialEventCue.ShowSilently:
                    Show(particles, light, definition);
                    break;

                // No new particles, but the ones already in the water live out their lifetime, so
                // the glow thins out instead of vanishing. The light has no such tail. The start
                // clip is short and long finished by the time a 20 s window closes.
                case SpecialEventCue.Fade:
                    if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    if (light != null) light.enabled = false;
                    break;

                case SpecialEventCue.HideAtOnce:
                    if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (light != null) light.enabled = false;
                    if (audio != null) audio.Stop();
                    break;
            }
        }

        // Only the light takes the definition's colour. The particle system keeps what Mert
        // authored into it: overwriting its start colour would undo his gradient. With no
        // definition the light keeps its own colour rather than being tinted to a default.
        private static void Show(ParticleSystem particles, Light light, RecordingEventDefinition definition)
        {
            if (light != null)
            {
                if (definition != null) light.color = definition.SignalColor;
                light.enabled = true;
            }
            if (particles != null) particles.Play(true);
        }
    }
}
