#ifndef HB_UNDERWATER_PASS_INCLUDED
#define HB_UNDERWATER_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Liquid/LiquidSurfacePass.hlsl"

TEXTURE2D(_HB_UnderColour);
TEXTURE2D(_HB_UnderMask);
TEXTURE2D(_HB_UnderHeight);
TEXTURE2D_FLOAT(_HB_UnderDepth);

float4 _HB_UnderView;
float4 _HB_UnderMix;
float4 _HB_UnderQuality;
float4 _HB_LiquidUnder[4];
float4 _HB_LiquidUnderFog[4];

#define HB_UNDER_EMPTY          -5000.0
#define HB_UNDER_FAR            10000.0
#define HB_UNDER_RISE           1e-3
#define HB_UNDER_SNELL          0.66
#define HB_UNDER_SNELL_SOFT     0.06
#define HB_UNDER_LINE           1.5
#define HB_UNDER_LINE_DARK      0.45h
#define HB_UNDER_SEEN_LEAST     0.05
#define HB_UNDER_LIGHT_LEAST    0.5
#define HB_UNDER_WOBBLE         0.006
#define HB_UNDER_WOBBLE_ROWS    37.0
#define HB_UNDER_WOBBLE_COLUMNS 29.0
#define HB_UNDER_WOBBLE_PACE    float2(1.7, 1.3)
#define HB_UNDER_SHAFT_RANGE    24.0
#define HB_UNDER_SHAFT_FADE     10.0
#define HB_UNDER_SHAFT_GAIN     0.35h
#define HB_UNDER_SHAFT_LOD      2.0
#define HB_UNDER_SHAFT_SUN      0.05h
#define HB_UNDER_BUBBLE_DENSITY 1.5
#define HB_UNDER_BUBBLE_SHARE   0.25
#define HB_UNDER_BUBBLE_SPREAD  0.5
#define HB_UNDER_BUBBLE_NEAR    0.3
#define HB_UNDER_BUBBLE_NUDGE   1e-3
#define HB_UNDER_BUBBLE_LEAST   1e-6
#define HB_UNDER_BUBBLE_SIZE    0.025
#define HB_UNDER_BUBBLE_FAR     6.0
#define HB_UNDER_BUBBLE_RISE    0.12
#define HB_UNDER_BUBBLE_QUICK   1.6
#define HB_UNDER_BUBBLE_SWAY    0.08
#define HB_UNDER_BUBBLE_EDGE    0.12
#define HB_UNDER_BUBBLE_RIM     0.9
#define HB_UNDER_BUBBLE_BODY    0.12
#define HB_UNDER_BUBBLE_SPARK   1.4
#define HB_UNDER_BUBBLE_GLINT   float2(-0.38, 0.42)
#define HB_UNDER_BUBBLE_SHARP   4.0
#define HB_UNDER_BUBBLE_SEED    17.31
#define HB_UNDER_BUBBLE_TWIST   6.0
#define HB_UNDER_BUBBLE_TURN    0.7

struct HBUnderVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

HBUnderVaryings HB_UnderVertex(uint vertexID : SV_VertexID)
{
    HBUnderVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    output.uv = GetFullScreenTriangleTexCoord(vertexID);
    return output;
}

bool HB_UnderSolid(float depth)
{
#if UNITY_REVERSED_Z
    return depth > 0.0;
#else
    return depth < 1.0;
#endif
}

float4 HB_UnderHash(float2 id)
{
    float4 p = frac(id.xyxy * float4(0.1031, 0.1030, 0.0973, 0.1099));
    p += dot(p, p.wzxy + 33.33);
    return frac((p.xxyz + p.yzzw) * p.zywx);
}

half4 HB_UnderMixed(float4 values[4], half4 mix)
{
    return half4(values[0] * mix.x + values[1] * mix.y + values[2] * mix.z + values[3] * mix.w);
}

float HB_UnderPlane(float2 xz)
{
    return _HB_UnderView.x + dot(_HB_UnderView.zw, xz - GetCameraPositionWS().xz);
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

bool HB_UnderAt(float3 positionWS)
{
    bool known;
    float surfaceY = HB_UnderSurfaceAt(positionWS.xz, known);

    return known ? positionWS.y < surfaceY : _HB_UnderView.y > 0.5;
}

half3 HB_UnderLight(float3 eye, half4 mix, HBLiquidLook look, float lightDepth)
{
    Light mainLight = GetMainLight();
    half3 sky = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h))) * 0.5h;
    half3 sun = half3(mainLight.color) * HB_LiquidFloorShadow(eye) * HB_LIQUID_SUN_SCATTER;
    half dim = half(exp(-max(_HB_UnderView.x - eye.y, 0.0) / max(lightDepth, HB_UNDER_LIGHT_LEAST)));
    half glow = look.glow * (HB_LIQUID_GLOW_BASE + HB_LIQUID_GLOW_GRAIN * HB_LIQUID_GRAIN) * HB_LiquidThrob(mix);

    return (sky + sun) * dim + glow;
}

