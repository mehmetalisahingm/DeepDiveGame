using DeepDive.Core.Contracts;
using DeepDive.Network;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Trip
{
    // Per-player network mirror of the shared BoatTripState. Trip rules remain in BoatTripManager;
    // this class only carries owner input plus presentation data to guests.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class BoatTripPlayerSync : NetworkBehaviour
    {
        [Header("P3 Boat Presentation")]
        [SerializeField] private Material boatMaterial;

        public readonly NetworkVariable<FixedString32Bytes> TripId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<FixedString32Bytes> RouteId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<byte> Phase = new NetworkVariable<byte>();
        public readonly NetworkVariable<FixedString32Bytes> MySeatId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<bool> AmOwner = new NetworkVariable<bool>();
        public readonly NetworkVariable<int> SeatedCount = new NetworkVariable<int>();

        public readonly NetworkVariable<Vector3> BoatWorldPosition = new NetworkVariable<Vector3>();
        public readonly NetworkVariable<float> BoatYaw = new NetworkVariable<float>();
        public readonly NetworkVariable<bool> BoatVisible = new NetworkVariable<bool>();

        public readonly NetworkVariable<ulong> LastRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<bool> LastAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastReasonCode = new NetworkVariable<FixedString32Bytes>();

        public readonly NetworkVariable<byte> HullKind = new NetworkVariable<byte>();
        // Host-derived, persistent-day weather mirror. UI cannot choose or write this.
        public readonly NetworkVariable<bool> WindyDay = new NetworkVariable<bool>();

        private BoatHullPresentation hullPresentation;
        private ulong localRequestId;
        private GameObject boatVisual;
        private Material runtimeBoatMaterial;

        public bool IsSeated => MySeatId.Value.Length > 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner) EnsureBoatVisual();
        }

        public override void OnNetworkDespawn()
        {
            if (boatVisual != null) Destroy(boatVisual);
            if (runtimeBoatMaterial != null) Destroy(runtimeBoatMaterial);
            boatVisual = null;
            runtimeBoatMaterial = null;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;

            EnsureBoatVisual();
            UpdateBoatVisual();

            if (Input.GetKeyDown(KeyCode.B)) RequestBoardNearestLocal();
            if (Input.GetKeyDown(KeyCode.G)) RequestDisembarkLocal();
            if (Input.GetKeyDown(KeyCode.O)) RequestStartRouteLocal(BoatTripIds.NearRouteId);
            if (Input.GetKeyDown(KeyCode.R)) RequestReturnLocal();
        }

        public void PublishTripState(BoatTripState state, PlayerId self)
        {
            if (!IsServer) return;

            TripId.Value = new FixedString32Bytes(state.TripId);
            RouteId.Value = new FixedString32Bytes(state.RouteId);
            Phase.Value = (byte)state.Phase;
            SeatedCount.Value = state.Seats != null ? state.Seats.Count : 0;
            AmOwner.Value = state.HasOwner && state.Owner.Equals(self);

            var seat = string.Empty;
            if (state.Seats != null)
            {
                foreach (var assignment in state.Seats)
                {
                    if (!assignment.Player.Equals(self)) continue;
                    seat = assignment.SeatId;
                    break;
                }
            }
            MySeatId.Value = new FixedString32Bytes(seat);
        }

        public void PublishBoatPose(Vector3 position, Quaternion rotation, bool visible, BoatHullKind hull = BoatHullKind.Rowboat)
        {
            if (!IsServer) return;
            BoatWorldPosition.Value = position;
            BoatYaw.Value = rotation.eulerAngles.y;
            BoatVisible.Value = visible;
            HullKind.Value = (byte)hull;
        }

        public void PublishWeather(bool windy)
        {
            if (IsServer) WindyDay.Value = windy;
        }

        public void PublishResult(TransactionResult result)
        {
            if (!IsServer || result.RequestId < LastRequestId.Value) return;
            LastReasonCode.Value = new FixedString32Bytes(result.ReasonCode);
            LastAccepted.Value = result.Accepted;
            LastRequestId.Value = result.RequestId;
        }

        // Compatibility entry point for any old caller that still passes a seat id. The client is
        // no longer allowed to choose the authoritative seat; the host resolves the nearest free one.
        public void RequestBoardLocal(string ignoredSeatId) => RequestBoardNearestLocal();

        public void RequestBoardNearestLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestBoardServerRpc(++localRequestId);
        }

        public void RequestDisembarkLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestDisembarkServerRpc(++localRequestId);
        }

        public void RequestStartRouteLocal(string routeId)
        {
            if (!IsSpawned || !IsOwner) return;
            RequestStartRouteServerRpc(new FixedString32Bytes(routeId), ++localRequestId);
        }

        public void RequestReturnLocal()
        {
            if (!IsSpawned || !IsOwner) return;
            RequestReturnServerRpc(++localRequestId);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestBoardServerRpc(ulong requestId, ServerRpcParams rpc = default)
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
            if (manager == null)
            {
                PublishResult(TransactionResult.Reject(requestId, "InvalidState", 0));
                return;
            }
            PublishResult(manager.TryStartRoute(new PlayerId(OwnerClientId), routeId.ToString(), requestId));
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestReturnServerRpc(ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var manager = FindFirstObjectByType<BoatTripManager>();
            if (manager == null)
            {
                PublishResult(TransactionResult.Reject(requestId, "InvalidState", 0));
                return;
            }
            PublishResult(manager.TryRequestReturn(new PlayerId(OwnerClientId), requestId));
        }

        private void EnsureBoatVisual()
        {
            if (!IsOwner || boatVisual != null) return;

            boatVisual = new GameObject("P3BoatVisual");
            hullPresentation = boatVisual.AddComponent<BoatHullPresentation>();
            hullPresentation.Initialize(ResolveBoatMaterial());
            hullPresentation.ApplyHullPresentation((BoatHullKind)HullKind.Value);
        }

        private Material ResolveBoatMaterial()
        {
            if (boatMaterial != null) return boatMaterial;
            if (runtimeBoatMaterial != null) return runtimeBoatMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            runtimeBoatMaterial = new Material(shader) { name = "P3BoatRuntimeMaterial" };
            return runtimeBoatMaterial;
        }

        private void UpdateBoatVisual()
        {
            if (boatVisual == null) return;
            hullPresentation.ApplyHullPresentation((BoatHullKind)HullKind.Value);
            boatVisual.SetActive(BoatVisible.Value);
            if (!BoatVisible.Value) return;
            boatVisual.transform.SetPositionAndRotation(
                BoatWorldPosition.Value,
                Quaternion.Euler(0f, BoatYaw.Value, 0f));
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            var phase = (BoatTripPhase)Phase.Value;
            GUI.Box(new Rect(20, 306, 300, 24), $"SANDAL: {phase} ({SeatedCount.Value}/4)");
            if (IsSeated)
                GUI.Box(new Rect(20, 334, 300, 24), $"KOLTUK: {MySeatId.Value}" + (AmOwner.Value ? " (SEFER SORUMLUSU)" : string.Empty));
            GUI.Box(new Rect(20, 362, 300, 24), "B: bin | G: in | O: rotaya cik | R: donus");
            if (LastRequestId.Value > 0 && !LastAccepted.Value)
                GUI.Box(new Rect(20, 390, 300, 24), $"SANDAL RED: {LastReasonCode.Value}");
            if (WindyDay.Value && phase == BoatTripPhase.Docked)
                GUI.Box(new Rect(20, 418, 440, 24), "RUZGAR: DERIN ROTA KAPALI - DONUS HER ZAMAN ACIK");
        }
    }
}
