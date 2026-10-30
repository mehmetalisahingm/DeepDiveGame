using System.IO;
using System.Linq;
using DeepDive.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeepDive.Editor
{
    public static class OceanVisualSetup
    {
        private const string ResourcesPath="Assets/DeepDive/Composition/Resources";
        [MenuItem("DeepDive/P4.11/Build ocean presentation")]
        public static void Generate()
        {
            var water=Material("OceanSurface","DeepDive/Ocean Surface");water.SetVector("_Absorption",new Vector4(.23f,.13f,.085f,0));
            water.SetColor("_DeepColor",new Color(.009f,.12f,.17f));
            Material("OceanSky","DeepDive/Ocean Sky");Material("OceanSeabed","DeepDive/Ocean Seabed");
            Material("OceanParticles","DeepDive/Ocean Particles");var post=Material("OceanUnderwater","Hidden/DeepDive/Ocean Underwater");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/P0RenderPipeline.asset");
            pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=true;EditorUtility.SetDirty(pipeline);
            GraphicsSettings.lightsUseLinearIntensity=true;GraphicsSettings.lightsUseColorTemperature=true;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/P0Renderer.asset");
            OceanUnderwaterFeature feature=null;
            foreach(var item in renderer.rendererFeatures)if(item is OceanUnderwaterFeature f)feature=f;
            if(feature==null)
            {feature=ScriptableObject.CreateInstance<OceanUnderwaterFeature>();feature.name="Ocean optical volume";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
            feature.passMaterial=post;feature.requirements=ScriptableRenderPassInput.Depth;
            feature.fetchColorBuffer=true;feature.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.SetActive(true);feature.Create();EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("OCEAN_PRESENTATION_GENERATED");
        }
        private static Material Material(string name,string shaderName)
        {
            if(!AssetDatabase.IsValidFolder(ResourcesPath))AssetDatabase.CreateFolder("Assets/DeepDive/Composition","Resources");
            var path=ResourcesPath+"/"+name+".mat";var shader=Shader.Find(shaderName);
            if(shader==null)throw new System.InvalidOperationException("Ocean shader missing: "+shaderName);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            else material.shader=shader;
            EditorUtility.SetDirty(material);return material;
        }
        public static void GenerateAndPreview(){Generate();Preview();}
        public static void Preview()
        {
            var scene=EditorSceneManager.OpenScene("Assets/DeepDive/World/Scenes/DiveTestArea.unity");
            var atmosphere=Object.FindFirstObjectByType<CoastalAtmosphere>();
            if(atmosphere==null)atmosphere=new GameObject("OceanPreviewAtmosphere").AddComponent<CoastalAtmosphere>();
            Directory.CreateDirectory("Logs/OceanPreview");
            OceanPresentation.SuppressForPreview=true;atmosphere.SendMessage("OnEnable");
            Take(atmosphere,"before-shore",new Vector3(3,10.4f,-13),new Vector3(0,8,25));
            atmosphere.SendMessage("OnDisable");OceanPresentation.SuppressForPreview=false;atmosphere.SendMessage("OnEnable");
            var ocean=atmosphere.GetComponent<OceanPresentation>();ocean.PreviewTime=3;
            Take(atmosphere,"after-shore",new Vector3(3,10.4f,-13),new Vector3(0,8,25));
            Take(atmosphere,"after-low-surface",new Vector3(0,8.55f,1),new Vector3(0,8,40));
            Take(atmosphere,"after-underwater",new Vector3(-6,5.2f,-2),new Vector3(0,1,13));
            Take(atmosphere,"after-snells-window",new Vector3(0,4,3),new Vector3(0,12,12));
            atmosphere.PreviewMinute=22*60;
            Take(atmosphere,"after-night",new Vector3(3,10.4f,-13),new Vector3(0,8,25));
            atmosphere.PreviewMinute=12*60;ocean.PreviewWind=1;ocean.PreviewTime=8;
            Take(atmosphere,"after-wind",new Vector3(0,8.7f,1),new Vector3(0,8,40));
            atmosphere.SendMessage("OnDisable");ocean.SendMessage("OnDisable");
            foreach(var name in new[]{"DeepDive/Ocean Surface","DeepDive/Ocean Sky","Hidden/DeepDive/Ocean Underwater","DeepDive/Ocean Particles","DeepDive/Ocean Seabed"})
            {
                var errors=ShaderUtil.GetShaderMessages(Shader.Find(name)).Where(m=>m.severity.ToString()=="Error").ToArray();
                if(errors.Length>0)throw new System.InvalidOperationException(name+": "+string.Join("; ",errors.Select(e=>e.message)));
            }
            Debug.Log("OCEAN_VISUAL_PREVIEW_COMPLETE");
        }
        private static void Take(CoastalAtmosphere atmosphere,string name,Vector3 position,Vector3 target)
        {
            var go=new GameObject("OceanPreviewCapture");go.transform.position=position;go.transform.LookAt(target);
            var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.04f;camera.farClipPlane=2000;camera.fieldOfView=65;
            atmosphere.ApplyTo(camera);var ocean=atmosphere.GetComponent<OceanPresentation>();
            if(!OceanPresentation.SuppressForPreview && position.y>8.1f)ocean.RenderReflection(camera);
            var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {rt.Create();camera.targetTexture=rt;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
                RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Logs/OceanPreview/"+name+".png",texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        }
    }
}
