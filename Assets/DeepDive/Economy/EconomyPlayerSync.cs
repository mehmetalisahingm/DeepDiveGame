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

        // P3.2 town state, host-written and only informational for the client UI.
        public readonly NetworkVariable<int> PendingCatches = new NetworkVariable<int>();
        public readonly NetworkVariable<int> PendingRecordings = new NetworkVariable<int>();
        public readonly NetworkVariable<int> BoatPartsDone = new NetworkVariable<int>();
        public readonly NetworkVariable<int> BoatPartsMask = new NetworkVariable<int>();
        public readonly NetworkVariable<FixedString32Bytes> ActiveServiceId = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<ulong> LastServiceRequestId = new NetworkVariable<ulong>();
        public readonly NetworkVariable<byte> LastServiceType = new NetworkVariable<byte>();
        public readonly NetworkVariable<bool> LastServiceAccepted = new NetworkVariable<bool>();
        public readonly NetworkVariable<FixedString32Bytes> LastServiceReason = new NetworkVariable<FixedString32Bytes>();
        public readonly NetworkVariable<int> LastServiceAmount = new NetworkVariable<int>();
        public readonly NetworkVariable<int> LastServiceItems = new NetworkVariable<int>();

        private EconomyManager _economy;
        private ulong _observedRequestId;
        private ulong _observedServiceRequestId;
        private ulong _localRequestId;
        private string _statusMessage = "";
        private float _statusUntil;
        private int _observedBalance;

        public bool ShopOpen => ActiveServiceId.Value.ToString() == TownServiceCatalog.EquipmentShopId;

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

        public void PublishServiceOutcome(TownServiceOutcome outcome)
        {
            if (!IsServer) return;
            if (outcome.ServiceType == ServicePointType.EquipmentShop && outcome.ReasonCode == "ShopOpen")
            {
                SetActiveService(outcome.ServiceId);
                return;
            }
            LastServiceType.Value = (byte)outcome.ServiceType;
            LastServiceAccepted.Value = outcome.Accepted;
            LastServiceReason.Value = new FixedString32Bytes(outcome.ReasonCode);
            LastServiceAmount.Value = outcome.Amount;
            LastServiceItems.Value = outcome.ItemCount;
            // Written last so the owner sees a consistent outcome when it observes the id change.
            LastServiceRequestId.Value = outcome.RequestId;
        }

        public void SetActiveService(string serviceId)
        {
            if (!IsServer) return;
            var value = new FixedString32Bytes(serviceId ?? "");
            if (!ActiveServiceId.Value.Equals(value)) ActiveServiceId.Value = value;
        }

        public void PublishTownProgress(int pendingCatches, int pendingRecordings, int boatPartsDone, int boatPartsMask = 0)
        {
            if (!IsServer) return;
            if (PendingCatches.Value != pendingCatches) PendingCatches.Value = pendingCatches;
            if (PendingRecordings.Value != pendingRecordings) PendingRecordings.Value = pendingRecordings;
            if (BoatPartsDone.Value != boatPartsDone) BoatPartsDone.Value = boatPartsDone;
            if (BoatPartsMask.Value != boatPartsMask) BoatPartsMask.Value = boatPartsMask;
        }

        public void RequestPurchase(string itemId)
        {
            if (!IsSpawned || !IsOwner || string.IsNullOrWhiteSpace(itemId)) return;
            var id = ++_localRequestId;
            RequestPurchaseServerRpc(new FixedString32Bytes(itemId), id);
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

            if (SharedBalance.Value > _observedBalance)
            {
                _statusMessage = $"KAZANC: +{SharedBalance.Value - _observedBalance} KREDI";
                _statusUntil = Time.unscaledTime + 3f;
            }
            _observedBalance = SharedBalance.Value;

            ObserveServiceOutcome();

            if (LastRequestId.Value == 0 || LastRequestId.Value == _observedRequestId) return;
            _observedRequestId = LastRequestId.Value;
            _statusMessage = LastAccepted.Value ? "SATIN ALINDI" : FriendlyReason(LastReasonCode.Value.ToString());
            _statusUntil = Time.unscaledTime + 2f;
        }

        private void ObserveServiceOutcome()
        {
            if (LastServiceRequestId.Value == 0 || LastServiceRequestId.Value == _observedServiceRequestId) return;
            _observedServiceRequestId = LastServiceRequestId.Value;

            if (!LastServiceAccepted.Value)
                _statusMessage = FriendlyReason(LastServiceReason.Value.ToString());
            else if ((ServicePointType)LastServiceType.Value == ServicePointType.FishBuyer)
                _statusMessage = $"AV SATILDI: +{LastServiceAmount.Value} KREDI ({LastServiceItems.Value} AV)";
            else if ((ServicePointType)LastServiceType.Value == ServicePointType.RecordingBuyer)
                _statusMessage = $"KAYIT TESLIM: +{LastServiceAmount.Value} KREDI ({LastServiceItems.Value} KAYIT)";
            else return;
            _statusUntil = Time.unscaledTime + 3f;
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
            "NotAtShop" => "DUKKANA YAKIN DEGILSIN",
            "NothingToTurnIn" => "TESLIM EDILECEK URUN YOK",
            _ => string.IsNullOrWhiteSpace(reason) ? "ISLEM REDDEDILDI" : reason
        };

        private void OnGUI()
        {
            if (!IsSpawned || !IsOwner) return;
            GUI.Box(new Rect(20, 166, 230, 24), $"PARA: {SharedBalance.Value}");
            GUI.Box(new Rect(20, 194, 230, 24),
                $"BEKLEYEN: {PendingCatches.Value} AV / {PendingRecordings.Value} KAYIT");
            GUI.Box(new Rect(20, 222, 230, 24), $"SANDAL ONARIM: {BoatPartsDone.Value}/{BoatRepairParts.All.Count}");
            GUI.Box(new Rect(20, 250, 230, 24), "F: NPC ILE ETKILESIM");

            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_statusMessage))
                GUI.Box(new Rect(20, 278, 260, 24), _statusMessage);

            if (!ShopOpen) return;
            GUI.Box(new Rect(270, 20, 290, 296), "EKIPMAN DUKKANI");
            ShopRow(50, "Tup I  | +30 sn | 100", "TUP I AL", "tube-1");
            ShopRow(88, "Tup II | +60 sn | 250", "TUP II AL", "tube-2");
            ShopRow(126, "Temel kamera | 150", "KAMERA AL", EconomyManager.CameraBasicId);
            ShopRow(164, "Sandal govdesi | 120", "GOVDE AL", BoatRepairParts.Hull);
            ShopRow(202, "Sandal motoru | 120", "MOTOR AL", BoatRepairParts.Engine);
            ShopRow(240, "Sandal yakit deposu | 120", "DEPO AL", BoatRepairParts.FuelTank);
        }

        private void ShopRow(float y, string label, string button, string itemId)
        {
            GUI.Label(new Rect(284, y, 260, 18), label);
            if (GUI.Button(new Rect(284, y + 18, 260, 18), button)) RequestPurchase(itemId);
        }
    }
}
