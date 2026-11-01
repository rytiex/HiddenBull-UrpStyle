Shader "Hidden/HB URP Style/Anti Aliasing"
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
            Name "Anti Aliasing"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_AntiAliasingVertex
            #pragma fragment HB_AntiAliasingFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AntiAliasing/AntiAliasingPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
