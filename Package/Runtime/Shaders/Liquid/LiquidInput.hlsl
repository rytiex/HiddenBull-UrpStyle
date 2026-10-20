#ifndef HIDDENBULL_URPSTYLE_LIQUID_INPUT_INCLUDED
#define HIDDENBULL_URPSTYLE_LIQUID_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _WaterGrid;
    float4 _WaterArea;
    float4 _WaterBox;
    float _WaterLayers;
    float _WaterStreams;
    half4 _DepthColor0;
    half4 _DepthColor1;
    half4 _DepthColor2;
    half4 _DepthColor3;
    half4 _DepthColor4;
    half4 _DepthColor5;
    float4 _DepthTimes0;
    float4 _DepthTimes1;
    float4 _DepthAlpha0;
    float4 _DepthAlpha1;
    float4 _DepthAlphaTimes0;
    float4 _DepthAlphaTimes1;
    half4 _FoamColor;
    half4 _CrustColor;
    half4 _CrestColor;
    half _Tiling;
    half _BumpScale;
    half _Smoothness;
    half _Refraction;
    half _RefractionBlur;
    half _FoamDistance;
    half _FoamAmount;
    half _ShoreFoam;
    half _PatternScale;
    half _CrustAmount;
    half _Pulse;
    half _BubbleAmount;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _ReceiveWeather;
    half _Glow;
    half _Waves;
    half _WindWaves;
    half _WaveHeight;
    half _WaveSteepness;
    half _WaveLength;
    half _Caustics;
    half _CausticScale;
    half _CrestSoftness;
    half _UnderShafts;
    half _UnderBubbles;
    half _UnderDistortion;
CBUFFER_END

float _WaterUnbatched;

#include "../Library/StyleBrush.hlsl"
#include "../Library/StyleWaves.hlsl"
#include "../Library/StyleShore.hlsl"

#define HB_LIQUID_SECOND_SCALE  1.37
#define HB_LIQUID_SECOND_SPEED  0.71
#define HB_LIQUID_SECOND_TURN   0.62
#define HB_FOAM_EDGE_SOFTNESS   0.4h
#define HB_FOAM_FEATHER         0.06
#define HB_FOAM_VEIL            0.6h
#define HB_FOAM_FADE_FROM       60.0
#define HB_FOAM_FADE_RANGE      140.0
#define HB_FOAM_GLOW            0.45h
#define HB_FOAM_COVER_MOST      0.85h
#define HB_CAP_CALM             0.6
#define HB_CAP_STORM            0.22
#define HB_TRAIL_FROM           0.05
#define HB_TRAIL_STEP           1.6
#define HB_TRAIL_PIXEL          0.004
#define HB_TRAIL_NEAR           0.7
#define HB_TRAIL_FAR            0.4
#define HB_FOAM_SECOND_SPEED    0.6
#define HB_FOAM_SECOND_SCALE    0.57
#define HB_FOAM_SECOND_TURN     1.9
#define HB_FOAM_SECOND_SHARE    0.4h
#define HB_SHORE_FOAM_SPAN      4.0
#define HB_SHORE_FOAM_PACE      0.12
#define HB_SHORE_FOAM_WOBBLE    0.5
#define HB_SHORE_FOAM_FRONT     0.12
#define HB_SHORE_FOAM_TAIL      0.55
#define HB_SHORE_FOAM_BASE      0.35
#define HB_SHORE_FOAM_WAVES     4.0
#define HB_CRUST_SOFTNESS       0.18h
#define HB_BUBBLE_CELLS         6.0
#define HB_BUBBLE_RADIUS        0.36
#define HB_BUBBLE_POP           0.82
#define HB_BUBBLE_RATE          0.25
#define HB_BUBBLE_LENS          0.6
#define HB_LAVA_CHURN           0.12
#define HB_GLINT_SCALE          6.0
#define HB_LIQUID_FLOW_SPEED    0.025
#define HB_LIQUID_STILL_SPREAD  5.9
#define HB_WAVE_CAP_FROM        0.35h
#define HB_WAVE_CAP_RANGE       0.4h
#define HB_WAVE_POOL            float3(0.0, 0.2, 1.0)
#define HB_SHORE_SHARE          0.7
#define HB_SHORE_GAIN           1.6
#define HB_CAUSTIC_FOCUS        2.0
#define HB_CAUSTIC_SHARP        0.25
#define HB_CAUSTIC_GAIN         1.2h
#define HB_CAUSTIC_SHORE        0.3
#define HB_CAUSTIC_DEEP         3.0
#define HB_CAUSTIC_BLUR         0.35
#define HB_CAUSTIC_BLUR_MOST    3.0
#define HB_CAUSTIC_SECOND       0.731
#define HB_CAUSTIC_SHIFT        0.37
#define HB_CAUSTIC_SIZE         256.0
#define HB_LIQUID_CREST_LIGHT   1.5h
#define HB_LIQUID_CREST_SKY     0.5h
#define HB_LIQUID_CREST_EARLY   0.5h
#define HB_LIQUID_CREST_GRAIN   0.3h
#define HB_LIQUID_CREST_BASE    0.5h
#define HB_LIQUID_CREST_FROM    0.175h
#define HB_LIQUID_CREST_HARD    0.02h
#define HB_LIQUID_CREST_SOFT    0.5h
#define HB_LIQUID_GRID_HALF     32.0
#define HB_LIQUID_SNAP          2.0
#define HB_LIQUID_RISE          16.0
#define HB_LIQUID_COARSEST      6.0
#define HB_LIQUID_EDGE          32.0
#define HB_LIQUID_PAST          2.5
#define HB_LIQUID_BOX_EDGE      0.5
#define HB_LIQUID_SPAN          16.0
#define HB_LIQUID_GRADIENT_DEPTH 4.0
#define HB_LIQUID_TINT_DEPTH    2.0
#define HB_LIQUID_FAR_FROM      150.0
#define HB_LIQUID_FAR_RANGE     450.0
#define HB_LIQUID_MORPH         0.7
#define HB_LIQUID_SLOPE_SPAN    1.0
#define HB_LIQUID_BLOCKS        64.0
#define HB_LIQUID_LEVEL         0.01
#define HB_LIQUID_NEAR          0.05
#define HB_WAVE_LENGTH_MIN      0.05
#define HB_WAVE_FOLD_LIMIT      1.0
#define HB_WAVE_DRIFT           0.25
#define HB_WAVE_DRIFT_FROM      0.4
#define HB_STREAM_PERIOD        4.0
#define HB_STREAM_WHITE         6.0
#define HB_STREAM_TURN          1.0
#define HB_STREAM_STREAK        0.12
#define HB_STREAM_SHEAR         0.6
#define HB_STREAM_BANK          1.5
#define HB_STREAM_SHEET         0.35h
#define HB_STREAM_LEAST         1e-6
#define HB_LIQUID_PATTERN_TURN  0.3

