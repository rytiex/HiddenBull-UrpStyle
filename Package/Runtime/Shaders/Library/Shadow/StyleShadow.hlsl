#ifndef HB_STYLE_SHADOW_INCLUDED
#define HB_STYLE_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

#define HB_SHADOW_SOFTNESS     _HB_ShadowFilter.x
#define HB_SHADOW_CONTACT      _HB_ShadowFilter.y
#define HB_SHADOW_TAPS         _HB_ShadowFilter.z
#define HB_SHADOW_REACH        _HB_ShadowFilter.w

#define HB_SHADOW_BRUSH        _HB_ShadowBrush.x
#define HB_SHADOW_TILES        _HB_ShadowBrush.y
#define HB_SHADOW_ATLAS        _HB_ShadowBrush.zw

#define HB_SHADOW_MAX_SOFTNESS 0.225
#define HB_SHADOW_MAX_CONTACT  4.5
#define HB_SHADOW_MAX_TAPS     16.0
#define HB_SHADOW_GOLDEN       2.39996323

#define HB_DEBUG_SHADOW_ATTENUATION 1
#define HB_DEBUG_SHADOW_UNFILTERED  2
#define HB_DEBUG_SHADOW_BLOCKERS    3
#define HB_DEBUG_SHADOW_CASCADES    4
#define HB_DEBUG_SHADOW_FILTER      5
#define HB_DEBUG_SHADOW_RADIUS      6

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    #define HB_MAIN_LIGHT_SHADOW_MAP
#endif

struct HBShadowProbe
{
    half blockers;
    half radius;
    half reach;
    half realtime;
};

HBShadowProbe HB_NoShadowProbe()
{
    HBShadowProbe probe;
    probe.blockers = 0.0h;
    probe.radius = 0.0h;
    probe.reach = 0.0h;
    probe.realtime = 1.0h;
    return probe;
}

float3 HB_BrushShadowPosition(float3 positionWS, float3 positionDx, float3 positionDy, half3 normalWS,
                              half3 direction)
{
    half strength = half(HB_SHADOW_BRUSH);

    if (strength <= 0.0h || HB_BRUSH_ATLAS_BOUND < 0.5h || dot(direction, direction) < 0.5h)
        return positionWS;

    half3 reference = abs(direction.y) > 0.99h ? half3(0.0h, 0.0h, 1.0h) : half3(0.0h, 1.0h, 0.0h);

    half3 right = normalize(cross(direction, reference));
    half3 up = cross(right, direction);

    float2 plane = float2(dot(positionWS, right), dot(positionWS, up)) * HB_SHADOW_TILES;
    float2 planeDx = float2(dot(positionDx, right), dot(positionDx, up)) * HB_SHADOW_TILES;
    float2 planeDy = float2(dot(positionDy, right), dot(positionDy, up)) * HB_SHADOW_TILES;

    half4 atlas = SAMPLE_TEXTURE2D_GRAD(_HB_BrushAtlas, sampler_HB_BrushAtlas, plane, planeDx, planeDy);
    half2 warp = atlas.rg * 2.0h - 1.0h;

    float3 offset = (right * warp.x + up * warp.y) * strength;

    return positionWS + (offset - normalWS * dot(offset, normalWS));
}

float4 HB_ShadowCoordAt(float3 positionWS, float4 fallback)
{
#ifdef HB_MAIN_LIGHT_SHADOW_MAP
    return TransformWorldToShadowCoord(positionWS);
#else
    return fallback;
#endif
}

#ifdef HB_MAIN_LIGHT_SHADOW_MAP

float2 HB_ShadowDisc(float index, float count, float rotation)
{
    float theta = index * HB_SHADOW_GOLDEN + rotation;

    return float2(cos(theta), sin(theta)) * sqrt((index + 0.5) / count);
}

half HB_RawMainLightShadow(float4 shadowCoord)
{
    half4 params = GetMainLightShadowParams();
    params.x = 1.0h;

    return MainLightRealtimeShadow(shadowCoord, params, GetMainLightShadowSamplingData());
}

