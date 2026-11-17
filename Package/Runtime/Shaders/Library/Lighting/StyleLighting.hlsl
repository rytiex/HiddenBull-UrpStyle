#ifndef HB_STYLE_LIGHTING_INCLUDED
#define HB_STYLE_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Shadow/StyleShadow.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleEnvironmentSky.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleReflection.hlsl"

#define HB_SELF_SHADOW_FADE     0.05h
#define HB_WET_FILM_SMOOTHNESS  0.85h

#if defined(HB_DECAL_SURFACE) && defined(_DBUFFER) && defined(_DBUFFER_MRT3)
#define HB_DECAL_SHEEN

half3 HB_DecalSurface(float2 screenUV)
{
    half4 maos = half4(LOAD_TEXTURE2D_X(_DBufferTexture2, uint2(screenUV * _ScreenSize.xy)));
    half share = 1.0h - maos.w;
    half inverse = rcp(max(share, half(HB_EPSILON)));

    return half3(maos.x * inverse, maos.z * inverse, share);
}
#endif

half3 HB_LightResponse(Light light, half3 normalWS, half3 surfaceNormalWS, HBStyleData style, half terminatorOffset)
{
    half wrap = saturate(style.diffuseWrap);
    half inverseWrap = rcp(1.0h + wrap);

#ifdef HB_VOLUME_LIGHT
    half lambert = 1.0h;
    half selfShadow = 1.0h;
#else
    half NdotL = dot(normalWS, light.direction) + terminatorOffset;
    half lambert = saturate((NdotL + wrap) * inverseWrap);

    half selfShadow = saturate(dot(surfaceNormalWS, light.direction) * rcp(HB_SELF_SHADOW_FADE));
#endif

    half tone = HB_ToneBands(lambert * light.shadowAttenuation * selfShadow, wrap * inverseWrap,
                             style.halfTone, style.diffuseSoftness);

    return light.color * (light.distanceAttenuation * tone);
}

half3 HB_RimLight(half3 normalWS, half3 viewDirectionWS, HBStyleData style, half occlusion)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = PositivePow(fresnel, max(style.rimPower, half(HB_EPSILON)));

    return rim * style.rimIntensity * style.rimColor * HB_AmbientSkyColor() * occlusion;
}

#ifdef HB_OVERLAY_SHADOW
float _HB_OverlayShadow;

float3 HB_OverlayShadowPosition(float3 shadowPosition, float3 positionWS)
{
    float3 away = positionWS - _WorldSpaceCameraPos;

    return dot(away, away) < _HB_OverlayShadow ? _WorldSpaceCameraPos : shadowPosition;
}
#endif

half3 HB_LightContribution(Light light, half3 toneNormal, half3 surfaceNormal, HBStyleData style,
                           half terminatorOffset, half3 diffuse, BRDFData brdfData, InputData inputData,
                           SurfaceData surfaceData, half wet, half gloss)
{
    half3 radiance = HB_LightResponse(light, toneNormal, surfaceNormal, style, terminatorOffset);
    half3 colour = radiance * diffuse;

#ifdef HB_HAS_EXTRA_LIGHT
    colour += HB_ExtraLight(light, inputData, surfaceData, toneNormal);
#endif

#if defined(_HB_SPECULAR)
    colour += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS,
                                           style.diffuseSoftness, terminatorOffset) * gloss;
#elif defined(HB_WEATHER) || defined(HB_DECAL_SHEEN)
    UNITY_BRANCH
    if (wet > 0.0h)
    {
        colour += radiance * HB_DirectSpecular(brdfData, inputData.normalWS, light.direction,
                                               inputData.viewDirectionWS, style.diffuseSoftness, terminatorOffset)
                * (wet * gloss);
    }
#endif

    return colour;
}

