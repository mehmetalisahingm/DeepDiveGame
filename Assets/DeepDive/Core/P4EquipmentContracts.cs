namespace DeepDive.Core.Contracts
{
    public enum CameraTier
    {
        None = 0,
        Basic = 1,
        Advanced = 2,
        Professional = 3
    }

    // Player-facing capability snapshot derived from the authoritative LoadoutState + catalog.
    // It contains no prices or world validation rules: Mert owns catalog/ownership, Utku owns
    // capture/world validation, and Mehmet consumes this result in player/network presentation.
    public readonly struct PlayerEquipmentCapabilities
    {
        public readonly CameraTier CameraTier;
        public readonly int FinsLevel;
        public readonly int BagLevel;
        public readonly int HarpoonLevel;

        public PlayerEquipmentCapabilities(CameraTier cameraTier, int finsLevel, int bagLevel, int harpoonLevel)
        {
            CameraTier = cameraTier;
            FinsLevel = NonNegative(finsLevel);
            BagLevel = NonNegative(bagLevel);
            HarpoonLevel = NonNegative(harpoonLevel);
        }

        public bool HasCamera => CameraTier != CameraTier.None;

        private static int NonNegative(int value) => value < 0 ? 0 : value;
    }
}