TEXTURE2D(_WaterLevelMap);
TEXTURE2D(_WaterBlockMap);
TEXTURE2D(_HB_LiquidCutMask);
TEXTURE2D(_HB_FoamPattern);
SAMPLER(sampler_HB_FoamPattern);
TEXTURE2D(_HB_Caustics);
SAMPLER(sampler_HB_Caustics);

float4 _HB_CausticParams;
float _HB_LiquidSeat;

static float s_Seat = 0.0;
static float s_Fade = 1.0;
static float2 s_Stream = float2(0.0, 0.0);
static float s_Shear = 0.0;
static float3 s_Normal = float3(0.0, 1.0, 0.0);
static float2 s_UvDx = float2(0.0, 0.0);
static float2 s_UvDy = float2(0.0, 0.0);
static half s_Grain = 0.5h;

float HB_LiquidCutValue(float2 screenUV)
{
    int2 pixel = clamp((int2)floor(screenUV * _ScaledScreenParams.xy), 0, (int2)_ScaledScreenParams.xy - 1);

    return LOAD_TEXTURE2D(_HB_LiquidCutMask, pixel)[(int)_WaterGrid.z];
}

void HB_LiquidCut(float4 positionCS, float3 positionWS)
{
#if defined(HB_LIQUID_SKY)
    if (_WaterGrid.z >= 0.0 && HB_LiquidDryPoint(positionWS, _WaterGrid.z))
        clip(-1.0);
#elif !defined(HB_LIQUID_CUT_MASK)
    UNITY_BRANCH
    if (HB_CUT_ANY && _WaterGrid.z >= 0.0)
    {
        float value = HB_LiquidCutValue(GetNormalizedScreenSpaceUV(positionCS));

        if (HB_LiquidCutHidden(value, _HB_LiquidCutInclude[(int)_WaterGrid.z]))
            clip(-1.0);
    }
#endif
}

float HB_LiquidRise(float2 positionXZ)
{
    return HB_LevelRead(_WaterLevelMap, _WaterArea, positionXZ).r;
}

bool HB_LiquidStill()
{
    return _WaterGrid.w > 1.5;
}

bool HB_LiquidTiled()
{
    return _WaterGrid.w > 0.5;
}

float HB_LiquidSheet(float2 positionXZ)
{
    float2 at = ((positionXZ - _WaterArea.xy) * _WaterArea.zw + 0.5) * HB_LIQUID_BLOCKS;

    if (_WaterArea.z <= 0.0 || any(at < 0.0) || any(at >= HB_LIQUID_BLOCKS))
        return 0.0;

    return LOAD_TEXTURE2D(_WaterBlockMap, (int2)at).r;
}

bool HB_LiquidInside(float2 positionXZ)
{
    float2 at = (positionXZ - _WaterArea.xy) * _WaterArea.zw + 0.5;

    return _WaterArea.z > 0.0 && all(at > 0.0) && all(at < 1.0);
}

bool HB_LiquidSheeted(float2 positionXZ)
{
    return HB_LiquidSheet(positionXZ) > 0.75;
}

float HB_LiquidSheets(float2 positionXZ, float reach)
{
    float here = HB_LiquidSheet(positionXZ);

    if (here < 0.25)
        return 0.0;

    return float(here > 0.75) + float(HB_LiquidSheeted(positionXZ + float2(reach, 0.0)))
         + float(HB_LiquidSheeted(positionXZ - float2(reach, 0.0)))
         + float(HB_LiquidSheeted(positionXZ + float2(0.0, reach)))
         + float(HB_LiquidSheeted(positionXZ - float2(0.0, reach)));
}

float HB_LiquidKept(float2 positionXZ)
{
    return 1.0 - HB_LevelRead(_WaterLevelMap, _WaterArea, positionXZ).g;
}

float HB_LiquidTime()
{
    return _Time.y;
}

float HB_LiquidStreamUV(float2 uv, out float2 first, out float2 second)
{
    float phase = frac(HB_LiquidTime() / HB_STREAM_PERIOD);
    float2 run = s_Stream * (float(_Tiling) * HB_STREAM_PERIOD);

    first = uv - run * (phase - 0.5);
    second = uv - run * (frac(phase + 0.5) - 0.5);

    return abs(phase * 2.0 - 1.0);
}

struct HiddenBullStream
{
    float2 first;
    float2 second;
    float2 firstDx;
    float2 firstDy;
    float2 secondDx;
    float2 secondDy;
    float2 baseDx;
    float2 baseDy;
    half blend;
    bool moving;
};

