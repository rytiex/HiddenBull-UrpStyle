#ifndef HB_FOLIAGE_FORWARD_PASS_INCLUDED
#define HB_FOLIAGE_FORWARD_PASS_INCLUDED

#define HB_WEATHER

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageInput.hlsl"

#ifndef HB_PLANT_BODY

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

#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

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
    float4 positionOS        : POSITION;
    float3 normalOS          : NORMAL;
    float4 tangentOS         : TANGENT;
    float2 texcoord          : TEXCOORD0;
    float2 staticLightmapUV  : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    float3 plantData         : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                 : TEXCOORD0;
    float3 positionWS         : TEXCOORD1;
    half3 normalWS            : TEXCOORD2;

#ifdef _NORMALMAP
    half3 tangentWS           : TEXCOORD3;
    half3 bitangentWS         : TEXCOORD4;
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight         : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord        : TEXCOORD6;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

#ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV  : TEXCOORD8;
#endif

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion     : TEXCOORD9;
#endif

    float4 restWS             : TEXCOORD10;

#ifndef HB_PLANT_BODY
    half3 leafNormalWS        : TEXCOORD11;
#endif

    float4 positionCS         : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleForwardInput.hlsl"

Varyings HB_PlantVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 restWS = TransformObjectToWorld(input.positionOS.xyz);
    float share = HB_PlantShare(input.plantData);

    output.restWS = float4(restWS, share < 0.0 ? share : share * share * input.plantData.y);

    VertexPositionInputs vertexInput = (VertexPositionInputs)0;
    vertexInput.positionWS = HB_PlantPosition(restWS, share, input.plantData.y);
    vertexInput.positionCS = HB_PlantClipPosition(vertexInput.positionWS);

#ifdef _NORMALMAP
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    float3 normalWS = normalInput.normalWS;

    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
#else
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#ifndef HB_PLANT_BODY
    output.leafNormalWS = normalWS;

    if (HB_PlantBaked(input.plantData))
        normalWS = lerp(normalWS, TransformObjectToWorldNormal(input.tangentOS.xyz), float(_CanopyShape));
#endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;
    output.normalWS = NormalizeNormalPerVertex(normalWS);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS),
               output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLight = VertexLighting(vertexInput.positionWS, output.normalWS);
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

#if defined(HB_STYLE_DEBUG)
void HB_PlantDebug(half3 normalWS, float3 restWS, float sway, inout half4 colour)
{
    bool unbaked = sway < 0.0;
    half3 missing = half3(1.0h, 0.0h, 1.0h);

    #ifndef HB_PLANT_BODY
    if (HB_DEBUG_VIEW == HB_DEBUG_FOLIAGE_CANOPY)
        colour.rgb = unbaked ? missing : normalWS * 0.5h + 0.5h;
    #endif

    if (HB_DEBUG_VIEW == HB_DEBUG_WEATHER_GUST)
    {
        float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
        half streak = half(HB_PlantGust(restWS, wind, HB_WIND, _Time.y));

        colour.rgb = half3(0.0h, streak, streak);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_FOLIAGE_SWAY)
        colour.rgb = unbaked ? missing : half(saturate(sway / HB_PLANT_MOST_SWAY)).xxx;
}
#endif

half4 HB_PlantColour(Varyings input, bool frontFace)
{
    float3 restWS = input.restWS.xyz;

#ifdef HB_PLANT_BODY
    half3 normalWS = NormalizeNormalPerPixel(frontFace ? input.normalWS : -input.normalWS);
#else
    half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
#endif

    HBStyleData style = HB_PlantStyle();

    float2 shift = float2(0.0, 0.0);
    float2 lightmapShift = float2(0.0, 0.0);
    float2 uvDdx = ddx(input.uv);
    float2 uvDdy = ddy(input.uv);

#ifdef _HB_BRUSH
    half scale = max(half(_BrushScale), half(HB_EPSILON));
    HBBrushSample brush = HB_SampleBrush(restWS, normalWS, _BrushObjectSpace, float3(0.0, 0.0, 0.0), scale,
                                         HB_PlantBrushMask(input.uv));

    HB_ResolveBrushShift(input, restWS, normalWS, uvDdx, uvDdy, brush, shift, lightmapShift);

    bool brushed = true;
#else
    HBBrushSample brush = HB_NoBrush();
    bool brushed = false;
#endif

    SurfaceData surfaceData;
    HB_PlantSurface(input.uv, shift, uvDdx, uvDdy, surfaceData);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    HB_InitializeInputData(input, normalWS, GetWorldSpaceNormalizeViewDir(input.positionWS),
                           surfaceData.normalTS, inputData);

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    HB_InitializeBakedGI(input, lightmapShift, inputData);

#ifdef HB_PLANT_BODY
    half3 weatherNormalWS = inputData.normalWS;
#else
    half3 weatherNormalWS = SafeNormalize(input.leafNormalWS);

    weatherNormalWS *= weatherNormalWS.y < 0.0h ? -1.0h : 1.0h;
#endif

    HBWeather weather = HB_ResolveSurfaceWeather(restWS, weatherNormalWS, brush.coverage, half(_Wetness),
                                                 half(_SnowCover), false, 1.0h);

    HB_ApplyWeather(weather, surfaceData, inputData.normalWS, false, 1.0h);
    inputData.normalWS = HB_RainRipples(restWS, inputData.normalWS, weather);
    HB_SoftenBrushUnderSnow(weather.snow, style);

    half wet = max(weather.wet, weather.flow);
    half snow = weather.snow;

    half4 colour = HB_StyleLit(inputData, surfaceData, style, brush, brushed, wet);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_SHELTER)
    {
        half open = HB_ShelterOpen(restWS, inputData.normalWS);
        half cover = HB_ShelterCover(restWS.xz);

        colour.rgb = half3(open, open * cover, open * cover);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_WETNESS)
        colour.rgb = half3(wet, wet, wet);

    if (HB_DEBUG_VIEW == HB_DEBUG_SNOW)
        colour.rgb = half3(snow, snow, snow);

    HB_PlantDebug(inputData.normalWS, restWS, input.restWS.w, colour);
#endif

    return colour;
}

void HB_PlantFragment(
    Varyings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    outColor = half4(HB_PlantColour(input, IS_FRONT_VFACE(frontFace, true, false)).rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
