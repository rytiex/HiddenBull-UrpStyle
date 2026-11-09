Shader "HB URP Style/Terrain Liquid"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-100"
        }

        Pass
        {
            Name "Liquid"
            Tags
            {
                "LightMode" = "HBStyleLiquid"
            }

            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LiquidVertex
            #pragma fragment HB_LiquidFragment

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG
            #pragma multi_compile_local _ HB_LIQUID_POOL

            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidSurfacePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "LiquidCut"
            Tags
            {
                "LightMode" = "HBStyleLiquidCut"
            }

            ZWrite On
            ZTest LEqual
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LiquidVertex
            #pragma fragment HB_LiquidCutFragment

            #pragma multi_compile_local _ HB_LIQUID_POOL

            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidSurfacePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "LiquidMask"
            Tags
            {
                "LightMode" = "HBStyleLiquidMask"
            }

            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LiquidVertex
            #pragma fragment HB_LiquidMaskFragment

            #pragma multi_compile_local _ HB_LIQUID_POOL

            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidSurfacePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "LiquidHeight"
            Tags
            {
                "LightMode" = "HBStyleLiquidHeight"
            }

            ZWrite Off
            ZTest Always
            Cull Off
            BlendOp Max
            Blend One One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LiquidHeightVertex
            #pragma fragment HB_LiquidHeightFragment

            #pragma multi_compile_local _ HB_LIQUID_POOL

            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidSurfacePass.hlsl"
            ENDHLSL
        }
    }
}
