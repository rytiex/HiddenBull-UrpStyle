#ifndef HB_FOLIAGE_INPUT_INCLUDED
#define HB_FOLIAGE_INPUT_INCLUDED

#ifndef HB_PLANT_BODY
    #define HB_SNOW_NO_SLOPE   -0.2h
    #define HB_SNOW_FULL_SLOPE 0.0h
    #define HB_SNOW_EDGE_DELAY 0.0h
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWind.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BrushMask_ST;
    half4 _BaseColor;
#ifdef HB_PLANT_BODY
    half _BumpScale;
#else
    half _CanopyShape;
    half _WindFlutter;
#endif
    half _Cutoff;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
    half _BrushObjectSpace;
    half _BrushScale;
    half _BrushPaint;
    half _BrushEdgeKeep;
    half _ToneStrokes;
    half _BakedStrokes;
    half _BrushShading;
    half _BrushAmbient;
    half _BrushAlbedo;
    half _BrushRelief;
    half _Wetness;
    half _SnowCover;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _Cutoff    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#endif

TEXTURE2D(_BrushMask);
SAMPLER(sampler_BrushMask);

#ifdef _HB_MASK_MAP
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
#endif

#define HB_PLANT_BAKED      0.5
#define HB_PLANT_UNBAKED    -1.0
#define HB_PLANT_LEAN_BASE  0.3
#define HB_PLANT_LEAN_PRESS 0.7
#define HB_PLANT_ROCK_SPEED 1.3
#define HB_PLANT_ROCK       0.15
#define HB_PLANT_DROP       0.3
#define HB_PLANT_IDLE       0.2
#define HB_PLANT_FLUTTER    7.0

bool HB_PlantBaked(float3 plantData)
{
    return plantData.z > HB_PLANT_BAKED;
}

float HB_PlantShare(float3 plantData)
{
    return HB_PlantBaked(plantData) ? plantData.x : HB_PLANT_UNBAKED;
}

float3 HB_PlantSway(float3 restWS, float share, float swayMetres)
{
    float strength = HB_WIND;
    float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
    float2 side = float2(-wind.y, wind.x);
    float time = _Time.y;
    float wave = dot(restWS.xz, float2(0.37, 0.23));
    float moving = HB_PLANT_IDLE + (1.0 - HB_PLANT_IDLE) * strength;
    float reach = max(share, 0.0);

    float streak = HB_GustStreak(restWS.xz, 0.0, wind, strength, time);
    float lean = strength * (HB_PLANT_LEAN_BASE + HB_PLANT_LEAN_PRESS * streak);
    float rock = moving * HB_PLANT_ROCK * sin(time * HB_PLANT_ROCK_SPEED + wave);
    float2 push = (wind * (lean + rock) + side * rock) * (reach * reach * swayMetres);

    float3 sway = float3(push.x, -length(push) * HB_PLANT_DROP * reach, push.y);

#ifndef HB_PLANT_BODY
    float flutter = sin(time * HB_PLANT_FLUTTER + dot(restWS, float3(3.1, 2.3, 4.7))) * reach * moving * _WindFlutter;

    sway += float3(side.x, 0.6, side.y) * flutter;
#endif

    return sway;
}

float3 HB_PlantPosition(float3 restWS, float share, float swayMetres)
{
    precise float3 positionWS = restWS + HB_PlantSway(restWS, share, swayMetres);

    return positionWS;
}

half HB_PlantAlpha(float2 uv)
{
    half alpha = _BaseColor.a;

#ifdef _HB_BASE_MAP
    alpha *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
#endif

    return AlphaDiscard(alpha, _Cutoff);
}

half3 HB_PlantNormalMap(float2 uv)
{
#ifdef _NORMALMAP
    return UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

void HB_PlantSurface(float2 uv, float2 shift, float2 uvDdx, float2 uvDdy, out SurfaceData surfaceData)
{
    surfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;
    float2 detailUV = uv + shift;

#ifdef _HB_BASE_MAP
    half4 painted = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, detailUV, uvDdx, uvDdy);

    #ifdef _HB_BRUSH
    half4 original = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, uvDdx, uvDdy);
    half keep = HB_PaintKeep(painted, original, half(_BrushEdgeKeep));

    painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
    detailUV = uv + shift * keep;
    #endif

    albedoAlpha *= painted;
#endif

    surfaceData.alpha = AlphaDiscard(albedoAlpha.a, _Cutoff);
    surfaceData.albedo = albedoAlpha.rgb;
    surfaceData.occlusion = 1.0h;

#ifdef _NORMALMAP
    surfaceData.normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, detailUV, uvDdx, uvDdy),
                                             _BumpScale);
#else
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
#endif

#ifdef _HB_MASK_MAP
    surfaceData.occlusion = SAMPLE_TEXTURE2D_GRAD(_MaskMap, sampler_MaskMap, detailUV, uvDdx, uvDdy).g;
#endif
}

half HB_PlantBrushMask(float2 uv)
{
#ifdef _HB_BRUSH_MASK
    return SAMPLE_TEXTURE2D(_BrushMask, sampler_BrushMask, TRANSFORM_TEX(uv, _BrushMask)).r;
#else
    return 1.0h;
#endif
}

HBStyleData HB_PlantStyle()
{
    HBStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    style.rimColor = half3(0.0h, 0.0h, 0.0h);
    style.rimPower = 0.0h;
    style.rimIntensity = 0.0h;
    style.brushShading = _BrushShading;
    style.brushAmbient = _BrushAmbient;
    style.brushAlbedo = _BrushAlbedo;
    style.brushRelief = _BrushRelief;
    return style;
}

#endif
