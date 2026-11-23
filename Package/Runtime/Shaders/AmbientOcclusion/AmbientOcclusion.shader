Shader "Hidden/HB URP Style/Ambient Occlusion"
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
            Name "Ambient Occlusion Horizon"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_AOVertex
            #pragma fragment HB_AOHorizon

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AmbientOcclusion/AmbientOcclusionPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Ambient Occlusion Blur"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_AOVertex
            #pragma fragment HB_AOBlur

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AmbientOcclusion/AmbientOcclusionPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Ambient Occlusion Debug"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_AOVertex
            #pragma fragment HB_AODebug

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AmbientOcclusion/AmbientOcclusionPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
