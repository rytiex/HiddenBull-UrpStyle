Shader "Hidden/HB URP Style/Terrain Layer Slice"
{
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        TEXTURE2D(_Source);
        SAMPLER(sampler_Source);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);

        float4 _Fill;
        float _HasSource;
        float _HasAlbedo;

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            output.uv = GetFullScreenTriangleTexCoord(vertexID);
            return output;
        }

        float4 Albedo(float2 uv)
        {
            return _HasAlbedo > 0.5 ? SAMPLE_TEXTURE2D_LOD(_Albedo, sampler_Albedo, uv, 0) : _Fill;
        }
        ENDHLSL

        Pass
        {
            Name "Albedo"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            float4 Fragment(Varyings input) : SV_Target
            {
                return Albedo(input.uv);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Normal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            float4 Fragment(Varyings input) : SV_Target
            {
                float3 normal = float3(0.0, 0.0, 1.0);

                if (_HasSource > 0.5)
                    normal = UnpackNormalMapRGorAG(SAMPLE_TEXTURE2D_LOD(_Source, sampler_Source, input.uv, 0), 1.0);

                return float4(normal * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Mask"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            float4 Fragment(Varyings input) : SV_Target
            {
                if (_HasSource > 0.5)
                    return SAMPLE_TEXTURE2D_LOD(_Source, sampler_Source, input.uv, 0);

                float4 albedo = Albedo(input.uv);
                float height = dot(albedo.rgb, float3(0.299, 0.587, 0.114));

                return float4(1.0, 1.0, saturate(height), albedo.a);
            }
            ENDHLSL
        }
    }
}
