#ifndef HIDDENBULL_URPSTYLE_LIT_FORWARD_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_FORWARD_PASS_INCLUDED

#include "LitSurface.hlsl"
#include "LitSubsurface.hlsl"
#include "../Library/StyleLighting.hlsl"
#include "../Library/StyleFog.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS       : POSITION;
    float3 normalOS         : NORMAL;
    float4 tangentOS        : TANGENT;
    float2 texcoord         : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
#if defined(_HB_BRUSH_ANCHOR) && !defined(HB_ANCHOR_REST)
    float3 brushAnchorOS    : TEXCOORD3;
#endif
#ifdef HB_VERTEX_COLOR
    half4 color             : COLOR;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                          : TEXCOORD0;
    float3 positionWS                  : TEXCOORD1;

    #ifdef _NORMALMAP
        half4 normalWS                 : TEXCOORD2;
        half4 tangentWS                : TEXCOORD3;
        half4 bitangentWS              : TEXCOORD4;
    #else
        half3 normalWS                 : TEXCOORD2;
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        half3 vertexLight              : TEXCOORD5;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord             : TEXCOORD6;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

    #ifdef DYNAMICLIGHTMAP_ON
        float2 dynamicLightmapUV       : TEXCOORD8;
    #endif

    #ifdef USE_APV_PROBE_OCCLUSION
        float4 probeOcclusion          : TEXCOORD9;
    #endif

    #if defined(_HB_BRUSH_ANCHOR) || defined(HB_ANCHOR_REST)
        float3 brushAnchor             : TEXCOORD10;
    #endif

    float4 positionCS                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
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

void InitializeBakedGIData(Varyings input, float2 lightmapShift, inout InputData inputData)
{
#if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV + lightmapShift, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV + lightmapShift);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV + lightmapShift, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV + lightmapShift);
#endif
}

Varyings HiddenBullLitVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 restOS = input.positionOS.xyz;

#ifdef HB_HAS_VERTEX_HOOK
    #ifdef HB_VERTEX_COLOR
        HB_VertexHook(input.positionOS.xyz, input.color);
    #else
        HB_VertexHook(input.positionOS.xyz, half4(1.0h, 1.0h, 1.0h, 1.0h));
    #endif
#endif

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

#if defined(HB_ANCHOR_REST)
    output.brushAnchor = restOS;
#elif defined(_HB_BRUSH_ANCHOR)
    output.brushAnchor = input.brushAnchorOS;
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
    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

HiddenBullSurfaceInput HB_LitSurfaceInput(Varyings input, bool frontFace)
{
#ifdef _NORMALMAP
    half3 tangentWS = input.tangentWS.xyz;
    half3 bitangentWS = input.bitangentWS.xyz;
#else
    half3 tangentWS = half3(1.0h, 0.0h, 0.0h);
    half3 bitangentWS = half3(0.0h, 1.0h, 0.0h);
#endif

#if defined(_HB_BRUSH_ANCHOR) || defined(HB_ANCHOR_REST)
    float3 anchorOS = input.brushAnchor;
#else
    float3 anchorOS = float3(0.0, 0.0, 0.0);
#endif

    return HB_SurfaceInput(input.uv, input.positionWS, input.normalWS.xyz, tangentWS, bitangentWS, anchorOS,
                           frontFace);
}

