Shader "Hidden/HB URP Style/Grass"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_GrassVertex
            #pragma fragment HB_GrassFragment

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #define HB_GRASS_FORWARD
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_GrassVertex
            #pragma fragment HB_GrassDepthFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_GrassVertex
            #pragma fragment HB_GrassDepthNormalsFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #define HB_DEPTH_NORMALS
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
