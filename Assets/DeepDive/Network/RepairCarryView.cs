using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // What the host-published work state just did to the carried material; each cue has its own sound.
    public enum RepairCue { None, Lift, Seat, Drop }

    // Cosmetic only. Host-published work/stroke variables drive both local and observer views.
    [DefaultExecutionOrder(200)]
    public sealed class RepairCarryView : MonoBehaviour
    {
        private NetworkPlayer player;
        private GameObject board, hammer;
        private Material wood, metal;
        private AudioSource audioSource;
        private AudioClip impact, liftClip, dropClip, seatClip;
        private bool cuePrimed, wasBusy, wasPlaced;
        private uint stroke;
        private float pulse;
        private Transform boardLeft, boardRight, hammerGrip, toolContact;
        private bool ownsImpact;
        private int rememberedPart=-1, rememberedHits, strikePart=-1, strikeNail;
        private Vector3 previousPosition;
        // Lift: hands newly busy. Seat: the carried board went onto its slot. Drop: hands freed without an accepted final stroke
        // (Q / disconnect / release); a completing stroke frees the hands in the same network tick and is voiced by the impact.
        // Only host-published variables move these edges, so a rejected action never makes a success sound.
        public static RepairCue CueFor(bool wasBusy,bool wasPlaced,bool busy,bool placed,bool struck)
        {
            if(!wasBusy&&busy)return RepairCue.Lift;
            if(busy&&!wasPlaced&&placed)return RepairCue.Seat;
            if(wasBusy&&!busy&&!struck)return RepairCue.Drop;
            return RepairCue.None;
        }
        public bool UsesAuthoredArt {get;private set;}
        public Vector3 RightGripPosition => hammerGrip!=null?hammerGrip.position:transform.position;
        public float NailContactError {get;private set;}=float.PositiveInfinity;
        public float StrokeAge => Time.unscaledTime < pulse ? Mathf.Clamp01(1f-(pulse-Time.unscaledTime)/.28f) : 1f;
        private void Awake() { player = GetComponent<NetworkPlayer>(); }
        private void Start()
        {
            stroke=player.RepairStroke.Value;
            wood = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(.64f,.4f,.18f) };
            metal = new Material(wood) { color = new Color(.22f,.25f,.28f) };
            var catalog=Resources.Load<DiverPresentationCatalog>(DiverPresentationCatalog.ResourceName);
            if(catalog!=null && catalog.RepairPlankPrefab!=null)
                board=Instantiate(catalog.RepairPlankPrefab,transform,false);
            else
            {
                board=new GameObject("PrototypeCarriedRepairPlank");board.transform.SetParent(transform,false);
                Cube("Wood",board.transform,new Vector3(.85f,.18f,.08f),wood);
                AddMarker(board.transform,"Grip_Left",new Vector3(-.34f,0,-.04f));
                AddMarker(board.transform,"Grip_Right",new Vector3(.34f,0,-.04f));
            }
            if(catalog!=null && catalog.RepairHatchetPrefab!=null)
                hammer=Instantiate(catalog.RepairHatchetPrefab,transform,false);
            else
            {
                hammer = new GameObject("PrototypeRepairHatchet"); hammer.transform.SetParent(transform,false);
                var handle = Cube("WoodHandle",hammer.transform,new Vector3(.035f,.34f,.04f),wood); handle.transform.localPosition=Vector3.up*.1f;
                var head=Cube("SteelHead",hammer.transform,new Vector3(.18f,.09f,.07f),metal); head.transform.localPosition=Vector3.up*.25f;
                AddMarker(hammer.transform,"GripPoint",Vector3.zero);
            }
            boardLeft=RepairPropPlacement.Marker(board.transform,"Grip_Left");
            boardRight=RepairPropPlacement.Marker(board.transform,"Grip_Right");
            hammerGrip=RepairPropPlacement.Marker(hammer.transform,"GripPoint");
            toolContact=RepairPropPlacement.Marker(hammer.transform,"ToolContact");
            UsesAuthoredArt=catalog!=null&&catalog.RepairPlankPrefab!=null&&catalog.RepairHatchetPrefab!=null;
            previousPosition=transform.position;
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false; audioSource.spatialBlend=player.IsOwner?0f:1f; audioSource.maxDistance=15;
            // Short prototype wood impact; final art/audio remains #189's delivery.
            impact=catalog!=null?catalog.RepairImpactClip:null;
            liftClip=catalog!=null?catalog.RepairLiftClip:null;dropClip=catalog!=null?catalog.RepairDropClip:null;seatClip=catalog!=null?catalog.RepairSeatClip:null;
            if(impact==null)
            {
                var samples=new float[4410]; uint seed=137;
                for(int i=0;i<samples.Length;i++){seed=seed*1664525+1013904223;samples[i]=(((seed>>8)&65535)/32768f-1f)*Mathf.Exp(-i/350f)*.35f;}
                impact=AudioClip.Create("RepairWoodImpact",samples.Length,1,44100,false);impact.SetData(samples,0);ownsImpact=true;
            }
        }
        private static void AddMarker(Transform root,string name,Vector3 position)
        {var marker=new GameObject(name).transform;marker.SetParent(root,false);marker.localPosition=position;}
        private static GameObject Cube(string name,Transform parent,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent,false);obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;return obj;
        }
        private void LateUpdate()
        {
            if(board==null||player==null)return;
            var struck=player.RepairStroke.Value!=stroke;
            if(struck)
            {
                strikePart=player.RepairHandsBusy?player.RepairPartIndex.Value:rememberedPart;
                strikeNail=player.RepairHandsBusy?Mathf.Clamp(player.RepairHits.Value-1,0,2):Mathf.Clamp(rememberedHits,0,2);
                stroke=player.RepairStroke.Value;pulse=Time.unscaledTime+.28f;
                audioSource.PlayOneShot(impact);
            }
            bool active=player.IsSpawned&&!player.Passive.Value&&player.RepairHandsBusy;
            bool finishing=player.IsSpawned&&!player.Passive.Value&&!player.Seated.Value&&Time.unscaledTime<pulse;
            var placedNow=active&&player.RepairPlaced.Value;
            if(cuePrimed)
            {
                var cue=CueFor(wasBusy,wasPlaced,active,placedNow,struck);
                var clip=cue==RepairCue.Lift?liftClip:cue==RepairCue.Seat?seatClip:cue==RepairCue.Drop?dropClip:null;
                if(clip!=null)audioSource.PlayOneShot(clip);
            }
            cuePrimed=true;wasBusy=active;wasPlaced=placedNow;
            if(active){rememberedPart=player.RepairPartIndex.Value;rememberedHits=player.RepairHits.Value;}
            board.SetActive(active&&!player.RepairPlaced.Value);
            hammer.SetActive((active&&player.RepairPlaced.Value)||finishing);
            var camera=player.IsOwner?GetComponentInChildren<Camera>():null;
            var parent=camera!=null?camera.transform:transform;
            board.transform.SetParent(parent,false);hammer.transform.SetParent(parent,false);
            var speed=Time.deltaTime>.0001f?Mathf.Clamp01(Vector3.Distance(transform.position,previousPosition)/(Time.deltaTime*4f)):0;
            previousPosition=transform.position;
            var bob=Mathf.Sin(Time.unscaledTime*8f)*.007f*speed;
            board.transform.localPosition=(camera!=null?new Vector3(0,-.30f,.52f):new Vector3(0,1.22f,.42f))+Vector3.up*bob;
            board.transform.localRotation=Quaternion.Euler(0,0,-4+Mathf.Sin(Time.unscaledTime*4f)*speed);
            var arc=finishing?Mathf.Sin(StrokeAge*Mathf.PI)*45f:0;
            var gripPosition=parent.TransformPoint(camera!=null?new Vector3(.28f,-.28f,.6f):new Vector3(.28f,1.15f,.48f));
            var gripRotation=parent.rotation*Quaternion.Euler(-20+arc,0,-15);
            RepairPropPlacement.AlignGrip(hammer.transform,hammerGrip,gripPosition,gripRotation);
            NailContactError=float.PositiveInfinity;
            if(hammer.activeSelf && toolContact!=null && hammerGrip!=null)
            {
                var index=finishing?strikePart:player.RepairPartIndex.Value;
                var target=RepairWorkContactTarget.ForPart(index);
                if(target!=null)
                {
                    var nail=target.ContactAt(finishing?strikeNail:Mathf.Clamp(player.RepairHits.Value,0,2));
                    // The accepted stroke starts at contact, then lifts clear. There is
                    // no speculative success animation/audio before host acceptance.
                    var lift=finishing?Mathf.SmoothStep(0,1,StrokeAge):1f;
                    var contact=nail-target.transform.forward*(.13f*lift)+Vector3.up*(.11f*lift);
                    var restGrip=transform.TransformPoint(new Vector3(.22f,1.26f,.38f));
                    RepairPropPlacement.AimToolContact(hammer.transform,toolContact,hammerGrip,contact,restGrip,transform.rotation);
                    gripPosition=hammerGrip.position;
                    NailContactError=Vector3.Distance(toolContact.position,nail);
                }
            }
            if(active||finishing)
            {
                var left=board.activeSelf && boardLeft!=null?boardLeft.position:
                    transform.TransformPoint(new Vector3(-.22f,1.05f,.25f));
                var right=board.activeSelf && boardRight!=null?boardRight.position:gripPosition;
                var leftRotation=board.activeSelf && boardLeft!=null?boardLeft.rotation:transform.rotation*Quaternion.Euler(55,-25,-15);
                var rightRotation=board.activeSelf && boardRight!=null?boardRight.rotation:hammerGrip!=null?hammerGrip.rotation:transform.rotation;
                GetComponent<PlayerPresentationView>()?.ApplyRepairGrips(left,right,leftRotation,rightRotation,finishing,board.activeSelf);
            }
        }
        private void OnGUI()
        {
            if(player==null||!player.IsSpawned||!player.IsOwner||!player.RepairHandsBusy)return;
            var text=player.RepairPlaced.Value?$"KESER: sol tik ({player.RepairHits.Value}/3) | Q: askiya birak":"TAHTA ELINDE: iskelede eslesen yuvaya bak + E | Q: ortak askiya birak";
            GUI.Box(new Rect(20,Screen.height-62,620,38),text);
        }
        private void OnDestroy()
        {if(board!=null)Destroy(board);if(hammer!=null)Destroy(hammer);if(wood!=null)Destroy(wood);if(metal!=null)Destroy(metal);if(ownsImpact&&impact!=null)Destroy(impact);}
    }
}
