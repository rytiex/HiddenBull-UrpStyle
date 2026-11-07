#ifndef HB_LIQUID_WAVES_INCLUDED
#define HB_LIQUID_WAVES_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidLevel.hlsl"

TEXTURE2D_ARRAY(_HB_WaveDisplacement);
TEXTURE2D_ARRAY(_HB_WaveDerivative);
TEXTURE2D(_HB_LiquidCalm);
TEXTURE2D(_HB_LiquidTiles);
SAMPLER(sampler_trilinear_repeat);

float4 _HB_TerrainSize;
float4 _HB_TerrainOrigin;
float4 _HB_WaveScale;
float4 _HB_WaveTexel;
float4 _HB_WaveFrame;
float4 _HB_LiquidWaves[4];
float4 _HB_LiquidCalmGrid;

#define HB_WAVE_BANDS         3
#define HB_WAVE_BOUND         (_HB_WaveFrame.z > 0.5)
#define HB_WAVE_WIND          _HB_WaveFrame.w
#define HB_WAVE_SECOND_SCALE  0.618
#define HB_WAVE_SECOND_SHARE  0.7
#define HB_WAVE_LENGTH_MIN    0.05
#define HB_WAVE_SHARE_LEAST   1e-3
#define HB_WAVE_FOLD_LIMIT    1.0
#define HB_WAVE_SHOAL_DEPTH   8.0
#define HB_WAVE_TROUGH        3.0
#define HB_WAVE_NEAR          150.0
#define HB_WAVE_FADE          450.0
#define HB_WAVE_SHORE_BAND    1
#define HB_WAVE_SHORE_SCATTER 0.137
#define HB_WAVE_SHORE_SHARE   0.7
#define HB_WAVE_SHORE_GAIN    1.6
#define HB_WAVE_SHORE_DEPTH   10.0
#define HB_WAVE_SHORE_LEAST   1e-3
#define HB_WAVE_STREAM_TURN   1.0
#define HB_WAVE_HEADING_SURE  0.25
#define HB_WAVE_CAP_CALM      0.6
#define HB_WAVE_CAP_STORM     0.22
#define HB_WAVE_CAP_FROM      0.35
#define HB_WAVE_CAP_RANGE     0.4
#define HB_WAVE_TRAIL_STEP    1.6
#define HB_WAVE_TRAIL_NEAR    0.7
#define HB_WAVE_TRAIL_FAR     0.4
#define HB_CALM_SLOPE         _HB_LiquidCalmGrid.x
#define HB_CALM_TILE          _HB_LiquidCalmGrid.y
#define HB_CALM_TILES         _HB_LiquidCalmGrid.z
#define HB_CALM_BOUND         (_HB_LiquidCalmGrid.w > 0.5)

struct HBLiquidWaveShape
{
    float amount;
    float height;
    float steepness;
    float length;
    float cap;
    float4 mix;
};

struct HBLiquidSea
{
    float wind;
    float shore;
    float depth;
};

HBLiquidWaveShape HB_LiquidWaveShape(half4 mix)
{
    HBLiquidWaveShape shape = (HBLiquidWaveShape)0;
    float total = 0.0;

    UNITY_UNROLL
    for (int slot = 0; slot < 4; slot++)
    {
        float share = mix[slot];
        float4 wave = _HB_LiquidWaves[slot];

        shape.amount += (wave.x < 0.0 ? HB_WAVE_WIND : wave.x) * share;
        shape.height += wave.y * share;
        shape.steepness += wave.z * share;
        shape.length += wave.w * share;
        shape.cap += (wave.x < 0.0 ? lerp(HB_WAVE_CAP_CALM, HB_WAVE_CAP_STORM, saturate(HB_WAVE_WIND))
                                   : HB_WAVE_CAP_FROM) * share;
        total += share;
    }

    float scale = rcp(max(total, 1e-3));

    shape.amount *= scale;
    shape.height *= scale;
    shape.steepness *= scale;
    shape.length = max(shape.length * scale, HB_WAVE_LENGTH_MIN);
    shape.cap *= scale;
    shape.mix = float4(mix);

    if (!HB_WAVE_BOUND)
        shape.amount = 0.0;

    return shape;
}

float2 HB_LiquidTerrainUV(float2 xzWS)
{
    return (xzWS - _HB_TerrainOrigin.xz) / _HB_TerrainSize.xz;
}

