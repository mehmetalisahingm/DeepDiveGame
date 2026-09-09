namespace DeepDive.Session
{
    // Composition connects Mert's state policy to Mehmet's actual scene service.
    // Rejected requests must not advance the phase or revision.
    public interface ISessionNetworkBridge
    {
        bool IsAuthority { get; }
        bool RequestSceneLoad(SessionState newState);
    }
}
