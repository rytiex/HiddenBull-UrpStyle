#ifndef HB_LIQUID_SURFACE_PASS_INCLUDED
#define HB_LIQUID_SURFACE_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherSurface.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparentFog.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidLevel.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidWaves.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_HB_LiquidGradient);
TEXTURE2D(_HB_LiquidFoamPattern);
TEXTURE2D(_HB_LiquidCaustics);
TEXTURE2D(_HB_LiquidCutMask);
TEXTURE2D(_HB_LiquidNormal0);
TEXTURE2D(_HB_LiquidNormal1);
TEXTURE2D(_HB_LiquidNormal2);
TEXTURE2D(_HB_LiquidNormal3);

float4 _HB_LiquidRipple[4];
float4 _HB_LiquidOptics[4];
float4 _HB_LiquidShore[4];
float4 _HB_LiquidStyle[4];
float4 _HB_LiquidFoam[4];
float4 _HB_LiquidFoamShape[4];
float4 _HB_LiquidCrest[4];
float4 _HB_LiquidCausticGrid;
float4 _HB_LiquidBrush[4];
float4 _HB_LiquidCutState;
float _HB_LiquidKeep;
float _HB_LiquidId;

float4 _HB_TerrainCamera;
float4 _HB_LiquidRanges[12];

#define HB_LIQUID_SLOTS          4
#define HB_LIQUID_SHARE_MIN      1e-3h
#define HB_LIQUID_GRADIENT_DEPTH 4.0
#define HB_LIQUID_GRADIENT_SIZE  64.0
#define HB_LIQUID_TINT_DEPTH     2.0
#define HB_LIQUID_TINT_MOST      64.0
#define HB_LIQUID_EDGE_SOFT      4.0h
#define HB_LIQUID_MURK_BLUR      0.35h
#define HB_LIQUID_SOLID          0.004h
#define HB_LIQUID_GRAIN          0.5h
#define HB_LIQUID_GLOW_BASE      0.75h
#define HB_LIQUID_GLOW_GRAIN     0.5h
#define HB_LIQUID_THROB_BASE     0.85
#define HB_LIQUID_THROB_RANGE    0.15
#define HB_LIQUID_THROB_GRAIN    9.0
#define HB_LIQUID_SUN_SCATTER    0.5h
#define HB_LIQUID_HALF_TONE      0.35h
#define HB_LIQUID_BAKED_SOFTNESS 0.5h
#define HB_LIQUID_DEBUG_RANGE    0.25h
#define HB_LIQUID_DEBUG_AMBIENT  0.35h
#define HB_LIQUID_FOAM_FROM      60.0
#define HB_LIQUID_FOAM_RANGE     140.0
#define HB_LIQUID_FOAM_FEATHER   0.06
#define HB_LIQUID_FOAM_SOFT      0.4h
#define HB_LIQUID_FOAM_VEIL      0.6h
#define HB_LIQUID_FOAM_GLOW      0.45h
#define HB_LIQUID_FOAM_MOST      0.85h
#define HB_LIQUID_FOAM_TURN      0.3
#define HB_LIQUID_FOAM_SPEED     0.025
#define HB_LIQUID_FOAM_TIGHT     0.57
#define HB_LIQUID_FOAM_TIGHT_TURN  1.9
#define HB_LIQUID_FOAM_TIGHT_PACE  0.6
#define HB_LIQUID_FOAM_TIGHT_SHARE 0.4h
#define HB_LIQUID_STRIPE_SPAN    4.0
#define HB_LIQUID_STRIPE_PACE    0.12
#define HB_LIQUID_STRIPE_WOBBLE  0.5
#define HB_LIQUID_STRIPE_FRONT   0.12
#define HB_LIQUID_STRIPE_TAIL    0.55
#define HB_LIQUID_SHORE_BASE     0.35
#define HB_LIQUID_SHORE_SWELL    4.0
#define HB_LIQUID_CREST_LIGHT    1.5h
#define HB_LIQUID_CREST_SKY      0.5h
#define HB_LIQUID_CREST_EARLY    0.5h
#define HB_LIQUID_CREST_GRAIN    0.3h
#define HB_LIQUID_CREST_BASE     0.5h
#define HB_LIQUID_CREST_FROM     0.175h
#define HB_LIQUID_CREST_HARD     0.02h
#define HB_LIQUID_CREST_SOFT     0.5h
#define HB_LIQUID_CREST_DAWN     4.0h
#define HB_LIQUID_CAUSTIC_FOCUS  2.0
#define HB_LIQUID_CAUSTIC_SHARP  0.25
#define HB_LIQUID_CAUSTIC_GAIN   1.2h
#define HB_LIQUID_CAUSTIC_SHORE  0.3
#define HB_LIQUID_CAUSTIC_DEEP   3.0
#define HB_LIQUID_CAUSTIC_BLUR   0.35
#define HB_LIQUID_CAUSTIC_MOST   3.0
#define HB_LIQUID_CAUSTIC_SECOND 0.731
#define HB_LIQUID_CAUSTIC_SHIFT  0.37
#define HB_LIQUID_CAUSTIC_SIZE   256.0
#define HB_LIQUID_CAUSTIC_TINT   2.0h
#define HB_LIQUID_CAUSTIC_MURK   0.996h
#define HB_LIQUID_CAUSTIC_SUN    0.05h
#define HB_LIQUID_CAUSTIC_NIGHT  0.35h
#define HB_LIQUID_CAUSTIC_DUSK   0.1
#define HB_LIQUID_BRUSH_LEAST    0.05
#define HB_LIQUID_BRUSH_CYCLE    6.0
#define HB_LIQUID_BRUSH_WIND     0.6
#define HB_LIQUID_BRUSH_FLOW     0.5
#define HB_LIQUID_BRUSH_SKIP     float2(17.3, 9.1)
#define HB_LIQUID_BRUSH_SHADING  0.6h
#define HB_LIQUID_BRUSH_RELIEF   0.35h
#define HB_LIQUID_STREAM_PERIOD  4.0
#define HB_LIQUID_STREAM_LEAST   1e-4
#define HB_LIQUID_STREAM_WHITE   6.0
#define HB_LIQUID_STREAM_BANK    1.5
#define HB_LIQUID_STREAM_SHEET   0.35h
#define HB_LIQUID_STREAM_SHEAR   0.6
#define HB_LIQUID_STREAM_STREAK  0.12
#define HB_LIQUID_STREAM_SPAN    1.0
#define HB_LIQUID_RIPPLE_SPEED   0.025

