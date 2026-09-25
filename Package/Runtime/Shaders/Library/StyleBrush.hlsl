#ifndef HIDDENBULL_URPSTYLE_BRUSH_INCLUDED
#define HIDDENBULL_URPSTYLE_BRUSH_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D(_HB_BrushAtlas);
SAMPLER(sampler_HB_BrushAtlas);

float4 _HB_BrushParams;

#define HB_BRUSH_TILES_PER_UNIT  _HB_BrushParams.x
#define HB_BRUSH_FADE_START      _HB_BrushParams.y
#define HB_BRUSH_FADE_RATE       _HB_BrushParams.z
#define HB_BRUSH_ATLAS_BOUND     _HB_BrushParams.w

struct HiddenBullBrushSample
{
    half3 warp;
    half coverage;
};

struct HiddenBullBrushSpace
{
    float3 position;
    half3 normal;
    half localSpace;
};

HiddenBullBrushSample HB_NoBrush()
{
    HiddenBullBrushSample brush;
    brush.warp = half3(0.0h, 0.0h, 0.0h);
    brush.coverage = 0.0h;
    return brush;
}

float3 HB_ObjectScale()
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();

    return float3(
        length(objectToWorld._m00_m10_m20),
        length(objectToWorld._m01_m11_m21),
        length(objectToWorld._m02_m12_m22));
}

float3 HB_ObjectSpacePosition(float3 positionWS)
{
    float3 positionOS = mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;

    return positionOS * HB_ObjectScale();
}

half3 HB_ObjectSpaceNormal(half3 normalWS)
{
    return SafeNormalize(mul(normalWS, (half3x3)GetObjectToWorldMatrix()));
}

HiddenBullBrushSpace HB_ResolveBrushSpace(float3 positionWS, half3 normalWS, half objectSpace,
                                          float3 anchorOS)
{
    HiddenBullBrushSpace space;

#ifdef _HB_BRUSH_ANCHOR
    space.position = anchorOS * HB_ObjectScale();
    space.normal = HB_ObjectSpaceNormal(normalWS);
    space.localSpace = 1.0h;
#else
    space.position = lerp(positionWS, HB_ObjectSpacePosition(positionWS), objectSpace);
    space.normal = lerp(normalWS, HB_ObjectSpaceNormal(normalWS), objectSpace);
    space.localSpace = objectSpace;
#endif

    return space;
}

half3 HB_TriplanarBlend(half3 normal)
{
    half3 blend = abs(normal);
    blend *= blend;
    blend *= blend;

    return blend / max(blend.x + blend.y + blend.z, HB_EPSILON);
}

HiddenBullBrushSample HB_SampleBrush(float3 positionWS, half3 normalWS, half objectSpace,
                                     float3 anchorOS, half mask)
{
    HiddenBullBrushSample brush = HB_NoBrush();

    if (HB_BRUSH_ATLAS_BOUND < 0.5h || mask <= 0.0h)
        return brush;

    float3 toSurface = positionWS - GetCameraPositionWS();
    half fade = saturate(1.0h - (length(toSurface) - HB_BRUSH_FADE_START) * HB_BRUSH_FADE_RATE);

    if (fade > 0.0h)
    {
        HiddenBullBrushSpace space = HB_ResolveBrushSpace(positionWS, normalWS, objectSpace, anchorOS);

        half3 blend = HB_TriplanarBlend(space.normal);
        float3 uvw = space.position * HB_BRUSH_TILES_PER_UNIT;

        half4 planeX = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.zy);
        half4 planeY = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xz);
        half4 planeZ = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xy);

        half2 warpX = planeX.rg * 2.0h - 1.0h;
        half2 warpY = planeY.rg * 2.0h - 1.0h;
        half2 warpZ = planeZ.rg * 2.0h - 1.0h;

        half3 warp = half3(0.0h, warpX.y, warpX.x) * blend.x
                   + half3(warpY.x, 0.0h, warpY.y) * blend.y
                   + half3(warpZ.x, warpZ.y, 0.0h) * blend.z;

        half3 rotated = SafeNormalize(mul((half3x3)GetObjectToWorldMatrix(), warp)) * length(warp);

        half coverage = planeX.b * blend.x + planeY.b * blend.y + planeZ.b * blend.z;

        brush.warp = lerp(warp, rotated, space.localSpace) * fade;
        brush.coverage = (coverage * 2.0h - 1.0h) * fade;
    }

    brush.warp *= mask;
    brush.coverage *= mask;

    return brush;
}

#endif
