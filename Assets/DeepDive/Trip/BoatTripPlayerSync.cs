using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Trip
{
    // Per-player mirror of the shared BoatTripState, the same reasoning as EconomyPlayerSync:
    // InventoryManager/BoatTripManager's state is host-only server state, so nothing replicates it
    // to a guest without this. Presentation and input only - every rule lives in BoatTripManager,
    // this class never decides whether a board/sail/return request is valid.
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

        public void PublishTripState(BoatTripState state, PlayerId self)
        {
            if (!IsServer) return;
            TripId.Value = new FixedString32Bytes(state.TripId);
            RouteId.Value = new FixedString32Bytes(state.RouteId);
            Phase.Value = (byte)state.Phase;
            SeatedCount.Value = state.Seats.Count;
            AmOwner.Value = state.HasOwner && state.Owner.Equals(self);

            var seat = "";
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

        // ---- Owner-submitted requests, mirroring NetworkPlayer.SubmitXLocal --------------------

        public void RequestBoardLocal(string seatId)
        {
            if (!IsSpawned || !IsOwner) return;
            RequestBoardServerRpc(new FixedString32Bytes(seatId), ++_localRequestId);
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
        private void RequestBoardServerRpc(FixedString32Bytes seatId, ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var result = BoatBoarding.TryBoard(new PlayerId(OwnerClientId), BoatTripIds.BoatId, seatId.ToString(), requestId);
            PublishResult(result);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestDisembarkServerRpc(ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var result = BoatBoarding.TryDisembark(new PlayerId(OwnerClientId), requestId);
            PublishResult(result);
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

        // Placeholder-art HUD, the same spirit as EconomyPlayerSync's IMGUI: real UI (map screen,
        // seat icons) is a later pass. Real seat *interaction* (walking up, aiming, pressing a key
        // in range) is Mehmet's #75, not modelled here - these keys act on the nearest seat/route
        // BoatTripBinding finds, standing in for it until his seam lands (see #66's seam-freeze
        // comment: BoatBoarding is his to call once real proximity/interaction exists).
        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            var phase = (BoatTripPhase)Phase.Value;
            GUI.Box(new Rect(20, 306, 260, 24), $"SANDAL: {phase} ({SeatedCount.Value} koltuklu)");
            if (IsSeated) GUI.Box(new Rect(20, 334, 260, 24), $"KOLTUK: {MySeatId.Value}" + (AmOwner.Value ? " (SEFER SORUMLUSU)" : ""));
            GUI.Box(new Rect(20, 362, 260, 24), "B: bin / G: in / O: yakin rotaya cik / R: donus iste");
        }
    }
}
