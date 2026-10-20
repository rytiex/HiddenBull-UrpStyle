#ifndef HIDDENBULL_URPSTYLE_WAVES_INCLUDED
#define HIDDENBULL_URPSTYLE_WAVES_INCLUDED

#include "StyleShore.hlsl"

TEXTURE2D_ARRAY(_HB_WaveDisplacement);
TEXTURE2D_ARRAY(_HB_WaveDerivative);
SAMPLER(sampler_HB_WaveDisplacement);
SAMPLER(sampler_HB_WaveDerivative);
TEXTURE2D_FLOAT(_HB_WaterFloor);
SAMPLER(sampler_HB_WaterFloor);
TEXTURE2D_FLOAT(_HB_WaterFloorOld);
SAMPLER(sampler_HB_WaterFloorOld);
TEXTURE2D(_HB_WaterShoreOld);
SAMPLER(sampler_HB_WaterShoreOld);
TEXTURE2D_FLOAT(_HB_WaterSeabed);
SAMPLER(sampler_HB_WaterSeabed);
TEXTURE2D_FLOAT(_HB_WaterWide);
SAMPLER(sampler_HB_WaterWide);

float4 _HB_WaveFrame;
float4 _HB_WaveScale;
float4 _HB_WaveTexel;
float4 _HB_WaterFloorOldParams;
float4 _HB_WaterShoreOldParams;
float _HB_WaterBlend;
float4 _HB_WaterWideParams;
float _HB_LiquidEconomy;

#define HB_WAVES_BOUND      _HB_WaveFrame.z
#define HB_LIQUID_LOW       (_HB_LiquidEconomy > 1.5)
#define HB_LIQUID_FINE      (_HB_LiquidEconomy < 0.5)
#define HB_WAVE_BAND_COUNT  3
#define HB_WAVE_SECOND_SCALE 0.618
#define HB_WAVE_SECOND_SHARE 0.7
#define HB_WAVE_LITE         1.13
#define HB_WAVE_SHOAL_DEPTH  8.0
#define HB_WAVE_TROUGH       3.0
#define HB_WAVE_SIZE         256.0
#define HB_WAVE_SHORE_LEVEL  0.5
#define HB_WAVE_SHORE_REACH  8.0
#define HB_SHORE_TILE        12.0
#define HB_SHORE_REACH       40.0
#define HB_SHORE_BAND        1
#define HB_SHORE_SCATTER     0.137

float2 HB_WaveTurn(float2 value, float way)
{
    return float2(value.x * 0.8 - value.y * (0.6 * way), value.x * (0.6 * way) + value.y * 0.8);
}

float2 HB_WaveFramePosition(float2 positionXZ)
{
    return float2(dot(positionXZ, _HB_WaveFrame.xy), dot(positionXZ, float2(-_HB_WaveFrame.y, _HB_WaveFrame.x)));
}

float2 HB_WaveWorldVector(float2 framed)
{
    return _HB_WaveFrame.xy * framed.x + float2(-_HB_WaveFrame.y, _HB_WaveFrame.x) * framed.y;
}

float HB_WaveLod(float spacing, int band)
{
    return max(log2(spacing / _HB_WaveTexel[band]), 0.0);
}

float HB_WaveShoalOf(float3 restWS, float trough, float reach, bool old)
{
    float4 mapping = old ? _HB_WaterFloorOldParams : _HB_WaterFloorParams;
    float2 uv = (restWS.xz - mapping.xy) * mapping.z + 0.5;
    float seabed;

    if (old)
        seabed = SAMPLE_TEXTURE2D_LOD(_HB_WaterFloorOld, sampler_HB_WaterFloorOld, uv, 0).r;
    else
        seabed = SAMPLE_TEXTURE2D_LOD(_HB_WaterFloor, sampler_HB_WaterFloor, uv, 0).r;

    bool known = mapping.w > 0.5 && all(abs(uv - 0.5) < 0.49);

    UNITY_BRANCH
    if (!known)
    {
        float2 spread = (restWS.xz - _HB_WaterWideParams.xy) * _HB_WaterWideParams.z + 0.5;

        known = _HB_WaterWideParams.w > 0.5 && all(abs(spread - 0.5) < 0.49);
        seabed = SAMPLE_TEXTURE2D_LOD(_HB_WaterWide, sampler_HB_WaterWide, spread, 0).r;
    }

    float deep = max(restWS.y - seabed, 0.0);
    float open = saturate(deep / reach);

    return known ? min(open * (2.0 - open), deep / max(trough, 1e-3)) : 1.0;
}