half3 HB_UnderFog(half3 colour, half3 source, half4 mix, HBLiquidLook look, half3 lit, float path, bool exits,
                  half window)
{
    UNITY_BRANCH
    if (exits && window < 1.0h)
    {
        HBLiquidLook deep = HB_LiquidLookAt(mix, HB_UNDER_FAR);

        colour = lerp(deep.tone * lit, source, window);
    }

    half3 through = PositivePow(clamp(look.shore, 1e-3h, 1.0h),
                                half(min(path / HB_LIQUID_TINT_DEPTH, HB_LIQUID_TINT_MOST)))
                  * (1.0h - look.murk);

    return colour * through + look.tone * look.murk * lit;
}

half3 HB_UnderCaustics(float3 floorWS, half4 mix, HBLiquidLook look)
{
    half3 sunDirection = half3(_MainLightPosition.xyz);
    half3 caustics = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (look.caustics > 0.0h && sunDirection.y > HB_LIQUID_CAUSTIC_SUN)
    {
        half daylight = lerp(HB_LIQUID_CAUSTIC_NIGHT, 1.0h,
                             half(saturate(_HB_SunDirection.y / HB_LIQUID_CAUSTIC_DUSK)));
        half bright = HB_LiquidCaustics(floorWS, HB_UnderPlane(floorWS.xz), sunDirection, mix)
                    * (look.caustics * daylight);

        UNITY_BRANCH
        if (bright > 0.0h)
            caustics = half3(GetMainLight().color) * (bright * HB_LiquidFloorShadow(floorWS));
    }

    return caustics;
}

half HB_UnderShafts(float3 start, float3 heading, float path, half4 mix, half amount, float2 pixel)
{
    half3 sunDirection = half3(_MainLightPosition.xyz);
    int steps = (int)_HB_UnderQuality.x;
    half shafts = 0.0h;

    UNITY_BRANCH
    if (amount > 0.0h && steps > 0 && sunDirection.y > HB_UNDER_SHAFT_SUN && _HB_LiquidCausticGrid.y > 0.5)
    {
        float span = min(path, HB_UNDER_SHAFT_RANGE);
        float stride = span / steps;
        float begin = InterleavedGradientNoise(pixel, 0) * stride;
        float scale = dot(float4(mix), float4(_HB_LiquidStyle[0].w, _HB_LiquidStyle[1].w, _HB_LiquidStyle[2].w,
                                              _HB_LiquidStyle[3].w));
        float tiles = _HB_LiquidCausticGrid.x / max(scale, 0.05);
        float still = 1.0 / (1.0 + HB_LIQUID_CAUSTIC_SHARP);
        float gathered = 0.0;

        UNITY_LOOP
        for (int index = 0; index < steps; index++)
        {
            float along = begin + stride * index;
            float3 there = start + heading * along;
            float under = HB_UnderPlane(there.xz) - there.y;

            if (under <= 0.0)
                continue;

            float2 lit = there.xz - _HB_TerrainOrigin.xz + float2(sunDirection.xz) * (under / float(sunDirection.y));
            float focus = HB_LiquidCausticFocus(lit * tiles, HB_LIQUID_CAUSTIC_DEEP * HB_LIQUID_CAUSTIC_FOCUS,
                                                HB_UNDER_SHAFT_LOD);

            gathered += max(focus - still, 0.0) * float(HB_LiquidFloorShadow(there))
                      * exp(-(along + under) / HB_UNDER_SHAFT_FADE);
        }

        shafts = half(gathered * stride) * amount * HB_UNDER_SHAFT_GAIN;
    }

    return shafts;
}

