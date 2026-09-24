using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using DeepDive.Trip;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeepDive.Tests.P3A
{
    public class BoatNetworkRulesTests
    {
        private const string DiverPrefabPath = "Assets/DeepDive/Network/Prefabs/NetworkDiver.prefab";
        private const string BoatControllerPath = "Assets/DeepDive/Network/NetworkBoatController.cs";

        [Test]
        public void NearRouteLengthAndNominalDurationProduceExpectedSpeed()
        {
            var points = new List<Vector3>
            {
                new Vector3(9f, 8f, -0.4f),
                new Vector3(9f, 8f, 3.5f),
                new Vector3(9f, 8f, 8.5f)
            };

            Assert.AreEqual(8.9f, BoatRouteMath.PathLength(points), 0.0001f);
            Assert.AreEqual(1.1125f, BoatRouteMath.ResolveLegSpeed(points, 8f), 0.0001f);
        }

        [Test]
        public void InvalidNominalDurationFallsBackToConfiguredSpeed()
        {
            var points = new[] { Vector3.zero, Vector3.forward };
            Assert.AreEqual(4f, BoatRouteMath.ResolveLegSpeed(points, 0f, 4f), 0.0001f);
        }

        [Test]
        public void P3BoatHasExactlyFourStableSeatIds()
        {
            Assert.IsTrue(BoatSeatLayoutRules.IsValid());
            CollectionAssert.AreEqual(
                new[] { BoatTripIds.Seat0, BoatTripIds.Seat1, BoatTripIds.Seat2, BoatTripIds.Seat3 },
                BoatSeatLayoutRules.SeatIds);
            Assert.AreEqual(4, BoatSeatLayoutRules.LocalSeatOffsets.Count);
        }

        [Test]
        public void PhysicalInteractionSeamBindsAndUnbindsExactHandlers()
        {
            TransactionResult Board(PlayerId player, ulong requestId) => TransactionResult.Ok(requestId, 1);
            TransactionResult Exit(PlayerId player, ulong requestId) => TransactionResult.Ok(requestId, 2);

            BoatBoardingPhysicalInteraction.Bind(Board, Exit);
            try
            {
                Assert.IsTrue(BoatBoardingPhysicalInteraction.IsBound);
                Assert.IsTrue(BoatBoardingPhysicalInteraction.TryBoardNearest(new PlayerId(1), 10).Accepted);
                Assert.IsTrue(BoatBoardingPhysicalInteraction.TryDisembark(new PlayerId(1), 11).Accepted);
            }
            finally
            {
                BoatBoardingPhysicalInteraction.Unbind(Board, Exit);
            }

            Assert.IsFalse(BoatBoardingPhysicalInteraction.IsBound);
        }

        [Test]
        public void NetworkDiverPrefabCarriesBoatTripSync()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DiverPrefabPath);
            Assert.IsNotNull(prefab, "NetworkDiver prefab missing");
            Assert.IsNotNull(prefab.GetComponent<BoatTripPlayerSync>(),
                "BoatTripPlayerSync must be authored on the NGO player prefab before spawn");
        }

        [Test]
        public void BoatSeatAuthorityUsesNetworkPlayerTeleportInsteadOfRawTransformWrites()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(BoatControllerPath);
            Assert.IsNotNull(script, "NetworkBoatController source missing");
            StringAssert.Contains("player.Teleport(new Pose(worldPosition, transform.rotation))", script.text);
            StringAssert.Contains("player.Teleport(new Pose(exitPosition, transform.rotation))", script.text);
            StringAssert.DoesNotContain("player.transform.SetPositionAndRotation", script.text,
                "CharacterController can overwrite raw transform seat writes; use NetworkPlayer.Teleport");
        }

        [Test]
        public void BoardingRangeIncludesDockClearanceMargin()
        {
            var go = new GameObject("BoatControllerTest");
            try
            {
                var controller = go.AddComponent<NetworkBoatController>();
                var serialized = new SerializedObject(controller);
                var range = serialized.FindProperty("boardingRange");
                Assert.IsNotNull(range);
                Assert.AreEqual(2.75f, range.floatValue, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
