#ifndef HIDDENBULL_URPSTYLE_SKY_INCLUDED
#define HIDDENBULL_URPSTYLE_SKY_INCLUDED

#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"
#include "StyleBrush.hlsl"

float4 _HB_SkyParams;
float4 _HB_SkyPaint;

#define HB_SKY_PAINT         _HB_SkyPaint.x

#define HB_SKY_BODY_MASK  0.005h
#define HB_SKY_GLOW_MASK  0.15h

#define HB_SKY_BRUSH_SCALE   _HB_SkyParams.x
#define HB_SKY_SUNSET_FOCUS  _HB_SkyParams.y
#define HB_SKY_BRUSH         _HB_SkyParams.z
#define HB_SKY_BRUSH_SMOOTH  _HB_SkyParams.w

#define HB_SKY_SUNSET_DELAY  0.35h

float4 _HB_SunDirection;
float4 _HB_SunColor;
float4 _HB_SunDisc;
float4 _HB_SunGlow;
float4 _HB_MoonColor;
float4 _HB_MoonDisc;
float4 _HB_MoonGlow;
float4 _HB_StarParams;

half HB_SunFacing(half3 direction)
{
    float2 azimuth = _HB_SunDirection.xz;

    half toward = half(dot(direction.xz, azimuth * rsqrt(max(dot(azimuth, azimuth), 1e-6))));

    half swept = saturate((half(HB_SKY_TIME) - HB_SKY_SUNSET_DELAY)
                          * rcp(1.0h - HB_SKY_SUNSET_DELAY));

    half front = lerp(-1.0h, 1.0h, swept);
    half width = lerp(2.2h, 0.85h, half(HB_SKY_SUNSET_FOCUS));

    half t = saturate((toward - front + width) * rcp(2.0h * width));

    return t * t * t * (t * (t * 6.0h - 15.0h) + 10.0h);
}

half3 HB_SkySample(half3 direction, half up)
{
    half3 toward = HB_SampleSkyLut(up, HB_LUT_TIME, HB_LUT_SKY);
    half3 away = HB_SampleSkyLut(up, HB_LUT_AWAY, HB_LUT_SKY);

    return lerp(away, toward, HB_SunFacing(direction)) * half(HB_SKY_INTENSITY);
}

float HB_SkyBrushTurns(float scale)
{
    return max(round(scale * 4.0), 1.0);
}

float2 HB_SkyBrushUV(half3 direction, float scale)
{
    float azimuth = atan2(direction.z, direction.x) * (0.5 / PI);

    return float2(azimuth * HB_SkyBrushTurns(scale), direction.y * scale);
}

half4 HB_SkyStroke(half3 direction)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas,
                                HB_SkyBrushUV(direction, max(HB_SKY_BRUSH_SCALE, 1e-3)),
                                HB_SKY_BRUSH_SMOOTH * 4.0);
}

half3 HB_SkyPaintDirection(half3 direction, half4 stroke, half reach, out half lift)
{
    float scale = max(HB_SKY_BRUSH_SCALE, 1e-3);
    float2 spine = float2(stroke.rg * 2.0h - 1.0h) * (HB_BRUSH_SPINE_RANGE * reach);

    float sine, cosine;
    sincos(spine.x * (TWO_PI / HB_SkyBrushTurns(scale)), sine, cosine);

    lift = half(spine.y / scale);

    return half3(direction.x * cosine - direction.z * sine,
                 direction.y,
                 direction.x * sine + direction.z * cosine);
}

half3 HB_SkyGradient(half3 direction)
{
    half up = direction.y;

    half brush = half(HB_SKY_BRUSH);
    half paint = half(HB_SKY_PAINT);

    if ((brush <= 0.0h && paint <= 0.0h) || HB_BRUSH_ATLAS_BOUND <= 0.5h)
        return HB_SkySample(direction, up);

    half mask = smoothstep(0.0h, 0.2h, up) * (1.0h - smoothstep(0.85h, 1.0h, up));

    if (mask <= 0.0h)
        return HB_SkySample(direction, up);

    half4 atlas = HB_SkyStroke(direction);
    half stroke = (atlas.b * 2.0h - 1.0h) * mask;

    half lift;
    half3 painted = HB_SkyPaintDirection(direction, atlas, paint * mask, lift);

    half3 colour = HB_SkySample(painted, up + lift + stroke * brush * 0.32h);

    return colour * (1.0h + stroke * brush * 0.5h);
}

