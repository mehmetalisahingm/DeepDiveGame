Shader "DeepDive/Ocean Particles"
{
    SubShader
    {
        Tags {"Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _OceanDaylight,_OceanSurfaceY;
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float y:TEXCOORD1;};
            Varyings Vert(Attributes i){Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;o.y=TransformObjectToWorld(i.positionOS.xyz).y;return o;}
            half4 Frag(Varyings i):SV_Target
            {clip(_OceanSurfaceY-i.y);float a=pow(saturate(1-length(i.uv*2-1)),2)*.22;return half4(float3(.55,.8,.86)*lerp(.2,1,_OceanDaylight),a*i.color.a);}
            ENDHLSL
        }
    }
}