float HB_WaterDepthAt(float3 positionWS, out bool known)
{
    float2 uv = (positionWS.xz - _HB_WaterFloorParams.xy) * _HB_WaterFloorParams.z + 0.5;
    float seabed = SAMPLE_TEXTURE2D_LOD(_HB_WaterSeabed, sampler_HB_WaterSeabed, uv, 0).r;

    known = _HB_WaterFloorParams.w > 0.5 && all(abs(uv - 0.5) < 0.49);

    UNITY_BRANCH
    if (!known)
    {
        float2 spread = (positionWS.xz - _HB_WaterWideParams.xy) * _HB_WaterWideParams.z + 0.5;

        known = _HB_WaterWideParams.w > 0.5 && all(abs(spread - 0.5) < 0.49);
        seabed = SAMPLE_TEXTURE2D_LOD(_HB_WaterWide, sampler_HB_WaterWide, spread, 0).r;
    }

    known = known && abs(positionWS.y - HB_ShoreLevel(positionWS.xz)) < HB_WAVE_SHORE_REACH;

    return max(positionWS.y - seabed, 0.0);
}

float HB_WaveShoal(float3 restWS, float trough, float reach)
{
    float calm = HB_WaveShoalOf(restWS, trough, reach, false);

    UNITY_BRANCH
    if (_HB_WaterBlend < 1.0)
        calm = lerp(HB_WaveShoalOf(restWS, trough, reach, true), calm, _HB_WaterBlend);

    return calm;
}

float4 HB_ShoreFieldOf(float2 positionXZ, float level, bool old)
{
    float4 mapping = old ? _HB_WaterFloorOldParams : _HB_WaterFloorParams;
    float4 shore = old ? _HB_WaterShoreOldParams : _HB_WaterShoreParams;
    float2 uv = (positionXZ - mapping.xy) * mapping.z + 0.5;
    float4 field;

    if (old)
        field = SAMPLE_TEXTURE2D_LOD(_HB_WaterShoreOld, sampler_HB_WaterShoreOld, uv, 0);
    else
        field = SAMPLE_TEXTURE2D_LOD(_HB_WaterShore, sampler_HB_WaterShore, uv, 0);

    bool known = mapping.w > 0.5 && shore.y > 0.5 && all(abs(uv - 0.5) < 0.49)
              && abs(level - shore.x - HB_ShoreRise(positionXZ)) < HB_WAVE_SHORE_LEVEL;

    return float4(field.xyz, known ? 1.0 : 0.0);
}

float HB_ShoreTurnOf(float3 restWS, bool old)
{
    float4 field = HB_ShoreFieldOf(restWS.xz, restWS.y, old);
    float close = saturate(1.0 - field.x / HB_SHORE_REACH);

    return field.w > 0.5 && field.x > 0.0 ? saturate(length(field.yz)) * close * close * (3.0 - 2.0 * close) : 0.0;
}

float HB_ShoreTurn(float3 restWS)
{
    float turn = HB_ShoreTurnOf(restWS, false);

    UNITY_BRANCH
    if (_HB_WaterBlend < 1.0)
        turn = lerp(HB_ShoreTurnOf(restWS, true), turn, _HB_WaterBlend);

    return turn;
}

float2 HB_ShoreHeadingOf(float2 centre, bool old)
{
    float level = (old ? _HB_WaterShoreOldParams.x : _HB_WaterShoreParams.x) + HB_ShoreRise(centre);
    float4 field = HB_ShoreFieldOf(centre, level, old);
    float2 toward = field.yz * (field.x < 0.0 ? -1.0 : 1.0);
    float sure = dot(toward, toward);

    float2 stream = HB_LevelRead(_HB_LiquidLevelMap, _HB_LiquidArea, centre).ba;
    float rush = dot(stream, stream);

    if (rush > 1e-4)
        return stream * rsqrt(rush);

    return field.w > 0.5 && sure > 0.25 ? toward * rsqrt(sure) : _HB_WaveFrame.xy;
}

float2 HB_ShoreHeading(float2 centre)
{
    float2 heading = HB_ShoreHeadingOf(centre, false);

    UNITY_BRANCH
    if (_HB_WaterBlend < 1.0)
    {
        float2 mixed = lerp(HB_ShoreHeadingOf(centre, true), heading, _HB_WaterBlend);
        float sure = dot(mixed, mixed);

        heading = sure > 1e-4 ? mixed * rsqrt(sure) : heading;
    }

    return heading;
}

