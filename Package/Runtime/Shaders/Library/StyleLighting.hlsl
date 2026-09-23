#ifndef HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED
#define HIDDENBULL_URPSTYLE_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "StyleCommon.hlsl"
#include "StyleAmbient.hlsl"
#include "StyleBrush.hlsl"

half3 HB_LightResponse(Light light, half3 normalWS, HiddenBullStyleData style, half terminatorOffset)
{
    half NdotL = dot(normalWS, light.direction) + terminatorOffset;
    half shaped = HB_WrappedDiffuse(NdotL, style.diffuseWrap, style.diffuseSoftness);

    return light.color * (light.distanceAttenuation * light.shadowAttenuation * shaped);
}

half3 HB_RimLight(half3 normalWS, half3 viewDirectionWS, HiddenBullStyleData style, half occlusion)
{
    half fresnel = 1.0h - saturate(dot(normalWS, viewDirectionWS));
    half rim = PositivePow(fresnel, max(style.rimPower, HB_EPSILON));
    return rim * style.rimIntensity * style.rimColor * HB_AmbientSkyColor() * occlusion;
}

half4 HiddenBullFragmentLit(InputData inputData, SurfaceData surfaceData, HiddenBullStyleData style,
                            out half sunVisibility)
{
#ifdef _HB_BRUSH
    HiddenBullBrushSample brush = HB_SampleBrush(inputData.positionWS, inputData.normalWS,
                                                 style.brushObjectSpace, style.brushAnchor);

    half terminatorOffset = brush.coverage * style.brushShading;

    inputData.normalWS = SafeNormalize(inputData.normalWS + brush.warp * style.brushRelief);

    half warmth = brush.coverage * style.brushWarmth * 0.25h;

    surfaceData.albedo *= saturate(1.0h + brush.coverage * style.brushAlbedo)
                        * half3(1.0h + warmth, 1.0h, 1.0h - warmth);
#else
    half terminatorOffset = 0.0h;
#endif

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    if (dot(_HB_KeyDirection.xyz, _HB_KeyDirection.xyz) > 0.5)
    {
        mainLight.direction = half3(_HB_KeyDirection.xyz);
        mainLight.color = _HB_KeyColor.rgb;
        mainLight.shadowAttenuation = lerp(1.0h, mainLight.shadowAttenuation, half(_HB_KeyColor.a));
    }

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    sunVisibility = mainLight.shadowAttenuation;

    half occlusion = surfaceData.occlusion * aoFactor.indirectAmbientOcclusion;

    half3 ambient = HB_ResolveAmbient(inputData.normalWS, inputData.bakedGI) * occlusion;

#ifdef _HB_SPECULAR
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
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
        half3 radiance = HB_LightResponse(mainLight, inputData.normalWS, style, terminatorOffset);

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

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, inputData.normalWS, style, terminatorOffset);

            color += radiance * surfaceData.albedo;
    #ifdef _HB_SPECULAR
            color += radiance * brdfData.specular *
                     DirectBRDFSpecular(brdfData, inputData.normalWS, light.direction, inputData.viewDirectionWS);
    #endif
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
        {
            half3 radiance = HB_LightResponse(light, inputData.normalWS, style, terminatorOffset);

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