float4 HB_LiquidCalmAt(float2 xzWS)
{
    float2 uv = HB_LiquidTerrainUV(xzWS);
    float2 inside = saturate(uv);
    float4 calm = SAMPLE_TEXTURE2D_LOD(_HB_LiquidCalm, sampler_LinearClamp, inside, 0);

    calm.x += length((uv - inside) * _HB_TerrainSize.xz) * HB_CALM_SLOPE;
    return calm;
}

HBLiquidSea HB_LiquidSeaAt(float2 xzWS, HBLiquidWaveShape shape)
{
    float size = shape.height * shape.length;
    float reach = HB_WAVE_SHOAL_DEPTH * max(size, 1.0);
    float trough = HB_WAVE_TROUGH * shape.amount * size;
    float2 uv = HB_LiquidTerrainUV(xzWS);
    float3 flow = HB_WaterInside(uv) ? HB_WaterFlow(uv) : float3(0.0, 0.0, 0.0);
    float4 calm = HB_CALM_BOUND ? HB_LiquidCalmAt(xzWS) : float4(reach, 0.0, 0.0, 0.0);

    float open = saturate(calm.x / reach);
    float shoal = min(open, calm.x / max(trough, 1e-3)) * (1.0 - saturate(flow.z));
    float close = 1.0 - saturate(calm.x / HB_WAVE_SHORE_DEPTH);
    float rush = saturate(length(flow.xy) / HB_WAVE_STREAM_TURN);
    float turn = max(close * close * (3.0 - 2.0 * close) * calm.w, rush);

    HBLiquidSea sea;
    sea.wind = shoal * (1.0 - turn * HB_WAVE_SHORE_SHARE);
    sea.shore = shoal * turn * HB_WAVE_SHORE_GAIN;
    sea.depth = calm.x;
    return sea;
}

float HB_WaveNear(float distance)
{
    return 1.0 - saturate((distance - HB_WAVE_NEAR) / HB_WAVE_FADE);
}

float HB_WavePush(HBLiquidWaveShape shape, float wind)
{
    return min(shape.height * shape.steepness, HB_WAVE_FOLD_LIMIT / max(shape.amount * wind, 1e-3));
}

float2 HB_WaveFramed(float2 xz)
{
    return float2(dot(xz, _HB_WaveFrame.xy), dot(xz, float2(-_HB_WaveFrame.y, _HB_WaveFrame.x)));
}

float2 HB_WaveWorld(float2 framed)
{
    return _HB_WaveFrame.xy * framed.x + float2(-_HB_WaveFrame.y, _HB_WaveFrame.x) * framed.y;
}

float2 HB_WaveTurn(float2 value, float way)
{
    return float2(value.x * 0.8 - value.y * (0.6 * way), value.x * (0.6 * way) + value.y * 0.8);
}

void HB_WaveTiles(float2 xzWS, out float2 base, out float2 blend)
{
    float2 grid = (xzWS - _HB_TerrainOrigin.xz) / HB_CALM_TILE - 0.5;

    base = floor(grid);
    blend = grid - base;
    blend = blend * blend * (3.0 - 2.0 * blend);
}

float2 HB_WaveTileCentre(float2 base, float2 tile)
{
    return _HB_TerrainOrigin.xz + (base + tile + 0.5) * HB_CALM_TILE;
}

float2 HB_WaveTileHeading(float2 base, float2 tile)
{
    int2 index = int2(clamp(base + tile, 0.0, HB_CALM_TILES - 1.0));
    float2 heading = LOAD_TEXTURE2D_LOD(_HB_LiquidTiles, index, 0).xy;

    return dot(heading, heading) > HB_WAVE_HEADING_SURE ? heading : _HB_WaveFrame.xy;
}

float HB_WaveSlotLength(int slot)
{
    return max(_HB_LiquidWaves[slot].w, HB_WAVE_LENGTH_MIN);
}

float3 HB_WaveWindOffsetAt(float2 restXZ, float wide, float spacing)
{
    float2 framed = HB_WaveFramed(restXZ / wide);
    float3 moved = float3(0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int band = 0; band < HB_WAVE_BANDS; band++)
    {
        float lod = max(log2(spacing / wide / _HB_WaveTexel[band]), 0.0);

        moved += SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDisplacement, sampler_trilinear_repeat,
                                            framed * _HB_WaveScale[band], band, lod).xyz;
    }

    float2 pushed = HB_WaveWorld(moved.xz);

    return float3(pushed.x, moved.y, pushed.y) * wide;
}