struct HBLiquidAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBLiquidVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 rest : TEXCOORD1;
    float4 wave : TEXCOORD2;
    float4 foam : TEXCOORD3;
    float shear : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct HBLiquidLook
{
    half3 tone;
    half murk;
    half3 shore;
    half smoothness;
    half blur;
    half glow;
    half wrap;
    half softness;
    half3 foamTone;
    half foamAmount;
    float waterline;
    float shoreFoam;
    half3 crestTone;
    half crestSoftness;
    half caustics;
    half brush;
};

struct HBLiquidFoam
{
    half cover;
    half band;
    half grain;
};

float2 HB_LiquidUV(float2 xzWS)
{
    return (xzWS - _HB_TerrainOrigin.xz) / _HB_TerrainSize.xz;
}

float HB_LiquidHeight(float2 xzWS)
{
    return HB_WaterHeight(HB_LiquidUV(xzWS), _HB_TerrainOrigin.y);
}

HBWaterLevel HB_LiquidWaterAt(float2 xzWS)
{
#if defined(HB_LIQUID_POOL)
    HBWaterLevel water;
    water.level = HB_POOL_LEVEL;
    water.wet = HB_PoolInward(xzWS) >= 0.0;
    water.mix = half4(1.0h, 0.0h, 0.0h, 0.0h);
    return water;
#else
    return HB_WaterLevel(HB_LiquidUV(xzWS), _HB_TerrainOrigin.y);
#endif
}

void HB_LiquidCut(float4 positionCS)
{
    UNITY_BRANCH
    if (_HB_LiquidCutState.x > 0.5)
    {
        float2 cut = LOAD_TEXTURE2D(_HB_LiquidCutMask, uint2(positionCS.xy)).rg;

        if (cut.r > 0.5 || (_HB_LiquidKeep > 0.5 && cut.g < 0.5))
            discard;
    }
}

HBLiquidVaryings HB_LiquidVertex(HBLiquidAttributes input)
{
    HBLiquidVaryings output = (HBLiquidVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float2 xz;
    float spacing;

#if defined(HB_LIQUID_POOL)
    xz = HB_PoolWorld((input.positionOS.xz * 2.0 - 1.0) * HB_POOL_HALF);
    spacing = HB_POOL_CELL;
#else
    float size = UNITY_MATRIX_M._m00;
    float level = UNITY_MATRIX_M._m11 - 1.0;
    float2 origin = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23);

    UNITY_BRANCH
    if (level < 0.0)
    {
        xz = origin + input.positionOS.xz * float2(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m22);
        spacing = max(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m22) / _HB_TerrainCamera.w;
    }
    else
    {
        spacing = _HB_TerrainOrigin.w * exp2(level) / _HB_TerrainCamera.w;
        float2 grid = input.positionOS.xz * round(size / spacing);

        xz = origin + grid * spacing;

        float distance = length(float3(xz.x, HB_LiquidHeight(xz), xz.y) - _HB_TerrainCamera.xyz);
        float2 range = _HB_LiquidRanges[(int)level].xy;
        float morph = saturate((distance - range.x) / max(range.y - range.x, HB_EPSILON));

        grid -= frac(grid * 0.5) * 2.0 * morph;
        xz = origin + grid * spacing;
        spacing *= 1.0 + morph;
    }
#endif

    HBWaterLevel water = HB_LiquidWaterAt(xz);
    HBLiquidWaveShape shape = HB_LiquidWaveShape(water.mix);
    HBLiquidSea sea = HB_LiquidSeaAt(xz, shape);
    float away = length(float3(xz.x, water.level, xz.y) - _HB_TerrainCamera.xyz);
    float near = HB_WaveNear(away);
    float3 moved = float3(0.0, 0.0, 0.0);
    float2 caps = float2(0.0, 0.0);

    UNITY_BRANCH
    if (shape.amount * near > 0.0)
        moved = HB_WaveOffset(xz, shape, sea, spacing) * near;

    UNITY_BRANCH
    if (shape.amount * sea.wind * near > 0.0)
        caps = HB_WaveCaps(xz, shape, sea, spacing) * near;

    output.positionWS = float3(xz.x + moved.x, water.level + moved.y, xz.y + moved.z);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.rest = float4(xz, sea.wind, sea.shore);
    output.wave = float4(moved.y, shape.amount, shape.height, shape.length);
    output.foam = float4(caps.x, sea.depth, caps.y, shape.cap);

#if !defined(HB_LIQUID_POOL)
    float2 east = float2(HB_LIQUID_STREAM_SPAN, 0.0);
    float2 north = float2(0.0, HB_LIQUID_STREAM_SPAN);
    float2 across = HB_WaterFlow(HB_LiquidUV(xz + east)).xy - HB_WaterFlow(HB_LiquidUV(xz - east)).xy;
    float2 along = HB_WaterFlow(HB_LiquidUV(xz + north)).xy - HB_WaterFlow(HB_LiquidUV(xz - north)).xy;

    output.shear = (length(across) + length(along)) / (HB_LIQUID_STREAM_SPAN * 2.0);
#endif

    return output;
}

