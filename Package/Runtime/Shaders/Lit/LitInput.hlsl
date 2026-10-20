#ifndef HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "../Library/StyleCommon.hlsl"
#include "../Library/StyleBillboardInstancing.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BrushMask_ST;
    half4 _BaseColor;
    half4 _RimColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _BumpScale;
    half _Metallic;
    half _Smoothness;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
    half _RimPower;
    half _RimIntensity;
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
    half _Refraction;
    half _RefractionBlur;
    half _Dispersion;
    half _StrokeBend;
    half _StrokeAlpha;
    half _ReceiveWeather;
    half _Wetness;
    half _SnowCover;
    half _DropDensity;
    half _DropSize;
    half _FlakeDensity;
    half _FlakeSize;
    half _DropLife;
    half _FlakeLife;
    half4 _SubsurfaceColor;
    half4 _SheenColor;
    half4 _TransmissionColor;
    half _SkinMode;
    half _SubsurfaceStrength;
    half _SubsurfaceWidth;
    half _Transmission;
    half _TransmissionBend;
    half _TransmissionFocus;
    half _ThicknessMapEnabled;
    half _SheenStrength;
    half _SheenPower;
    half _Weave;
    half _WeaveScale;
#ifdef HB_MATERIAL_EXTRA
    HB_MATERIAL_EXTRA
#endif
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseWrap)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffuseSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _HalfTone)
    UNITY_DOTS_INSTANCED_PROP(float , _BakedTones)
    UNITY_DOTS_INSTANCED_PROP(float , _BakedSoftness)
    UNITY_DOTS_INSTANCED_PROP(float , _RimPower)
    UNITY_DOTS_INSTANCED_PROP(float , _RimIntensity)
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
    UNITY_DOTS_INSTANCED_PROP(float , _Refraction)
    UNITY_DOTS_INSTANCED_PROP(float , _RefractionBlur)
    UNITY_DOTS_INSTANCED_PROP(float , _Dispersion)
    UNITY_DOTS_INSTANCED_PROP(float , _StrokeBend)
    UNITY_DOTS_INSTANCED_PROP(float , _StrokeAlpha)
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
#define _RimColor           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor)
#define _EmissionColor      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor)
#define _Cutoff             UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff)
#define _BumpScale          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale)
#define _Metallic           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic)
#define _Smoothness         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness)
#define _DiffuseWrap        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseWrap)
#define _DiffuseSoftness    UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffuseSoftness)
#define _HalfTone           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _HalfTone)
#define _BakedTones         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BakedTones)
#define _BakedSoftness      UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BakedSoftness)
#define _RimPower           UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimPower)
#define _RimIntensity       UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RimIntensity)
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
#define _Refraction         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Refraction)
#define _RefractionBlur     UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _RefractionBlur)
#define _Dispersion         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Dispersion)
#define _StrokeBend         UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _StrokeBend)
#define _StrokeAlpha        UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _StrokeAlpha)
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

TEXTURE2D(_BrushMask);
SAMPLER(sampler_BrushMask);
TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);

#if defined(_SURFACE_TYPE_TRANSPARENT) || defined(HB_OIT_MOMENTS)
    #define HB_STROKE_ALPHA
#endif

#if defined(HB_STROKE_ALPHA) && defined(_HB_REFRACTION)
    #define HB_GLASS
#endif

half HB_StrokeAlpha(half alpha, half coverage)
{
#ifdef HB_STROKE_ALPHA
    return saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);
#else
    return alpha;
#endif
}

half HB_SampleBrushMask(float2 uv)
{
#ifdef _HB_BRUSH_MASK
    return SAMPLE_TEXTURE2D(_BrushMask, sampler_BrushMask, TRANSFORM_TEX(uv, _BrushMask)).r;
#else
    return 1.0h;
#endif
}

#define HB_PAINT_EDGE_START 0.15h
#define HB_PAINT_EDGE_END   0.35h

half HB_PaintKeep(half4 painted, half4 original)
{
    half difference = length(sqrt(max(painted.rgb, 0.0h)) - sqrt(max(original.rgb, 0.0h)));

    return lerp(1.0h, 1.0h - smoothstep(HB_PAINT_EDGE_START, HB_PAINT_EDGE_END, difference), half(_BrushEdgeKeep));
}

