Shader "Hidden/HB URP Style/Sky Map"
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
            Name "Sky Map"

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_SkyMapVertex
            #pragma fragment HB_SkyMapFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Sky/SkyMapPass.hlsl"
            ENDHLSL
        }
    }
}
