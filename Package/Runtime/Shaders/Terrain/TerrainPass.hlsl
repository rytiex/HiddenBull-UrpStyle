#ifndef HIDDENBULL_URPSTYLE_TERRAIN_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_TERRAIN_PASS_INCLUDED

#include "TerrainInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"

#define HB_WEATHER_NO_BOUNDS
#define HB_SHADOW_CRISP_REACH 15.0
#define HB_WET_FILM_SMOOTHNESS 0.5h

#include "../Library/StyleLighting.hlsl"
#include "../Library/StyleWeatherSurface.hlsl"
#include "TerrainNoise.hlsl"

#define HB_TERRAIN_STROKE_RANGE 4.0h
#define HB_TERRAIN_CURVE_GAIN   6.0
#define HB_TERRAIN_CAVITY_TINT  0.45h
#define HB_TERRAIN_CAVITY_SHADE 0.6h
#define HB_TERRAIN_RIDGE_LIFT   0.25h

float _HB_TerrainLayerMap;

SAMPLER(s_hb_height_linear_clamp);

half3 HB_TerrainGeometryNormal(Varyings IN)
{
#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    return NormalizeNormalPerPixel(IN.normal.xyz);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float2 sampleCoords = (IN.uvMainAndLM.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    return NormalizeNormalPerPixel(TransformObjectToWorldNormal(normalize(
        SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2.0 - 1.0)));
#else
    return NormalizeNormalPerPixel(IN.normal);
#endif
}

half HB_TerrainCurvature(float2 terrainUV)
{
    half curve = 0.0h;

#ifdef UNITY_INSTANCING_ENABLED
    UNITY_BRANCH
    if (_Curvature > 0.0h)
    {
        float2 texel = terrainUV / _TerrainHeightmapRecipSize.zw;
        float reach = max(float(_CurvatureRadius) / _TerrainHeightmapScale.x, 1.0);
        float2 toUV = _TerrainHeightmapRecipSize.xy;

        float centre = UnpackHeightmap(SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, s_hb_height_linear_clamp,
                                                            (texel + 0.5) * toUV, 0));
        float around = UnpackHeightmap(SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, s_hb_height_linear_clamp,
                                                            (texel + float2(reach, 0.0) + 0.5) * toUV, 0))
                     + UnpackHeightmap(SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, s_hb_height_linear_clamp,
                                                            (texel - float2(reach, 0.0) + 0.5) * toUV, 0))
                     + UnpackHeightmap(SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, s_hb_height_linear_clamp,
                                                            (texel + float2(0.0, reach) + 0.5) * toUV, 0))
                     + UnpackHeightmap(SAMPLE_TEXTURE2D_LOD(_TerrainHeightmapTexture, s_hb_height_linear_clamp,
                                                            (texel - float2(0.0, reach) + 0.5) * toUV, 0));

        float sink = (around * 0.25 - centre) * _TerrainHeightmapScale.y;

        curve = half(clamp(sink / max(float(_CurvatureRadius), 0.01) * HB_TERRAIN_CURVE_GAIN, -1.0, 1.0));
    }
#endif

    return curve;
}

void HB_TerrainShapeShade(half curve, inout half3 albedo, inout half occlusion)
{
    half cavity = saturate(curve) * _Curvature;
    half ridge = saturate(-curve) * _Curvature * _CurvatureRidge;

    albedo *= 1.0h - cavity * HB_TERRAIN_CAVITY_TINT + ridge * HB_TERRAIN_RIDGE_LIFT;
    occlusion *= 1.0h - cavity * HB_TERRAIN_CAVITY_SHADE;
}

HiddenBullStyleData HB_TerrainStyle()
{
    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    style.brushShading = _BrushShading;
    style.brushAmbient = _BrushAmbient;
    style.brushAlbedo = _BrushAlbedo;
    style.brushRelief = _BrushRelief;
    return style;
}

HiddenBullBrushSample HB_TerrainBrush(float3 positionWS, half3 normalWS)
{
#ifdef _HB_BRUSH
    return HB_SampleBrush(positionWS, normalWS, 0.0h, float3(0.0, 0.0, 0.0), max(half(_BrushScale), HB_EPSILON), 1.0h);
#else
    return HB_NoBrush();
#endif
}

#include "TerrainArrays.hlsl"

void HiddenBullTerrainFragment(
    Varyings IN
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

#ifdef _ALPHATEST_ON
    ClipHoles(IN.uvMainAndLM.xy);
#endif

    half3 geometryNormal = HB_TerrainGeometryNormal(IN);
    HiddenBullBrushSample brush = HB_TerrainBrush(IN.positionWS, geometryNormal);

    HiddenBullTerrainSurface layered = HB_TerrainArraySurface(IN, geometryNormal, brush);
    half3 normalTS = layered.normalTS;
    half3 albedo = layered.albedo;
    half smoothness = layered.smoothness;
    half metallic = layered.metallic;
    half occlusion = layered.occlusion;
    half2 limits = layered.limits;

    HB_TerrainShapeShade(HB_TerrainCurvature(IN.uvMainAndLM.xy), albedo, occlusion);

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS + layered.bend);

#if defined(_DBUFFER)
    half3 specular = half3(0.0h, 0.0h, 0.0h);
    ApplyDecal(IN.clipPos, albedo, specular, inputData.normalWS, metallic, occlusion, smoothness);
#endif

    InitializeBakedGIData(IN, inputData);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = 1.0h;

    HiddenBullStyleData style = HB_TerrainStyle();

    HiddenBullWeather weather = HB_ResolveWeather(inputData.positionWS, inputData.normalWS, brush.coverage,
                                                  float3(0.0, 0.0, 0.0), float3(0.0, 0.0, 0.0),
                                                  half(_ReceiveWeather), half4(0.0h, 0.0h, 0.0h, 0.0h),
                                                  half2(1.0h, 1.0h), limits);

    half film = HB_ApplyGroundWeather(weather, surfaceData, inputData.normalWS);
    inputData.normalWS = HB_RainRipples(inputData.positionWS, inputData.normalWS, weather);
    HB_SoftenBrushUnderSnow(weather.snow, style);

    half sunVisibility;
    half lightReach;

    half4 colour = HiddenBullFragmentLit(inputData, surfaceData, style, brush, film, sunVisibility, lightReach);

    UNITY_BRANCH
    if (HB_WEATHER_DEBUG > 0.5)
        colour.rgb = weather.debug;

    UNITY_BRANCH
    if (_HB_TerrainLayerMap > 0.5)
        colour.rgb = layered.debug;

    outColor = half4(colour.rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
