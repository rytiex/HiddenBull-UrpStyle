#ifndef HIDDENBULL_URPSTYLE_SHADER_GRAPH_INCLUDED
#define HIDDENBULL_URPSTYLE_SHADER_GRAPH_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
    #ifndef _HB_BRUSH
        #define _HB_BRUSH
    #endif
    #ifndef _HB_SPECULAR
        #define _HB_SPECULAR
    #endif

    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleLighting.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleWeatherSurface.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/StyleFog.hlsl"
#endif

float3 HB_GraphNormal(float3 normalWS, float3 geometryNormalWS)
{
    return dot(normalWS, normalWS) > 1e-4 ? normalize(normalWS) : normalize(geometryNormalWS);
}

float3 HB_GraphPreview(float3 albedo, float3 normalWS)
{
    float lambert = saturate(dot(normalWS, normalize(float3(0.4, 0.8, 0.3))));

    return albedo * (0.35 + 0.65 * lambert);
}

void HB_StyleLighting_float(float3 PositionWS, float3 GeometryNormalWS, float4 ScreenPosition, float3 Albedo,
                            float3 NormalWS, float Metallic, float Smoothness, float Occlusion, float3 Emission,
                            float DiffuseWrap, float DiffuseSoftness, float HalfTone, float Wet, float BrushSize,
                            float BrushSpace, float BrushShading, float BrushRelief, float BrushAlbedo,
                            float BrushAmbient, out float3 Color)
{
    float3 normalWS = HB_GraphNormal(NormalWS, GeometryNormalWS);

#ifdef SHADERGRAPH_PREVIEW
    Color = HB_GraphPreview(Albedo, normalWS) + Emission;
#else
    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;
    inputData.normalWS = half3(normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(PositionWS);
    inputData.normalizedScreenSpaceUV = ScreenPosition.xy;
    inputData.bakedGI = HB_SampleProbeGI(PositionWS, inputData.normalWS, inputData.viewDirectionWS,
                                         ScreenPosition.xy * _ScreenParams.xy, inputData.shadowMask);

    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(PositionWS);
    #endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = half3(max(Albedo, 0.0));
    surfaceData.metallic = half(saturate(Metallic));
    surfaceData.smoothness = half(saturate(Smoothness));
    surfaceData.occlusion = half(saturate(Occlusion));
    surfaceData.emission = half3(max(Emission, 0.0));
    surfaceData.alpha = 1.0h;

    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = half(DiffuseWrap);
    style.diffuseSoftness = half(DiffuseSoftness);
    style.halfTone = half(HalfTone);
    style.bakedSoftness = 0.5h;
    style.brushShading = half(BrushShading);
    style.brushRelief = half(BrushRelief);
    style.brushAlbedo = half(BrushAlbedo);
    style.brushAmbient = half(BrushAmbient);

    HiddenBullBrushSample brush = HB_NoBrush();

    UNITY_BRANCH
    if (BrushSize > 0.0)
        brush = HB_SampleBrush(PositionWS, inputData.normalWS, half(saturate(BrushSpace)), float3(0.0, 0.0, 0.0),
                               half(BrushSize), 1.0h);

    half sunVisibility;
    half lightReach;

    Color = HiddenBullFragmentLit(inputData, surfaceData, style, brush, half(saturate(Wet)), sunVisibility,
                                  lightReach).rgb;
#endif
}

void HB_GraphSurface_float(float4 BaseSample, float4 BaseColor, float4 NormalSample, float NormalStrength,
                           float3 TangentWS, float3 BitangentWS, float3 GeometryNormalWS, float4 MaskSample,
                           float Metallic, float Smoothness, float OcclusionStrength, out float3 Albedo,
                           out float Alpha, out float3 NormalWS, out float OutMetallic, out float OutSmoothness,
                           out float Occlusion)
{
    float4 base = BaseSample * BaseColor;
    float3 normalTS = float3(NormalSample.xy * NormalStrength, max(NormalSample.z, 1e-3));

    Albedo = base.rgb;
    Alpha = base.a;
    NormalWS = normalize(normalTS.x * TangentWS + normalTS.y * BitangentWS + normalTS.z * GeometryNormalWS);
    OutMetallic = MaskSample.r * Metallic;
    OutSmoothness = MaskSample.a * Smoothness;
    Occlusion = lerp(1.0, MaskSample.g, OcclusionStrength);
}

void HB_GraphWeather(float3 positionWS, float3 normalWS, float3 anchorOS, half receive, float3 albedo,
                     float smoothness, float wetness, float snowCover, half4 beads, half2 life,
                     out float3 outAlbedo, out float outSmoothness, out float3 outNormal, out float wet)
{
#ifdef SHADERGRAPH_PREVIEW
    outAlbedo = albedo;
    outSmoothness = smoothness;
    outNormal = normalWS;
    wet = 0.0;
#else
    HiddenBullWeather weather = HB_ResolveWeather(positionWS, half3(normalWS), 0.0h, float3(0.0, 0.0, 0.0),
                                                  anchorOS, receive, beads, life,
                                                  half2(saturate(wetness), saturate(snowCover)));

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = half3(max(albedo, 0.0));
    surfaceData.smoothness = half(saturate(smoothness));
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    HB_ApplyWeather(weather, surfaceData, half3(normalWS));

    outAlbedo = surfaceData.albedo;
    outSmoothness = surfaceData.smoothness;
    outNormal = HB_RainRipples(positionWS, half3(normalWS), weather);
    wet = max(weather.wet, weather.flow);
#endif
}

void HB_WeatherStatic_float(float3 PositionWS, float3 GeometryNormalWS, float3 Albedo, float Smoothness,
                            float3 NormalWS, float Wetness, float SnowCover, out float3 OutAlbedo,
                            out float OutSmoothness, out float3 OutNormalWS, out float Wet)
{
    HB_GraphWeather(PositionWS, HB_GraphNormal(NormalWS, GeometryNormalWS), float3(0.0, 0.0, 0.0), 1.0h, Albedo,
                    Smoothness, Wetness, SnowCover, half4(0.0h, 0.0h, 0.0h, 0.0h), half2(1.0h, 1.0h), OutAlbedo,
                    OutSmoothness, OutNormalWS, Wet);
}

void HB_WeatherMoving_float(float3 PositionWS, float3 PositionOS, float3 GeometryNormalWS, float3 Albedo,
                            float Smoothness, float3 NormalWS, float Wetness, float SnowCover, float DropDensity,
                            float DropSize, float FlakeDensity, float FlakeSize, float DropLife, float FlakeLife,
                            out float3 OutAlbedo, out float OutSmoothness, out float3 OutNormalWS, out float Wet)
{
    HB_GraphWeather(PositionWS, HB_GraphNormal(NormalWS, GeometryNormalWS), PositionOS, 2.0h, Albedo, Smoothness,
                    Wetness, SnowCover, half4(DropDensity, DropSize, FlakeDensity, FlakeSize),
                    half2(max(DropLife, 0.1), max(FlakeLife, 0.1)), OutAlbedo, OutSmoothness, OutNormalWS, Wet);
}

void HB_Fog_float(float3 PositionWS, float4 ScreenPosition, float3 Color, out float3 Out)
{
#ifdef SHADERGRAPH_PREVIEW
    Out = Color;
#else
    Out = HB_FogSurface(half3(Color), HB_ResolveFog(PositionWS, ScreenPosition.xy, false));
#endif
}

#endif
