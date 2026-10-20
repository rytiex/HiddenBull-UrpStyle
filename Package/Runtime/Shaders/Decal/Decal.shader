Shader "HB URP Style/Decal"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Normal][NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0.0, 2.0)) = 1.0
        _NormalBlend("Normal Opacity", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.3
        _MAOSOpacity("Surface Opacity", Range(0.0, 1.0)) = 1.0

        [ToggleUI] _AffectAlbedo("Affect Color", Float) = 1.0
        [ToggleUI] _AffectNormal("Affect Normal", Float) = 1.0
        [ToggleUI] _AffectMAOS("Affect Surface", Float) = 0.0

        _Wear("Edge Wear", Range(0.0, 1.0)) = 0.5
        _BrushScale("Brush Size", Range(0.25, 4.0)) = 1.0
        _StrokeAlpha("Stroke Alpha", Range(0.0, 1.0)) = 0.5

        _DiffuseWrap("Diffuse Wrap", Range(0.0, 1.0)) = 0.265
        _DiffuseSoftness("Diffuse Softness", Range(0.0, 2.0)) = 0.75
        _HalfTone("Half Tone", Range(0.0, 1.0)) = 0.35

        [IntRange] _DrawOrder("Draw Order", Range(-50, 50)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "DisableBatching" = "False"
        }

        Pass
        {
            Name "DBufferProjector"
            Tags
            {
                "LightMode" = "DBufferProjector"
            }

            Cull Front
            ZTest Greater
            ZWrite Off
            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore

            #pragma vertex HiddenBullDecalVertex
            #pragma fragment HiddenBullDecalDBufferFragment

            #pragma multi_compile _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _DECAL_LAYERS
            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Decal/DecalPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DecalScreenSpaceProjector"
            Tags
            {
                "LightMode" = "DecalScreenSpaceProjector"
            }

            Cull Front
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HiddenBullDecalVertex
            #pragma fragment HiddenBullDecalScreenFragment

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_fragment _ _DECAL_LAYERS
            #pragma multi_compile_instancing

            #define HB_DECAL_SCREEN_SPACE
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Decal/DecalPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HiddenBull.UrpStyle.Editor.DecalShaderGUI"
    Fallback Off
}
