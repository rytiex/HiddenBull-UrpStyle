#ifndef HB_PRECIPITATION_PASS_INCLUDED
#define HB_PRECIPITATION_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleShelter.hlsl"

TEXTURE2D(_HB_GroundHeightmap);
TEXTURE2D_FLOAT(_HB_ShelterDrops);

float4 _HB_PrecipitationOffset;
float4 _HB_PrecipitationVelocity;
float4 _HB_GroundSize;
float4 _HB_GroundOrigin;

#define HB_GROUND_HEIGHT_SCALE  (65535.0 / 32766.0)

#define HB_DROP_OFFSET          _HB_PrecipitationOffset.xyz
#define HB_DROP_SEED            uint(_HB_PrecipitationOffset.w)
#define HB_DROP_VELOCITY        _HB_PrecipitationVelocity.xyz

#define HB_PRECIPITATION_BOX    30.0
#define HB_DEBUG_PRECIPITATION  11

#define HB_LANDING              0.3
#define HB_LANDING_SCATTER      0.3
#define HB_NEAR_START           0.3
#define HB_NEAR_END             1.5
#define HB_EDGE_FADE            0.75
#define HB_MIN_PIXELS           1.5
#define HB_MIN_COVERAGE         0.3
#define HB_NEAR_CLIP            0.05

#define HB_RAIN_SHUTTER         0.04
#define HB_RAIN_MAX_PIXELS      4.0
#define HB_RAIN_AMBIENT         1.2h
#define HB_RAIN_KEY             0.5h

#define HB_SNOW_SIZE_RANGE      0.4
#define HB_SNOW_SWAY            0.25
#define HB_SNOW_AMBIENT         1.2h
#define HB_SNOW_KEY             0.8h

#define HB_SHELTERED_COLOUR     half3(1.0h, 0.1h, 0.05h)

struct HBDropVaryings
{
    float4 positionCS : SV_POSITION;
    float2 coord      : TEXCOORD0;
    half4 colour      : COLOR;
};

static const float2 HB_DROP_CORNERS[6] =
{
    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
};

float3 HB_DropSeed(uint drop)
{
    uint3 v = uint3(drop, drop * 1664525u + 1013904223u, drop ^ 0x9E3779B9u);

    v = v * 1664525u + 1013904223u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v ^= v >> 16u;
    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;

    return float3(v) * (1.0 / 4294967295.0);
}

float3 HB_WrapDrop(float3 seed, float3 cameraPositionWS)
{
    float3 origin = cameraPositionWS - HB_PRECIPITATION_BOX * 0.5;
    float3 local = seed * HB_PRECIPITATION_BOX + HB_DROP_OFFSET - origin;

    return origin + local - floor(local / HB_PRECIPITATION_BOX) * HB_PRECIPITATION_BOX;
}

half HB_DropGrounded(float3 positionWS)
{
    half grounded = 0.0h;
    float2 uv = (positionWS.xz - _HB_GroundOrigin.xz) / max(_HB_GroundSize.xz, 1e-3);

    UNITY_BRANCH
    if (_HB_GroundOrigin.w > 0.5 && all(uv >= 0.0) && all(uv <= 1.0))
    {
        float resolution = _HB_GroundSize.w;
        float2 texel = (uv * (resolution - 1.0) + 0.5) / resolution;
        float ground = _HB_GroundOrigin.y + _HB_GroundSize.y * HB_GROUND_HEIGHT_SCALE
                     * SAMPLE_TEXTURE2D_LOD(_HB_GroundHeightmap, sampler_LinearClamp, texel, 0).r;

        grounded = half(saturate((ground + HB_LANDING - positionWS.y) / HB_LANDING));
    }

    return grounded;
}

half HB_DropSheltered(float3 positionWS)
{
    half sheltered = 0.0h;

    UNITY_BRANCH
    if (HB_SHELTER_BOUND > 0.5)
    {
        float4 clip = mul(_HB_ShelterViewProj, float4(positionWS.x, 0.0, positionWS.z, 1.0));
        float2 uv = clip.xy;
#if UNITY_UV_STARTS_AT_TOP
        uv.y = -uv.y;
#endif
        uv = uv * 0.5 + 0.5;

        if (all(uv > 0.0) && all(uv < 1.0))
        {
            float depth = SAMPLE_TEXTURE2D_LOD(_HB_ShelterDrops, sampler_PointClamp, uv, 0).r;
            float roof = depth * HB_SHELTER_HEIGHT_SCALE + HB_SHELTER_HEIGHT_BIAS;
            float empty = HB_SHELTER_HEIGHT_BIAS + min(HB_SHELTER_HEIGHT_SCALE, 0.0) + HB_SHELTER_EMPTY_MARGIN;

            sheltered = roof > empty ? half(saturate((roof + HB_LANDING - positionWS.y) / HB_LANDING)) : 0.0h;
        }
    }

    return max(sheltered, HB_DropGrounded(positionWS));
}

half HB_DropFade(float3 positionWS, float3 seed, float3 cameraPositionWS, out half sheltered)
{
    float gap = distance(positionWS, cameraPositionWS);
    float radius = HB_PRECIPITATION_BOX * 0.5;

    half fade = half((1.0 - smoothstep(radius * HB_EDGE_FADE, radius, gap))
                     * smoothstep(HB_NEAR_START, HB_NEAR_END, gap));

    sheltered = 0.0h;

    UNITY_BRANCH
    if (fade > 0.0h)
    {
        float2 scatter = (frac(seed.yz * 7.31 + seed.x) - 0.5) * HB_LANDING_SCATTER;

        sheltered = HB_DropSheltered(positionWS + float3(scatter.x, 0.0, scatter.y));
        fade *= 1.0h - HB_FogAmount(positionWS, cameraPositionWS);
    }

    return fade;
}

