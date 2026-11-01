#ifndef HB_PLANT_CAPTURE_INCLUDED
#define HB_PLANT_CAPTURE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define HB_CAPTURE_LIFT       0.35
#define HB_CAPTURE_BIAS       0.1
#define HB_CAPTURE_RANGE      0.6
#define HB_CAPTURE_CONTRAST   2.0
#define HB_CAPTURE_NO_SLOPE   0.174
#define HB_CAPTURE_FULL_SLOPE 0.407

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    half _BumpScale;
    half _Canopy;
    half _Leaf;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_HB_CaptureTop);
SAMPLER(sampler_HB_CaptureTop);

float4 _HB_CaptureMin;
float4 _HB_CaptureSize;

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

CaptureVaryings HB_CaptureVertex(CaptureAttributes input)
{
    CaptureVaryings output;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
    return output;
}

float2 HB_CaptureTopUV(float2 xz)
{
    return (xz - _HB_CaptureMin.xz) / _HB_CaptureSize.xz;
}

CaptureVaryings HB_CaptureTopVertex(CaptureAttributes input)
{
    CaptureVaryings output = (CaptureVaryings)0;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

    float2 clipXY = HB_CaptureTopUV(output.positionWS.xz) * 2.0 - 1.0;

#if UNITY_UV_STARTS_AT_TOP
    clipXY.y = -clipXY.y;
#endif

    output.positionCS = float4(clipXY, 0.5, 1.0);
    return output;
}

half4 HB_CaptureSurface(CaptureVaryings input)
{
    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

    clip(albedo.a - _Cutoff);

    return albedo;
}

half4 HB_CaptureAlbedoFragment(CaptureVaryings input) : SV_Target
{
    return half4(HB_CaptureSurface(input).rgb, 1.0h);
}

half4 HB_CaptureTopFragment(CaptureVaryings input) : SV_Target
{
    HB_CaptureSurface(input);

    return half((input.positionWS.y - _HB_CaptureMin.y) / _HB_CaptureSize.y).xxxx;
}

half HB_CaptureOpen(float3 positionWS, float3 weatherNormal)
{
    float2 probe = positionWS.xz + weatherNormal.xz * HB_CAPTURE_LIFT;
    float top = SAMPLE_TEXTURE2D(_HB_CaptureTop, sampler_HB_CaptureTop, HB_CaptureTopUV(probe)).r;
    float roof = _HB_CaptureMin.y + top * _HB_CaptureSize.y;
    float height = positionWS.y + weatherNormal.y * HB_CAPTURE_LIFT;
    float covered = saturate((roof - height - HB_CAPTURE_BIAS) / HB_CAPTURE_RANGE);

    return half(saturate(0.5 + (0.5 - covered) * HB_CAPTURE_CONTRAST));
}

half4 HB_CaptureNormalFragment(CaptureVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    HB_CaptureSurface(input);

    float3 normal = normalize(input.normalWS);
    float3 weatherNormal;
    half slope = 1.0h;

    if (_Leaf > 0.5h)
    {
        weatherNormal = normal.y < 0.0 ? -normal : normal;
        normal = SafeNormalize(lerp(normal, input.tangentWS.xyz, float(_Canopy)));
    }
    else
    {
        normal *= IS_FRONT_VFACE(frontFace, 1.0, -1.0);

        float3 tangent = normalize(input.tangentWS.xyz);
        float3 bitangent = cross(normal, tangent) * input.tangentWS.w;
        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);

        normal = normalize(mul(float3(normalTS), float3x3(tangent, bitangent, normal)));
        weatherNormal = normal;
        slope = half(saturate((normal.y - HB_CAPTURE_NO_SLOPE) / (HB_CAPTURE_FULL_SLOPE - HB_CAPTURE_NO_SLOPE)));
    }

    return half4(normal * 0.5 + 0.5, HB_CaptureOpen(input.positionWS, weatherNormal) * slope);
}

#endif
