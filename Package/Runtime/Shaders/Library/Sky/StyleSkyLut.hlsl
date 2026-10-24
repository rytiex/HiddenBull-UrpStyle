#ifndef HB_STYLE_SKY_LUT_INCLUDED
#define HB_STYLE_SKY_LUT_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

TEXTURE2D_ARRAY(_HB_SkyLut);

#define HB_LUT_WIDTH   256.0
#define HB_LUT_AMBIENT 0.0
#define HB_LUT_SKY     1.0
#define HB_LUT_FOG     2.0

half3 HB_SampleSkyLut(half up, float row, float slice)
{
    float u = saturate(float(up) * 0.5 + 0.5) * ((HB_LUT_WIDTH - 1.0) / HB_LUT_WIDTH) + 0.5 / HB_LUT_WIDTH;

    return SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_SkyLut, sampler_LinearClamp, float2(u, row), slice, 0).rgb;
}

#endif