void HB_ShadowScale(float3 positionWS, out float perMetre, out float depthPerMetre)
{
#if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float4x4 toShadow = _MainLightWorldToShadow[(int)ComputeCascadeIndex(positionWS)];
#else
    float4x4 toShadow = _MainLightWorldToShadow[0];
#endif

    perMetre = length(float3(toShadow._m00, toShadow._m01, toShadow._m02));
    depthPerMetre = max(length(float3(toShadow._m20, toShadow._m21, toShadow._m22)), 1e-6);
}

half HB_ContactShadow(float4 shadowCoord, float3 positionWS, float2 screenUV, inout HBShadowProbe probe)
{
    float softness = HB_SHADOW_SOFTNESS;

    float perMetre, depthPerMetre;
    HB_ShadowScale(positionWS, perMetre, depthPerMetre);

    float count = HB_SHADOW_TAPS;
    float rotation = InterleavedGradientNoise(screenUV * _ScreenSize.xy, 0) * TWO_PI;

    float2 tile = max(HB_SHADOW_ATLAS, 1e-3);
    float2 margin = min(_MainLightShadowmapSize.xy * 1.5, tile * 0.25);
    float2 lowest = floor(shadowCoord.xy / tile) * tile + margin;
    float2 highest = lowest + tile - margin * 2.0;

    float search = softness * perMetre;
    float ignore = depthPerMetre * (0.03 + softness * 0.1);

    probe.radius = half(saturate(search / max(_MainLightShadowmapSize.x, 1e-9) * (1.0 / 16.0)));

    float blockerSum = 0.0;
    float blockerCount = 0.0;

    UNITY_LOOP
    for (float i = 0.0; i < count; i += 1.0)
    {
        float2 tap = clamp(shadowCoord.xy + HB_ShadowDisc(i, count, rotation) * search, lowest, highest);

        float depth = SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture, sampler_PointClamp, tap, 0).r;

    #if UNITY_REVERSED_Z
        float blocked = depth > shadowCoord.z + ignore ? 1.0 : 0.0;
    #else
        float blocked = depth < shadowCoord.z - ignore ? 1.0 : 0.0;
    #endif

        blockerSum += depth * blocked;
        blockerCount += blocked;
    }

    probe.blockers = half(blockerCount / count);

    half shadow = 0.0h;

    UNITY_BRANCH
    if (blockerCount < 0.5)
    {
        shadow = half(SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare,
                                              shadowCoord.xyz));
    }
    else if (blockerCount < count)
    {
        float gap = abs(blockerSum / blockerCount - shadowCoord.z) / depthPerMetre;
        float reach = HB_SHADOW_CONTACT > 0.0 ? saturate(gap / HB_SHADOW_CONTACT) : 1.0;

        probe.reach = half(reach);

        float radius = max(softness * reach * perMetre, perMetre * 0.002);

        half sum = 0.0h;

        UNITY_LOOP
        for (float j = 0.0; j < count; j += 1.0)
        {
            float2 tap = clamp(shadowCoord.xy + HB_ShadowDisc(j, count, rotation) * radius, lowest, highest);

            sum += half(SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare,
                                                float3(tap, shadowCoord.z)));
        }

        shadow = sum / half(count);
    }

    return shadow;
}

#endif

