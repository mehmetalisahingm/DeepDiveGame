Shader "DeepDive/Ocean Seabed"
{
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "OceanCommon.hlsl"
            struct Attributes {float4 p:POSITION;float3 n:NORMAL;};
            struct Varyings {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float fog:TEXCOORD2;};
            Varyings Vert(Attributes i){Varyings o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(Varyings i):SV_Target
            {
                float3 n=normalize(i.normal);float sand=saturate((n.y-.25)*2);
                float noise=OceanNoise(i.world.xz*.7)+OceanNoise(i.world.zy*2.3)*.35;
                float ripple=sin(i.world.x*11+OceanNoise(i.world.xz*.25)*6)*.035;
                float fine=(OceanNoise(i.world.xz*65)-.5)*.05/(1+length(fwidth(i.world.xz))*40);
                float3 albedo=lerp(float3(.18,.22,.20),float3(.49,.43,.31),sand)*( .75+noise*.25)+ripple*sand+fine;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                float3 lit=albedo*(sun.color*saturate(dot(n,sun.direction))*sun.shadowAttenuation+
                    float3(.16,.20,.23)*lerp(.2,1,_OceanDaylight));
                return half4(MixFog(lit,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 Vert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
            half4 Frag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A {float4 p:POSITION;float3 n:NORMAL;};
            float4 Vert(A i):SV_POSITION
            {
                float3 p=ApplyShadowBias(TransformObjectToWorld(i.p.xyz),TransformObjectToWorldNormal(i.n),_LightDirection);
                float4 clip=TransformWorldToHClip(p);
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 Frag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
