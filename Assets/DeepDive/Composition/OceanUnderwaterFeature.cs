using UnityEngine.Rendering.Universal;

namespace DeepDive.Composition
{
    // Uses URP 17's own RenderGraph-compatible fullscreen implementation.
    public sealed class OceanUnderwaterFeature : FullScreenPassRendererFeature
    {
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            var ocean=OceanPresentation.Active;
            if(ocean==null || !ocean.IsReady || data.cameraData.camera.transform.position.y>ocean.SurfaceY+.4f)return;
            base.AddRenderPasses(renderer,ref data);
        }
    }
}
