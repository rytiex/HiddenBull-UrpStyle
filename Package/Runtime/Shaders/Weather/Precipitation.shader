Shader "Hidden/HB URP Style/Precipitation"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Rain"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_RainVertex
            #pragma fragment HB_RainFragment

            #pragma multi_compile_vertex _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Snow"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_SnowVertex
            #pragma fragment HB_SnowFragment

            #pragma multi_compile_vertex _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Far Rain"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_FarVertex
            #pragma fragment HB_FarRainFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Far Snow"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_FarVertex
            #pragma fragment HB_FarSnowFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #define HB_FAR_SNOW
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Wind"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_WindVertex
            #pragma fragment HB_WindFragment

            #pragma multi_compile_vertex _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationWindPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
