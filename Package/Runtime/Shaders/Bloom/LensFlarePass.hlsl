#ifndef HB_LENS_FLARE_PASS_INCLUDED
#define HB_LENS_FLARE_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Bloom/BloomPass.hlsl"

TEXTURE2D(_HB_FlareSun);
float4 _HB_FlareParams;

#define HB_FLARE_THRESHOLD  _HB_BloomParams.x
#define HB_FLARE_GHOSTS     ((int)_HB_BloomParams.y)
#define HB_FLARE_SPACING    _HB_BloomParams.z
#define HB_FLARE_HALO_SIZE  _HB_BloomParams.w
#define HB_FLARE_HALO       _HB_BloomTint.x
#define HB_FLARE_FRINGE     _HB_BloomTint.y
#define HB_FLARE_TEXEL      _HB_BloomTint.zw
#define HB_FLARE_SUN        _HB_FlareParams.x
#define HB_FLARE_LIGHTS     _HB_FlareParams.y
#define HB_FLARE_RAYS       _HB_FlareParams.z
#define HB_FLARE_EDGE       0.70710678
#define HB_FLARE_SHIFT      0.02
#define HB_FLARE_WIDE       2.0
#define HB_FLARE_SPLIT      0.08
#define HB_FLARE_GLOW       0.25
#define HB_FLARE_RAY_GAIN   0.6
#define HB_FLARE_GHOST_GAIN 0.15
#define HB_FLARE_HALO_GAIN  0.12
#define HB_FLARE_HALO_WIDTH 0.06
#define HB_FLARE_LIGHT_GAIN 0.5

static const float4 HB_FLARE_GHOST_TABLE[8] =
{
    float4(0.55, 0.035, 0.6, 0.1),
    float4(0.25, 0.07, 0.35, 0.6),
    float4(-0.2, 0.025, 0.7, 0.3),
    float4(-0.45, 0.11, 0.25, 0.8),
    float4(-0.7, 0.05, 0.45, 0.2),
    float4(-1.0, 0.16, 0.18, 0.9),
    float4(-1.3, 0.04, 0.5, 0.45),
    float4(0.1, 0.02, 0.5, 0.7)
};

float3 HB_FlareHue(float shift)
{
    return lerp(float3(1.0, 0.86, 0.72), float3(0.72, 0.88, 1.0), shift);
}

float2 HB_FlareDirection(float2 vector2)
{
    return vector2 * rsqrt(max(dot(vector2, vector2), 1e-8));
}

float HB_FlareWindow(float2 uv)
{
    float2 inside = step(0.0, uv) * step(uv, 1.0);
    float weight = saturate(1.0 - length(uv - 0.5) / HB_FLARE_EDGE);

    return inside.x * inside.y * weight * weight;
}

float3 HB_FlareSpot(float2 uv)
{
    float3 sharp = HB_BloomTap(_HB_BloomSource, uv);
    float3 wide = HB_BloomTap(_HB_BloomLow, uv);
    float3 spot = max(sharp - wide * HB_FLARE_WIDE, 0.0);
    float bright = max(spot.r, max(spot.g, spot.b));
    float3 light = spot * (max(bright - HB_FLARE_THRESHOLD, 0.0) / max(bright, 1e-4));

    return light / (1.0 + max(light.r, max(light.g, light.b)));
}

float3 HB_FlareFringe(float2 uv, float2 direction)
{
    float2 shift = direction * HB_FLARE_FRINGE * HB_FLARE_SHIFT;

    return float3(HB_FlareSpot(uv + shift).r, HB_FlareSpot(uv).g, HB_FlareSpot(uv - shift).b);
}

float3 HB_FlareLights(float2 uv)
{
    float2 flipped = 1.0 - uv;
    float2 centre = 0.5 - flipped;
    float2 stride = centre * HB_FLARE_SPACING;
    float2 direction = HB_FlareDirection(centre);
    float3 flare = 0.0;
    int ghosts = HB_FLARE_GHOSTS;

    UNITY_LOOP
    for (int ghost = 0; ghost < ghosts; ghost++)
    {
        float2 tap = flipped + stride * ghost;

        flare += HB_FlareFringe(tap, direction) * HB_FlareWindow(tap) * HB_FlareHue(frac(ghost * 0.618));
    }

    return flare * HB_FLARE_LIGHT_GAIN;
}

