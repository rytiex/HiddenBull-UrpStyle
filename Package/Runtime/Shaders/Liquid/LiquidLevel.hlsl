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
#define HB_DEBUG_LIQUID_UNDER 27

struct HBWaterLevel
{
    float level;
    bool wet;
    half fade;
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

float4 HB_WaterSpline(TEXTURE2D_PARAM(map, samplerMap), float2 uv)
{
    uint width;
    uint height;
    map.GetDimensions(width, height);

    float2 size = float2(width, height);
    float2 at = saturate(uv) * size - 0.5;
    float2 base = floor(at);
    float2 f = at - base;
    float2 f2 = f * f;
    float2 f3 = f2 * f;
    float2 w0 = (1.0 - 3.0 * f + 3.0 * f2 - f3) / 6.0;
    float2 w1 = (4.0 - 6.0 * f2 + 3.0 * f3) / 6.0;
    float2 w2 = (1.0 + 3.0 * f + 3.0 * f2 - 3.0 * f3) / 6.0;
    float2 w3 = f3 / 6.0;
    float2 g0 = w0 + w1;
    float2 g1 = w2 + w3;
    float2 low = (base - 0.5 + w1 / g0) / size;
    float2 high = (base + 1.5 + w3 / g1) / size;

    float4 a = SAMPLE_TEXTURE2D_LOD(map, samplerMap, low, 0);
    float4 b = SAMPLE_TEXTURE2D_LOD(map, samplerMap, float2(high.x, low.y), 0);
    float4 c = SAMPLE_TEXTURE2D_LOD(map, samplerMap, float2(low.x, high.y), 0);
    float4 d = SAMPLE_TEXTURE2D_LOD(map, samplerMap, high, 0);

    return (a * g0.x + b * g1.x) * g0.y + (c * g0.x + d * g1.x) * g1.y;
}

float4 HB_WaterMixAt(float2 uv)
{
    return HB_WaterSpline(TEXTURE2D_ARGS(_HB_WaterMix, sampler_LinearClamp), uv);
}

HBWaterLevel HB_WaterLevel(float2 uv, float base)
{
    bool inside = HB_WaterInside(uv);
    float cut = SAMPLE_TEXTURE2D_LOD(_HB_WaterLevel, sampler_LinearClamp, uv, 0).g;
    half4 mix = half4(HB_WaterMixAt(uv));

    mix = inside ? mix : half4(0.0h, 0.0h, 0.0h, 0.0h);

    HBWaterLevel water;
    water.level = HB_WaterHeight(uv, base);
    water.wet = HB_WATER_ON > 0.5 && (!inside || cut < HB_WATER_DRY);
    water.fade = inside ? half(saturate(1.0 - cut / HB_WATER_DRY)) : 1.0h;
    half rest = saturate(1.0h - dot(mix, half4(1.0h, 1.0h, 1.0h, 1.0h)));
    water.mix = mix + rest * half4(HB_WATER_SEA_SLOT == float4(0.0, 1.0, 2.0, 3.0));
    return water;
}

float3 HB_WaterFlow(float2 uv)
{
    return HB_WaterSpline(TEXTURE2D_ARGS(_HB_WaterFlow, sampler_LinearClamp), uv).rgb;
}

float4 _HB_PoolBox;
float4 _HB_PoolFrame;
float4 _HB_PoolDepth;

#define HB_POOL_CENTRE _HB_PoolBox.xy
#define HB_POOL_HALF   _HB_PoolBox.zw
#define HB_POOL_TURN   _HB_PoolFrame.xy
#define HB_POOL_LEVEL  _HB_PoolFrame.z
#define HB_POOL_CELL   _HB_PoolFrame.w
#define HB_POOL_BOTTOM _HB_PoolDepth.x
#define HB_POOL_EDGE   _HB_PoolDepth.y

float2 HB_PoolLocal(float2 xzWS)
{
    float2 away = xzWS - HB_POOL_CENTRE;

    return float2(away.x * HB_POOL_TURN.y - away.y * HB_POOL_TURN.x, away.x * HB_POOL_TURN.x + away.y * HB_POOL_TURN.y);
}

float2 HB_PoolWorld(float2 local)
{
    return HB_POOL_CENTRE + float2(local.x * HB_POOL_TURN.y + local.y * HB_POOL_TURN.x,
                                   local.y * HB_POOL_TURN.y - local.x * HB_POOL_TURN.x);
}

float HB_PoolInward(float2 xzWS)
{
    float2 inward = HB_POOL_HALF - abs(HB_PoolLocal(xzWS));

    return min(inward.x, inward.y);
}

half3 HB_WaterTint(half4 mix)
{
    return half3(_HB_WaterTint[0].rgb) * mix.x + half3(_HB_WaterTint[1].rgb) * mix.y
         + half3(_HB_WaterTint[2].rgb) * mix.z + half3(_HB_WaterTint[3].rgb) * mix.w;
}

#endif