half HB_UnderBubbles(float3 eye, float3 heading, float away, half amount)
{
    int steps = (int)_HB_UnderQuality.y;
    half bubbles = 0.0h;

    UNITY_BRANCH
    if (amount > 0.0h && steps > 0)
    {
        float limit = min(away, HB_UNDER_BUBBLE_FAR * _HB_UnderQuality.z) * HB_UNDER_BUBBLE_DENSITY;
        float range = HB_UNDER_BUBBLE_FAR * _HB_UnderQuality.z;
        float3 right = UNITY_MATRIX_V[0].xyz;
        float3 up = UNITY_MATRIX_V[1].xyz;
        float3 origin = eye * HB_UNDER_BUBBLE_DENSITY;
        float2 home = floor(origin.xz);
        float2 seat = origin.xz - home;
        float2 risen = origin.y - _Time.y * HB_UNDER_BUBBLE_RISE * HB_UNDER_BUBBLE_DENSITY
                     * float2(1.0, HB_UNDER_BUBBLE_QUICK);
        float2 floors = floor(risen);
        float2 heights = risen - floors;
        float3 slope = lerp(HB_UNDER_BUBBLE_LEAST, heading, step(HB_UNDER_BUBBLE_LEAST, abs(heading)));
        float3 ahead = step(0.0, slope);
        float march = 0.0;

        UNITY_LOOP
        for (int index = 0; index < steps; index++)
        {
            if (march > limit)
                break;

            float2 walk = seat + heading.xz * march;
            float2 column = floor(walk);
            float2 block = home + column;
            bool fast = frac((block.x + block.y) * 0.5) > 0.25;
            float quick = fast ? HB_UNDER_BUBBLE_QUICK : 1.0;
            float level = (fast ? heights.y : heights.x) + heading.y * march;
            float storey = floor(level);
            float3 local = float3(walk.x - column.x, level - storey, walk.y - column.y);
            float3 exits = (ahead - local) / slope;
            float here = march;

            march += min(exits.x, min(exits.y, exits.z)) + HB_UNDER_BUBBLE_NUDGE;

            float4 chance = HB_UnderHash(block + ((fast ? floors.y : floors.x) + storey) * HB_UNDER_BUBBLE_SEED);

            if (chance.w > float(amount) * HB_UNDER_BUBBLE_SHARE)
                continue;

            float sway = _Time.y * (1.0 + chance.z) + chance.x * HB_UNDER_BUBBLE_TWIST;
            float3 centre = 0.5 + (chance.xyz - 0.5) * HB_UNDER_BUBBLE_SPREAD
                          + float3(sin(sway), 0.0, cos(sway * HB_UNDER_BUBBLE_TURN)) * (HB_UNDER_BUBBLE_SWAY * quick);
            float3 toward = centre - local;
            float along = dot(toward, heading);
            float3 across = toward - heading * along;
            float radius = HB_UNDER_BUBBLE_SIZE * HB_UNDER_BUBBLE_DENSITY * quick
                         * (0.35 + 1.3 * chance.z * chance.z);
            float span = length(across) / radius;
            float reach = (here + along) / HB_UNDER_BUBBLE_DENSITY;

            if (span >= 1.0 || reach >= away)
                continue;

            float2 disc = float2(dot(across, right), dot(across, up)) / radius;
            float edge = saturate((1.0 - span) / HB_UNDER_BUBBLE_EDGE);
            float rim = span * span * span * span * HB_UNDER_BUBBLE_RIM;
            float spark = saturate(1.0 - length(disc + HB_UNDER_BUBBLE_GLINT) * HB_UNDER_BUBBLE_SHARP);
            float fade = saturate(1.0 - reach / range) * saturate(reach / HB_UNDER_BUBBLE_NEAR - 1.0);
            float look = (HB_UNDER_BUBBLE_BODY + rim) * edge + spark * spark * HB_UNDER_BUBBLE_SPARK;

            bubbles = max(bubbles, half(look * fade * fade));
        }
    }

    return bubbles;
}

