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
            // Sea bass / shallows, north of the sand slope that rises to the wade (#219).
            new Vector3(0f, 4f, 8f),
            new Vector3(-8f, 4f, 8f),
            new Vector3(-4f, 4f, 8f),
            // Tuna / open shallows.
            new Vector3(-5f, 4f, 11f),
            new Vector3(0f, 4f, 11.5f),
            new Vector3(5f, 4f, 10f),
            // Red snapper / eastern reef shelf.
            new Vector3(7f, -4f, 30.5f),
            new Vector3(8f, -4f, 35f),
            new Vector3(6f, -4f, 38f),
            // Grouper / western reef band, above reef floor.
            new Vector3(-10.5f, -3.5f, 26.5f),
            new Vector3(-10f, -4.5f, 33f),
            new Vector3(-10f, -4.5f, 38f),
            // Lanternfish / over the basin floor, west of boss & boat and off the boss traces. #258: the first two
            // rose 2.5 m when trace 3 went down to the bed, which put them 3.8 m from it; x/z keep them off the walls.
            new Vector3(-10f, -19.5f, 58f),
            new Vector3(-9.5f, -19f, 60f),
            new Vector3(-4f, -21f, 60f)
        };

        public static string SpeciesForSlot(int index) => SpeciesIds[index / 3];
        public static string VisualForSlot(int index) => VisualNames[index / 3];


    }
}