void HB_ShoreTiles(float2 positionXZ, out float2 base, out float2 blend)
{
    float2 grid = positionXZ / HB_SHORE_TILE - 0.5;

    base = floor(grid);
    blend = grid - base;
    blend = blend * blend * (3.0 - 2.0 * blend);
}

float3 HB_SampleShoreOffsetAt(float2 positionXZ, float spacing)
{
    float2 base;
    float2 blend;

    HB_ShoreTiles(positionXZ, base, blend);

    float lod = HB_WaveLod(spacing, HB_SHORE_BAND);
    float3 total = float3(0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int corner = 0; corner < 4; corner++)
    {
        float2 tile = float2(corner & 1, corner >> 1);
        float2 centre = (base + tile + 0.5) * HB_SHORE_TILE;
        float2 local = positionXZ - centre;
        float2 heading = HB_ShoreHeading(centre);
        float2 side = float2(-heading.y, heading.x);
        float2 share = lerp(1.0 - blend, blend, tile);

        float2 uv = float2(dot(local, heading), dot(local, side)) * _HB_WaveScale[HB_SHORE_BAND]
                  + frac(centre * HB_SHORE_SCATTER);
        float3 moved = SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDisplacement, sampler_HB_WaveDisplacement, uv,
                                                  HB_SHORE_BAND, lod).xyz;
        float2 pushed = heading * moved.x + side * moved.z;

        total += float3(pushed.x, moved.y, pushed.y) * (share.x * share.y);
    }

    return HB_WAVES_BOUND < 0.5 ? float3(0.0, 0.0, 0.0) : total;
}

float HB_SampleShoreSurface(float2 positionXZ, float2 positionDx, float2 positionDy, out float2 slope)
{
    float2 base;
    float2 blend;

    HB_ShoreTiles(positionXZ, base, blend);

    float scale = _HB_WaveScale[HB_SHORE_BAND];
    float trace = 0.0;

    slope = float2(0.0, 0.0);

    UNITY_UNROLL
    for (int corner = 0; corner < 4; corner++)
    {
        float2 tile = float2(corner & 1, corner >> 1);
        float2 centre = (base + tile + 0.5) * HB_SHORE_TILE;
        float2 local = positionXZ - centre;
        float2 heading = HB_ShoreHeading(centre);
        float2 side = float2(-heading.y, heading.x);
        float2 share = lerp(1.0 - blend, blend, tile);

        float4 bent = SAMPLE_TEXTURE2D_ARRAY_GRAD(
            _HB_WaveDerivative, sampler_HB_WaveDerivative,
            float2(dot(local, heading), dot(local, side)) * scale + frac(centre * HB_SHORE_SCATTER), HB_SHORE_BAND,
            float2(dot(positionDx, heading), dot(positionDx, side)) * scale,
            float2(dot(positionDy, heading), dot(positionDy, side)) * scale);

        slope += (heading * bent.x + side * bent.y) * (share.x * share.y);
        trace += (bent.z + bent.w) * (share.x * share.y);
    }

    if (HB_WAVES_BOUND < 0.5)
        slope = float2(0.0, 0.0);

    return HB_WAVES_BOUND < 0.5 ? 0.0 : -trace;
}

float3 HB_WavePlain(float2 uv, int band, float lod)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDisplacement, sampler_HB_WaveDisplacement, uv, band, lod).xyz;
}

float3 HB_WaveTap(float2 uv, int band, float lod)
{
    float3 plain = HB_WavePlain(uv, band, lod);

    UNITY_BRANCH
    if (lod < 1.0 && !HB_LIQUID_LOW)
    {
        float2 at = uv * HB_WAVE_SIZE - 0.5;
        float2 corner = floor(at);
        float2 part = at - corner;
        float2 square = part * part;
        float2 cube = square * part;

        float2 first = (1.0 - 3.0 * part + 3.0 * square - cube) / 6.0;
        float2 second = (4.0 - 6.0 * square + 3.0 * cube) / 6.0;
        float2 third = (1.0 + 3.0 * part + 3.0 * square - 3.0 * cube) / 6.0;
        float2 fourth = cube / 6.0;

        float2 low = first + second;
        float2 high = third + fourth;
        float2 near0 = (corner + second / low - 0.5) / HB_WAVE_SIZE;
        float2 near1 = (corner + fourth / high + 1.5) / HB_WAVE_SIZE;

        float3 eased = low.y * (low.x * HB_WavePlain(float2(near0.x, near0.y), band, 0.0)
                              + high.x * HB_WavePlain(float2(near1.x, near0.y), band, 0.0))
                     + high.y * (low.x * HB_WavePlain(float2(near0.x, near1.y), band, 0.0)
                               + high.x * HB_WavePlain(float2(near1.x, near1.y), band, 0.0));

        plain = lerp(eased, plain, saturate(lod));
    }

    return plain;
}