float3 HB_FlareSplit(float radius)
{
    return radius * (1.0 + HB_FLARE_FRINGE * HB_FLARE_SPLIT * float3(1.0, 0.0, -1.0));
}

float HB_FlareDisc(float distance, float radius)
{
    float body = 1.0 - smoothstep(radius * 0.8, radius, distance);
    float rim = smoothstep(radius * 0.5, radius * 0.95, distance);

    return body * (0.3 + 0.7 * rim);
}

float3 HB_FlareGhost(float2 position, float2 centre, float radius)
{
    float distance = length(position - centre);
    float3 radii = HB_FlareSplit(radius);

    return float3(HB_FlareDisc(distance, radii.r), HB_FlareDisc(distance, radii.g), HB_FlareDisc(distance, radii.b));
}

float HB_FlareRays(float2 offset, float distance)
{
    float angle = atan2(offset.y, offset.x + 1e-6);
    float main = pow(abs(cos(angle * 3.0)), 48.0);
    float side = pow(abs(sin(angle * 3.0)), 64.0);
    float streak = pow(abs(cos(angle * 23.0 + 0.7)), 10.0);

    return main * exp(-distance * 5.0) + side * 0.5 * exp(-distance * 9.0) + streak * 0.2 * exp(-distance * 16.0);
}

float3 HB_FlareSun(float2 uv, float aspect, float2 sun)
{
    float3 colour = _HB_SunColor.rgb / max(max(_HB_SunColor.r, max(_HB_SunColor.g, _HB_SunColor.b)), 1e-4);
    float2 scale = float2(aspect, 1.0);
    float2 position = (uv - 0.5) * scale;
    float2 source = (sun - 0.5) * scale;
    float2 offset = position - source;
    float distance = length(offset);
    float rays = HB_FlareRays(offset, distance) * HB_FLARE_RAY_GAIN * HB_FLARE_RAYS;
    float3 flare = exp(-distance * 12.0) * HB_FLARE_GLOW + rays;
    float spread = 0.5 + HB_FLARE_SPACING * 1.5;
    float3 ghostLight = 0.0;
    int ghosts = HB_FLARE_GHOSTS;

    UNITY_LOOP
    for (int ghost = 0; ghost < ghosts; ghost++)
    {
        float4 shape = HB_FLARE_GHOST_TABLE[ghost];
        float2 centre = source * (1.0 - (1.0 - shape.x) * spread);

        ghostLight += HB_FlareGhost(position, centre, shape.y) * shape.z * HB_FlareHue(shape.w);
    }

    float radius = HB_FLARE_HALO_SIZE * 0.5;
    float3 ring = (distance - HB_FlareSplit(radius)) / (radius * HB_FLARE_HALO_WIDTH);

    flare += ghostLight * HB_FLARE_GHOST_GAIN + exp(-ring * ring) * HB_FLARE_HALO * HB_FLARE_HALO_GAIN;

    return flare * colour;
}

float4 HB_LensFlare(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 texel = HB_FLARE_TEXEL;
    float2 uv = positionCS.xy * texel;
    float3 flare = 0.0;

    float4 sun = LOAD_TEXTURE2D(_HB_FlareSun, int2(0, 0));
    float strength = sun.z * saturate(_HB_SunColor.a) * HB_FLARE_SUN;

    UNITY_BRANCH
    if (strength > 0.0)
        flare += HB_FlareSun(uv, texel.y / texel.x, sun.xy) * strength;

    UNITY_BRANCH
    if (HB_FLARE_LIGHTS > 0.0)
        flare += HB_FlareLights(uv) * HB_FLARE_LIGHTS;

    return float4(flare, 0.0);
}

#endif
