#ifndef HB_BILLBOARD_FORWARD_PASS_INCLUDED
#define HB_BILLBOARD_FORWARD_PASS_INCLUDED

#define HB_WEATHER

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

#define HB_PLANT_SHADE_LIGHT  0.15h
#define HB_PLANT_SHADE_SHADOW 0.35h

#define HB_HAS_EXTRA_LIGHT
#define HB_SHADOW_LIFT hb_PlantLift

half3 HB_ExtraLight(Light light, InputData inputData, SurfaceData surfaceData, half3 normalWS)
{
    half away = saturate(-dot(normalWS, light.direction) * 0.5h + 0.5h);
    half shadow = lerp(HB_PLANT_SHADE_SHADOW, 1.0h, light.shadowAttenuation);

    return light.color * light.distanceAttenuation * surfaceData.albedo * (away * shadow * HB_PLANT_SHADE_LIGHT);
}

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherSurface.hlsl"

#define HB_DEBUG_SHELTER        8
#define HB_DEBUG_WETNESS        9
#define HB_DEBUG_SNOW           10
#define HB_DEBUG_WEATHER_GUST   17
#define HB_DEBUG_FOLIAGE_CANOPY 18
#define HB_DEBUG_FOLIAGE_SWAY   19

#define HB_PLANT_MOST_SWAY 2.0

struct Attributes
{
    float4 positionOS : POSITION;
    float2 corner     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 corner             : TEXCOORD0;
    float3 positionWS         : TEXCOORD1;
    float4 frame              : TEXCOORD2;

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight         : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord        : TEXCOORD6;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion     : TEXCOORD9;
#endif

    float4 positionCS         : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleForwardInput.hlsl"

Varyings HB_BillboardVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 toward;
    float3 frame;
    float3 positionWS = HB_BillboardPosition(input.corner, toward, frame);

    VertexPositionInputs vertexInput = (VertexPositionInputs)0;
    vertexInput.positionWS = positionWS;
    vertexInput.positionCS = HB_PlantClipPosition(positionWS);

    output.corner = input.corner;
    output.positionWS = positionWS;
    output.frame = float4(frame, HB_BillboardTop().y);
    output.positionCS = vertexInput.positionCS;

    half3 normalWS = half3(toward);

    OUTPUT_SH4(positionWS, normalWS, GetWorldSpaceNormalizeViewDir(positionWS), output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLight = VertexLighting(positionWS, normalWS);
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

HBStyleData HB_BillboardStyle()
{
    HBStyleData style = (HBStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    return style;
}

half4 HB_BillboardColour(Varyings input)
{
    float2 first = HB_BillboardAtlasUV(input.corner, input.frame.x);
    float2 second = HB_BillboardAtlasUV(input.corner, input.frame.y);
    half4 albedo = lerp(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, first),
                        SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, second), half(input.frame.z));

    clip(albedo.a - _Cutoff);
    LODFadeCrossFade(input.positionCS);

    half4 normal = HB_BillboardNormal(input.corner, input.frame.xyz);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo.rgb;
    surfaceData.alpha = 1.0h;
    surfaceData.occlusion = 1.0h;
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);

    InputData inputData;
    HB_InitializeInputData(input, normal.xyz, GetWorldSpaceNormalizeViewDir(input.positionWS), surfaceData.normalTS,
                           inputData);
    HB_InitializeBakedGI(input, float2(0.0, 0.0), inputData);

    float3 weatherWS = float3(input.positionWS.x, input.frame.w, input.positionWS.z);
    HBWeather weather = HB_ResolveSurfaceWeather(weatherWS, half3(0.0h, 1.0h, 0.0h), 0.0h, half(_Wetness),
                                                 half(_SnowCover), true, half(input.corner.y));

    weather.snow *= normal.a;
    weather.wet *= normal.a;
    weather.rain *= normal.a;

    HB_ApplyWeather(weather, surfaceData, inputData.normalWS, false, 1.0h);

    half wet = weather.wet;
    half snow = weather.snow;

    half4 colour = HB_StyleLit(inputData, surfaceData, HB_BillboardStyle(), HB_NoBrush(), false, wet);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_SHELTER)
    {
        half open = HB_ShelterOpen(weatherWS, half3(0.0h, 1.0h, 0.0h)) * normal.a;
        half cover = HB_ShelterCover(weatherWS);

        colour.rgb = half3(open, open * cover, open * cover);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_WETNESS)
        colour.rgb = half3(wet, wet, wet);

    if (HB_DEBUG_VIEW == HB_DEBUG_SNOW)
        colour.rgb = half3(snow, snow, snow);

    if (HB_DEBUG_VIEW == HB_DEBUG_FOLIAGE_CANOPY)
        colour.rgb = normal.xyz * 0.5h + 0.5h;

    if (HB_DEBUG_VIEW == HB_DEBUG_WEATHER_GUST)
    {
        float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
        half streak = half(HB_PlantGust(weatherWS, wind, HB_WIND, _Time.y));

        colour.rgb = half3(0.0h, streak, streak);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_FOLIAGE_SWAY)
        colour.rgb = half(saturate(_BillboardSway / HB_PLANT_MOST_SWAY)).xxx;
#endif

    return colour;
}

void HB_BillboardFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    outColor = half4(HB_BillboardColour(input).rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
