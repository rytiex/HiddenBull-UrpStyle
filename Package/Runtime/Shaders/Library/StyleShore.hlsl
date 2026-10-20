#ifndef HIDDENBULL_URPSTYLE_SHORE_INCLUDED
#define HIDDENBULL_URPSTYLE_SHORE_INCLUDED

TEXTURE2D(_HB_WaterShore);
SAMPLER(sampler_HB_WaterShore);
TEXTURE2D(_HB_LiquidDryMap);
TEXTURE2D(_HB_LiquidDryFarMap);
TEXTURE2D(_HB_LiquidDryFloor);
TEXTURE2D(_HB_LiquidDryFarFloor);
TEXTURE2D(_HB_LiquidDryRoof);
TEXTURE2D(_HB_LiquidLevelMap);

float4 _HB_WaterFloorParams;
float4 _HB_WaterShoreParams;
float4x4 _HB_LiquidDryViewProj;
float4x4 _HB_LiquidDryFarViewProj;
float4 _HB_LiquidCutState;
float4 _HB_LiquidCutInclude;
float4 _HB_LiquidArea;
float4 _HB_LiquidPools[8];
float4 _HB_LiquidPoolBoxes[8];
float4 _HB_LiquidPoolSides[8];
float _HB_LiquidPoolCount;

#define HB_SHORE_KNOWN      (_HB_WaterFloorParams.w > 0.5 && _HB_WaterShoreParams.y > 0.5)
#define HB_SHORE_SWELL      _HB_WaterShoreParams.z
#define HB_SHORE_SWELL_REACH 32.0
#define HB_SHORE_WET_REACH  2.5
#define HB_SHORE_WET_RISE   0.6
#define HB_SHORE_SUNK       0.05
#define HB_CUT_INCLUDE      8.0
#define HB_CUT_GUARD        0.03
#define HB_CUT_TIGHT        0.09
#define HB_CUT_ANY          (_HB_LiquidCutState.x > 0.5)
#define HB_CUT_DRY          (_HB_LiquidCutState.z > 0.5)
#define HB_CUT_SEA          _HB_LiquidCutState.w
#define HB_DRY_SIZE         512
#define HB_DRY_RIM          0.45
#define HB_DRY_EDGE         0.49
#define HB_DRY_HEIGHT       10000.0
#define HB_DRY_UNDER        0.15
#define HB_DRY_REACH        0.35
#define HB_POOL_MOST        8
#define HB_POOL_UNDER       0.5
#define HB_POOL_WET_EDGE    0.25

float4 HB_LevelRead(TEXTURE2D(map), float4 area, float2 positionXZ)
{
    float2 size;

    map.GetDimensions(size.x, size.y);

    float2 at = clamp(((positionXZ - area.xy) * area.zw + 0.5) * size - 0.5, 0.0, size - 1.0);
    int2 corner = (int2)floor(at);
    int2 next = min(corner + 1, (int2)size - 1);
    float2 blend = at - corner;

    float4 low = lerp(LOAD_TEXTURE2D(map, corner), LOAD_TEXTURE2D(map, int2(next.x, corner.y)), blend.x);
    float4 high = lerp(LOAD_TEXTURE2D(map, int2(corner.x, next.y)), LOAD_TEXTURE2D(map, next), blend.x);

    return lerp(low, high, blend.y);
}

float HB_ShoreRise(float2 positionXZ)
{
    float2 stored = HB_LevelRead(_HB_LiquidLevelMap, _HB_LiquidArea, positionXZ).rg;

    return stored.y > 0.5 ? -HB_DRY_HEIGHT : stored.x;
}

float HB_ShoreLevel(float2 positionXZ)
{
    return _HB_WaterShoreParams.x + HB_ShoreRise(positionXZ);
}

float HB_LiquidCutKept(float value)
{
    return floor((value + HB_CUT_GUARD) / HB_CUT_INCLUDE);
}

bool HB_LiquidCutHidden(float value, float need)
{
    float kept = HB_LiquidCutKept(value);

    return value - kept * HB_CUT_INCLUDE > 0.5 || (need > 0.5 && kept < 0.5);
}

float2 HB_LiquidDryUV(float3 positionWS, bool wide)
{
    float4 projected = wide ? mul(_HB_LiquidDryFarViewProj, float4(positionWS, 1.0))
                            : mul(_HB_LiquidDryViewProj, float4(positionWS, 1.0));
    float2 uv = projected.xy;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = -uv.y;
#endif
    return uv * 0.5 + 0.5;
}

