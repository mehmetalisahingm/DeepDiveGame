using UnityEngine;
using DeepDive.World;

namespace DeepDive.Composition
{
    // #255: the host's own weight range per species, read from the species definitions the scene's fish carry, for the channel's size
    // evidence. Read only at a day close (rare), so scanning the scene's fish is cheap. (0, 0) = no such species in the scene: the
    // size part of the bonus is then neutral rather than guessed.
    public static class ChannelSpeciesWeights
    {
        public static (int Min, int Max) Range(string speciesId)
        {
            if (string.IsNullOrWhiteSpace(speciesId)) return (0, 0);
            foreach (var fish in Object.FindObjectsByType<FishActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var species = fish != null ? fish.Species : null;
                if (species != null && species.SpeciesId == speciesId) return (species.Weight.MinGrams, species.Weight.MaxGrams);
            }
            return (0, 0);
        }
    }
}
