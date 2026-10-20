Shader "Hidden/HB URP Style/Billboard Capture"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff("Cutoff", Float) = 0.5
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _Canopy("Canopy", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite On
        ZTest LEqual

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _BumpScale;
            half _Canopy;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        float4x4 _HB_CaptureWorldToRoot;
        float4 _HB_CaptureCrown;

        struct CaptureAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
            float2 uv         : TEXCOORD0;
        };

        struct CaptureVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float4 tangentWS  : TEXCOORD2;
            float3 positionWS : TEXCOORD3;
        };

        CaptureVaryings CaptureVertex(CaptureAttributes input)
        {
            CaptureVaryings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
            return output;
        }

        half4 CaptureAlbedo(CaptureVaryings input)
        {
            half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
            clip(albedo.a - _Cutoff);
            return albedo;
        }
        ENDHLSL

        Pass
        {
            Name "Albedo"

            HLSLPROGRAM
            #pragma vertex CaptureVertex
            #pragma fragment CaptureAlbedoFragment

            half4 CaptureAlbedoFragment(CaptureVaryings input) : SV_Target
            {
                return half4(CaptureAlbedo(input).rgb, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Normal"

            HLSLPROGRAM
            #pragma vertex CaptureVertex
            #pragma fragment CaptureNormalFragment

            half4 CaptureNormalFragment(CaptureVaryings input,
                                        FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                CaptureAlbedo(input);

                float facing = IS_FRONT_VFACE(frontFace, 1.0, -1.0);
                float3 normal = normalize(input.normalWS) * facing;
                float3 tangent = normalize(input.tangentWS.xyz);
                float3 bitangent = cross(normal, tangent) * input.tangentWS.w;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 normalWS = normalize(mul(normalTS, float3x3(tangent, bitangent, normal)));

                float3 meshRoot = normalize(mul((float3x3)_HB_CaptureWorldToRoot, normalWS));

                if (_Canopy > 0.5)
                    normalWS = SafeNormalize(input.positionWS - _HB_CaptureCrown.xyz);

                float3 normalRoot = normalize(mul((float3x3)_HB_CaptureWorldToRoot, normalWS));

                return half4(normalRoot * 0.5 + 0.5, abs(meshRoot.y));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
