Shader "Hidden/HiddenBull/Style Grass"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GrassDepth"

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HiddenBullGrassVertex
            #pragma fragment HiddenBullGrassDepthFragment

            #pragma multi_compile_local _ _HB_GRASS_FLOWERS _HB_GRASS_FAR

            #define HB_GRASS_DEPTH
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Grass"

            Cull Off
            ZWrite Off
            ZTest Equal

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HiddenBullGrassVertex
            #pragma fragment HiddenBullGrassFragment

            #pragma multi_compile_local _ _HB_GRASS_FLOWERS _HB_GRASS_FAR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrassPass.hlsl"
            ENDHLSL
        }
    }
}
