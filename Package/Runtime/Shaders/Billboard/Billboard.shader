Shader "Hidden/HiddenBull/Style Billboard"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _BaseMap("Albedo Atlas", 2D) = "white" {}
        [NoScaleOffset] _NormalAtlas("Normal Atlas", 2D) = "gray" {}
        [MainColor] _BaseColor("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Frames("Frames", Float) = 8.0
        _BillboardCenter("Center", Vector) = (0.0, 0.0, 0.0, 0.0)
        _BillboardSize("Size", Float) = 1.0
        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.4
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.75
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35
        _WindStrength("Wind Strength", Range(0.0, 3.0)) = 1.0
        [ToggleUI] _ReceiveWeather("Receive Weather", Float) = 1.0
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

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullBillboardVertex
            #pragma fragment HiddenBullBillboardFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_BillboardInstancingSetup

            #define HB_BILLBOARD_FORWARD
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Billboard/BillboardPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HiddenBullBillboardVertex
            #pragma fragment HiddenBullBillboardDepthFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_BillboardInstancingSetup

            #define HB_BILLBOARD_SHADOW
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Billboard/BillboardPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HiddenBullBillboardVertex
            #pragma fragment HiddenBullBillboardDepthFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_BillboardInstancingSetup

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Billboard/BillboardPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HiddenBullBillboardVertex
            #pragma fragment HiddenBullBillboardDepthNormalsFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:HB_BillboardInstancingSetup

            #define HB_BILLBOARD_DEPTH_NORMALS
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Billboard/BillboardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HiddenBull.UrpStyle.Editor.BillboardShaderGUI"
    Fallback Off
}