bool HB_LiquidStreamed()
{
    return dot(s_Stream, s_Stream) > HB_STREAM_LEAST;
}

void HB_LiquidFootprint(float2 uvDx, float2 uvDy)
{
    s_UvDx = uvDx;
    s_UvDy = uvDy;
}

HiddenBullStream HB_LiquidStreamOf(float2 uv)
{
    HiddenBullStream stream;

    stream.blend = half(HB_LiquidStreamUV(uv, stream.first, stream.second));
    stream.firstDx = ddx(stream.first);
    stream.firstDy = ddy(stream.first);
    stream.secondDx = ddx(stream.second);
    stream.secondDy = ddy(stream.second);
    stream.baseDx = ddx(uv);
    stream.baseDy = ddy(uv);
    stream.moving = HB_LiquidStreamed();

    return stream;
}

#include "LiquidLayers.hlsl"

float2 HB_LiquidFlow()
{
    return HB_WAVES_BOUND > 0.5 ? _HB_WaveFrame.xy : float2(0.0, 1.0);
}

float HB_LiquidCoarse(float level)
{
    float above = abs(GetCameraPositionWS().y - level);

    return clamp(log2(max(above, 1e-3) / HB_LIQUID_RISE) + 1.0, 0.0, HB_LIQUID_COARSEST);
}

float HB_LiquidCell(float level)
{
    return _WaterGrid.x * float(1u << (uint)floor(HB_LiquidCoarse(level)));
}

float HB_LiquidLift(float level)
{
    return saturate(frac(HB_LiquidCoarse(level)) * 2.0 - 1.0);
}

float2 HB_LiquidOrigin(float cell)
{
    float snap = cell * HB_LIQUID_SNAP;

    return floor(GetCameraPositionWS().xz / snap) * snap;
}

float HB_LiquidMorph(float cell, float ring, float away, float lift)
{
    float fine = cell * exp2(ring);
    float last = (HB_LIQUID_GRID_HALF - HB_LIQUID_SNAP - 1.0) * fine;
    float begun = last * HB_LIQUID_MORPH;
    float morph = saturate((away - begun) / (last - begun));

    return ring < 0.5 ? max(morph, lift) : morph;
}

float HB_LiquidSpacing(float cell, float lift, float2 positionXZ)
{
    float2 away = abs(positionXZ - GetCameraPositionWS().xz);

    return max(cell * exp2(lift), max(away.x, away.y) / HB_LIQUID_SPAN);
}

float2 HB_LiquidAnchor()
{
    return HB_LiquidOrigin(HB_LiquidCell(s_Seat)) * _Tiling;
}

float2 HB_LiquidUV(float2 local)
{
    return local * _Tiling;
}

float2 HB_LiquidTurn(float2 value, float turn)
{
    float s, c;
    sincos(turn, s, c);

    return float2(c * value.x - s * value.y, s * value.x + c * value.y);
}

float2 HB_LiquidLayer(float2 uv, float2 flow, float speed, float scale, float turn)
{
    float2 turned = HB_LiquidTurn(uv, turn) * scale + frac(HB_LiquidTurn(HB_LiquidAnchor(), turn) * scale);

    if (HB_LiquidStill())
        flow = HB_LiquidTurn(flow, turn * HB_LIQUID_STILL_SPREAD);

    return turned + frac(flow * speed * HB_LiquidTime());
}

float2 HB_LiquidLayerSlope(float2 slope, float scale, float turn)
{
    return HB_LiquidTurn(slope, turn) * scale;
}

float HB_LiquidWaveAmount()
{
    bool bound = HB_WAVES_BOUND > 0.5;
    float own = _WindWaves > 0.5h && bound ? _HB_WaveFrame.w : float(_Waves);
    float first = _OtherWave[0].y > 0.5 && bound ? _HB_WaveFrame.w : _OtherWave[0].x;
    float second = _OtherWave[1].y > 0.5 && bound ? _HB_WaveFrame.w : _OtherWave[1].x;
    float third = _OtherWave[2].y > 0.5 && bound ? _HB_WaveFrame.w : _OtherWave[2].x;

    return HB_Mix(own, first, second, third);
}

float3 HB_LiquidBands()
{
    return (HB_LiquidStill() ? HB_WAVE_POOL : float3(1.0, 1.0, 1.0)) * HB_LiquidWaveAmount();
}

float HB_LiquidOpen(float2 shore)
{
    return shore.x * (1.0 - shore.y * HB_SHORE_SHARE);
}

float HB_LiquidShoreward(float2 shore)
{
    return shore.x * shore.y * HB_LiquidWaveAmount() * HB_SHORE_GAIN;
}

float HB_LiquidPush(float open)
{
    float pressed = HB_LiquidWaveAmount() * open;

    return min(HB_LiquidHeight() * HB_LiquidSteep(), HB_WAVE_FOLD_LIMIT / max(pressed, 1e-3));
}

float HB_LiquidCalm(float3 rest)
{
    float size = HB_LiquidHeight() * HB_LiquidLength();

    return HB_WaveShoal(rest, HB_WAVE_TROUGH * HB_LiquidWaveAmount() * size,
                        HB_WAVE_SHOAL_DEPTH * max(size, 1.0));
}

float3 HB_LiquidMoved(float3 rest, float spacing, float2 shore)
{
    float wide = HB_LiquidLength();
    float open = HB_LiquidOpen(shore);

    float3 moved = HB_SampleWaveOffsetAt(rest.xz / wide, HB_LiquidBands() * open, spacing / wide,
                                         HB_LiquidStill() ? 1.0 : 0.0) * wide;

    UNITY_BRANCH
    if (shore.y > 0.0)
        moved += HB_SampleShoreOffsetAt(rest.xz, spacing) * HB_LiquidShoreward(shore);

    moved.y *= HB_LiquidHeight();
    moved.xz *= HB_LiquidPush(open);

    return moved;
}

