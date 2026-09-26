#ifndef HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
#define HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED

#include "StyleCommon.hlsl"

TEXTURE2D_ARRAY(_HB_SkyLut);
SAMPLER(sampler_HB_SkyLut);

float4 _HB_AmbientParams;
float4 _HB_SkyLutRemap;
float4 _HB_AmbientFloor;

#define HB_AMBIENT_LIGHT_BIAS _HB_AmbientParams.x
#define HB_AMBIENT_INTENSITY  _HB_AmbientParams.y
#define HB_SKY_INTENSITY      _HB_AmbientParams.z

#define HB_AMBIENT_OPEN       _HB_AmbientFloor.w

#define HB_LUT_SCALE          _HB_SkyLutRemap.x
#define HB_LUT_OFFSET         _HB_SkyLutRemap.y
#define HB_LUT_TIME           _HB_SkyLutRemap.z
#define HB_LUT_AWAY           _HB_SkyLutRemap.w

#define HB_LUT_ROWS    3.0
#define HB_SKY_TIME    ((HB_LUT_TIME * HB_LUT_ROWS - 0.5) / (HB_LUT_ROWS - 1.0))

#define HB_LUT_AMBIENT 0.0
#define HB_LUT_SKY     1.0
#define HB_LUT_FOG     2.0

half3 HB_SampleSkyLut(half up, float time, float slice)
{
    float u = saturate(up * 0.5h + 0.5h) * HB_LUT_SCALE + HB_LUT_OFFSET;

    return SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_SkyLut, sampler_HB_SkyLut,
                                      float2(u, time), slice, 0).rgb;
}

half3 HB_SampleAmbientLut(half up)
{
    return HB_SampleSkyLut(up, HB_LUT_TIME, HB_LUT_AMBIENT) * half(HB_AMBIENT_INTENSITY);
}

half3 HB_GradientAmbient(half3 directionWS)
{
    return HB_SampleAmbientLut(directionWS.y);
}

half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI, half brush, half bakedTones, half softness,
                        out half bakedVisibility)
{
    half3 gradient = HB_SampleAmbientLut(normalWS.y + brush);

    half3 light = half3(_HB_KeyDirection.xyz);

    half bias = half(HB_AMBIENT_LIGHT_BIAS)
              * step(0.5h, dot(light, light))
              * half(_HB_KeyDirection.w);

    if (bias > 0.0h)
    {
        half3 alongLight = HB_SampleAmbientLut(dot(normalWS, light) + brush)
                         - half3(_HB_AmbientFloor.rgb);

        gradient += alongLight * bias;
    }

#ifdef LIGHTMAP_ON
    half ratio = Luminance(bakedGI) / max(Luminance(gradient), HB_EPSILON);

    bakedVisibility = saturate(ratio);

    half banded = HB_BakedToneBands(ratio, softness) / max(ratio, HB_EPSILON);
    half3 toned = bakedGI * lerp(1.0h, banded, saturate(bakedTones));

    return lerp(gradient, toned, half(_HB_KeyColor.a));
#else
    bakedVisibility = 1.0h;

    return gradient;
#endif
}

half3 HB_AmbientSkyColor()
{
    return HB_SampleAmbientLut(1.0h);
}

#endif
