#ifndef HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
#define HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D(_HB_SkyLut);
SAMPLER(sampler_HB_SkyLut);

float4 _HB_AmbientParams;

#define HB_AMBIENT_BAKED_WEIGHT _HB_AmbientParams.x
#define HB_LUT_SCALE            _HB_AmbientParams.y
#define HB_LUT_OFFSET           _HB_AmbientParams.z
#define HB_AMBIENT_LIGHT_BIAS   _HB_AmbientParams.w

#define HB_LUT_ROW_AMBIENT  0.125
#define HB_LUT_ROW_SKY      0.375
#define HB_LUT_ROW_SKY_AWAY 0.625
#define HB_LUT_ROW_FOG      0.875

half3 HB_SampleSkyLut(half up, float row)
{
    float u = saturate(up * 0.5h + 0.5h) * HB_LUT_SCALE + HB_LUT_OFFSET;

    return SAMPLE_TEXTURE2D_LOD(_HB_SkyLut, sampler_HB_SkyLut, float2(u, row), 0).rgb;
}

half3 HB_GradientAmbient(half3 directionWS)
{
    return HB_SampleSkyLut(directionWS.y, HB_LUT_ROW_AMBIENT);
}

half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI, half brush)
{
    half3 gradient = HB_SampleSkyLut(normalWS.y + brush, HB_LUT_ROW_AMBIENT);

    half3 light = half3(_HB_KeyDirection.xyz);

    half bias = half(HB_AMBIENT_LIGHT_BIAS)
              * step(0.5h, dot(light, light))
              * half(_HB_KeyDirection.w);

    if (bias > 0.0h)
    {
        half3 alongLight = HB_SampleSkyLut(dot(normalWS, light) + brush, HB_LUT_ROW_AMBIENT)
                         - HB_SampleSkyLut(-1.0h, HB_LUT_ROW_AMBIENT);

        gradient += alongLight * bias;
    }

    half weight = HB_AMBIENT_BAKED_WEIGHT * half(_HB_KeyColor.a);

    return lerp(gradient, bakedGI, weight);
}

half3 HB_AmbientSkyColor()
{
    return HB_SampleSkyLut(1.0h, HB_LUT_ROW_AMBIENT);
}

#endif
