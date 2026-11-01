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
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageInstancing.hlsl"

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

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageSway.hlsl"

bool HB_PlantBaked(float3 plantData)
{
    return plantData.z > HB_PLANT_BAKED;
}

float HB_PlantShare(float3 plantData)
{
    return HB_PlantBaked(plantData) ? plantData.x : HB_PLANT_UNBAKED;
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