HBLiquidLook HB_LiquidLookAt(half4 mix, float path)
{
    HBLiquidLook look = (HBLiquidLook)0;

    float at = 1.0 - exp(-max(path, 0.0) / HB_LIQUID_GRADIENT_DEPTH);
    float u = (at * (HB_LIQUID_GRADIENT_SIZE - 1.0) + 0.5) / HB_LIQUID_GRADIENT_SIZE;
    half total = 0.0h;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        half share = mix[slot];

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float v = (slot + 0.5) / HB_LIQUID_SLOTS;
            half4 depth = half4(SAMPLE_TEXTURE2D_LOD(_HB_LiquidGradient, sampler_LinearClamp, float2(u, v), 0));
            half4 optics = half4(_HB_LiquidOptics[slot]);
            half4 style = half4(_HB_LiquidStyle[slot]);
            half4 foam = half4(_HB_LiquidFoam[slot]);
            float4 foamShape = _HB_LiquidFoamShape[slot];
            half4 crest = half4(_HB_LiquidCrest[slot]);
            half4 strokes = half4(_HB_LiquidBrush[slot]);

            look.tone += depth.rgb * share;
            look.murk += depth.a * share;
            look.shore += half3(_HB_LiquidShore[slot].rgb) * share;
            look.smoothness += optics.x * share;
            look.caustics += optics.y * share;
            look.blur += optics.z * share;
            look.glow += optics.w * share;
            look.wrap += style.x * share;
            look.softness += style.y * share;
            look.foamTone += foam.rgb * share;
            look.foamAmount += foam.a * share;
            look.waterline += foamShape.x * share;
            look.shoreFoam += foamShape.y * share;
            look.crestTone += crest.rgb * share;
            look.crestSoftness += crest.a * share;
            look.brush += strokes.x * share;
            total += share;
        }
    }

    half scale = rcp(max(total, HB_LIQUID_SHARE_MIN));

    look.tone *= scale;
    look.murk = saturate(look.murk * scale);
    look.shore *= scale;
    look.smoothness *= scale;
    look.caustics *= scale;
    look.blur *= scale;
    look.glow *= scale;
    look.wrap *= scale;
    look.softness *= scale;
    look.foamTone *= scale;
    look.foamAmount *= scale;
    look.waterline *= scale;
    look.shoreFoam *= scale;
    look.crestTone *= scale;
    look.crestSoftness *= scale;
    look.brush *= scale;
    return look;
}

float HB_LiquidBehind(float surfaceEye, float2 screenUV)
{
    return LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams) - surfaceEye;
}

float2 HB_LiquidTurn(float2 value, float turn)
{
    float s, c;
    sincos(turn, s, c);

    return float2(c * value.x - s * value.y, s * value.x + c * value.y);
}

struct HBLiquidStream
{
    float2 first;
    float2 flow;
    float2 second;
    float speed;
    float shear;
    half blend;
    bool moving;
};

HBLiquidStream HB_LiquidStreamAt(float2 restXZ)
{
    HBLiquidStream stream = (HBLiquidStream)0;

#if !defined(HB_LIQUID_POOL)
    float2 uv = HB_LiquidUV(restXZ);
    float2 flow = HB_WaterInside(uv) ? HB_WaterFlow(uv).xy : float2(0.0, 0.0);
    float phase = frac(_Time.y / HB_LIQUID_STREAM_PERIOD);
    float2 run = flow * HB_LIQUID_STREAM_PERIOD;

    stream.first = run * (phase - 0.5);
    stream.second = run * (frac(phase + 0.5) - 0.5);
    stream.flow = flow;
    stream.speed = length(flow);
    stream.blend = half(abs(phase * 2.0 - 1.0));
    stream.moving = dot(flow, flow) > HB_LIQUID_STREAM_LEAST;
#endif

    return stream;
}

half HB_LiquidFoamRead(float2 xz, float2 dx, float2 dy, float scale, float turn, float pace)
{
    float2 flow = HB_WAVE_BOUND ? _HB_WaveFrame.xy : float2(0.0, 1.0);
    float2 uv = HB_LiquidTurn(xz, turn) * scale + frac(flow * (HB_LIQUID_FOAM_SPEED * pace * _Time.y));

    return half(SAMPLE_TEXTURE2D_GRAD(_HB_LiquidFoamPattern, sampler_trilinear_repeat, uv,
                                      HB_LiquidTurn(dx, turn) * scale, HB_LiquidTurn(dy, turn) * scale).r);
}