float2 HB_CelestialPlane(float3 direction, float3 axis)
{
    float3 reference = abs(axis.y) > 0.99 ? float3(0.0, 0.0, 1.0) : float3(0.0, 1.0, 0.0);
    float3 right = normalize(cross(axis, reference));
    float3 up = cross(right, axis);

    return float2(dot(direction, right), dot(direction, up));
}

half3 HB_CelestialBody(float3 direction, float3 axis, float4 disc, float4 glow, float4 color,
                       half bodyMask, half glowMask)
{
    half cosAngle = half(saturate(dot(direction, axis)));

    half halo = PositivePow(cosAngle, max(half(glow.x), HB_EPSILON)) * half(glow.y);

    half rayStrength = half(glow.z) * bodyMask;
    if (rayStrength > 0.0h)
    {
        float2 rayPlane = HB_CelestialPlane(direction, axis);
        half angle = half(atan2(rayPlane.y, rayPlane.x));
        half rays = sin(angle * 12.0h) * 0.5h + 0.5h;
        halo *= 1.0h + rays * rays * rayStrength;
    }

    half body = 0.0h;

    if (bodyMask > 0.0h && cosAngle > half(disc.x) - half(disc.y) * 2.0h)
    {
        half edgeOffset = 0.0h;

        if (disc.z > 0.0h && HB_BRUSH_ATLAS_BOUND > 0.5h)
        {
            float2 plane = HB_CelestialPlane(direction, axis);

            half4 atlas = SAMPLE_TEXTURE2D_LOD(_HB_BrushAtlas, sampler_HB_BrushAtlas,
                                               plane * 6.0, 0);
            edgeOffset = (atlas.b * 2.0h - 1.0h) * half(disc.z);
        }

        half threshold = half(disc.x) + edgeOffset * (1.0h - half(disc.x));
        half softness = max(half(disc.y), HB_EPSILON);

        body = smoothstep(threshold - softness, threshold + softness, cosAngle);
    }

    return color.rgb * color.a * (body * bodyMask + halo * glowMask);
}

float HB_StarHash(float3 cell)
{
    float3 p = frac(cell * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.x + p.y) * p.z);
}

half3 HB_Stars(float3 direction)
{
    half brightness = half(_HB_StarParams.y);
    if (brightness <= 0.0h)
        return half3(0.0h, 0.0h, 0.0h);

    float3 scaled = direction * _HB_StarParams.x;
    float3 cell = floor(scaled);

    float presence = HB_StarHash(cell);
    if (presence < _HB_StarParams.w)
        return half3(0.0h, 0.0h, 0.0h);

    float3 offset = float3(
        HB_StarHash(cell + 11.0),
        HB_StarHash(cell + 23.0),
        HB_StarHash(cell + 37.0));

    float toCentre = length(scaled - (cell + 0.25 + offset * 0.5));
    half star = half(saturate(1.0 - toCentre * 6.0));
    star = star * star * star;

    half twinkle = 1.0h - half(_HB_StarParams.z) *
        (0.5h + 0.5h * half(sin(_Time.y * 3.0 + presence * 40.0)));

    return star * brightness * twinkle;
}

half3 HB_SkyColour(half3 direction)
{
    half3 sky = HB_SkyGradient(direction);

    half glowMask = smoothstep(-HB_SKY_GLOW_MASK, 0.0h, direction.y);
    if (glowMask <= 0.0h)
        return sky;

    half bodyMask = smoothstep(-HB_SKY_BODY_MASK, HB_SKY_BODY_MASK, direction.y);

    half3 celestial = HB_Stars(direction) * bodyMask;

    if (_HB_SunColor.a > 0.0)
        celestial += HB_CelestialBody(direction, _HB_SunDirection.xyz, _HB_SunDisc, _HB_SunGlow,
                                      _HB_SunColor, bodyMask, glowMask);

    if (_HB_MoonColor.a > 0.0)
        celestial += HB_CelestialBody(direction, -_HB_SunDirection.xyz, _HB_MoonDisc, _HB_MoonGlow,
                                      _HB_MoonColor, bodyMask, glowMask);

    return sky + celestial;
}

#endif