half HB_DropAlpha(half opacity, half fade, half sheltered, out bool marked)
{
    marked = false;

#if defined(HB_STYLE_DEBUG)
    marked = HB_DEBUG_VIEW == HB_DEBUG_PRECIPITATION && sheltered > 0.5h;
#endif

    return marked ? max(opacity, 0.5h) * fade : opacity * fade * (1.0h - sheltered);
}

HBDropVaryings HB_CollapsedDrop()
{
    HBDropVaryings output = (HBDropVaryings)0;
    output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
    return output;
}

HBDropVaryings HB_RainVertex(uint vertexID : SV_VertexID)
{
    float2 corner = HB_DROP_CORNERS[vertexID % 6u];
    float3 seed = HB_DropSeed(vertexID / 6u + HB_DROP_SEED);
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 head = HB_WrapDrop(seed, cameraPositionWS);

    half sheltered;
    bool marked;
    half fade = HB_DropFade(head, seed, cameraPositionWS, sheltered);
    half alpha = HB_DropAlpha(half(HB_RAIN_OPACITY), fade, sheltered, marked);

    if (alpha <= 0.0h)
        return HB_CollapsedDrop();

    float3 tail = head - HB_DROP_VELOCITY * HB_RAIN_SHUTTER;
    float4 headCS = TransformWorldToHClip(head);
    float4 tailCS = TransformWorldToHClip(tail);

    if (headCS.w < HB_NEAR_CLIP || tailCS.w < HB_NEAR_CLIP)
        return HB_CollapsedDrop();

    float2 halfScreen = _ScreenParams.xy * 0.5;
    float2 axis = (tailCS.xy / tailCS.w - headCS.xy / headCS.w) * halfScreen;
    float span = length(axis);
    axis = span > 1e-3 ? axis / span : float2(0.0, 1.0);

    float projected = HB_RAIN_WIDTH * HB_PixelsPerMetre(headCS.w);
    float width = clamp(projected, HB_MIN_PIXELS, HB_RAIN_MAX_PIXELS);

    float4 positionCS = corner.x < 0.5 ? headCS : tailCS;
    float2 side = float2(-axis.y, axis.x) * ((corner.y * 2.0 - 1.0) * width * 0.5);
    positionCS.xy += side / halfScreen * positionCS.w;

    half3 light = marked
        ? HB_SHELTERED_COLOUR
        : (HB_AmbientSkyColor() * HB_RAIN_AMBIENT + half3(_MainLightColor.rgb) * HB_RAIN_KEY) * half3(HB_RAIN_COLOR);
    half coverage = half(max(saturate(projected / width), HB_MIN_COVERAGE));

    HBDropVaryings output;
    output.positionCS = positionCS;
    output.coord = float2(corner.x, corner.y * 2.0 - 1.0);
    output.colour = half4(light, alpha * coverage);
    return output;
}

half4 HB_RainFragment(HBDropVaryings input) : SV_Target
{
    half across = 1.0h - smoothstep(0.3h, 1.0h, half(abs(input.coord.y)));
    half along = smoothstep(0.0h, 0.15h, half(input.coord.x)) * (1.0h - smoothstep(0.5h, 1.0h, half(input.coord.x)));
    half alpha = input.colour.a * across * along;

    return half4(input.colour.rgb * alpha, alpha);
}

HBDropVaryings HB_SnowVertex(uint vertexID : SV_VertexID)
{
    float2 corner = HB_DROP_CORNERS[vertexID % 6u];
    float3 seed = HB_DropSeed(vertexID / 6u + HB_DROP_SEED);
    float3 cameraPositionWS = GetCameraPositionWS();
    float3 centre = HB_WrapDrop(seed, cameraPositionWS);

    centre += float3(sin(HB_SURFACE_CLOCK * 0.9 + seed.y * TWO_PI), 0.0,
                     cos(HB_SURFACE_CLOCK * 0.7 + seed.z * TWO_PI)) * HB_SNOW_SWAY;

    half sheltered;
    bool marked;
    half fade = HB_DropFade(centre, seed, cameraPositionWS, sheltered);
    half alpha = HB_DropAlpha(half(HB_SNOW_OPACITY), fade, sheltered, marked);

    if (alpha <= 0.0h)
        return HB_CollapsedDrop();

    float4 positionCS = TransformWorldToHClip(centre);

    if (positionCS.w < HB_NEAR_CLIP)
        return HB_CollapsedDrop();

    float projected = HB_SNOW_SIZE * (1.0 + (seed.x - 0.5) * 2.0 * HB_SNOW_SIZE_RANGE)
                    * HB_PixelsPerMetre(positionCS.w);
    float size = max(projected, HB_MIN_PIXELS);

    float2 halfScreen = _ScreenParams.xy * 0.5;
    positionCS.xy += (corner * 2.0 - 1.0) * (size * 0.5) / halfScreen * positionCS.w;

    half3 light = marked
        ? HB_SHELTERED_COLOUR
        : HB_AmbientSkyColor() * HB_SNOW_AMBIENT + half3(_MainLightColor.rgb) * HB_SNOW_KEY;
    half coverage = half(max(saturate(projected / size), HB_MIN_COVERAGE));

    HBDropVaryings output;
    output.positionCS = positionCS;
    output.coord = corner * 2.0 - 1.0;
    output.colour = half4(light, alpha * coverage * coverage);
    return output;
}

half4 HB_SnowFragment(HBDropVaryings input) : SV_Target
{
    half alpha = input.colour.a * (1.0h - smoothstep(0.4h, 1.0h, half(length(input.coord))));

    return half4(input.colour.rgb * alpha, alpha);
}

#endif
