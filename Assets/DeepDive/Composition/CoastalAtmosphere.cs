using UnityEngine;
using UnityEngine.Rendering;
using DeepDive.Network;

namespace DeepDive.Composition
{
    // Camera-local presentation. This never changes the host water/oxygen classification.
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
        private void OnEnable()
        {
            originalFog = RenderSettings.fog; originalFogColor = RenderSettings.fogColor; originalDensity = RenderSettings.fogDensity;
            originalAmbient = RenderSettings.ambientLight; originalMode = RenderSettings.ambientMode;
            originalSky = RenderSettings.skybox; sun = RenderSettings.sun;
            if (sun != null) { originalSunColor = sun.color; originalSunIntensity = sun.intensity; originalSunRotation = sun.transform.rotation; }
            var water = FindFirstObjectByType<SwimVolume>();
            if (water != null) surfaceY = water.GetComponent<Collider>().bounds.max.y;
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }
        private void BeginCamera(ScriptableRenderContext context, Camera camera) => ApplyTo(camera);
        public void ApplyTo(Camera camera)
        {
            var depth = Mathf.Max(0, surfaceY - camera.transform.position.y);
            var submerged = depth > .08f;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = submerged ? Mathf.Lerp(.03f, .065f, depth / 8) : .009f;
            RenderSettings.fogColor = submerged ? new Color(.04f, .30f, .36f) : new Color(.54f, .72f, .78f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = submerged ? new Color(.26f, .48f, .52f) : new Color(.64f, .69f, .72f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = submerged ? RenderSettings.fogColor : new Color(.43f, .66f, .78f);
            if (sun != null)
            {
                sun.color = submerged ? new Color(.61f, .85f, .90f) : new Color(1f, .89f, .73f);
                sun.intensity = submerged ? 1.0f : 1.6f;
                sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderSettings.fog = originalFog; RenderSettings.fogColor = originalFogColor; RenderSettings.fogDensity = originalDensity;
            RenderSettings.ambientMode = originalMode; RenderSettings.ambientLight = originalAmbient; RenderSettings.skybox = originalSky;
            if (sun != null) { sun.color = originalSunColor; sun.intensity = originalSunIntensity; sun.transform.rotation = originalSunRotation; }
        }
    }
}
