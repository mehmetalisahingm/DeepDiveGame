using DeepDive.Composition;
using DeepDive.Core.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    // #220: A host can shut down a room and create a fresh room using the
    // SAME NetworkManager. NetworkObjectId and request IDs may restart.
    public sealed class PlayerActionSessionBoundaryTests
    {
        [TestCase(false, false, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, true)]
        public void OnlyTransportChangesOrRoomListeningTransitionsResetReceipts(
            bool transportChanged, bool wasListening, bool listening, bool expected)
        {
            Assert.AreEqual(expected, PlayerActionSessionRules.ShouldResetReceipts(
                transportChanged, wasListening, listening));
        }

        [Test]
        public void RehostAcceptsFirstRealCatchAndSaleAfterReusedObjectAndRequestIds()
        {
            var catchMessage = new PlayerActionMessage
            {
                PlayerObject = 42,
                Sequence = 1,
                At = 10,
                Kind = PlayerVisualAction.Collect,
                Species = new FixedString64Bytes("tuna"),
                Source = new Vector3(1, 2, 3),
                Count = 1
            };
            var sold = new TownServiceOutcome("buyer-fish", ServicePointType.FishBuyer,
                1, true, "Sold", 120, 1);
            var oldActions = new PlayerActionReceipt();
            var oldSales = new ServicePresentationReceipt();
            Assert.IsTrue(oldActions.Accept(NetworkManager.ServerClientId, catchMessage, 10));
            Assert.IsTrue(oldSales.Accept(42, sold));
            Assert.IsFalse(oldActions.Accept(NetworkManager.ServerClientId, catchMessage, 10));
            Assert.IsFalse(oldSales.Accept(42, sold));

            // A stopped host restarts within the same process. Fresh receipts
            // must be used even when the network dispatcher instance is unchanged.
            Assert.IsTrue(PlayerActionSessionRules.ShouldResetReceipts(false, true, false));
            Assert.IsTrue(PlayerActionSessionRules.ShouldResetReceipts(false, false, true));
            var newActions = new PlayerActionReceipt();
            var newSales = new ServicePresentationReceipt();
            Assert.IsTrue(newActions.Accept(NetworkManager.ServerClientId, catchMessage, 10));
            Assert.IsTrue(newSales.Accept(42, sold));
            Assert.IsFalse(newActions.Accept(NetworkManager.ServerClientId, catchMessage, 10));
            Assert.IsFalse(newSales.Accept(42, sold));
        }
    }
}
