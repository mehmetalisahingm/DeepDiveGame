Shader "DeepDive/Ocean Sky"
{
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "OceanCommon.hlsl"
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.direction=TransformObjectToWorldDir(i.positionOS.xyz);return o;}
            half4 Frag(Varyings i):SV_Target{return half4(OceanSky(i.direction),1);}
            ENDHLSL
        }
    }
}
