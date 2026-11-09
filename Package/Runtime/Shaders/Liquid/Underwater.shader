Shader "Hidden/HB URP Style/Underwater"
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
            Name "Underwater"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_UnderVertex
            #pragma fragment HB_UnderFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/UnderwaterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "UnderwaterOverlay"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_UnderVertex
            #pragma fragment HB_UnderOverlayFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/UnderwaterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
