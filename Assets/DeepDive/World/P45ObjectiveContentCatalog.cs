using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepDive.World
{
    // Authored content references are available in town before the dive scene is loaded.
    // This is habitat metadata, never a live creature position or a second world authority.
    public sealed class P45ObjectiveContentCatalog : ScriptableObject
    {
        public const string ResourceName = "P45ObjectiveContentCatalog";

        [Serializable]
        public sealed class Target
        {
            public SpeciesDefinition Species;
            public RecordingEventDefinition Event;
            public string HabitatBandId;
            public string RouteId;
            public string SubjectId => Species != null ? Species.SpeciesId : Event != null ? Event.EventId : "";
            public bool IsValid => (Species != null && Event == null && Species.IsValid(out _)) ||
                (Event != null && Species == null && Event.IsValid(out _));
        }

        [SerializeField] private Target[] targets = Array.Empty<Target>();
        public IReadOnlyList<Target> Targets => targets;
    }
}
