Shader "Hidden/HB URP Style/Liquid Cut"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Mask"

            ZWrite Off
            ZTest Greater
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_LiquidCutVertex
            #pragma fragment HB_LiquidCutMaskFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidCutPass.hlsl"
            ENDHLSL
        }
    }
}
