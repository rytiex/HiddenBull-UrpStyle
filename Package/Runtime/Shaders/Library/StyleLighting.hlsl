#ifndef HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED
#define HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"
#include "StyleBrush.hlsl"
#include "StyleShadow.hlsl"
#include "StyleShadowTint.hlsl"
#include "StyleSky.hlsl"

#define HB_SELF_SHADOW_FADE 0.05h

#define HB_SPECULAR_SHEEN       0.15h
#define HB_SPECULAR_CORE        0.5h
#define HB_SPECULAR_SHEEN_LEVEL 0.3h
#define HB_SPECULAR_SHEEN_MOST  1.5h
#define HB_SPECULAR_BRUSH       1.0h
#define HB_REFLECTION_HORIZON   0.04h
#ifndef HB_WET_FILM_SMOOTHNESS
    #define HB_WET_FILM_SMOOTHNESS 0.85h
#endif

half3 HB_SampleProbeGI(float3 positionWS, half3 normalWS, half3 viewDirectionWS, float2 positionSS,
                       out half4 shadowMask)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float4 probeOcclusion;
    half3 gi = SampleProbeVolumePixel(half3(0.0h, 0.0h, 0.0h), GetAbsolutePositionWS(positionWS), normalWS,
                                      viewDirectionWS, positionSS, float4(1.0, 1.0, 1.0, 1.0), probeOcclusion);
    shadowMask = half4(probeOcclusion);
    return gi;
#else
    shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
    return SampleSH(normalWS);
#endif
}

half3 HB_LightResponse(Light light, half3 normalWS, half3 surfaceNormalWS, HiddenBullStyleData style,
                       half terminatorOffset)
{
    half wrap = saturate(style.diffuseWrap);
    half inverseWrap = rcp(1.0h + wrap);

    half NdotL = dot(normalWS, light.direction) + terminatorOffset;
    half lambert = saturate((NdotL + wrap) * inverseWrap);

    half selfShadow = saturate(dot(surfaceNormalWS, light.direction) * rcp(HB_SELF_SHADOW_FADE));

    half tone = HB_ToneBands(lambert * light.shadowAttenuation * selfShadow, wrap * inverseWrap,
                             style.halfTone, style.diffuseSoftness);

    return light.color * (light.distanceAttenuation * tone);
}

half3 HB_RimLight(half3 normalWS, half3 viewDirectionWS, HiddenBullStyleData style, half occlusion)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = PositivePow(fresnel, max(style.rimPower, HB_EPSILON));
    return rim * style.rimIntensity * style.rimColor * HB_AmbientSkyColor() * occlusion;
}

half3 HB_ProbeReflection(half3 reflectVector, float3 positionWS, half mip, float2 screenUV, out half weight)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    float total = 0.0;

#if USE_CLUSTER_LIGHT_LOOP && CLUSTER_HAS_REFLECTION_PROBES
    uint probeIndex;
    ClusterIterator it = ClusterInit(screenUV, positionWS, 1);

    [loop] while (ClusterNext(it, probeIndex) && total < 0.99)
    {
        probeIndex -= URP_FP_PROBES_BEGIN;

        float probeWeight = min(CalculateProbeWeight(positionWS, urp_ReflProbes_BoxMin[probeIndex],
                                                     urp_ReflProbes_BoxMax[probeIndex]), 1.0 - total);

        half3 direction = reflectVector;

        if (_REFLECTION_PROBE_BOX_PROJECTION)
            direction = BoxProjectedCubemapDirection(reflectVector, positionWS, urp_ReflProbes_ProbePosition[probeIndex],
                                                     urp_ReflProbes_BoxMin[probeIndex], urp_ReflProbes_BoxMax[probeIndex]);

        half probeMip = min(mip, half(abs(urp_ReflProbes_ProbePosition[probeIndex].w) - 1.0));
        float2 uv = saturate(PackNormalOctQuadEncode(direction) * 0.5 + 0.5);

        float mip0 = floor(probeMip);
        float4 scaleOffset0 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0];
        float4 scaleOffset1 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0 + 1];

        half3 sample0 = SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp,
                                             uv * scaleOffset0.xy + scaleOffset0.zw, 0.0).rgb;
        half3 sample1 = SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp,
                                             uv * scaleOffset1.xy + scaleOffset1.zw, 0.0).rgb;

        irradiance += probeWeight * lerp(sample0, sample1, probeMip - mip0);
        total += probeWeight;
    }
