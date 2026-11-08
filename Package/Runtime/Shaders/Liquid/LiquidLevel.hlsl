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

float4 HB_WaterMixAt(float2 uv)
{
    uint width;
    uint height;
    _HB_WaterMix.GetDimensions(width, height);

    int2 last = int2(width, height) - 1;
    float2 at = saturate(uv) * float2(width, height) - 0.5;
    float2 base = floor(at);
    float2 blend = at - base;
    int2 low = clamp(int2(base), 0, last);
    int2 high = clamp(int2(base) + 1, 0, last);

    float4 a = LOAD_TEXTURE2D_LOD(_HB_WaterMix, low, 0);
    float4 b = LOAD_TEXTURE2D_LOD(_HB_WaterMix, int2(high.x, low.y), 0);
    float4 c = LOAD_TEXTURE2D_LOD(_HB_WaterMix, int2(low.x, high.y), 0);
    float4 d = LOAD_TEXTURE2D_LOD(_HB_WaterMix, high, 0);

    return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
}

HBWaterLevel HB_WaterLevel(float2 uv, float base)
{
    bool inside = HB_WaterInside(uv);
    float cut = SAMPLE_TEXTURE2D_LOD(_HB_WaterLevel, sampler_PointClamp, uv, 0).g;
    half4 mix = half4(HB_WaterMixAt(uv));

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
