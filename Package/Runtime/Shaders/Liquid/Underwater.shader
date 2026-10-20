Shader "Hidden/HiddenBull/Style Underwater"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Style Underwater"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HiddenBullUnderwaterFragment
            #pragma target 3.5

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define HB_LIQUID_UNDERWATER
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Style Underwater Line"

            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment HiddenBullUnderwaterLineFragment
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define HB_LIQUID_UNDERWATER
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
