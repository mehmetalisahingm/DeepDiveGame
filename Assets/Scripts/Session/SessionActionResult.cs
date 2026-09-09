namespace DeepDive.Session
{
    // Subset of docs/plan/CONTRACTS.md "Ortak red sonuclari" relevant to session/roster actions.
    public enum SessionActionResult
    {
        Ok,
        WrongPhase,
        PlayerInactive,
        AlreadyProcessed
    }
}
