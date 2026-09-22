#ifndef HIDDENBULL_URPSTYLE_BRUSH_INCLUDED
#define HIDDENBULL_URPSTYLE_BRUSH_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D(_HB_BrushAtlas);
SAMPLER(sampler_HB_BrushAtlas);

float4 _HB_BrushParams;

struct HiddenBullBrushSample
{
    half3 warp;
    half coverage;
};

HiddenBullBrushSample HB_NoBrush()
{
    HiddenBullBrushSample brush;
    brush.warp = half3(0.0h, 0.0h, 0.0h);
    brush.coverage = 0.0h;
    return brush;
}

float3 HB_ObjectSpacePosition(float3 positionWS)
{
    float3 positionOS = mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;

    float4x4 objectToWorld = GetObjectToWorldMatrix();

    float3 lossyScale = float3(
        length(objectToWorld._m00_m10_m20),
        length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));

    return positionOS * lossyScale;
}

half3 HB_ObjectSpaceNormal(half3 normalWS)
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    return SafeNormalize(mul(normalWS, (half3x3)objectToWorld));
}

HiddenBullBrushSample HB_SampleBrush(float3 positionWS, half3 normalWS, half objectSpace)
{
    HiddenBullBrushSample brush = HB_NoBrush();

    if (_HB_BrushParams.w < 0.5h)
        return brush;

    float3 position = lerp(positionWS, HB_ObjectSpacePosition(positionWS), objectSpace);
    half3 normal = lerp(normalWS, HB_ObjectSpaceNormal(normalWS), objectSpace);

    half3 blend = abs(normal);
    blend *= blend;
    blend *= blend;
    blend /= max(blend.x + blend.y + blend.z, HB_EPSILON);

    float3 uvw = position * _HB_BrushParams.x;

    half4 planeX = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.zy);
    half4 planeY = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xz);
    half4 planeZ = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xy);

    half2 warpX = planeX.rg * 2.0h - 1.0h;
    half2 warpY = planeY.rg * 2.0h - 1.0h;
    half2 warpZ = planeZ.rg * 2.0h - 1.0h;

    half3 warp = half3(0.0h, warpX.y, warpX.x) * blend.x
               + half3(warpY.x, 0.0h, warpY.y) * blend.y
               + half3(warpZ.x, warpZ.y, 0.0h) * blend.z;

    float4x4 objectToWorld = GetObjectToWorldMatrix();
    half3 rotated = SafeNormalize(mul((half3x3)objectToWorld, warp)) * length(warp);
    brush.warp = lerp(warp, rotated, objectSpace);

    half coverage = planeX.b * blend.x + planeY.b * blend.y + planeZ.b * blend.z;
    brush.coverage = coverage * 2.0h - 1.0h;

    float distanceToCamera = length(GetCameraPositionWS() - positionWS);
    half fade = saturate(1.0h - (distanceToCamera - _HB_BrushParams.y) * _HB_BrushParams.z);

    brush.warp *= fade;
    brush.coverage *= fade;

    return brush;
}

#endif
