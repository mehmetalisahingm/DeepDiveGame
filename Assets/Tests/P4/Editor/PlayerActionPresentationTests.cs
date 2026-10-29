using DeepDive.Composition;
using DeepDive.Core.Contracts;
using DeepDive.Network;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.P4.Tests
{
    public sealed class PlayerActionPresentationTests
    {
        private static PlayerActionMessage Accepted()=>new PlayerActionMessage{PlayerObject=18,Sequence=1,At=10,
            Kind=PlayerVisualAction.Collect,Source=new Vector3(2,3,4),Species=new FixedString64Bytes("tuna"),Count=1};
        [Test] public void ClientCannotForgeSuccessAndReplayDoesNotConsumeSequence()
        {
            var gate=new PlayerActionReceipt();var message=Accepted();
            Assert.That(gate.Accept(9,message,10.1),Is.False);
            Assert.That(gate.Accept(NetworkManager.ServerClientId,message,10.1),Is.True);
            Assert.That(gate.Accept(NetworkManager.ServerClientId,message,10.1),Is.False);
            message.Sequence=2;Assert.That(gate.Accept(NetworkManager.ServerClientId,message,10.2),Is.True);
        }
        [TestCase(8.0)] [TestCase(12.0)] [TestCase(double.NaN)]
        public void ExpiredFutureAndNonFiniteActionsAreNotPlayed(double at)
        {var message=Accepted();message.At=at;Assert.That(new PlayerActionReceipt().Accept(0,message,10),Is.False);}
        [Test] public void InvalidPayloadDoesNotPoisonNextValidAction()
        {
            var gate=new PlayerActionReceipt();var message=Accepted();message.Source.x=float.NaN;
            Assert.That(gate.Accept(0,message,10),Is.False);Assert.That(gate.Accept(0,Accepted(),10),Is.True);
        }
        [Test] public void NewPlayerSequenceIsIndependentAndOldActionCannotReplaceNewer()
        {
            var gate=new PlayerActionReceipt();var message=Accepted();message.Sequence=6;
            Assert.That(gate.Accept(0,message,10),Is.True);Assert.That(gate.Accept(0,Accepted(),10),Is.False);
            message=Accepted();message.PlayerObject=19;Assert.That(gate.Accept(0,message,10),Is.True);
        }
        [Test] public void RefusedEmptyAndReplayedOldSaleNeverShowAnotherCrate()
        {
            var gate=new ServicePresentationReceipt();
            var rejected=new TownServiceOutcome("buyer-fish",ServicePointType.FishBuyer,1,false,"SaveFailed",0,2);
            Assert.That(gate.Accept(7,rejected),Is.False);
            var empty=new TownServiceOutcome("buyer-fish",ServicePointType.FishBuyer,1,true,"NoCatches",0,0);
            Assert.That(gate.Accept(7,empty),Is.False);
            var sale=new TownServiceOutcome("buyer-fish",ServicePointType.FishBuyer,1,true,"Sold",120,1);
            Assert.That(gate.Accept(7,sale),Is.True);
            Assert.That(gate.Accept(7,new TownServiceOutcome("buyer-fish",ServicePointType.FishBuyer,2,true,"Sold",120,1)),Is.True);
            Assert.That(gate.Accept(7,sale),Is.False);Assert.That(gate.Accept(8,sale),Is.True);
        }
        [Test] public void EnvelopeRoundTripAndTruncationAndTrailingData()
        {
            using(var writer=new FastBufferWriter(160,Allocator.Temp))
            {
                var writable=writer;Accepted().Write(ref writable);
                using(var reader=new FastBufferReader(writer,Allocator.Temp))
                {
                    var readable=reader;Assert.That(PlayerActionMessage.Read(ref readable,out var result),Is.True);
                    Assert.That(result.PlayerObject,Is.EqualTo(18));Assert.That(result.Source,Is.EqualTo(new Vector3(2,3,4)));
                    Assert.That(result.Species.ToString(),Is.EqualTo("tuna"));Assert.That(result.Valid(10),Is.True);
                }
                writer.WriteValueSafe((byte)99);
                using(var reader=new FastBufferReader(writer,Allocator.Temp))
                {var readable=reader;Assert.That(PlayerActionMessage.Read(ref readable,out _),Is.False);}
            }
            using(var reader=new FastBufferReader(new byte[20],Allocator.Temp))
            {var readable=reader;Assert.That(PlayerActionMessage.Read(ref readable,out _),Is.False);}
        }
        [TestCase(PlayerActionView.BagResource)] [TestCase(PlayerActionView.CrateResource)]
        public void AuthoredPropsHaveTwoGripsAndNoGameplayComponents(string resource)
        {
            var prefab=Resources.Load<GameObject>(resource);Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.transform.Find("Grip_Left"),Is.Not.Null);Assert.That(prefab.transform.Find("Grip_Right"),Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<NetworkBehaviour>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Renderer>(true).Length,Is.InRange(1,6));
        }
        [TestCase("sea_bass")] [TestCase("tuna")] [TestCase("red_snapper")]
        public void CaughtVisualUsesActualSpeciesAndHasNoNetworkOrPhysics(string species)
        {
            var prefab=Resources.Load<GameObject>(PlayerActionView.FishResource(species));Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<NetworkObject>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(PlayerActionView.FishResource("unmodelled_species"),Is.Null);
        }
        [Test] public void BoatEyePresentationNeverMovesAuthoritativeRoot()
        {
            var root=new GameObject("Eye presentation test");
            try
            {
                var actor=root.AddComponent<NetworkPlayer>();var before=root.transform.position;
                actor.SetVisualBoatSeated(true);Assert.That(actor.VisualBoatSeated,Is.True);
                Assert.That(root.transform.position,Is.EqualTo(before));Assert.That(actor.Seated.Value,Is.False);
                actor.SetVisualBoatSeated(false);Assert.That(actor.VisualBoatSeated,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
