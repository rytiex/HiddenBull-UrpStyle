Shader "Hidden/HB URP Style/Cavity"
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
            Name "Cavity"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend DstColor SrcAlpha, Zero One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CavityVertex
            #pragma fragment HB_CavityFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Cavity/CavityPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "CavityDebug"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CavityVertex
            #pragma fragment HB_CavityDebugFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Cavity/CavityPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
