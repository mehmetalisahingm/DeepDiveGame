using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Session
{
    // Owns room roster and phase transitions (docs/plan/CONTRACTS.md: "Oturum asamalari | Mert |
    // Hazirlik/dalis/donus/sonuc durumu, hazir oyuncular ve gecis kosullari; gercek yuklemeyi
    // Mehmet'in ag servisi yurutur"). This class is the authority for the state it holds; it does
    // not decide connection admission (max 4 players etc. is Mehmet's P1-A, "Ag baglantisi").
    //
    // A null bridge is for isolated state-machine tests only. The game always assigns one.
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
            if (Bridge != null && !Bridge.IsAuthority) return SessionActionResult.NotHost;
            if (_ready.ContainsKey(player))
                return SessionActionResult.AlreadyProcessed;

            if (_state.Phase != SessionPhase.Lobby) return SessionActionResult.WrongPhase;
            if (_ready.Count >= 4) return SessionActionResult.RoomFull;

            _ready[player] = false;
            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        public SessionActionResult Leave(PlayerId player)
        {
            if (Bridge != null && !Bridge.IsAuthority) return SessionActionResult.NotHost;
            if (!_ready.Remove(player))
                return SessionActionResult.PlayerInactive;

            RaiseRosterChanged();
            return SessionActionResult.Ok;
        }

        public SessionActionResult SetReady(PlayerId player, bool ready)
        {
            if (Bridge != null && !Bridge.IsAuthority) return SessionActionResult.NotHost;
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

            var next = _state;
            next.Phase = SessionPhase.Prep;
            return Advance(next);
        }

        // Prep -> Dive.
        public SessionActionResult BeginDive(string diveId)
        {
            if (_state.Phase != SessionPhase.Prep)
                return SessionActionResult.WrongPhase;

            var next = _state;
            next.Phase = SessionPhase.Dive;
            next.DiveId = diveId;
            return Advance(next);
        }

        // Dive -> Return.
        public SessionActionResult BeginReturn()
        {
            if (_state.Phase != SessionPhase.Dive)
                return SessionActionResult.WrongPhase;

            var next = _state;
            next.Phase = SessionPhase.Return;
            return Advance(next);
        }

        // Return -> Lobby. Clears DiveId and resets ready flags for the next round.
        public SessionActionResult CompleteReturn()
        {
            if (_state.Phase != SessionPhase.Return)
                return SessionActionResult.WrongPhase;

            var next = _state;
            next.Phase = SessionPhase.Lobby;
            next.DiveId = string.Empty;
            var result = Advance(next);
            if (result != SessionActionResult.Ok) return result;
            foreach (var player in new List<PlayerId>(_ready.Keys))
                _ready[player] = false;

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

        public void ApplyRemoteSnapshot(SessionState state, IReadOnlyDictionary<PlayerId, bool> roster)
        {
            if (Bridge != null && Bridge.IsAuthority) return;
            if (state.Revision < _state.Revision || roster.Count > 4) return;
            _ready.Clear();
            foreach (var pair in roster) _ready.Add(pair.Key, pair.Value);
            ApplyRemoteState(state);
            RaiseRosterChanged();
        }

        private SessionActionResult Advance(SessionState next)
        {
            if (Bridge != null && !Bridge.IsAuthority) return SessionActionResult.NotHost;
            next.Revision++;
            if (Bridge != null && !Bridge.RequestSceneLoad(next)) return SessionActionResult.SceneLoadFailed;
            _state = next;
            RaiseStateChanged();
            return SessionActionResult.Ok;
        }

        private void RaiseStateChanged() => OnSessionStateChanged?.Invoke(_state);
        private void RaiseRosterChanged() => OnRosterChanged?.Invoke(_ready);
    }
}
