using UnityEngine;

namespace DeepDive.Composition
{
    // Shared by the optical camera boundary and the GPU surface. Never used by movement.
    public static class OceanVisualMath
    {
        public static float WaveHeight(Vector2 point,float time,float wind)
        {
            var shore=Mathf.SmoothStep(.16f,1,Mathf.InverseLerp(-12,-2,point.y));
            var strength=Mathf.Lerp(.55f,1,Mathf.Clamp01(wind));
            return shore*strength*(.105f*Mathf.Sin(Vector2.Dot(point,new Vector2(.48f,.19f))-time*1.15f)+
                .045f*Mathf.Sin(Vector2.Dot(point,new Vector2(-.25f,.73f))-time*1.7f)+
                .02f*Mathf.Sin(Vector2.Dot(point,new Vector2(1.4f,.7f))-time*2.15f));
        }
        public static Mesh BuildSurface(int cells=192,float extent=1400)
        {
            cells=Mathf.Clamp(cells,16,240);
            var vertices=new Vector3[(cells+1)*(cells+1)];var uv=new Vector2[vertices.Length];
            var triangles=new int[cells*cells*6];var at=0;
            for(var z=0;z<=cells;z++)for(var x=0;x<=cells;x++)
            {
                var px=2f*x/cells-1;var pz=2f*z/cells-1;var i=z*(cells+1)+x;
                vertices[i]=new Vector3(Mathf.Sign(px)*Mathf.Pow(Mathf.Abs(px),2.3f)*extent,0,
                    Mathf.Sign(pz)*Mathf.Pow(Mathf.Abs(pz),2.3f)*extent);
                uv[i]=new Vector2((float)x/cells,(float)z/cells);
                if(x==cells||z==cells)continue;
                triangles[at++]=i;triangles[at++]=i+cells+1;triangles[at++]=i+1;
                triangles[at++]=i+1;triangles[at++]=i+cells+1;triangles[at++]=i+cells+2;
            }
            var mesh=new Mesh{name="Ocean visual grid (no physics)"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
            mesh.RecalculateNormals();mesh.bounds=new Bounds(Vector3.zero,new Vector3(extent*2,2,extent*2));return mesh;
        }
    }
}