half HB_LiquidFoamGrain(float2 restXZ, float2 restDx, float2 restDy, float4 mix)
{
    float2 xz = restXZ - _HB_TerrainOrigin.xz;
    half grain = 0.0h;
    half total = 0.0h;

    UNITY_LOOP
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        half share = half(mix[slot]);

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float scale = _HB_LiquidFoamShape[slot].z;
            half coarse = HB_LiquidFoamRead(xz, restDx, restDy, scale, HB_LIQUID_FOAM_TURN, 1.0);
            half fine = HB_LiquidFoamRead(xz, restDx, restDy, scale * HB_LIQUID_FOAM_TIGHT,
                                          HB_LIQUID_FOAM_TIGHT_TURN, HB_LIQUID_FOAM_TIGHT_PACE);

            grain += lerp(coarse, fine, HB_LIQUID_FOAM_TIGHT_SHARE) * share;
            total += share;
        }
    }

    return grain * rcp(max(total, HB_LIQUID_SHARE_MIN));
}

half HB_LiquidFoamCover(half amount, half grain)
{
    return saturate((amount * (1.0h + HB_LIQUID_FOAM_SOFT) - grain) / HB_LIQUID_FOAM_SOFT);
}

half HB_LiquidShoreFoam(float depth, half grain, float reach, float swell, out half band)
{
    float away = depth / max(HB_CALM_SLOPE, 1e-3);
    float near = reach > 0.0 ? saturate(1.0 - away / reach) : 0.0;
    float front = frac(away / HB_LIQUID_STRIPE_SPAN + _Time.y * HB_LIQUID_STRIPE_PACE
                       + (float(grain) - 0.5) * HB_LIQUID_STRIPE_WOBBLE);
    float stripe = smoothstep(0.0, HB_LIQUID_STRIPE_FRONT, front)
                 * (1.0 - smoothstep(HB_LIQUID_STRIPE_FRONT, HB_LIQUID_STRIPE_TAIL, front));

    band = half(near);
    return half(near * near * HB_LIQUID_SHORE_BASE + stripe * near * swell);
}

HBLiquidFoam HB_LiquidFoamAt(HBLiquidLook look, float2 restXZ, float2 restDx, float2 restDy, float deep, float away,
                             float4 carried, HBLiquidWaveShape shape, HBLiquidStream stream)
{
    HBLiquidFoam foam = (HBLiquidFoam)0;
    foam.grain = 0.5h;

    half feather = half(saturate(deep / HB_LIQUID_FOAM_FEATHER));
    half close = half(1.0 - saturate((away - HB_LIQUID_FOAM_FROM) / HB_LIQUID_FOAM_RANGE));
    half strength = feather * close * look.foamAmount;

    UNITY_BRANCH
    if (strength > 0.0h)
    {
        half grain = HB_LiquidFoamGrain(restXZ, restDx, restDy, shape.mix);

        UNITY_BRANCH
        if (stream.moving)
        {
            float2 reach = stream.flow * HB_LIQUID_STREAM_STREAK;

            grain = (grain + HB_LiquidFoamGrain(restXZ - reach, restDx, restDy, shape.mix)
                     + HB_LiquidFoamGrain(restXZ + reach, restDx, restDy, shape.mix)) * (1.0h / 3.0h);
        }

        foam.grain = grain;
        half edge = half(1.0 - saturate(deep / max(look.waterline, 1e-3)));
        float swell = saturate(shape.amount * shape.height * HB_LIQUID_SHORE_SWELL);
        half shore = HB_LiquidShoreFoam(carried.y, grain, look.shoreFoam, swell, foam.band);
        half veil = HB_LiquidFoamCover(edge * edge, grain) * strength * HB_LIQUID_FOAM_VEIL;
        half rush = half(saturate(stream.speed / HB_LIQUID_STREAM_WHITE));
        half bank = half(1.0 - saturate(deep / HB_LIQUID_STREAM_BANK));
        half churn = saturate(half(stream.shear * HB_LIQUID_STREAM_SHEAR) + rush * bank
                              + rush * rush * HB_LIQUID_STREAM_SHEET);
        half spots = HB_LiquidFoamCover(saturate(shore + max(half(carried.x), churn)), grain) * strength;
        half solid = spots * spots;

        foam.cover = min(solid + (1.0h - solid) * min(spots - solid + veil, HB_LIQUID_FOAM_GLOW),
                         HB_LIQUID_FOAM_MOST);
    }

    return foam;
}

float HB_LiquidCausticFocus(float2 uv, float reach, float lod)
{
    float3 bent = SAMPLE_TEXTURE2D_LOD(_HB_LiquidCaustics, sampler_trilinear_repeat, uv, lod).xyz;
    float spread = (1.0 + reach * bent.x) * (1.0 + reach * bent.y) - reach * reach * bent.z * bent.z;

    return 1.0 / (abs(spread) + HB_LIQUID_CAUSTIC_SHARP);
}

