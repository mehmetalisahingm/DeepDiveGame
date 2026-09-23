using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Trip
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BoatTripPlayerSync : NetworkBehaviour
    {
        public readonly NetworkVariable<FixedString32Bytes> TripId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<FixedString32Bytes> RouteId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<byte> Phase = new NetworkVariable<byte>();
        public readonly NetworkVariable<FixedString32Bytes> MySeatId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<bool> AmOwner = new NetworkVariable<bool>();
        public readonly NetworkVariable<int> SeatedCount = new NetworkVariable<int>();
        public readonly NetworkVariable<ulong> LastRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<bool> LastAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastReasonCode = new NetworkVariable<FixedString32Bytes>();

        private ulong _localRequestId;
        public bool IsSeated => MySeatId.Value.Length > 0;
        public bool ReadKeyboard { get; set; } = true;

        public void PublishTripState(BoatTripState state, PlayerId self)
        {
            if (!IsServer) return;
            TripId.Value = new FixedString32Bytes(state.TripId);
            RouteId.Value = new FixedString32Bytes(state.RouteId);
            Phase.Value = (byte)state.Phase;
            SeatedCount.Value = state.Seats?.Count ?? 0;
            AmOwner.Value = state.HasOwner && state.Owner.Equals(self);

            var seat = "";
            if (state.Seats != null)
                foreach (var assignment in state.Seats)
                    if (assignment.Player.Equals(self)) { seat = assignment.SeatId; break; }
            MySeatId.Value = new FixedString32Bytes(seat);
        }

        public void PublishResult(TransactionResult result)
        {
            if (!IsServer || result.RequestId < LastRequestId.Value) return;
            LastReasonCode.Value = new FixedString32Bytes(result.ReasonCode);
            LastAccepted.Value = result.Accepted;
            LastRequestId.Value = result.RequestId;
        }

        private void Update()
        {
            if (!ReadKeyboard || !IsSpawned || !IsOwner || !Application.isFocused) return;
            if (Input.GetKeyDown(KeyCode.B) && !IsSeated) RequestBoardNearestLocal();
            if (Input.GetKeyDown(KeyCode.G) && IsSeated) RequestDisembarkLocal();
        }

        public void RequestBoardNearestLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestBoardServerRpc(default, ++_localRequestId);
        }

        // Compatibility for UI/tests that already pass a seat id. The server deliberately ignores
        // the client-selected id and resolves the nearest authored seat through Mehmet's physical
        // authority instead.
        public void RequestBoardLocal(string seatId)
        {
            if (!IsSpawned || !IsOwner) return;
            RequestBoardServerRpc(new FixedString32Bytes(seatId ?? string.Empty), ++_localRequestId);
        }

        public void RequestDisembarkLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestDisembarkServerRpc(++_localRequestId);
        }

        public void RequestStartRouteLocal(string routeId)
        {
            if (!IsSpawned || !IsOwner) return;
            RequestStartRouteServerRpc(new FixedString32Bytes(routeId), ++_localRequestId);
        }

        public void RequestReturnLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestReturnServerRpc(++_localRequestId);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestBoardServerRpc(FixedString32Bytes ignoredClientSeatId, ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            PublishResult(BoatBoardingPhysicalInteraction.TryBoardNearest(new PlayerId(OwnerClientId), requestId));
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestDisembarkServerRpc(ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            PublishResult(BoatBoardingPhysicalInteraction.TryDisembark(new PlayerId(OwnerClientId), requestId));
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestStartRouteServerRpc(FixedString32Bytes routeId, ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var manager = FindFirstObjectByType<BoatTripManager>();
            if (manager == null) { PublishResult(TransactionResult.Reject(requestId, "InvalidState", 0)); return; }
            PublishResult(manager.TryStartRoute(new PlayerId(OwnerClientId), routeId.ToString(), requestId));
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestReturnServerRpc(ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var manager = FindFirstObjectByType<BoatTripManager>();
            if (manager == null) { PublishResult(TransactionResult.Reject(requestId, "InvalidState", 0)); return; }
            PublishResult(manager.TryRequestReturn(new PlayerId(OwnerClientId), requestId));
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            var phase = (BoatTripPhase)Phase.Value;
            GUI.Box(new Rect(20, 306, 260, 24), $"SANDAL: {phase} ({SeatedCount.Value} koltuklu)");
            if (IsSeated) GUI.Box(new Rect(20, 334, 260, 24), $"KOLTUK: {MySeatId.Value}" + (AmOwner.Value ? " (SEFER SORUMLUSU)" : ""));
            GUI.Box(new Rect(20, 362, 300, 24), "B: yakindaki koltuga bin / G: in / O: rota / R: donus");
        }
    }
}