float3 HB_SampleWaveOffsetAt(float2 positionXZ, float3 bands, float spacing, float stillness)
{
    float2 framed = HB_WaveFramePosition(positionXZ);
    float3 moved = float3(0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int band = 0; band < HB_WAVE_BAND_COUNT; band++)
    {
        float2 uv = framed * _HB_WaveScale[band];
        float lod = HB_WaveLod(spacing, band);
        float3 there = HB_WaveTap(uv, band, lod);

        UNITY_BRANCH
        if (stillness > 0.0)
        {
            float3 back = HB_WaveTap(-uv, band, lod);

            there = lerp(there, (there + float3(-back.x, back.y, -back.z)) * 0.5, stillness);
        }

        moved += there * bands[band];
    }

    if (HB_WAVES_BOUND < 0.5)
        return float3(0.0, 0.0, 0.0);

    float2 pushed = HB_WaveWorldVector(moved.xz);

    return float3(pushed.x, moved.y, pushed.y);
}

float HB_WaveFold(float4 bent)
{
    return -(bent.z + bent.w) - bent.z * bent.w;
}

float HB_SampleWaveFoldLite(float2 positionXZ, float3 bands, float spacing)
{
    float2 framed = HB_WaveFramePosition(positionXZ);
    float4 bent = float4(0.0, 0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int band = 0; band < HB_WAVE_BAND_COUNT; band++)
    {
        bent += SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_WaveDerivative, sampler_HB_WaveDerivative,
                                           framed * _HB_WaveScale[band], band, HB_WaveLod(spacing, band))
              * bands[band];
    }

    return HB_WAVES_BOUND < 0.5 ? 0.0 : HB_WaveFold(bent * HB_WAVE_LITE);
}

float HB_SampleWaveSurface(float2 positionXZ, float2 positionDx, float2 positionDy, float3 bands, float stillness,
                            out float2 slope)
{
    float2 framed = HB_WaveFramePosition(positionXZ);
    float2 framedDx = HB_WaveFramePosition(positionDx);
    float2 framedDy = HB_WaveFramePosition(positionDy);
    float2 turned = HB_WaveTurn(framed, 1.0) * HB_WAVE_SECOND_SCALE;
    float2 turnedDx = HB_WaveTurn(framedDx, 1.0) * HB_WAVE_SECOND_SCALE;
    float2 turnedDy = HB_WaveTurn(framedDy, 1.0) * HB_WAVE_SECOND_SCALE;
    float4 bent = float4(0.0, 0.0, 0.0, 0.0);

    UNITY_UNROLL
    for (int band = 0; band < HB_WAVE_BAND_COUNT; band++)
    {
        float scale = _HB_WaveScale[band];
        float4 first = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_HB_WaveDerivative, framed * scale,
                                                   band, framedDx * scale, framedDy * scale);
        float4 second = float4(0.0, 0.0, 0.0, 0.0);

        UNITY_BRANCH
        if (stillness < 1.0)
        {
            float4 ahead = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_HB_WaveDerivative,
                                                       turned * scale, band, turnedDx * scale, turnedDy * scale);

            ahead.xy = HB_WaveTurn(ahead.xy, -1.0);
            second += ahead * (1.0 - stillness);
        }

        UNITY_BRANCH
        if (stillness > 0.0)
        {
            float4 back = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_HB_WaveDerivative,
                                                      framed * -scale, band, framedDx * scale, framedDy * scale);
            float4 behind = SAMPLE_TEXTURE2D_ARRAY_GRAD(_HB_WaveDerivative, sampler_HB_WaveDerivative,
                                                        turned * -scale, band, turnedDx * scale, turnedDy * scale);

            first = lerp(first, (first + float4(-back.xy, back.zw)) * 0.5, stillness);
            behind.xy = -HB_WaveTurn(behind.xy, -1.0);
            second += behind * stillness;
        }

        bent += (first + second * HB_WAVE_SECOND_SCALE) * (HB_WAVE_SECOND_SHARE * bands[band]);
    }

    bool bound = HB_WAVES_BOUND > 0.5;

    slope = bound ? HB_WaveWorldVector(bent.xy) : float2(0.0, 0.0);

    return bound ? HB_WaveFold(bent) : 0.0;
}

#endif
