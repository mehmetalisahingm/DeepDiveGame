namespace DeepDive.Inventory
{
    // Subset of docs/plan/CONTRACTS.md "Ortak red sonuclari" relevant to bag/catch actions.
    public enum InventoryActionResult
    {
        Ok,
        WrongPhase,
        InvalidTarget,
        AlreadyClaimed,
        InventoryFull,
        PlayerInactive
    }
}
