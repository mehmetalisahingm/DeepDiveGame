using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Network
{
    // Cosmetic only. Host-published work/stroke variables drive both local and observer views.
    public sealed class RepairCarryView : MonoBehaviour
    {
        private NetworkPlayer player;
        private GameObject board, hammer;
        private Material wood, metal;
        private AudioSource audioSource;
        private AudioClip impact;
        private uint stroke;
        private float pulse;
        private void Awake() { player = GetComponent<NetworkPlayer>(); }
        private void Start()
        {
            stroke=player.RepairStroke.Value;
            wood = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(.64f,.4f,.18f) };
            metal = new Material(wood) { color = new Color(.22f,.25f,.28f) };
            board = Cube("CarriedRepairPlank", transform, new Vector3(.85f,.18f,.08f), wood);
            hammer = new GameObject("RepairHatchet"); hammer.transform.SetParent(transform,false);
            var handle = Cube("WoodHandle",hammer.transform,new Vector3(.035f,.34f,.04f),wood); handle.transform.localPosition=Vector3.up*.1f;
            var head=Cube("SteelHead",hammer.transform,new Vector3(.18f,.09f,.07f),metal); head.transform.localPosition=Vector3.up*.25f;
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false; audioSource.spatialBlend=player.IsOwner?0f:1f; audioSource.maxDistance=15;
            // Short prototype wood impact; final art/audio remains #189's delivery.
            var samples=new float[4410]; uint seed=137;
            for(int i=0;i<samples.Length;i++){seed=seed*1664525+1013904223;samples[i]=(((seed>>8)&65535)/32768f-1f)*Mathf.Exp(-i/350f)*.35f;}
            impact=AudioClip.Create("RepairWoodImpact",samples.Length,1,44100,false);impact.SetData(samples,0);
        }
        private static GameObject Cube(string name,Transform parent,Vector3 size,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent,false);obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=mat;return obj;
        }
        private void LateUpdate()
        {
            if(board==null||player==null)return;
            if(player.RepairStroke.Value!=stroke)
            {stroke=player.RepairStroke.Value;pulse=Time.unscaledTime+.3f;audioSource.PlayOneShot(impact);}
            bool active=player.IsSpawned&&!player.Passive.Value&&player.RepairHandsBusy;
            board.SetActive(active&&!player.RepairPlaced.Value);
            hammer.SetActive((active&&player.RepairPlaced.Value)||Time.unscaledTime<pulse);
            var camera=player.IsOwner?GetComponentInChildren<Camera>():null;
            var parent=camera!=null?camera.transform:transform;
            board.transform.SetParent(parent,false);hammer.transform.SetParent(parent,false);
            board.transform.localPosition=camera!=null?new Vector3(0,-.28f,.65f):new Vector3(0,1.05f,.55f);
            board.transform.localRotation=Quaternion.Euler(0,0,-8);
            hammer.transform.localPosition=camera!=null?new Vector3(.28f,-.28f,.6f):new Vector3(.28f,1.15f,.48f);
            var arc=Time.unscaledTime<pulse?Mathf.Sin((pulse-Time.unscaledTime)/.3f*Mathf.PI)*65f:0;
            hammer.transform.localRotation=Quaternion.Euler(-20+arc,0,-15);
        }
        private void OnGUI()
        {
            if(player==null||!player.IsSpawned||!player.IsOwner||!player.RepairHandsBusy)return;
            var text=player.RepairPlaced.Value?$"KESER: sol tik ({player.RepairHits.Value}/3) | Q: askiya birak":"TAHTA ELINDE: iskelede eslesen yuvaya bak + E | Q: ortak askiya birak";
            GUI.Box(new Rect(20,Screen.height-62,620,38),text);
        }
        private void OnDestroy()
        {if(board!=null)Destroy(board);if(hammer!=null)Destroy(hammer);if(wood!=null)Destroy(wood);if(metal!=null)Destroy(metal);if(impact!=null)Destroy(impact);}
    }
}
