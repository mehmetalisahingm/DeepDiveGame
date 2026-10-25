using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepDive.Editor
{
    // Original project-authored meshes. No downloaded model, image, font or audio.
    // Regeneration keeps existing asset GUIDs and never edits supplied art/scenes.
    public static class RepairToolArtBuilder
    {
        public const string Root = "Assets/DeepDive/Art/RepairTools";
        private static Material oak, bevel, end, grain, steel, edge, leather;

        [MenuItem("DeepDive/P4/Generate repair tools")]
        public static void Generate()
        {
            Folder(Root); Folder(Root+"/Meshes"); Folder(Root+"/Materials");
            oak=Material("WarmOak",new Color(.47f,.25f,.105f),0,.18f);
            bevel=Material("WornOakEdges",new Color(.63f,.38f,.18f),0,.21f);
            end=Material("OakEndGrain",new Color(.32f,.16f,.065f),0,.12f);
            grain=Material("FineWoodGrain",new Color(.25f,.115f,.045f),0,.13f);
            steel=Material("ForgedSteel",new Color(.115f,.145f,.16f),.75f,.38f);
            edge=Material("WornSteelEdges",new Color(.42f,.49f,.51f),.85f,.62f);
            leather=Material("HandleBinding",new Color(.105f,.065f,.035f),0,.25f);

            var plank=new GameObject("OakRepairPlank");
            Plank(plank.transform,"CarrySlat",.78f,.22f,.05f);
            Marker(plank.transform,"Grip_Left",new Vector3(-.27f,-.07f,-.03f)).localRotation=Quaternion.Euler(0,-90,0);
            Marker(plank.transform,"Grip_Right",new Vector3(.27f,-.07f,-.03f)).localRotation=Quaternion.Euler(0,90,0);
            Marker(plank.transform,"GripPoint",Vector3.zero);
            SavePrefab(plank,"OakRepairPlank");

            var patch=new GameObject("OakRepairPatch");
            for(int i=0;i<3;i++)
            {
                var slat=new GameObject("FittedSlat_"+i);slat.transform.SetParent(patch.transform,false);
                slat.transform.localPosition=new Vector3(0,(i-1)*.225f,0);
                slat.transform.localRotation=Quaternion.Euler(0,0,(i-1)*.7f);
                Plank(slat.transform,"PatchSlat",.58f,.215f,.06f);
            }
            Marker(patch.transform,"FastenerSurface",new Vector3(0,0,-.03f));
            SavePrefab(patch,"OakRepairPatch");

            var hatchet=new GameObject("CarpentersAdze");
            var handleRings=new[]{new Vector3(0,-.125f,.018f),new Vector3(-.004f,-.105f,.025f),
                new Vector3(-.007f,.02f,.023f),new Vector3(.003f,.19f,.021f),new Vector3(.016f,.34f,.018f),
                new Vector3(.008f,.47f,.017f),new Vector3(0,.515f,.015f)};
            Part(hatchet.transform,"ShapedOakHandle",SaveMesh(Tube(handleRings,12),"AdzeHandle"),new[]{oak,bevel,end});
            for(int i=0;i<7;i++)
            {
                var y=-.085f+i*.019f;
                var wrap=new[]{new Vector3(-.004f,y,.025f),new Vector3(-.004f,y+.012f,.025f)};
                Part(hatchet.transform,"GripBinding_"+i,SaveMesh(Tube(wrap,12),"GripWrap_"+i),new[]{leather,leather,leather});
            }
            var profile=new[]{new Vector2(-.14f,.425f),new Vector2(-.14f,.515f),new Vector2(-.06f,.54f),
                new Vector2(.095f,.53f),new Vector2(.16f,.48f),new Vector2(.20f,.385f),
                new Vector2(.16f,.365f),new Vector2(.105f,.435f),new Vector2(-.055f,.445f)};
            var head=Part(hatchet.transform,"BevelledForgedHead",SaveMesh(Chamfer(profile,.095f,.009f),"AdzeHead"),new[]{steel,edge,steel});
            head.localRotation=Quaternion.Euler(0,90,0);
            Marker(hatchet.transform,"GripPoint",new Vector3(.024f,0,0));
            Marker(hatchet.transform,"ToolContact",new Vector3(0,.47f,.14f));
            SavePrefab(hatchet,"CarpentersAdze");

            var nail=new GameObject("ForgedBoatNail");
            var nailShape=new[]{new Vector3(0,-.007f,.016f),new Vector3(0,-.004f,.02f),
                new Vector3(0,.002f,.018f),new Vector3(0,.004f,.004f),
                new Vector3(0,.055f,.0035f),new Vector3(0,.072f,0)};
            var shaft=Part(nail.transform,"ShankAndHammeredHead",SaveMesh(Tube(nailShape,10),"BoatNail"),new[]{steel,edge,steel});
            shaft.localRotation=Quaternion.Euler(90,0,0);
            Marker(nail.transform,"ContactPoint",new Vector3(0,0,-.007f));
            SavePrefab(nail,"ForgedBoatNail");

            WriteImpact();
            var catalog=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/DeepDive/Network/Resources/DiverPresentationCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Missing diver catalog");
            var serialized=new SerializedObject(catalog);
            Set(serialized,"repairPlankPrefab","OakRepairPlank.prefab");
            Set(serialized,"repairHatchetPrefab","CarpentersAdze.prefab");
            Set(serialized,"repairPlacedPlankPrefab","OakRepairPatch.prefab");
            Set(serialized,"repairNailPrefab","ForgedBoatNail.prefab");
            Set(serialized,"repairImpactClip","OakNailImpact.wav");
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("P187_REPAIR_ART_GENERATED original meshes/materials/audio and catalog bindings");
        }

        private static void Set(SerializedObject catalog,string field,string file)
        {catalog.FindProperty(field).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Object>(Root+"/"+file);}
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static Material Material(string name,Color color,float metallic,float smoothness)
        {
            var path=Root+"/Materials/"+name+".mat";
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(value==null){value=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(value,path);}
            value.name=name;value.SetColor("_BaseColor",color);value.SetFloat("_Metallic",metallic);value.SetFloat("_Smoothness",smoothness);
            value.enableInstancing=true;EditorUtility.SetDirty(value);return value;
        }
        private static Mesh SaveMesh(Mesh mesh,string name)
        {
            var path=Root+"/Meshes/"+name+".asset";mesh.name=name;
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;
        }
        private static Transform Part(Transform parent,string name,Mesh mesh,Material[] materials)
        {
            var part=new GameObject(name);part.transform.SetParent(parent,false);
            part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterials=materials;
            return part.transform;
        }
        private static Transform Marker(Transform parent,string name,Vector3 position)
        {var marker=new GameObject(name).transform;marker.SetParent(parent,false);marker.localPosition=position;return marker;}
        private static void SavePrefab(GameObject value,string name)
        {PrefabUtility.SaveAsPrefabAsset(value,Root+"/"+name+".prefab");Object.DestroyImmediate(value);}

        private static void Plank(Transform parent,string id,float width,float height,float depth)
        {
            var outline=new[]{new Vector2(-.50f,-.38f),new Vector2(-.475f,-.49f),new Vector2(.39f,-.47f),
                new Vector2(.49f,-.36f),new Vector2(.50f,.25f),new Vector2(.465f,.47f),
                new Vector2(-.37f,.50f),new Vector2(-.49f,.35f)};
            for(int i=0;i<outline.Length;i++)outline[i]=Vector2.Scale(outline[i],new Vector2(width,height));
            Part(parent,"BevelledOak",SaveMesh(Chamfer(outline,depth,.008f),id),new[]{oak,bevel,end});
            var builder=new MeshParts();
            foreach(var side in new[]{-1,1})
            {
                for(int row=0;row<9;row++)
                {
                    var line=new List<Vector3>();
                    for(int j=0;j<=20;j++)
                    {
                        float x=(j/20f-.5f)*width*.9f;
                        float y=(row/8f-.5f)*height*.79f+Mathf.Sin(j*.4f+row*1.7f)*height*.017f;
                        line.Add(new Vector3(x,y,side*(depth*.5f+.0007f)));
                    }
                    Ribbon(builder,line,.00065f,Vector3.forward*side);
                }
                for(int ring=0;ring<3;ring++)
                {
                    var line=new List<Vector3>();
                    for(int j=0;j<=28;j++)
                    {
                        var angle=j/28f*Mathf.PI*2;
                        line.Add(new Vector3(-width*.23f+Mathf.Cos(angle)*(.007f+ring*.004f),
                            height*.10f+Mathf.Sin(angle)*(.005f+ring*.002f),side*(depth*.5f+.001f)));
                    }
                    Ribbon(builder,line,.0008f,Vector3.forward*side);
                }
            }
            Part(parent,"CarvedGrainAndKnot",SaveMesh(builder.Mesh(),id+"Grain"),new[]{grain,grain,grain});
        }
        private static void Ribbon(MeshParts builder,List<Vector3> points,float width,Vector3 normal)
        {
            for(int i=0;i<points.Count-1;i++)
            {
                var across=Vector3.Cross(normal,(points[i+1]-points[i]).normalized)*width*.5f;
                builder.Quad(points[i]+across,points[i]-across,points[i+1]-across,points[i+1]+across,0);
            }
        }
        private static Mesh Chamfer(Vector2[] outline,float thickness,float inset)
        {
            var polygon=new List<Vector2>(outline);
            float signedArea=0;
            for(int i=0;i<polygon.Count;i++)signedArea+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);
            if(signedArea<0)polygon.Reverse();
            var center=Vector2.zero;foreach(var point in polygon)center+=point;center/=polygon.Count;
            var inner=new Vector2[polygon.Count];
            for(int i=0;i<inner.Length;i++)inner[i]=polygon[i]+(center-polygon[i]).normalized*inset;
            var builder=new MeshParts();var triangles=Triangulate(inner);
            foreach(int side in new[]{-1,1})
            {
                foreach(var tri in triangles)
                {
                    var a=V(inner[tri.x],side*thickness*.5f);var b=V(inner[tri.y],side*thickness*.5f);var c=V(inner[tri.z],side*thickness*.5f);
                    if(side>0)builder.Triangle(a,b,c,0);else builder.Triangle(a,c,b,2);
                }
                for(int i=0;i<inner.Length;i++)
                {
                    int next=(i+1)%inner.Length;
                    var a=V(inner[i],side*thickness*.5f);var b=V(polygon[i],side*(thickness*.5f-inset));
                    var c=V(polygon[next],side*(thickness*.5f-inset));var d=V(inner[next],side*thickness*.5f);
                    if(side>0)builder.Quad(a,b,c,d,1);else builder.Quad(d,c,b,a,1);
                }
            }
            for(int i=0;i<inner.Length;i++)
            {
                int n=(i+1)%inner.Length;
                builder.Quad(V(polygon[i],-thickness*.5f+inset),V(polygon[n],-thickness*.5f+inset),
                    V(polygon[n],thickness*.5f-inset),V(polygon[i],thickness*.5f-inset),2);
            }
            return builder.Mesh();
        }
        private static Vector3 V(Vector2 point,float z)=>new Vector3(point.x,point.y,z);
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private static List<Vector3Int> Triangulate(Vector2[] points)
        {
            var indices=new List<int>();for(int i=0;i<points.Length;i++)indices.Add(i);
            var triangles=new List<Vector3Int>();int safety=points.Length*points.Length;
            while(indices.Count>2 && safety-->0)
            {
                bool found=false;
                for(int j=0;j<indices.Count;j++)
                {
                    int a=indices[(j+indices.Count-1)%indices.Count],b=indices[j],c=indices[(j+1)%indices.Count];
                    if(Cross(points[b]-points[a],points[c]-points[b])<=0)continue;
                    bool contains=false;
                    foreach(int k in indices)
                        if(k!=a&&k!=b&&k!=c&&Cross(points[b]-points[a],points[k]-points[a])>=0&&
                            Cross(points[c]-points[b],points[k]-points[b])>=0&&Cross(points[a]-points[c],points[k]-points[c])>=0){contains=true;break;}
                    if(contains)continue;
                    triangles.Add(new Vector3Int(a,b,c));indices.RemoveAt(j);found=true;break;
                }
                if(!found)throw new InvalidOperationException("Repair art polygon could not be triangulated");
            }
            if(indices.Count>2)throw new InvalidOperationException("Repair art triangulation incomplete");
            return triangles;
        }
        // Rings contain center-x, center-y and radius; the axis is +Y.
        private static Mesh Tube(Vector3[] rings,int segments)
        {
            var builder=new MeshParts();
            Vector3 Point(int ring,int segment){var a=segment/(float)segments*Mathf.PI*2;return new Vector3(rings[ring].x+Mathf.Cos(a)*rings[ring].z,rings[ring].y,Mathf.Sin(a)*rings[ring].z);}
            for(int ring=0;ring<rings.Length-1;ring++)for(int n=0;n<segments;n++)
                builder.Quad(Point(ring,n),Point(ring+1,n),Point(ring+1,n+1),Point(ring,n+1),n%5==0?1:0);
            for(int n=0;n<segments;n++)
            {
                builder.Triangle(new Vector3(rings[0].x,rings[0].y,0),Point(0,n),Point(0,n+1),2);
                int last=rings.Length-1;
                builder.Triangle(new Vector3(rings[last].x,rings[last].y,0),Point(last,n+1),Point(last,n),2);
            }
            return builder.Mesh();
        }
        private sealed class MeshParts
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<Vector2> uv=new List<Vector2>();
            private readonly List<int>[] triangles={new List<int>(),new List<int>(),new List<int>()};
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,int material)
            {
                int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                uv.Add(new Vector2(a.x,a.y));uv.Add(new Vector2(b.x,b.y));uv.Add(new Vector2(c.x,c.y));
                triangles[material].Add(start);triangles[material].Add(start+1);triangles[material].Add(start+2);
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material)
            {Triangle(a,b,c,material);Triangle(a,c,d,material);}
            public Mesh Mesh()
            {
                var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=3;
                for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
        private static void WriteImpact()
        {
            const int rate=22050,count=3528;
            using(var bytes=new MemoryStream())using(var writer=new BinaryWriter(bytes))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
                writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                uint seed=917;
                for(int i=0;i<count;i++)
                {
                    seed=seed*1664525+1013904223;float t=i/(float)rate;
                    float noise=((seed>>8)&65535)/32768f-1;
                    float value=noise*Mathf.Exp(-t*95)*.48f+Mathf.Sin(2*Mathf.PI*420*t)*Mathf.Exp(-t*38)*.23f+
                        Mathf.Sin(2*Mathf.PI*1510*t)*Mathf.Exp(-t*70)*.10f;
                    writer.Write((short)(Mathf.Clamp(value,-1,1)*short.MaxValue));
                }
                var path=Root+"/OakNailImpact.wav";var output=bytes.ToArray();
                bool same=File.Exists(path)&&Convert.ToBase64String(File.ReadAllBytes(path))==Convert.ToBase64String(output);
                if(!same)File.WriteAllBytes(path,output);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