float3 HB_WaveTileOffset(float2 restXZ, float spacing)
{
    float2 base;
    float2 blend;

    HB_WaveTiles(restXZ, base, blend);

    float lod = max(log2(spacing / _HB_WaveTexel[HB_WAVE_SHORE_BAND]), 0.0);
    float3 total = float3(0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int corner = 0; corner < 4; corner++)
    {
        float2 tile = float2(corner & 1, corner >> 1);
        float2 centre = HB_WaveTileCentre(base, tile);
        float2 heading = HB_WaveTileHeading(base, tile);
        float2 side = float2(-heading.y, heading.x);
        float2 share = lerp(1.0 - blend, blend, tile);
        float2 local = restXZ - centre;

        float2 uv = float2(dot(local, heading), dot(local, side)) * _HB_WaveScale[HB_WAVE_SHORE_BAND]
                  + frac(centre * HB_WAVE_SHORE_SCATTER);
        float3 moved = SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDisplacement, sampler_trilinear_repeat, uv,
                                                  HB_WAVE_SHORE_BAND, lod).xyz;
        float2 pushed = heading * moved.x + side * moved.z;

        total += float3(pushed.x, moved.y, pushed.y) * (share.x * share.y);
    }

    return total;
}

float3 HB_WaveWindOffset(float2 restXZ, HBLiquidWaveShape shape, float spacing)
{
    float3 moved = float3(0.0, 0.0, 0.0);
    float total = 0.0;

    UNITY_LOOP
    for (int slot = 0; slot < 4; slot++)
    {
        float share = shape.mix[slot];

        UNITY_BRANCH
        if (share > HB_WAVE_SHARE_LEAST)
        {
            moved += HB_WaveWindOffsetAt(restXZ, HB_WaveSlotLength(slot), spacing) * share;
            total += share;
        }
    }

    return moved * rcp(max(total, HB_WAVE_SHARE_LEAST));
}

float3 HB_WaveOffset(float2 restXZ, HBLiquidWaveShape shape, HBLiquidSea sea, float spacing)
{
    float wind = shape.amount * sea.wind;
    float shore = shape.amount * sea.shore;
    float3 moved = float3(0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (wind > 0.0)
        moved += HB_WaveWindOffset(restXZ, shape, spacing) * wind;

    UNITY_BRANCH
    if (shore > HB_WAVE_SHORE_LEAST && HB_CALM_BOUND)
        moved += HB_WaveTileOffset(restXZ, spacing) * shore;

    moved.y *= shape.height;
    moved.xz *= HB_WavePush(shape, sea.wind);
    return moved;
}

float2 HB_WaveCapsAt(float2 restXZ, HBLiquidWaveShape shape, HBLiquidSea sea, float wide, float spacing)
{
    float push = HB_WavePush(shape, sea.wind) * shape.amount * sea.wind;
    float2 framed = HB_WaveFramed(restXZ / wide);
    float2 caps = float2(0.0, 0.0);

    UNITY_UNROLL
    for (int past = 0; past < 3; past++)
    {
        float2 ahead = framed + float2(HB_WAVE_TRAIL_STEP * past, 0.0);
        float2 bent = float2(0.0, 0.0);

        UNITY_UNROLL
        for (int band = 0; band < HB_WAVE_BANDS; band++)
        {
            float lod = max(log2(spacing / wide / _HB_WaveTexel[band]), 0.0);

            bent += SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDerivative, sampler_trilinear_repeat,
                                               ahead * _HB_WaveScale[band], band, lod).zw;
        }

        bent *= push;

        float fold = -(bent.x + bent.y) - bent.x * bent.y;
        float weight = past == 0 ? 1.0 : (past == 1 ? HB_WAVE_TRAIL_NEAR : HB_WAVE_TRAIL_FAR);

        caps.x = max(caps.x, saturate((fold - shape.cap) / HB_WAVE_CAP_RANGE) * weight);
        caps.y = past == 0 ? fold : caps.y;
    }

    return caps;
}