#elif !USE_CLUSTER_LIGHT_LOOP
    if (_REFLECTION_PROBE_BLENDING)
    {
        half volume0 = CalculateProbeVolumeSqrMagnitude(unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
        half volume1 = CalculateProbeVolumeSqrMagnitude(unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
        half volumeDiff = volume0 - volume1;
        float importance = unity_SpecCube1_BoxMin.w;

        bool dominant0 = importance > 0.0 || (importance == 0.0 && volumeDiff < -0.0001h);
        bool dominant1 = importance < 0.0 || (importance == 0.0 && volumeDiff > 0.0001h);

        float desired0 = CalculateProbeWeight(positionWS, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
        float desired1 = CalculateProbeWeight(positionWS, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

        float weight0 = dominant1 ? min(desired0, 1.0 - desired1) : desired0;
        float weight1 = dominant0 ? min(desired1, 1.0 - desired0) : desired1;

        total = weight0 + weight1;
        weight0 /= max(total, 1.0);
        weight1 /= max(total, 1.0);
        total = min(total, 1.0);

        if (weight0 > 0.01)
        {
            half3 direction = _REFLECTION_PROBE_BOX_PROJECTION
                ? BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition,
                                               unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax)
                : reflectVector;

            irradiance += weight0 * DecodeHDREnvironment(
                SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, mip), unity_SpecCube0_HDR);
        }

        if (weight1 > 0.01)
        {
            half3 direction = _REFLECTION_PROBE_BOX_PROJECTION
                ? BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube1_ProbePosition,
                                               unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax)
                : reflectVector;

            irradiance += weight1 * DecodeHDREnvironment(
                SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, direction, mip), unity_SpecCube1_HDR);
        }
    }
#endif

    weight = half(total);

    return irradiance;
}

half3 HB_EnvironmentReflection(half3 reflectVector, half3 normalWS, float3 positionWS, half perceptualRoughness,
                               float2 screenUV, half skyVisibility)
{
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half weight;

    half3 probes = HB_ProbeReflection(reflectVector, positionWS, mip, screenUV, weight);

    half roughness = perceptualRoughness * perceptualRoughness;
    half3 skyDirection = normalize(lerp(reflectVector, normalWS, roughness));

    half horizon = smoothstep(-HB_REFLECTION_HORIZON, HB_REFLECTION_HORIZON, skyDirection.y);
    half3 sky = lerp(HB_GradientAmbient(skyDirection), HB_SkySample(skyDirection, max(skyDirection.y, 0.0h)),
                     horizon) * skyVisibility;

    return probes + sky * (1.0h - weight);
}

half3 HB_DirectSpecular(BRDFData brdfData, half3 normalWS, half3 lightDirection, half3 viewDirection,
                        half softness, half offset)
{
    half term = DirectBRDFSpecular(brdfData, normalWS, lightDirection, viewDirection);
    half peak = rcp(max(brdfData.roughness2 * brdfData.normalizationTerm, HB_EPSILON));

    half level = saturate(term / peak);
    half shaped = level * (1.0h + offset * HB_SPECULAR_BRUSH);
    half width = max(softness * 0.1h, 0.01h);

    half sheen = smoothstep(HB_SPECULAR_SHEEN - width, HB_SPECULAR_SHEEN + width, shaped);
    half core = smoothstep(HB_SPECULAR_CORE - width, HB_SPECULAR_CORE + width, shaped);

    half haze = min(HB_SPECULAR_SHEEN_LEVEL, HB_SPECULAR_SHEEN_MOST / peak);
    half banded = lerp(min(level, haze), lerp(haze, 1.0h, core), sheen);

    return brdfData.specular * (peak * banded);
}

