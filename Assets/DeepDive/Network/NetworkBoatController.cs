using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Network
{
    public interface IBoatRoutePathSource
    {
        bool TryGetRoute(string routeId, List<Vector3> points, out float outboundSeconds, out float inboundSeconds);
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

    public static class BoatRouteMath
    {
        public static float PathLength(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count < 2) return 0f;
            var total = 0f;
            for (var i = 1; i < points.Count; i++) total += Vector3.Distance(points[i - 1], points[i]);
            return total;
        }

        public static float ResolveLegSpeed(IReadOnlyList<Vector3> points, float nominalSeconds, float fallbackSpeed = 4f)
        {
            var length = PathLength(points);
            if (length <= 0.001f || nominalSeconds <= 0.01f) return Mathf.Max(0.1f, fallbackSpeed);
            return Mathf.Max(0.1f, length / nominalSeconds);
        }
    }

    public static class BoatSeatLayoutRules
    {
        public static readonly IReadOnlyList<string> SeatIds = new[]
        {
            BoatTripIds.Seat0, BoatTripIds.Seat1, BoatTripIds.Seat2, BoatTripIds.Seat3
        };

        public static readonly IReadOnlyList<Vector3> LocalSeatOffsets = new[]
        {
            new Vector3(-0.55f, 0.35f, -0.9f),
            new Vector3( 0.55f, 0.35f, -0.9f),
            new Vector3(-0.55f, 0.35f,  0.8f),
            new Vector3( 0.55f, 0.35f,  0.8f)
        };

        public static bool IsValid()
        {
            if (SeatIds.Count != 4 || LocalSeatOffsets.Count != 4) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < SeatIds.Count; i++)
                if (!BoatTripIds.IsSeat(SeatIds[i]) || !seen.Add(SeatIds[i])) return false;
            return seen.Count == 4;
        }
    }

    // Host-only physical authority for the repaired P3 rowboat. The GameObject itself is not a
    // second NetworkObject; its approved pose is mirrored through BoatTripPlayerSync, while seated
    // NetworkPlayers keep using their existing server-authoritative NetworkTransform.
    [DisallowMultipleComponent]
    public sealed class NetworkBoatController : MonoBehaviour, IBoatApprovedWorldPositionSource
    {
        public const float HullWidth = 2.4f;
        public const float HullLength = 5f;

        [SerializeField, Min(0.25f)] private float boardingRange = 2.75f;
        [SerializeField, Min(0.1f)] private float fallbackMoveSpeed = 4f;
        [SerializeField, Min(1f)] private float turnDegreesPerSecond = 120f;
        [SerializeField, Min(0.02f)] private float waypointTolerance = 0.2f;

        private readonly Dictionary<PlayerId, string> boundSeats = new Dictionary<PlayerId, string>();
        private readonly List<Vector3> path = new List<Vector3>();
        private NetworkManager networkManager;
        private IBoatRoutePathSource routeSource;
        private BoatTripState state;
        private int appliedRevision = int.MinValue;
        private int waypointIndex;
        private bool routeActive;
        private bool arrivalPending;
        private ulong arrivalRequestId;
        private float nextArrivalRetry;
        private BoatTripPhase arrivalPhase;
        private float legSpeed;
        private float outboundSeconds = 8f;
        private float inboundSeconds = 8f;

        public string BoatId => BoatTripIds.BoatId;
        public BoatTripState AppliedState => state;
        public float CurrentLegSpeed => legSpeed;
        public bool IsHostAuthority => networkManager != null && networkManager.IsServer;

        public void Initialize(NetworkManager manager, IBoatRoutePathSource source)
        {
            networkManager = manager;
            routeSource = source;
            BoatApprovedPositions.Bind(this);
            SnapToDockRouteStart();
        }

        public void Shutdown()
        {
            ReleaseAllPlayers();
            BoatApprovedPositions.Unbind(this);
            routeActive = false;
            arrivalPending = false;
            path.Clear();
            networkManager = null;
            routeSource = null;
        }

        public void SetRoutePathSource(IBoatRoutePathSource source)
        {
            routeSource = source;
            if (!IsHostAuthority) return;
            if (IsMovingPhase(state.Phase)) BeginRouteIfReady(true);
            else if (state.Phase == BoatTripPhase.Docked) SnapToDockRouteStart();
        }

        public void ApplyTripState(BoatTripState next)
        {
            if (!IsHostAuthority || !string.Equals(next.BoatId, BoatTripIds.BoatId, StringComparison.Ordinal)) return;
            if (next.Revision == appliedRevision) return;

            var previousPhase = state.Phase;
            state = next;
            appliedRevision = next.Revision;
            SyncSeatBindings(next);

            if (IsMovingPhase(next.Phase) && next.Phase != previousPhase)
                BeginRouteIfReady(true);
            else if (!IsMovingPhase(next.Phase))
            {
                routeActive = false;
                arrivalPending = false;
                path.Clear();
                if (next.Phase == BoatTripPhase.Docked) SnapToDockRouteStart();
            }
        }

        public TransactionResult TryBoardNearest(PlayerId player, ulong requestId)
        {
            if (!IsHostAuthority || requestId == 0)
                return TransactionResult.Reject(requestId, "InvalidState", state.Revision);
            if (state.Phase != BoatTripPhase.Docked && state.Phase != BoatTripPhase.Anchored)
                return TransactionResult.Reject(requestId, "WrongPhase", state.Revision);
            if (!TryGetPlayer(player, out var networkPlayer))
                return TransactionResult.Reject(requestId, "InvalidTarget", state.Revision);

            var bestIndex = -1;
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < BoatSeatLayoutRules.SeatIds.Count; i++)
            {
                var seatId = BoatSeatLayoutRules.SeatIds[i];
                if (IsSeatOccupied(seatId)) continue;
                var seatPosition = transform.TransformPoint(BoatSeatLayoutRules.LocalSeatOffsets[i]);
                var distance = Vector3.Distance(networkPlayer.transform.position, seatPosition);
                if (distance > boardingRange || distance >= bestDistance) continue;
                bestIndex = i;
                bestDistance = distance;
            }

            if (bestIndex < 0) return TransactionResult.Reject(requestId, "OutOfRange", state.Revision);
            return BoatBoarding.TryBoard(player, BoatTripIds.BoatId, BoatSeatLayoutRules.SeatIds[bestIndex], requestId);
        }

        public TransactionResult TryDisembark(PlayerId player, ulong requestId)
        {
            if (!IsHostAuthority || requestId == 0)
                return TransactionResult.Reject(requestId, "InvalidState", state.Revision);
            return BoatBoarding.TryDisembark(player, requestId);
        }

        private void FixedUpdate()
        {
            if (!IsHostAuthority) return;

            if (IsMovingPhase(state.Phase) && !routeActive && !arrivalPending)
                BeginRouteIfReady(false);

            if (routeActive) AdvanceRoute(Time.fixedDeltaTime);
            else if (arrivalPending && Time.unscaledTime >= nextArrivalRetry) ReportArrival();

            KeepSeatedPlayersAttached();
        }

        private void LateUpdate()
        {
            if (IsHostAuthority) KeepSeatedPlayersAttached();
        }

        private void BeginRouteIfReady(bool force)
        {
            if (!IsHostAuthority || !IsMovingPhase(state.Phase) || routeSource == null || string.IsNullOrEmpty(state.RouteId)) return;
            if (!force && (routeActive || arrivalPending)) return;

            path.Clear();
            if (!routeSource.TryGetRoute(state.RouteId, path, out outboundSeconds, out inboundSeconds) || path.Count < 2)
            {
                path.Clear();
                return;
            }

            if (state.Phase == BoatTripPhase.Inbound) path.Reverse();
            var duration = state.Phase == BoatTripPhase.Outbound ? outboundSeconds : inboundSeconds;
            legSpeed = BoatRouteMath.ResolveLegSpeed(path, duration, fallbackMoveSpeed);

            waypointIndex = 0;
            while (waypointIndex < path.Count - 1 && Vector3.Distance(transform.position, path[waypointIndex]) <= waypointTolerance)
                waypointIndex++;

            arrivalPhase = state.Phase == BoatTripPhase.Outbound ? BoatTripPhase.Anchored : BoatTripPhase.Docked;
            routeActive = true;
            arrivalPending = false;
        }

        private void AdvanceRoute(float deltaTime)
        {
            if (waypointIndex >= path.Count)
            {
                ArmArrival();
                return;
            }

            var target = path[waypointIndex];
            var delta = target - transform.position;
            var maxStep = legSpeed * Mathf.Max(0f, deltaTime);
            if (delta.sqrMagnitude <= maxStep * maxStep)
            {
                transform.position = target;
                waypointIndex++;
                if (waypointIndex >= path.Count) ArmArrival();
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target, maxStep);
            var planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (planar.sqrMagnitude > 0.0001f)
            {
                var desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, desired, Mathf.Max(0f, turnDegreesPerSecond) * Mathf.Max(0f, deltaTime));
            }
        }

        private void ArmArrival()
        {
            routeActive = false;
            arrivalPending = true;
            arrivalRequestId = arrivalRequestId == 0 ? 1UL : arrivalRequestId + 1UL;
            nextArrivalRetry = 0f;
            ReportArrival();
        }

        private void ReportArrival()
        {
            if (!arrivalPending) return;
            var result = BoatRouteProgress.ReportArrival(BoatTripIds.BoatId, arrivalPhase, arrivalRequestId);
            if (result.Accepted)
            {
                arrivalPending = false;
                return;
            }
            nextArrivalRetry = Time.unscaledTime + 0.25f;
        }

        private void SnapToDockRouteStart()
        {
            if (!IsHostAuthority || routeSource == null) return;
            var scratch = new List<Vector3>();
            if (!routeSource.TryGetRoute(BoatTripIds.NearRouteId, scratch, out outboundSeconds, out inboundSeconds) || scratch.Count == 0)
                return;
            transform.position = scratch[0];
            if (scratch.Count > 1)
            {
                var forward = Vector3.ProjectOnPlane(scratch[1] - scratch[0], Vector3.up);
                if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        private void SyncSeatBindings(BoatTripState next)
        {
            var desired = new Dictionary<PlayerId, string>();
            if (next.Seats != null)
            {
                for (var i = 0; i < next.Seats.Count; i++)
                    desired[next.Seats[i].Player] = next.Seats[i].SeatId;
            }

            var released = new List<PlayerId>();
            foreach (var pair in boundSeats)
                if (!desired.ContainsKey(pair.Key)) released.Add(pair.Key);
            for (var i = 0; i < released.Count; i++) ReleasePlayer(released[i]);

            foreach (var pair in desired)
            {
                boundSeats[pair.Key] = pair.Value;
                if (TryGetPlayer(pair.Key, out var player)) player.SetSeatedServer(true);
            }

            KeepSeatedPlayersAttached();
        }

        private void KeepSeatedPlayersAttached()
        {
            foreach (var pair in boundSeats)
            {
                if (!TryGetPlayer(pair.Key, out var player)) continue;
                var index = SeatIndex(pair.Value);
                if (index < 0) continue;
                var worldPosition = transform.TransformPoint(BoatSeatLayoutRules.LocalSeatOffsets[index]);
                player.Teleport(new Pose(worldPosition, transform.rotation));
            }
        }

        private void ReleasePlayer(PlayerId playerId)
        {
            if (!boundSeats.Remove(playerId)) return;
            if (!TryGetPlayer(playerId, out var player)) return;
            player.SetSeatedServer(false);

            var localExit = state.Phase == BoatTripPhase.Docked
                ? new Vector3(0f, 0.4f, -3.3f)
                : new Vector3(2f, 0f, 0.5f);
            var exitPosition = transform.TransformPoint(localExit);
            player.Teleport(new Pose(exitPosition, transform.rotation));
        }

        private void ReleaseAllPlayers()
        {
            var players = new List<PlayerId>(boundSeats.Keys);
            for (var i = 0; i < players.Count; i++) ReleasePlayer(players[i]);
            boundSeats.Clear();
        }

        private bool TryGetPlayer(PlayerId playerId, out NetworkPlayer player)
        {
            player = null;
            if (networkManager == null || !networkManager.ConnectedClients.TryGetValue(playerId.Value, out var client) || client.PlayerObject == null)
                return false;
            player = client.PlayerObject.GetComponent<NetworkPlayer>();
            return player != null;
        }

        private bool IsSeatOccupied(string seatId)
        {
            if (state.Seats == null) return false;
            for (var i = 0; i < state.Seats.Count; i++)
                if (string.Equals(state.Seats[i].SeatId, seatId, StringComparison.Ordinal)) return true;
            return false;
        }

        private static int SeatIndex(string seatId)
        {
            for (var i = 0; i < BoatSeatLayoutRules.SeatIds.Count; i++)
                if (string.Equals(BoatSeatLayoutRules.SeatIds[i], seatId, StringComparison.Ordinal)) return i;
            return -1;
        }

        public bool TryGetBoatWorldPosition(string boatId, out Vector3 position)
        {
            if (IsHostAuthority && string.Equals(boatId, BoatTripIds.BoatId, StringComparison.Ordinal))
            {
                position = transform.position;
                return true;
            }
            position = default;
            return false;
        }

        public bool TryGetPlayerWorldPosition(PlayerId player, out Vector3 position)
        {
            if (IsHostAuthority && IsPartyMember(player) && TryGetPlayer(player, out var networkPlayer))
            {
                position = networkPlayer.transform.position;
                return true;
            }
            position = default;
            return false;
        }

        private bool IsPartyMember(PlayerId player)
        {
            if (state.Party == null) return false;
            for (var i = 0; i < state.Party.Count; i++)
                if (state.Party[i].Equals(player)) return true;
            return false;
        }

        private static bool IsMovingPhase(BoatTripPhase phase) =>
            phase == BoatTripPhase.Outbound || phase == BoatTripPhase.Inbound;
    }
}