half HB_LiquidCaustics(float3 floorWS, float surfaceY, half3 sunDirection, float4 mix)
{
    float depth = max(surfaceY - floorWS.y, 0.0);
    float2 lit = floorWS.xz - _HB_TerrainOrigin.xz + float2(sunDirection.xz) * (depth / float(sunDirection.y));
    float reach = min(depth, HB_LIQUID_CAUSTIC_DEEP) * HB_LIQUID_CAUSTIC_FOCUS;
    float pixel = -TransformWorldToView(floorWS).z / (abs(UNITY_MATRIX_P[1][1]) * _ScaledScreenParams.y * 0.5);
    float still = 2.0 / (1.0 + HB_LIQUID_CAUSTIC_SHARP);
    float bright = 0.0;
    float total = 0.0;

    UNITY_LOOP
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        float share = mix[slot];

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float tiles = _HB_LiquidCausticGrid.x / max(_HB_LiquidStyle[slot].w, 0.05);
            float2 uv = lit * tiles;
            float lod = min(depth * HB_LIQUID_CAUSTIC_BLUR, HB_LIQUID_CAUSTIC_MOST)
                      + max(log2(max(pixel * tiles * HB_LIQUID_CAUSTIC_SIZE, 1e-4)), 0.0);
            float focus = HB_LiquidCausticFocus(uv, reach, lod)
                        + HB_LiquidCausticFocus(HB_WaveTurn(uv, 1.0) * HB_LIQUID_CAUSTIC_SECOND
                                                + HB_LIQUID_CAUSTIC_SHIFT, reach, lod);

            bright += max(focus - still, 0.0) * share;
            total += share;
        }
    }

    bright *= 0.5 * saturate(depth / HB_LIQUID_CAUSTIC_SHORE) * rcp(max(total, HB_LIQUID_SHARE_MIN));
    return half(bright) * HB_LIQUID_CAUSTIC_GAIN;
}

half HB_LiquidFloorShadow(float3 floorWS)
{
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    half4 crisp = GetMainLightShadowParams();

    crisp.y = SOFT_SHADOW_QUALITY_OFF;
    return MainLightRealtimeShadow(TransformWorldToShadowCoord(floorWS), crisp, GetMainLightShadowSamplingData());
#else
    return 1.0h;
#endif
}

half3 HB_LiquidRipples(float3 positionWS, half3 normalWS, half4 mix)
{
    half share = dot(mix, half4(_HB_LiquidFoamShape[0].w, _HB_LiquidFoamShape[1].w, _HB_LiquidFoamShape[2].w,
                                _HB_LiquidFoamShape[3].w));

    UNITY_BRANCH
    if (HB_SURFACE_RAIN > 0.0 && share > 0.0h)
    {
        half detail = HB_WeatherDetail(positionWS);

        UNITY_BRANCH
        if (detail > 0.0h)
        {
            HBWeather weather = (HBWeather)0;
            weather.rain = half(HB_SURFACE_RAIN) * HB_ShelterOpen(positionWS, half3(0.0h, 1.0h, 0.0h))
                         * HB_ShelterCover(positionWS.xz) * detail * share;

            normalWS = HB_RainRipples(positionWS, normalWS, weather);
        }
    }

    return normalWS;
}

float2 HB_LiquidDrift(float2 restXZ)
{
    float2 wind = (HB_WAVE_BOUND ? _HB_WaveFrame.xy : float2(0.0, 1.0)) * (HB_WAVE_WIND * HB_LIQUID_BRUSH_WIND);

#if defined(HB_LIQUID_POOL)
    return wind;
#else
    float2 uv = HB_LiquidUV(restXZ);
    float2 flow = HB_WaterInside(uv) ? HB_WaterFlow(uv).xy * HB_LIQUID_BRUSH_FLOW : float2(0.0, 0.0);

    return dot(flow, flow) > dot(wind, wind) ? flow : wind;
#endif
}

half2 HB_LiquidRippleRead(TEXTURE2D_PARAM(map, samplerMap), float2 xz, float2 dx, float2 dy, float tiling)
{
    float2 wind = HB_WAVE_BOUND ? _HB_WaveFrame.xy : float2(0.0, 1.0);
    float2 uv = xz * tiling + frac(wind * (HB_LIQUID_RIPPLE_SPEED * _Time.y));

    return UnpackNormal(SAMPLE_TEXTURE2D_GRAD(map, samplerMap, uv, dx * tiling, dy * tiling)).xy;
}

half2 HB_LiquidRippleFlow(TEXTURE2D_PARAM(map, samplerMap), float2 xz, float2 dx, float2 dy, float tiling,
                          HBLiquidStream stream)
{
    half2 ripple = HB_LiquidRippleRead(TEXTURE2D_ARGS(map, samplerMap), xz - stream.first, dx, dy, tiling);

    UNITY_BRANCH
    if (stream.moving)
    {
        half2 second = HB_LiquidRippleRead(TEXTURE2D_ARGS(map, samplerMap), xz - stream.second, dx, dy, tiling);

        ripple = lerp(ripple, second, stream.blend);
    }

    return ripple;
}