bool HB_LiquidDryWithin(float2 uv)
{
    return all(abs(uv - 0.5) < HB_DRY_EDGE);
}

float4 HB_LiquidDryLoad(int2 texel, bool wide)
{
    texel = clamp(texel, 0, HB_DRY_SIZE - 1);

    float4 value;

    if (wide)
        value = LOAD_TEXTURE2D(_HB_LiquidDryFarMap, texel);
    else
        value = LOAD_TEXTURE2D(_HB_LiquidDryMap, texel);

    return value;
}

float HB_LiquidDryTexel(int2 texel, float channel, bool wide)
{
    float4 value = HB_LiquidDryLoad(texel, wide);

    return HB_LiquidCutHidden(value[(int)channel], _HB_LiquidCutInclude[(int)channel]) ? 1.0 : 0.0;
}

float HB_LiquidDryOver(int2 texel)
{
    float stored = LOAD_TEXTURE2D(_HB_LiquidDryRoof, clamp(texel, 0, HB_DRY_SIZE - 1)).r;

    return stored > 0.0 ? stored - HB_DRY_HEIGHT : HB_DRY_HEIGHT;
}

bool HB_LiquidDryAt(float3 positionWS, float channel)
{
    float2 uv = HB_LiquidDryUV(positionWS, false);
    bool wide = !HB_LiquidDryWithin(uv);

    if (wide)
        uv = HB_LiquidDryUV(positionWS, true);

    bool dry = false;

    if (HB_LiquidDryWithin(uv))
    {
        int2 texel = (int2)floor(uv * HB_DRY_SIZE);

        dry = HB_LiquidDryTexel(texel, channel, wide) > 0.5
           && (wide || positionWS.y < HB_LiquidDryOver(texel) + HB_DRY_UNDER);
    }

    return dry;
}

bool HB_LiquidDryPoint(float3 positionWS, float channel)
{
    bool dry = false;

    UNITY_BRANCH
    if (HB_CUT_DRY)
    {
        dry = HB_LiquidDryAt(positionWS, channel)
           || HB_LiquidDryAt(positionWS + float3(HB_DRY_REACH, 0.0, 0.0), channel)
           || HB_LiquidDryAt(positionWS - float3(HB_DRY_REACH, 0.0, 0.0), channel)
           || HB_LiquidDryAt(positionWS + float3(0.0, 0.0, HB_DRY_REACH), channel)
           || HB_LiquidDryAt(positionWS - float3(0.0, 0.0, HB_DRY_REACH), channel);
    }

    return dry;
}

float HB_LiquidDryUnder(int2 texel, bool wide)
{
    texel = clamp(texel, 0, HB_DRY_SIZE - 1);

    float stored;

    if (wide)
        stored = LOAD_TEXTURE2D(_HB_LiquidDryFarFloor, texel).r;
    else
        stored = LOAD_TEXTURE2D(_HB_LiquidDryFloor, texel).r;

    return stored > 0.0 ? HB_DRY_HEIGHT - stored : -HB_DRY_HEIGHT;
}

half HB_LiquidDry(float3 positionWS, float channel)
{
    half dry = 0.0h;

    UNITY_BRANCH
    if (HB_CUT_DRY && channel >= 0.0)
    {
        float2 uv = HB_LiquidDryUV(positionWS, false);
        bool wide = !HB_LiquidDryWithin(uv);

        if (wide)
            uv = HB_LiquidDryUV(positionWS, true);

        UNITY_BRANCH
        if (HB_LiquidDryWithin(uv))
        {
            float2 at = uv * HB_DRY_SIZE - 0.5;
            int2 corner = (int2)floor(at);
            float2 blend = at - corner;

            float low = lerp(HB_LiquidDryTexel(corner, channel, wide),
                             HB_LiquidDryTexel(corner + int2(1, 0), channel, wide), blend.x);
            float high = lerp(HB_LiquidDryTexel(corner + int2(0, 1), channel, wide),
                              HB_LiquidDryTexel(corner + int2(1, 1), channel, wide), blend.x);

            float under = max(max(HB_LiquidDryUnder(corner, wide), HB_LiquidDryUnder(corner + int2(1, 0), wide)),
                              max(HB_LiquidDryUnder(corner + int2(0, 1), wide),
                                  HB_LiquidDryUnder(corner + int2(1, 1), wide)));

            dry = positionWS.y > under - HB_DRY_UNDER ? half(lerp(low, high, blend.y)) : 0.0h;
        }
    }

    return dry;
}