half4 HB_StyleLit(InputData inputData, SurfaceData surfaceData, HBStyleData style, HBBrushSample brush,
                  bool brushed, half wet)
{
    half3 surfaceNormal = inputData.normalWS;

    half terminatorOffset = 0.0h;
    half ambientOffset = 0.0h;
    half3 formNormal = inputData.normalWS;
    half3 toneNormal = inputData.normalWS;

    if (brushed)
    {
        terminatorOffset = brush.coverage * style.brushShading;
        ambientOffset = brush.coverage * style.brushAmbient;

        formNormal = SafeNormalize(inputData.normalWS - brush.spine * (style.brushRelief * HB_BRUSH_FORM_RELIEF));
        inputData.normalWS = SafeNormalize(inputData.normalWS - brush.spine * style.brushRelief);
        toneNormal = SafeNormalize(inputData.normalWS + brush.normalShift);

        surfaceData.albedo *= saturate(1.0h + min(brush.coverage, 0.0h) * style.brushAlbedo);
    }

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

    float3 positionDx = ddx(inputData.positionWS);
    float3 positionDy = ddy(inputData.positionWS);
    half3 geometry = half3(normalize(cross(positionDy, positionDx)));

    geometry = dot(geometry, inputData.normalWS) < 0.0h ? -geometry : geometry;

    half3 mainDirection = half3(_MainLightPosition.xyz);
    float3 shadowPosition = brushed ? inputData.positionWS + brush.offset
                                    : HB_BrushShadowPosition(inputData.positionWS, positionDx, positionDy,
                                                             inputData.normalWS, mainDirection);

#ifdef HB_SHADOW_LIFT
    shadowPosition += _MainLightPosition.xyz * HB_SHADOW_LIFT;
#endif

#ifdef HB_OVERLAY_SHADOW
    shadowPosition = HB_OverlayShadowPosition(shadowPosition, inputData.positionWS);
#endif

    inputData.shadowCoord = HB_ShadowCoordAt(shadowPosition, inputData.shadowCoord);

    Light mainLight = GetMainLight();

    half3 shadowTint = HB_TransparentShadowTint(shadowPosition, inputData.positionWS);

    HBShadowProbe probe;
    mainLight.shadowAttenuation = HB_MainLightShadow(inputData.shadowCoord, inputData.positionWS, shadowMask,
                                                     inputData.normalizedScreenSpaceUV, probe);

#if defined(HB_STYLE_DEBUG)
    half3 debugColour;

    if (HB_DEBUG_VIEW == HB_DEBUG_SHADOW_TINT)
        return half4(shadowTint, 1.0h);

    if (HB_ShadowDebug(inputData.positionWS, mainLight.shadowAttenuation, probe, debugColour))
        return half4(debugColour, 1.0h);
#endif

#ifdef _LIGHT_COOKIES
    mainLight.color *= SampleMainLightCookie(inputData.positionWS);
#endif

    mainLight.color *= shadowTint;

#if defined(_SCREEN_SPACE_OCCLUSION)
    mainLight.color *= aoFactor.directAmbientOcclusion;
#endif

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    half occlusion = surfaceData.occlusion * aoFactor.indirectAmbientOcclusion;

    half lightReach;
    half3 ambient = HB_ResolveAmbient(formNormal, inputData.bakedGI, ambientOffset, style.bakedTones,
                                      style.bakedSoftness, lightReach) * occlusion;

#ifdef _HB_SPECULAR
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half3 environmentSpecular = HB_EnvironmentReflection(reflectVector, inputData.normalWS, inputData.positionWS,
                                                         brdfData.perceptualRoughness,
                                                         inputData.normalizedScreenSpaceUV, lightReach);
    half4 screen = HB_ScreenReflectAbove(inputData.positionWS, reflectVector, geometry, brdfData.perceptualRoughness);

    environmentSpecular = lerp(environmentSpecular, screen.rgb, screen.a) * occlusion;
    half gloss = 1.0h - brdfData.perceptualRoughness;
    half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS))) * (gloss * gloss);

    half3 colour = EnvironmentBRDF(brdfData, ambient, environmentSpecular, fresnelTerm);
    half3 diffuse = brdfData.diffuse;
#else
    BRDFData brdfData = (BRDFData)0;
    SurfaceData film = surfaceData;
    film.albedo = half3(0.0h, 0.0h, 0.0h);
    film.metallic = 0.0h;
    film.smoothness = HB_WET_FILM_SMOOTHNESS;
    film.alpha = 1.0h;

    #ifdef HB_DECAL_SHEEN
    half3 decal = HB_DecalSurface(inputData.normalizedScreenSpaceUV);

    UNITY_BRANCH
    if (decal.z > wet)
    {
        film.albedo = surfaceData.albedo;
        film.metallic = decal.x;
        film.smoothness = decal.y;
        surfaceData.albedo *= lerp(1.0h, OneMinusReflectivityMetallic(decal.x), decal.z);
        wet = decal.z;
    }
    #endif

    half3 colour = ambient * surfaceData.albedo;
    half3 diffuse = surfaceData.albedo;

    #if defined(HB_WEATHER) || defined(HB_DECAL_SHEEN)
    UNITY_BRANCH
    if (wet > 0.0h)
    {
        InitializeBRDFData(film, brdfData);

        half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
        half3 environment = HB_EnvironmentReflection(reflectVector, inputData.normalWS, inputData.positionWS,
                                                     brdfData.perceptualRoughness,
                                                     inputData.normalizedScreenSpaceUV, lightReach);
        #ifdef HB_WEATHER
        half4 screen = HB_ScreenReflectAbove(inputData.positionWS, reflectVector, geometry,
                                             brdfData.perceptualRoughness);

        environment = lerp(environment, screen.rgb, screen.a);
        #endif
        environment *= occlusion;
        half gloss = 1.0h - brdfData.perceptualRoughness;
        half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS))) * (gloss * gloss);

        colour += EnvironmentBRDFSpecular(brdfData, fresnelTerm) * environment * wet;
    }
    #endif
#endif

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        colour += HB_LightContribution(mainLight, toneNormal, surfaceNormal, style, terminatorOffset, diffuse,
                                       brdfData, inputData, surfaceData, wet, HB_SUN_GLOSS);
    }

#if defined(_ADDITIONAL_LIGHTS)
    HB_UNLESS_SIMPLE_LIGHTING
    {
        uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTER_LIGHT_LOOP
        [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                    lightIndex++)
        {
            CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

            Light light = HB_GetAdditionalLight(lightIndex, inputData.positionWS, positionDx, positionDy,
                                                inputData.normalWS, shadowMask, aoFactor, brush.offset, brushed);
        #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
            {
                colour += HB_LightContribution(light, toneNormal, surfaceNormal, style, terminatorOffset, diffuse,
                                               brdfData, inputData, surfaceData, wet, 1.0h);
            }
        }
    #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = HB_GetAdditionalLight(lightIndex, inputData.positionWS, positionDx, positionDy,
                                                inputData.normalWS, shadowMask, aoFactor, brush.offset, brushed);
        #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
            {
                colour += HB_LightContribution(light, toneNormal, surfaceNormal, style, terminatorOffset, diffuse,
                                               brdfData, inputData, surfaceData, wet, 1.0h);
            }
        LIGHT_LOOP_END
    }
#endif

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    colour += inputData.vertexLighting * diffuse;
#endif

#ifdef _HB_RIM
    colour += HB_RimLight(inputData.normalWS, inputData.viewDirectionWS, style, occlusion);
#endif

    colour += surfaceData.emission;

    return half4(colour, surfaceData.alpha);
}

#endif
