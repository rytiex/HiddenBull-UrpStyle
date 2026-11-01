Shader "Hidden/HB URP Style/Foliage Capture"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff("Cutoff", Float) = 0.5
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Canopy("Canopy", Float) = 0.0
        _Leaf("Leaf", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "Albedo"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CaptureVertex
            #pragma fragment HB_CaptureAlbedoFragment

            #include "Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Normal"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CaptureVertex
            #pragma fragment HB_CaptureNormalFragment

            #include "Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Top"

            ZWrite Off
            ZTest Always
            Blend One One
            BlendOp Max

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CaptureTopVertex
            #pragma fragment HB_CaptureTopFragment

            #include "Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
