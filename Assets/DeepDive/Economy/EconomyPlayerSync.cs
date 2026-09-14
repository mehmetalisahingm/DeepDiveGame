using DeepDive.Core.Contracts;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Economy
{
    // Per-player economy mirror and the minimal real shop transport/UI for P3. The owner sees the
    // shared balance and next tube upgrade; purchase intent goes to the host, which is the only
    // process allowed to mutate EconomyManager.
    [RequireComponent(typeof(NetworkObject))]
    public sealed class EconomyPlayerSync : NetworkBehaviour
    {
        public readonly NetworkVariable<int> SharedBalance = new NetworkVariable<int>();
        public readonly NetworkVariable<ulong> LastRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<bool> LastAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastReasonCode = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<int> TubeLevel = new NetworkVariable<int>();
        public readonly NetworkVariable<int> NextTubePrice = new NetworkVariable<int>();
        public readonly NetworkVariable<FixedString64Bytes> NextTubeId = new NetworkVariable<FixedString64Bytes>();
        public readonly NetworkVariable<bool> ShopOpen = new NetworkVariable<bool>();

        private EconomyManager _economy;
        private ulong _observedRequestId;
        private ulong _localRequestSequence;
        private string _statusMessage = "";
        private float _statusUntil;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            _economy = FindFirstObjectByType<EconomyManager>();
            if (_economy == null) return;
            _economy.OnBalanceChanged += Refresh;
            _economy.OnLoadoutChanged += LoadoutChanged;
            Refresh();
        }

        public override void OnNetworkDespawn()
        {
            if (_economy != null)
            {
                _economy.OnBalanceChanged -= Refresh;
                _economy.OnLoadoutChanged -= LoadoutChanged;
            }
        }

        private void LoadoutChanged(PlayerId player)
        {
            if (player.Value == OwnerClientId) Refresh();
        }

        private void Refresh()
        {
            if (_economy == null || !IsServer) return;
            SharedBalance.Value = _economy.SharedBalance;
            RefreshUpgrade();
        }

        private void RefreshUpgrade()
        {
            var player = new PlayerId(OwnerClientId);
            if (_economy.TryGetNextTubeUpgrade(player, out var next))
            {
                NextTubeId.Value = new FixedString64Bytes(next.EquipmentId);
                NextTubePrice.Value = next.Price;
                TubeLevel.Value = Mathf.Max(0, next.Level - 1);
            }
            else
            {
                NextTubeId.Value = default;
                NextTubePrice.Value = 0;
                var level = 0;
                foreach (var equipmentId in _economy.LoadoutFor(player))
                    if (_economy.TryGetEquipmentDefinition(equipmentId, out var definition) &&
                        definition.Slot == "tube")
                        level = Mathf.Max(level, definition.Level);
                TubeLevel.Value = level;
            }
        }

        public void PublishPurchaseResult(TransactionResult result)
        {
            if (!IsServer || result.RequestId < LastRequestId.Value) return;
            LastReasonCode.Value = new FixedString32Bytes(result.ReasonCode);
            LastAccepted.Value = result.Accepted;
            LastRequestId.Value = result.RequestId;
            Refresh();
        }

        public void RequestNextTubeUpgradeLocal()
        {
            if (!IsSpawned || !IsOwner || !ShopOpen.Value || NextTubeId.Value.Length == 0) return;
            var requestId = ++_localRequestSequence;
            RequestPurchaseServerRpc(NextTubeId.Value, requestId);
        }

        [ServerRpc]
        private void RequestPurchaseServerRpc(FixedString64Bytes equipmentId, ulong requestId,
            ServerRpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
            if (_economy == null) _economy = FindFirstObjectByType<EconomyManager>();

            var result = _economy != null
                ? _economy.TryPurchase(new PlayerId(OwnerClientId), equipmentId.ToString(), requestId)
                : TransactionResult.Reject(requestId, "InvalidState", 0);
            PublishPurchaseResult(result);
        }

        private void Update()
        {
            if (IsServer && _economy != null)
            {
                if (ShopOpen.Value != _economy.PurchasesEnabled)
                    ShopOpen.Value = _economy.PurchasesEnabled;
                if (SharedBalance.Value != _economy.SharedBalance)
                    Refresh();
            }

            if (!IsSpawned || !IsOwner) return;
            if (ShopOpen.Value && Input.GetKeyDown(KeyCode.U)) RequestNextTubeUpgradeLocal();

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

            if (!ShopOpen.Value)
            {
                GUI.Box(new Rect(20, 222, 230, 24), "DUKKAN: DALISTA KAPALI");
                return;
            }

            if (NextTubeId.Value.Length == 0)
            {
                GUI.Box(new Rect(20, 222, 230, 24), $"TUP: MAX SEVIYE {TubeLevel.Value}");
                return;
            }

            if (GUI.Button(new Rect(20, 222, 230, 28),
                $"TUP +{TubeLevel.Value + 1}  {NextTubePrice.Value} KREDI  [U]"))
                RequestNextTubeUpgradeLocal();
        }
    }
}