void HB_ResolveSurfaceColour(half4 albedoAlpha, half coverage, inout SurfaceData surfaceData)
{
    surfaceData.alpha = AlphaDiscard(HB_StrokeAlpha(albedoAlpha.a, coverage), _Cutoff);
    surfaceData.albedo = AlphaModulate(albedoAlpha.rgb, surfaceData.alpha);

#if defined(HB_GLASS)
    surfaceData.albedo = 0.0h;
#elif defined(_HB_PREMULTIPLY)
    surfaceData.albedo *= surfaceData.alpha;
#endif
}

void HB_ResolveSurfaceMask(half4 mask, inout SurfaceData surfaceData)
{
#ifdef _HB_MASK_MAP
    surfaceData.metallic = mask.r;
    surfaceData.smoothness = mask.a * _Smoothness;
    surfaceData.occlusion = mask.g;
#else
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Smoothness;
    surfaceData.occlusion = 1.0h;
#endif
}

void InitializeHiddenBullSurfaceData(float2 uv, float2 shift, float2 uvDdx, float2 uvDdy, half coverage,
                                     out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;
    float2 detailUV = uv + shift;

#ifdef _HB_BASE_MAP
    half4 painted = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, detailUV, uvDdx, uvDdy);

    #ifdef _HB_BRUSH
        half4 original = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uv, uvDdx, uvDdy);
        half keep = HB_PaintKeep(painted, original);

        painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
        detailUV = uv + shift * keep;
    #endif

    albedoAlpha *= painted;
#endif

    HB_ResolveSurfaceColour(albedoAlpha, coverage, outSurfaceData);

#ifdef _NORMALMAP
    outSurfaceData.normalTS = UnpackNormalScale(
        SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, detailUV, uvDdx, uvDdy), _BumpScale);
#else
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
#endif

#ifdef _HB_MASK_MAP
    HB_ResolveSurfaceMask(SAMPLE_TEXTURE2D_GRAD(_MaskMap, sampler_MaskMap, detailUV, uvDdx, uvDdy), outSurfaceData);
#else
    HB_ResolveSurfaceMask(half4(0.0h, 1.0h, 0.0h, 1.0h), outSurfaceData);
#endif

#ifdef _HB_EMISSION
    outSurfaceData.emission = SAMPLE_TEXTURE2D_GRAD(_EmissionMap, sampler_EmissionMap, detailUV, uvDdx, uvDdy).rgb
                            * half3(_EmissionColor.rgb);
#else
    outSurfaceData.emission = 0.0h;
#endif
}

#ifdef _HB_TRIPLANAR

#include "../Library/StyleBrush.hlsl"

#define HB_TRIPLANAR_CUTOFF 0.02h

struct HiddenBullTriplanar
{
    float3 position;
    float3 positionDx;
    float3 positionDy;
    half3 blend;
    half3 normal;
    half localSpace;
};

HiddenBullTriplanar HB_SetupTriplanar(float3 positionWS, half3 normalWS, half objectSpace, float3 anchorOS)
{
    HiddenBullBrushSpace space = HB_ResolveBrushSpace(positionWS, normalWS, objectSpace, anchorOS);

    HiddenBullTriplanar triplanar;
    triplanar.position = space.position * _BaseMap_ST.x;
    triplanar.positionDx = ddx(triplanar.position);
    triplanar.positionDy = ddy(triplanar.position);
    triplanar.blend = HB_TriplanarBlend(space.normal);
    triplanar.normal = space.normal;
    triplanar.localSpace = space.localSpace;
    return triplanar;
}

float3 HB_TriplanarShift(float3 offsetWS, half localSpace)
{
    float3 local = mul((float3x3)GetWorldToObjectMatrix(), offsetWS) * HB_ObjectScale();

    return lerp(offsetWS, local, localSpace) * _BaseMap_ST.x;
}

