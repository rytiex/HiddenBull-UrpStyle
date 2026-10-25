#ifndef HB_PRECIPITATION_FAR_PASS_INCLUDED
#define HB_PRECIPITATION_FAR_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl"

#define HB_FAR_SCROLL           _HB_PrecipitationOffset.x
#define HB_FAR_SHARE            _HB_PrecipitationOffset.y

#define HB_FAR_SEGMENTS         64u
#define HB_FAR_INNER            20.0
#define HB_FAR_OUTER            60.0
#define HB_FAR_BELOW            30.0
#define HB_FAR_ABOVE            60.0
#define HB_FAR_TOP_FADE         20.0
#define HB_FAR_OPACITY          0.5h
#define HB_FAR_JITTER           0.6
#define HB_FAR_SCROLL_WRAP      60.0

#define HB_FAR_RAIN_COLUMNS     524.0
#define HB_FAR_RAIN_ROW         0.1
#define HB_FAR_RAIN_LENGTH      0.35
#define HB_FAR_RAIN_SOFT        0.05

#define HB_FAR_SNOW_COLUMNS     314.0
#define HB_FAR_SNOW_SIZE_RANGE  0.4

struct HBFarVaryings
{
    float4 positionCS               : SV_POSITION;
    float3 positionWS               : TEXCOORD0;
    nointerpolation float radius    : TEXCOORD1;
    nointerpolation half3 light     : TEXCOORD2;
};

struct HBCurtain
{
    float s;
    float r;
    float circumference;
    float metresPerPixel;
    half fade;
    half sheltered;
};

HBFarVaryings HB_FarVertex(uint vertexID : SV_VertexID)
{
    uint quad = vertexID / 6u;
    float2 corner = HB_DROP_CORNERS[vertexID % 6u];
    float radius = quad < HB_FAR_SEGMENTS ? HB_FAR_OUTER : HB_FAR_INNER;
    float angle = (float(quad % HB_FAR_SEGMENTS) + corner.x) * (TWO_PI / float(HB_FAR_SEGMENTS));
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 positionWS = cameraPositionWS + float3(cos(angle) * radius, lerp(-HB_FAR_BELOW, HB_FAR_ABOVE, corner.y),
                                                  sin(angle) * radius);

    HBFarVaryings output;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionWS = positionWS;
    output.radius = radius;
#if defined(HB_FAR_SNOW)
    output.light = HB_AmbientSkyColor() * HB_SNOW_AMBIENT + half3(_MainLightColor.rgb) * HB_SNOW_KEY;
#else
    output.light = (HB_AmbientSkyColor() * HB_RAIN_AMBIENT + half3(_MainLightColor.rgb) * HB_RAIN_KEY)
                 * half3(HB_RAIN_COLOR);
#endif
    return output;
}

HBCurtain HB_ResolveCurtain(HBFarVaryings input)
{
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 offset = input.positionWS - cameraPositionWS;
    float2 around = offset.xz / max(length(offset.xz), HB_EPSILON);
    float fall = max(-HB_DROP_VELOCITY.y, 0.1);
    float drift = dot(HB_DROP_VELOCITY.xz, float2(-around.y, around.x)) / fall;

    HBCurtain curtain;
    curtain.circumference = TWO_PI * input.radius;
    curtain.s = atan2(around.y, around.x) * input.radius + offset.y * drift;
    curtain.r = input.positionWS.y + HB_FAR_SCROLL;
    curtain.metresPerPixel = 1.0 / HB_PixelsPerMetre(HB_ViewDepth(input.positionWS));
    curtain.fade = half((1.0 - smoothstep(HB_FAR_ABOVE - HB_FAR_TOP_FADE, HB_FAR_ABOVE, offset.y))
                        * (1.0 - HB_FogAmount(input.positionWS, cameraPositionWS)));
    curtain.sheltered = HB_DropSheltered(input.positionWS);
    return curtain;
}

half4 HB_CurtainColour(half3 light, half opacity, half shape, HBCurtain curtain)
{
    bool marked;
    half alpha = HB_DropAlpha(opacity * HB_FAR_OPACITY, curtain.fade, curtain.sheltered, marked) * shape;
    half3 colour = marked ? HB_SHELTERED_COLOUR : light;

    return half4(colour * alpha, alpha);
}

uint HB_CurtainWrap(float index, float count)
{
    return uint(index - count * floor(index / count));
}

half4 HB_FarRainFragment(HBFarVaryings input) : SV_Target
{
    HBCurtain curtain = HB_ResolveCurtain(input);

    float width = curtain.circumference / HB_FAR_RAIN_COLUMNS;
    float row = input.radius * HB_FAR_RAIN_ROW;
    float column = floor(curtain.s / width);
    uint index = HB_CurtainWrap(column, HB_FAR_RAIN_COLUMNS) + uint(input.radius) * 4096u;

    float3 lane = HB_DropSeed(index);
    float along = curtain.r / row + lane.x;
    uint lap = HB_CurtainWrap(floor(along), round(HB_FAR_SCROLL_WRAP / row));
    float3 streak = HB_DropSeed(index * 7919u + lap * 104729u + HB_DROP_SEED);

    float start = (1.0 - HB_FAR_RAIN_LENGTH) * streak.y;
    float through = frac(along) - start;
    half body = half(saturate(through / HB_FAR_RAIN_SOFT) * saturate((HB_FAR_RAIN_LENGTH - through) /
                                                                     (HB_FAR_RAIN_LENGTH * 0.6)));

    float across = (frac(curtain.s / width) - 0.5 - (lane.y - 0.5) * HB_FAR_JITTER) * width;
    float halfWidth = max(HB_RAIN_WIDTH * 0.5, curtain.metresPerPixel * 0.75);
    half shape = half(saturate(1.0 - abs(across) / halfWidth)) * body
               * half(max(saturate(HB_RAIN_WIDTH * 0.5 / halfWidth), HB_MIN_COVERAGE))
               * (half(streak.x) < half(HB_FAR_SHARE) ? 1.0h : 0.0h);

    return HB_CurtainColour(input.light, half(HB_RAIN_OPACITY), shape, curtain);
}

half4 HB_FarSnowFragment(HBFarVaryings input) : SV_Target
{
    HBCurtain curtain = HB_ResolveCurtain(input);

    float size = curtain.circumference / HB_FAR_SNOW_COLUMNS;
    float rows = round(HB_FAR_SCROLL_WRAP / size);
    float2 grid = float2(curtain.s / size, curtain.r * rows / HB_FAR_SCROLL_WRAP);
    float2 cell = floor(grid);
    uint index = HB_CurtainWrap(cell.x, HB_FAR_SNOW_COLUMNS) + uint(input.radius) * 4096u;

    float3 flake = HB_DropSeed(index * 7919u + HB_CurtainWrap(cell.y, rows) * 104729u + HB_DROP_SEED);
    float2 centre = 0.5 + (flake.yz - 0.5) * HB_FAR_JITTER;
    float distanceToCentre = length(frac(grid) - centre) * size;

    float radius = HB_SNOW_SIZE * 0.5 * (1.0 + (flake.z - 0.5) * 2.0 * HB_FAR_SNOW_SIZE_RANGE);
    float reach = max(radius, curtain.metresPerPixel * 0.75);
    half coverage = half(max(saturate(radius / reach), HB_MIN_COVERAGE));
    half shape = half(saturate(1.0 - distanceToCentre / reach)) * coverage * coverage
               * (half(flake.x) < half(HB_FAR_SHARE) ? 1.0h : 0.0h);

    return HB_CurtainColour(input.light, half(HB_SNOW_OPACITY), shape, curtain);
}

#endif
