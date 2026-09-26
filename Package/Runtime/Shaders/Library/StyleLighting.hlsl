#ifndef HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED
#define HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"
#include "StyleBrush.hlsl"
#include "StyleShadow.hlsl"

half3 HB_LightResponse(Light light, half3 normalWS, HiddenBullStyleData style, half terminatorOffset)
{
    half wrap = saturate(style.diffuseWrap);
    half inverseWrap = rcp(1.0h + wrap);

    half NdotL = dot(normalWS, light.direction) + terminatorOffset;
    half lambert = saturate((NdotL + wrap) * inverseWrap);

    half tone = HB_ToneBands(lambert * light.shadowAttenuation, wrap * inverseWrap,
                             style.halfTone, style.diffuseSoftness);

    return light.color * (light.distanceAttenuation * tone);
}

half3 HB_RimLight(half3 normalWS, half3 viewDirectionWS, HiddenBullStyleData style, half occlusion)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = PositivePow(fresnel, max(style.rimPower, HB_EPSILON));
    return rim * style.rimIntensity * style.rimColor * HB_AmbientSkyColor() * occlusion;
}

half4 HiddenBullFragmentLit(InputData inputData, SurfaceData surfaceData, HiddenBullStyleData style,
                            HiddenBullBrushSample brush, out half sunVisibility, out half lightReach)
{
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

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

#ifdef _HB_BRUSH
    HB_StrokeShadowCoord(inputData.shadowCoord, inputData.positionWS + brush.offset);
#else
    HB_BrushShadowCoord(inputData.shadowCoord, inputData.positionWS, inputData.normalWS);
#endif

    Light mainLight = GetMainLight();

    mainLight.shadowAttenuation = HB_MainLightShadow(
        inputData.shadowCoord, inputData.positionWS, shadowMask, inputData.normalizedScreenSpaceUV);

    if (dot(_HB_KeyDirection.xyz, _HB_KeyDirection.xyz) > 0.5)
    {
        mainLight.direction = half3(_HB_KeyDirection.xyz);
        mainLight.color = _HB_KeyColor.rgb;
        mainLight.shadowAttenuation = lerp(1.0h, mainLight.shadowAttenuation, half(_HB_KeyColor.a));
    }

    if (HB_SHADOW_DEBUG > 0.5)
    {
        half3 debugColor;

        HB_ShadowDebug(inputData.shadowCoord, inputData.positionWS,
                       inputData.normalizedScreenSpaceUV, mainLight.shadowAttenuation, debugColor);

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

    half3 reflectVector = reflect(-inputData.viewDirectionWS, formNormal);
    half3 environmentSpecular = HB_GradientAmbient(reflectVector) * occlusion;
    half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS)));

    half3 color = EnvironmentBRDF(brdfData, ambient, environmentSpecular, fresnelTerm);
#else
    half3 color = ambient * surfaceData.albedo;
#endif

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        half3 radiance = HB_LightResponse(mainLight, toneNormal, style, terminatorOffset);

        color += radiance * surfaceData.albedo;
#ifdef _HB_SPECULAR
        color += radiance * brdfData.specular *
                 DirectBRDFSpecular(brdfData, inputData.normalWS, mainLight.direction, inputData.viewDirectionWS);
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
            half3 radiance = HB_LightResponse(light, toneNormal, style, terminatorOffset);

            color += radiance * surfaceData.albedo;
    #ifdef _HB_SPECULAR
            color += radiance * brdfData.specular *
                     DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
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
            half3 radiance = HB_LightResponse(light, toneNormal, style, terminatorOffset);

            color += radiance * surfaceData.albedo;
    #ifdef _HB_SPECULAR
            color += radiance * brdfData.specular *
                     DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    #endif
        }
    LIGHT_LOOP_END
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    color += inputData.vertexLighting * surfaceData.albedo;
#endif

#ifdef _HB_RIM
    color += HB_RimLight(inputData.normalWS, inputData.viewDirectionWS, style, occlusion);
#endif

    color += surfaceData.emission;

    return half4(color, surfaceData.alpha);
}

#endif
