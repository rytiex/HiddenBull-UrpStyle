Shader "HB URP Style/Sky"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HB_SkyVertex
            #pragma fragment HB_SkyFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Sky/SkyPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
