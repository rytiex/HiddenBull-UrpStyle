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
TEXTURE2D(_HB_LiquidPlates0);
TEXTURE2D(_HB_LiquidPlates1);
TEXTURE2D(_HB_LiquidPlates2);
TEXTURE2D(_HB_LiquidPlates3);
TEXTURE2D(_HB_UnderHeight);

float4 _HB_LiquidRipple[4];
float4 _HB_LiquidMolten[4];
float4 _HB_LiquidUnderFog[4];
float4 _HB_LiquidCrustTone[4];
float4 _HB_LiquidOptics[4];
float4 _HB_LiquidStyle[4];
float4 _HB_LiquidFoam[4];
float4 _HB_LiquidFoamShape[4];
float4 _HB_LiquidCrest[4];
float4 _HB_LiquidCausticGrid;
float4 _HB_LiquidBrush[4];
float4 _HB_LiquidCutState;
float _HB_LiquidKeep;
float _HB_LiquidId;
float _HB_LiquidEye;
float4 _HB_UnderMap;

float4 _HB_TerrainCamera;
float4 _HB_LiquidRanges[12];

#define HB_LIQUID_SLOTS          4
#define HB_LIQUID_SHARE_MIN      1e-3h
#define HB_LIQUID_GRADIENT_DEPTH 4.0
#define HB_LIQUID_GRADIENT_SIZE  64.0
#define HB_LIQUID_GRADIENT_END   1.02
#define HB_LIQUID_EDGE_SOFT      4.0h
#define HB_LIQUID_MURK_BLUR      0.35h
#define HB_LIQUID_SOLID          0.004h
#define HB_LIQUID_GLOW_BASE      0.75h
#define HB_LIQUID_GLOW_GRAIN     0.5h
#define HB_LIQUID_GLOW_HUE_LEAST 1e-3h
#define HB_LIQUID_GLOW_LIT_LEAST 0.1h
#define HB_LIQUID_GLOW_LIFT_MOST 8.0h
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
#define HB_LIQUID_STREAM_LEAST   1e-4
#define HB_LIQUID_STREAM_WHITE   6.0
#define HB_LIQUID_STREAM_BANK    1.5
#define HB_LIQUID_STREAM_SHEET   0.35h
#define HB_LIQUID_STREAM_SHEAR   0.6
#define HB_LIQUID_STREAM_STREAK  0.12
#define HB_LIQUID_STREAM_SPAN    1.0
#define HB_LIQUID_CELLS          4
#define HB_LIQUID_CELL_SIZE      2.0
#define HB_LIQUID_STREAM_STRETCH 0.35
#define HB_LIQUID_STREAM_RUSH    0.75h
#define HB_LIQUID_SWELL_SCALE    0.27
#define HB_LIQUID_SWELL_TURN     1.9
#define HB_LIQUID_SWELL_PACE     0.45
#define HB_LIQUID_SWELL_STRENGTH 0.8h
#define HB_LIQUID_ABSORB_LEAST   0.4h
#define HB_LIQUID_ABSORB_MOST    1.6h
#define HB_LIQUID_PASS_LEAST     1e-4
#define HB_LIQUID_LIGHT_PATH     0.5
#define HB_LIQUID_PATH_LEAST     0.05
#define HB_LIQUID_PATH_MOST      4.0
#define HB_LIQUID_RIPPLE_SPEED   0.025

#if defined(HB_LIQUID_POOL)
#define HB_LIQUID_PHASES UNITY_UNROLL
#else
#define HB_LIQUID_PHASES UNITY_LOOP
#endif

#define HB_LIQUID_SECOND_SCALE   1.37
#define HB_LIQUID_SECOND_PACE    0.71
#define HB_LIQUID_SECOND_TURN    0.62
#define HB_LIQUID_GLINT_SCALE    6.0
#define HB_LIQUID_GLINT_PACE     1.7
#define HB_LIQUID_GLINT_POWER    48.0h
#define HB_LIQUID_LAYER_ROUGH    0.5h
#define HB_LIQUID_LAYER_GRAIN    6.0
#define HB_LIQUID_LAYER_SECOND   0.53
#define HB_LIQUID_LAYER_DRIFT    0.012
#define HB_LIQUID_LAYER_BLUR     2.0
#define HB_LIQUID_LAYER_FLOOR    1e-3h
#define HB_LIQUID_LAYER_PURE     0.999h
#define HB_LIQUID_GLINT_FROM     0.72h
#define HB_LIQUID_GLINT_TO       0.85h
#define HB_LIQUID_GLINT_GAIN     4.0h
#define HB_LIQUID_CRUST_SOFT     0.3h
#define HB_LIQUID_CRUST_CALM     0.3h
#define HB_LIQUID_CRUST_PATCH    0.35h
#define HB_LIQUID_CRUST_TEXELS   256.0
#define HB_LIQUID_ANISO_LEAST    0.25
#define HB_LIQUID_FAR_LODS       6.0
#define HB_LIQUID_FAR_FLATTEN    0.5h
#define HB_LIQUID_FAR_SPREAD     0.4h
#define HB_LIQUID_FAR_MOST       0.06h
#define HB_LIQUID_THROB_CRUST    8.0h
#define HB_LIQUID_BUBBLE_CELLS   6.0
#define HB_LIQUID_BUBBLE_RADIUS  0.36
#define HB_LIQUID_BUBBLE_POP     0.82
#define HB_LIQUID_BUBBLE_RATE    0.25
#define HB_LIQUID_BUBBLE_PULSE   0.05
#define HB_LIQUID_BUBBLE_BEND    0.6h
#define HB_LIQUID_BUBBLE_DOME    0.3h
#define HB_LIQUID_BUBBLE_RING    0.9h
#define HB_LIQUID_BUBBLE_SHORE   2.0h
#define HB_LIQUID_BUBBLE_SHARE   4.0h
#define HB_LIQUID_BUBBLE_FLASH   2.0h
#define HB_LIQUID_DISTORTION     0.02
#define HB_LIQUID_BEND_DEEP      1.5
#define HB_LIQUID_BEND_NEAR      4.0
#define HB_LIQUID_CAUSTIC_FADE   0.25
#define HB_LIQUID_CAUSTIC_LEAST  0.5
#define HB_LIQUID_MAP_SAME       1e-3
#define HB_UNDER_EMPTY           -5000.0
#define HB_LIQUID_FAR_FROM       150.0
#define HB_LIQUID_FAR_RANGE      450.0
#define HB_LIQUID_UNDER_DEEP     64.0
#define HB_LIQUID_UNDER_WINDOW   0.66h
#define HB_LIQUID_UNDER_SOFT     0.06h
#define HB_LIQUID_UNDER_SMALLEST 0.97h
#define HB_LIQUID_UNDER_SHRINK   4.0
#define HB_LIQUID_UNDER_SHADE    0.35h
#define HB_LIQUID_UNDER_BELOW    0.55h
#define HB_LIQUID_UNDER_TILT     0.5h
#define HB_LIQUID_UNDER_EDGE     0.4h
#define HB_LIQUID_FLAT_LEAST     0.1h
#define HB_LIQUID_DEEP_REACH     1.0

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
    half spots;
    half band;
    half grain;
    half flash;
    half2 bend;
};

