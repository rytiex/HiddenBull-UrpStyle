#ifndef HIDDENBULL_URPSTYLE_FOG_INCLUDED
#define HIDDENBULL_URPSTYLE_FOG_INCLUDED

#include "StyleCommon.hlsl"
#include "StyleSky.hlsl"

float4 _HB_FogParams;
float4 _HB_FogScatter;

half HB_FogAmount(float3 positionWS, float3 cameraPositionWS)
{
    float density = _HB_FogParams.x;
    if (density <= 0.0)
        return 0.0h;

    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    if (rayLength < 1e-4)
        return 0.0h;

    float3 direction = ray / rayLength;

    float travelled = rayLength - _HB_FogScatter.z;
    if (travelled <= 0.0)
        return 0.0h;

    float3 origin = cameraPositionWS + direction * _HB_FogScatter.z;

    float falloff = max(_HB_FogParams.y, 1e-4);
    float relativeHeight = clamp((origin.y - _HB_FogParams.z) * falloff, -40.0, 40.0);
    float densityAtOrigin = density * exp(-relativeHeight);

    float verticalRate = direction.y * falloff;

    float integrated = abs(verticalRate) > 1e-4
        ? densityAtOrigin * (1.0 - exp(-clamp(verticalRate * travelled, -40.0, 40.0))) / verticalRate
        : densityAtOrigin * travelled;

    return half(saturate(1.0 - exp(-max(integrated, 0.0))) * _HB_FogParams.w);
}

#define HB_FOG_SCATTER_POWER 8.0h

half3 HB_FogColour(half3 direction, half sunVisibility)
{
    half shade = half(_HB_FogScatter.y) * (1.0h - sunVisibility);

    half3 colour = HB_SampleSkyLut(direction.y - shade, HB_LUT_TIME, HB_LUT_FOG);

    half amount = half(_HB_FogScatter.x) * half(_HB_FogScatter.w) * sunVisibility;
    if (amount > 0.0h && _HB_SunDirection.w > 0.5)
    {
        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));
        half scatter = PositivePow(towardSun, HB_FOG_SCATTER_POWER);

        colour = lerp(colour, _HB_SunColor.rgb, saturate(scatter * amount));
    }

    return colour;
}

half HB_SkyHaze(half3 direction)
{
    float density = _HB_FogParams.x;
    if (density <= 0.0)
        return 0.0h;

    float falloff = max(_HB_FogParams.y, 1e-4);
    float relativeHeight = clamp((GetCameraPositionWS().y - _HB_FogParams.z) * falloff, -40.0, 40.0);
    float densityAtCamera = density * exp(-relativeHeight);

    float opticalDepth = densityAtCamera / max(abs(direction.y) * falloff, 1e-3);

    return half(saturate(1.0 - exp(-min(opticalDepth, 40.0))) * _HB_FogParams.w);
}

half3 HB_SkyWithFog(half3 direction)
{
    half3 sky = HB_SkyColour(direction);
    half haze = HB_SkyHaze(direction);

    return lerp(sky, HB_FogColour(direction, 1.0h), haze);
}

half3 HB_ApplyFog(half3 colour, float3 positionWS, half sunVisibility)
{
    float3 cameraPositionWS = GetCameraPositionWS();

    half amount = HB_FogAmount(positionWS, cameraPositionWS);
    if (amount <= 0.0h)
        return colour;

    half3 direction = half3(SafeNormalize(positionWS - cameraPositionWS));

    return lerp(colour, HB_FogColour(direction, sunVisibility), amount);
}

#endif