#define HB_LIQUID_RIPPLE_SLOT(index)                                                                                 \
    UNITY_BRANCH                                                                                                     \
    if (half(mix[index]) > HB_LIQUID_SHARE_MIN && _HB_LiquidRipple[index].y > 0.0)                                   \
        ripple += HB_LiquidRippleFlow(TEXTURE2D_ARGS(_HB_LiquidNormal##index, sampler_trilinear_repeat), xz, dx, dy, \
                                      _HB_LiquidRipple[index].x, stream) * half(mix[index] * _HB_LiquidRipple[index].y);

half2 HB_LiquidRipple(float2 restXZ, float2 dx, float2 dy, float4 mix, HBLiquidStream stream)
{
    float2 xz = restXZ - _HB_TerrainOrigin.xz;
    half2 ripple = half2(0.0h, 0.0h);

    HB_LIQUID_RIPPLE_SLOT(0)
    HB_LIQUID_RIPPLE_SLOT(1)
    HB_LIQUID_RIPPLE_SLOT(2)
    HB_LIQUID_RIPPLE_SLOT(3)

    return ripple;
}

HBBrushTap HB_LiquidBrushTap(float2 xz, float2 dx, float2 dy, float frequency)
{
    return HB_BrushRead(xz * frequency, dx * frequency, dy * frequency);
}

HBBrushSample HB_LiquidBrush(float3 positionWS, float2 restXZ, float2 restDx, float2 restDy, half amount,
                             float4 mix)
{
    HBBrushSample brush = HB_NoBrush();
    float away = distance(positionWS, GetCameraPositionWS());
    half fade = half(saturate(1.0 - (away - HB_BRUSH_FADE_START) * HB_BRUSH_FADE_RATE)) * amount;

    UNITY_BRANCH
    if (fade > 0.0h && HB_BRUSH_ATLAS_BOUND > 0.5)
    {
        float2 drift = HB_LiquidDrift(restXZ) * HB_LIQUID_BRUSH_CYCLE;
        float cycle = _Time.y / HB_LIQUID_BRUSH_CYCLE;
        float first = frac(cycle);
        float second = frac(cycle + 0.5);
        half share = half(1.0 - abs(first * 2.0 - 1.0));
        half2 spine = half2(0.0h, 0.0h);
        half coverage = 0.0h;
        half total = 0.0h;

        UNITY_LOOP
        for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
        {
            float4 strokes = _HB_LiquidBrush[slot];
            half weight = half(mix[slot] * strokes.x);

            UNITY_BRANCH
            if (weight > HB_LIQUID_SHARE_MIN)
            {
                float frequency = HB_BRUSH_TILES_PER_UNIT / max(strokes.y, HB_LIQUID_BRUSH_LEAST);

                HBBrushTap one = HB_LiquidBrushTap(restXZ - drift * first, restDx, restDy, frequency);
                HBBrushTap two = HB_LiquidBrushTap(restXZ - drift * second + HB_LIQUID_BRUSH_SKIP, restDx, restDy,
                                                   frequency);

                spine += lerp(two.spine, one.spine, share) * weight;
                coverage += lerp(two.coverage, one.coverage, share) * weight;
                total += weight;
            }
        }

        half scale = rcp(max(total, HB_LIQUID_SHARE_MIN));

        brush.spine = half3(spine.x, 0.0h, spine.y) * (scale * fade);
        brush.coverage = (coverage * scale * 2.0h - 1.0h) * fade * saturate(total * rcp(HB_LIQUID_SHARE_MIN));
    }

    return brush;
}

void HB_LiquidLight(Light light, half3 litNormalWS, half3 normalWS, half3 viewWS, BRDFData brdfData,
                    HBStyleData style, half shading, inout half3 reflection, inout half3 scatter, inout half3 foam)
{
    half3 reach = half3(light.color) * light.distanceAttenuation;

    reflection += HB_LightResponse(light, litNormalWS, normalWS, style, shading)
                * HB_DirectSpecular(brdfData, litNormalWS, half3(light.direction), viewWS, style.diffuseSoftness, 0.0h);
    scatter += reach;
    foam += reach * saturate(dot(normalWS, half3(light.direction)) * 0.5h + 0.5h);
}

#if defined(_ADDITIONAL_LIGHTS)
Light HB_LiquidExtraLight(uint index, float3 positionWS)
{
#if USE_CLUSTER_LIGHT_LOOP
    return GetAdditionalPerObjectLight(index, positionWS);
#else
    return GetAdditionalPerObjectLight(GetPerObjectLightIndex(index), positionWS);
#endif
}
#endif

void HB_LiquidLights(float3 positionWS, float2 screenUV, half3 litNormalWS, half3 normalWS, half3 viewWS,
                     BRDFData brdfData, HBStyleData style, half shading, inout half3 reflection,
                     inout half3 scatter, inout half3 foam)
{
#if defined(_ADDITIONAL_LIGHTS)
    InputData inputData = (InputData)0;
    inputData.positionWS = positionWS;
    inputData.normalizedScreenSpaceUV = screenUV;

    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = HB_LiquidExtraLight(lightIndex, positionWS);
        HB_LiquidLight(light, litNormalWS, normalWS, viewWS, brdfData, style, shading, reflection, scatter, foam);
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = HB_LiquidExtraLight(lightIndex, positionWS);
        HB_LiquidLight(light, litNormalWS, normalWS, viewWS, brdfData, style, shading, reflection, scatter, foam);
    LIGHT_LOOP_END
#endif
}

half HB_LiquidThrob(float4 mix)
{
    float beat = 0.0;
    float total = 0.0;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        float weight = mix[slot] * max(_HB_LiquidOptics[slot].w, 0.0);

        beat += sin(_Time.y * _HB_LiquidStyle[slot].z + HB_LIQUID_GRAIN * HB_LIQUID_THROB_GRAIN) * weight;
        total += weight;
    }

    return half(HB_LIQUID_THROB_BASE + HB_LIQUID_THROB_RANGE * beat * rcp(max(total, HB_EPSILON)));
}

