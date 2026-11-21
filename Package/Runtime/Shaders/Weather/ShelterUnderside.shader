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

        Pass
        {
            Name "UndersidePlant"

            ZWrite On
            ZTest GEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex HB_UndersidePlantVertex
            #pragma fragment HB_UndersideFragment

            #define HB_UNDERSIDE_PLANT
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUndersidePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Top"

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex HB_ShelterTopVertex
            #pragma fragment HB_ShelterTopFragment

            #define HB_SHELTER_TOP
            #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUndersidePass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
