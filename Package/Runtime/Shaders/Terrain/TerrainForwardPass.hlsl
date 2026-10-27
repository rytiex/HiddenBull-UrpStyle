#ifndef HB_TERRAIN_FORWARD_PASS_INCLUDED
#define HB_TERRAIN_FORWARD_PASS_INCLUDED

#define HB_WEATHER

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainGrass.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainShade.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWeatherSurface.hlsl"

#define HB_DEBUG_SHELTER 8
#define HB_DEBUG_WETNESS 9
#define HB_DEBUG_SNOW    10
#define HB_DEBUG_TERRAIN_RINGS 12
#define HB_DEBUG_TERRAIN_LAYERS 13
#define HB_TERRAIN_HIGHLIGHT_COLOUR half3(0.2h, 0.55h, 1.0h)
#define HB_TERRAIN_HIGHLIGHT_AMOUNT 0.45h
#define HB_TERRAIN_LAYER_MAP        -1.5
#define HB_DEBUG_TERRAIN_CURVATURE  14
#define HB_DEBUG_GRASS_DENSITY      15
#define HB_GRASS_DEBUG_BARE         half3(0.18h, 0.18h, 0.18h)
#define HB_GRASS_DEBUG_SHORT        half3(0.9h, 0.85h, 0.2h)
#define HB_GRASS_DEBUG_TALL         half3(0.05h, 0.5h, 0.4h)
#define HB_GRASS_DEBUG_TALLEST      1.0h

struct HBTerrainAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBTerrainVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float2 ring       : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

HBStyleData HB_TerrainStyle()
{
    HBStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    style.rimColor = half3(0.0h, 0.0h, 0.0h);
    style.rimPower = 1.0h;
    style.rimIntensity = 0.0h;
    style.brushShading = half(HB_TERRAIN_SHADING_BREAKUP);
    style.brushAmbient = half(HB_TERRAIN_AMBIENT_BREAKUP);
    style.brushAlbedo = half(HB_TERRAIN_ALBEDO_VARIATION);
    style.brushRelief = half(HB_TERRAIN_BRUSH_RELIEF);
    return style;
}

half3 HB_RingColour(float level, float morph)
{
    static const half3 colours[6] =
    {
        half3(1.0h, 0.25h, 0.2h), half3(1.0h, 0.75h, 0.2h), half3(0.4h, 1.0h, 0.3h),
        half3(0.2h, 0.8h, 1.0h), half3(0.4h, 0.35h, 1.0h), half3(1.0h, 0.35h, 0.9h)
    };

    uint index = (uint)round(level);

    return lerp(colours[index % 6u], colours[(index + 1u) % 6u], half(morph));
}

half3 HB_LayerColour(int layer)
{
    static const half3 colours[8] =
    {
        half3(0.90h, 0.30h, 0.25h), half3(0.30h, 0.70h, 0.30h), half3(0.25h, 0.45h, 0.90h),
        half3(0.95h, 0.80h, 0.20h), half3(0.70h, 0.35h, 0.85h), half3(0.20h, 0.80h, 0.80h),
        half3(0.95h, 0.55h, 0.15h), half3(0.85h, 0.85h, 0.85h)
    };

    half shade = layer >= 8 ? 0.6h : 1.0h;

    return pow(colours[(uint)layer % 8u] * shade, 2.2h);
}

