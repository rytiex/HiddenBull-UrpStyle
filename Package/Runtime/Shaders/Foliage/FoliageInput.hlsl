#ifndef HIDDENBULL_URPSTYLE_FOLIAGE_INPUT_INCLUDED
#define HIDDENBULL_URPSTYLE_FOLIAGE_INPUT_INCLUDED

// Foliage shades with Lit's surface code but owns its material: every value here is a property of
// Foliage.shader, which keeps it SRP Batcher and GPU Resident Drawer compatible.
#define HB_MATERIAL_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BrushMask_ST;
    half4 _BaseColor;
    half4 _TranslucencyColor;
    half _CanopyShape;
    half _Cutoff;
    half _BumpScale;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
    half _BrushObjectSpace;
    half _TriplanarSpace;
    half _BrushScale;
    half _BrushPaint;
    half _BrushEdgeKeep;
    half _ToneStrokes;
    half _BakedStrokes;
    half _BrushShading;
    half _BrushAmbient;
    half _BrushAlbedo;
    half _BrushRelief;
    half _ReceiveWeather;
    half _Wetness;
    half _SnowCover;
    half _DropDensity;
    half _DropSize;
    half _FlakeDensity;
    half _FlakeSize;
    half _DropLife;
    half _FlakeLife;
    half _WindStrength;
    half _WindHeight;
    half _WindBend;
    half _WindFlutter;
    half _WindSource;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseWrap)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _HalfTone)
    UNITY_DOTS_INSTANCED_PROP(float , _BakedTones)
    UNITY_DOTS_INSTANCED_PROP(float , _BakedSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushObjectSpace)
    UNITY_DOTS_INSTANCED_PROP(float , _TriplanarSpace)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushScale)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushPaint)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushEdgeKeep)
    UNITY_DOTS_INSTANCED_PROP(float , _ToneStrokes)
    UNITY_DOTS_INSTANCED_PROP(float , _BakedStrokes)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushShading)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushAmbient)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushAlbedo)
    UNITY_DOTS_INSTANCED_PROP(float , _BrushRelief)
    UNITY_DOTS_INSTANCED_PROP(float , _ReceiveWeather)
    UNITY_DOTS_INSTANCED_PROP(float , _Wetness)
    UNITY_DOTS_INSTANCED_PROP(float , _SnowCover)
    UNITY_DOTS_INSTANCED_PROP(float , _DropDensity)
    UNITY_DOTS_INSTANCED_PROP(float , _DropSize)
    UNITY_DOTS_INSTANCED_PROP(float , _FlakeDensity)
    UNITY_DOTS_INSTANCED_PROP(float , _FlakeSize)
    UNITY_DOTS_INSTANCED_PROP(float , _DropLife)
    UNITY_DOTS_INSTANCED_PROP(float , _FlakeLife)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#define _BumpScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale)
#define _DiffuseWrap        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseWrap)
#define _DiffuseSoftness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseSoftness)
#define _HalfTone           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HalfTone)
#define _BakedTones         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BakedTones)
#define _BakedSoftness      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BakedSoftness)
#define _BrushObjectSpace   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushObjectSpace)
#define _TriplanarSpace     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _TriplanarSpace)
#define _BrushScale         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushScale)
#define _BrushEdgeKeep      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushEdgeKeep)
#define _ToneStrokes        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ToneStrokes)
#define _BakedStrokes       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BakedStrokes)
#define _BrushPaint         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushPaint)
#define _BrushShading       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushShading)
#define _BrushAmbient       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushAmbient)
#define _BrushAlbedo        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushAlbedo)
#define _BrushRelief        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BrushRelief)
#define _ReceiveWeather     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ReceiveWeather)
#define _Wetness            UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Wetness)
#define _SnowCover          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SnowCover)
#define _DropDensity        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DropDensity)
#define _DropSize           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DropSize)
#define _FlakeDensity       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _FlakeDensity)
#define _FlakeSize          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _FlakeSize)
#define _DropLife           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DropLife)
#define _FlakeLife          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _FlakeLife)
#endif

// Lit surface terms leaves do not have, fixed at the values Foliage materials always rendered with:
// no metallic, no specular, no rim.
#define _Metallic           0.0h
#define _Smoothness         0.0h
#define _RimColor           half4(0.0h, 0.0h, 0.0h, 0.0h)
#define _RimPower           0.0h
#define _RimIntensity       0.0h

#include "../Lit/LitInput.hlsl"

#endif
