using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Trip
{
    // Host-only trip/progression authority for issue #66 (docs/plan/CONTRACTS.md "Sandal tamiri ve
    // yolculuk"). Separate from EconomyManager on purpose - repair is #62's, travel is #66's, and
    // CONTRACTS explicitly keeps BoatTripState apart from SessionState/DiveId. This class owns the
    // state and every transition; Mehmet's seat/boarding and fixed-route movement never write it
    // directly, they call BoatBoarding/BoatRouteProgress (Core.Contracts), which this class binds
    // itself to while it is the host - one trip authority, matching the P3 boat draft's "uc ayri
    // sandal otoritesi kurulmaz".
    //
    // Not yet wired to any real scene object: Utku's dock/anchor anchors and Mehmet's seat/movement
    // do not exist yet as of this commit (#75/#76 have no code). This class is deliberately usable
    // and fully tested without them - board/disembark/sail/arrive/return/dock only need ids and a
    // requestId, not a Transform - so it is ready the moment either seam is bound.
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
        private readonly Dictionary<(PlayerId, ulong, byte), TransactionResult> _processed =
            new Dictionary<(PlayerId, ulong, byte), TransactionResult>();

        private string _tripId = "";
        private string _routeId = "";
        private BoatTripPhase _phase = BoatTripPhase.Docked;
        private PlayerId _owner;
        private bool _hasOwner;
        private int _revision;
        private int _tripSequence;

        private Func<BoatRepairStatus> _repairStatus;
        private ISessionRoster _roster;

        // Kept as a thin interface rather than a direct EconomyManager/NetworkSession reference so
        // this class - and its tests - never need either component to exist.
        public interface ISessionRoster
        {
            bool IsConnected(PlayerId player);
        }

        public BoatTripState State => new BoatTripState(
            BoatTripIds.BoatId, _tripId, _routeId, _phase, SnapshotSeats(), SnapshotParty(), _owner, _hasOwner, _revision);

        // Binds here rather than in Awake/OnEnable: plain MonoBehaviours do not receive those Unity
        // lifecycle calls in the Editor outside Play mode (measured directly - AddComponent in an
        // EditMode test left enabled/activeInHierarchy both true but never invoked OnEnable), which
        // is also why EconomyManager leans on a lazily-called EnsureSubscribed() rather than trusting
        // Awake alone. Composition and every test call Configure explicitly, so binding here is the
        // one path that actually runs in both contexts.
        public void Configure(Func<BoatRepairStatus> repairStatus, ISessionRoster roster)
        {
            _repairStatus = repairStatus;
            _roster = roster;
            BoatBoarding.Bind(TryBoard, TryDisembark);
            BoatRouteProgress.Bind(ReportArrival);
        }

        // Composition calls this before destroying the host object; tests call it in TearDown. Not
        // OnDestroy, for the same reason binding is not in OnEnable.
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

        // ---- Boarding / disembarking --------------------------------------------------------

        // Docked or Anchored only (CONTRACTS point 5/7): a diver who left the boat at anchor must be
        // able to re-board for the trip back, but nobody boards or leaves while the boat is moving.
        private TransactionResult TryBoard(PlayerId player, string boatId, string seatId, ulong requestId)
        {
            var key = (player, requestId, OpBoard);
            if (_processed.TryGetValue(key, out var replayed)) return replayed;

            TransactionResult result;
            if (!string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
            else if (!BoatTripIds.IsSeat(seatId))
                result = TransactionResult.Reject(requestId, "InvalidTarget", _revision);
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
                // Anchored: leaving the boat to dive does not leave the trip party (CONTRACTS point
                // 7 - the party is preserved at anchor). Docked: the trip is over for this player.
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

        // ---- Route: only the trip owner starts or calls for return -------------------------

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
                // Checked before ownership: with nobody aboard there is no meaningful owner to
                // compare the caller against, and "no passengers" is the more useful answer anyway.
                result = TransactionResult.Reject(requestId, "NoPassengers", _revision);
            else if (!_hasOwner || !_owner.Equals(player))
                result = TransactionResult.Reject(requestId, "InvalidState", _revision);
            else if (_repairStatus != null && _repairStatus() != BoatRepairStatus.Repaired)
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

        // Called through BoatRouteProgress by Mehmet's host-authoritative movement once it physically
        // reaches the anchor (Outbound -> Anchored) or the dock (Inbound -> Docked). This class does
        // not second-guess the arrival itself - that would be a second movement authority - only
        // whether it is valid for the phase the trip is actually in.
        private TransactionResult ReportArrival(string boatId, BoatTripPhase reachedPhase, ulong requestId)
        {
            // Movement authority reports, not a player action - keyed on the boat, not a PlayerId, so
            // a default PlayerId(0) here never collides with a real player's own idempotency keys
            // because the op byte is unique to this method.
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

        // ---- Disconnects and the empty boat --------------------------------------------------

        // CONTRACTS point 6/7: ownership passes to a living passenger on disconnect, never blocks the
        // rest of the party. Call from Composition's roster-changed handler (host only).
        public void HandlePlayerDisconnected(PlayerId player)
        {
            if (!_party.Contains(player) && !IsSeated(player)) return;
            RemoveFromSeat(player);
            _party.Remove(player);
            ReassignOwnerIfNeeded(player);
            _revision++;
            OnTripChanged?.Invoke();
        }

        // CONTRACTS point 8: an empty boat recalls itself once no active trip diver remains, and a
        // repeated call never creates a second boat. "Empty" here means nobody is seated AND nobody
        // is still out at anchor as a live party member with the trip open - i.e. the whole party has
        // disconnected or disembarked with nobody left to sail it back. Host calls this on a tick;
        // it is a no-op except in that exact situation.
        public bool TryAutoRecallIfEmpty()
        {
            if (_phase != BoatTripPhase.Anchored) return false;
            if (_party.Count > 0) return false;
            _phase = BoatTripPhase.Inbound;
            _revision++;
            OnTripChanged?.Invoke();
            return true;
        }

        // ---- Save/load --------------------------------------------------------------------

        // CONTRACTS: "Yeniden acilista hareket halindeki seferi restore etme... sandal iskelede,
        // koltuklar bostur." Trip state is intentionally never serialized (EconomySaveData already
        // covers the one thing that DOES persist - repair progress, i.e. whether the near route is
        // unlocked at all) - so a fresh instance, or an explicit reset on load, is already correct by
        // construction. This method exists so the load path has something explicit to call rather
        // than relying on "a new scene never had passengers".
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
