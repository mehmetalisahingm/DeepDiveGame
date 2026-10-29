using System.Collections.Generic;
using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.Trip;
using DeepDive.World;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Composition
{
    [DefaultExecutionOrder(300)]
    public sealed class OceanPresentation : MonoBehaviour
    {
        public static OceanPresentation Active {get;private set;}
        public bool IsReady=>surface!=null && sky!=null;
        public float SurfaceY {get;private set;}=8;
        public float VisualTime {get;private set;}
        public float Wind {get;private set;}
        public bool ReflectionReady=>reflectionTarget!=null && reflectionValid;
        public Mesh SurfaceMesh=>mesh;
#if UNITY_EDITOR
        public static bool SuppressForPreview;
        public float PreviewTime=-1,PreviewWind=-1;
#endif
        private Mesh mesh;
        private GameObject surface;
        private Material water,sky,particleMaterial,seabed;
        private readonly Dictionary<Renderer,Material[]> originalBeds=new Dictionary<Renderer,Material[]>();
        private Material originalSky;
        private ParticleSystem motes;
        private Camera reflectionCamera,ownerCamera;
        private RenderTexture reflectionTarget;
        private bool reflecting,reflectionValid;
        private float nextReflection,nextActors;
        private readonly Dictionary<Renderer,bool> originalSurfaces=new Dictionary<Renderer,bool>();
        private readonly Dictionary<ulong,Vector3> playerPrevious=new Dictionary<ulong,Vector3>();
        private readonly Vector4[] actors=new Vector4[8],velocities=new Vector4[8];
        private Vector3 previousBoat;
        private bool haveBoat;
        private float actorAt;
        private NetworkPlayer owner;

        private void OnEnable()=>Initialize();
        public void Initialize()
        {
            if(IsReady)return;
            var source=Resources.Load<Material>("OceanSurface");var skySource=Resources.Load<Material>("OceanSky");
            if(source==null||skySource==null)return;
            Active=this;originalSky=RenderSettings.skybox;
            water=new Material(source){name="Live ocean"};sky=new Material(skySource){name="Live coastal sky"};
            var volume=FindFirstObjectByType<SwimVolume>();
            if(volume!=null)SurfaceY=volume.GetComponent<Collider>().bounds.max.y;
            foreach(var name in new[]{"WaterSurface","WaterSurface_North"})
            {
                var old=GameObject.Find(name)?.GetComponent<Renderer>();
                if(old!=null){originalSurfaces[old]=old.enabled;old.enabled=false;}
            }
            surface=new GameObject("Ocean_VisualSurface"){hideFlags=HideFlags.DontSave};
            surface.transform.SetParent(transform,false);surface.transform.position=new Vector3(0,SurfaceY,25);
            mesh=OceanVisualMath.BuildSurface();surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=water;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var bedSource=Resources.Load<Material>("OceanSeabed");
            if(bedSource!=null)
            {
                seabed=new Material(bedSource);
                foreach(var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(r.name=="SeaFloor" || r.name.StartsWith("Wall_") || r.name=="Reef_Shelf" ||
                        r.name=="Reef_DropOff" || r.name=="Deep_DropOff" || r.name=="Deep_Basin" || r.name.StartsWith("Reef_Head_"))
                    {originalBeds[r]=r.sharedMaterials;r.sharedMaterial=seabed;}
            }
            RenderSettings.skybox=sky;
            Shader.SetGlobalFloat("_OceanSurfaceY",SurfaceY);Shader.SetGlobalFloat("_OceanReflectionValid",0);
            CreateMotes();
        }

        public void ConfigureCamera(Camera camera,float daylight,bool windy)
        {
            if(!IsReady || camera==null || camera.cameraType==CameraType.Reflection)return;
            Wind=windy?1:.25f;
            var manager=NetworkManager.Singleton;
            VisualTime=manager!=null && manager.IsListening?(float)manager.ServerTime.Time:Time.unscaledTime;
#if UNITY_EDITOR
            if(PreviewTime>=0)VisualTime=PreviewTime;
            if(PreviewWind>=0)Wind=PreviewWind;
#endif
            Shader.SetGlobalFloat("_OceanTime",VisualTime);Shader.SetGlobalFloat("_OceanWind",Wind);
            Shader.SetGlobalFloat("_OceanDaylight",daylight);Shader.SetGlobalFloat("_OceanSurfaceY",SurfaceY);
            var sun=RenderSettings.sun;
            Shader.SetGlobalVector("_OceanSunDirection",sun!=null?-sun.transform.forward:Vector3.up);
            Shader.SetGlobalVector("_OceanSunColor",sun!=null?(Vector4)(sun.color.linear*sun.intensity):Vector4.one);
            camera.farClipPlane=Mathf.Max(camera.farClipPlane,2000);
        }

        private void LateUpdate()
        {
            if(!IsReady)return;
            if(owner==null || !owner.IsSpawned)
                foreach(var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner){owner=p;break;}
            if(owner!=null)ownerCamera=owner.GetComponentInChildren<Camera>(true);
            if(ownerCamera==null)return;
            var atmosphere=GetComponent<CoastalAtmosphere>();if(atmosphere!=null)atmosphere.ApplyTo(ownerCamera);
            var position=ownerCamera.transform.position;
            if(motes!=null)
            {
                motes.transform.position=position+ownerCamera.transform.forward*5;
                if(position.y<SurfaceY-.2f){if(!motes.isPlaying)motes.Play();}
                else if(motes.isPlaying)motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if(Time.unscaledTime>=nextActors){UpdateActors();nextActors=Time.unscaledTime+.1f;}
            if(position.y>SurfaceY+.05f && Time.unscaledTime>=nextReflection)
            {RenderReflection(ownerCamera);nextReflection=Time.unscaledTime+.083f;}
        }

        private void UpdateActors()
        {
            var now=Time.unscaledTime;var dt=Mathf.Max(.05f,now-actorAt);actorAt=now;var count=0;
            foreach(var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if(!p.IsSpawned)continue;var pos=p.transform.position;
                p.SetVisualWaterSurface(SurfaceY);
                var velocity=playerPrevious.TryGetValue(p.OwnerClientId,out var old)?(pos-old)/dt:Vector3.zero;
                playerPrevious[p.OwnerClientId]=pos;
                if(p.Seated.Value || p.Passive.Value || pos.y<SurfaceY-2.2f || pos.y>SurfaceY+.1f || count>=7)continue;
                actors[count]=new Vector4(pos.x,pos.z,.45f,Mathf.Clamp01(velocity.magnitude*.3f));
                velocities[count++]=new Vector4(velocity.x,velocity.z,0,0);
            }
            var trip=owner!=null?owner.GetComponent<BoatTripPlayerSync>():null;
            if(trip!=null && trip.IsSpawned && Mathf.Abs(trip.BoatWorldPosition.Value.y-SurfaceY)<2 && count<8)
            {
                var pos=trip.BoatWorldPosition.Value;var velocity=haveBoat?(pos-previousBoat)/dt:Vector3.zero;
                previousBoat=pos;haveBoat=true;
                actors[count]=new Vector4(pos.x,pos.z,1.4f,Mathf.Clamp01(velocity.magnitude*.2f)+.05f);
                velocities[count++]=new Vector4(velocity.x,velocity.z,1,0);
            }
            Shader.SetGlobalVectorArray("_OceanActors",actors);Shader.SetGlobalVectorArray("_OceanVelocities",velocities);
            Shader.SetGlobalInt("_OceanActorCount",count);
        }

        public void RenderReflection(Camera source)
        {
            if(reflecting || !IsReady || source==null || source.transform.position.y<=SurfaceY)return;
            reflecting=true;var culling=GL.invertCulling;
            var hidden=new Dictionary<Renderer,bool>();var surfaceRenderer=surface.GetComponent<Renderer>();
            try
            {
                if(reflectionCamera==null)
                {
                    var go=new GameObject("Ocean_ReflectionCamera"){hideFlags=HideFlags.HideAndDontSave};
                    reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;
                    reflectionTarget=new RenderTexture(768,432,24,RenderTextureFormat.ARGBHalf){name="Ocean planar reflection",hideFlags=HideFlags.DontSave};
                    reflectionTarget.Create();
                }
                reflectionCamera.CopyFrom(source);reflectionCamera.enabled=false;reflectionCamera.cameraType=CameraType.Reflection;
                reflectionCamera.targetTexture=reflectionTarget;reflectionCamera.allowMSAA=false;
                var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*SurfaceY;
                reflectionCamera.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
                var pos=source.transform.position;reflectionCamera.transform.position=new Vector3(pos.x,2*SurfaceY-pos.y,pos.z);
                var plane=new Vector4(0,1,0,-SurfaceY-.03f);
                var clip=reflectionCamera.worldToCameraMatrix.inverse.transpose*plane;
                reflectionCamera.projectionMatrix=source.projectionMatrix;
                reflectionCamera.projectionMatrix=reflectionCamera.CalculateObliqueMatrix(clip);
                foreach(var r in source.GetComponentsInChildren<Renderer>(true)){hidden[r]=r.forceRenderingOff;r.forceRenderingOff=true;}
                surfaceRenderer.forceRenderingOff=true;GL.invertCulling=true;
                RenderPipeline.SubmitRenderRequest(reflectionCamera,new RenderPipeline.StandardRequest{destination=reflectionTarget});
                Shader.SetGlobalMatrix("_OceanReflectionVP",GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix,true)*reflectionCamera.worldToCameraMatrix);
                Shader.SetGlobalTexture("_OceanReflection",reflectionTarget);reflectionValid=true;
                Shader.SetGlobalFloat("_OceanReflectionValid",1);
            }
            finally
            {
                GL.invertCulling=culling;surfaceRenderer.forceRenderingOff=false;
                foreach(var item in hidden)if(item.Key!=null)item.Key.forceRenderingOff=item.Value;
                reflecting=false;
            }
        }

        private void CreateMotes()
        {
            var source=Resources.Load<Material>("OceanParticles");if(source==null)return;
            particleMaterial=new Material(source);
            var go=new GameObject("Ocean_SuspendedMotes"){hideFlags=HideFlags.DontSave};go.transform.SetParent(transform,false);
            motes=go.AddComponent<ParticleSystem>();var main=motes.main;
            main.playOnAwake=false;main.startLifetime=12;main.startSpeed=.025f;main.startSize=new ParticleSystem.MinMaxCurve(.012f,.035f);
            main.maxParticles=300;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=motes.emission;emission.rateOverTime=18;
            var shape=motes.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(14,10,16);
            var renderer=motes.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private static void Release(Object obj){if(obj==null)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
        private void OnDisable()
        {
            foreach(var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))player.SetVisualWaterSurface(float.NaN);
            foreach(var item in originalSurfaces)if(item.Key!=null)item.Key.enabled=item.Value;originalSurfaces.Clear();
            foreach(var item in originalBeds)if(item.Key!=null)item.Key.sharedMaterials=item.Value;originalBeds.Clear();
            if(RenderSettings.skybox==sky)RenderSettings.skybox=originalSky;
            if(reflectionTarget!=null)reflectionTarget.Release();
            Release(reflectionTarget);if(reflectionCamera!=null)Release(reflectionCamera.gameObject);
            if(motes!=null)Release(motes.gameObject);Release(particleMaterial);Release(seabed);Release(surface);Release(mesh);Release(water);Release(sky);
            surface=null;sky=null;reflectionValid=false;
            if(Active==this)Active=null;
            Shader.SetGlobalFloat("_OceanReflectionValid",0);Shader.SetGlobalInt("_OceanActorCount",0);
        }
    }
}
