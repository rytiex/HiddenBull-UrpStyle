#ifndef HIDDENBULL_URPSTYLE_SKY_INCLUDED
#define HIDDENBULL_URPSTYLE_SKY_INCLUDED

#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"
#include "StyleBrush.hlsl"

float4 _HB_SkyParams;
float4 _HB_SunDirection;
float4 _HB_SunColor;
float4 _HB_SunDisc;
float4 _HB_SunGlow;
float4 _HB_MoonColor;
float4 _HB_MoonDisc;
float4 _HB_MoonGlow;
float4 _HB_StarParams;

half3 HB_SkyGradient(half3 direction)
{
    return HB_SampleSkyLut(direction.y, HB_LUT_ROW_SKY);
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

    half rayStrength = half(glow.z);
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

        if (disc.z > 0.0h && _HB_BrushParams.w > 0.5h)
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

    half glowMask = smoothstep(-half(_HB_SkyParams.y), 0.0h, direction.y);
    if (glowMask <= 0.0h)
        return sky;

    half bodyMask = smoothstep(-half(_HB_SkyParams.x), half(_HB_SkyParams.x), direction.y);

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
