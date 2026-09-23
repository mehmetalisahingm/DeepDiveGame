using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DeepDive.Network
{
    public interface IBoatRoutePathSource
    {
        bool TryGetWorldPath(string routeId, List<Vector3> points);
    }

    public interface IBoatApprovedWorldPositionSource
    {
        bool TryGetBoatWorldPosition(string boatId, out Vector3 position);
        bool TryGetPlayerWorldPosition(PlayerId player, out Vector3 position);
    }

    public static class BoatApprovedPositions
    {
        public static IBoatApprovedWorldPositionSource Source { get; private set; }

        public static void Bind(IBoatApprovedWorldPositionSource source) => Source = source;

        public static void Unbind(IBoatApprovedWorldPositionSource source)
        {
            if (ReferenceEquals(Source, source)) Source = null;
        }
    }

    // Owner input never submits a seat id. The host resolves the nearest authored seat from the
    // authoritative player position, then BoatTripManager remains the only occupancy/trip authority.
    public static class BoatNetworkAuthority
    {
        private static NetworkBoatController _controller;

        public static bool IsBound => _controller != null;
        public static void Bind(NetworkBoatController controller) => _controller = controller;
        public static void Unbind(NetworkBoatController controller)
        {
            if (_controller == controller) _controller = null;
        }

        public static TransactionResult TryBoardNearest(PlayerId player, ulong requestId) =>
            _controller != null
                ? _controller.TryBoardNearest(player, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);

        public static TransactionResult TryDisembark(PlayerId player, ulong requestId) =>
            _controller != null
                ? _controller.TryDisembark(player, requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);
    }

    [DisallowMultipleComponent]
    public sealed class BoatSeatAnchor : MonoBehaviour
    {
        [SerializeField] private string seatId = BoatTripIds.Seat0;
        [SerializeField] private Transform seatedPose;
        [SerializeField] private Transform disembarkPose;

        public string SeatId => seatId ?? string.Empty;
        public Transform SeatedPose => seatedPose != null ? seatedPose : transform;
        public Transform DisembarkPose => disembarkPose != null ? disembarkPose : transform;
        public Vector3 BoardingPosition => transform.position;

        public void Configure(string id, Transform seated, Transform disembark)
        {
            seatId = id ?? string.Empty;
            seatedPose = seated;
            disembarkPose = disembark;
        }
    }

    public static class BoatSeatLayoutRules
    {
        public static bool IsValid(IReadOnlyList<string> seatIds)
        {
            if (seatIds == null || seatIds.Count != BoatTripIds.Seats.Count) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < seatIds.Count; i++)
            {
                var id = seatIds[i];
                if (!BoatTripIds.IsSeat(id) || !seen.Add(id)) return false;
            }
            return seen.Count == BoatTripIds.Seats.Count;
        }
    }

    // Mehmet/#75 movement authority. Mert owns BoatTripState, Utku owns the path points. This
    // component only validates physical boarding, binds players to seats and advances the real boat
    // along the supplied path on the host. No steering input exists.
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    [DisallowMultipleComponent]
    public sealed class NetworkBoatController : NetworkBehaviour, IBoatApprovedWorldPositionSource
    {
        [SerializeField, Min(0.25f)] private float boardingRange = 2.5f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
        [SerializeField, Min(1f)] private float turnDegreesPerSecond = 120f;
        [SerializeField, Min(0.02f)] private float waypointTolerance = 0.2f;

        private readonly Dictionary<string, BoatSeatAnchor> _seats =
            new Dictionary<string, BoatSeatAnchor>(StringComparer.Ordinal);
        private readonly Dictionary<PlayerId, string> _boundSeats = new Dictionary<PlayerId, string>();
        private readonly List<Vector3> _path = new List<Vector3>();

        private IBoatRoutePathSource _routeSource;
        private BoatTripState _state;
        private int _appliedRevision = int.MinValue;
        private int _waypointIndex;
        private bool _routeActive;
        private bool _arrivalPending;
        private ulong _arrivalRequestId;
        private float _nextArrivalRetry;
        private BoatTripPhase _arrivalPhase;

        public string BoatId => BoatTripIds.BoatId;
        public BoatTripState AppliedState => _state;

        public override void OnNetworkSpawn()
        {
            CacheSeats();
            if (!IsServer) return;
            BoatNetworkAuthority.Bind(this);
            BoatApprovedPositions.Bind(this);
            BoatBoarding.BindPhysicalValidator(ValidateBoardTarget);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                BoatBoarding.UnbindPhysicalValidator(ValidateBoardTarget);
                BoatApprovedPositions.Unbind(this);
                BoatNetworkAuthority.Unbind(this);
                ReleaseAllPlayers();
            }
            base.OnNetworkDespawn();
        }

        public void SetRoutePathSource(IBoatRoutePathSource source)
        {
            _routeSource = source;
            if (IsServer && IsMovingPhase(_state.Phase)) BeginRouteIfReady(force: true);
        }

        public void ApplyTripState(BoatTripState state)
        {
            if (!IsServer || !string.Equals(state.BoatId, BoatTripIds.BoatId, StringComparison.Ordinal)) return;
            if (state.Revision == _appliedRevision) return;

            var previousPhase = _state.Phase;
            _state = state;
            _appliedRevision = state.Revision;
            SyncSeatBindings(state);

            if (IsMovingPhase(state.Phase) && state.Phase != previousPhase)
                BeginRouteIfReady(force: true);
            else if (!IsMovingPhase(state.Phase))
            {
                _routeActive = false;
                _arrivalPending = false;
                _path.Clear();
            }
        }

        public TransactionResult TryBoardNearest(PlayerId player, ulong requestId)
        {
            if (!IsServer || requestId == 0) return TransactionResult.Reject(requestId, "InvalidState", _state.Revision);
            CacheSeats();
            if (!TryGetPlayer(player, out var networkPlayer))
                return TransactionResult.Reject(requestId, "InvalidTarget", _state.Revision);

            BoatSeatAnchor best = null;
            var bestDistance = float.PositiveInfinity;
            foreach (var anchor in _seats.Values)
            {
                var distance = Vector3.Distance(networkPlayer.transform.position, anchor.BoardingPosition);
                if (distance > boardingRange || distance >= bestDistance) continue;
                best = anchor;
                bestDistance = distance;
            }

            if (best == null) return TransactionResult.Reject(requestId, "OutOfRange", _state.Revision);
            return BoatBoarding.TryBoard(player, BoatTripIds.BoatId, best.SeatId, requestId);
        }

        public TransactionResult TryDisembark(PlayerId player, ulong requestId)
        {
            if (!IsServer || requestId == 0) return TransactionResult.Reject(requestId, "InvalidState", _state.Revision);
            return BoatBoarding.TryDisembark(player, requestId);
        }

        public bool ValidateBoardTarget(PlayerId player, string boatId, string seatId)
        {
            if (!IsServer || !string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal)) return false;
            CacheSeats();
            if (!_seats.TryGetValue(seatId ?? string.Empty, out var anchor)) return false;
            if (!TryGetPlayer(player, out var networkPlayer)) return false;
            return Vector3.Distance(networkPlayer.transform.position, anchor.BoardingPosition) <= boardingRange;
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            KeepSeatedPlayersAttached();

            if (IsMovingPhase(_state.Phase) && !_routeActive && !_arrivalPending)
                BeginRouteIfReady(force: false);

            if (_routeActive) AdvanceRoute(Time.fixedDeltaTime);
            else if (_arrivalPending && Time.unscaledTime >= _nextArrivalRetry) ReportArrival();
        }

        private void BeginRouteIfReady(bool force)
        {
            if (!IsMovingPhase(_state.Phase) || _routeSource == null || string.IsNullOrEmpty(_state.RouteId)) return;
            if (!force && (_routeActive || _arrivalPending)) return;

            _path.Clear();
            if (!_routeSource.TryGetWorldPath(_state.RouteId, _path) || _path.Count < 2)
            {
                _path.Clear();
                return;
            }

            if (_state.Phase == BoatTripPhase.Inbound) _path.Reverse();
            _waypointIndex = 0;
            while (_waypointIndex < _path.Count - 1 &&
                   Vector3.Distance(transform.position, _path[_waypointIndex]) <= waypointTolerance)
                _waypointIndex++;

            _arrivalPhase = _state.Phase == BoatTripPhase.Outbound ? BoatTripPhase.Anchored : BoatTripPhase.Docked;
            _routeActive = true;
            _arrivalPending = false;
        }

        private void AdvanceRoute(float deltaTime)
        {
            if (_waypointIndex >= _path.Count)
            {
                ArmArrival();
                return;
            }

            var target = _path[_waypointIndex];
            var delta = target - transform.position;
            if (delta.sqrMagnitude <= waypointTolerance * waypointTolerance)
            {
                transform.position = target;
                _waypointIndex++;
                if (_waypointIndex >= _path.Count) ArmArrival();
                return;
            }

            var step = Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, target, step);
            var planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (planar.sqrMagnitude > 0.0001f)
            {
                var desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desired,
                    Mathf.Max(0f, turnDegreesPerSecond) * Mathf.Max(0f, deltaTime));
            }
        }

        private void ArmArrival()
        {
            _routeActive = false;
            _arrivalPending = true;
            if (_arrivalRequestId == 0) _arrivalRequestId = 1;
            else _arrivalRequestId++;
            _nextArrivalRetry = 0f;
            ReportArrival();
        }

        private void ReportArrival()
        {
            if (!_arrivalPending) return;
            var result = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, _arrivalPhase, _arrivalRequestId);
            if (result.Accepted)
            {
                _arrivalPending = false;
                return;
            }
            _nextArrivalRetry = Time.unscaledTime + 0.25f;
        }

        private void CacheSeats()
        {
            _seats.Clear();
            var anchors = GetComponentsInChildren<BoatSeatAnchor>(true);
            for (var i = 0; i < anchors.Length; i++)
            {
                var id = anchors[i].SeatId;
                if (!BoatTripIds.IsSeat(id) || _seats.ContainsKey(id)) continue;
                _seats.Add(id, anchors[i]);
            }
        }

        public bool HasValidSeatLayout()
        {
            CacheSeats();
            var ids = new List<string>(_seats.Keys);
            return BoatSeatLayoutRules.IsValid(ids);
        }

        private void SyncSeatBindings(BoatTripState state)
        {
            CacheSeats();
            var next = new Dictionary<PlayerId, string>();
            if (state.Seats != null)
            {
                for (var i = 0; i < state.Seats.Count; i++)
                {
                    var assignment = state.Seats[i];
                    if (!_seats.ContainsKey(assignment.SeatId)) continue;
                    next[assignment.Player] = assignment.SeatId;
                }
            }

            var released = new List<PlayerId>();
            foreach (var pair in _boundSeats)
                if (!next.ContainsKey(pair.Key)) released.Add(pair.Key);

            for (var i = 0; i < released.Count; i++) ReleasePlayer(released[i]);
            foreach (var pair in next)
            {
                _boundSeats[pair.Key] = pair.Value;
                if (TryGetPlayer(pair.Key, out var player)) player.SetSeatedServer(true);
            }
            KeepSeatedPlayersAttached();
        }

        private void KeepSeatedPlayersAttached()
        {
            foreach (var pair in _boundSeats)
            {
                if (!_seats.TryGetValue(pair.Value, out var seat) || !TryGetPlayer(pair.Key, out var player)) continue;
                var pose = seat.SeatedPose;
                player.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private void ReleasePlayer(PlayerId playerId)
        {
            if (!_boundSeats.TryGetValue(playerId, out var seatId)) return;
            _boundSeats.Remove(playerId);
            if (!TryGetPlayer(playerId, out var player)) return;
            player.SetSeatedServer(false);
            if (_seats.TryGetValue(seatId, out var seat))
            {
                var pose = seat.DisembarkPose;
                player.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private void ReleaseAllPlayers()
        {
            var players = new List<PlayerId>(_boundSeats.Keys);
            for (var i = 0; i < players.Count; i++) ReleasePlayer(players[i]);
            _boundSeats.Clear();
        }

        private bool TryGetPlayer(PlayerId playerId, out NetworkPlayer player)
        {
            player = null;
            var manager = NetworkManager != null ? NetworkManager : NetworkManager.Singleton;
            if (manager == null || !manager.ConnectedClients.TryGetValue(playerId.Value, out var client) ||
                client.PlayerObject == null) return false;
            player = client.PlayerObject.GetComponent<NetworkPlayer>();
            return player != null;
        }

        public bool TryGetBoatWorldPosition(string boatId, out Vector3 position)
        {
            if (IsServer && string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal))
            {
                position = transform.position;
                return true;
            }
            position = default;
            return false;
        }

        public bool TryGetPlayerWorldPosition(PlayerId player, out Vector3 position)
        {
            if (IsServer && IsPartyMember(player) && TryGetPlayer(player, out var networkPlayer))
            {
                position = networkPlayer.transform.position;
                return true;
            }
            position = default;
            return false;
        }

        private bool IsPartyMember(PlayerId player)
        {
            if (_state.Party == null) return false;
            for (var i = 0; i < _state.Party.Count; i++)
                if (_state.Party[i].Equals(player)) return true;
            return false;
        }

        private static bool IsMovingPhase(BoatTripPhase phase) =>
            phase == BoatTripPhase.Outbound || phase == BoatTripPhase.Inbound;
    }
}