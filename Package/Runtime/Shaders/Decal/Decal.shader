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

        [ToggleUI] _AffectNormal("Affect Normal", Float) = 1.0
        [ToggleUI] _AffectMAOS("Affect Surface", Float) = 0.0

        _Wear("Edge Wear", Range(0.0, 1.0)) = 0.5
        _BrushScale("Brush Size", Range(0.25, 4.0)) = 1.0
        _StrokeAlpha("Stroke Alpha", Range(0.0, 1.0)) = 0.5

        [IntRange] _DrawOrder("Draw Order", Range(-50, 50)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
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

            #pragma vertex HB_DecalVertex
            #pragma fragment HB_DecalFragment

            #pragma multi_compile _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _DECAL_LAYERS
            #pragma multi_compile_instancing

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Decal/DecalPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "HiddenBull.UrpStyle.Editor.StyleDecalShaderGUI"
    Fallback Off
}
