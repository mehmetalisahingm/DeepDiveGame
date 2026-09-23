using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Trip
{
    [DisallowMultipleComponent]
    public sealed class BoatTripManager : MonoBehaviour
    {
        private const byte OpBoard = 1;
        private const byte OpDisembark = 2;
        private const byte OpStartRoute = 3;
        private const byte OpRequestReturn = 4;
        private const byte OpArrival = 5;

        public event Action OnTripChanged;

        private readonly Dictionary<string, PlayerId> _seats = new Dictionary<string, PlayerId>();
        private readonly HashSet<PlayerId> _party = new HashSet<PlayerId>();
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> _processed = new Dictionary<(PlayerId, ulong, byte), TransactionResult>();
        private string _tripId = "";
        private string _routeId = "";
        private BoatTripPhase _phase = BoatTripPhase.Docked;
        private PlayerId _owner;
        private bool _hasOwner;
        private int _revision;
        private int _tripSequence;
        private Func<BoatRepairStatus> _repairStatus;
        private ISessionRoster _roster;

        public interface ISessionRoster { bool IsConnected(PlayerId player); }

        public BoatTripState State => new BoatTripState(
            BoatTripIds.BoatId, _tripId, _routeId, _phase, SnapshotSeats(), SnapshotParty(), _owner, _hasOwner, _revision);

        public void Configure(Func<BoatRepairStatus> repairStatus, ISessionRoster roster)
        {
            _repairStatus = repairStatus;
            _roster = roster;
            BoatBoarding.Bind(TryBoard, TryDisembark);
            BoatRouteProgress.Bind(ReportArrival);
        }

        public void Shutdown()
        {
            BoatBoarding.Unbind(TryBoard, TryDisembark);
            BoatRouteProgress.Unbind(ReportArrival);
        }

        private List<BoatSeatAssignment> SnapshotSeats()
        {
            var list = new List<BoatSeatAssignment>(_seats.Count);
            foreach (var pair in _seats) list.Add(new BoatSeatAssignment(pair.Key, pair.Value));
            return list;
        }

        private List<PlayerId> SnapshotParty() => new List<PlayerId>(_party);

        private TransactionResult TryBoard(PlayerId player, string boatId, string seatId, ulong requestId)
        {
            var key = (player, requestId, OpBoard);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;

            TransactionResult result;
            if (!string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (!BoatTripIds.IsSeat(seatId))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (_repairStatus == null || _repairStatus() != BoatRepairStatus.Repaired)
                result = TransactionResult.Reject(requestId, "BoatNotRepaired", _revision);
            else if (_phase != BoatTripPhase.Docked && _phase != BoatTripPhase.Anchored)
                result = TransactionResult.Reject(requestId, "WrongPhase", _revision);
            else if (_seats.ContainsKey(seatId))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", _revision);
            else if (IsSeated(player))
                result = TransactionResult.Reject(requestId, "AlreadyProcessed", _revision);
            else
            {
                _seats[seatId] = player;
                _party.Add(player);
                if (!_hasOwner) { _owner = player; _hasOwner = true; }
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }

            _processed[key] = result;
            if (result.Accepted) OnTripChanged?.Invoke();
            return result;
        }

        private TransactionResult TryDisembark(PlayerId player, ulong requestId)
        {
            var key = (player, requestId, OpDisembark);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;
            TransactionResult result;
            if (_phase != BoatTripPhase.Docked && _phase != BoatTripPhase.Anchored)
                result = TransactionResult.Reject(requestId, "WrongPhase", _revision);
            else if (!IsSeated(player))
                result = TransactionResult.Reject(requestId, "InvalidState", _revision);
            else
            {
                RemoveFromSeat(player);
                if (_phase == BoatTripPhase.Docked) _party.Remove(player);
                ReassignOwnerIfNeeded(player);
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }
            _processed[key] = result;
            if (result.Accepted) OnTripChanged?.Invoke();
            return result;
        }

        private bool IsSeated(PlayerId player)
        {
            foreach (var pair in _seats) if (pair.Value.Equals(player)) return true;
            return false;
        }

        private void RemoveFromSeat(PlayerId player)
        {
            string found = null;
            foreach (var pair in _seats) if (pair.Value.Equals(player)) { found = pair.Key; break; }
            if (found != null) _seats.Remove(found);
        }

        private void ReassignOwnerIfNeeded(PlayerId leaving)
        {
            if (!_hasOwner || !_owner.Equals(leaving)) return;
            foreach (var member in _party)
            {
                if (member.Equals(leaving)) continue;
                if (_roster != null && !_roster.IsConnected(member)) continue;
                _owner = member; _hasOwner = true; return;
            }
            _hasOwner = false; _owner = default;
        }

        public TransactionResult TryStartRoute(PlayerId player, string routeId, ulong requestId)
        {
            var key = (player, requestId, OpStartRoute);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;
            TransactionResult result;
            if (_phase != BoatTripPhase.Docked)
                result = TransactionResult.Reject(requestId, "WrongPhase", _revision);
            else if (!string.Equals(routeId, BoatTripIds.NearRouteId, StringComparison.Ordinal))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (_seats.Count == 0)
                result = TransactionResult.Reject(requestId, "NoPassengers", _revision);
            else if (!_hasOwner || !_owner.Equals(player))
                result = TransactionResult.Reject(requestId, "InvalidState", _revision);
            else if (_repairStatus == null || _repairStatus() != BoatRepairStatus.Repaired)
                result = TransactionResult.Reject(requestId, "BoatNotRepaired", _revision);
            else
            {
                _tripSequence++;
                _tripId = $"{BoatTripIds.BoatId}-trip-{_tripSequence}";
                _routeId = routeId;
                _phase = BoatTripPhase.Outbound;
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }
            _processed[key] = result;
            if (result.Accepted) OnTripChanged?.Invoke();
            return result;
        }

        public TransactionResult TryRequestReturn(PlayerId player, ulong requestId)
        {
            var key = (player, requestId, OpRequestReturn);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;
            TransactionResult result;
            if (!_hasOwner || !_owner.Equals(player))
                result = TransactionResult.Reject(requestId, "InvalidState", _revision);
            else if (_phase != BoatTripPhase.Anchored)
                result = TransactionResult.Reject(requestId, "WrongPhase", _revision);
            else
            {
                _phase = BoatTripPhase.Inbound;
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }
            _processed[key] = result;
            if (result.Accepted) OnTripChanged?.Invoke();
            return result;
        }

        private TransactionResult ReportArrival(string boatId, BoatTripPhase reachedPhase, ulong requestId)
        {
            var key = (default(PlayerId), requestId, OpArrival);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;
            TransactionResult result;
            if (!string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (reachedPhase == BoatTripPhase.Anchored && _phase == BoatTripPhase.Outbound)
            {
                _phase = BoatTripPhase.Anchored;
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }
            else if (reachedPhase == BoatTripPhase.Docked && _phase == BoatTripPhase.Inbound)
            {
                _phase = BoatTripPhase.Docked;
                _tripId = "";
                _revision++;
                result = TransactionResult.Ok(requestId, _revision);
            }
            else result = TransactionResult.Reject(requestId, "WrongPhase", _revision);
            _processed[key] = result;
            if (result.Accepted) OnTripChanged?.Invoke();
            return result;
        }

        public void HandlePlayerDisconnected(PlayerId player)
        {
            if (!_party.Contains(player) && !IsSeated(player)) return;
            RemoveFromSeat(player);
            _party.Remove(player);
            ReassignOwnerIfNeeded(player);
            _revision++;
            OnTripChanged?.Invoke();
        }

        public bool TryAutoRecallIfEmpty()
        {
            if (_phase != BoatTripPhase.Anchored) return false;
            if (_party.Count > 0) return false;
            _phase = BoatTripPhase.Inbound;
            _revision++;
            OnTripChanged?.Invoke();
            return true;
        }

        public void ResetToDocked()
        {
            _seats.Clear();
            _party.Clear();
            _tripId = "";
            _routeId = "";
            _phase = BoatTripPhase.Docked;
            _hasOwner = false;
            _owner = default;
            _revision++;
            OnTripChanged?.Invoke();
        }
    }
}