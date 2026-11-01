using DeepDive.Network;
using DeepDive.Session;
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
        private LineRenderer shoulderStrap;
        private Material cord;
        private bool seated;
        private int seatTransition;
        private double seatAt;
        private HeldEquipmentMode equipmentAtStart;
        private ulong requestAtStart;
        private Vector3 fishCenter;
        private Mesh[] frozenMeshes=new Mesh[0];
        public bool HasCaughtFish=>fish!=null && fish.activeInHierarchy;
        public Vector3 CaughtFishPosition=>fish!=null?fish.transform.TransformPoint(fishCenter):Vector3.zero;
        private Renderer[] bagRenderers;
        private SessionNetworkAdapter session;
        private SessionPhase phase;
        private bool phaseKnown;
        public Vector3 LeftGripPosition{get;private set;}
        public Vector3 RightGripPosition{get;private set;}
        public bool ContactExpected{get;private set;}
        public float BodyGripError=>hands!=null?hands.ActionGripError(false,LeftGripPosition,RightGripPosition):float.PositiveInfinity;
        public float OwnerGripError=>hands!=null?hands.ActionGripError(true,LeftGripPosition,RightGripPosition):float.PositiveInfinity;
        public Transform Satchel=>bag!=null?bag.transform:null;
        public Transform SatchelHip=>hands!=null?hands.ActionHip:null;
        public int CollectCount{get;private set;}
        public int DeliveryCount{get;private set;}
        public int BoardCount{get;private set;}
        public int ExitCount{get;private set;}
        public PlayerVisualAction ActiveKind{get;private set;}
        public bool AuthoredProps=>bag!=null&&crate!=null;
        public float ActionAge{get;private set;}
        public Vector3 ActionFocus=>ActiveKind==PlayerVisualAction.Deliver && crate!=null ? crate.transform.position :
            ActiveKind==PlayerVisualAction.Collect && bag!=null ? ContactExpected?(LeftGripPosition+RightGripPosition)*.5f:bag.transform.position+Body.up*.3f : Point(Vector3.up*.85f);
        private double Now=>NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening?NetworkManager.Singleton.ServerTime.Time:Time.unscaledTimeAsDouble;
        private void Awake()
        {
            player=GetComponent<NetworkPlayer>();hands=GetComponent<PlayerPresentationView>();trip=GetComponent<BoatTripPlayerSync>();
            session=FindFirstObjectByType<SessionNetworkAdapter>();
            bag=Spawn(BagResource);crate=Spawn(CrateResource);
            bagRenderers=bag!=null?bag.GetComponentsInChildren<Renderer>(true):new Renderer[0];
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
                    fish.transform.localScale*=.62f;
                    fishCenter=FreezeCaughtMesh(fish,out frozenMeshes);
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
        public static Vector3 FreezeCaughtMesh(GameObject root,out Mesh[] ownedMeshes)
        {
            foreach(var animation in root.GetComponentsInChildren<Animation>(true)){animation.Stop();animation.enabled=false;}
            foreach(var animator in root.GetComponentsInChildren<Animator>(true))animator.enabled=false;
            var meshes=new System.Collections.Generic.List<Mesh>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=new Mesh{name="Frozen caught fish"};skin.BakeMesh(mesh,false);mesh.RecalculateBounds();
                var baked=new GameObject("Frozen catch mesh");baked.transform.SetParent(skin.transform,false);
                baked.AddComponent<MeshFilter>().sharedMesh=mesh;
                baked.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                skin.enabled=false;meshes.Add(mesh);
            }
            // A newly spawned skinned renderer may not have valid world bounds yet.
            // Derive the centre from real mesh geometry in the clone's root frame.
            var bounds=new Bounds();bool found=false;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<Renderer>();
                if(filter.sharedMesh==null || renderer==null || !renderer.enabled || !filter.gameObject.activeInHierarchy)continue;
                var local=filter.sharedMesh.bounds;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    var corner=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var point=matrix.MultiplyPoint3x4(corner);
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            ownedMeshes=meshes.ToArray();return found?bounds.center:Vector3.zero;
        }
        private Transform Body=>hands!=null&&hands.ActionBodyFrame!=null?hands.ActionBodyFrame:transform;
        private Vector3 Point(Vector3 local)=>Body.TransformPoint(local);
        public static Pose StowedSatchelPose(Transform hip,bool sitting=false)
        {
            // Outside the thigh/tank and forward of the bench; the long sack lies along the leg.
            return new Pose(hip.position+hip.rotation*new Vector3(-.43f,-.12f,sitting?.58f:.13f),hip.rotation);
        }
        private void Grips(Vector3 left,Vector3 right,Quaternion leftRotation,Quaternion rightRotation,bool contact=true)
        {
            LeftGripPosition=left;RightGripPosition=right;ContactExpected=contact;
            hands?.ApplyActionGrips(left,right,leftRotation,rightRotation);
        }
        private void LateUpdate()
        {
            if(player==null || !player.IsSpawned){Hide();return;}
            if(session!=null && session.Session!=null)
            {
                var current=session.Session.State.Phase;
                if(phaseKnown && current!=phase){until=0;seatTransition=0;}
                phase=current;phaseKnown=true;
            }
            var blocked=player.Passive.Value || player.RepairHandsBusy || player.RecordingPresentation.Value;
            blocked |= player.CurrentHeldEquipment!=equipmentAtStart && Now<until;
            // An accepted shot is free to interrupt this cosmetic recovery immediately.
            if(Now<until && player.LastActionRequestId.Value!=requestAtStart &&
                player.LastActionKind.Value==(byte)PlayerActionKind.Harpoon)blocked=true;
            ActiveKind=PlayerVisualAction.None;ContactExpected=false;
            if(blocked){until=0;seatTransition=0;}
            var active=!blocked && Now<until;
            var holdingBag=active && action.Kind==PlayerVisualAction.Collect;
            if(bag!=null)
            {
                bag.SetActive(!player.Passive.Value);
                var hip=SatchelHip;
                var stowed=hip!=null?StowedSatchelPose(hip,seated):new Pose(Point(new Vector3(-.43f,.75f,.13f)),Body.rotation);
                bag.transform.SetPositionAndRotation(holdingBag?Point(new Vector3(0,.87f,.32f)):stowed.position,holdingBag?Body.rotation:stowed.rotation);
                foreach(var renderer in bagRenderers)
                    renderer.shadowCastingMode=player.IsOwner&&!holdingBag?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
                if(!holdingBag&&!player.Passive.Value)
                {
                    EnsureStrap();shoulderStrap.enabled=true;
                    shoulderStrap.shadowCastingMode=player.IsOwner?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
                    shoulderStrap.SetPosition(0,bag.transform.TransformPoint(new Vector3(0,.31f,.13f)));
                    shoulderStrap.SetPosition(1,Point(new Vector3(-.22f,1.48f,.07f)));
                    shoulderStrap.SetPosition(2,Point(new Vector3(-.22f,1.48f,-.12f)));
                    shoulderStrap.SetPosition(3,bag.transform.TransformPoint(new Vector3(0,.31f,-.13f)));
                }
                else if(shoulderStrap!=null)shoulderStrap.enabled=false;
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
                    Grips(Point(new Vector3(-.29f,y+lift,.32f)),Point(new Vector3(.29f,y+lift,.32f)),
                        Body.rotation*Quaternion.Euler(60,0,-20),Body.rotation*Quaternion.Euler(60,0,20),false);
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
            var held=Point(new Vector3(.10f,1.44f,.22f));
            var arrive=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.35f));
            var stow=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.48f)/.42f));
            // A constant-size fish is lowered through the open mouth into the opaque cloth sack.
            var fishPosition=Vector3.Lerp(Vector3.Lerp(action.Source,held,arrive),Point(new Vector3(0,.87f,.32f)),stow);
            if(fish!=null)
            {
                fish.transform.rotation=Quaternion.Slerp(Body.rotation*Quaternion.Euler(0,90,10),
                    Body.rotation*Quaternion.Euler(-90,0,0),Mathf.SmoothStep(0,1,Mathf.Clamp01((age-.25f)/.25f)));
                fish.transform.position+=fishPosition-fish.transform.TransformPoint(fishCenter);
                fish.SetActive(age<.92f);
            }
            var right=Vector3.Lerp(held+Body.right*.035f,bagRight.position,stow);
            Grips(bagLeft.position,right,bagLeft.rotation,Quaternion.Slerp(Body.rotation*Quaternion.Euler(5,0,60),bagRight.rotation,stow),age>.35f&&age<.88f);
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
            var held=Point(new Vector3(0,1.30f,.26f));
            // Hands release at arm's reach; the crate then settles on the real service counter.
            crate.transform.SetPositionAndRotation(Vector3.Lerp(Vector3.Lerp(Point(new Vector3(0,1.22f,.24f)),held,raise),
                action.Source,lower),Body.rotation);
            if(lower<.06f)Grips(crateLeft.position,crateRight.position,crateLeft.rotation,crateRight.rotation);
            else Grips(Point(new Vector3(-.2f,1.05f,.35f)),Point(new Vector3(.2f,1.05f,.35f)),Body.rotation,Body.rotation,false);
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
        private void EnsureStrap()
        {
            if(shoulderStrap!=null)return;
            EnsureLanyard();lanyard.enabled=false;
            var obj=new GameObject("Satchel shoulder strap");obj.transform.SetParent(transform,false);
            shoulderStrap=obj.AddComponent<LineRenderer>();shoulderStrap.positionCount=4;
            shoulderStrap.startWidth=.032f;shoulderStrap.endWidth=.032f;
            shoulderStrap.receiveShadows=false;shoulderStrap.sharedMaterial=cord;
        }
        private void ClearFish()
        {if(fish!=null)Destroy(fish);fish=null;foreach(var mesh in frozenMeshes)if(mesh!=null)Destroy(mesh);frozenMeshes=new Mesh[0];if(lanyard!=null)lanyard.enabled=false;}
        private void Hide()
        {until=0;seatTransition=0;ActiveKind=PlayerVisualAction.None;ContactExpected=false;if(bag!=null)bag.SetActive(false);if(crate!=null)crate.SetActive(false);if(shoulderStrap!=null)shoulderStrap.enabled=false;ClearFish();}
        private void OnDisable(){Hide();if(player!=null)player.SetVisualBoatSeated(false);}
        private void OnDestroy()
        {
            if(trip!=null)trip.MySeatId.OnValueChanged-=SeatChanged;
            if(bag!=null)Destroy(bag);if(crate!=null)Destroy(crate);ClearFish();if(cord!=null)Destroy(cord);
        }
    }
}
