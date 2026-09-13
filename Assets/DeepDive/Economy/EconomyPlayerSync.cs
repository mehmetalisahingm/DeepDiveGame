using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Economy
{
    // Basic UI feedback for sale/shop results (docs/plan/CONTRACTS.md "Alan arayuzleri | ...
    // Mert envanter/dukkan/oturum ekrani", and issue #36's "Satis, alisveris ve odul
    // sonuclarini anlasilir temel UI geri bildirimiyle goster"). EconomyManager only exists on
    // the host; this mirrors the shared balance onto a NetworkVariable the same way
    // InventoryPlayerSync mirrors bag state, so every client sees the live crew-wide total
    // regardless of who is host.
    //
    // PublishPurchaseResult exists as the interface for whoever wires the real client purchase
    // request (no shop-request UI/transport exists yet - this only demonstrates and tests the
    // feedback path itself, driven directly from EconomyManager.TryPurchase's return value).
    [RequireComponent(typeof(NetworkObject))]
    public sealed class EconomyPlayerSync : NetworkBehaviour
    {
        public readonly NetworkVariable<int> SharedBalance = new NetworkVariable<int>();
        public readonly NetworkVariable<ulong> LastRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<bool> LastAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastReasonCode = new NetworkVariable<FixedString32Bytes>();

        private EconomyManager _economy;
        private ulong _observedRequestId;
        private string _statusMessage = "";
        private float _statusUntil;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _economy = FindFirstObjectByType<EconomyManager>();
            if (_economy == null) return;
            _economy.OnBalanceChanged += Refresh;
            Refresh();
        }

        public override void OnNetworkDespawn()
        {
            if (_economy != null) _economy.OnBalanceChanged -= Refresh;
        }

        private void Refresh() => SharedBalance.Value = _economy.SharedBalance;

        // Called by host-side composition right after EconomyManager.TryPurchase, so the
        // requesting player's own client sees the outcome (mirrors NetworkPlayer.PublishAction).
        // Ignores a stale/out-of-order replay just like PublishAction does.
        public void PublishPurchaseResult(TransactionResult result)
        {
            if (!IsServer || result.RequestId < LastRequestId.Value) return;
            LastReasonCode.Value = new FixedString32Bytes(result.ReasonCode);
            LastAccepted.Value = result.Accepted;
            LastRequestId.Value = result.RequestId;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            if (LastRequestId.Value == 0 || LastRequestId.Value == _observedRequestId) return;
            _observedRequestId = LastRequestId.Value;
            _statusMessage = LastAccepted.Value ? "SATIN ALINDI" : LastReasonCode.Value.ToString();
            _statusUntil = Time.unscaledTime + 1.5f;
        }

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            GUI.Box(new Rect(20, 166, 230, 24), $"PARA: {SharedBalance.Value}");
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_statusMessage))
                GUI.Box(new Rect(20, 194, 230, 24), _statusMessage);
        }
    }
}
