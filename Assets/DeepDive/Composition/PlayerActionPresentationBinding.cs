using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.World;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DeepDive.Composition
{
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerActionPresentationBinding : MonoBehaviour
    {
        public const string MessageName="DeepDive.P49.VisualAction.v1";
        private NetworkManager manager;
        private CustomMessagingManager messages;
        private PlayerActionReceipt receipt=new PlayerActionReceipt();
        private ServicePresentationReceipt serviceRequests=new ServicePresentationReceipt();
        private ulong sequence;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var root=new GameObject("PlayerActionPresentation");DontDestroyOnLoad(root);
            root.AddComponent<PlayerActionPresentationBinding>();
        }
        private void OnEnable()
        {
            CatchObject.CollectedForPresentation+=Collected;
            EconomyPlayerSync.ServiceOutcomeForPresentation+=Delivered;
        }
        private void OnDisable()
        {
            CatchObject.CollectedForPresentation-=Collected;
            EconomyPlayerSync.ServiceOutcomeForPresentation-=Delivered;
            if(messages!=null)messages.UnregisterNamedMessageHandler(MessageName);
            messages=null;manager=null;
        }
        private void Update()
        {
            var current=NetworkManager.Singleton;
            if(current!=manager || current?.CustomMessagingManager!=messages)
            {
                if(messages!=null)messages.UnregisterNamedMessageHandler(MessageName);
                manager=current;messages=manager!=null?manager.CustomMessagingManager:null;
                receipt=new PlayerActionReceipt();serviceRequests=new ServicePresentationReceipt();sequence=0;
                messages?.RegisterNamedMessageHandler(MessageName,Receive);
            }
            if(manager==null || !manager.IsListening || manager.SpawnManager==null)return;
            foreach(var obj in manager.SpawnManager.SpawnedObjectsList)
            {
                var player=obj.GetComponent<NetworkPlayer>();
                if(player!=null && player.IsSpawned && player.GetComponent<PlayerActionView>()==null)
                    player.gameObject.AddComponent<PlayerActionView>();
            }
        }
        private void Collected(CatchObject fish,PlayerId player,CaptureResult capture)
        {
            if(manager==null || !manager.IsServer)return;
            foreach(var obj in manager.SpawnManager.SpawnedObjectsList)
            {
                var actor=obj.GetComponent<NetworkPlayer>();
                if(actor==null || actor.OwnerClientId!=player.Value)continue;
                Publish(actor,PlayerVisualAction.Collect,fish.transform.position,capture.SpeciesId,1);break;
            }
        }
        private void Delivered(EconomyPlayerSync actor,TownServiceOutcome outcome)
        {
            if(manager==null || !manager.IsServer || !serviceRequests.Accept(actor.NetworkObjectId,outcome))return;
            var player=actor.GetComponent<NetworkPlayer>();if(player==null)return;
            var source=player.transform.position+player.transform.forward*.75f+Vector3.up*.9f;
            foreach(var anchor in FindObjectsByType<ServicePointAnchor>(FindObjectsSortMode.None))
                if(anchor.Definition.ServiceId==outcome.ServiceId)
                {source=anchor.WorldPosition+anchor.transform.forward*.65f+Vector3.up*1.24f;break;}
            Publish(player,PlayerVisualAction.Deliver,source,"",outcome.ItemCount);
        }
        private void Publish(NetworkPlayer player,PlayerVisualAction kind,Vector3 source,string species,int count)
        {
            var message=new PlayerActionMessage{PlayerObject=player.NetworkObjectId,Sequence=++sequence,
                At=manager.ServerTime.Time,Kind=kind,Source=source,Species=new FixedString64Bytes(species??""),Count=count};
            Show(NetworkManager.ServerClientId,message);
            if(messages==null)return;
            using(var writer=new FastBufferWriter(PlayerActionMessage.MaxBytes,Allocator.Temp))
            {
                var writable=writer;message.Write(ref writable);
                foreach(var client in manager.ConnectedClientsIds)
                    if(client!=NetworkManager.ServerClientId)messages.SendNamedMessage(MessageName,client,writer,NetworkDelivery.ReliableSequenced);
            }
        }
        private void Receive(ulong sender,FastBufferReader reader)
        {
            if(sender!=NetworkManager.ServerClientId || manager==null || manager.IsServer)return;
            if(PlayerActionMessage.Read(ref reader,out var message))Show(sender,message);
        }
        private void Show(ulong sender,PlayerActionMessage message)
        {
            if(!manager.SpawnManager.SpawnedObjects.TryGetValue(message.PlayerObject,out var obj) ||
                !receipt.Accept(sender,message,manager.ServerTime.Time))return;
            var player=obj.GetComponent<NetworkPlayer>();if(player==null)return;
            var view=player.GetComponent<PlayerActionView>()??player.gameObject.AddComponent<PlayerActionView>();
            view.Play(message);
        }
    }
}
