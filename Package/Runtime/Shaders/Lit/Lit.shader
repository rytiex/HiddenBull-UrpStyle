Shader "HiddenBull/URP Style/Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.7, 0.7, 0.7, 1.0)

        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.265
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.75
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35
        _BakedTones("Baked Tones", Range(0.0, 1.0)) = 0.25
        _BakedSoftness("Baked Softness", Range(0.0, 1.0)) = 0.375

        [Toggle(_HB_RIM)] _RimEnabled("Enable Rim Light", Float) = 0.0
        _RimColor("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.5, 16.0)) = 4.0
        _RimIntensity("Rim Intensity", Range(0.0, 4.0)) = 1.0

        [Toggle(_HB_SPECULAR)] _SpecularEnabled("Enable Specular", Float) = 0.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5


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

        [Toggle(_HB_BRUSH_MASK)] _BrushMaskEnabled("Use Brush Mask", Float) = 0.0
        _BrushMask("Brush Mask", 2D) = "white" {}
        _BrushAlbedo("Brush Albedo Variation", Range(0.0, 1.0)) = 0.125

        [Toggle(_HB_BASE_MAP)] _BaseMapEnabled("Use Base Map", Float) = 0.0
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Toggle(_NORMALMAP)] _NormalMapEnabled("Use Normal Map", Float) = 0.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        [ToggleUI] _AlphaClipEnabled("Alpha Clip", Float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 2.0

        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex HiddenBullLitVertex
            #pragma fragment HiddenBullLitFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local _HB_BRUSH_ANCHOR
            #pragma shader_feature_local_fragment _HB_BRUSH_MASK
            #pragma shader_feature_local_fragment _HB_SPECULAR
            #pragma shader_feature_local_fragment _HB_RIM
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "OITMoments"
            Tags
            {
                "LightMode" = "HiddenBullOITMoments"
            }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullOITMomentsVertex
            #pragma fragment HiddenBullOITMomentsFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_OIT_MOMENTS
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitTransparencyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "OITColor"
            Tags
            {
                "LightMode" = "HiddenBullOITColor"
            }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullLitVertex
            #pragma fragment HiddenBullOITColorFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local _HB_BRUSH_ANCHOR
            #pragma shader_feature_local_fragment _HB_BRUSH_MASK
            #pragma shader_feature_local_fragment _HB_SPECULAR
            #pragma shader_feature_local_fragment _HB_RIM
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _HB_PREMULTIPLY
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define _SURFACE_TYPE_TRANSPARENT
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitTransparencyPass.hlsl"
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment HiddenBullFragmentMeta

            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "HiddenBull.UrpStyle.Editor.LitShaderGUI"
}
