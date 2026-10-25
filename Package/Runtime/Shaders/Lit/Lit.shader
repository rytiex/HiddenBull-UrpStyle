Shader "HB URP Style/Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.7, 0.7, 0.7, 1.0)

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
        [Toggle(_HB_BRUSH_MASK)] _BrushMaskEnabled("Use Brush Mask", Float) = 0.0
        _BrushMask("Brush Mask", 2D) = "white" {}

        [Toggle(_HB_SPECULAR)] _SpecularEnabled("Enable Specular", Float) = 0.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [Toggle(_HB_RIM)] _RimEnabled("Enable Rim Light", Float) = 0.0
        _RimColor("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.5, 16.0)) = 4.0
        _RimIntensity("Rim Intensity", Range(0.0, 4.0)) = 1.0

        [Toggle(_HB_SUBSURFACE)] _SubsurfaceEnabled("Subsurface", Float) = 0.0
        [Enum(Skin, 0, Cloth, 1)] _SkinMode("Material", Float) = 0.0
        [HDR] _SubsurfaceColor("Subsurface Color", Color) = (0.85, 0.25, 0.15, 1.0)
        _SubsurfaceStrength("Subsurface Strength", Range(0.0, 1.0)) = 0.6
        _SubsurfaceWidth("Subsurface Width", Range(0.0, 1.0)) = 0.5
        _Transmission("Transmission", Range(0.0, 1.0)) = 0.3
        _TransmissionBend("Transmission Distortion", Range(0.0, 1.0)) = 0.3
        _TransmissionFocus("Transmission Concentration", Range(1.0, 16.0)) = 4.0
        [HDR] _TransmissionColor("Transmission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [ToggleUI] _ThicknessMapEnabled("Use Translucency Map", Float) = 0.0
        [NoScaleOffset] _ThicknessMap("Translucency Map", 2D) = "white" {}
        _SheenColor("Sheen Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SheenStrength("Sheen Strength", Range(0.0, 2.0)) = 0.6
        _SheenPower("Sheen Width", Range(1.0, 8.0)) = 4.0
        _Weave("Weave", Range(0.0, 1.0)) = 0.0
        _WeaveScale("Weave Scale", Float) = 200.0

        [Toggle(_HB_EMISSION)] _EmissionEnabled("Emission", Float) = 0.0
        [HDR] _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}

        [Enum(None, 0, Moving, 2, Static, 1)] _ReceiveWeather("Receive Weather", Float) = 1.0
        _Wetness("Wetness", Range(0.0, 1.0)) = 0.5
        _SnowCover("Snow Cover", Range(0.0, 1.0)) = 1.0
        _DropDensity("Drop Density", Range(0.0, 1.0)) = 0.25
        _DropSize("Drop Size", Range(0.005, 0.2)) = 0.0125
        _DropLife("Drop Life", Range(0.2, 20.0)) = 2.0
        _FlakeDensity("Flake Density", Range(0.0, 1.0)) = 0.25
        _FlakeSize("Flake Size", Range(0.005, 0.1)) = 0.0125
        _FlakeLife("Flake Life", Range(0.2, 20.0)) = 2.0

        [ToggleUI] _AlphaClipEnabled("Alpha Clip", Float) = 0.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [Enum(Front, 2, Back, 1, Both, 0)] _Cull("Render Face", Float) = 2.0
        [HideInInspector] _QueueOffset("Queue Offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "DisableBatching" = "True"
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
            #pragma target 4.5

            #pragma vertex HB_LitVertex
            #pragma fragment HB_LitFragment

            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _HB_MASK_MAP
            #pragma shader_feature_local_fragment _HB_TRIPLANAR
            #pragma shader_feature_local_fragment _HB_BRUSH
            #pragma shader_feature_local _HB_BRUSH_ANCHOR
            #pragma shader_feature_local_fragment _HB_BRUSH_MASK
            #pragma shader_feature_local_fragment _HB_SPECULAR
            #pragma shader_feature_local_fragment _HB_RIM
            #pragma shader_feature_local_fragment _HB_SUBSURFACE
            #pragma shader_feature_local_fragment _HB_CLOTH
            #pragma shader_feature_local_fragment _HB_THICKNESS_MAP
            #pragma shader_feature_local_fragment _HB_EMISSION
            #pragma shader_feature_local_fragment _ _HB_WEATHER _HB_WEATHER_MOVING
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

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
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HB_DepthVertex
            #pragma fragment HB_ShadowFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _HB_TRIPLANAR
            #pragma shader_feature_local _HB_BRUSH_ANCHOR

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_SHADOW_CASTER
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HB_DepthVertex
            #pragma fragment HB_DepthFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _HB_TRIPLANAR
            #pragma shader_feature_local _HB_BRUSH_ANCHOR

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HB_DepthVertex
            #pragma fragment HB_DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local _HB_TRIPLANAR
            #pragma shader_feature_local _HB_BRUSH_ANCHOR

            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define HB_DEPTH_NORMALS
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl"
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
            #pragma target 3.5

            #pragma vertex HB_MetaVertex
            #pragma fragment HB_MetaFragment

            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma shader_feature_local _HB_BASE_MAP
            #pragma shader_feature_local_fragment _HB_TRIPLANAR
            #pragma shader_feature_local_fragment _HB_MASK_MAP
            #pragma shader_feature_local_fragment _HB_EMISSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitMetaPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "HiddenBull.UrpStyle.Editor.StyleLitShaderGUI"
}