HBStyleData HB_LiquidStyle(HBLiquidLook look)
{
    HBStyleData style = (HBStyleData)0;
    style.diffuseWrap = look.wrap;
    style.diffuseSoftness = look.softness;
    style.halfTone = HB_LIQUID_HALF_TONE;
    style.bakedSoftness = HB_LIQUID_BAKED_SOFTNESS;
    style.rimPower = 1.0h;
    return style;
}

half4 HB_LiquidFragment(HBLiquidVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_DEPTH || HB_DEBUG_VIEW == HB_DEBUG_LIQUID_FLOW)
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }
#endif

    float2 restXZ = input.rest.xy;
    HBWaterLevel water = HB_LiquidWaterAt(restXZ);

    UNITY_BRANCH
    if (!water.wet)
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    HB_LiquidCut(input.positionCS);

    HBLiquidWaveShape shape = (HBLiquidWaveShape)0;
    shape.amount = input.wave.y;
    shape.height = input.wave.z;
    shape.length = input.wave.w;
    shape.mix = float4(water.mix);

    float2 restDx = ddx(restXZ);
    float2 restDy = ddy(restXZ);
    float2 slope = float2(0.0, 0.0);

    HBLiquidSea sea = (HBLiquidSea)0;
    sea.wind = input.rest.z;
    sea.shore = input.rest.w;

    UNITY_BRANCH
    if (shape.amount > 0.0)
        slope = HB_WaveSlope(restXZ, restDx, restDy, shape, sea);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
    HBLiquidStream stream = HB_LiquidStreamAt(restXZ);
    stream.shear = input.shear;

    slope -= float2(HB_LiquidRipple(restXZ, restDx, restDy, water.mix, stream));

    half3 normalWS = HB_LiquidRipples(input.positionWS, half3(normalize(float3(-slope.x, 1.0, -slope.y))),
                                      water.mix);
    half3 viewWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_WAVES)
    {
        half rise = saturate(0.5h + half(input.wave.x) * HB_LIQUID_DEBUG_RANGE);
        half3 tone = lerp(half3(0.05h, 0.15h, 0.45h), half3(1.0h, 1.0h, 1.0h), rise);
        half facing = saturate(dot(normalWS, half3(_MainLightPosition.xyz)));

        return half4(tone * (HB_LIQUID_DEBUG_AMBIENT + (1.0h - HB_LIQUID_DEBUG_AMBIENT) * facing), 1.0h);
    }
#endif

    float surfaceEye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());
    float thickness = max(HB_LiquidBehind(surfaceEye, screenUV), 0.0);

    HBLiquidLook look = HB_LiquidLookAt(water.mix, thickness);
    HBLiquidFoam foam = HB_LiquidFoamAt(look, restXZ, restDx, restDy, thickness * abs(viewWS.y),
                                        distance(input.positionWS, GetCameraPositionWS()), input.foam, shape,
                                        stream);

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_FOAM)
    {
        half3 ground = half3(0.02h, 0.05h, 0.12h) + half3(0.0h, 0.25h, 0.3h) * foam.band;

        return half4(lerp(ground, half3(1.0h, 1.0h, 1.0h), foam.cover), 1.0h);
    }
