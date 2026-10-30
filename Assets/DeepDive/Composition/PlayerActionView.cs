using DeepDive.Network;
using DeepDive.Trip;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Composition
{
    [DisallowMultipleComponent,DefaultExecutionOrder(350)]
    public sealed class PlayerActionView : MonoBehaviour
    {
        public const string BagResource="PlayerActions/CatchSatchel",CrateResource="PlayerActions/FishCrate";
        private NetworkPlayer player;
        private PlayerPresentationView hands;
        private BoatTripPlayerSync trip;
        private GameObject bag,crate,fish;
        private Transform bagLeft,bagRight,crateLeft,crateRight;
        private PlayerActionMessage action;
        private double until;
        private LineRenderer lanyard;
        private Material cord;
        private bool seated;
        private int seatTransition;
        private double seatAt;
        private HeldEquipmentMode equipmentAtStart;
        private ulong requestAtStart;
        public int CollectCount{get;private set;}
        public int DeliveryCount{get;private set;}
        public int BoardCount{get;private set;}
        public int ExitCount{get;private set;}
        public PlayerVisualAction ActiveKind{get;private set;}
        public bool AuthoredProps=>bag!=null&&crate!=null;
        public float ActionAge{get;private set;}
        public Vector3 ActionFocus=>ActiveKind==PlayerVisualAction.Deliver && crate!=null ? crate.transform.position :
            ActiveKind==PlayerVisualAction.Collect && bag!=null ? bag.transform.position+Vector3.up*.3f : Point(Vector3.up*.85f);
        private double Now=>NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening?NetworkManager.Singleton.ServerTime.Time:Time.unscaledTimeAsDouble;
        private void Awake()
        {
            player=GetComponent<NetworkPlayer>();hands=GetComponent<PlayerPresentationView>();trip=GetComponent<BoatTripPlayerSync>();
            bag=Spawn(BagResource);crate=Spawn(CrateResource);
            bagLeft=Marker(bag,"Grip_Left");bagRight=Marker(bag,"Grip_Right");
            crateLeft=Marker(crate,"Grip_Left");crateRight=Marker(crate,"Grip_Right");
            if(crate!=null)crate.SetActive(false);
            if(trip!=null)
            {
                seated=trip.IsSeated;player.SetVisualBoatSeated(seated);
                trip.MySeatId.OnValueChanged+=SeatChanged;
            }
        }
        private void SeatChanged(FixedString32Bytes before,FixedString32Bytes after)
        {
            seated=after.Length>0;player.SetVisualBoatSeated(seated);
            if((before.Length>0)==seated)return;
            seatTransition=seated?1:-1;seatAt=Now;
            if(seated)BoardCount++;else ExitCount++;
        }
        public static string FishResource(string species)
        {
            switch(species)
            {case "sea_bass":return "SuppliedKit/CoastalFish";case "tuna":return "SuppliedKit/Tuna";
                case "red_snapper":return "SuppliedKit/RedSnapper";default:return null;}
        }
        public void Play(PlayerActionMessage value)
        {
            action=value;until=value.At+1.6;ClearFish();
            equipmentAtStart=player.CurrentHeldEquipment;requestAtStart=player.LastActionRequestId.Value;
            if(value.Kind==PlayerVisualAction.Collect)
            {
                CollectCount++;
                var resource=FishResource(value.Species.ToString());
                if(resource!=null)fish=Spawn(resource);
                if(fish!=null)
                {
                    foreach(var animation in fish.GetComponentsInChildren<Animation>(true))animation.enabled=false;
                    foreach(var animator in fish.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                    fish.transform.localScale*=.62f;
                }
            }
            else DeliveryCount++;
            Debug.Log($"P49_VISUAL_ACTION player={player.OwnerClientId} kind={value.Kind} sequence={value.Sequence} count={value.Count}");
        }
        private GameObject Spawn(string resource)
        {
            var prefab=Resources.Load<GameObject>(resource);if(prefab==null)return null;
            var obj=Instantiate(prefab,transform,false);obj.name="Action_"+prefab.name;
            foreach(var collider in obj.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            return obj;
        }
        private static Transform Marker(GameObject root,string name)
        {
            if(root==null)return null;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;
            return null;
        }
        private Vector3 Point(Vector3 local)=>transform.TransformPoint(local)+Vector3.up*(seated?NetworkPlayer.BoatSeatedVisualOffset:player.WaterViewLift);
        private void LateUpdate()
        {
            if(player==null || !player.IsSpawned){Hide();return;}
            var blocked=player.Passive.Value || player.RepairHandsBusy || player.RecordingPresentation.Value;
            blocked |= player.CurrentHeldEquipment!=equipmentAtStart && Now<until;
            // An accepted shot is free to interrupt this cosmetic recovery immediately.
            if(Now<until && player.LastActionRequestId.Value!=requestAtStart &&
                player.LastActionKind.Value==(byte)PlayerActionKind.Harpoon)blocked=true;
            ActiveKind=PlayerVisualAction.None;
            if(blocked)until=0;
            var active=!blocked && Now<until;
            var holdingBag=active && action.Kind==PlayerVisualAction.Collect;
            if(bag!=null)
            {
                bag.SetActive(!player.Passive.Value);
                bag.transform.SetPositionAndRotation(Point(holdingBag?new Vector3(0,.87f,.58f):new Vector3(-.36f,.75f,-.07f)),transform.rotation);
                foreach(var renderer in bag.GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode=player.IsOwner&&!holdingBag?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
            }
            if(crate!=null)crate.SetActive(active&&action.Kind==PlayerVisualAction.Deliver);
            if(!active)
            {
                ClearFish();
                if(!blocked&&seatTransition!=0&&Now-seatAt<.65)
                {
                    ActiveKind=seatTransition>0?PlayerVisualAction.Board:PlayerVisualAction.Disembark;
                    var age=Mathf.Clamp01((float)(Now-seatAt)/.65f);
                    ActionAge=age;
                    var lift=Mathf.Sin(age*Mathf.PI)*.12f;
                    var y=seated?.96f:1.15f;
                    hands?.ApplyActionGrips(Point(new Vector3(-.29f,y+lift,.32f)),Point(new Vector3(.29f,y+lift,.32f)),
                        transform.rotation*Quaternion.Euler(60,0,-20),transform.rotation*Quaternion.Euler(60,0,20));
                }
                return;
            }
            ActiveKind=action.Kind;ActionAge=Mathf.Clamp01((float)(Now-action.At)/1.6f);
            if(action.Kind==PlayerVisualAction.Deliver)Deliver();else Collect();
        }
        private void Collect()
        {
            if(bag==null || bagLeft==null || bagRight==null)return;
            var age=ActionAge;
            var held=Point(new Vector3(.10f,1.38f,.58f));
            var arrive=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.35f));
            var stow=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.48f)/.42f));
            // A constant-size fish is lowered through the open mouth into the opaque cloth sack.
            var fishPosition=Vector3.Lerp(Vector3.Lerp(action.Source,held,arrive),Point(new Vector3(0,.87f,.58f)),stow);
            if(fish!=null)
            {
                fish.transform.SetPositionAndRotation(fishPosition,Quaternion.Slerp(transform.rotation*Quaternion.Euler(0,90,10),
                    transform.rotation*Quaternion.Euler(-90,0,0),Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.25f)/.25f))));
                fish.SetActive(age<.92f);
            }
            var right=Vector3.Lerp(held+transform.right*.06f-Vector3.up*.025f,bagRight.position,stow);
            hands?.ApplyActionGrips(bagLeft.position,right,bagLeft.rotation,Quaternion.Slerp(transform.rotation*Quaternion.Euler(5,0,60),bagRight.rotation,stow));
            if(age<.32f&&fish!=null)
            {
                EnsureLanyard();lanyard.enabled=true;lanyard.SetPosition(0,right);lanyard.SetPosition(1,fishPosition);
            }
            else if(lanyard!=null)lanyard.enabled=false;
        }
        private void Deliver()
        {
            if(crate==null||crateLeft==null||crateRight==null)return;
            var raise=Mathf.SmoothStep(0,1,Mathf.Clamp01(ActionAge/.25f));
            var lower=Mathf.SmoothStep(0,1,Mathf.Clamp01((ActionAge-.4f)/.5f));
            var held=Point(new Vector3(0,1.12f,.52f));
            // Hands release at arm's reach; the crate then settles on the real service counter.
            crate.transform.SetPositionAndRotation(Vector3.Lerp(Vector3.Lerp(Point(new Vector3(0,.82f,.35f)),held,raise),
                action.Source,lower),transform.rotation);
            if(lower<.45f)hands?.ApplyActionGrips(crateLeft.position,crateRight.position,crateLeft.rotation,crateRight.rotation);
            else hands?.ApplyActionGrips(Point(new Vector3(-.2f,1.05f,.35f)),Point(new Vector3(.2f,1.05f,.35f)),transform.rotation,transform.rotation);
        }
        private void EnsureLanyard()
        {
            if(lanyard!=null)return;
            var obj=new GameObject("Catch retrieval cord");obj.transform.SetParent(transform,false);
            lanyard=obj.AddComponent<LineRenderer>();lanyard.positionCount=2;lanyard.startWidth=.005f;lanyard.endWidth=.005f;
            lanyard.shadowCastingMode=ShadowCastingMode.Off;lanyard.receiveShadows=false;
            cord=new Material(Shader.Find("Universal Render Pipeline/Unlit"));cord.color=new Color(.24f,.3f,.25f);
            lanyard.sharedMaterial=cord;
        }
        private void ClearFish()
        {if(fish!=null)Destroy(fish);fish=null;if(lanyard!=null)lanyard.enabled=false;}
        private void Hide()
        {if(bag!=null)bag.SetActive(false);if(crate!=null)crate.SetActive(false);ClearFish();}
        private void OnDisable(){Hide();if(player!=null)player.SetVisualBoatSeated(false);}
        private void OnDestroy()
        {
            if(trip!=null)trip.MySeatId.OnValueChanged-=SeatChanged;
            if(bag!=null)Destroy(bag);if(crate!=null)Destroy(crate);ClearFish();if(cord!=null)Destroy(cord);
        }
    }
}