float3 HB_LiquidDisplaceAs(float3 rest, float spacing, out float2 shore)
{
    float calm = HB_LiquidCalm(rest);
    float turn = HB_LiquidStill() ? 0.0 : HB_ShoreTurn(rest);

    UNITY_BRANCH
    if (_WaterStreams > 0.5)
        turn = max(turn, saturate(length(s_Stream) / HB_STREAM_TURN));

    shore = float2(calm, turn);

    float3 moved = HB_LiquidMoved(rest, spacing, shore);

    float drift = length(moved.xz);

    UNITY_BRANCH
    if (calm > 0.0 && drift > HB_WAVE_DRIFT * HB_WAVE_DRIFT_FROM)
    {
        float landed = min(calm, HB_LiquidCalm(rest + float3(moved.x, 0.0, moved.z)));

        float pull = saturate((drift / HB_WAVE_DRIFT - HB_WAVE_DRIFT_FROM) / (1.0 - HB_WAVE_DRIFT_FROM));

        UNITY_BRANCH
        if (landed < calm)
        {
            shore.x = lerp(calm, landed, pull);
            moved = HB_LiquidMoved(rest, spacing, shore);
        }
    }

    return moved;
}

float3 HB_LiquidDisplace(float3 rest, float spacing, out float2 shore)
{
    float3 keep = s_Mix;
    float own = HB_LiquidOwnShare(keep);
    float3 moved = float3(0.0, 0.0, 0.0);

    shore = float2(0.0, 0.0);

    UNITY_LOOP
    for (int source = 0; source < 4; source++)
    {
        float share = HB_LiquidSourceShare(source, own, keep);

        if (share <= 0.0)
            continue;

        float2 other;

        s_Mix = HB_LiquidSourceMix(source);
        moved += HB_LiquidDisplaceAs(rest, spacing, other) * share;
        shore += other * share;
    }

    s_Mix = keep;

    return moved;
}

float HB_LiquidBoxRim(float2 inside)
{
    float2 inward = _WaterBox.xy - abs(inside);
    float edge = min(HB_LIQUID_EDGE, min(_WaterBox.x, _WaterBox.y) * HB_LIQUID_BOX_EDGE);

    return saturate(min(inward.x, inward.y) / edge);
}

float HB_LiquidRim(float2 flat, float2 centre)
{
    float2 inward = _WaterGrid.y - abs(flat - centre);

    return saturate(min(inward.x, inward.y) / HB_LIQUID_EDGE);
}

half HB_LiquidWavesAs(float2 restXZ, float2 restDx, float2 restDy, float2 shore, out float2 slope)
{
    float wide = HB_LiquidLength();
    float open = HB_LiquidOpen(shore);
    float fold = 0.0;

    slope = float2(0.0, 0.0);

    float amount = HB_LiquidWaveAmount();

    UNITY_BRANCH
    if (open > 0.0 && amount > 0.0)
        fold = HB_SampleWaveSurface(restXZ / wide, restDx / wide, restDy / wide, HB_LiquidBands() * open,
                                    HB_LiquidStill() ? 1.0 : 0.0, slope);

    UNITY_BRANCH
    if (shore.y > 0.01 && shore.x > 0.0 && amount > 0.0)
    {
        float2 turned;
        float folded = HB_SampleShoreSurface(restXZ, restDx, restDy, turned);
        float gain = HB_LiquidShoreward(shore);

        slope += turned * gain;
        fold += folded * gain;
    }

    slope *= HB_LiquidHeight();
    fold *= HB_LiquidPush(open);

    return half(fold);
}

half HB_LiquidWaves(float2 restXZ, float2 local, float2 shore, out float2 slope)
{
    float3 keep = s_Mix;
    float own = HB_LiquidOwnShare(keep);
    float2 restDx = ddx(local);
    float2 restDy = ddy(local);
    half fold = 0.0h;

    slope = float2(0.0, 0.0);

    UNITY_LOOP
    for (int source = 0; source < 4; source++)
    {
        float share = HB_LiquidSourceShare(source, own, keep);

        if (share <= 0.0)
            continue;

        float2 other;

        s_Mix = HB_LiquidSourceMix(source);
        fold += HB_LiquidWavesAs(restXZ, restDx, restDy, shore, other) * half(share);
        slope += other * share;
    }

    s_Mix = keep;

    return fold;
}

void HB_LiquidGradientOf(float2 path, float4 colours[6], float4 times[2], float4 covers[2], float4 fades[2],
                         out half4 first, out half4 second)
{
    float2 at = 1.0 - exp(-max(path, 0.0) / HB_LIQUID_GRADIENT_DEPTH);
    float edges[6] = { times[0].x, times[0].y, times[0].z, times[0].w, times[1].x, times[1].y };
    float fadeEdges[6] = { fades[0].x, fades[0].y, fades[0].z, fades[0].w, fades[1].x, fades[1].y };
    float coverValues[6] = { covers[0].x, covers[0].y, covers[0].z, covers[0].w, covers[1].x, covers[1].y };

    first = half4(half3(colours[0].rgb), half(coverValues[0]));
    second = first;

    UNITY_UNROLL
    for (int stop = 1; stop < 6; stop++)
    {
        half2 tone = half2(saturate((at - edges[stop - 1]) / max(edges[stop] - edges[stop - 1], 1e-4)));
        half2 fade = half2(saturate((at - fadeEdges[stop - 1]) / max(fadeEdges[stop] - fadeEdges[stop - 1], 1e-4)));

        first.rgb = lerp(first.rgb, half3(colours[stop].rgb), tone.x);
        second.rgb = lerp(second.rgb, half3(colours[stop].rgb), tone.y);
        first.a = lerp(first.a, half(coverValues[stop]), fade.x);
        second.a = lerp(second.a, half(coverValues[stop]), fade.y);
    }

    first.a = saturate(first.a);
    second.a = saturate(second.a);
}