HBTerrainVaryings HB_TerrainForwardVertex(HBTerrainAttributes input)
{
    HBTerrainVaryings output = (HBTerrainVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    HBTerrainVertex vertex = HB_TerrainVertex(input.positionOS.xyz);

    output.positionWS = vertex.positionWS;
    output.positionCS = TransformWorldToHClip(vertex.positionWS);
    output.ring = float2(vertex.level, vertex.morph);
    return output;
}

half4 HB_TerrainForwardFragment(HBTerrainVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_TerrainClipHoles(input.positionWS);

    float footprint = max(length(ddx(input.positionWS.xz)), length(ddy(input.positionWS.xz)));
    float curvature;
    half3 normalWS = half3(HB_TerrainShape(input.positionWS.xz, footprint, curvature));
    half3 geometryNormalWS = normalWS;
    half bend = HB_TerrainBend(curvature);

    half brushSize = max(half(HB_TERRAIN_BRUSH_SIZE), half(HB_EPSILON));
    HBBrushSample brush = HB_SampleBrush(input.positionWS, geometryNormalWS, 0.0h, float3(0.0, 0.0, 0.0),
                                         brushSize, 1.0h);
    float3 strokeOffset = HB_BrushStrokeOffset(brush.spine, geometryNormalWS, brushSize * brush.growth);
    float2 strokeScreen = HB_BrushScreenOffset(strokeOffset, input.positionWS, geometryNormalWS);
    half toneStrokes = half(HB_TERRAIN_TONE_STROKES);

    brush.offset = strokeOffset * toneStrokes;
    brush.normalShift = half3(strokeScreen.x * ddx(geometryNormalWS) + strokeScreen.y * ddy(geometryNormalWS))
                      * toneStrokes;

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = _BaseColor.rgb;
    surfaceData.alpha = 1.0h;
    surfaceData.occlusion = 1.0h;
    surfaceData.smoothness = _Smoothness;
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);

    int strongest = -1;
    half wetness = 1.0h;
    half snowCover = 1.0h;
    half groundHeight = 0.5h;

    UNITY_BRANCH
    if (HB_TERRAIN_PAINTED > 0.5)
    {
        HBTerrainSurface layers = HB_TerrainLayerSurface(input.positionWS,
                                                         strokeOffset.xz * HB_TERRAIN_BRUSH_PAINT);

        surfaceData.albedo = layers.albedo;
        surfaceData.occlusion = layers.occlusion;
        surfaceData.smoothness = layers.smoothness;
        surfaceData.metallic = layers.metallic;
        surfaceData.normalTS = layers.normalTS;
        normalWS = HB_TerrainNormalToWorld(layers.normalTS, normalWS);
        strongest = layers.strongest;
        wetness = layers.wetness;
        snowCover = layers.snowCover;
        groundHeight = 1.0h - layers.height;
    }

    surfaceData.albedo *= HB_TerrainShade(input.positionWS.xz, bend);

    HBStyleData style = HB_TerrainStyle();
    HBWeather weather = HB_ResolveSurfaceWeather(input.positionWS, normalWS, brush.coverage, wetness, snowCover,
                                                 true, groundHeight);

    HB_ApplyWeather(weather, surfaceData, normalWS, true);
    HB_SoftenBrushUnderSnow(weather.snow, style);

    half wet = max(weather.wet, weather.flow);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    inputData.bakedGI = SampleSH(normalWS);
    inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

    half4 colour = HB_StyleLit(inputData, surfaceData, style, brush, true, wet);


    UNITY_BRANCH
    if (HB_TERRAIN_HIGHLIGHT >= 0.0)
    {
        half weight = HB_TerrainLayerWeight(input.positionWS, (int)HB_TERRAIN_HIGHLIGHT);
        colour.rgb = lerp(colour.rgb, HB_TERRAIN_HIGHLIGHT_COLOUR, weight * HB_TERRAIN_HIGHLIGHT_AMOUNT);
    }
    else if (HB_TERRAIN_HIGHLIGHT < HB_TERRAIN_LAYER_MAP && strongest >= 0)
    {
        colour.rgb = HB_LayerColour(strongest) * saturate(normalWS.y * 0.5h + 0.5h);
    }

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_TERRAIN_RINGS)
        colour.rgb = HB_RingColour(input.ring.x, input.ring.y) * saturate(normalWS.y * 0.5h + 0.5h);

    if (HB_DEBUG_VIEW == HB_DEBUG_SHELTER)
    {
        half open = HB_ShelterOpen(input.positionWS, normalWS);
        colour.rgb = half3(open, open, open);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_WETNESS)
        colour.rgb = half3(wet, wet, wet);

    if (HB_DEBUG_VIEW == HB_DEBUG_SNOW)
        colour.rgb = half3(weather.snow, weather.snow, weather.snow);

    if (HB_DEBUG_VIEW == HB_DEBUG_TERRAIN_CURVATURE)
        colour.rgb = half3(saturate(-bend), 0.5h - abs(bend) * 0.5h, saturate(bend)) + 0.25h;

    if (HB_DEBUG_VIEW == HB_DEBUG_TERRAIN_LAYERS)
    {
        colour.rgb = strongest < 0 ? half3(0.5h, 0.5h, 0.5h) : HB_LayerColour(strongest);
        colour.rgb *= saturate(normalWS.y * 0.5h + 0.5h);
    }

    if (HB_DEBUG_VIEW == HB_DEBUG_GRASS_DENSITY)
    {
        HBTerrainGrass grass = HB_TerrainGrass(HB_TerrainLocal(input.positionWS.xz) / HB_TERRAIN_SIZE.xz);
        half3 hue = lerp(HB_GRASS_DEBUG_SHORT, HB_GRASS_DEBUG_TALL, saturate(grass.height / HB_GRASS_DEBUG_TALLEST));

        colour.rgb = pow(lerp(HB_GRASS_DEBUG_BARE, hue, saturate(grass.density)), 2.2h)
                   * saturate(normalWS.y * 0.5h + 0.5h);
    }
#endif

    return half4(colour.rgb, 1.0h);
}

#endif
