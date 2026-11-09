Shader "Hidden/HB URP Style/Reflect Depth"
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
            Name "Reflect Depth"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment HB_ReflectDepthFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Reflection/ReflectDepthPass.hlsl"
            ENDHLSL
        }
    }
}