half4 HiddenBullFragmentLit(InputData inputData, SurfaceData surfaceData, HiddenBullStyleData style,
                            HiddenBullBrushSample brush, half wet, out half sunVisibility, out half lightReach)
{
    half3 surfaceNormal = inputData.normalWS;

#ifdef _HB_BRUSH
    half terminatorOffset = brush.coverage * style.brushShading;
    half ambientOffset = brush.coverage * style.brushAmbient;

    half3 formNormal = SafeNormalize(
        inputData.normalWS - brush.spine * (style.brushRelief * HB_BRUSH_FORM_RELIEF));

    inputData.normalWS = SafeNormalize(inputData.normalWS - brush.spine * style.brushRelief);

    half3 toneNormal = SafeNormalize(inputData.normalWS + brush.normalShift);

    surfaceData.albedo *= saturate(1.0h + min(brush.coverage, 0.0h) * style.brushAlbedo);
#else
    half terminatorOffset = 0.0h;
    half ambientOffset = 0.0h;
    half3 formNormal = inputData.normalWS;
    half3 toneNormal = inputData.normalWS;
#endif

#ifdef HB_BAKED_INPUT
    half4 shadowMask = inputData.shadowMask;
#else
    half4 shadowMask = CalculateShadowMask(inputData);
#endif
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

#ifdef _HB_BRUSH
    HB_StrokeShadowCoord(inputData.shadowCoord, inputData.positionWS + brush.offset);
#else
    HB_BrushShadowCoord(inputData.shadowCoord, inputData.positionWS, inputData.normalWS);
#endif

    Light mainLight = GetMainLight();

    half3 shadowProbe;
    half shadowRealtime;

    mainLight.shadowAttenuation = HB_MainLightShadow(inputData.shadowCoord, inputData.positionWS, shadowMask,
                                                     inputData.normalizedScreenSpaceUV, shadowProbe, shadowRealtime);

    if (dot(_HB_KeyDirection.xyz, _HB_KeyDirection.xyz) > 0.5)
    {
        mainLight.direction = half3(_HB_KeyDirection.xyz);
        mainLight.color = _HB_KeyColor.rgb;
    }

    half4 shadowTint = HB_ShadowTint(inputData.positionWS + brush.offset);

    mainLight.shadowAttenuation *= shadowTint.a;
    mainLight.color *= shadowTint.rgb;

    if (HB_SHADOW_DEBUG > 0.5)
    {
        half3 debugColor;

        HB_ShadowDebug(inputData.positionWS, mainLight.shadowAttenuation, shadowProbe, shadowRealtime, debugColor);

        sunVisibility = mainLight.shadowAttenuation;
        lightReach = 1.0h;

        return half4(debugColor, 1.0h);
    }

#ifdef _LIGHT_COOKIES
    mainLight.color *= SampleMainLightCookie(inputData.positionWS);
#endif

#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
    mainLight.color *= aoFactor.directAmbientOcclusion;
#endif

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    half occlusion = surfaceData.occlusion * aoFactor.indirectAmbientOcclusion;

    half3 ambient = HB_ResolveAmbient(formNormal, inputData.bakedGI, ambientOffset,
                                      style.bakedTones, style.bakedSoftness,
                                      lightReach) * occlusion;

    sunVisibility = mainLight.shadowAttenuation;

#ifdef _HB_SPECULAR
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half3 environmentSpecular = HB_EnvironmentReflection(reflectVector, inputData.normalWS, inputData.positionWS,
                                                         brdfData.perceptualRoughness,
                                                         inputData.normalizedScreenSpaceUV, lightReach) * occlusion;
    half gloss = 1.0h - brdfData.perceptualRoughness;
    half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS))) * (gloss * gloss);

    half3 color = EnvironmentBRDF(brdfData, ambient, environmentSpecular, fresnelTerm);
    half3 diffuse = brdfData.diffuse;
#else
    half3 color = ambient * surfaceData.albedo;
    half3 diffuse = surfaceData.albedo;

    BRDFData brdfData = (BRDFData)0;

    UNITY_BRANCH
    if (wet > 0.0h)
    {
        SurfaceData film = surfaceData;
        film.albedo = half3(0.0h, 0.0h, 0.0h);
        film.metallic = 0.0h;
        film.smoothness = HB_WET_FILM_SMOOTHNESS;
        film.alpha = 1.0h;

        InitializeBRDFData(film, brdfData);

        half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
        half3 environment = HB_EnvironmentReflection(reflectVector, inputData.normalWS, inputData.positionWS,
                                                     brdfData.perceptualRoughness,
                                                     inputData.normalizedScreenSpaceUV, lightReach) * occlusion;
        half gloss = 1.0h - brdfData.perceptualRoughness;
        half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS))) * (gloss * gloss);

        color += EnvironmentBRDFSpecular(brdfData, fresnelTerm) * environment * wet;
    }
#endif

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        half3 radiance = HB_LightResponse(mainLight, toneNormal, surfaceNormal, style, terminatorOffset);

        color += radiance * diffuse;
#ifdef HB_HAS_EXTRA_LIGHT
        color += HB_ExtraLight(mainLight, inputData, surfaceData, toneNormal);
#endif
#ifdef _HB_SPECULAR
        color += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, mainLight.direction,
                                              inputData.viewDirectionWS, style.diffuseSoftness, terminatorOffset);
#else
        UNITY_BRANCH
        if (wet > 0.0h)
        {
            color += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, mainLight.direction,
                                                  inputData.viewDirectionWS, style.diffuseSoftness,
                                                  terminatorOffset) * wet;
        }
#endif
    }

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

        Light light = HB_GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, brush.offset);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, toneNormal, surfaceNormal, style, terminatorOffset);

            color += radiance * diffuse;
#ifdef HB_HAS_EXTRA_LIGHT
            color += HB_ExtraLight(light, inputData, surfaceData, toneNormal);
#endif
    #ifdef _HB_SPECULAR
            color += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, light.direction,
                                                  inputData.viewDirectionWS, style.diffuseSoftness, terminatorOffset);
    #endif
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = HB_GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor, brush.offset);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, toneNormal, surfaceNormal, style, terminatorOffset);

            color += radiance * diffuse;
#ifdef HB_HAS_EXTRA_LIGHT
            color += HB_ExtraLight(light, inputData, surfaceData, toneNormal);
#endif
    #ifdef _HB_SPECULAR
            color += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, light.direction,
                                                  inputData.viewDirectionWS, style.diffuseSoftness, terminatorOffset);
    #endif
        }
    LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    color += inputData.vertexLighting * diffuse;
#endif

#ifdef _HB_RIM
    color += HB_RimLight(inputData.normalWS, inputData.viewDirectionWS, style, occlusion);
#endif

    color += surfaceData.emission;

    return half4(color, surfaceData.alpha);
}

#endif
