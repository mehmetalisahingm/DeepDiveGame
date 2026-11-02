using UnityEngine;

namespace DeepDive.World
{
    // Data-only five-species editor authoring plan. NGO identities and rendering
    // remain in the authored scene, never this roster.
    public static class P411FiveSpeciesRoster
    {
        public const int Population = 15;
        public static readonly string[] SpeciesIds =
            { "sea_bass", "tuna", "red_snapper", "grouper", "lanternfish" };
        public static readonly string[] VisualNames =
            { "SeaBass", "Tuna", "RedSnapper", "Grouper", "Lanternfish" };

        // Three individuals of each species. Two different species are accessible
        // from the early shallow coast, two inhabit the reef and one the deep basin.
        // The existing 15 NGO scene identities survive; no extra prefab registrations.
        public static readonly Vector3[] Homes =
        {
            // Shallow / the original game-day hunting approach.
            new Vector3(0f, 4f, 8f),
            new Vector3(-8f, 3.5f, 2.5f),
            new Vector3(-5.3333f, 3.5f, 2.5f),
            // Tuna / open shallows.
            new Vector3(-5f, 3.5f, 10f),
            new Vector3(0f, 3.5f, 11f),
            new Vector3(5f, 3.5f, 10f),
            // Red snapper / eastern reef shelf.
            new Vector3(7f, -4f, 30.5f),
            new Vector3(8f, -4f, 35f),
            new Vector3(6f, -4f, 38f),
            // Grouper / western reef band, above reef floor.
            new Vector3(-10f, -4.5f, 28f),
            new Vector3(-10f, -4.5f, 33f),
            new Vector3(-10f, -4.5f, 38f),
            // Lanternfish / deep open-water column, west of boss & boat.
            new Vector3(-8f, -18f, 49f),
            new Vector3(-9f, -18f, 56f),
            new Vector3(-8f, -18f, 60f)
        };

        public static string SpeciesForSlot(int index) => SpeciesIds[index / 3];
        public static string VisualForSlot(int index) => VisualNames[index / 3];


    }
}