half4 HB_LiquidGradientPair(float path, float deep, out half4 deepWater)
{
    float own = HB_LiquidOwnShare(s_Mix);
    half4 water = half4(0.0h, 0.0h, 0.0h, 0.0h);

    deepWater = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_LOOP
    for (int source = 0; source < 4; source++)
    {
        float share = HB_LiquidSourceShare(source, own, s_Mix);

        if (share <= 0.0)
            continue;

        int slot = max(source - 1, 0);
        bool mine = source == 0;

        float4 colours[6] =
        {
            mine ? _DepthColor0 : _OtherDepthColor0[slot], mine ? _DepthColor1 : _OtherDepthColor1[slot],
            mine ? _DepthColor2 : _OtherDepthColor2[slot], mine ? _DepthColor3 : _OtherDepthColor3[slot],
            mine ? _DepthColor4 : _OtherDepthColor4[slot], mine ? _DepthColor5 : _OtherDepthColor5[slot]
        };
        float4 times[2] = { mine ? _DepthTimes0 : _OtherDepthTimes0[slot],
                            mine ? _DepthTimes1 : _OtherDepthTimes1[slot] };
        float4 covers[2] = { mine ? _DepthAlpha0 : _OtherDepthAlpha0[slot],
                             mine ? _DepthAlpha1 : _OtherDepthAlpha1[slot] };
        float4 fades[2] = { mine ? _DepthAlphaTimes0 : _OtherDepthAlphaTimes0[slot],
                            mine ? _DepthAlphaTimes1 : _OtherDepthAlphaTimes1[slot] };

        half4 first;
        half4 second;

        HB_LiquidGradientOf(float2(path, deep), colours, times, covers, fades, first, second);

        water += first * half(share);
        deepWater += second * half(share);
    }

    return water;
}

half4 HB_LiquidGradient(float path)
{
    half4 unused;

    return HB_LiquidGradientPair(path, path, unused);
}

float HB_LiquidReach(float path, float3 positionWS)
{
    float away = distance(positionWS, GetCameraPositionWS());
    float remote = saturate((away - HB_LIQUID_FAR_FROM) / HB_LIQUID_FAR_RANGE);
    float deep = path;

    UNITY_BRANCH
    if (remote > 0.0)
    {
        bool known;
        float mapped = HB_WaterDepthAt(positionWS, known);

        if (known)
            deep = lerp(path, max(path, mapped), remote);
    }

    return deep;
}

float HB_CausticFocus(float2 uv, float reach, float lod)
{
    float3 bent = SAMPLE_TEXTURE2D_LOD(_HB_Caustics, sampler_HB_Caustics, uv, lod).xyz;
    float spread = (1.0 + reach * bent.x) * (1.0 + reach * bent.y) - reach * reach * bent.z * bent.z;

    return 1.0 / (abs(spread) + HB_CAUSTIC_SHARP);
}

half HB_LiquidCaustics(float3 floorWS, float surfaceY, half3 sunDirection)
{
    float depth = surfaceY - floorWS.y;

    if (HB_LiquidCausticAmount() <= 0.0h || depth <= 0.0 || sunDirection.y <= 0.05h || _HB_CausticParams.y < 0.5)
        return 0.0h;

    float2 lit = floorWS.xz + float2(sunDirection.xz) * (depth / float(sunDirection.y));
    float tiles = _HB_CausticParams.x / max(float(_CausticScale), 0.05);
    float2 uv = lit * tiles;
    float reach = min(depth, HB_CAUSTIC_DEEP) * HB_CAUSTIC_FOCUS;
    float pixel = -TransformWorldToView(floorWS).z / (abs(UNITY_MATRIX_P[1][1]) * _ScaledScreenParams.y * 0.5);
    float lod = min(depth * HB_CAUSTIC_BLUR, HB_CAUSTIC_BLUR_MOST)
              + max(log2(max(pixel * tiles * HB_CAUSTIC_SIZE, 1e-4)), 0.0);

    float focus = HB_CausticFocus(uv, reach, lod)
                + HB_CausticFocus(HB_WaveTurn(uv, 1.0) * HB_CAUSTIC_SECOND + HB_CAUSTIC_SHIFT, reach, lod);
    float still = 2.0 / (1.0 + HB_CAUSTIC_SHARP);

    half bright = half(max(focus - still, 0.0) * 0.5 * saturate(depth / HB_CAUSTIC_SHORE));

    return bright * HB_LiquidCausticAmount() * HB_CAUSTIC_GAIN;
}

half3 HB_LiquidRipple(TEXTURE2D_PARAM(map, samplerMap), float2 uv, float2 flow, float fit, half strength)
{
    float wide = HB_LIQUID_SECOND_SCALE * fit;
    float2 first = HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED, fit, 0.0);
    float2 second = HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED * HB_LIQUID_SECOND_SPEED, wide,
                                   HB_LIQUID_SECOND_TURN);

    half3 a = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(map, samplerMap, first, s_UvDx * fit, s_UvDy * fit),
                                strength);
    half3 b = UnpackNormalScale(
        SAMPLE_TEXTURE2D_GRAD(map, samplerMap, second, HB_LiquidLayerSlope(s_UvDx, wide, HB_LIQUID_SECOND_TURN),
                              HB_LiquidLayerSlope(s_UvDy, wide, HB_LIQUID_SECOND_TURN)), strength);

    return normalize(half3(a.xy + b.xy, a.z * b.z));
}

