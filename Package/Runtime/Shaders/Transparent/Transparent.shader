Shader "HB URP Style/Transparent"
{
    Properties
    {
        [Enum(Alpha, 0, Premultiply, 1, Additive, 2, Tinted, 3, Refractive, 4)] _BlendMode("Blend Mode", Float) = 0.0
        _Refraction("Refraction", Range(0.0, 1.0)) = 0.5
        _RefractionBlur("Blur", Range(0.0, 1.0)) = 0.0
        _Dispersion("Dispersion", Range(0.0, 1.0)) = 0.0
        _StrokeBend("Stroke Bend", Range(0.0, 1.0)) = 0.5
        [MainColor] _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 0.5)

        [Toggle(_HB_TRIPLANAR)] _TriplanarEnabled("Triplanar Mapping", Float) = 0.0
        [Enum(World, 0, Object, 1, Rest Pose, 2)] _TriplanarSpace("Mapping Space", Float) = 1.0
        [Toggle(_HB_BASE_MAP)] _BaseMapEnabled("Use Base Map", Float) = 0.0
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Toggle(_NORMALMAP)] _NormalMapEnabled("Use Normal Map", Float) = 0.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        [Toggle(_HB_MASK_MAP)] _MaskMapEnabled("Use Mask Map", Float) = 0.0
        [NoScaleOffset] _MaskMap("Mask Map", 2D) = "white" {}

        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.265
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.75
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35
        _BakedTones("Baked Tones", Range(0.0, 1.0)) = 0.25
        _BakedSoftness("Baked Softness", Range(0.0, 1.0)) = 0.375

        [Toggle(_HB_BRUSH)] _BrushEnabled("Enable Brush", Float) = 1.0
        [Enum(World, 0, Object, 1, Rest Pose, 2)] _BrushObjectSpace("Brush Space", Float) = 1.0
        _BrushScale("Brush Size", Range(0.25, 4.0)) = 1.0
        _BrushPaint("Brush Paint", Range(0.0, 1.0)) = 1.0
        _BrushEdgeKeep("Brush Edge Keep", Range(0.0, 1.0)) = 1.0
        _ToneStrokes("Tone Strokes", Range(0.0, 2.0)) = 1.0
        _BakedStrokes("Baked Strokes", Range(0.0, 1.0)) = 1.0
        _BrushRelief("Brush Relief", Range(0.0, 1.0)) = 0.235
        _BrushShading("Brush Shading Break-up", Range(0.0, 0.5)) = 0.5
        _BrushAmbient("Brush Ambient Break-up", Range(0.0, 0.5)) = 0.125
        _BrushAlbedo("Brush Albedo Variation", Range(0.0, 1.0)) = 0.125
        _StrokeAlpha("Stroke Alpha", Range(0.0, 1.0)) = 0.25
        [Toggle(_HB_BRUSH_MASK)] _BrushMaskEnabled("Use Brush Mask", Float) = 0.0
        _BrushMask("Brush Mask", 2D) = "white" {}

        [Toggle(_HB_SPECULAR)] _SpecularEnabled("Enable Specular", Float) = 1.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.8

        [Toggle(_HB_RIM)] _RimEnabled("Enable Rim Light", Float) = 0.0
        _RimColor("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.5, 16.0)) = 4.0
        _RimIntensity("Rim Intensity", Range(0.0, 4.0)) = 1.0

        [Toggle(_HB_EMISSION)] _EmissionEnabled("Emission", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ToggleUI] _ColouredShadow("Coloured Shadow", Float) = 1.0
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
        }
        LOD 300

        Pass
        {
            Name "Tint"
            Tags
            {
                "LightMode" = "HBStyleTransparentTint"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend DstColor Zero

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LitVertex
            #pragma fragment HB_TransparentTintFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _HB_TRIPLANAR
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local _HB_BRUSH_ANCHOR
            #pragma shader_feature_local_fragment _HB_BRUSH_MASK

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_TRANSPARENT
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "HBStyleTransparent"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LitVertex
            #pragma fragment HB_TransparentFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _HB_MASK_MAP
            #pragma shader_feature_local_fragment _HB_TRIPLANAR
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local _HB_BRUSH_ANCHOR
            #pragma shader_feature_local_fragment _HB_BRUSH_MASK
            #pragma shader_feature_local_fragment _HB_SPECULAR
            #pragma shader_feature_local_fragment _HB_RIM
            #pragma shader_feature_local_fragment _HB_EMISSION
            #pragma shader_feature_local_fragment _ _HB_PREMULTIPLY _HB_ADDITIVE _HB_TINTED
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #define _SHADOWS_SOFT_MEDIUM
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_TRANSPARENT
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Nearest"
            Tags
            {
                "LightMode" = "HBStyleTransparentNearest"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend One One
            BlendOp Max
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LitVertex
            #pragma fragment HB_TransparentNearestFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_TRANSPARENT
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Refractive"
            Tags
            {
                "LightMode" = "HBStyleTransparentRefractive"
            }

            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Blend One One
            BlendOp Max
            ColorMask G

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LitVertex
            #pragma fragment HB_TransparentNearestFragment

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_TRANSPARENT
            #define HB_REFRACTIVE_NEAREST
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One, One One
            BlendOp Add, Max

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_DepthVertex
            #pragma fragment HB_ShadowTintFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _HB_TRIPLANAR
            #pragma shader_feature_local _HB_BRUSH_ANCHOR

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentShadowPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "HiddenBull.UrpStyle.Editor.StyleTransparentShaderGUI"
}
