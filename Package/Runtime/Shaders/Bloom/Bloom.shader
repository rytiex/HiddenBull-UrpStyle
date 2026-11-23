Shader "Hidden/HB URP Style/Bloom"
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

        Pass
        {
            Name "Bloom Prefilter"

            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_BloomVertex
            #pragma fragment HB_BloomPrefilter

            #pragma multi_compile_local_fragment _ HB_BLOOM_STABLE

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/BloomPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Down"

            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_BloomVertex
            #pragma fragment HB_BloomDown

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/BloomPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Up"

            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_BloomVertex
            #pragma fragment HB_BloomUp

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/BloomPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Composite"

            Blend One One, Zero One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_BloomVertex
            #pragma fragment HB_BloomComposite

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/BloomPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