half3 HB_LiquidNormalAt(float2 uv)
{
    float2 flow = HB_LiquidFlow();
    float tiling = max(float(_Tiling), 1e-4);
    float own = HB_LiquidOwnShare(s_Mix);
    half3 ripple = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (own > 0.0)
    {
        ripple += HB_LiquidRipple(TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), uv, flow, 1.0, _BumpScale)
                * half(own);
    }

    UNITY_BRANCH
    if (s_Mix.x > 0.0)
    {
        ripple += HB_LiquidRipple(TEXTURE2D_ARGS(_OtherBumpMap0, sampler_BumpMap), uv, flow,
                                  _OtherSurface[0].x / tiling, half(_OtherSurface[0].y)) * half(s_Mix.x);
    }

    UNITY_BRANCH
    if (s_Mix.y > 0.0)
    {
        ripple += HB_LiquidRipple(TEXTURE2D_ARGS(_OtherBumpMap1, sampler_BumpMap), uv, flow,
                                  _OtherSurface[1].x / tiling, half(_OtherSurface[1].y)) * half(s_Mix.y);
    }

    UNITY_BRANCH
    if (s_Mix.z > 0.0)
    {
        ripple += HB_LiquidRipple(TEXTURE2D_ARGS(_OtherBumpMap2, sampler_BumpMap), uv, flow,
                                  _OtherSurface[2].x / tiling, half(_OtherSurface[2].y)) * half(s_Mix.z);
    }

    return normalize(ripple);
}

half3 HB_LiquidNormalTS(float2 uv)
{
#if defined(_HB_LIQUID_STREAMS)
    HiddenBullStream stream = HB_LiquidStreamOf(uv);
    half3 normal;

    UNITY_BRANCH
    if (stream.moving)
    {
        HB_LiquidFootprint(stream.firstDx, stream.firstDy);

        half3 first = HB_LiquidNormalAt(stream.first);

        HB_LiquidFootprint(stream.secondDx, stream.secondDy);
        normal = normalize(lerp(first, HB_LiquidNormalAt(stream.second), stream.blend));
    }
    else
    {
        HB_LiquidFootprint(stream.baseDx, stream.baseDy);
        normal = HB_LiquidNormalAt(uv);
    }

    HB_LiquidFootprint(stream.baseDx, stream.baseDy);

    return normal;
#else
    HB_LiquidFootprint(ddx(uv), ddy(uv));

    return HB_LiquidNormalAt(uv);
#endif
}

half HB_LiquidPattern(float2 uv, float speed, float scale)
{
    float wide = scale * HB_LiquidPatternFit();
    float2 p = HB_LiquidLayer(uv, HB_LiquidFlow(), speed, wide, HB_LIQUID_PATTERN_TURN);

    if (HB_BRUSH_ATLAS_BOUND < 0.5h)
        return 0.5h;

    return HB_BrushRead(p, HB_LiquidLayerSlope(s_UvDx, wide, HB_LIQUID_PATTERN_TURN),
                        HB_LiquidLayerSlope(s_UvDy, wide, HB_LIQUID_PATTERN_TURN)).coverage;
}

float HB_LiquidCapFrom()
{
    float driven = HB_WAVES_BOUND > 0.5 ? lerp(HB_CAP_CALM, HB_CAP_STORM, saturate(_HB_WaveFrame.w))
                                        : HB_WAVE_CAP_FROM;

    return HB_Mix(_WindWaves > 0.5h ? driven : HB_WAVE_CAP_FROM, _OtherWave[0].y > 0.5 ? driven : HB_WAVE_CAP_FROM,
                  _OtherWave[1].y > 0.5 ? driven : HB_WAVE_CAP_FROM, _OtherWave[2].y > 0.5 ? driven : HB_WAVE_CAP_FROM);
}

half HB_LiquidTrailAs(float2 restXZ, float2 shore, float3 positionWS)
{
    half trail = 0.0h;
    float open = HB_LiquidOpen(shore);
    float3 bands = HB_LiquidBands() * open;
    float away = distance(positionWS, GetCameraPositionWS());

    UNITY_BRANCH
    if (HB_LiquidFoamAmount() > 0.0 && bands.x + bands.y > HB_TRAIL_FROM && !HB_LIQUID_LOW
        && away < HB_FOAM_FADE_FROM + HB_FOAM_FADE_RANGE)
    {
        float wide = HB_LiquidLength();
        float push = HB_LiquidPush(open);
        float spacing = away * HB_TRAIL_PIXEL / wide;
        float from = HB_LiquidCapFrom();

        UNITY_UNROLL
        for (int past = 1; past <= 2; past++)
        {
            float2 ahead = restXZ / wide + _HB_WaveFrame.xy * (HB_TRAIL_STEP * past);
            float fold = HB_SampleWaveFoldLite(ahead, bands, spacing) * push;

            trail = max(trail, half(saturate((fold - from) / HB_WAVE_CAP_RANGE) * (past == 1 ? HB_TRAIL_NEAR
                                                                                           : HB_TRAIL_FAR)));
        }
    }

    return trail;
}

half HB_LiquidTrail(float2 restXZ, float2 shore, float3 positionWS)
{
    float3 keep = s_Mix;
    float own = HB_LiquidOwnShare(keep);
    half trail = 0.0h;

    UNITY_LOOP
    for (int source = 0; source < 4; source++)
    {
        float share = HB_LiquidSourceShare(source, own, keep);

        if (share <= 0.0)
            continue;

        s_Mix = HB_LiquidSourceMix(source);
        trail += HB_LiquidTrailAs(restXZ, shore, positionWS) * half(share);
    }

    s_Mix = keep;

    return trail;
}

