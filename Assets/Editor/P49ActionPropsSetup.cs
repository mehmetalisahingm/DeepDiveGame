using System.Collections.Generic;
using System.IO;
using DeepDive.Network;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace DeepDive.EditorTools
{
    public static class P49ActionPropsSetup
    {
        private const string Root="Assets/DeepDive/Art/Resources/PlayerActions";
        private static Material cloth,rope,wood,edge,iron,leather;
        [MenuItem("DeepDive/P4.9/Generate catch satchel and delivery crate")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
            cloth=Material("WaxedCanvas",new Color(.13f,.26f,.23f),.08f);
            rope=Material("HempRope",new Color(.64f,.56f,.38f),.07f);
            wood=Material("OiledCedar",new Color(.43f,.27f,.12f),.18f);
            edge=Material("WornCedar",new Color(.57f,.39f,.21f),.12f);
            iron=Material("BlackenedIron",new Color(.13f,.16f,.17f),.45f,.65f);
            leather=Material("LeatherBinding",new Color(.20f,.12f,.065f),.16f);
            var bag=new GameObject("CatchSatchel");
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int rings=9,sides=48;
            for(int ring=0;ring<rings;ring++)
            {
                var y=-.34f+ring*.085f;
                var shape=Mathf.Lerp(.48f,1,Mathf.Sin((float)ring/(rings-1)*Mathf.PI*.55f));
                for(int n=0;n<sides;n++)
                {
                    var a=n*Mathf.PI*2/sides;
                    var fold=1+.045f*Mathf.Sin(a*12+ring*.3f)+.018f*Mathf.Sin(a*19-ring*.7f);
                    vertices.Add(new Vector3(Mathf.Cos(a)*.23f*shape*fold,y,Mathf.Sin(a)*.16f*shape*fold));
                    if(ring==0)continue;
                    var p=ring*sides+n;var q=ring*sides+(n+1)%sides;
                    triangles.AddRange(new[]{p,q,p-sides,q,q-sides,p-sides});
                }
            }
            vertices.Add(new Vector3(0,-.34f,0));
            for(int n=0;n<sides;n++)triangles.AddRange(new[]{vertices.Count-1,n,(n+1)%sides});
            var sack=new Mesh{name="Folded canvas sack"};sack.SetVertices(vertices);sack.SetTriangles(triangles,0);sack.RecalculateNormals();sack.RecalculateBounds();
            sack=SaveMesh(sack,"Canvas");Part(bag,"Canvas",sack,Vector3.zero,Vector3.one,cloth);
            var mouth=new List<Vector3>();
            for(int i=0;i<=64;i++){var a=i*Mathf.PI*2/64;mouth.Add(new Vector3(Mathf.Cos(a)*.228f,.34f,Mathf.Sin(a)*.16f));}
            Tube(bag,"BoundMouth",mouth,.009f,rope);
            for(int side=-1;side<=1;side+=2)
            {
                var handle=new List<Vector3>();
                for(int i=0;i<=16;i++){var t=i/16f;handle.Add(new Vector3(side*.20f,.31f+Mathf.Sin(t*Mathf.PI)*.105f,Mathf.Lerp(-.08f,.08f,t)));}
                Tube(bag,"Handle"+side,handle,.012f,rope);
                Grip(bag,side<0?"Grip_Left":"Grip_Right",new Vector3(side*.20f,.415f,0));
                for(int i=0;i<19;i++)
                {
                    var y=-.28f+i*.028f;var shape=Mathf.Lerp(.48f,1,Mathf.Sin((y+.34f)/.68f*Mathf.PI*.55f));
                    Box(bag,"Stitch",new Vector3(side*.162f*shape,y,-.116f*shape),new Vector3(.018f,.005f,.005f),rope);
                }
            }
            Box(bag,"ReinforcedBase",new Vector3(0,-.31f,0),new Vector3(.25f,.06f,.17f),leather);
            Save(bag,"CatchSatchel");

            var crate=new GameObject("FishCrate");
            for(int i=0;i<6;i++)
            {
                var x=-.25f+i*.1f;
                foreach(var y in new[]{-.16f,.15f})Box(crate,"LidSlat",new Vector3(x,y,0),new Vector3(.095f,.028f,.37f),i%2==0?wood:edge);
            }
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<3;i++)
                {
                    Box(crate,"SideSlat",new Vector3(0,-.1f+i*.105f,side*.18f),new Vector3(.62f,.095f,.025f),i%2==0?wood:edge);
                    Box(crate,"EndSlat",new Vector3(side*.3f,-.1f+i*.105f,0),new Vector3(.025f,.095f,.36f),i%2==0?edge:wood);
                }
                foreach(var z in new[]{-.172f,.172f})Box(crate,"CornerBrace",new Vector3(side*.285f,0,z),new Vector3(.047f,.34f,.045f),leather);
                var handle=new List<Vector3>();
                for(int i=0;i<=16;i++){var t=i/16f;handle.Add(new Vector3(side*(.31f+Mathf.Sin(t*Mathf.PI)*.065f),.04f-Mathf.Sin(t*Mathf.PI)*.018f,Mathf.Lerp(-.075f,.075f,t)));}
                Tube(crate,"RopeHandle"+side,handle,.012f,rope);
                Grip(crate,side<0?"Grip_Left":"Grip_Right",new Vector3(side*.375f,.022f,0));
                foreach(var x in new[]{-.26f,.26f})foreach(var y in new[]{-.1f,0f,.1f})
                    Box(crate,"NailHead",new Vector3(x,y,side*.198f),new Vector3(.012f,.012f,.006f),iron);
            }
            Save(crate,"FishCrate");
            // The supplied open rowboat has no benches. Add visible supports under the existing four seats.
            var benches=new GameObject("RowboatBenches");
            foreach(var seat in new[]{BoatSeatLayoutRules.SeatIds[0],BoatSeatLayoutRules.SeatIds[2]})
            {
                BoatHullSeatRules.TryResolveLocalOffset(BoatHullKind.Rowboat,seat,out var offset);
                var y=offset.y+.24f;
                for(int slat=0;slat<3;slat++)Box(benches,"BenchSlat",new Vector3(0,y,offset.z+(slat-1)*.17f),new Vector3(1.9f,.08f,.162f),slat==1?edge:wood);
                foreach(var x in new[]{-.76f,.76f})
                {
                    Box(benches,"BenchSupport",new Vector3(x,.335f,offset.z),new Vector3(.08f,.43f,.38f),wood);
                    foreach(var z in new[]{-.17f,.17f})Box(benches,"BenchPeg",new Vector3(x,y+.042f,offset.z+z),new Vector3(.025f,.008f,.025f),iron);
                }
            }
            Save(benches,"RowboatBenches");AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("P49_ACTION_PROPS_DONE");
        }
        private static Material Material(string name,Color color,float smooth,float metal=0)
        {
            var path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smooth);material.SetFloat("_Metallic",metal);material.SetFloat("_Cull",0);
            EditorUtility.SetDirty(material);return material;
        }
        private static void Grip(GameObject root,string name,Vector3 position)
        {
            var grip=new GameObject(name).transform;grip.SetParent(root.transform,false);grip.localPosition=position;
            grip.localRotation=Quaternion.LookRotation(Vector3.up,Vector3.back);
        }
        private static void Box(GameObject root,string name,Vector3 position,Vector3 size,Material material)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DeepDive/Art/RepairTools/Meshes/CarrySlat.asset");
            var bounds=mesh.bounds;
            var obj=Part(root,name,mesh,position,new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z),material);
            obj.transform.localPosition-=Vector3.Scale(bounds.center,obj.transform.localScale);
        }
        private static GameObject Part(GameObject root,string name,Mesh mesh,Vector3 position,Vector3 scale,Material material)
        {
            var obj=new GameObject(name);obj.transform.SetParent(root.transform,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var materials=new Material[mesh.subMeshCount];for(int i=0;i<materials.Length;i++)materials[i]=material;
            obj.AddComponent<MeshRenderer>().sharedMaterials=materials;return obj;
        }
        private static void Tube(GameObject root,string name,List<Vector3> path,float radius,Material material)
        {
            var verts=new List<Vector3>();var indices=new List<int>();const int sides=8;
            for(int i=0;i<path.Count;i++)
            {
                var forward=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;
                var right=Vector3.Cross(forward,Vector3.forward).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;
                var up=Vector3.Cross(right,forward).normalized;
                for(int j=0;j<sides;j++)
                {
                    var a=j*Mathf.PI*2/sides;verts.Add(path[i]+radius*(Mathf.Cos(a)*right+Mathf.Sin(a)*up));
                    if(i==0)continue;var p=i*sides+j;var q=i*sides+(j+1)%sides;indices.AddRange(new[]{p,p-sides,q,q,p-sides,q-sides});
                }
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,root.name+"_"+name);Part(root,name,mesh,Vector3.zero,Vector3.one,material);
        }
        private static Mesh SaveMesh(Mesh mesh,string name)
        {
            var path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old!=null){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static void Save(GameObject root,string name)
        {
            // Batch identical materials into one draw per material; markers remain independent transforms.
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var materials=filter.GetComponent<MeshRenderer>().sharedMaterials;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                {
                    var mat=materials[Mathf.Min(sub,materials.Length-1)];
                    if(!groups.TryGetValue(mat,out var list))groups[mat]=list=new List<CombineInstance>();
                    list.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=sub,transform=filter.transform.localToWorldMatrix});
                }
            }
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name=name+"_"+pair.Key.name};mesh.CombineMeshes(pair.Value.ToArray());mesh=SaveMesh(mesh,mesh.name);
                Part(root,"Batched_"+pair.Key.name,mesh,Vector3.zero,Vector3.one,pair.Key);
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(!filter.name.StartsWith("Batched_"))Object.DestroyImmediate(filter.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");Object.DestroyImmediate(root);
        }
    }
}
