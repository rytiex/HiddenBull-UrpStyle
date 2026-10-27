Shader "Hidden/HB URP Style/Atmosphere"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Atmosphere"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_AtmosphereVertex
            #pragma fragment HB_AtmosphereFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Atmosphere/AtmospherePass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
