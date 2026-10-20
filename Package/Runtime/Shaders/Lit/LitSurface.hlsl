#ifndef HIDDENBULL_URPSTYLE_LIT_SURFACE_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_SURFACE_INCLUDED

#include "LitInput.hlsl"
#include "../Library/StyleBrush.hlsl"
#include "../Library/StyleWeatherSurface.hlsl"

struct HiddenBullSurfaceInput
{
    float2 uv;
    float3 positionWS;
    half3 normalWS;
    half3 tangentWS;
    half3 bitangentWS;
    float3 anchorOS;
    half facing;
};

HiddenBullSurfaceInput HB_SurfaceInput(float2 uv, float3 positionWS, half3 normalWS, half3 tangentWS,
                                       half3 bitangentWS, float3 anchorOS, bool frontFace)
{
    HiddenBullSurfaceInput surface;
    surface.uv = uv;
    surface.positionWS = positionWS;
    surface.normalWS = normalWS;
    surface.tangentWS = tangentWS;
    surface.bitangentWS = bitangentWS;
    surface.anchorOS = anchorOS;
    surface.facing = frontFace ? 1.0h : -1.0h;
    return surface;
}

half3 HB_FacingNormal(HiddenBullSurfaceInput surface)
{
    return NormalizeNormalPerPixel(surface.normalWS * surface.facing);
}

#ifdef _HB_TRIPLANAR
HiddenBullTriplanar HB_SurfaceTriplanar(HiddenBullSurfaceInput surface, half3 normalWS)
{
    return HB_SetupTriplanar(surface.positionWS, normalWS, _TriplanarSpace, surface.anchorOS);
}
#endif

half4 HB_SampleSurfaceBase(HiddenBullSurfaceInput surface)
{
    half4 colour = _BaseColor;

#ifdef _HB_BASE_MAP
    #ifdef _HB_TRIPLANAR
        colour *= HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap),
                                     HB_SurfaceTriplanar(surface, HB_FacingNormal(surface)),
                                     float3(0.0, 0.0, 0.0));
    #else
        colour *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, surface.uv);
    #endif
#endif

    return colour;
}

half HB_ResolveAlpha(HiddenBullSurfaceInput surface, half coverage)
{
    return AlphaDiscard(HB_StrokeAlpha(HB_SampleSurfaceBase(surface).a, coverage), _Cutoff);
}

half3 HB_ResolveTint(HiddenBullSurfaceInput surface, half alpha)
{
    return lerp(half3(1.0h, 1.0h, 1.0h), HB_SampleSurfaceBase(surface).rgb, alpha);
}

half3 HB_ResolveNormal(HiddenBullSurfaceInput surface, half3 normalWS)
{
#if defined(_NORMALMAP) && defined(_HB_TRIPLANAR)
    return NormalizeNormalPerPixel(HB_SampleTriplanarNormal(
        HB_SurfaceTriplanar(surface, NormalizeNormalPerPixel(normalWS)), float3(0.0, 0.0, 0.0)));
#elif defined(_NORMALMAP)
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, surface.uv), _BumpScale);

    return NormalizeNormalPerPixel(TransformTangentToWorld(
        normalTS, half3x3(surface.tangentWS, surface.bitangentWS, normalWS)));
#else
    return NormalizeNormalPerPixel(normalWS);
#endif
}

HiddenBullBrushSample HB_ResolveBrush(HiddenBullSurfaceInput surface)
{
#ifdef _HB_BRUSH
    return HB_SampleBrush(surface.positionWS, HB_FacingNormal(surface), _BrushObjectSpace, surface.anchorOS,
                          max(half(_BrushScale), HB_EPSILON), HB_SampleBrushMask(surface.uv));
#else
    return HB_NoBrush();
#endif
}

HiddenBullWeather HB_ResolveSurfaceWeather(HiddenBullSurfaceInput surface, half coverage)
{
    return HB_ResolveWeather(surface.positionWS, HB_ResolveNormal(surface, surface.normalWS), coverage,
                             float3(0.0, 0.0, 0.0), surface.anchorOS, half(_ReceiveWeather),
                             half4(_DropDensity, _DropSize, _FlakeDensity, _FlakeSize),
                             half2(_DropLife, _FlakeLife), half2(_Wetness, _SnowCover));
}

void HB_ResolveSurface(HiddenBullSurfaceInput surface, float2 uvShift, float2 uvDdx, float2 uvDdy,
                       float3 strokeShift, half coverage, out SurfaceData surfaceData, out half3 mappedNormalWS)
{
#ifdef _HB_TRIPLANAR
    HiddenBullTriplanar triplanar = HB_SurfaceTriplanar(surface, HB_FacingNormal(surface));

    InitializeHiddenBullTriplanarSurfaceData(triplanar,
                                             HB_TriplanarShift(strokeShift, triplanar.localSpace) * _BrushPaint,
                                             coverage, surfaceData, mappedNormalWS);
#else
    InitializeHiddenBullSurfaceData(surface.uv, uvShift, uvDdx, uvDdy, coverage, surfaceData);
    mappedNormalWS = half3(0.0h, 0.0h, 0.0h);
#endif
}

#endif
