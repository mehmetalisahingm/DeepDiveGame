using DeepDive.Core.Contracts;
using DeepDive.Economy;
using DeepDive.Network;
using DeepDive.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepDive.Composition
{
    // Camera-local presentation. This never changes the host water/oxygen classification.
    // P4.1 daylight is derived from the already replicated shared day; no second clock or light
    // authority is introduced here.
    public sealed class CoastalAtmosphere : MonoBehaviour
    {
        private bool originalFog;
        private Color originalFogColor, originalAmbient;
        private float originalDensity;
        private AmbientMode originalMode;
        private Material originalSky;
        private Color originalSunColor;
        private float originalSunIntensity;
        private Quaternion originalSunRotation;
        private Light sun;
        private float surfaceY = 8;
        private EconomyPlayerSync daySource;
        private OceanPresentation ocean;
        private Color originalAmbientSky,originalAmbientEquator,originalAmbientGround;
#if UNITY_EDITOR
        public int PreviewMinute=-1;
#endif

        private void OnEnable()
        {
            originalFog = RenderSettings.fog; originalFogColor = RenderSettings.fogColor; originalDensity = RenderSettings.fogDensity;
            originalAmbient = RenderSettings.ambientLight; originalMode = RenderSettings.ambientMode;
            originalAmbientSky=RenderSettings.ambientSkyColor;originalAmbientEquator=RenderSettings.ambientEquatorColor;originalAmbientGround=RenderSettings.ambientGroundColor;
            originalSky = RenderSettings.skybox; sun = RenderSettings.sun;
            if (sun != null) { originalSunColor = sun.color; originalSunIntensity = sun.intensity; originalSunRotation = sun.transform.rotation; }
            var water = FindFirstObjectByType<SwimVolume>();
            if (water != null) surfaceY = water.GetComponent<Collider>().bounds.max.y;
            var installOcean=true;
#if UNITY_EDITOR
            installOcean=!OceanPresentation.SuppressForPreview;
#endif
            if(installOcean)
            {
                ocean=GetComponent<OceanPresentation>();if(ocean==null)ocean=gameObject.AddComponent<OceanPresentation>();
                if(!Application.isPlaying)ocean.hideFlags=HideFlags.DontSave;
                ocean.Initialize();
            }
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }

        private void BeginCamera(ScriptableRenderContext context, Camera camera) => ApplyTo(camera);

        public void ApplyTo(Camera camera)
        {
            if (camera == null || camera.cameraType==CameraType.Reflection) return;
            var daylight = SharedDaylight();
            var windy = daySource != null && daySource.IsSpawned &&
                P45WorldRules.Weather(daySource.DayNumber.Value, daySource.DayWeatherSeed.Value) == P45WeatherKind.Windy;
            if(ocean!=null && ocean.IsReady)
            {
                var opticalDepth=Mathf.Max(0,surfaceY-camera.transform.position.y);
                var below=opticalDepth>.08f;
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
                RenderSettings.fogDensity=below ? .006f : (windy ? .004f : .0025f);
                RenderSettings.fogColor=ScaleRgb(below?new Color(.025f,.17f,.21f):new Color(.64f,.77f,.83f),daylight.FogColorMultiplier);
                RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=ScaleRgb(new Color(.48f,.66f,.82f),daylight.AmbientMultiplier);
                RenderSettings.ambientEquatorColor=ScaleRgb(new Color(.30f,.42f,.46f),daylight.AmbientMultiplier);
                RenderSettings.ambientGroundColor=ScaleRgb(new Color(.14f,.18f,.16f),daylight.AmbientMultiplier);
                camera.clearFlags=CameraClearFlags.Skybox;
                if(sun!=null)
                {
                    var minute=daySource!=null?daySource.DayClockMinute.Value:DayIds.DayStartMinute;
#if UNITY_EDITOR
                    if(PreviewMinute>=0)minute=PreviewMinute;
#endif
                    var progress=Mathf.Clamp01((minute-360)/900f);
                    var elevation=Mathf.Sin(progress*Mathf.PI)*68;
                    if(minute<360 || minute>1260)elevation=-25;
                    sun.transform.rotation=Quaternion.Euler(elevation,Mathf.Lerp(-100,100,progress),0);
                    sun.color=Color.Lerp(new Color(1,.64f,.40f),new Color(1,.95f,.86f),Mathf.Clamp01(elevation/48));
                    sun.intensity=(windy?1.6f:2.1f)*daylight.SunIntensityMultiplier;
                }
                ocean.ConfigureCamera(camera,daylight.DaylightFactor,windy);
                return;
            }
            var depth = Mathf.Max(0, surfaceY - camera.transform.position.y);
            var submerged = depth > .08f;
            var baseFog = submerged ? new Color(.04f, .30f, .36f) : new Color(.54f, .72f, .78f);
            var baseAmbient = submerged ? new Color(.26f, .48f, .52f) : new Color(.64f, .69f, .72f);
            var baseBackground = submerged ? baseFog : new Color(.43f, .66f, .78f);

            RenderSettings.fog = true;
            RenderSettings.fogDensity = submerged ? Mathf.Lerp(.03f, .065f, depth / 8) :
                windy ? .016f : .009f;
            RenderSettings.fogColor = ScaleRgb(baseFog, daylight.FogColorMultiplier);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ScaleRgb(baseAmbient, daylight.AmbientMultiplier);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ScaleRgb(baseBackground, daylight.FogColorMultiplier);
            if (sun != null)
            {
                sun.color = submerged ? new Color(.61f, .85f, .90f) : new Color(1f, .89f, .73f);
                sun.intensity = (submerged ? 1.0f : (windy ? 1.2f : 1.6f)) * daylight.SunIntensityMultiplier;
                sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }
        }

        private DaylightState SharedDaylight()
        {
#if UNITY_EDITOR
            if(PreviewMinute>=0)return DaylightModel.Evaluate(PreviewMinute,DayPhase.Running,0);
#endif
            if (daySource == null || !daySource.IsSpawned)
            {
                daySource = null;
                var syncs = FindObjectsByType<EconomyPlayerSync>(FindObjectsSortMode.None);
                for (var i = 0; i < syncs.Length; i++)
                {
                    if (!syncs[i].IsSpawned) continue;
                    if (daySource == null) daySource = syncs[i];
                    if (syncs[i].IsOwner) { daySource = syncs[i]; break; }
                }
            }

            return daySource != null
                ? DaylightModel.Evaluate(daySource.DayClockMinute.Value, (DayPhase)daySource.DayPhaseValue.Value,
                    daySource.DayWeatherSeed.Value)
                : DaylightModel.Evaluate(DayIds.DayStartMinute, DayPhase.Running, 0);
        }

        private static Color ScaleRgb(Color color, float multiplier) =>
            new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderSettings.fog = originalFog; RenderSettings.fogColor = originalFogColor; RenderSettings.fogDensity = originalDensity;
            RenderSettings.ambientMode = originalMode; RenderSettings.ambientLight = originalAmbient; RenderSettings.skybox = originalSky;
            RenderSettings.ambientSkyColor=originalAmbientSky;RenderSettings.ambientEquatorColor=originalAmbientEquator;RenderSettings.ambientGroundColor=originalAmbientGround;
            if (sun != null) { sun.color = originalSunColor; sun.intensity = originalSunIntensity; sun.transform.rotation = originalSunRotation; }
            daySource = null;
        }
    }
}
