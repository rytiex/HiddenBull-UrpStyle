Shader "Hidden/HB URP Style/Foliage Billboard"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _BaseMap("Albedo Atlas", 2D) = "white" {}
        [NoScaleOffset] _NormalAtlas("Normal Atlas", 2D) = "gray" {}
        [NoScaleOffset] _ShadowAtlas("Shadow Atlas", 2D) = "black" {}
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Frames("Frames", Float) = 8.0
        _BillboardCenter("Center", Vector) = (0.0, 0.0, 0.0, 0.0)
        _BillboardSize("Size", Float) = 1.0
        _BillboardTop("Top", Float) = 1.0
        _BillboardRadius("Radius", Float) = 1.0
        _BillboardSway("Sway", Float) = 0.0
        _ShadowSize("Shadow Size", Float) = 1.0
        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.4
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.75
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35
        _BakedTones("Baked Tones", Range(0.0, 1.0)) = 0.5
        _BakedSoftness("Baked Softness", Range(0.0, 1.0)) = 0.5
        _Wetness("Wetness", Range(0.0, 1.0)) = 0.5
        _SnowCover("Snow Cover", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"
            "IgnoreProjector" = "True"
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

            #pragma vertex HB_BillboardVertex
            #pragma fragment HB_BillboardFragment

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer procedural:HB_PlantInstancingSetup

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_BillboardShadowVertex
            #pragma fragment HB_BillboardShadowFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_PlantInstancingSetup

            #define HB_SHADOW_CASTER
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardShadowPass.hlsl"
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

            #pragma vertex HB_BillboardDepthVertex
            #pragma fragment HB_BillboardDepthFragment

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_PlantInstancingSetup

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl"
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

            #pragma vertex HB_BillboardDepthVertex
            #pragma fragment HB_BillboardDepthNormalsFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_PlantInstancingSetup

            #define HB_DEPTH_NORMALS
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
