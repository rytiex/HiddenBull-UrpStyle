#ifndef HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
#define HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D(_HB_SkyLut);
SAMPLER(sampler_HB_SkyLut);

float4 _HB_AmbientParams;

#define HB_AMBIENT_BAKED_WEIGHT _HB_AmbientParams.x
#define HB_LUT_SCALE            _HB_AmbientParams.y
#define HB_LUT_OFFSET           _HB_AmbientParams.z

#define HB_LUT_ROW_AMBIENT  0.16667
#define HB_LUT_ROW_SKY      0.50000
#define HB_LUT_ROW_SKY_AWAY 0.83333

half3 HB_SampleSkyLut(half up, float row)
{
    float u = saturate(up * 0.5h + 0.5h) * HB_LUT_SCALE + HB_LUT_OFFSET;

    return SAMPLE_TEXTURE2D_LOD(_HB_SkyLut, sampler_HB_SkyLut, float2(u, row), 0).rgb;
}

half3 HB_GradientAmbient(half3 directionWS)
{
    return HB_SampleSkyLut(directionWS.y, HB_LUT_ROW_AMBIENT);
}

half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI)
{
    half weight = HB_AMBIENT_BAKED_WEIGHT * half(_HB_KeyDirection.w);

    return lerp(HB_GradientAmbient(normalWS), bakedGI, weight);
}

half3 HB_AmbientSkyColor()
{
    return HB_SampleSkyLut(1.0h, HB_LUT_ROW_AMBIENT);
}

#endif
