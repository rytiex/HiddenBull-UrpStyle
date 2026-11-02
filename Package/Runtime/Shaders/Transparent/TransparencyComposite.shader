Shader "Hidden/HB URP Style/Transparency Composite"
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
            Name "Transmit"

            Blend DstColor Zero

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CompositeVertex
            #pragma fragment HB_TransmitFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyCompositePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Add"

            Blend One One

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CompositeVertex
            #pragma fragment HB_AddFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyCompositePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Refract"

            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CompositeVertex
            #pragma fragment HB_RefractFragment

            #pragma multi_compile_fragment _ HB_STYLE_DEBUG

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyCompositePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Background Depth"

            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CompositeVertex
            #pragma fragment HB_BackgroundDepthFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyCompositePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Downsample Depth"

            ZWrite On
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_CompositeVertex
            #pragma fragment HB_DownsampleDepthFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparencyCompositePass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
