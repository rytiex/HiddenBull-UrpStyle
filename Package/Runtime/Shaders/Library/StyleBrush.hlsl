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

#define HB_BRUSH_SPINE_RANGE     0.0625
#define HB_BRUSH_SEAM_FOOTPRINT  0.25
#define HB_BRUSH_LIGHTMAP_SEAM   0.02

struct HiddenBullBrushSample
{
    half3 spine;
    half coverage;
    float3 offset;
    half3 normalShift;
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
    brush.spine = half3(0.0h, 0.0h, 0.0h);
    brush.coverage = 0.0h;
    brush.offset = float3(0.0, 0.0, 0.0);
    brush.normalShift = half3(0.0h, 0.0h, 0.0h);
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
                                     float3 anchorOS, half scale, half mask)
{
    HiddenBullBrushSample brush = HB_NoBrush();

    if (HB_BRUSH_ATLAS_BOUND < 0.5 || mask <= 0.0h)
        return brush;

    float distanceToCamera = distance(positionWS, GetCameraPositionWS());
    half fade = half(saturate(1.0 - (distanceToCamera - HB_BRUSH_FADE_START) * HB_BRUSH_FADE_RATE));

    if (fade > 0.0h)
    {
        HiddenBullBrushSpace space = HB_ResolveBrushSpace(positionWS, normalWS, objectSpace, anchorOS);

        half3 blend = HB_TriplanarBlend(space.normal);
        float3 uvw = space.position * (HB_BRUSH_TILES_PER_UNIT / scale);

        half4 planeX = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.zy);
        half4 planeY = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xz);
        half4 planeZ = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, uvw.xy);

        half2 spineX = planeX.rg * 2.0h - 1.0h;
        half2 spineY = planeY.rg * 2.0h - 1.0h;
        half2 spineZ = planeZ.rg * 2.0h - 1.0h;

        half3 spine = half3(0.0h, spineX.y, spineX.x) * blend.x
                    + half3(spineY.x, 0.0h, spineY.y) * blend.y
                    + half3(spineZ.x, spineZ.y, 0.0h) * blend.z;

        half3 rotated = SafeNormalize(mul((half3x3)GetObjectToWorldMatrix(), spine)) * length(spine);

        half coverage = planeX.b * blend.x + planeY.b * blend.y + planeZ.b * blend.z;

        brush.spine = lerp(spine, rotated, space.localSpace) * (fade * mask);
        brush.coverage = (coverage * 2.0h - 1.0h) * (fade * mask);
    }

    return brush;
}

float3 HB_BrushStrokeOffset(half3 spine, half3 normalWS, half scale)
{
    float3 normal = float3(normalWS);
    float3 offset = float3(spine) * (HB_BRUSH_SPINE_RANGE * scale / max(HB_BRUSH_TILES_PER_UNIT, 1e-4));

    return offset - normal * dot(offset, normal);
}

float2 HB_BrushScreenOffset(float3 offset, float3 positionWS, half3 normalWS)
{
    float3 dpdx = ddx(positionWS);
    float3 dpdy = ddy(positionWS);

    float3 edgeY = cross(dpdy, float3(normalWS));
    float3 edgeX = cross(float3(normalWS), dpdx);

    float determinant = dot(dpdx, edgeY);

    if (abs(determinant) < 1e-18)
        return float2(0.0, 0.0);

    return float2(dot(offset, edgeY), dot(offset, edgeX)) / determinant;
}

float2 HB_BrushUvShift(float2 screen, float2 uvDdx, float2 uvDdy, float seam)
{
    float2 footprint = max(abs(uvDdx), abs(uvDdy));

    if (max(footprint.x, footprint.y) > seam)
        return float2(0.0, 0.0);

    return screen.x * uvDdx + screen.y * uvDdy;
}

#endif
