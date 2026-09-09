using System;

namespace DeepDive.Session
{
    // Order matches docs/plan/CONTRACTS.md ("Hazirlik/dalis/donus" durum) plus the room/ready
    // stage that precedes it per issue P1-C (#14): players join and ready up before prep.
    public enum SessionPhase
    {
        Lobby,
        Prep,
        Dive,
        Return
    }

    // docs/plan/CONTRACTS.md ortak veri sozlugu: "SessionState | Asama, sessionId, istege bagli
    // diveId, regionId ve revision | Mert -> Mehmet/Utku/UI | P1". Mert owns this shape and the
    // transitions between phases; the actual networked scene load is executed by Mehmet's
    // network service (see ISessionNetworkBridge).
    [Serializable]
    public struct SessionState : IEquatable<SessionState>
    {
        public SessionPhase Phase;
        public string SessionId;
        public string DiveId;
        public string RegionId;
        public int Revision;

        public static SessionState CreateLobby(string sessionId, string regionId)
        {
            return new SessionState
            {
                Phase = SessionPhase.Lobby,
                SessionId = sessionId,
                DiveId = string.Empty,
                RegionId = regionId,
                Revision = 0
            };
        }

        public bool Equals(SessionState other) =>
            Phase == other.Phase &&
            SessionId == other.SessionId &&
            DiveId == other.DiveId &&
            RegionId == other.RegionId &&
            Revision == other.Revision;

        public override bool Equals(object obj) => obj is SessionState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (int)Phase;
                hash = hash * 31 + (SessionId != null ? SessionId.GetHashCode() : 0);
                hash = hash * 31 + (DiveId != null ? DiveId.GetHashCode() : 0);
                hash = hash * 31 + (RegionId != null ? RegionId.GetHashCode() : 0);
                hash = hash * 31 + Revision;
                return hash;
            }
        }
    }
}
