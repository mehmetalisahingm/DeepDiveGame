using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Economy
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class EconomyPlayerSync : NetworkBehaviour
    {
        public readonly NetworkVariable<int> SharedBalance = new NetworkVariable<int>();
        public readonly NetworkVariable<ulong> LastRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<bool> LastAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastReasonCode = new NetworkVariable<FixedString32Bytes>();

        private EconomyManager _economy;
        private ulong _observedRequestId;
        private ulong _localRequestId;
        private string _statusMessage = "";
        private float _statusUntil;
        private bool _shopOpen;

        public override void OnNetworkSpawn() => EnsureEconomy();

        public override void OnNetworkDespawn()
        {
            if (_economy != null) _economy.OnBalanceChanged -= Refresh;
            _economy = null;
        }

        private void EnsureEconomy()
        {
            if (!IsServer || _economy != null) return;
            _economy = FindFirstObjectByType<EconomyManager>();
            if (_economy == null) return;
            _economy.OnBalanceChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (IsServer && _economy != null) SharedBalance.Value = _economy.SharedBalance;
        }

        public void PublishPurchaseResult(TransactionResult result)
        {
            if (!IsServer || result.RequestId < LastRequestId.Value) return;
            LastReasonCode.Value = new FixedString32Bytes(result.ReasonCode);
            LastAccepted.Value = result.Accepted;
            LastRequestId.Value = result.RequestId;
        }

        public void RequestPurchase(string equipmentId)
        {
            if (!IsSpawned || !IsOwner || string.IsNullOrWhiteSpace(equipmentId)) return;
            var id = ++_localRequestId;
            RequestPurchaseServerRpc(new FixedString32Bytes(equipmentId), id);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestPurchaseServerRpc(FixedString32Bytes equipmentId, ulong requestId, ServerRpcParams rpc = default)
        {
            if (!IsServer || rpc.Receive.SenderClientId != OwnerClientId || requestId == 0) return;
            var result = EconomyPurchaseAuthority.TryPurchase(new PlayerId(OwnerClientId), equipmentId.ToString(), requestId);
            PublishPurchaseResult(result);
        }

        private void Update()
        {
            EnsureEconomy();
            if (!IsSpawned || !IsOwner) return;

            if (Input.GetKeyDown(KeyCode.B)) _shopOpen = !_shopOpen;

            if (LastRequestId.Value == 0 || LastRequestId.Value == _observedRequestId) return;
            _observedRequestId = LastRequestId.Value;
            _statusMessage = LastAccepted.Value ? "SATIN ALINDI" : FriendlyReason(LastReasonCode.Value.ToString());
            _statusUntil = Time.unscaledTime + 2f;
        }

        private static string FriendlyReason(string reason) => reason switch
        {
            "InsufficientFunds" => "YETERSIZ PARA",
            "AlreadyProcessed" => "ZATEN SAHIPSIN",
            "WrongPhase" => "DALISTA ALISVERIS YOK",
            "PlayerInactive" => "OYUNCU AKTIF DEGIL",
            "InvalidState" => "DUKKAN HAZIR DEGIL",
            "InvalidTarget" => "GECERSIZ URUN",
            "SaveFailed" => "KAYIT HATASI",
            _ => string.IsNullOrWhiteSpace(reason) ? "ISLEM REDDEDILDI" : reason
        };

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            GUI.Box(new Rect(20, 166, 230, 24), $"PARA: {SharedBalance.Value}");
            GUI.Box(new Rect(20, 194, 230, 24), "B: DUKKAN");

            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_statusMessage))
                GUI.Box(new Rect(20, 222, 230, 24), _statusMessage);

            if (!_shopOpen) return;
            GUI.Box(new Rect(270, 20, 250, 136), "DALIS EKIPMANI");
            GUI.Label(new Rect(284, 50, 220, 20), "Tup I  | +30 sn | 100 kredi");
            if (GUI.Button(new Rect(284, 72, 220, 28), "TUP I SATIN AL")) RequestPurchase("tube-1");
            GUI.Label(new Rect(284, 104, 220, 20), "Tup II | +60 sn | 250 kredi");
            if (GUI.Button(new Rect(284, 126, 220, 28), "TUP II SATIN AL")) RequestPurchase("tube-2");
        }
    }
}