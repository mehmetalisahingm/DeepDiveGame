using System.Linq;
using DeepDive.Composition;
using DeepDive.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DeepDive.P4.Tests
{
    public sealed class OceanPresentationTests
    {
        [Test]
        public void WavesStayFiniteAndBoundedAcrossCoastAndOpenWater()
        {
            for(var wind=0;wind<=1;wind++)for(var time=0;time<120;time++)for(var z=-16;z<90;z+=3)
            {
                var h=OceanVisualMath.WaveHeight(new Vector2(z*.31f,z),time*.37f,wind);
                Assert.That(h,Is.InRange(-.1701f,.1701f));Assert.That(float.IsNaN(h),Is.False);
            }
        }
        [Test]
        public void WaveAnimationChangesWithTimeAndWindButHasNoRandomAuthority()
        {
            var p=new Vector2(4,28);var h=OceanVisualMath.WaveHeight(p,3,1);
            Assert.That(OceanVisualMath.WaveHeight(p,3,1),Is.EqualTo(h));
            Assert.That(Mathf.Abs(OceanVisualMath.WaveHeight(p,4,1)-h),Is.GreaterThan(.001f));
            Assert.That(Mathf.Abs(OceanVisualMath.WaveHeight(p,3,0)),Is.LessThan(Mathf.Abs(h)));
        }
        [Test]
        public void AdaptiveGridCoversTheHorizonWithoutExceeding16BitIndices()
        {
            var mesh=OceanVisualMath.BuildSurface();
            try
            {
                Assert.That(mesh.vertexCount,Is.LessThan(65535));
                Assert.That(mesh.bounds.size.x,Is.GreaterThan(2500));Assert.That(mesh.bounds.size.y,Is.GreaterThan(.34f));
                Assert.That(mesh.triangles.Max(),Is.LessThan(mesh.vertexCount));
                Assert.That(mesh.normals.All(n=>n.y>.99f),Is.True);
            }finally{Object.DestroyImmediate(mesh);}
        }
        [Test]
        public void OceanResourcesAndDepthColorPassAreActuallyBoundToTheShippingRenderer()
        {
            foreach(var name in new[]{"OceanSurface","OceanSky","OceanUnderwater","OceanParticles","OceanSeabed"})
            {var material=Resources.Load<Material>(name);Assert.That(material,Is.Not.Null,name);Assert.That(material.shader.isSupported,Is.True,name);}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/P0RenderPipeline.asset");
            Assert.That(pipeline.supportsCameraDepthTexture,Is.True);Assert.That(pipeline.supportsCameraOpaqueTexture,Is.True);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/P0Renderer.asset");
            var feature=renderer.rendererFeatures.OfType<OceanUnderwaterFeature>().Single();
            Assert.That(feature.isActive,Is.True);Assert.That(feature.passMaterial,Is.EqualTo(Resources.Load<Material>("OceanUnderwater")));
        }
        [TestCase(LocomotionMode.Land)]
        [TestCase(LocomotionMode.Seated)]
        [TestCase(LocomotionMode.Passive)]
        public void SurfaceEyeNeverLiftsDrySeatedOrPassivePlayers(LocomotionMode mode)
        {Assert.That(SurfaceCameraRules.Lift(mode,8,8),Is.Zero);}
        [Test]
        public void EyesRiseContinuouslyThroughTheRealLineAndReturnToNormalAtDepth()
        {
            Assert.That(SurfaceCameraRules.Lift(LocomotionMode.Underwater,7.5f,8),Is.Zero);
            var lower=SurfaceCameraRules.Lift(LocomotionMode.Underwater,7.8f,8);
            var surface=SurfaceCameraRules.Lift(LocomotionMode.Surface,8,8);
            Assert.That(lower,Is.GreaterThan(0).And.LessThan(surface));
            Assert.That(6.2f+1.55f+surface,Is.GreaterThan(8));
            Assert.That(SurfaceCameraRules.Lift(LocomotionMode.Surface,8,float.NaN),Is.Zero);
        }
        [Test]
        public void WadingUpTheRampReturnsToTheDryEyeWithoutA35CmStep()
        {
            var prior=SurfaceCameraRules.Lift(LocomotionMode.Surface,8,8);
            for(var top=8.01f;top<9.85f;top+=.01f)
            {
                var lift=SurfaceCameraRules.Lift(top<9.8f?LocomotionMode.Surface:LocomotionMode.Land,top,8);
                Assert.That(Mathf.Abs(lift-prior),Is.LessThan(.02f));prior=lift;
            }
            Assert.That(prior,Is.Zero);
        }
    }
}