half HB_FoamGrain(float2 uv)
{
    float2 flow = HB_LiquidFlow();
    float wide = float(_PatternScale);
    float tight = wide * HB_FOAM_SECOND_SCALE;
    float2 first = HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED, wide, HB_LIQUID_PATTERN_TURN);
    float2 second = HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED * HB_FOAM_SECOND_SPEED, tight,
                                   HB_FOAM_SECOND_TURN);

    half coarse = SAMPLE_TEXTURE2D_GRAD(_HB_FoamPattern, sampler_HB_FoamPattern, first,
                                        HB_LiquidLayerSlope(s_UvDx, wide, HB_LIQUID_PATTERN_TURN),
                                        HB_LiquidLayerSlope(s_UvDy, wide, HB_LIQUID_PATTERN_TURN)).r;
    half fine = SAMPLE_TEXTURE2D_GRAD(_HB_FoamPattern, sampler_HB_FoamPattern, second,
                                      HB_LiquidLayerSlope(s_UvDx, tight, HB_FOAM_SECOND_TURN),
                                      HB_LiquidLayerSlope(s_UvDy, tight, HB_FOAM_SECOND_TURN)).r;

    return _HB_CausticParams.z > 0.5 ? lerp(coarse, fine, HB_FOAM_SECOND_SHARE) : 0.5h;
}

half HB_FoamGrainAlong(float2 uv)
{
    half grain = s_Grain;

#if defined(_HB_LIQUID_STREAMS)
    UNITY_BRANCH
    if (HB_LiquidStreamed())
    {
        float2 reach = s_Stream * (float(_Tiling) * HB_STREAM_STREAK);

        grain = (HB_FoamGrain(uv - reach) + HB_FoamGrain(uv) + HB_FoamGrain(uv + reach)) * (1.0h / 3.0h);
    }
#endif

    return grain;
}

half HB_FoamCover(half amount, half grain)
{
    return saturate((amount * (1.0h + HB_FOAM_EDGE_SOFTNESS) - grain) / HB_FOAM_EDGE_SOFTNESS);
}

float HB_FoamShoreAway(float3 rest)
{
    float4 field = HB_ShoreFieldOf(rest.xz, rest.y, false);
    float away = field.w > 0.5 ? field.x : -1.0;

    UNITY_BRANCH
    if (_HB_WaterBlend < 1.0)
    {
        float4 before = HB_ShoreFieldOf(rest.xz, rest.y, true);

        if (before.w > 0.5 && field.w > 0.5)
            away = lerp(before.x, field.x, _HB_WaterBlend);
    }

    return away;
}

half HB_FoamShore(float3 positionWS, half grain)
{
    half amount = 0.0h;

    float reach = HB_LiquidShoreFoam();

    UNITY_BRANCH
    if (reach > 0.0 && !HB_LiquidStill())
    {
        float away = HB_FoamShoreAway(float3(positionWS.x, UNITY_MATRIX_M._m13 + HB_LiquidRise(positionWS.xz),
                                              positionWS.z));

        UNITY_BRANCH
        if (away > 0.0 && away < reach)
        {
            float band = 1.0 - away / reach;
            float swell = saturate(HB_LiquidWaveAmount() * HB_LiquidHeight() * HB_SHORE_FOAM_WAVES);
            float front = frac(away / HB_SHORE_FOAM_SPAN + HB_LiquidTime() * HB_SHORE_FOAM_PACE
                             + (float(grain) - 0.5) * HB_SHORE_FOAM_WOBBLE);
            float stripe = smoothstep(0.0, HB_SHORE_FOAM_FRONT, front)
                         * (1.0 - smoothstep(HB_SHORE_FOAM_FRONT, HB_SHORE_FOAM_TAIL, front));

            amount = half(band * band * HB_SHORE_FOAM_BASE + stripe * band * swell);
        }
    }

    return amount;
}

half HB_LiquidFoam(float2 uv, float thickness, half fold, half trail, float3 positionWS, out half veil)
{
    float deep = thickness * abs(dot(float3(GetWorldSpaceNormalizeViewDir(positionWS)), s_Normal));
    half feather = half(saturate(deep / HB_FOAM_FEATHER));
    float away = distance(positionWS, GetCameraPositionWS());
    half close = half(1.0 - saturate((away - HB_FOAM_FADE_FROM) / HB_FOAM_FADE_RANGE));
    half strength = feather * close * half(HB_LiquidFoamAmount());
    half foam = 0.0h;

    veil = 0.0h;

    UNITY_BRANCH
    if (strength > 0.0h)
    {
        half grain = HB_FoamGrainAlong(uv);
        half edge = half(1.0 - saturate(deep / HB_LiquidFoamDistance()));
        half rush = half(saturate(length(s_Stream) / HB_STREAM_WHITE));
        half bank = half(1.0 - saturate(deep / HB_STREAM_BANK));
        half churn = saturate(half(s_Shear * HB_STREAM_SHEAR) + rush * bank + rush * rush * HB_STREAM_SHEET);
        half caps = max(max(half(saturate((fold - HB_LiquidCapFrom()) / HB_WAVE_CAP_RANGE)), trail), churn);

        veil = HB_FoamCover(edge * edge, grain) * strength * HB_FOAM_VEIL;
        foam = HB_FoamCover(saturate(HB_FoamShore(positionWS, grain) + caps), grain) * strength;
    }

    return foam;
}

struct HiddenBullBubbles
{
    half dome;
    half ring;
    half burst;
    half2 slope;
    float2 lens;
};