half4 HB_LitColour(Varyings input, bool frontFace, out InputData inputData, out half sunVisibility,
                   out float3 strokeShift, out half snowCover)
{
    strokeShift = float3(0.0, 0.0, 0.0);

    HiddenBullSurfaceInput surface = HB_LitSurfaceInput(input, frontFace);

    if (!frontFace)
        input.normalWS.xyz = -input.normalWS.xyz;

    HiddenBullStyleData styleData = InitializeHiddenBullStyleData();
    styleData.brushAnchor = surface.anchorOS;

    float2 shift = float2(0.0, 0.0);
    float2 lightmapShift = float2(0.0, 0.0);
    float2 uvDdx = ddx(input.uv);
    float2 uvDdy = ddy(input.uv);

    half3 geometryNormalWS = HB_FacingNormal(surface);

    HiddenBullBrushSample brush = HB_ResolveBrush(surface);

#ifdef _HB_BRUSH
    half scale = max(half(_BrushScale), HB_EPSILON);

    float3 strokeOffset = HB_BrushStrokeOffset(brush.spine, geometryNormalWS, scale * brush.growth);
    float2 strokeScreen = HB_BrushScreenOffset(strokeOffset, input.positionWS, geometryNormalWS);

    strokeShift = strokeOffset;

    shift = HB_BrushUvShift(strokeScreen, uvDdx, uvDdy, HB_BRUSH_SEAM_FOOTPRINT) * _BrushPaint;

    half toneStrokes = half(_ToneStrokes);

    brush.offset = strokeOffset * toneStrokes;
    brush.normalShift = half3(strokeScreen.x * ddx(geometryNormalWS)
                            + strokeScreen.y * ddy(geometryNormalWS)) * toneStrokes;

    #ifdef LIGHTMAP_ON
        lightmapShift = HB_BrushUvShift(strokeScreen, ddx(input.staticLightmapUV),
                                        ddy(input.staticLightmapUV), HB_BRUSH_LIGHTMAP_SEAM)
                      * (toneStrokes * half(_BakedStrokes));
    #endif
#endif

    SurfaceData surfaceData;
    half3 mappedNormalWS;

    HB_ResolveSurface(surface, shift, uvDdx, uvDdy, strokeShift, brush.coverage, surfaceData, mappedNormalWS);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InitializeInputData(input, surfaceData.normalTS, inputData);

#if defined(_HB_TRIPLANAR) && defined(_NORMALMAP)
    inputData.normalWS = NormalizeNormalPerPixel(mappedNormalWS);
#endif

#ifdef HB_HAS_NORMAL_HOOK
    half3 meshNormalWS = inputData.normalWS;
    inputData.normalWS = HB_NormalHook(inputData.normalWS, surface.anchorOS);
#endif

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

#ifdef HB_HAS_SURFACE_HOOK
    #if defined(_HB_SUBSURFACE) && defined(_HB_CLOTH) && defined(_HB_TRIPLANAR)
    hb_WeaveTriplanar = HB_SurfaceTriplanar(surface, HB_FacingNormal(surface));
    #endif

    HB_SurfaceHook(input.uv, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, lightmapShift, inputData);

#ifdef _SURFACE_TYPE_TRANSPARENT
    HiddenBullWeather weather = HB_ResolveSurfaceWeather(surface, brush.coverage);
#else
    half3 weatherNormalWS = inputData.normalWS;

    #ifdef HB_HAS_NORMAL_HOOK
    weatherNormalWS = HB_WeatherNormal(meshNormalWS, inputData.normalWS);
    #endif

    HiddenBullWeather weather = HB_ResolveWeather(inputData.positionWS, weatherNormalWS, brush.coverage,
                                                  strokeShift, styleData.brushAnchor, half(_ReceiveWeather),
                                                  half4(_DropDensity, _DropSize, _FlakeDensity, _FlakeSize),
                                                  half2(_DropLife, _FlakeLife), half2(_Wetness, _SnowCover));
#endif

    HB_ApplyWeather(weather, surfaceData, inputData.normalWS);
    inputData.normalWS = HB_RainRipples(inputData.positionWS, inputData.normalWS, weather);
    snowCover = weather.snow;

#ifdef HB_HAS_SNOW_HOOK
    HB_SnowHook(weather.snow);
#endif

    HB_SoftenBrushUnderSnow(weather.snow, styleData);

    half lightReach;

    half4 lit = HiddenBullFragmentLit(inputData, surfaceData, styleData, brush, max(weather.wet, weather.flow),
                                      sunVisibility, lightReach);

    UNITY_BRANCH
    if (HB_WEATHER_DEBUG > 0.5)
        lit.rgb = weather.debug;

    return lit;
}

void HiddenBullLitFragment(
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

    InputData inputData;
    half sunVisibility;
    float3 strokeShift;
    half snowCover;

    outColor = half4(HB_LitColour(input, IS_FRONT_VFACE(frontFace, true, false),
                                  inputData, sunVisibility, strokeShift, snowCover).rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
