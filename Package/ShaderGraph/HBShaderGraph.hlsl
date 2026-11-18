#ifndef HB_SHADER_GRAPH_INCLUDED
#define HB_SHADER_GRAPH_INCLUDED

#pragma target 4.5

#ifndef HB_GRAPH_SIMPLE
    #if defined(SHADERGRAPH_PREVIEW) || !defined(SHADERPASS) || SHADERPASS != SHADERPASS_UNLIT
        #define HB_GRAPH_SIMPLE
    #endif
#endif

#ifndef HB_GRAPH_SIMPLE
    #ifndef UNLIT_REALTIME_LIGHTING
        #error "HB Base needs Keep Lighting Variants. Turn it on in Graph Settings."
    #endif

    #undef UNLIT_DEFAULT_DECAL_BLENDING
    #undef UNLIT_DEFAULT_SSAO

    #ifdef _SURFACE_TYPE_TRANSPARENT
        #error "HB Base supports Opaque graphs only. Use the HB URP Style/Transparent shader for transparent surfaces."
    #endif

    #define HB_OVERLAY_SHADOW
    #define HB_DECAL_SURFACE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleSubsurface.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"

    #ifdef HB_WEATHER
        #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherSurface.hlsl"
    #elif defined(HB_STYLE_DEBUG)
        #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleShelter.hlsl"
    #endif
#endif

#define HB_GRAPH_PREVIEW_LIGHT float3(0.4, 0.8, 0.3)
#define HB_GRAPH_DEBUG_SHELTER 8
#define HB_GRAPH_DEBUG_WETNESS 9
#define HB_GRAPH_DEBUG_SNOW 10
#define HB_GRAPH_DEBUG_MOVING_BODY 22

