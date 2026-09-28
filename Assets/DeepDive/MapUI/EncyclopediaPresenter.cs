using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.MapUI
{
    // One encyclopedia line: which parts of the page the evidence has opened (WORLD_SYSTEMS "Ansiklopedi":
    // first verified encounter opens silhouette/category, a valid recording opens the name, a caught
    // specimen opens exact weight/value; habitat/depth comes with a recording). Research is P4.4.
    public readonly struct EncyclopediaEntry
    {
        public readonly string SpeciesId;
        public readonly string Category;
        public readonly bool Silhouette;
        public readonly bool NameKnown;
        public readonly string DisplayName;
        public readonly bool CatchData;
        public readonly bool HabitatKnown;
        public readonly IReadOnlyList<string> HabitatBandIds;
        public readonly int HabitatCellCount;
        public readonly int ProgressSteps;

        public EncyclopediaEntry(string speciesId, string category, bool silhouette, bool nameKnown, string displayName,
            bool catchData, bool habitatKnown, IReadOnlyList<string> habitatBandIds, int habitatCellCount, int progressSteps)
        {
            SpeciesId = speciesId;
            Category = category;
            Silhouette = silhouette;
            NameKnown = nameKnown;
            DisplayName = displayName;
            CatchData = catchData;
            HabitatKnown = habitatKnown;
            HabitatBandIds = habitatBandIds ?? Array.Empty<string>();
            HabitatCellCount = habitatCellCount;
            ProgressSteps = progressSteps;
        }

        public const int MaxProgressSteps = 3;
    }

    // #90 encyclopedia. Pure: SpeciesDiscoveryState in, entries out. Only species the host has counted
    // appear at all - an undiscovered species is not listed as "???", so the list cannot leak what exists.
    // Display text is the UI's (Mert's), the ids are Utku's; an id without text falls back to the id itself.
    public static class EncyclopediaPresenter
    {
        private static readonly Dictionary<string, (string Name, string Category)> Texts =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
            {
                ["sea_bass"] = ("Levrek", "Kiyi baligi")
            };

        public static IReadOnlyList<EncyclopediaEntry> Build(in ExplorationSnapshot snapshot)
        {
            var entries = new List<EncyclopediaEntry>(snapshot.Species.Count);
            for (var i = 0; i < snapshot.Species.Count; i++)
            {
                var s = snapshot.Species[i];
                if (string.IsNullOrEmpty(s.SpeciesId) || !(s.Sighted || s.Recorded || s.Caught)) continue;

                Texts.TryGetValue(s.SpeciesId, out var text);
                var category = text.Category ?? "Bilinmeyen tur";
                var name = text.Name ?? s.SpeciesId;
                // A catch or a recording is also an encounter: whoever has either has seen the silhouette.
                var silhouette = s.Sighted || s.Recorded || s.Caught;
                var steps = (s.Sighted ? 1 : 0) + (s.Recorded ? 1 : 0) + (s.Caught ? 1 : 0);
                entries.Add(new EncyclopediaEntry(s.SpeciesId, category, silhouette, s.Recorded, s.Recorded ? name : "???",
                    s.Caught, s.Recorded, s.Recorded ? s.ObservedDepthBandIds : Array.Empty<string>(),
                    s.Recorded ? s.ObservedCellIds.Count : 0, steps));
            }
            return entries;
        }

        public static string BandLabel(string depthBandId) => depthBandId switch
        {
            DepthBandIds.Shallow => "Sig (0-8 m)",
            DepthBandIds.Reef => "Resif",
            DepthBandIds.Deep => "Derin",
            _ => "Bilinmiyor"
        };
    }
}