#endif

    HBStyleData style = HB_LiquidStyle(look);
    HBBrushSample brush = HB_LiquidBrush(input.positionWS, restXZ, restDx, restDy, look.brush, water.mix);
    half3 litNormalWS = SafeNormalize(normalWS - brush.spine * HB_LIQUID_BRUSH_RELIEF);

    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    HBShadowProbe probe;

    Light mainLight = GetMainLight();
    mainLight.shadowAttenuation = HB_MainLightShadow(shadowCoord, input.positionWS, half4(1.0h, 1.0h, 1.0h, 1.0h),
                                                     screenUV, probe);

    half3 sunDirection = half3(_MainLightPosition.xyz);
    half3 sunLight = half3(mainLight.color) * mainLight.shadowAttenuation;

    half skyReach;
    HB_ResolveAmbient(normalWS, SampleSH(normalWS), 0.0h, 0.0h, HB_LIQUID_BAKED_SOFTNESS, skyReach);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.smoothness = look.smoothness;
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-viewWS, litNormalWS);
    half3 environment = HB_EnvironmentReflection(reflectVector, litNormalWS, input.positionWS,
                                                 brdfData.perceptualRoughness, screenUV, skyReach);
    half gloss = 1.0h - brdfData.perceptualRoughness;
    half fresnelTerm = Pow4(1.0h - saturate(dot(litNormalWS, viewWS))) * (gloss * gloss);

    half3 reflection = EnvironmentBRDFSpecular(brdfData, fresnelTerm) * environment;

    reflection += HB_LightResponse(mainLight, litNormalWS, normalWS, style, brush.coverage * HB_LIQUID_BRUSH_SHADING)
                * HB_DirectSpecular(brdfData, litNormalWS, sunDirection, viewWS, style.diffuseSoftness, 0.0h);
    half3 extraLight = half3(0.0h, 0.0h, 0.0h);
    half3 extraFoam = half3(0.0h, 0.0h, 0.0h);

    HB_LiquidLights(input.positionWS, screenUV, litNormalWS, normalWS, viewWS, brdfData, style,
                    brush.coverage * HB_LIQUID_BRUSH_SHADING, reflection, extraLight, extraFoam);

    reflection *= saturate(look.murk * HB_LIQUID_EDGE_SOFT);

    half3 skyLight = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                   * 0.5h;
    half throb = HB_LiquidThrob(water.mix);
    half3 lit = skyLight + (sunLight + extraLight) * HB_LIQUID_SUN_SCATTER
              + look.glow * (HB_LIQUID_GLOW_BASE + HB_LIQUID_GLOW_GRAIN * HB_LIQUID_GRAIN) * throb;
    half3 inscatter = look.tone * look.murk * lit;

    half fold = half(input.foam.z);
    half spread = lerp(HB_LIQUID_CREST_HARD, HB_LIQUID_CREST_SOFT, look.crestSoftness);
    half rise = max(min(HB_LIQUID_CREST_FROM, half(input.foam.w) * HB_LIQUID_CREST_EARLY), 1e-3h);
    half weave = (foam.grain - 0.5h) * HB_LIQUID_CREST_GRAIN;
    half crested = saturate((fold - rise + spread - weave) / (2.0h * spread)) * smoothstep(0.0h, rise, fold);
    half backlit = saturate(dot(viewWS, -sunDirection) * 0.5h + 0.5h);
    half crest = crested * lerp(HB_LIQUID_CREST_BASE, 1.0h, backlit * backlit)
               * saturate(1.0h - exp(-HB_LIQUID_CREST_DAWN * max(sunDirection.y, 0.0h)));

    inscatter += (sunLight + skyLight * HB_LIQUID_CREST_SKY) * look.crestTone * (crest * HB_LIQUID_CREST_LIGHT);

    UNITY_BRANCH
    if (look.caustics > 0.0h && look.murk < HB_LIQUID_CAUSTIC_MURK && thickness > 0.0
        && sunDirection.y > HB_LIQUID_CAUSTIC_SUN)
    {
        float3 floorWS = ComputeWorldSpacePosition(screenUV, SampleSceneDepth(screenUV), UNITY_MATRIX_I_VP);
        half daylight = lerp(HB_LIQUID_CAUSTIC_NIGHT, 1.0h,
                             half(saturate(_HB_SunDirection.y / HB_LIQUID_CAUSTIC_DUSK)));
        half caustics = HB_LiquidCaustics(floorWS, input.positionWS.y, sunDirection, shape.mix)
                      * (look.caustics * daylight);

        UNITY_BRANCH
        if (caustics > 0.0h)
        {
            half3 glint = lerp(half3(1.0h, 1.0h, 1.0h), look.tone, saturate(look.murk * HB_LIQUID_CAUSTIC_TINT));

            inscatter += half3(mainLight.color) * glint
                       * (caustics * HB_LiquidFloorShadow(floorWS) * (1.0h - look.murk));
        }
    }

    HBTransparentFog fog = HB_TransparentFog(input.positionWS);
    half3 foamLit = look.foamTone * (skyLight + sunLight * saturate(dot(normalWS, sunDirection) * 0.5h + 0.5h)
                                     + extraFoam);
    half3 colour = lerp(reflection + inscatter, foamLit, foam.cover) * fog.clear + fog.colour;

    half3 through = PositivePow(clamp(look.shore, 1e-3h, 1.0h),
                                half(min(thickness / HB_LIQUID_TINT_DEPTH, HB_LIQUID_TINT_MOST)))
                  * (1.0h - look.murk);
    half3 tint = lerp(half3(1.0h, 1.0h, 1.0h), through, fog.clear) * (1.0h - foam.cover);
    half blur = saturate(look.blur + look.murk * HB_LIQUID_MURK_BLUR) * fog.clear
              * saturate(look.murk * HB_LIQUID_EDGE_SOFT);

    UNITY_BRANCH
    if (max(tint.r, max(tint.g, tint.b)) > HB_LIQUID_SOLID)
        colour += HB_Refracted(screenUV, float2(0.0, 0.0), blur, 0.0h) * tint;

    return half4(colour, 1.0h);
}

float HB_LiquidCutFragment(HBLiquidVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HBWaterLevel water = HB_LiquidWaterAt(input.rest.xy);

    if (!water.wet)
        discard;

    return _HB_LiquidId;
}

float4 _HB_UnderMap;

float HB_LiquidMaskFragment(HBLiquidVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HBWaterLevel water = HB_LiquidWaterAt(input.rest.xy);

    if (!water.wet)
        discard;

    HB_LiquidCut(input.positionCS);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

    if (HB_LiquidBehind(LinearEyeDepth(input.positionWS, GetWorldToViewMatrix()), screenUV) < 0.0)
        discard;

    return max(distance(GetCameraPositionWS(), input.positionWS), HB_EPSILON);
}

HBLiquidVaryings HB_LiquidHeightVertex(HBLiquidAttributes input)
{
    HBLiquidVaryings output = HB_LiquidVertex(input);
    float2 local = (output.positionWS.xz - _HB_UnderMap.xy) * _HB_UnderMap.z;

#if UNITY_UV_STARTS_AT_TOP
    local.y = -local.y;
#endif

    output.positionCS = float4(local, 0.5, 1.0);
    return output;
}

float HB_LiquidHeightFragment(HBLiquidVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HBWaterLevel water = HB_LiquidWaterAt(input.rest.xy);

    if (!water.wet)
        discard;

    return input.positionWS.y;
}

#endif
