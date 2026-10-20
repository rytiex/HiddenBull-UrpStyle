#ifndef HIDDENBULL_URPSTYLE_SKY_EXPOSURE_INCLUDED
#define HIDDENBULL_URPSTYLE_SKY_EXPOSURE_INCLUDED

#include "StyleShore.hlsl"

TEXTURE2D_FLOAT(_HB_Shelter);
TEXTURE2D_FLOAT(_HB_ShelterRain);
TEXTURE2D_FLOAT(_HB_ShelterFar);
TEXTURE2D_FLOAT(_HB_ShelterFloor);
TEXTURE2D_FLOAT(_HB_ShelterFarFloor);
TEXTURE2D_HALF(_HB_ShelterFade);

float4 _HB_ShelterParams;
float4 _HB_ShelterState;
float4 _HB_ShelterFarParams;

#define HB_SHELTER_TEXEL        _HB_ShelterParams.x
#define HB_SHELTER_RCP_SIZE     _HB_ShelterParams.y
#define HB_SHELTER_CENTRE       _HB_ShelterParams.zw
#define HB_SHELTER_BOUND        _HB_ShelterState.x
#define HB_SHELTER_REACH        _HB_ShelterState.y
#define HB_SHELTER_BASE         _HB_ShelterState.z
#define HB_SHELTER_FAR_BOUND    _HB_ShelterState.w
#define HB_SHELTER_FAR_CENTRE   _HB_ShelterFarParams.xy
#define HB_SHELTER_FAR_RCP      _HB_ShelterFarParams.z
#define HB_SHELTER_FAR_TEXEL    _HB_ShelterFarParams.w

#define HB_SKY_EXPOSURE_LIFT    0.35
#define HB_SKY_EXPOSURE_BIAS    0.1
#define HB_SKY_EXPOSURE_RANGE   0.6

#define HB_SHELTER_FAR_BIAS     0.75
#define HB_SHELTER_FAR_STEEP    4.0
#define HB_SHELTER_FAR_BLEND    8.0
#define HB_SHELTER_FAR_MARGIN   2.0
#define HB_SHELTER_REACH_FADE   4.0
#define HB_SHELTER_COARSE_BIAS  0.3
#define HB_SHELTER_COARSE_RANGE 0.4

float HB_ShelterFloorAt(float2 cell)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_ShelterFloor, sampler_PointRepeat, (cell + 0.5) * HB_SHELTER_RCP_SIZE, 0).r;
}

half HB_ShelterOpen(float2 cell, float height, bool rain)
{
    float2 uv = (cell + 0.5) * HB_SHELTER_RCP_SIZE;
    float shelter;

    if (rain)
        shelter = SAMPLE_TEXTURE2D_LOD(_HB_ShelterRain, sampler_PointRepeat, uv, 0).r + HB_SHELTER_BASE;
    else
        shelter = SAMPLE_TEXTURE2D_LOD(_HB_Shelter, sampler_PointRepeat, uv, 0).r;

    float covered = saturate((shelter - height - HB_SKY_EXPOSURE_BIAS) / HB_SKY_EXPOSURE_RANGE);

    UNITY_BRANCH
    if (covered > 0.0)
        covered *= 1.0 - saturate((HB_ShelterFloorAt(cell) - height) / HB_SHELTER_REACH_FADE);

    return half(1.0 - covered);
}

half4 HB_BSplineWeights(half t)
{
    half t2 = t * t;
    half t3 = t2 * t;
    half s = 1.0h - t;

    return half4(s * s * s, 3.0h * t3 - 6.0h * t2 + 4.0h, -3.0h * t3 + 3.0h * t2 + 3.0h * t + 1.0h, t3)
         * (1.0h / 6.0h);
}

half HB_ShelterFarOpen(float2 uv, float height, float bias, float texel)
{
    float shelter = SAMPLE_TEXTURE2D_LOD(_HB_ShelterFar, sampler_PointClamp, uv, 0).r;
    float covered = saturate((shelter - height - bias) / texel);

    UNITY_BRANCH
    if (covered > 0.0)
    {
        float lowest = SAMPLE_TEXTURE2D_LOD(_HB_ShelterFarFloor, sampler_PointClamp, uv, 0).r;

        covered *= 1.0 - saturate((lowest - height) / HB_SHELTER_REACH_FADE);
    }

    return half(1.0 - covered);
}

