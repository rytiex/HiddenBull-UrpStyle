Shader "HB URP Style/Liquid"
{
    Properties
    {
        _Tiling("Tiling", Float) = 0.125
        _Waves("Waves", Range(0.0, 1.0)) = 0.2
        [ToggleUI] _WindWaves("Wind Affects Waves", Float) = 1.0
        _WaveHeight("Wave Height", Range(0.0, 3.0)) = 1.0
        _WaveSteepness("Wave Steepness", Range(0.0, 2.0)) = 1.0
        _WaveLength("Wave Length", Range(0.25, 4.0)) = 1.0

        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0.0, 2.0)) = 0.25
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.65
        _Refraction("Refraction", Range(0.0, 1.0)) = 1.0
        _RefractionBlur("Refraction Blur", Range(0.0, 1.0)) = 0.145
        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.125
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.25

        [HideInInspector] [HDR] _DepthColor0("Depth Color 0", Color) = (0.0, 0.7176471, 0.6846459, 1.0)
        [HideInInspector] [HDR] _DepthColor1("Depth Color 1", Color) = (0.0, 0.21511805, 0.33962262, 1.0)
        [HideInInspector] [HDR] _DepthColor2("Depth Color 2", Color) = (0.0, 0.0031320888, 0.028301895, 1.0)
        [HideInInspector] [HDR] _DepthColor3("Depth Color 3", Color) = (0.0, 0.0031320888, 0.028301895, 1.0)
        [HideInInspector] [HDR] _DepthColor4("Depth Color 4", Color) = (0.0, 0.0031320888, 0.028301895, 1.0)
        [HideInInspector] [HDR] _DepthColor5("Depth Color 5", Color) = (0.0, 0.0031320888, 0.028301895, 1.0)
        [HideInInspector] _DepthTimes0("Depth Times 0", Vector) = (0.0, 0.30000764, 1.0, 1.0)
        [HideInInspector] _DepthTimes1("Depth Times 1", Vector) = (1.0, 1.0, 3.0, 5.0)
        [HideInInspector] _DepthAlpha0("Depth Alpha 0", Vector) = (0.0, 0.1254902, 0.2509804, 0.7538165)
        [HideInInspector] _DepthAlpha1("Depth Alpha 1", Vector) = (1.0, 1.0, 0.0, 0.0)
        [HideInInspector] _DepthAlphaTimes0("Depth Alpha Times 0", Vector) = (0.0, 0.28824294, 0.57941556, 0.8)
        [HideInInspector] _DepthAlphaTimes1("Depth Alpha Times 1", Vector) = (1.0, 1.0, 0.0, 0.0)
        _Caustics("Caustics", Range(0.0, 1.0)) = 0.05
        _Glow("Glow", Range(0.0, 1.0)) = 0.0
        _CausticScale("Caustic Scale", Range(0.25, 4.0)) = 0.65
        _CrestColor("Crest Color", Color) = (0.7075472, 0.7075472, 0.7075472, 1.0)
        _CrestSoftness("Crest Softness", Range(0.0, 1.0)) = 0.45

        _FoamColor("Foam Color", Color) = (0.76495194, 0.8158059, 0.8490566, 1.0)
        _FoamDistance("Foam Distance", Range(0.0, 3.0)) = 0.1
        _FoamAmount("Foam Amount", Range(0.0, 1.0)) = 0.3
        _ShoreFoam("Shore Foam", Range(0.0, 30.0)) = 2.5
        _BubbleAmount("Bubbles", Range(0.0, 1.0)) = 0.0
        _Pulse("Pulse", Range(0.0, 8.0)) = 1.0
        _PatternScale("Pattern Scale", Range(0.1, 8.0)) = 1.0

        _CrustAmount("Crust", Range(0.0, 1.0)) = 0.0
        _CrustColor("Crust Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _BaseMap("Crust Map", 2D) = "white" {}

        _UnderShafts("Light Shafts", Range(0.0, 1.0)) = 0.5
        _UnderBubbles("Underwater Bubbles", Range(0.0, 1.0)) = 0.25
        _UnderDistortion("Underwater Distortion", Range(0.0, 1.0)) = 0.3

        [ToggleUI] _ReceiveWeather("Rain Ripples", Float) = 1.0
        [HideInInspector] _WaterGrid("Water Grid", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _WaterArea("Water Area", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _WaterBox("Water Box", Vector) = (0.0, 0.0, 0.0, 0.0)
        [HideInInspector] _WaterLayers("Water Layers", Float) = 0.0
        [HideInInspector] _WaterStreams("Water Streams", Float) = 0.0
        [HideInInspector] [NoScaleOffset] _WaterLevelMap("Water Level Map", 2D) = "black" {}
        [HideInInspector] [NoScaleOffset] _WaterWeightMap("Water Weight Map", 2D) = "black" {}
        [HideInInspector] [NoScaleOffset] _WaterBlockMap("Water Block Map", 2D) = "black" {}
        [HideInInspector] _WaterUnbatched("Water Unbatched", Float) = 0.0
        [HideInInspector] _WaterStencil("Water Stencil", Float) = 8.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SkyOcclusion"
            Tags
            {
                "LightMode" = "HiddenBullSkyOcclusion"
            }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma vertex HiddenBullLiquidVertex
            #pragma fragment HiddenBullLiquidDepthFragment

            #define HB_LIQUID_DEPTH
            #define HB_LIQUID_SKY
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CutDepth"
            Tags
            {
                "LightMode" = "HiddenBullLiquidCutDepth"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma vertex HiddenBullLiquidVertex
            #pragma fragment HiddenBullLiquidCutDepthFragment

            #define HB_LIQUID_CUT_DEPTH
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CutMask"
            Tags
            {
                "LightMode" = "HiddenBullLiquidCut"
            }

            ZWrite Off
            ZTest Greater
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullLiquidCutVertex
            #pragma fragment HiddenBullLiquidCutFragment

            #define HB_LIQUID_CUT_MASK
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CutDry"
            Tags
            {
                "LightMode" = "HiddenBullLiquidCutDry"
            }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullLiquidCutVertex
            #pragma fragment HiddenBullLiquidCutFragment

            #define HB_LIQUID_CUT_MASK
            #define HB_LIQUID_CUT_DRY
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CutFloor"
            Tags
            {
                "LightMode" = "HiddenBullLiquidCutFloor"
            }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullLiquidCutVertex
            #pragma fragment HiddenBullLiquidCutFragment

            #define HB_LIQUID_CUT_MASK
            #define HB_LIQUID_CUT_DRY
            #define HB_LIQUID_CUT_FLOOR
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CutRoof"
            Tags
            {
                "LightMode" = "HiddenBullLiquidCutRoof"
            }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullLiquidCutVertex
            #pragma fragment HiddenBullLiquidCutFragment

            #define HB_LIQUID_CUT_MASK
            #define HB_LIQUID_CUT_DRY
            #define HB_LIQUID_CUT_ROOF
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Prime"
            Tags
            {
                "LightMode" = "HiddenBullLiquidPrime"
            }

            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Off

            Stencil
            {
                Ref [_WaterStencil]
                WriteMask 8
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma vertex HiddenBullLiquidVertex
            #pragma fragment HiddenBullLiquidPrimeFragment

            #define HB_LIQUID_PRIME
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "HiddenBullLiquid"
            }

            Blend Off
            ZWrite Off
            ZTest LEqual
            Offset 0, -1
            Cull Off

            Stencil
            {
                Ref [_WaterStencil]
                ReadMask 8
                Comp Equal
            }

            HLSLPROGRAM
            #pragma target 3.5

            #pragma multi_compile_instancing
            #pragma vertex HiddenBullLiquidVertex
            #pragma fragment HiddenBullLiquidColorFragment

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #define _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_fragment _ _HB_LIQUID_STREAMS

            #define HB_LIQUID_COLOR
            #define HB_LIQUID_FORWARD
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidPass.hlsl"
            ENDHLSL
        }

    }

    CustomEditor "HiddenBull.UrpStyle.Editor.LiquidShaderGUI"
    Fallback Off
}