bool HB_LiquidDryTight(float3 positionWS, float channel)
{
    float2 uv = HB_LiquidDryUV(positionWS, false);
    bool wide = !HB_LiquidDryWithin(uv);

    if (wide)
        uv = HB_LiquidDryUV(positionWS, true);

    int2 corner = (int2)floor(uv * HB_DRY_SIZE - 0.5);
    float tight = 0.0;

    UNITY_UNROLL
    for (int tap = 0; tap < 4; tap++)
    {
        float4 value = HB_LiquidDryLoad(corner + int2(tap & 1, tap >> 1), wide);

        tight = max(tight, frac(value[(int)channel] + HB_CUT_GUARD));
    }

    return tight > HB_CUT_TIGHT;
}

half HB_LiquidDryInside(float3 positionWS, float channel)
{
    half dry = HB_LiquidDry(positionWS, channel);

    UNITY_BRANCH
    if (dry > 0.0h && !HB_LiquidDryTight(positionWS, channel))
    {
        dry = min(dry, HB_LiquidDry(positionWS + float3(HB_DRY_RIM, 0.0, 0.0), channel));
        dry = min(dry, HB_LiquidDry(positionWS - float3(HB_DRY_RIM, 0.0, 0.0), channel));
        dry = min(dry, HB_LiquidDry(positionWS + float3(0.0, 0.0, HB_DRY_RIM), channel));
        dry = min(dry, HB_LiquidDry(positionWS - float3(0.0, 0.0, HB_DRY_RIM), channel));
    }

    return dry;
}

half HB_PoolWet(float3 positionWS)
{
    half wet = 0.0h;
    int count = min((int)_HB_LiquidPoolCount, HB_POOL_MOST);

    UNITY_LOOP
    for (int index = 0; index < count; index++)
    {
        float4 pool = _HB_LiquidPools[index];
        float4 box = _HB_LiquidPoolBoxes[index];
        float above = positionWS.y - pool.z;

        if (above > HB_SHORE_WET_RISE || positionWS.y < pool.w - HB_POOL_UNDER)
            continue;

        float2 offset = positionWS.xz - pool.xy;
        float2 inside = float2(offset.x * box.w - offset.y * box.z, offset.x * box.z + offset.y * box.w);
        float2 inward = box.xy - abs(inside);
        float beside = saturate(1.0 + min(inward.x, inward.y) / HB_POOL_WET_EDGE);

        if (beside <= 0.0)
            continue;

        half here = half(beside * saturate(1.0 - above / HB_SHORE_WET_RISE));

        here *= 1.0h - HB_LiquidDryInside(positionWS, _HB_LiquidPoolSides[index].x);
        wet = max(wet, here);
    }

    return wet;
}

half HB_ShoreWet(float3 positionWS)
{
    half wet = 0.0h;

    UNITY_BRANCH
    if (HB_SHORE_KNOWN)
    {
        float level = HB_ShoreLevel(positionWS.xz);

        UNITY_BRANCH
        if (positionWS.y < level + HB_SHORE_WET_RISE + HB_SHORE_SWELL)
        {
            float2 uv = (positionWS.xz - _HB_WaterFloorParams.xy) * _HB_WaterFloorParams.z + 0.5;
            float above = positionWS.y - level;
            float beside = above < 0.0 ? 1.0 : 0.0;
            float rise = HB_SHORE_WET_RISE;

            UNITY_BRANCH
            if (all(abs(uv - 0.5) < 0.49))
            {
                float away = SAMPLE_TEXTURE2D_LOD(_HB_WaterShore, sampler_HB_WaterShore, uv, 0).x;

                beside = saturate(1.0 + away / HB_SHORE_WET_REACH);
                rise += HB_SHORE_SWELL * saturate(away / HB_SHORE_SWELL_REACH);
            }

            wet = half(beside * saturate(1.0 - above / rise));
            wet *= 1.0h - HB_LiquidDryInside(positionWS, HB_CUT_SEA);
        }
    }

    UNITY_BRANCH
    if (_HB_LiquidPoolCount > 0.5)
        wet = max(wet, HB_PoolWet(positionWS));

    return wet;
}

#endif