half HB_MainLightShadow(float4 shadowCoord, float3 positionWS, half4 shadowMask, float2 screenUV,
                        out HBShadowProbe probe)
{
    probe = HB_NoShadowProbe();

    half realtime = 1.0h;

#ifdef HB_MAIN_LIGHT_SHADOW_MAP
    bool contact = HB_SHADOW_SOFTNESS > 0.0 && _MainLightShadowmapSize.z >= 2.0
                && distance(positionWS, GetCameraPositionWS()) <= HB_SHADOW_REACH;

    #if defined(HB_STYLE_DEBUG)
    contact = contact && HB_DEBUG_VIEW != HB_DEBUG_SHADOW_UNFILTERED;
    #endif

    UNITY_BRANCH
    if (BEYOND_SHADOW_FAR(shadowCoord))
        realtime = 1.0h;
    else if (contact)
        realtime = HB_ContactShadow(shadowCoord, positionWS, screenUV, probe);
    else
        realtime = HB_RawMainLightShadow(shadowCoord);
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    realtime = MainLightRealtimeShadow(shadowCoord);
#endif

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    realtime = LerpWhiteTo(realtime, GetMainLightShadowParams().x);

    half fade = GetMainLightShadowFade(positionWS);
#else
    half fade = 1.0h;
#endif

    probe.realtime = realtime;

#if defined(CALCULATE_BAKED_SHADOWS)
    half baked = BakedShadow(shadowMask, _MainLightOcclusionProbes);
#else
    half baked = 1.0h;
#endif

    return MixRealtimeAndBakedShadows(realtime, baked, fade);
}

Light HB_GetAdditionalLight(uint index, float3 positionWS, float3 positionDx, float3 positionDy, half3 normalWS,
                            half4 shadowMask, AmbientOcclusionFactor aoFactor, float3 strokeOffset, bool brushed)
{
#if USE_CLUSTER_LIGHT_LOOP
    int lightIndex = index;
#else
    int lightIndex = GetPerObjectLightIndex(index);
#endif

    Light light = GetAdditionalPerObjectLight(lightIndex, positionWS);

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[lightIndex].occlusionProbeChannels;
#else
    half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
#endif

    float3 shadowPosition = positionWS + strokeOffset;

    if (!brushed && GetAdditionalLightShadowParams(lightIndex).w >= 0.0h)
        shadowPosition = HB_BrushShadowPosition(positionWS, positionDx, positionDy, normalWS, light.direction);

    light.shadowAttenuation = AdditionalLightShadow(lightIndex, shadowPosition, light.direction, shadowMask,
                                                    occlusionProbeChannels);

#if defined(_LIGHT_COOKIES)
    light.color *= SampleAdditionalLightCookie(lightIndex, positionWS);
#endif

#if defined(_SCREEN_SPACE_OCCLUSION)
    light.color *= aoFactor.directAmbientOcclusion;
#endif

    return light;
}

#if defined(HB_STYLE_DEBUG)
bool HB_ShadowDebug(float3 positionWS, half attenuation, HBShadowProbe probe, out half3 color)
{
    int view = HB_DEBUG_VIEW;

    color = half3(attenuation, attenuation, attenuation);

    if (view < HB_DEBUG_SHADOW_ATTENUATION || view > HB_DEBUG_SHADOW_RADIUS)
        return false;

    if (view == HB_DEBUG_SHADOW_FILTER)
    {
        color = half3(HB_SHADOW_SOFTNESS / HB_SHADOW_MAX_SOFTNESS, HB_SHADOW_CONTACT / HB_SHADOW_MAX_CONTACT,
                      HB_SHADOW_TAPS / HB_SHADOW_MAX_TAPS);
        return true;
    }

#ifdef HB_MAIN_LIGHT_SHADOW_MAP
    if (view == HB_DEBUG_SHADOW_UNFILTERED)
    {
        color = half3(probe.realtime, probe.realtime, probe.realtime);
    }
    else if (view == HB_DEBUG_SHADOW_BLOCKERS)
    {
        color = half3(probe.blockers, probe.reach, 0.0h);
    }
    else if (view == HB_DEBUG_SHADOW_RADIUS)
    {
        color = half3(probe.radius, probe.radius * probe.radius, 0.0h);
    }
    else if (view == HB_DEBUG_SHADOW_CASCADES)
    {
    #if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
        int cascade = (int)ComputeCascadeIndex(positionWS);
    #else
        int cascade = 0;
    #endif

        color = cascade == 0 ? half3(0.8h, 0.2h, 0.2h)
              : cascade == 1 ? half3(0.2h, 0.8h, 0.2h)
              : cascade == 2 ? half3(0.2h, 0.4h, 0.9h)
                             : half3(0.9h, 0.8h, 0.2h);
    }
#endif

    return true;
}
#endif

#endif
