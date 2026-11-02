Shader "Hidden/HB URP Style/Refraction Blur"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            Name "Downsample"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_RefractionBlurVertex
            #pragma fragment HB_RefractionDownsampleFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/RefractionBlurPass.hlsl"
            ENDHLSL
        }
    }
}
