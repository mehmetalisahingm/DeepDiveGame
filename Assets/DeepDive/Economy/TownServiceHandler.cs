using System;
using System.Collections.Generic;
using DeepDive.Core.Contracts;

namespace DeepDive.Economy
{
    // Executes the concrete P3.2 NPC actions once the interaction layer (Mehmet) has already validated
    // range, line of sight and owner intent. It adds the economy-side gates: town phase, active player,
    // catalog identity, and "the shop is only usable while you are at the shop".
    //
    // Pure C# (no NetworkBehaviour) so the rules are testable without a network session; Composition
    // supplies the environment as delegates.
    public sealed class TownServiceHandler
    {
        private readonly EconomyManager economy;
        private readonly Func<bool> townOpen;
        private readonly Func<PlayerId, bool> canServe;
        private readonly Func<PlayerId, ServicePointDefinition, bool> inRange;
        private readonly Dictionary<PlayerId, ServicePointDefinition> shopSessions =
            new Dictionary<PlayerId, ServicePointDefinition>();

        public event Action<PlayerId, TownServiceOutcome> OutcomeReady;
        public event Action<PlayerId> ShopClosed;

        public TownServiceHandler(EconomyManager economy, Func<bool> townOpen, Func<PlayerId, bool> canServe,
            Func<PlayerId, ServicePointDefinition, bool> inRange)
        {
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.townOpen = townOpen ?? throw new ArgumentNullException(nameof(townOpen));
            this.canServe = canServe ?? throw new ArgumentNullException(nameof(canServe));
            this.inRange = inRange ?? throw new ArgumentNullException(nameof(inRange));
        }

        public bool IsShopOpenFor(PlayerId player) => shopSessions.ContainsKey(player);

        public bool TryGetShop(PlayerId player, out ServicePointDefinition definition) =>
            shopSessions.TryGetValue(player, out definition);

        public PlayerActionResult HandleInteraction(PlayerId player, ServicePointDefinition service, ulong requestId)
        {
            if (!TownServiceCatalog.Matches(service)) return PlayerActionResult.InvalidTarget;
            if (!townOpen() || !canServe(player)) return PlayerActionResult.InvalidState;

            switch (service.ServiceType)
            {
                case ServicePointType.FishBuyer:
                    return Report(player, service, requestId, economy.TrySellCatches(player, requestId));
                case ServicePointType.RecordingBuyer:
                    return Report(player, service, requestId, economy.TryTurnInRecordings(player, requestId));
                case ServicePointType.EquipmentShop:
                    shopSessions[player] = service;
                    Publish(player, new TownServiceOutcome(service.ServiceId, service.ServiceType, requestId,
                        true, "ShopOpen", 0, 0));
                    return PlayerActionResult.Accepted;
                default:
                    return PlayerActionResult.InvalidTarget;
            }
        }

        // Purchase RPC entry. Equipment and boat parts are both sold by the equipment shop, so both need
        // an open shop session AND the player must still be in range of that shop.
        public TransactionResult HandlePurchase(PlayerId player, string itemId, ulong requestId)
        {
            if (!townOpen()) return TransactionResult.Reject(requestId, "WrongPhase", economy.Revision);
            if (!canServe(player)) return TransactionResult.Reject(requestId, "PlayerInactive", economy.Revision);
            if (!shopSessions.TryGetValue(player, out var shop) || !inRange(player, shop))
            {
                CloseShop(player);
                return TransactionResult.Reject(requestId, "NotAtShop", economy.Revision);
            }

            var result = BoatRepairParts.IsPart(itemId)
                ? economy.TryContributeBoatPart(player, itemId, BoatPartSource.Purchased, requestId)
                : economy.TryPurchase(player, itemId, requestId);

            Publish(player, new TownServiceOutcome(shop.ServiceId, shop.ServiceType, requestId, result.Accepted,
                result.ReasonCode, 0, 0));
            return result;
        }

        // Free world part (Utku's anchors). Host-only, same progress state as the bought alternative.
        public TransactionResult HandleFoundPart(PlayerId player, string partId, ulong requestId)
        {
            if (!canServe(player)) return TransactionResult.Reject(requestId, "PlayerInactive", economy.Revision);
            return economy.TryContributeBoatPart(player, partId, BoatPartSource.Found, requestId);
        }

        // Called every host frame/tick: closes shops whose player walked away or when the dive starts.
        public void Tick()
        {
            if (shopSessions.Count == 0) return;
            List<PlayerId> closed = null;
            var open = townOpen();
            foreach (var pair in shopSessions)
                if (!open || !canServe(pair.Key) || !inRange(pair.Key, pair.Value))
                    (closed ??= new List<PlayerId>()).Add(pair.Key);
            if (closed == null) return;
            foreach (var player in closed) CloseShop(player);
        }

        public void CloseShop(PlayerId player)
        {
            if (shopSessions.Remove(player)) ShopClosed?.Invoke(player);
        }

        public void CloseAll()
        {
            foreach (var player in new List<PlayerId>(shopSessions.Keys)) CloseShop(player);
        }

        private PlayerActionResult Report(PlayerId player, ServicePointDefinition service, ulong requestId,
            TurnInResult result)
        {
            Publish(player, new TownServiceOutcome(service.ServiceId, service.ServiceType, requestId,
                result.Accepted, result.ReasonCode, result.Earned, result.ItemCount));
            return result.Accepted ? PlayerActionResult.Accepted : PlayerActionResult.Rejected;
        }

        private void Publish(PlayerId player, TownServiceOutcome outcome) => OutcomeReady?.Invoke(player, outcome);
    }
}
