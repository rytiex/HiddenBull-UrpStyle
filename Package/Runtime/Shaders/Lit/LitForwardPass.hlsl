#ifndef HB_LIT_FORWARD_PASS_INCLUDED
#define HB_LIT_FORWARD_PASS_INCLUDED

#if defined(_HB_WEATHER) || defined(_HB_WEATHER_MOVING)
    #define HB_WEATHER
#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSubsurface.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#ifdef HB_WEATHER
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherSurface.hlsl"
#elif defined(HB_STYLE_DEBUG)
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleShelter.hlsl"
#endif

#define HB_DEBUG_SHELTER 8
#define HB_DEBUG_WETNESS 9
#define HB_DEBUG_SNOW    10

#if defined(_HB_BRUSH_ANCHOR)
    #define HB_ANCHOR
#endif

struct Attributes
{
    float4 positionOS        : POSITION;
    float3 normalOS          : NORMAL;
    float4 tangentOS         : TANGENT;
    float2 texcoord          : TEXCOORD0;
    float2 staticLightmapUV  : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
#ifdef HB_ANCHOR
    float3 anchorOS          : TEXCOORD3;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                 : TEXCOORD0;
    float3 positionWS         : TEXCOORD1;

#ifdef _NORMALMAP
    half4 normalWS            : TEXCOORD2;
    half4 tangentWS           : TEXCOORD3;
    half4 bitangentWS         : TEXCOORD4;
#else
    half3 normalWS            : TEXCOORD2;
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

#ifdef HB_ANCHOR
    float3 anchorOS           : TEXCOORD10;
#endif

    float4 positionCS         : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void HB_InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
    inputData.normalWS = input.normalWS;
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

Varyings HB_LitVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

#ifdef HB_ANCHOR
    output.anchorOS = input.anchorOS;
#endif

#ifdef _NORMALMAP
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS),
               output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

HBSurfaceInput HB_LitSurfaceInput(Varyings input, bool frontFace)
{
#ifdef _NORMALMAP
    half3 tangentWS = input.tangentWS.xyz;
    half3 bitangentWS = input.bitangentWS.xyz;
#else
    half3 tangentWS = half3(1.0h, 0.0h, 0.0h);
    half3 bitangentWS = half3(0.0h, 1.0h, 0.0h);
#endif

#ifdef HB_ANCHOR
    float3 anchorOS = input.anchorOS;
#else
    float3 anchorOS = float3(0.0, 0.0, 0.0);
#endif

    return HB_SurfaceInput(input.uv, input.positionWS, input.normalWS.xyz, tangentWS, bitangentWS, anchorOS,
                           frontFace);
}

half4 HB_LitColour(Varyings input, bool frontFace)
{
    HBSurfaceInput surface = HB_LitSurfaceInput(input, frontFace);

    if (!frontFace)
        input.normalWS.xyz = -input.normalWS.xyz;

    HBStyleData style = HB_InitializeStyleData();

    float2 shift = float2(0.0, 0.0);
    float2 lightmapShift = float2(0.0, 0.0);
    float3 strokeShift = float3(0.0, 0.0, 0.0);
    float2 uvDdx = ddx(input.uv);
    float2 uvDdy = ddy(input.uv);

    HBBrushSample brush = HB_ResolveBrush(surface);

#ifdef _HB_BRUSH
    half3 geometryNormalWS = HB_FacingNormal(surface);
    half scale = max(half(_BrushScale), half(HB_EPSILON));

    float3 strokeOffset = HB_BrushStrokeOffset(brush.spine, geometryNormalWS, scale * brush.growth);
    float2 strokeScreen = HB_BrushScreenOffset(strokeOffset, input.positionWS, geometryNormalWS);

    strokeShift = strokeOffset;
    shift = HB_BrushUvShift(strokeScreen, uvDdx, uvDdy, HB_BRUSH_SEAM_FOOTPRINT) * _BrushPaint;

    half toneStrokes = half(_ToneStrokes);

    brush.offset = strokeOffset * toneStrokes;
    brush.normalShift = half3(strokeScreen.x * ddx(geometryNormalWS) + strokeScreen.y * ddy(geometryNormalWS))
                      * toneStrokes;

    #ifdef LIGHTMAP_ON
    lightmapShift = HB_BrushUvShift(strokeScreen, ddx(input.staticLightmapUV), ddy(input.staticLightmapUV),
                                    HB_BRUSH_LIGHTMAP_SEAM) * (toneStrokes * half(_BakedStrokes));
    #endif

    bool brushed = true;
#else
    bool brushed = false;
#endif

    SurfaceData surfaceData;
    half3 mappedNormalWS;

    HB_ResolveSurface(surface, shift, uvDdx, uvDdy, strokeShift, surfaceData, mappedNormalWS);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    HB_InitializeInputData(input, surfaceData.normalTS, inputData);

#if defined(_HB_TRIPLANAR) && defined(_NORMALMAP)
    inputData.normalWS = NormalizeNormalPerPixel(mappedNormalWS);
#endif

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

#ifdef _HB_SUBSURFACE
    HB_SubsurfaceSurface(input.uv, surface, inputData);
#endif

    HB_InitializeBakedGI(input, lightmapShift, inputData);

#ifdef HB_WEATHER
    HBWeatherMaterial weatherMaterial;
    weatherMaterial.wetness = half(_Wetness);
    weatherMaterial.snowCover = half(_SnowCover);
    weatherMaterial.beads = half4(_DropDensity, _DropSize, _FlakeDensity, _FlakeSize);
    weatherMaterial.beadLife = half2(_DropLife, _FlakeLife);

    HBWeather weather = HB_ResolveWeather(inputData.positionWS, inputData.normalWS, brush.coverage, surface.anchorOS,
                                          weatherMaterial);

    HB_ApplyWeather(weather, surfaceData, inputData.normalWS, false);
    inputData.normalWS = HB_RainRipples(inputData.positionWS, inputData.normalWS, weather);
    HB_SoftenBrushUnderSnow(weather.snow, style);

    half wet = max(weather.wet, weather.flow);
    half snow = weather.snow;
#else
    half wet = 0.0h;
    half snow = 0.0h;
#endif

    half4 colour = HB_StyleLit(inputData, surfaceData, style, brush, brushed, wet);

    colour.rgb = HB_ApplyAtmosphere(colour.rgb, inputData.positionWS);

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_SHELTER)
    {
        half open = HB_ShelterOpen(inputData.positionWS, inputData.normalWS);
        colour.rgb = half3(open, open, open);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_WETNESS)
        colour.rgb = half3(wet, wet, wet);

    if (HB_DEBUG_VIEW == HB_DEBUG_SNOW)
        colour.rgb = half3(snow, snow, snow);
#endif

    return colour;
}

void HB_LitFragment(
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

    outColor = half4(HB_LitColour(input, IS_FRONT_VFACE(frontFace, true, false)).rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
