#ifndef HIDDENBULL_URPSTYLE_SHADOW_INCLUDED
#define HIDDENBULL_URPSTYLE_SHADOW_INCLUDED

#include "StyleCommon.hlsl"
#include "StyleBrush.hlsl"

float4 _HB_ShadowBrush;
float4 _HB_ShadowFilter;

#define HB_SHADOW_BRUSH    _HB_ShadowBrush.x
#define HB_SHADOW_TILES    _HB_ShadowBrush.y
#define HB_SHADOW_ATLAS    _HB_ShadowBrush.zw

#define HB_SHADOW_SOFTNESS _HB_ShadowFilter.x
#define HB_SHADOW_CONTACT  _HB_ShadowFilter.y
#define HB_SHADOW_TAPS     _HB_ShadowFilter.z

#define HB_SHADOW_GOLDEN   2.39996323

float3 HB_BrushShadowPosition(float3 positionWS, half3 normalWS, half3 direction)
{
    half strength = half(HB_SHADOW_BRUSH);

    if (strength <= 0.0h || HB_BRUSH_ATLAS_BOUND < 0.5h || dot(direction, direction) < 0.5h)
        return positionWS;

    half3 reference = abs(direction.y) > 0.99h ? half3(0.0h, 0.0h, 1.0h)
                                               : half3(0.0h, 1.0h, 0.0h);

    half3 right = normalize(cross(direction, reference));
    half3 up = cross(right, direction);

    float2 plane = float2(dot(positionWS, right), dot(positionWS, up)) * HB_SHADOW_TILES;

    half4 atlas = SAMPLE_TEXTURE2D(_HB_BrushAtlas, sampler_HB_BrushAtlas, plane);
    half2 warp = atlas.rg * 2.0h - 1.0h;

    float3 offset = (right * warp.x + up * warp.y) * strength;

    return positionWS + (offset - normalWS * dot(offset, normalWS));
}

void HB_BrushShadowCoord(inout float4 shadowCoord, float3 positionWS, half3 normalWS)
{
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float3 moved = HB_BrushShadowPosition(positionWS, normalWS, half3(_HB_KeyDirection.xyz));

    if (any(moved != positionWS))
        shadowCoord = TransformWorldToShadowCoord(moved);
#endif
}

#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)

float2 HB_ShadowDisc(float index, float count, float rotation)
{
    float theta = index * HB_SHADOW_GOLDEN + rotation;

    return float2(cos(theta), sin(theta)) * sqrt((index + 0.5) / count);
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

half HB_ContactShadow(float4 shadowCoord, float3 positionWS, float2 screenUV)
{
    float softness = HB_SHADOW_SOFTNESS;

    if (softness <= 0.0)
        return MainLightRealtimeShadow(shadowCoord);

    float perMetre, depthPerMetre;
    HB_ShadowScale(positionWS, perMetre, depthPerMetre);

    float count = HB_SHADOW_TAPS;
    float rotation = InterleavedGradientNoise(screenUV * _ScreenSize.xy, 0) * TWO_PI;

    float2 tile = HB_SHADOW_ATLAS;
    float2 margin = _MainLightShadowmapSize.xy * 1.5;
    float2 lowest = floor(shadowCoord.xy / tile) * tile + margin;
    float2 highest = lowest + tile - margin * 2.0;

    float search = softness * perMetre;
    float ignore = depthPerMetre * (0.03 + softness * 0.1);

    float blockerSum = 0.0;
    float blockerCount = 0.0;

    UNITY_LOOP
    for (float i = 0.0; i < count; i += 1.0)
    {
        float2 tap = clamp(shadowCoord.xy + HB_ShadowDisc(i, count, rotation) * search,
                           lowest, highest);

        float depth = SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture, sampler_PointClamp,
                                           tap, 0).r;

    #if UNITY_REVERSED_Z
        float blocked = depth > shadowCoord.z + ignore ? 1.0 : 0.0;
    #else
        float blocked = depth < shadowCoord.z - ignore ? 1.0 : 0.0;
    #endif

        blockerSum += depth * blocked;
        blockerCount += blocked;
    }

    if (blockerCount < 0.5)
        return 1.0h;

    float gap = abs(blockerSum / blockerCount - shadowCoord.z) / depthPerMetre;

    float reach = HB_SHADOW_CONTACT > 0.0 ? saturate(gap / HB_SHADOW_CONTACT) : 1.0;

    float radius = max(softness * reach * perMetre, perMetre * 0.002);

    half sum = 0.0h;

    UNITY_LOOP
    for (float j = 0.0; j < count; j += 1.0)
    {
        float2 tap = clamp(shadowCoord.xy + HB_ShadowDisc(j, count, rotation) * radius,
                           lowest, highest);

        sum += half(SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare,
                                            float3(tap, shadowCoord.z)));
    }

    return sum / half(count);
}

#endif

half HB_MainLightShadow(float4 shadowCoord, float3 positionWS, half4 shadowMask, float2 screenUV)
{
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    half realtime = BEYOND_SHADOW_FAR(shadowCoord)
        ? 1.0h
        : HB_ContactShadow(shadowCoord, positionWS, screenUV);
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    half realtime = MainLightRealtimeShadow(shadowCoord);
#else
    half realtime = 1.0h;
#endif

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    realtime = LerpWhiteTo(realtime, GetMainLightShadowParams().x);

    half fade = GetMainLightShadowFade(positionWS);
#else
    half fade = 1.0h;
#endif

#if defined(CALCULATE_BAKED_SHADOWS)
    half baked = BakedShadow(shadowMask, _MainLightOcclusionProbes);
#else
    half baked = 1.0h;
#endif

    return MixRealtimeAndBakedShadows(realtime, baked, fade);
}

Light HB_GetAdditionalLight(uint index, InputData inputData, half4 shadowMask,
                            AmbientOcclusionFactor aoFactor)
{
#if USE_CLUSTER_LIGHT_LOOP
    int lightIndex = index;
#else
    int lightIndex = GetPerObjectLightIndex(index);
#endif

    Light light = GetAdditionalPerObjectLight(lightIndex, inputData.positionWS);

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[lightIndex].occlusionProbeChannels;
#else
    half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
#endif

    float3 shadowPosition = HB_BrushShadowPosition(inputData.positionWS, inputData.normalWS,
                                                   light.direction);

    light.shadowAttenuation = AdditionalLightShadow(lightIndex, shadowPosition, light.direction,
                                                    shadowMask, occlusionProbeChannels);

#if defined(_LIGHT_COOKIES)
    light.color *= SampleAdditionalLightCookie(lightIndex, inputData.positionWS);
#endif

#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
    light.color *= aoFactor.directAmbientOcclusion;
#endif

    return light;
}

#endif
