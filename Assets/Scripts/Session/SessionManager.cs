using System;
using System.Collections.Generic;
using DeepDive.Core;
using UnityEngine;

namespace DeepDive.Session
{
    // Owns room roster and phase transitions (docs/plan/CONTRACTS.md: "Oturum asamalari | Mert |
    // Hazirlik/dalis/donus/sonuc durumu, hazir oyuncular ve gecis kosullari; gercek yuklemeyi
    // Mehmet'in ag servisi yurutur"). This class is the authority for the state it holds; it does
    // not decide connection admission (max 4 players etc. is Mehmet's P1-A, "Ag baglantisi").
    //
    // Runs standalone with no bridge assigned so it can be exercised solo, before Mehmet's
    // network scene-loading service (P1-A, issue #12) exists. Assign Bridge once that service
    // is ready so real transitions also request the networked scene load.
    public class SessionManager : MonoBehaviour
    {
        public ISessionNetworkBridge Bridge;

        public event Action<SessionState> OnSessionStateChanged;
        public event Action<IReadOnlyDictionary<PlayerId, bool>> OnRosterChanged;

        private SessionState _state;
        private readonly Dictionary<PlayerId, bool> _ready = new Dictionary<PlayerId, bool>();

        public SessionState State => _state;
        public IReadOnlyDictionary<PlayerId, bool> Roster => _ready;

        public void Initialize(string sessionId, string regionId)
        {
            _state = SessionState.CreateLobby(sessionId, regionId);
            _ready.Clear();
            RaiseStateChanged();
            RaiseRosterChanged();
        }

        public SessionActionResult Join(PlayerId player)
        {
            if (_ready.ContainsKey(player))
                return SessionActionResult.AlreadyProcessed;

            _ready[player] = false;
            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        public SessionActionResult Leave(PlayerId player)
        {
            if (!_ready.Remove(player))
                return SessionActionResult.PlayerInactive;

            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        public SessionActionResult SetReady(PlayerId player, bool ready)
        {
            if (_state.Phase != SessionPhase.Lobby)
                return SessionActionResult.WrongPhase;
            if (!_ready.ContainsKey(player))
                return SessionActionResult.PlayerInactive;

            _ready[player] = ready;
            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        // Lobby -> Prep. Requires at least one joined player and every joined player ready.
        public SessionActionResult BeginPrep()
        {
            if (_state.Phase != SessionPhase.Lobby)
                return SessionActionResult.WrongPhase;
            if (_ready.Count == 0)
                return SessionActionResult.PlayerInactive;
            foreach (var ready in _ready.Values)
            {
                if (!ready)
                    return SessionActionResult.PlayerInactive;
            }

            _state.Phase = SessionPhase.Prep;
            Advance();
            return SessionActionResult.Ok;
        }

        // Prep -> Dive.
        public SessionActionResult BeginDive(string diveId)
        {
            if (_state.Phase != SessionPhase.Prep)
                return SessionActionResult.WrongPhase;

            _state.Phase = SessionPhase.Dive;
            _state.DiveId = diveId;
            Advance();
            return SessionActionResult.Ok;
        }

        // Dive -> Return.
        public SessionActionResult BeginReturn()
        {
            if (_state.Phase != SessionPhase.Dive)
                return SessionActionResult.WrongPhase;

            _state.Phase = SessionPhase.Return;
            Advance();
            return SessionActionResult.Ok;
        }

        // Return -> Lobby. Clears DiveId and resets ready flags for the next round.
        public SessionActionResult CompleteReturn()
        {
            if (_state.Phase != SessionPhase.Return)
                return SessionActionResult.WrongPhase;

            _state.Phase = SessionPhase.Lobby;
            _state.DiveId = string.Empty;
            foreach (var player in new List<PlayerId>(_ready.Keys))
                _ready[player] = false;

            Advance();
            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        // Applies a state received from the network authority (non-host clients). Does not
        // request a scene load back through the bridge; the authority already did that.
        public void ApplyRemoteState(SessionState remoteState)
        {
            if (remoteState.Revision < _state.Revision)
                return;

            _state = remoteState;
            OnSessionStateChanged?.Invoke(_state);
        }

        private void Advance()
        {
            _state.Revision++;
            RaiseStateChanged();
            Bridge?.RequestSceneLoad(_state);
        }

        private void RaiseStateChanged() => OnSessionStateChanged?.Invoke(_state);
        private void RaiseRosterChanged() => OnRosterChanged?.Invoke(_ready);
    }
}