float4 HB_LiquidHash4(float2 id)
{
    float4 p = frac(id.xyxy * float4(0.1031, 0.1030, 0.0973, 0.1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

HiddenBullBubbles HB_LiquidBubbleField(float2 uv, half amount, float cells, float rate)
{
    HiddenBullBubbles bubbles = (HiddenBullBubbles)0;

    float density = cells * _PatternScale;
    float2 p = (uv + HB_LiquidAnchor()) * density;
    float2 cell = floor(p);
    float time = HB_LiquidTime() * max(float(_Pulse), 0.05) * HB_BUBBLE_RATE * rate;

    UNITY_UNROLL
    for (int y = -1; y <= 1; y++)
    {
        UNITY_UNROLL
        for (int x = -1; x <= 1; x++)
        {
            float2 id = cell + float2(x, y);
            float4 h = HB_LiquidHash4(id);

            if (h.w >= amount)
                continue;

            float2 centre = id + 0.5 + (h.xy - 0.5) * 0.5;
            float radius = HB_BUBBLE_RADIUS * (0.6 + 0.4 * h.z);
            float age = frac(time * (0.6 + 0.8 * h.z) + h.x * 3.1 + h.y * 1.7);

            float2 offset = p - centre;
            float distance = length(offset);

            if (age < HB_BUBBLE_POP)
            {
                float growth = smoothstep(0.0, 1.0, age / HB_BUBBLE_POP);
                float size = radius * growth;
                float dome = saturate(1.0 - (distance * distance) / max(size * size, 1e-4)) * growth;

                bubbles.dome = max(bubbles.dome, half(dome));
                bubbles.slope += half2(offset / max(size, 1e-3) * saturate(dome * 4.0));
                bubbles.lens -= offset * (dome * HB_BUBBLE_LENS / density);
            }
            else
            {
                float t = (age - HB_BUBBLE_POP) / (1.0 - HB_BUBBLE_POP);
                float ringRadius = radius * (1.0 + 0.8 * t);
                float width = radius * 0.12;
                float ring = exp(-(distance - ringRadius) * (distance - ringRadius) / (width * width)) * (1.0 - t);

                bubbles.ring = max(bubbles.ring, half(ring));
                bubbles.burst = max(bubbles.burst, half(step(distance, ringRadius) * (1.0 - t)));
            }
        }
    }

    return bubbles;
}

half HB_LavaSample(float2 uv, float2 uvDx, float2 uvDy)
{
    float own = HB_LiquidOwnShare(s_Mix);
    half crust = 0.0h;

    UNITY_BRANCH
    if (own > 0.0)
        crust += Luminance(SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, uvDx, uvDy).rgb) * half(own);

    UNITY_BRANCH
    if (s_Mix.x > 0.0)
        crust += Luminance(SAMPLE_TEXTURE2D_GRAD(_OtherBaseMap0, sampler_BaseMap, uv, uvDx, uvDy).rgb) * half(s_Mix.x);

    UNITY_BRANCH
    if (s_Mix.y > 0.0)
        crust += Luminance(SAMPLE_TEXTURE2D_GRAD(_OtherBaseMap1, sampler_BaseMap, uv, uvDx, uvDy).rgb) * half(s_Mix.y);

    UNITY_BRANCH
    if (s_Mix.z > 0.0)
        crust += Luminance(SAMPLE_TEXTURE2D_GRAD(_OtherBaseMap2, sampler_BaseMap, uv, uvDx, uvDy).rgb) * half(s_Mix.z);

    return crust;
}

half HB_LavaCool(float2 uv)
{
    float2 flow = HB_LiquidFlow();
    float fit = HB_LiquidTilingFit();
    float wide = HB_LIQUID_SECOND_SCALE * fit;

    half churn = HB_LavaSample(HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED * 0.6, wide, HB_LIQUID_SECOND_TURN),
                               HB_LiquidLayerSlope(s_UvDx, wide, HB_LIQUID_SECOND_TURN),
                               HB_LiquidLayerSlope(s_UvDy, wide, HB_LIQUID_SECOND_TURN));
    half cool = HB_LavaSample(HB_LiquidLayer(uv, flow, HB_LIQUID_FLOW_SPEED, fit, 0.0)
                              + (churn - 0.5h) * HB_LAVA_CHURN, s_UvDx * fit, s_UvDy * fit);
    half pattern = HB_LiquidPattern(uv, HB_LIQUID_FLOW_SPEED * 0.5, 1.0);

    return saturate(cool * 0.5h + churn * 0.2h + pattern * 0.3h);
}

half HB_LavaCrust(half cool)
{
    half threshold = 1.0h - HB_LiquidCrustAmount();

    return smoothstep(threshold - HB_CRUST_SOFTNESS, threshold + HB_CRUST_SOFTNESS, cool)
         * saturate(HB_LiquidCrustAmount() / HB_CRUST_SOFTNESS);
}

half HB_LiquidGlints(float2 uv, half3 normalWS, half3 viewDirectionWS, half3 lightDirectionWS)
{
    half sparkle = HB_LiquidPattern(uv, HB_LIQUID_FLOW_SPEED * 1.7, HB_GLINT_SCALE);
    half alignment = saturate(dot(reflect(-viewDirectionWS, normalWS), lightDirectionWS));

    return PositivePow(alignment, 48.0h) * smoothstep(0.72h, 0.85h, sparkle) * 4.0h;
}

float HB_LiquidBehind(float3 positionWS, float2 screenUV)
{
    float surface = LinearEyeDepth(positionWS, GetWorldToViewMatrix());
    float scene = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);

    return scene - surface;
}

float HB_LiquidThickness(float3 positionWS, float2 screenUV)
{
    return max(HB_LiquidBehind(positionWS, screenUV), 0.0);
}

#endif
