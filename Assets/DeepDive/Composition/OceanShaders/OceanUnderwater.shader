Shader "Hidden/DeepDive/Ocean Underwater"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline"}
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "OceanCommon.hlsl"
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=input.texcoord;float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 position=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 ray=normalize(position-_WorldSpaceCameraPos);float distanceToScene=min(90,distance(position,_WorldSpaceCameraPos));
                float3 nearPoint=_WorldSpaceCameraPos+ray*_ProjectionParams.y;
                float level=_OceanSurfaceY+OceanHeight(nearPoint.xz);
                float boundary=level-nearPoint.y;float wet=smoothstep(-.028,.028,boundary);
                float surfaceDistance=ray.y>.001?max(0,(level-_WorldSpaceCameraPos.y)/ray.y):90;
                float path=min(distanceToScene,surfaceDistance);
                float2 distortion=float2(sin(uv.y*40+_OceanTime*.7),cos(uv.x*35+_OceanTime*.5))*.00065*wet;
                float3 scene=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+distortion).rgb;
                float cameraDepth=max(0,_OceanSurfaceY-_WorldSpaceCameraPos.y);
                float sunlight=lerp(.12,1,_OceanDaylight)*exp(-cameraDepth*.025);
                float3 trans=exp(-float3(.09,.036,.023)*path);
                float3 water=scene*trans+float3(.018,.16,.205)*sunlight*(1-trans);
                float3 normal=normalize(cross(ddy(position),ddx(position)));
                float floorDepth=max(0,_OceanSurfaceY-position.y);
                float caustic=OceanCaustic(position.xz*1.15);
                float floorMask=saturate(normal.y)*exp(-floorDepth*.10)*step(.2,floorDepth)*step(distanceToScene,80);
                water+=caustic*floorMask*float3(.09,.16,.12)*sunlight;
                float rays=0;
                [unroll]for(int i=0;i<4;i++)
                {
                    float3 samplePoint=_WorldSpaceCameraPos+ray*(path*(i+.5)/4);
                    float d=max(0,_OceanSurfaceY-samplePoint.y);
                    float2 top=samplePoint.xz-_OceanSunDirection.xz*d/max(.2,_OceanSunDirection.y);
                    rays+=smoothstep(.48,.8,OceanNoise(top*.4+_OceanTime*.035))*exp(-d*.09);
                }
                water+=rays*.008*sunlight*saturate(path*.1)*float3(.45,.85,1);
                float rim=1-smoothstep(.002,.022,abs(boundary));
                float3 result=lerp(scene,water,wet);
                result=lerp(result,result*.65+float3(.06,.13,.14),rim*.35);
                return half4(result,1);
            }
            ENDHLSL
        }
    }
}
