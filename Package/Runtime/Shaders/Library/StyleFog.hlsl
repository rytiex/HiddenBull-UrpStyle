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

half3 HB_FogColour(half3 direction, half sunlit, half reach)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    half shade = half(_HB_FogScatter.y);
#else
    half shade = 0.0h;
#endif

    half3 colour = HB_SampleSkyLut(direction.y - shade * (1.0h - sunlit),
                                   HB_LUT_TIME, HB_LUT_FOG);

    half amount = half(_HB_FogScatter.x) * half(_HB_FogScatter.w) * sunlit;
    if (amount > 0.0h && _HB_SunDirection.w > 0.5)
    {
        half towardSun = half(saturate(dot(direction, _HB_SunDirection.xyz)));
        half scatter = PositivePow(towardSun, HB_FOG_SCATTER_POWER);

        colour = lerp(colour, _HB_SunColor.rgb, saturate(scatter * amount));
    }

    return colour * lerp(1.0h, reach, shade);
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

    return lerp(sky, HB_FogColour(direction, 1.0h, 1.0h), haze);
}

#define HB_FOG_STEPS  4
#define HB_FOG_SPREAD 0.75
#define HB_FOG_BLUR   0.5
#define HB_FOG_GOLDEN 2.39996323

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)

bool HB_AirLight(float3 positionWS, float3 bias, out float3 light)
{
    APVResources resources = FillAPVResources();

    float3 uvw;

    if (!TryToGetPoolUVW(resources, positionWS - _APVWorldOffset, bias, bias, uvw))
    {
        light = 0.0;
        return false;
    }

    WarpUVWLeakReduction(resources, 0xFFFFFFFF, uvw);

    light = SAMPLE_TEXTURE3D_LOD(resources.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgb;

    return true;
}

#endif

half HB_FogAirReach(half reach, float3 cameraPositionWS, half3 direction, float rayLength,
                    float2 screenUV)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float density = max(_HB_FogParams.x, 1e-4);
    float span = min(rayLength - _HB_FogScatter.z, 3.0 / density);

    if (span <= 0.0)
        return reach;

    float opacity = 1.0 - exp(-density * span);
    float rcpDensity = 1.0 / density;

    float3 origin = cameraPositionWS + float3(direction) * _HB_FogScatter.z;
    float2 pixel = screenUV * _ScreenSize.xy;
    float open = max(HB_AMBIENT_OPEN, HB_EPSILON);

    float jitter = InterleavedGradientNoise(pixel, 0);

    float3 forward = float3(direction);
    float3 reference = abs(forward.y) > 0.99 ? float3(0.0, 0.0, 1.0) : float3(0.0, 1.0, 0.0);
    float3 right = normalize(cross(forward, reference));
    float3 up = cross(right, forward);

    float radius = min(span * (HB_FOG_SPREAD / HB_FOG_STEPS), HB_FOG_BLUR);

    float lit = 0.0;
    float peak = -1.0;

    UNITY_UNROLL
    for (int i = 0; i < HB_FOG_STEPS; i++)
    {
        float slice = (i + jitter) * (1.0 / HB_FOG_STEPS);
        float depth = -log(max(1.0 - slice * opacity, 1e-6)) * rcpDensity;

        float angle = (jitter + i) * HB_FOG_GOLDEN;
        float3 spread = (right * cos(angle) + up * sin(angle)) * radius;

        float3 air;
        float light = HB_AirLight(origin + forward * depth + spread, -forward, air)
            ? saturate(Luminance(air) / open)
            : 1.0;

        lit += light;
        peak = max(peak, light);
    }

    return half((lit - peak) * (1.0 / (HB_FOG_STEPS - 1)));
#else
    return reach;
#endif
}

half3 HB_ApplyFog(half3 colour, float3 positionWS, half sunlit, half reach, float2 screenUV)
{
    float3 cameraPositionWS = GetCameraPositionWS();

    half amount = HB_FogAmount(positionWS, cameraPositionWS);
    if (amount <= 0.0h)
        return colour;

    float3 ray = positionWS - cameraPositionWS;
    float rayLength = length(ray);
    half3 direction = half3(ray / rayLength);

    reach = HB_FogAirReach(reach, cameraPositionWS, direction, rayLength, screenUV);

    return lerp(colour, HB_FogColour(direction, sunlit, reach), amount);
}

#endif