half4 HB_UnderFragment(HBUnderVaryings input) : SV_Target
{
    float2 uv = input.uv;
    half3 source = half3(SAMPLE_TEXTURE2D_LOD(_HB_UnderColour, sampler_PointClamp, uv, 0).rgb);
    float3 eye = GetCameraPositionWS();
    float3 nearWS = ComputeWorldSpacePosition(uv, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
    float start = distance(eye, nearWS);

    bool known;
    float surfaceY = HB_UnderSurfaceAt(nearWS.xz, known);
    bool under = known ? nearWS.y < surfaceY : _HB_UnderView.y > 0.5;
    float pixel = start * 2.0 / (abs(UNITY_MATRIX_P[1][1]) * _ScaledScreenParams.y);
    half seam = known ? half(saturate(1.0 - abs(nearWS.y - surfaceY) / max(pixel * HB_UNDER_LINE, HB_EPSILON)))
                      : 0.0h;

    half3 colour = source;
    bool exits = false;
    half window = 1.0h;

    UNITY_BRANCH
    if (under)
    {
        half4 mix = half4(_HB_UnderMix);
        half4 setting = HB_UnderMixed(_HB_LiquidUnder, mix);
        half4 fog = HB_UnderMixed(_HB_LiquidUnderFog, mix);
        float2 bent = uv;

        UNITY_BRANCH
        if (setting.z > 0.0h)
        {
            float2 wobble = float2(sin(uv.y * HB_UNDER_WOBBLE_ROWS + _Time.y * HB_UNDER_WOBBLE_PACE.x),
                                   cos(uv.x * HB_UNDER_WOBBLE_COLUMNS + _Time.y * HB_UNDER_WOBBLE_PACE.y));

            bent = saturate(uv + wobble * (HB_UNDER_WOBBLE * float(setting.z)));
            source = half3(SAMPLE_TEXTURE2D_LOD(_HB_UnderColour, sampler_LinearClamp, bent, 0).rgb);
        }

        float raw = SampleSceneDepth(bent);
        bool solid = HB_UnderSolid(raw);
        float3 farWS = ComputeWorldSpacePosition(bent, solid ? raw : UNITY_RAW_FAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
        float3 heading = normalize(farWS - nearWS);
        float hitAway = SAMPLE_TEXTURE2D_LOD(_HB_UnderMask, sampler_PointClamp, bent, 0).r;
        bool hit = hitAway > 0.0;
        float2 slope = _HB_UnderView.zw;
        float3 up = normalize(float3(-slope.x, 1.0, -slope.y));
        float scene = solid ? distance(nearWS, farWS) : HB_UNDER_FAR;
        float climb = heading.y - dot(slope, heading.xz);
        float rise = HB_UnderPlane(nearWS.xz) - nearWS.y;
        float leave = climb > HB_UNDER_RISE ? max(rise, 0.0) / climb : HB_UNDER_FAR;
        float path = hit ? max(hitAway - start, 0.0) : min(scene, leave);
        float seen = path / max(float(fog.x), HB_UNDER_SEEN_LEAST);

        exits = hit || leave < scene;
        window = half(smoothstep(HB_UNDER_SNELL - HB_UNDER_SNELL_SOFT, HB_UNDER_SNELL + HB_UNDER_SNELL_SOFT,
                                 dot(heading, up)));

        HBLiquidLook look = HB_LiquidLookAt(mix, seen);
        half3 lit = HB_UnderLight(eye, mix, look, fog.y);

        colour = source;

        UNITY_BRANCH
        if (!exits && solid)
            colour += HB_UnderCaustics(farWS, mix, look);

        colour = HB_UnderFog(colour, source, mix, look, lit, seen, exits, window);

        Light mainLight = GetMainLight();
        half3 sky = (HB_GradientAmbient(half3(0.0h, 1.0h, 0.0h)) + HB_GradientAmbient(half3(1.0h, 0.0h, 0.0h)))
                  * 0.5h;
        half dim = half(exp(-max(_HB_UnderView.x - eye.y, 0.0) / max(float(fog.y), HB_UNDER_LIGHT_LEAST)));

        colour += half3(mainLight.color) * look.tone
                * HB_UnderShafts(nearWS, heading, path, mix, setting.x, input.positionCS.xy);
        colour += (sky + half3(mainLight.color) * 0.5h) * (HB_UnderBubbles(eye, heading, path, setting.y) * dim);
    }

    colour *= 1.0h - seam * HB_UNDER_LINE_DARK;

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_UNDER)
    {
        half3 tone = under ? half3(0.1h, 0.35h, 0.9h) : source * 0.3h;

        if (under && exits)
            tone = lerp(half3(0.85h, 0.15h, 0.75h), half3(0.2h, 0.95h, 0.95h), window);

        return half4(lerp(tone, half3(1.0h, 1.0h, 1.0h), seam), 1.0h);
    }
#endif

    return half4(colour, 1.0h);
}

half4 HB_UnderOverlayFragment(HBUnderVaryings input) : SV_Target
{
    float2 uv = input.uv;
    float raw = SAMPLE_TEXTURE2D_LOD(_HB_UnderDepth, sampler_PointClamp, uv, 0).r;

    if (!HB_UnderSolid(raw))
        discard;

    float3 positionWS = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);

    if (!HB_UnderAt(positionWS))
        discard;

    half4 mix = half4(_HB_UnderMix);
    half4 fog = HB_UnderMixed(_HB_LiquidUnderFog, mix);
    half3 source = half3(SAMPLE_TEXTURE2D_LOD(_HB_UnderColour, sampler_PointClamp, uv, 0).rgb);
    float3 eye = GetCameraPositionWS();
    float seen = distance(eye, positionWS) / max(float(fog.x), HB_UNDER_SEEN_LEAST);
    HBLiquidLook look = HB_LiquidLookAt(mix, seen);
    half3 lit = HB_UnderLight(eye, mix, look, fog.y);
    half3 colour = HB_UnderFog(source, source, mix, look, lit, seen, false, 1.0h);

#if defined(HB_STYLE_DEBUG)
    UNITY_BRANCH
    if (HB_DEBUG_VIEW == HB_DEBUG_LIQUID_UNDER)
        return half4(0.95h, 0.8h, 0.25h, 1.0h);
#endif

    return half4(colour, 1.0h);
}

#endif
