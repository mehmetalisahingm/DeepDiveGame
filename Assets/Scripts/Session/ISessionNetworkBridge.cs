namespace DeepDive.Session
{
    // Implemented by Mehmet's network layer (P1-A, issue #12; docs/plan/CONTRACTS.md system
    // ownership: "Ag baglantisi | Mehmet | ... ag sahne yukleme mekanizmasi"). SessionManager
    // owns phase/roster decisions and calls this to request the actual networked scene load;
    // it does not perform networking itself. Not implemented yet: P1-A has not started, so
    // SessionManager runs standalone (see SessionManager.Bridge, nullable) for the solo test.
    public interface ISessionNetworkBridge
    {
        void RequestSceneLoad(SessionState newState);
    }
}
