#ifndef HB_STYLE_FORWARD_INPUT_INCLUDED
#define HB_STYLE_FORWARD_INPUT_INCLUDED

void HB_InitializeInputData(Varyings input, half3 normalWS, half3 viewDirWS, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

#ifdef _NORMALMAP
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, normalWS);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
    inputData.normalWS = normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = SafeNormalize(viewDirWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.vertexLighting = input.vertexLight;
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

void HB_InitializeBakedGI(Varyings input, float2 lightmapShift, inout InputData inputData)
{
#if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV + lightmapShift, input.dynamicLightmapUV, input.vertexSH,
                                  inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV + lightmapShift);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
                                  inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion,
                                  inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV + lightmapShift, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV + lightmapShift);
#endif
}

#ifdef _HB_BRUSH
float3 HB_ResolveBrushShift(Varyings input, float3 positionWS, half3 normalWS, float2 uvDdx, float2 uvDdy,
                            inout HBBrushSample brush, inout float2 shift, inout float2 lightmapShift)
{
    half scale = max(half(_BrushScale), half(HB_EPSILON));

    float3 strokeOffset = HB_BrushStrokeOffset(brush.spine, normalWS, scale * brush.growth);
    float2 strokeScreen = HB_BrushScreenOffset(strokeOffset, positionWS, normalWS);

    shift = HB_BrushUvShift(strokeScreen, uvDdx, uvDdy, HB_BRUSH_SEAM_FOOTPRINT) * _BrushPaint;

    half toneStrokes = half(_ToneStrokes);

    brush.offset = strokeOffset * toneStrokes;
    brush.normalShift = half3(strokeScreen.x * ddx(normalWS) + strokeScreen.y * ddy(normalWS)) * toneStrokes;

    #ifdef LIGHTMAP_ON
    lightmapShift = HB_BrushUvShift(strokeScreen, ddx(input.staticLightmapUV), ddy(input.staticLightmapUV),
                                    HB_BRUSH_LIGHTMAP_SEAM) * (toneStrokes * half(_BakedStrokes));
    #endif

    return strokeOffset;
}
#endif

#endif