void HB_Base_float(float3 PositionWS, float3 GeometryNormalWS, float3 TangentWS, float3 BitangentWS,
                   float4 ScreenPosition, float2 UV, float2 LightmapUV, bool FrontFace, float DiffuseWrap,
                   float DiffuseSoftness, float HalfTone, float BakedTones, float BakedSoftness, float4x4 Maps,
                   float4x4 Brush, float4x4 Specular, float4x4 Rim, float4x4 Subsurface, float4x4 Emission,
                   float4x4 Weather, out float3 Color, out float Alpha)
{
    float4 base = float4(0.7, 0.7, 0.7, 1.0);
    float3 geometryWS = FrontFace ? GeometryNormalWS : -GeometryNormalWS;
    float3 normalWS = normalize(geometryWS);
    float3 emission = float3(0.0, 0.0, 0.0);
    float metallic = 0.0;
    float smoothness = 0.5;
    float occlusion = 1.0;

#ifdef HB_GRAPH_MAPS
    base = Maps[0];
#endif

#ifdef HB_GRAPH_MAPS
    bool worldNormal = Maps[1].w > 0.5;

    if (worldNormal)
        normalWS = normalize(Maps[1].xyz);

    #if defined(_HB_NORMAL_MAP) || defined(HB_GRAPH_PREVIEW)
    if (!worldNormal)
        normalWS = normalize(TransformTangentToWorld(Maps[1].xyz, float3x3(TangentWS, BitangentWS, geometryWS)));
    #endif
#endif

#ifdef HB_GRAPH_EMISSION
    emission = Emission[0].rgb;
#endif

#ifdef _HB_SPECULAR
    metallic = Specular[0].x;
    smoothness = Specular[0].y;
#endif

#if defined(HB_GRAPH_MAPS) && defined(_HB_MASK_MAP)
    occlusion = Maps[2].g;
    #ifdef _HB_SPECULAR
    metallic = Maps[2].r;
    smoothness *= Maps[2].a;
    #endif
#endif

    Alpha = base.a;

#ifdef HB_GRAPH_SIMPLE
    float lambert = saturate(dot(normalWS, normalize(HB_GRAPH_PREVIEW_LIGHT)));

    Color = base.rgb * (0.35 + 0.65 * lambert) + emission;
#else
    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = half3(max(base.rgb, 0.0));
    surfaceData.metallic = half(saturate(metallic));
    surfaceData.smoothness = half(saturate(smoothness));
    surfaceData.occlusion = half(saturate(occlusion));
    surfaceData.emission = half3(max(emission, 0.0));
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    surfaceData.alpha = half(base.a);

    float2 pixel = ScreenPosition.xy * _ScreenSize.xy;

    InputData inputData = (InputData)0;
    inputData.positionWS = PositionWS;
    inputData.normalWS = half3(normalWS);
    inputData.viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(PositionWS));
    inputData.normalizedScreenSpaceUV = ScreenPosition.xy;

    #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(PositionWS);
    #endif

    #if defined(_DBUFFER) && !defined(HB_WEATHER)
    ApplyDecalToSurfaceData(float4(pixel, 0.0, 1.0), surfaceData, inputData);
    #endif

    #if defined(_HB_CLOTH)
    hb_Subsurface.sheenColour = half3(Subsurface[0].rgb);
    hb_Subsurface.sheenStrength = half(Subsurface[0].a);
    hb_Subsurface.sheenPower = half(Subsurface[1].x);
    hb_Subsurface.transmission = half(Subsurface[1].w);
    hb_Subsurface.transmissionColour = half3(Subsurface[2].rgb);
    hb_Subsurface.bend = half(Subsurface[2].a);
    hb_Subsurface.focus = half(Subsurface[3].x);
    hb_Subsurface.colour = half3(0.0h, 0.0h, 0.0h);
    hb_Subsurface.width = 0.0h;
    hb_Subsurface.strength = 0.0h;
    hb_Subsurface.translucency = 1.0h;

    UNITY_BRANCH
    if (Subsurface[1].y > 0.0)
        inputData.normalWS = HB_ClothWeave(inputData.normalWS, PositionWS, UV, half(Subsurface[1].y), Subsurface[1].z);
    #elif defined(_HB_SUBSURFACE)
    hb_Subsurface.colour = half3(Subsurface[0].rgb);
    hb_Subsurface.width = half(Subsurface[0].a);
    hb_Subsurface.strength = half(Subsurface[1].x);
    hb_Subsurface.transmission = half(Subsurface[1].y);
    hb_Subsurface.bend = half(Subsurface[1].z);
    hb_Subsurface.focus = half(Subsurface[1].w);
    hb_Subsurface.transmissionColour = half3(Subsurface[2].rgb);
    hb_Subsurface.translucency = half(saturate(Subsurface[2].a));
    hb_Subsurface.sheenColour = half3(0.0h, 0.0h, 0.0h);
    hb_Subsurface.sheenStrength = 0.0h;
    hb_Subsurface.sheenPower = 1.0h;
    #endif

    half3 vertexSH = half3(0.0h, 0.0h, 0.0h);
    float2 staticLightmapUV = LightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;

    #if defined(HB_GRAPH_BRUSH) && defined(LIGHTMAP_ON)
    if (Brush[1].w > 0.5)
    {
        staticLightmapUV += HB_BrushUvShift(Brush[2].xy, ddx(staticLightmapUV), ddy(staticLightmapUV),
                                            HB_BRUSH_LIGHTMAP_SEAM) * Brush[2].z;
    }
    #endif

    #if defined(LIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(staticLightmapUV, vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(staticLightmapUV);
    #elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float4 vertexOcclusion = float4(1.0, 1.0, 1.0, 1.0);

    inputData.bakedGI = SAMPLE_GI(vertexSH, GetAbsolutePositionWS(PositionWS), inputData.normalWS,
                                  inputData.viewDirectionWS, pixel, vertexOcclusion, inputData.shadowMask);
    #else
    inputData.bakedGI = SAMPLE_GI(staticLightmapUV, vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(staticLightmapUV);
    #endif

    HBStyleData style;
    style.diffuseWrap = half(DiffuseWrap);
    style.diffuseSoftness = half(DiffuseSoftness);
    style.halfTone = half(HalfTone);
    style.bakedTones = half(BakedTones);
    style.bakedSoftness = half(BakedSoftness);
    style.rimColor = half3(Rim[0].rgb);
    style.rimPower = half(Rim[0].a);
    style.rimIntensity = half(Rim[1].x);
    style.brushShading = 0.0h;
    style.brushAmbient = 0.0h;
    style.brushAlbedo = 0.0h;
    style.brushRelief = 0.0h;

    HBBrushSample brush = HB_NoBrush();
    bool brushed = false;

    #ifdef HB_GRAPH_BRUSH
    brushed = Brush[1].w > 0.5;

    if (brushed)
    {
        half3 facingWS = half3(normalize(geometryWS));

        brush.spine = half3(Brush[0].xyz);
        brush.coverage = half(Brush[0].w);
        brush.offset = Brush[1].xyz;
        brush.normalShift = half3(Brush[2].x * ddx(facingWS) + Brush[2].y * ddy(facingWS));
        style.brushShading = half(Brush[3].x);
        style.brushAmbient = half(Brush[3].y);
        style.brushAlbedo = half(Brush[3].z);
        style.brushRelief = half(Brush[3].w);
    }
    #endif

    half wet = 0.0h;
    half snow = 0.0h;

    #ifdef HB_WEATHER
    half weatherOn = Weather[0].z > 0.5 ? 1.0h : 0.0h;

    HBWeatherMaterial weatherMaterial;
    weatherMaterial.wetness = half(Weather[0].x) * weatherOn;
    weatherMaterial.snowCover = half(Weather[0].y) * weatherOn;
    weatherMaterial.beads = half4(Weather[1]) * weatherOn;
    weatherMaterial.beadLife = half2(Weather[2].xy);

    HBWeather weather = HB_ResolveWeather(PositionWS, inputData.normalWS, half3(normalize(geometryWS)),
                                          brush.coverage, Weather[3].xyz, weatherMaterial);

    HB_ApplyWeather(weather, surfaceData, inputData.normalWS, false, 1.0h);

    #if defined(_DBUFFER)
    HB_ApplyDecalOverWeather(float4(pixel, 0.0, 1.0), weather, false, surfaceData, inputData);
    #endif

    inputData.normalWS = HB_RainRipples(PositionWS, inputData.normalWS, weather);
    HB_SoftenBrushUnderSnow(weather.snow, style);

    wet = max(weather.wet, weather.flow) * weather.gloss;
    snow = weather.snow;
    #endif

    half3 colour = HB_StyleLit(inputData, surfaceData, style, brush, brushed, wet).rgb;

    #if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_GRAPH_DEBUG_SHELTER)
    {
        half open = HB_ShelterOpen(inputData.positionWS, inputData.normalWS);
        half cover = HB_ShelterCover(inputData.positionWS);

        colour = half3(open, open * cover, open * cover);
    }

    if (HB_DEBUG_VIEW == HB_GRAPH_DEBUG_WETNESS)
        colour = half3(wet, wet, wet);

    if (HB_DEBUG_VIEW == HB_GRAPH_DEBUG_SNOW)
        colour = half3(snow, snow, snow);

    if (HB_DEBUG_VIEW == HB_GRAPH_DEBUG_MOVING_BODY)
    {
        #if defined(_HB_WEATHER_MOVING)
        colour = HB_WeatherBodyTracked() ? half3(0.1h, 0.8h, 0.2h) : half3(0.9h, 0.1h, 0.1h);
        #else
        colour *= 0.15h;
        #endif
    }
    #endif

    Color = colour;
#endif
}

#endif