half4 HB_SampleTriplanar(TEXTURE2D_PARAM(textureName, samplerName), HiddenBullTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float2 offset = _BaseMap_ST.zw;
    half4 sum = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.zy + offset,
                                     triplanar.positionDx.zy, triplanar.positionDy.zy) * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.xz + offset,
                                     triplanar.positionDx.xz, triplanar.positionDy.xz) * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_TRIPLANAR_CUTOFF)
    {
        sum += SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, p.xy + offset,
                                     triplanar.positionDx.xy, triplanar.positionDy.xy) * triplanar.blend.z;
    }

    return sum;
}

half3 HB_SampleTriplanarNormal(HiddenBullTriplanar triplanar, float3 shift)
{
    float3 p = triplanar.position + shift;
    float2 offset = _BaseMap_ST.zw;
    half3 normal = triplanar.normal;
    half3 sum = half3(0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (triplanar.blend.x > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.zy + offset,
                                          triplanar.positionDx.zy, triplanar.positionDy.zy), _BumpScale);
        tangent = half3(tangent.xy + normal.zy, abs(tangent.z) * normal.x);
        sum += tangent.zyx * triplanar.blend.x;
    }

    UNITY_BRANCH
    if (triplanar.blend.y > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.xz + offset,
                                          triplanar.positionDx.xz, triplanar.positionDy.xz), _BumpScale);
        tangent = half3(tangent.xy + normal.xz, abs(tangent.z) * normal.y);
        sum += tangent.xzy * triplanar.blend.y;
    }

    UNITY_BRANCH
    if (triplanar.blend.z > HB_TRIPLANAR_CUTOFF)
    {
        half3 tangent = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, p.xy + offset,
                                          triplanar.positionDx.xy, triplanar.positionDy.xy), _BumpScale);
        tangent = half3(tangent.xy + normal.xy, abs(tangent.z) * normal.z);
        sum += tangent * triplanar.blend.z;
    }

    half3 mapped = SafeNormalize(sum);

    return triplanar.localSpace > 0.5h ? half3(TransformObjectToWorldNormal(float3(mapped))) : mapped;
}

void InitializeHiddenBullTriplanarSurfaceData(HiddenBullTriplanar triplanar, float3 shift, half coverage,
                                              out SurfaceData outSurfaceData, out half3 mappedNormalWS)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = _BaseColor;
    float3 detail = shift;

#ifdef _HB_BASE_MAP
    half4 painted = HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), triplanar, shift);

    #ifdef _HB_BRUSH
        half4 original = HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), triplanar,
                                            float3(0.0, 0.0, 0.0));
        half keep = HB_PaintKeep(painted, original);

        painted = half4(lerp(original.rgb, painted.rgb, keep), original.a);
        detail = shift * keep;
    #endif

    albedoAlpha *= painted;
#endif

    HB_ResolveSurfaceColour(albedoAlpha, coverage, outSurfaceData);

    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);

#ifdef _NORMALMAP
    mappedNormalWS = HB_SampleTriplanarNormal(triplanar, detail);
#else
    mappedNormalWS = half3(0.0h, 0.0h, 0.0h);
#endif

#ifdef _HB_MASK_MAP
    HB_ResolveSurfaceMask(HB_SampleTriplanar(TEXTURE2D_ARGS(_MaskMap, sampler_MaskMap), triplanar, detail),
                          outSurfaceData);
#else
    HB_ResolveSurfaceMask(half4(0.0h, 1.0h, 0.0h, 1.0h), outSurfaceData);
#endif

#ifdef _HB_EMISSION
    outSurfaceData.emission = HB_SampleTriplanar(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), triplanar,
                                                 detail).rgb * half3(_EmissionColor.rgb);
#else
    outSurfaceData.emission = 0.0h;
#endif
}

#endif

HiddenBullStyleData InitializeHiddenBullStyleData()
{
    HiddenBullStyleData style;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedTones = _BakedTones;
    style.bakedSoftness = _BakedSoftness;
    style.rimColor = _RimColor.rgb;
    style.rimPower = _RimPower;
    style.rimIntensity = _RimIntensity;
    style.brushObjectSpace = _BrushObjectSpace;
    style.brushAnchor = float3(0.0, 0.0, 0.0);
    style.brushShading = _BrushShading;
    style.brushAmbient = _BrushAmbient;
    style.brushAlbedo = _BrushAlbedo;
    style.brushRelief = _BrushRelief;
    return style;
}

#endif