half HB_ShelterFar(float2 probe, float height, half normalY)
{
    float2 offset = probe - HB_SHELTER_FAR_CENTRE;
    float texel = HB_SHELTER_FAR_TEXEL;
    float reach = 0.5 / HB_SHELTER_FAR_RCP - texel * HB_SHELTER_FAR_MARGIN;

    if (HB_SHELTER_FAR_BOUND < 0.5 || any(abs(offset) > reach))
        return HB_SHORE_KNOWN && height < HB_ShoreLevel(probe) - HB_SHORE_SUNK ? 0.0h : 1.0h;

    float steep = sqrt(saturate(1.0 - float(normalY) * float(normalY))) / max(float(normalY), 0.2);
    float bias = texel * (HB_SHELTER_FAR_BIAS + min(steep, HB_SHELTER_FAR_STEEP));

    float size = 1.0 / (HB_SHELTER_FAR_RCP * texel);
    float2 position = (offset * HB_SHELTER_FAR_RCP + 0.5) * size - 0.5;
    float2 cell = floor(position);
    float2 blend = position - cell;
    float2 corner = (cell + 0.5) / size;
    float pitch = 1.0 / size;

    half4 open = half4(HB_ShelterFarOpen(corner, height, bias, texel),
                       HB_ShelterFarOpen(corner + float2(pitch, 0.0), height, bias, texel),
                       HB_ShelterFarOpen(corner + float2(0.0, pitch), height, bias, texel),
                       HB_ShelterFarOpen(corner + float2(pitch, pitch), height, bias, texel));

    return lerp(lerp(open.x, open.y, half(blend.x)), lerp(open.z, open.w, half(blend.x)), half(blend.y));
}

half HB_ShelterBelowReach(float2 probe, float height)
{
    float2 away = abs(probe - HB_SHELTER_CENTRE);

    if (HB_SHELTER_BOUND < 0.5 || HB_SHELTER_REACH - max(away.x, away.y) <= 0.0)
        return 0.0h;

    float2 position = probe / HB_SHELTER_TEXEL - 0.5;
    float2 cell = floor(position);
    float2 blend = position - cell;

    float4 lowest = float4(HB_ShelterFloorAt(cell), HB_ShelterFloorAt(cell + float2(1.0, 0.0)),
                           HB_ShelterFloorAt(cell + float2(0.0, 1.0)), HB_ShelterFloorAt(cell + float2(1.0, 1.0)));
    float4 open = saturate((lowest - height) / HB_SHELTER_REACH_FADE);

    return half(lerp(lerp(open.x, open.y, blend.x), lerp(open.z, open.w, blend.x), blend.y));
}

half HB_ShelterFade(float2 positionXZ)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_ShelterFade, sampler_LinearRepeat,
                                positionXZ / HB_SHELTER_TEXEL * HB_SHELTER_RCP_SIZE, 0).r;
}

half HB_SkyExposureCoarse(float3 positionWS, half normalY)
{
    float2 away = abs(positionWS.xz - HB_SHELTER_CENTRE);

    if (HB_SHELTER_BOUND < 0.5 || HB_SHELTER_REACH - max(away.x, away.y) <= 0.0)
        return HB_ShelterFar(positionWS.xz, positionWS.y, normalY);

    float2 uv = (floor(positionWS.xz / HB_SHELTER_TEXEL) + 0.5) * HB_SHELTER_RCP_SIZE;
    float shelter = SAMPLE_TEXTURE2D_LOD(_HB_Shelter, sampler_PointRepeat, uv, 0).r;

    half open = half(1.0 - saturate((shelter - positionWS.y - HB_SHELTER_COARSE_BIAS) / HB_SHELTER_COARSE_RANGE));

    return max(open, HB_ShelterBelowReach(positionWS.xz, positionWS.y)) * HB_ShelterFade(positionWS.xz);
}

half HB_SkyExposure(float3 positionWS, half3 normalWS, float2 shift, bool rain)
{
    float2 probe = positionWS.xz + float2(normalWS.xz) * HB_SKY_EXPOSURE_LIFT + shift;
    float2 away = abs(probe - HB_SHELTER_CENTRE);
    float inside = HB_SHELTER_REACH - max(away.x, away.y);

    if (HB_SHELTER_BOUND < 0.5 || inside <= 0.0)
        return HB_ShelterFar(probe, positionWS.y, normalWS.y);

    float2 position = probe / HB_SHELTER_TEXEL - 0.5;
    float2 cell = floor(position);

    half4 weightX = HB_BSplineWeights(half(position.x - cell.x));
    half4 weightZ = HB_BSplineWeights(half(position.y - cell.y));

    half exposure = 0.0h;

    UNITY_UNROLL
    for (int z = 0; z < 4; z++)
    {
        half row = 0.0h;

        UNITY_UNROLL
        for (int x = 0; x < 4; x++)
            row += HB_ShelterOpen(cell + float2(x - 1, z - 1), positionWS.y, rain) * weightX[x];

        exposure += row * weightZ[z];
    }

    exposure *= HB_ShelterFade(probe);

    UNITY_BRANCH
    if (inside < HB_SHELTER_FAR_BLEND)
        exposure = lerp(HB_ShelterFar(probe, positionWS.y, normalWS.y), exposure, half(inside / HB_SHELTER_FAR_BLEND));

    return exposure;
}

#endif
