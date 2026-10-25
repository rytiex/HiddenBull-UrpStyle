Shader "Hidden/HB URP Style/Shelter Underside"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Underside"

            ZWrite On
            ZTest GEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HB_UndersideVertex
            #pragma fragment HB_UndersideFragment

            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUndersidePass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
