#ifndef HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "../Library/StyleCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _RimColor;
    half _Cutoff;
    half _BumpScale;
    half _Metallic;
    half _Smoothness;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _RimPower;
    half _RimIntensity;
    half _BrushObjectSpace;
    half _BrushShading;
    half _BrushAlbedo;
    half _BrushRelief;
    half _BrushWarmth;
    half _Surface;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseWrap)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _RimPower)
    UNITY_DOTS_INSTANCED_PROP(float , _RimIntensity)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushObjectSpace)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushShading)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushAlbedo)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushRelief)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushWarmth)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _RimColor           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#define _BumpScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale)
#define _Metallic           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic)
#define _Smoothness         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#define _DiffuseWrap        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseWrap)
#define _DiffuseSoftness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseSoftness)
#define _RimPower           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimPower)
#define _RimIntensity       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimIntensity)
#define _BrushObjectSpace   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushObjectSpace)
#define _BrushShading       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushShading)
#define _BrushAlbedo        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushAlbedo)
#define _BrushRelief        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushRelief)
#define _BrushWarmth        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushWarmth)
#define _Surface            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface)
#endif

void InitializeHiddenBullSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;

#ifdef _HB_BASE_MAP
    albedoAlpha *= SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
#endif

    outSurfaceData.alpha = AlphaDiscard(albedoAlpha.a, _Cutoff);
    outSurfaceData.albedo = AlphaModulate(albedoAlpha.rgb, outSurfaceData.alpha);

    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.occlusion = 1.0h;
    outSurfaceData.emission = 0.0h;
}

HiddenBullStyleData InitializeHiddenBullStyleData()
{
    HiddenBullStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.rimColor = _RimColor.rgb;
    style.rimPower = _RimPower;
    style.rimIntensity = _RimIntensity;
    style.brushObjectSpace = _BrushObjectSpace;
    style.brushAnchor = float3(0.0, 0.0, 0.0);
    style.brushShading = _BrushShading;
    style.brushAlbedo = _BrushAlbedo;
    style.brushRelief = _BrushRelief;
    style.brushWarmth = _BrushWarmth;
    return style;
}

#endif
