#ifndef HB_LIQUID_LEVEL_INCLUDED
#define HB_LIQUID_LEVEL_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

TEXTURE2D(_HB_WaterLevel);
TEXTURE2D(_HB_WaterMix);
TEXTURE2D(_HB_WaterFlow);

float4 _HB_WaterSea;
float4 _HB_WaterTint[4];

#define HB_WATER_SEA_LEVEL _HB_WaterSea.x
#define HB_WATER_ON        _HB_WaterSea.y
#define HB_WATER_RANGE     _HB_WaterSea.z
#define HB_WATER_SEA_SLOT  _HB_WaterSea.w
#define HB_WATER_DRY       0.5
#define HB_WATER_UNIT      65535.0
#define HB_WATER_ZERO      32768.0
#define HB_WATER_SPAN      32767.0

#define HB_DEBUG_LIQUID_DEPTH 23
#define HB_DEBUG_LIQUID_FLOW  24
#define HB_DEBUG_LIQUID_WAVES 25
#define HB_DEBUG_LIQUID_FOAM  26

struct HBWaterLevel
{
    float level;
    bool wet;
    half4 mix;
};

bool HB_WaterInside(float2 uv)
{
    return all(uv >= 0.0) && all(uv <= 1.0);
}

float HB_WaterHeight(float2 uv, float base)
{
    float raised = SAMPLE_TEXTURE2D_LOD(_HB_WaterLevel, sampler_LinearClamp, uv, 0).r;
    float offset = HB_WaterInside(uv) ? (raised * HB_WATER_UNIT - HB_WATER_ZERO) / HB_WATER_SPAN * HB_WATER_RANGE
                                      : 0.0;

    return base + HB_WATER_SEA_LEVEL + offset;
}

HBWaterLevel HB_WaterLevel(float2 uv, float base)
{
    bool inside = HB_WaterInside(uv);
    float cut = SAMPLE_TEXTURE2D_LOD(_HB_WaterLevel, sampler_PointClamp, uv, 0).g;
    half4 mix = half4(SAMPLE_TEXTURE2D_LOD(_HB_WaterMix, sampler_LinearClamp, uv, 0));

    mix = inside ? mix : half4(0.0h, 0.0h, 0.0h, 0.0h);

    HBWaterLevel water;
    water.level = HB_WaterHeight(uv, base);
    water.wet = HB_WATER_ON > 0.5 && (!inside || cut < HB_WATER_DRY);
    half rest = saturate(1.0h - dot(mix, half4(1.0h, 1.0h, 1.0h, 1.0h)));
    water.mix = mix + rest * half4(HB_WATER_SEA_SLOT == float4(0.0, 1.0, 2.0, 3.0));
    return water;
}

float3 HB_WaterFlow(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_WaterFlow, sampler_LinearClamp, uv, 0).rgb;
}

half3 HB_WaterTint(half4 mix)
{
    return half3(_HB_WaterTint[0].rgb) * mix.x + half3(_HB_WaterTint[1].rgb) * mix.y
         + half3(_HB_WaterTint[2].rgb) * mix.z + half3(_HB_WaterTint[3].rgb) * mix.w;
}

#endif