struct HBLiquidSpots
{
    half spots;
    half veil;
    half band;
    half flash;
    half2 bend;
};

half4 HB_LiquidMixed(float4 values[4], float4 mix)
{
    return half4(values[0] * mix.x + values[1] * mix.y + values[2] * mix.z + values[3] * mix.w);
}

float4 HB_LiquidHash4(float2 id)
{
    float4 p = frac(id.xyxy * float4(0.1031, 0.1030, 0.0973, 0.1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

float2 HB_LiquidWind()
{
    return HB_WAVE_BOUND ? _HB_WaveFrame.xy : float2(0.0, 1.0);
}

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
    water.fade = 1.0h;
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

float2 HB_LiquidGradientUV(float at, int slot)
{
    float seen = min(at, _HB_LiquidMolten[slot].w);

    return float2((seen * (HB_LIQUID_GRADIENT_SIZE - 1.0) + 0.5) / HB_LIQUID_GRADIENT_SIZE,
                  (slot + 0.5) / HB_LIQUID_SLOTS);
}

HBLiquidLook HB_LiquidLookAt(half4 mix, float path)
{
    HBLiquidLook look = (HBLiquidLook)0;

    float at = saturate((1.0 - exp(-max(path, 0.0) / HB_LIQUID_GRADIENT_DEPTH)) * HB_LIQUID_GRADIENT_END);
    half total = 0.0h;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        half share = mix[slot];

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float2 uv = HB_LiquidGradientUV(at, slot);
            half4 depth = half4(SAMPLE_TEXTURE2D_LOD(_HB_LiquidGradient, sampler_LinearClamp, uv, 0));
            half4 optics = half4(_HB_LiquidOptics[slot]);
            half4 style = half4(_HB_LiquidStyle[slot]);
            half4 foam = half4(_HB_LiquidFoam[slot]);
            float4 foamShape = _HB_LiquidFoamShape[slot];
            half4 crest = half4(_HB_LiquidCrest[slot]);
            half4 strokes = half4(_HB_LiquidBrush[slot]);

            look.tone += depth.rgb * share;
            look.murk += depth.a * share;
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

half4 HB_LiquidShadeAt(float4 mix, float path)
{
    float at = saturate((1.0 - exp(-max(path, 0.0) / HB_LIQUID_GRADIENT_DEPTH)) * HB_LIQUID_GRADIENT_END);
    half4 shade = half4(0.0h, 0.0h, 0.0h, 0.0h);
    half total = 0.0h;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        half share = half(mix[slot]);

        UNITY_BRANCH
        if (share > HB_LIQUID_SHARE_MIN)
        {
            float2 uv = HB_LiquidGradientUV(at, slot);

            shade += half4(SAMPLE_TEXTURE2D_LOD(_HB_LiquidGradient, sampler_LinearClamp, uv, 0)) * share;
            total += share;
        }
    }

    return shade * rcp(max(total, HB_LIQUID_SHARE_MIN));
}

half3 HB_LiquidToneAt(float4 mix, float path)
{
    return HB_LiquidShadeAt(mix, path).rgb;
}

half3 HB_LiquidPassed(half murk, half3 tone, float longer)
{
    half3 hue = tone * rcp(max(max(tone.r, max(tone.g, tone.b)), HB_LIQUID_GLOW_HUE_LEAST));
    half3 weight = lerp(HB_LIQUID_ABSORB_MOST, HB_LIQUID_ABSORB_LEAST, hue);

    weight *= rcp(max(dot(weight, half3(1.0h, 1.0h, 1.0h)) / 3.0h, HB_LIQUID_ABSORB_LEAST));
    return half3(exp2(log2(max(1.0 - float(murk), HB_LIQUID_PASS_LEAST)) * (float3(weight) * longer)));
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
    float4 low;
    float4 high;
    float4 scroll;
    float4 stretch;
    half4 weights;
    float2 flow;
    float speed;
    float shear;
    int cells;
    bool moving;
};

HBLiquidStream HB_LiquidStreamAt(float2 restXZ, float shear)
{
    HBLiquidStream stream = (HBLiquidStream)0;

    stream.low = float4(0.0, 1.0, 0.0, 1.0);
    stream.high = float4(0.0, 1.0, 0.0, 1.0);
    stream.stretch = float4(1.0, 1.0, 1.0, 1.0);
    stream.weights = half4(1.0h, 0.0h, 0.0h, 0.0h);
    stream.shear = shear;
    stream.cells = 1;

#if !defined(HB_LIQUID_POOL)
    float2 at = (restXZ - _HB_TerrainOrigin.xz) / HB_LIQUID_CELL_SIZE - 0.5;
    float2 base = floor(at);
    float2 blend = at - base;
    float4 heads[HB_LIQUID_CELLS / 2];
    float4 stretch;
    float2 flow = float2(0.0, 0.0);

    UNITY_UNROLL
    for (int cell = 0; cell < HB_LIQUID_CELLS; cell++)
    {
        float2 corner = float2(cell & 1, cell >> 1);
        float2 uv = HB_LiquidUV(_HB_TerrainOrigin.xz + (base + corner + 0.5) * HB_LIQUID_CELL_SIZE);
        float2 drift = HB_WaterInside(uv) ? SAMPLE_TEXTURE2D_LOD(_HB_WaterFlow, sampler_LinearClamp, uv, 0).xy
                                          : float2(0.0, 0.0);
        float2 lean = abs(corner - 1.0 + blend);
        float pace = length(drift);
        float2 head = pace > HB_LIQUID_STREAM_LEAST ? drift / pace : float2(0.0, 1.0);

        if ((cell & 1) == 0)
            heads[cell >> 1].xy = head;
        else
            heads[cell >> 1].zw = head;

        stream.scroll[cell] = pace * _Time.y;
        stretch[cell] = rcp(1.0 + pace * HB_LIQUID_STREAM_STRETCH);
        stream.weights[cell] = half(lean.x * lean.y);
        flow += drift * (lean.x * lean.y);
    }

    stream.flow = flow;
    stream.speed = length(flow);
    stream.moving = dot(flow, flow) > HB_LIQUID_STREAM_LEAST;

    UNITY_BRANCH
    if (stream.moving)
    {
        stream.low = heads[0];
        stream.high = heads[1];
        stream.stretch = stretch;
        stream.cells = HB_LIQUID_CELLS;
    }
    else
    {
        stream.scroll = float4(0.0, 0.0, 0.0, 0.0);
        stream.weights = half4(1.0h, 0.0h, 0.0h, 0.0h);
    }
#endif

    return stream;
}

float4 HB_LiquidCell(HBLiquidStream stream, int cell, out half weight)
{
    float4 pick = float4(cell == 0, cell == 1, cell == 2, cell == 3);
    float4 pair = cell < 2 ? stream.low : stream.high;

    weight = half(dot(float4(stream.weights), pick));
    return float4((cell & 1) == 0 ? pair.xy : pair.zw, dot(stream.scroll, pick), dot(stream.stretch, pick));
}

float2 HB_LiquidAlong(float4 heading, float2 value)
{
    return float2(heading.y * value.x - heading.x * value.y,
                  (heading.x * value.x + heading.y * value.y) * heading.w);
}

float2 HB_LiquidBack(float4 heading, float2 value)
{
    float along = value.y * heading.w;

    return float2(heading.y * value.x + heading.x * along, heading.y * along - heading.x * value.x);
}

float2 HB_LiquidCarried(float4 heading, float2 restXZ)
{
    float2 xz = restXZ - _HB_TerrainOrigin.xz;

    return _HB_TerrainOrigin.xz + float2(heading.y * xz.x - heading.x * xz.y,
                                         (heading.x * xz.x + heading.y * xz.y - heading.z) * heading.w);
}

half HB_LiquidFoamRead(float2 xz, float2 dx, float2 dy, float scale, float turn, float pace)
{
    float2 uv = HB_LiquidTurn(xz, turn) * scale + frac(HB_LiquidWind() * (HB_LIQUID_FOAM_SPEED * pace * _Time.y));

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

half4 HB_LiquidBubbles(float2 xz, half amount, float density, float time)
{
    half4 bubbles = half4(0.0h, 0.0h, 0.0h, 0.0h);
    float2 p = xz * density;
    float2 cell = floor(p);

    UNITY_LOOP
    for (int y = -1; y <= 1; y++)
    {
        UNITY_LOOP
        for (int x = -1; x <= 1; x++)
        {
            float2 id = cell + float2(x, y);
            float4 h = HB_LiquidHash4(id);

            if (h.w >= amount)
                continue;

            float2 centre = id + 0.5 + (h.xy - 0.5) * 0.5;
            float radius = HB_LIQUID_BUBBLE_RADIUS * (0.6 + 0.4 * h.z);
            float age = frac(time * (0.6 + 0.8 * h.z) + h.x * 3.1 + h.y * 1.7);
            float2 offset = p - centre;
            float distance = length(offset);

            if (age < HB_LIQUID_BUBBLE_POP)
            {
                float growth = smoothstep(0.0, 1.0, age / HB_LIQUID_BUBBLE_POP);
                float size = radius * growth;
                float dome = saturate(1.0 - (distance * distance) / max(size * size, 1e-4)) * growth;

                bubbles.x = max(bubbles.x, half(dome));
                bubbles.zw += half2(offset / max(size, 1e-3) * saturate(dome * 4.0));
            }
            else
            {
                float t = (age - HB_LIQUID_BUBBLE_POP) / (1.0 - HB_LIQUID_BUBBLE_POP);
                float ringRadius = radius * (1.0 + 0.8 * t);
                float width = radius * 0.12;
                float ring = exp(-(distance - ringRadius) * (distance - ringRadius) / (width * width)) * (1.0 - t);

                bubbles.y = max(bubbles.y, half(ring));
            }
        }
    }

    return bubbles;
}

HBLiquidSpots HB_LiquidSpotsAt(HBLiquidLook look, float2 restXZ, float2 restDx, float2 restDy, float4 mix,
                               half grain, half strength, half edge, half caps, float depth, float swell,
                               bool streaked, float2 reach, half bubbleAmount, half bubbleShare, float bubbleDensity,
                               float bubbleTime)
{
    HBLiquidSpots spots = (HBLiquidSpots)0;

    UNITY_BRANCH
    if (strength > 0.0h)
    {
        UNITY_BRANCH
        if (streaked)
        {
            grain = (HB_LiquidFoamGrain(restXZ - reach, restDx, restDy, mix)
                     + HB_LiquidFoamGrain(restXZ, restDx, restDy, mix)
                     + HB_LiquidFoamGrain(restXZ + reach, restDx, restDy, mix)) * (1.0h / 3.0h);
        }

        half shore = HB_LiquidShoreFoam(depth, grain, look.shoreFoam, swell, spots.band);

        spots.veil = HB_LiquidFoamCover(edge * edge * strength, grain) * HB_LIQUID_FOAM_VEIL;
        spots.spots = HB_LiquidFoamCover(saturate(shore + caps) * strength, grain);
    }

    UNITY_BRANCH
    if (bubbleShare > 0.0h)
    {
        half4 bubbles = HB_LiquidBubbles(restXZ - _HB_TerrainOrigin.xz, bubbleAmount, bubbleDensity, bubbleTime)
                      * bubbleShare;

        spots.spots = saturate(spots.spots + bubbles.x * HB_LIQUID_BUBBLE_DOME + bubbles.y * HB_LIQUID_BUBBLE_RING);
        spots.flash = bubbles.y;
        spots.bend = bubbles.zw;
    }

    return spots;
}

HBLiquidFoam HB_LiquidFoamAt(HBLiquidLook look, float2 restXZ, float2 restDx, float2 restDy, float deep,
                             float thickness, float away, float4 carried, HBLiquidWaveShape shape,
                             HBLiquidStream stream)
{
    HBLiquidFoam foam = (HBLiquidFoam)0;
    foam.grain = HB_LiquidFoamGrain(restXZ, restDx, restDy, shape.mix);

    half feather = half(saturate(deep / HB_LIQUID_FOAM_FEATHER));
    half close = half(1.0 - saturate((away - HB_LIQUID_FOAM_FROM) / HB_LIQUID_FOAM_RANGE));
    half strength = feather * close * look.foamAmount;
    half edge = half(1.0 - saturate(deep / max(look.waterline, 1e-3)));
    float swell = saturate(shape.amount * shape.height * HB_LIQUID_SHORE_SWELL);
    half rush = half(saturate(stream.speed / HB_LIQUID_STREAM_WHITE));
    half bank = half(1.0 - saturate(deep / HB_LIQUID_STREAM_BANK));
    half churn = saturate(half(stream.shear * HB_LIQUID_STREAM_SHEAR) + rush * bank
                          + rush * rush * HB_LIQUID_STREAM_SHEET);
    half caps = max(half(carried.x), churn);
    float2 reach = stream.flow * HB_LIQUID_STREAM_STREAK;

    half4 molten = HB_LiquidMixed(_HB_LiquidMolten, shape.mix);
    half4 bubbling = half4(step(1e-4, float4(_HB_LiquidMolten[0].y, _HB_LiquidMolten[1].y, _HB_LiquidMolten[2].y,
                                             _HB_LiquidMolten[3].y)));
    half bubbleShare = saturate(dot(half4(shape.mix), bubbling) * HB_LIQUID_BUBBLE_SHARE);
    half bubbleEdge = half(1.0 - saturate(thickness / max(look.waterline, 1e-3)));
    half bubbleAmount = saturate(molten.y * (1.0h + HB_LIQUID_BUBBLE_SHORE * bubbleEdge));
    float bubbleDensity = float(HB_LiquidMixed(_HB_LiquidFoamShape, shape.mix).z) * HB_LIQUID_BUBBLE_CELLS;
    float bubbleTime = _Time.y * max(float(HB_LiquidMixed(_HB_LiquidStyle, shape.mix).z), HB_LIQUID_BUBBLE_PULSE)
                     * HB_LIQUID_BUBBLE_RATE;

    bubbleShare = molten.y > 0.0h ? bubbleShare : 0.0h;

    half veil = 0.0h;

    HB_LIQUID_PHASES
    for (int cell = 0; cell < stream.cells; cell++)
    {
        half weight;
        float4 heading = HB_LiquidCell(stream, cell, weight);
        HBLiquidSpots spots = HB_LiquidSpotsAt(look, HB_LiquidCarried(heading, restXZ),
                                               HB_LiquidAlong(heading, restDx), HB_LiquidAlong(heading, restDy),
                                               shape.mix, foam.grain, strength, edge, caps, carried.y, swell,
                                               stream.moving, HB_LiquidAlong(heading, reach), bubbleAmount,
                                               bubbleShare, bubbleDensity, bubbleTime);

        foam.spots += spots.spots * weight;
        foam.flash += spots.flash * weight;
        foam.bend += half2(HB_LiquidBack(heading, spots.bend)) * weight;
        foam.band = cell == 0 ? spots.band : foam.band;
        veil += spots.veil * weight;
    }

    half solid = foam.spots * foam.spots;

    foam.cover = min(solid + (1.0h - solid) * min(foam.spots - solid + veil, HB_LIQUID_FOAM_GLOW),
                     HB_LIQUID_FOAM_MOST);
    return foam;
}

half HB_LiquidPatternRead(float2 xz, float2 dx, float2 dy, float scale, float pace)
{
    half coverage = 0.5h;

    UNITY_BRANCH
    if (HB_BRUSH_ATLAS_BOUND > 0.5)
    {
        float2 uv = HB_LiquidTurn(xz, HB_LIQUID_FOAM_TURN) * scale
                  + frac(HB_LiquidWind() * (HB_LIQUID_RIPPLE_SPEED * pace * _Time.y));

        coverage = HB_BrushRead(uv, HB_LiquidTurn(dx, HB_LIQUID_FOAM_TURN) * scale,
                                HB_LiquidTurn(dy, HB_LIQUID_FOAM_TURN) * scale).coverage;
    }

    return coverage;
}

half HB_LiquidGlints(float2 restXZ, float2 dx, float2 dy, float pattern, half3 normalWS, half3 viewWS,
                     half3 sunDirection)
{
    half alignment = saturate(dot(reflect(-viewWS, normalWS), sunDirection));
    half glint = 0.0h;

    UNITY_BRANCH
    if (alignment > 0.0h)
    {
        half sparkle = HB_LiquidPatternRead(restXZ - _HB_TerrainOrigin.xz, dx, dy, pattern * HB_LIQUID_GLINT_SCALE,
                                            HB_LIQUID_GLINT_PACE);

        glint = PositivePow(alignment, HB_LIQUID_GLINT_POWER)
              * smoothstep(HB_LIQUID_GLINT_FROM, HB_LIQUID_GLINT_TO, sparkle) * HB_LIQUID_GLINT_GAIN;
    }

    return glint;
}

float2 HB_LiquidBend(float surfaceEye, float thickness, float2 screenUV, half3 normalWS, half strength)
{
    float2 bend = float2(0.0, 0.0);
    float reach = saturate(min(thickness, HB_LIQUID_BEND_DEEP) * HB_LIQUID_BEND_NEAR / max(surfaceEye, HB_EPSILON));

    UNITY_BRANCH
    if (strength > 0.0h && reach > 0.0)
    {
        float2 tilt = mul((float3x3)UNITY_MATRIX_V, float3(normalWS)).xy;
        float2 target = screenUV + tilt * (float(strength) * HB_LIQUID_DISTORTION * reach);
        float2 border = min(target, 1.0 - target);

        bend = (target - screenUV) * saturate(min(border.x, border.y) / HB_REFRACTION_BORDER);

        if (HB_LiquidBehind(surfaceEye, screenUV + bend) <= 0.0)
            bend = float2(0.0, 0.0);
    }

    return bend;
}

float HB_UnderSurfaceAt(float2 xz, out bool known)
{
    known = false;

    uint width;
    uint height;
    _HB_UnderHeight.GetDimensions(width, height);

    float2 at = ((xz - _HB_UnderMap.xy) * (_HB_UnderMap.z * 0.5) + 0.5) * float2(width, height) - 0.5;
    float2 base = floor(at);
    float2 blend = at - base;
    int2 low = int2(base);
    int2 last = int2(width, height) - 1;

    if (any(low < 0) || any(low + 1 > last))
        return HB_UNDER_EMPTY;

    float a = LOAD_TEXTURE2D_LOD(_HB_UnderHeight, low, 0).r;
    float b = LOAD_TEXTURE2D_LOD(_HB_UnderHeight, low + int2(1, 0), 0).r;
    float c = LOAD_TEXTURE2D_LOD(_HB_UnderHeight, low + int2(0, 1), 0).r;
    float d = LOAD_TEXTURE2D_LOD(_HB_UnderHeight, low + int2(1, 1), 0).r;

    known = min(min(a, b), min(c, d)) > HB_UNDER_EMPTY;
    return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
}

float HB_LiquidEyeLevel(float2 xz)
{
    float level = _HB_LiquidEye;
    float2 apart = abs(_HB_UnderMap.xy - GetCameraPositionWS().xz);

    UNITY_BRANCH
    if (_HB_UnderMap.z > 0.0 && max(apart.x, apart.y) < HB_LIQUID_MAP_SAME)
    {
        bool known;
        float surface = HB_UnderSurfaceAt(xz, known);

        level = known ? surface : level;
    }

    return level;
}

float HB_LiquidReach(float path, float3 positionWS, float depth)
{
    float away = distance(positionWS, GetCameraPositionWS());
    float remote = saturate((away - HB_LIQUID_FAR_FROM) / HB_LIQUID_FAR_RANGE);

    return remote > 0.0 ? lerp(path, max(path, depth), remote) : path;
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

            bright += max(focus - still, 0.0) * share
                    * exp(-depth / max(_HB_LiquidUnderFog[slot].y * HB_LIQUID_CAUSTIC_FADE, HB_LIQUID_CAUSTIC_LEAST));
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

float2 HB_LiquidDrift(float2 restXZ, float pace)
{
    float2 wind = (HB_WAVE_BOUND ? _HB_WaveFrame.xy : float2(0.0, 1.0)) * (HB_WAVE_WIND * HB_LIQUID_BRUSH_WIND * pace);

#if defined(HB_LIQUID_POOL)
    return wind;
#else
    float2 uv = HB_LiquidUV(restXZ);
    float2 flow = HB_WaterInside(uv) ? HB_WaterFlow(uv).xy * HB_LIQUID_BRUSH_FLOW : float2(0.0, 0.0);

    return dot(flow, flow) > dot(wind, wind) ? flow : wind;
#endif
}

half3 HB_LiquidScaled(half3 raw, half strength)
{
    half2 tilt = raw.xy * strength;

    return half3(tilt, sqrt(saturate(1.0h - dot(tilt, tilt))));
}

half3 HB_LiquidRippleRead(TEXTURE2D_PARAM(map, samplerMap), TEXTURE2D_PARAM(plates, samplerPlates), float2 xz,
                          float2 dx, float2 dy, float tiling, half strength, half calm, float pace, out half plate)
{
    float2 drift = HB_LiquidWind() * (HB_LIQUID_RIPPLE_SPEED * _Time.y * pace);
    float wide = tiling * HB_LIQUID_SECOND_SCALE;
    float2 first = xz * tiling + frac(drift);
    float2 second = HB_LiquidTurn(xz, HB_LIQUID_SECOND_TURN) * wide + frac(drift * HB_LIQUID_SECOND_PACE);

    half3 rawA = UnpackNormal(SAMPLE_TEXTURE2D_GRAD(map, samplerMap, first, dx * tiling, dy * tiling));
    half3 rawB = UnpackNormal(SAMPLE_TEXTURE2D_GRAD(map, samplerMap, second,
                                                    HB_LiquidTurn(dx, HB_LIQUID_SECOND_TURN) * wide,
                                                    HB_LiquidTurn(dy, HB_LIQUID_SECOND_TURN) * wide));
    float big = tiling * HB_LIQUID_SWELL_SCALE;
    float2 third = HB_LiquidTurn(xz, HB_LIQUID_SWELL_TURN) * big + frac(drift * HB_LIQUID_SWELL_PACE);
    half3 rawC = UnpackNormal(SAMPLE_TEXTURE2D_GRAD(map, samplerMap, third,
                                                    HB_LiquidTurn(dx, HB_LIQUID_SWELL_TURN) * big,
                                                    HB_LiquidTurn(dy, HB_LIQUID_SWELL_TURN) * big));
    half3 a = HB_LiquidScaled(rawA, strength);
    half3 b = HB_LiquidScaled(rawB, strength * calm);
    half3 c = HB_LiquidScaled(rawC, strength * (HB_LIQUID_SWELL_STRENGTH * calm));

    plate = 0.0h;

    UNITY_BRANCH
    if (calm < 1.0h)
    {
        half2 shape = half2(SAMPLE_TEXTURE2D_GRAD(plates, samplerPlates, first, dx * tiling, dy * tiling).rg);

        plate = shape.r * lerp(1.0h - HB_LIQUID_CRUST_PATCH, 1.0h + HB_LIQUID_CRUST_PATCH, shape.g);
    }

    return normalize(half3(a.xy + b.xy + c.xy, a.z * b.z * c.z));
}

half HB_LiquidFarOf(float even, half4 ripple)
{
    float texels = even * ripple.x * HB_LIQUID_SECOND_SCALE * HB_LIQUID_CRUST_TEXELS;

    return half(saturate(log2(max(texels, 1.0)) / HB_LIQUID_FAR_LODS));
}

half HB_LiquidFarSmooth(half smoothness, half far, half strength)
{
    half rough = 1.0h - smoothness;
    half spread = min(far * strength * strength * HB_LIQUID_FAR_SPREAD, HB_LIQUID_FAR_MOST);

    return 1.0h - sqrt(sqrt(rough * rough * rough * rough + spread));
}

half3 HB_LiquidUnderCalm(half3 normalWS, half most)
{
    half2 tilt = normalWS.xz;

    tilt *= rcp(1.0h + length(tilt) / most);
    return half3(tilt.x, -sqrt(saturate(1.0h - dot(tilt, tilt))), tilt.y);
}

half HB_LiquidCrustOf(half plate, half amount)
{
    half threshold = 1.0h - amount;

    return smoothstep(threshold - HB_LIQUID_CRUST_SOFT, threshold + HB_LIQUID_CRUST_SOFT, plate)
         * saturate(amount / HB_LIQUID_CRUST_SOFT);
}

#define HB_LIQUID_RIPPLE_SLOT(index)                                                                                 \
    UNITY_BRANCH                                                                                                     \
    if (live[index])                                                                                                 \
    {                                                                                                                \
        half flat;                                                                                                   \
        half3 read = HB_LiquidRippleRead(TEXTURE2D_ARGS(_HB_LiquidNormal##index, sampler_trilinear_repeat),          \
                                         TEXTURE2D_ARGS(_HB_LiquidPlates##index, sampler_trilinear_repeat), along,   \
                                         alongDx, alongDy, _HB_LiquidRipple[index].x,                                \
                                         half(_HB_LiquidRipple[index].y) * rush, calm[index],                        \
                                         _HB_LiquidRipple[index].w, flat);                                           \
        sums[index] += half3(HB_LiquidBack(heading, read.xy), read.z) * weight;                                      \
        plates[index] += flat * weight;                                                                              \
    }

half3 HB_LiquidRipple(float2 restXZ, float2 dx, float2 dy, float4 mix, HBLiquidStream stream, out half crust,
                      out float even)
{
    float wideX = length(dx);
    float wideY = length(dy);

    even = max(sqrt(wideX * wideY), max(wideX, wideY) * HB_LIQUID_ANISO_LEAST);

    dx *= even / max(wideX, HB_EPSILON);
    dy *= even / max(wideY, HB_EPSILON);

    half3 ripple = half3(0.0h, 0.0h, 1.0h);
    half rush = 1.0h + half(saturate(stream.speed / HB_LIQUID_STREAM_WHITE)) * HB_LIQUID_STREAM_RUSH;
    bool live[HB_LIQUID_SLOTS];
    half calm[HB_LIQUID_SLOTS];
    half3 sums[HB_LIQUID_SLOTS];
    half plates[HB_LIQUID_SLOTS];

    crust = 0.0h;

    UNITY_UNROLL
    for (int slot = 0; slot < HB_LIQUID_SLOTS; slot++)
    {
        live[slot] = half(mix[slot]) > HB_LIQUID_SHARE_MIN
                  && (_HB_LiquidRipple[slot].y > 0.0 || _HB_LiquidMolten[slot].x > 0.0);
        calm[slot] = _HB_LiquidMolten[slot].x > 0.0 ? HB_LIQUID_CRUST_CALM : 1.0h;
        sums[slot] = half3(0.0h, 0.0h, 0.0h);
        plates[slot] = 0.0h;
    }

    HB_LIQUID_PHASES
    for (int cell = 0; cell < stream.cells; cell++)
    {
        half weight;
        float4 heading = HB_LiquidCell(stream, cell, weight);
        float2 along = HB_LiquidCarried(heading, restXZ) - _HB_TerrainOrigin.xz;
        float2 alongDx = HB_LiquidAlong(heading, dx);
        float2 alongDy = HB_LiquidAlong(heading, dy);

        HB_LIQUID_RIPPLE_SLOT(0)
        HB_LIQUID_RIPPLE_SLOT(1)
        HB_LIQUID_RIPPLE_SLOT(2)
        HB_LIQUID_RIPPLE_SLOT(3)
    }

    UNITY_UNROLL
    for (int layer = 0; layer < HB_LIQUID_SLOTS; layer++)
    {
        if (live[layer])
        {
            ripple += (normalize(sums[layer]) - half3(0.0h, 0.0h, 1.0h)) * half(mix[layer]);
            crust += HB_LiquidCrustOf(plates[layer], half(_HB_LiquidMolten[layer].x)) * half(mix[layer]);
        }
    }

    return normalize(half3(ripple.xy, max(ripple.z, HB_LIQUID_FLAT_LEAST)));
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
        float2 drift = HB_LiquidDrift(restXZ, HB_LiquidMixed(_HB_LiquidRipple, mix).w) * HB_LIQUID_BRUSH_CYCLE;
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

half HB_LiquidThrob(float4 mix, half grain)
{
    float pulse = HB_LiquidMixed(_HB_LiquidStyle, mix).z;
    half crust = HB_LiquidMixed(_HB_LiquidMolten, mix).x;
    half throb = half(HB_LIQUID_THROB_BASE + HB_LIQUID_THROB_RANGE * sin(_Time.y * pulse
                                                                         + float(grain) * HB_LIQUID_THROB_GRAIN));

    return lerp(1.0h, throb, saturate(crust * HB_LIQUID_THROB_CRUST));
}

half3 HB_LiquidGlowLift(half3 tone, half glow, half3 lit)
{
    half3 hue = tone * rcp(max(max(tone.r, max(tone.g, tone.b)), HB_LIQUID_GLOW_HUE_LEAST));

    return min(1.0h + hue * (glow * rcp(max(Luminance(lit), HB_LIQUID_GLOW_LIT_LEAST))), HB_LIQUID_GLOW_LIFT_MOST);
}

float HB_LiquidDeepBelow(float3 surfaceWS, float2 screenUV, float settled)
{
    float raw = SampleSceneDepth(screenUV);

#if UNITY_REVERSED_Z
    bool solid = raw > 0.0;
#else
    bool solid = raw < 1.0;
#endif

    float deep = settled;

    UNITY_BRANCH
    if (solid)
    {
        float3 floorWS = ComputeWorldSpacePosition(screenUV, raw, UNITY_MATRIX_I_VP);
        float near = saturate(1.0 - distance(surfaceWS.xz, floorWS.xz) / HB_LIQUID_DEEP_REACH);

        deep = lerp(settled, clamp(surfaceWS.y - floorWS.y, 0.0, settled), near);
    }

    return deep;
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

half4 HB_LiquidLayerBlend(half4 mix, float2 restXZ, float2 restDx, float2 restDy)
{
    half4 blended = mix;

    UNITY_BRANCH
    if (max(max(mix.x, mix.y), max(mix.z, mix.w)) <= HB_LIQUID_LAYER_PURE)
    {
        float scale = 1.0 / HB_LIQUID_LAYER_GRAIN;
        float2 uv = (restXZ - _HB_TerrainOrigin.xz) * scale;
        float2 drift = HB_LiquidWind() * (_Time.y * HB_LIQUID_LAYER_DRIFT);
        float2 dx = restDx * (scale * HB_LIQUID_LAYER_BLUR);
        float2 dy = restDy * (scale * HB_LIQUID_LAYER_BLUR);
        half coarse = half(SAMPLE_TEXTURE2D_GRAD(_HB_LiquidFoamPattern, sampler_trilinear_repeat, uv + frac(drift), dx,
                                                 dy).r);
        half fine = half(SAMPLE_TEXTURE2D_GRAD(_HB_LiquidFoamPattern, sampler_trilinear_repeat,
                                               HB_LiquidTurn(uv, 1.0) * HB_LIQUID_LAYER_SECOND - frac(drift * 0.7), dx,
                                               dy).r);
        half rough = (coarse + fine) * 0.5h * HB_LIQUID_LAYER_ROUGH;
        half4 weights = 1.0h - smoothstep(rough, rough + 1.0h - HB_LIQUID_LAYER_ROUGH, 1.0h - mix)
                      + mix * HB_LIQUID_LAYER_FLOOR;

        blended = weights / dot(weights, half4(1.0h, 1.0h, 1.0h, 1.0h));
    }

    return blended;
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

    float3 nearWS = ComputeWorldSpacePosition(GetNormalizedScreenSpaceUV(input.positionCS), UNITY_NEAR_CLIP_VALUE,
                                              UNITY_MATRIX_I_VP);
    bool front = nearWS.y >= HB_LiquidEyeLevel(nearWS.xz);

    HBLiquidWaveShape shape = (HBLiquidWaveShape)0;
    shape.amount = input.wave.y;
    shape.height = input.wave.z;
    shape.length = input.wave.w;
    shape.mix = float4(water.mix);

    float2 restDx = ddx(restXZ);
    float2 restDy = ddy(restXZ);

#if !defined(HB_LIQUID_POOL)
    water.mix = HB_LiquidLayerBlend(water.mix, restXZ, restDx, restDy);
    shape.mix = float4(water.mix);
#endif

    float2 slope = float2(0.0, 0.0);

    HBLiquidSea sea = (HBLiquidSea)0;
    sea.wind = input.rest.z;
    sea.shore = input.rest.w;

    UNITY_BRANCH
    if (shape.amount > 0.0)
        slope = HB_WaveSlope(restXZ, restDx, restDy, shape, sea);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
    HBLiquidStream stream = HB_LiquidStreamAt(restXZ, input.shear);

    half crust;
    float even;
    half3 ripple = HB_LiquidRipple(restXZ, restDx, restDy, water.mix, stream, crust, even);
    half4 rippleSet = HB_LiquidMixed(_HB_LiquidRipple, water.mix);
    half far = HB_LiquidFarOf(even, rippleSet);

    ripple.xy *= 1.0h - far * HB_LIQUID_FAR_FLATTEN;
    half3 normalWS = HB_LiquidRipples(input.positionWS, normalize(half3(ripple.x - half(slope.x), ripple.z,
                                                                        ripple.y - half(slope.y))), water.mix);
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
    float deep = front ? HB_LiquidDeepBelow(input.positionWS, screenUV, input.foam.y) : input.foam.y;
    float path = front ? HB_LiquidReach(thickness, input.positionWS, input.foam.y) : 0.0;

    HBLiquidLook look = HB_LiquidLookAt(water.mix, path);

    look.murk = lerp(look.murk, 1.0h, crust);
    HBLiquidFoam foam = HB_LiquidFoamAt(look, restXZ, restDx, restDy, deep, thickness,
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

    normalWS = NormalizeNormalPerPixel(normalWS + half3(foam.bend.x, 0.0h, foam.bend.y) * HB_LIQUID_BUBBLE_BEND)
             * (front ? 1.0h : -1.0h);

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

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.smoothness = HB_LiquidFarSmooth(look.smoothness, far, rippleSet.y);
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-viewWS, litNormalWS);
    half probeWeight;
    half3 probes = HB_ProbeReflection(reflectVector, input.positionWS,
                                      PerceptualRoughnessToMipmapLevel(brdfData.perceptualRoughness), screenUV,
                                      probeWeight);
    half3 sky = HB_EnvironmentSky(reflectVector, litNormalWS, brdfData.perceptualRoughness);
    half4 screen = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (front)
        screen = HB_ScreenReflect(input.positionWS, reflectVector, brdfData.perceptualRoughness);

    half3 environment = lerp(probes + sky * (1.0h - probeWeight), screen.rgb, screen.a);
    half gloss = 1.0h - brdfData.perceptualRoughness;
    half fresnelTerm = Pow4(1.0h - saturate(dot(litNormalWS, viewWS))) * (gloss * gloss);

    half3 reflection = EnvironmentBRDFSpecular(brdfData, fresnelTerm) * environment;

    reflection += HB_LightResponse(mainLight, litNormalWS, normalWS, style, brush.coverage * HB_LIQUID_BRUSH_SHADING)
                * HB_DirectSpecular(brdfData, litNormalWS, sunDirection, viewWS, style.diffuseSoftness, 0.0h);
    half3 extraLight = half3(0.0h, 0.0h, 0.0h);
    half3 extraFoam = half3(0.0h, 0.0h, 0.0h);

    HB_LiquidLights(input.positionWS, screenUV, litNormalWS, normalWS, viewWS, brdfData, style,
                    brush.coverage * HB_LIQUID_BRUSH_SHADING, reflection, extraLight, extraFoam);

    float pattern = HB_LiquidMixed(_HB_LiquidFoamShape, water.mix).z;

    reflection += sunLight * HB_LiquidGlints(restXZ, restDx, restDy, pattern, normalWS, viewWS, sunDirection);
    reflection *= saturate(look.murk * HB_LIQUID_EDGE_SOFT);

    half3 skyLight = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                   * 0.5h;
    half throb = HB_LiquidThrob(water.mix, foam.grain);
    half3 lit = skyLight + (sunLight + extraLight) * HB_LIQUID_SUN_SCATTER;
    half glow = look.glow * (HB_LIQUID_GLOW_BASE + HB_LIQUID_GLOW_GRAIN * foam.grain) * throb;
    half3 cooled = lerp(half3(1.0h, 1.0h, 1.0h), HB_LiquidMixed(_HB_LiquidCrustTone, water.mix).rgb, crust);
    half3 inscatter = look.tone * look.murk * (lit + glow) * cooled;
    half window = 1.0h;

    UNITY_BRANCH
    if (front)
    {
        half fold = half(input.foam.z);
        half spread = lerp(HB_LIQUID_CREST_HARD, HB_LIQUID_CREST_SOFT, look.crestSoftness);
        half rise = max(min(HB_LIQUID_CREST_FROM, half(input.foam.w) * HB_LIQUID_CREST_EARLY), 1e-3h);
        half weave = (foam.grain - 0.5h) * HB_LIQUID_CREST_GRAIN;
        half crested = saturate((fold - rise + spread - weave) / (2.0h * spread)) * smoothstep(0.0h, rise, fold);
        half backlit = saturate(dot(viewWS, -sunDirection) * 0.5h + 0.5h);
        half crest = crested * lerp(HB_LIQUID_CREST_BASE, 1.0h, backlit * backlit)
                   * saturate(1.0h - exp(-HB_LIQUID_CREST_DAWN * max(sunDirection.y, 0.0h)));

        inscatter += (sunLight + skyLight * HB_LIQUID_CREST_SKY) * look.crestTone
                   * (crest * HB_LIQUID_CREST_LIGHT * (1.0h - foam.cover));
    }
    else
    {
        float sunk = max(_HB_LiquidEye - GetCameraPositionWS().y, 0.0);
        half limit = lerp(HB_LIQUID_UNDER_WINDOW, HB_LIQUID_UNDER_SMALLEST,
                          half(1.0 - exp(-sunk / HB_LIQUID_UNDER_SHRINK)));
        half3 calmWS = HB_LiquidUnderCalm(normalWS, HB_LIQUID_UNDER_TILT);
        half mirror = saturate(reflect(-viewWS, calmWS).y + 1.0h);
        half facing = abs(viewWS.y) + dot(viewWS.xz, calmWS.xz) * HB_LIQUID_UNDER_EDGE;

        window = smoothstep(limit - HB_LIQUID_UNDER_SOFT, limit + HB_LIQUID_UNDER_SOFT, facing)
               * rippleSet.z;
        inscatter = HB_LiquidToneAt(water.mix, HB_LIQUID_UNDER_DEEP) * (lit * HB_LIQUID_UNDER_BELOW + glow)
                  * (lerp(HB_LIQUID_UNDER_SHADE, 1.0h, mirror) * (1.0h - window));
        reflection = half3(0.0h, 0.0h, 0.0h);
    }

    UNITY_BRANCH
    if (front && look.caustics > 0.0h && look.murk < HB_LIQUID_CAUSTIC_MURK && thickness > 0.0
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

    half3 spotLight = skyLight + sunLight * saturate(dot(normalWS, sunDirection) * 0.5h + 0.5h) + extraFoam
                    + look.tone * glow;
    half3 spotColour = look.foamTone * spotLight;
    half fade = water.fade;
    half molten = 1.0h - crust;
    half cover = foam.cover * fade * molten;
    half block = saturate(foam.cover * molten * rcp(HB_LIQUID_FOAM_MOST));

    HBTransparentFog fog = HB_TransparentFog(input.positionWS);

    UNITY_BRANCH
    if (!front)
    {
        fog.colour = half3(0.0h, 0.0h, 0.0h);
        fog.clear = 1.0h;
    }

    half3 colour = lerp((reflection + inscatter) * fade, spotColour, cover) * fog.clear + fog.colour * fade;

    colour += look.tone * (glow * foam.flash * HB_LIQUID_BUBBLE_FLASH * molten * fade * fog.clear);

    float longer = min((path + deep * HB_LIQUID_LIGHT_PATH) / max(path, HB_LIQUID_PATH_LEAST), HB_LIQUID_PATH_MOST);
    half3 passed = front ? HB_LiquidPassed(look.murk, look.tone, longer) : half3(window, window, window);
    half3 through = lerp(half3(1.0h, 1.0h, 1.0h), passed * (fog.clear * (1.0h - block)), fade);
    half open = max(through.r, max(through.g, through.b));
    half blur = saturate(look.blur + look.murk * HB_LIQUID_MURK_BLUR) * fog.clear
              * saturate(look.murk * HB_LIQUID_EDGE_SOFT) * fade;

    UNITY_BRANCH
    if (open > HB_LIQUID_SOLID)
    {
        half distortion = HB_LiquidMixed(_HB_LiquidMolten, water.mix).z * fog.clear * (1.0h - foam.spots) * fade;
        float2 bend = HB_LiquidBend(surfaceEye, thickness, screenUV, normalWS, distortion);
        half3 lift = front ? lerp(half3(1.0h, 1.0h, 1.0h), HB_LiquidGlowLift(look.tone, glow, lit), fade)
                           : half3(1.0h, 1.0h, 1.0h);

        colour += HB_Refracted(screenUV, bend, blur, 0.0h) * (lift * through);
    }

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
