using System.Collections.Generic;

namespace DeepDive.Core.Contracts
{
    // P4.8 (#188) repair-site anchors: the stable names of the places the starter-boat repair uses.
    // Kept apart from WorldAnchorIds on purpose: those are built by ContinuousTownLayout under the
    // town root, these are authored by World into DiveTestArea next to the beach they stand on.
    // Ids only; where each one stands is World's business (DiveTestAreaRepairCradleSetup). The
    // anchors are places, not authority: acquisition, reservation and completion stay with the
    // existing repair/economy authority, and there is still exactly one boat-1.
    public static class RepairAnchorIds
    {
        // Where the broken boat-1 lies while it is being repaired (pose: position + yaw).
        public const string Cradle = "repair-boat-cradle";
        // Safe drop / hand-over point for released material (Q, guest disconnect, Closing).
        public const string Drop = "repair-drop-0";

        public const int WorkCount = 4;
        private const string WorkPrefix = "repair-work-";

        private static readonly string[] all = BuildAll();

        // Work point i: 0..2 serve repair slot i (BoatRepairParts.All order), 3 is the fourth crew place.
        // Out of range -> "" so a bad index can never alias a real anchor.
        public static string Work(int index) => index >= 0 && index < WorkCount ? WorkPrefix + index : "";

        // Fixed order: cradle, work points, drop.
        public static IReadOnlyList<string> All => all;

        private static string[] BuildAll()
        {
            var ids = new List<string> { Cradle };
            for (var i = 0; i < WorkCount; i++) ids.Add(Work(i));
            ids.Add(Drop);
            return ids.ToArray();
        }
    }
}
