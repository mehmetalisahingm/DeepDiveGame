using UnityEngine;

namespace DeepDive.World
{
    // The static definition of one filmable special event, the same shape SpeciesDefinition has
    // for a fish: a stable id that goes over the network and into Mert's price table, plus the
    // numbers the rules read. Never per-player state (docs/plan/CONTRACTS.md: "ScriptableObject
    // nesnesi kalici oyuncu verisi gibi kullanilmaz").
    //
    // Every number here was decided by Mehmet on 14 September 2026: a small plankton cluster
    // with a blue/turquoise bioluminescent glow, started by the host 45 seconds into the dive,
    // open for 20 seconds, once per dive, the same window for everybody.
    //
    // The signal half is deliberately split. AudioClip and Color live here because they are
    // assets Mert tunes once; the ParticleSystem, Light and AudioSource that actually play them
    // live on SpecialEventPresenter in the scene, because a ScriptableObject cannot reference a
    // scene object. Both halves may be empty: a missing signal is silent, never an error.
    [CreateAssetMenu(menuName = "DeepDive/World/Recording Event Definition", fileName = "RecordingEvent")]
    public sealed class RecordingEventDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Goes out as RecordingResult.SubjectId and is what Mert prices. Stable forever.")]
        [SerializeField] private string eventId = "";

        [Tooltip("UI only. Never used as an identity.")]
        [SerializeField] private string displayName = "";

        [Header("Schedule (Mehmet, 14 September 2026)")]
        [Tooltip("Seconds after the dive starts before the host may begin the event.")]
        [SerializeField] private float triggerDelaySeconds = 45f;

        [Tooltip("How long the event stays filmable once it begins.")]
        [SerializeField] private float windowSeconds = 20f;

        [Header("Recording")]
        [Tooltip("Framing gates and the quality tier ladder for this event. Without it nothing can be filmed.")]
        [SerializeField] private RecordingQualityTable quality;

        [Tooltip("Roughly how large the event is, in metres, for the frame-fill measure.")]
        [SerializeField] private float subjectRadiusMetres = 1.5f;

        [Header("Signal (Mert fills these in; empty stays silent)")]
        [Tooltip("Played once when the event begins. Empty means no sound.")]
        [SerializeField] private AudioClip startClip;

        [Tooltip("Blue/turquoise glow colour driven by the presenter's light and particles.")]
        [SerializeField] private Color signalColor = new Color(0.25f, 0.85f, 0.95f, 1f);

        // The species/event id half of RecordingResult. Trimmed so a stray space in the
        // inspector cannot produce an id Mert's price table will never match.
        public string EventId => eventId == null ? "" : eventId.Trim();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EventId : displayName;

        public float TriggerDelaySeconds => triggerDelaySeconds;

        public float WindowSeconds => windowSeconds;

        public RecordingQualityTable Quality => quality;

        public float SubjectRadiusMetres => subjectRadiusMetres;

        public AudioClip StartClip => startClip;

        public Color SignalColor => signalColor;

        // The two rule objects this definition exists to configure. Built fresh per caller,
        // like SpeciesDefinition.Swim: the asset holds numbers, never live state.
        public SpecialEventWindow CreateWindow() => new SpecialEventWindow(windowSeconds);

        public SpecialEventSchedule CreateSchedule() => new SpecialEventSchedule(triggerDelaySeconds);

        // A setup fault check, in the same shape as SpeciesDefinition.IsValid and
        // RecordingQualityTable.IsValid, so the host-side shell can refuse to run a misconfigured
        // event loudly instead of producing an event nobody can film or be paid for.
        //
        // The signal fields are deliberately not checked: Mert may not have delivered the assets
        // yet, and a silent event is a working event.
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(eventId)) { error = "eventId is empty"; return false; }

            if (!CreateSchedule().IsValid)
            { error = "triggerDelaySeconds must be zero or a positive number"; return false; }

            if (!CreateWindow().IsValid)
            { error = "windowSeconds must be a positive number"; return false; }

            if (quality == null) { error = "quality table is not assigned"; return false; }
            if (!quality.IsValid(out var qualityError)) { error = "quality table: " + qualityError; return false; }

            if (float.IsNaN(subjectRadiusMetres) || float.IsInfinity(subjectRadiusMetres) ||
                subjectRadiusMetres <= 0f)
            { error = "subjectRadiusMetres must be a positive number"; return false; }

            error = "";
            return true;
        }
    }
}
