#ifndef HB_BILLBOARD_SHADOW_PASS_INCLUDED
#define HB_BILLBOARD_SHADOW_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

TEXTURE2D(_ShadowAtlas);
SAMPLER(sampler_ShadowAtlas);

#define HB_SHADOW_FRAMES     8.0
#define HB_SHADOW_LEAST_RISE 0.02
#define HB_SHADOW_ZENITH     0.999
#define HB_SHADOW_EDGE       0.5h

struct HBBillboardShadowAttributes
{
    float4 positionOS : POSITION;
    float2 corner     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBBillboardShadowVaryings
{
    float4 nearUV                 : TEXCOORD0;
    float2 farUV                  : TEXCOORD1;
    float3 weights                : TEXCOORD2;
    nointerpolation float3 frame  : TEXCOORD3;
    float4 positionCS             : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float3 HB_ShadowFrameView(float2 frame)
{
    float2 grid = frame / (HB_SHADOW_FRAMES - 1.0) * 2.0 - 1.0;
    float2 spread = float2(grid.x + grid.y, grid.x - grid.y) * 0.5;

    return normalize(float3(spread.x, 1.0 - abs(spread.x) - abs(spread.y), spread.y));
}

float2 HB_ShadowFrameUV(float2 frame, float3 spot, float3 sun)
{
    float3 view = HB_ShadowFrameView(frame);
    float3 right = normalize(cross(view, float3(0.0, 1.0, 0.0)));
    float3 lift = cross(right, view);
    float3 onFrame = spot - sun * (dot(spot, view) / dot(sun, view));

    return float2(dot(onFrame, right), dot(onFrame, lift)) / _ShadowSize + 0.5;
}

float3 HB_ShadowSun(float3 centerWS, out float3 towardWS)
{
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    towardWS = normalize(_LightPosition - centerWS);
#else
    towardWS = _LightDirection;
#endif

    float3 sun = TransformWorldToObjectDir(towardWS);

    sun.y = max(sun.y, HB_SHADOW_LEAST_RISE);

    return normalize(sun);
}

HBBillboardShadowVaryings HB_BillboardShadowVertex(HBBillboardShadowAttributes input)
{
    HBBillboardShadowVaryings output = (HBBillboardShadowVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 centerWS = TransformObjectToWorld(_BillboardCenter.xyz);
    float3 towardWS;
    float3 sun = HB_ShadowSun(centerWS, towardWS);

    float3 flattened = sun / (abs(sun.x) + abs(sun.y) + abs(sun.z));
    float2 cell = (float2(flattened.x + flattened.z, flattened.x - flattened.z) * 0.5 + 0.5) *
                  (HB_SHADOW_FRAMES - 1.0);
    float2 base = min(floor(cell), HB_SHADOW_FRAMES - 2.0);
    float2 part = cell - base;
    float upper = step(1.0, part.x + part.y);

    float3 weights = upper > 0.5
        ? float3(part.x + part.y - 1.0, 1.0 - part.y, 1.0 - part.x)
        : float3(1.0 - part.x - part.y, part.x, part.y);

    weights *= weights * weights;
    output.weights = weights / (weights.x + weights.y + weights.z);
    output.frame = float3(base, upper);

    float3 right = abs(sun.y) > HB_SHADOW_ZENITH
        ? float3(1.0, 0.0, 0.0)
        : normalize(cross(sun, float3(0.0, 1.0, 0.0)));
    float3 lift = cross(right, sun);
    float2 offset = (input.corner - 0.5) * _ShadowSize;
    float3 spot = right * offset.x + lift * offset.y;

    output.nearUV = float4(HB_ShadowFrameUV(base + upper, spot, sun),
                           HB_ShadowFrameUV(base + float2(1.0, 0.0), spot, sun));
    output.farUV = HB_ShadowFrameUV(base + float2(0.0, 1.0), spot, sun);

    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float scale = length(objectToWorld._m01_m11_m21);
    float height = saturate(0.5 + spot.y * 0.5 / max(_BillboardTop - _BillboardCenter.y, 1e-3));
    float3 restWS = TransformObjectToWorld(_BillboardCenter.xyz + spot);
    float3 positionWS = restWS + HB_PlantSway(centerWS, height * HB_BILLBOARD_REACH, _BillboardSway * scale);

    output.positionCS = HB_ShadowCasterPosition(positionWS, towardWS);

    return output;
}

half HB_ShadowFrameShape(float2 frame, float2 uv)
{
    float2 atlas = (frame + saturate(uv)) / HB_SHADOW_FRAMES;

    return SAMPLE_TEXTURE2D_LOD(_ShadowAtlas, sampler_ShadowAtlas, atlas, 0.0).r;
}

half4 HB_BillboardShadowFragment(HBBillboardShadowVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    float2 base = input.frame.xy;
    half first = HB_ShadowFrameShape(base + input.frame.z, input.nearUV.xy);
    half second = HB_ShadowFrameShape(base + float2(1.0, 0.0), input.nearUV.zw);
    half third = HB_ShadowFrameShape(base + float2(0.0, 1.0), input.farUV);

    clip(first * half(input.weights.x) + second * half(input.weights.y) + third * half(input.weights.z) -
         HB_SHADOW_EDGE);
    LODFadeCrossFade(input.positionCS);

    return 0;
}

#endif