float2 HB_WaveWindSlopeAt(float2 restXZ, float2 restDx, float2 restDy, float wide)
{
    float2 framed = HB_WaveFramed(restXZ / wide);
    float2 framedDx = HB_WaveFramed(restDx / wide);
    float2 framedDy = HB_WaveFramed(restDy / wide);
    float2 turned = HB_WaveTurn(framed, 1.0) * HB_WAVE_SECOND_SCALE;
    float2 turnedDx = HB_WaveTurn(framedDx, 1.0) * HB_WAVE_SECOND_SCALE;
    float2 turnedDy = HB_WaveTurn(framedDy, 1.0) * HB_WAVE_SECOND_SCALE;
    float2 bent = float2(0.0, 0.0);

    UNITY_UNROLL
    for (int band = 0; band < HB_WAVE_BANDS; band++)
    {
        float scale = _HB_WaveScale[band];
        float2 first = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_trilinear_repeat, framed * scale,
                                                   band, framedDx * scale, framedDy * scale).xy;
        float2 ahead = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_trilinear_repeat, turned * scale,
                                                   band, turnedDx * scale, turnedDy * scale).xy;

        bent += first + HB_WaveTurn(ahead, -1.0) * HB_WAVE_SECOND_SCALE;
    }

    return HB_WaveWorld(bent) * HB_WAVE_SECOND_SHARE;
}

float2 HB_WaveCaps(float2 restXZ, HBLiquidWaveShape shape, HBLiquidSea sea, float spacing)
{
    float2 caps = float2(0.0, 0.0);
    float total = 0.0;

    UNITY_LOOP
    for (int slot = 0; slot < 4; slot++)
    {
        float share = shape.mix[slot];

        UNITY_BRANCH
        if (share > HB_WAVE_SHARE_LEAST)
        {
            caps += HB_WaveCapsAt(restXZ, shape, sea, HB_WaveSlotLength(slot), spacing) * share;
            total += share;
        }
    }

    return caps * rcp(max(total, HB_WAVE_SHARE_LEAST));
}

float2 HB_WaveWindSlope(float2 restXZ, float2 restDx, float2 restDy, HBLiquidWaveShape shape)
{
    float2 slope = float2(0.0, 0.0);
    float total = 0.0;

    UNITY_LOOP
    for (int slot = 0; slot < 4; slot++)
    {
        float share = shape.mix[slot];

        UNITY_BRANCH
        if (share > HB_WAVE_SHARE_LEAST)
        {
            slope += HB_WaveWindSlopeAt(restXZ, restDx, restDy, HB_WaveSlotLength(slot)) * share;
            total += share;
        }
    }

    return slope * rcp(max(total, HB_WAVE_SHARE_LEAST));
}

float2 HB_WaveTileSlope(float2 restXZ, float2 restDx, float2 restDy)
{
    float2 base;
    float2 blend;

    HB_WaveTiles(restXZ, base, blend);

    float scale = _HB_WaveScale[HB_WAVE_SHORE_BAND];
    float2 slope = float2(0.0, 0.0);

    UNITY_UNROLL
    for (int corner = 0; corner < 4; corner++)
    {
        float2 tile = float2(corner & 1, corner >> 1);
        float2 centre = HB_WaveTileCentre(base, tile);
        float2 heading = HB_WaveTileHeading(base, tile);
        float2 side = float2(-heading.y, heading.x);
        float2 share = lerp(1.0 - blend, blend, tile);
        float2 local = restXZ - centre;

        float2 uv = float2(dot(local, heading), dot(local, side)) * scale + frac(centre * HB_WAVE_SHORE_SCATTER);
        float2 bent = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_trilinear_repeat, uv,
                                                  HB_WAVE_SHORE_BAND,
                                                  float2(dot(restDx, heading), dot(restDx, side)) * scale,
                                                  float2(dot(restDy, heading), dot(restDy, side)) * scale).xy;

        slope += (heading * bent.x + side * bent.y) * (share.x * share.y);
    }

    return slope;
}

float2 HB_WaveSlope(float2 restXZ, float2 restDx, float2 restDy, HBLiquidWaveShape shape, HBLiquidSea sea)
{
    float wind = shape.amount * sea.wind;
    float shore = shape.amount * sea.shore;
    float2 slope = float2(0.0, 0.0);

    UNITY_BRANCH
    if (wind > 0.0)
        slope += HB_WaveWindSlope(restXZ, restDx, restDy, shape) * wind;

    UNITY_BRANCH
    if (shore > HB_WAVE_SHORE_LEAST && HB_CALM_BOUND)
        slope += HB_WaveTileSlope(restXZ, restDx, restDy) * shore;

    return slope * shape.height;
}

#endif
