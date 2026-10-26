Shader "DeepDive/Ocean Surface"
{
    Properties { _Absorption("Absorption RGB",Vector)=(.23,.13,.085,0) _DeepColor("Deep scatter",Color)=(.009,.12,.17,1) }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent-20" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Name "OceanForward" Tags {"LightMode"="UniversalForward"}
            Blend One Zero ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "OceanCommon.hlsl"
            TEXTURE2D(_OceanReflection);SAMPLER(sampler_OceanReflection);
            CBUFFER_START(UnityPerMaterial)
                float4 _Absorption,_DeepColor;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float fog:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                float fade=1-smoothstep(120,500,distance(o.positionWS.xz,_WorldSpaceCameraPos.xz));
                o.positionWS.y+=OceanHeight(o.positionWS.xz)*fade;
                o.positionCS=TransformWorldToHClip(o.positionWS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            float3 Reconstruct(float2 uv,float depth)
            {
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                return ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float3 n=OceanNormal(input.positionWS.xz);
                float3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                bool above=_WorldSpaceCameraPos.y>input.positionWS.y;
                if(!above)n=-n;
                float raw=SampleSceneDepth(uv);float3 behind=Reconstruct(uv,raw);
                float thickness=min(80,distance(behind,input.positionWS));
                float waterEye=-TransformWorldToView(input.positionWS).z;
                float2 bend=n.xz*.008*saturate(thickness*.25);
                float2 refractedUV=saturate(uv+bend);
                if(LinearEyeDepth(SampleSceneDepth(refractedUV),_ZBufferParams)<waterEye)refractedUV=uv;
                float3 scene=SampleSceneColor(refractedUV);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float daylight=lerp(.14,1,_OceanDaylight);
                float3 transmission=exp(-_Absorption.rgb*thickness);
                float3 scatter=_DeepColor.rgb*daylight;
                float3 water=scene*transmission+scatter*(1-transmission);
                if(!above)water=scene;
                float nv=saturate(dot(n,view));
                float fresnel=.0204+.9796*pow(1-nv,5);
                if(!above)fresnel=max(fresnel,1-smoothstep(.61,.69,nv));
                float3 reflected=above?OceanSky(reflect(-view,n)):scatter*.8;
                float4 projected=mul(_OceanReflectionVP,float4(input.positionWS,1));
                float2 ruv=projected.xy/max(.001,projected.w)*.5+.5+n.xz*.018;
                if(above && _OceanReflectionValid>.5 && projected.w>0 && all(ruv>0) && all(ruv<1))
                    reflected=lerp(reflected,SAMPLE_TEXTURE2D(_OceanReflection,sampler_OceanReflection,ruv).rgb,.82);
                float3 color=lerp(water,reflected,fresnel);
                float3 h=normalize(view+sun.direction);
                float sparkle=pow(saturate(dot(n,h)),lerp(320,100,_OceanWind))*2.8;
                color+=sparkle*sun.color*sun.shadowAttenuation;
                float noise=OceanNoise(input.positionWS.xz*3+_OceanTime*float2(.11,-.07));
                float contact=1-smoothstep(.05,.55,thickness);
                float wash=(1-smoothstep(.3,2.6,thickness))*pow(saturate(sin(thickness*3-_OceanTime*1.4+noise)*.5+.5),8);
                float foam=saturate(contact*.75+wash*.8+OceanWake(input.positionWS.xz))*smoothstep(.28,.7,noise);
                if(!above)foam*=.18;
                color=lerp(color,float3(.87,.94,.92)*daylight,foam);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
