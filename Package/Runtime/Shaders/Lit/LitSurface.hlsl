#ifndef HB_LIT_SURFACE_INCLUDED
#define HB_LIT_SURFACE_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitInput.hlsl"

struct HBSurfaceInput
{
    float2 uv;
    float3 positionWS;
    half3 normalWS;
    half3 tangentWS;
    half3 bitangentWS;
    float3 anchorOS;
    half facing;
};

HBSurfaceInput HB_SurfaceInput(float2 uv, float3 positionWS, half3 normalWS, half3 tangentWS, half3 bitangentWS,
                               float3 anchorOS, bool frontFace)
{
    HBSurfaceInput surface;
    surface.uv = uv;
    surface.positionWS = positionWS;
    surface.normalWS = normalWS;
    surface.tangentWS = tangentWS;
    surface.bitangentWS = bitangentWS;
    surface.anchorOS = anchorOS;
    surface.facing = frontFace ? 1.0h : -1.0h;
    return surface;
}

half3 HB_FacingNormal(HBSurfaceInput surface)
{
    return NormalizeNormalPerPixel(surface.normalWS * surface.facing);
}

#ifdef _HB_TRIPLANAR
HBTriplanar HB_SurfaceTriplanar(HBSurfaceInput surface, half3 normalWS)
{
    return HB_SetupTriplanar(surface.positionWS, normalWS, _TriplanarSpace, surface.anchorOS);
}
#endif

half4 HB_SampleSurfaceBase(HBSurfaceInput surface)
{
    half4 colour = _BaseColor;

#ifdef _HB_BASE_MAP
    #ifdef _HB_TRIPLANAR
    colour *= HB_SampleTriplanar(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap),
                                 HB_SurfaceTriplanar(surface, HB_FacingNormal(surface)), float3(0.0, 0.0, 0.0));
    #else
    colour *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, surface.uv);
    #endif
#endif

    return colour;
}

half HB_ResolveAlpha(HBSurfaceInput surface)
{
    return AlphaDiscard(HB_SampleSurfaceBase(surface).a, _Cutoff);
}

half3 HB_ResolveNormal(HBSurfaceInput surface, half3 normalWS)
{
#if defined(_NORMALMAP) && defined(_HB_TRIPLANAR)
    return NormalizeNormalPerPixel(HB_SampleTriplanarNormal(
        HB_SurfaceTriplanar(surface, NormalizeNormalPerPixel(normalWS)), float3(0.0, 0.0, 0.0)));
#elif defined(_NORMALMAP)
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, surface.uv), _BumpScale);

    return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,
                                                           half3x3(surface.tangentWS, surface.bitangentWS, normalWS)));
#else
    return NormalizeNormalPerPixel(normalWS);
#endif
}

HBBrushSample HB_ResolveBrush(HBSurfaceInput surface)
{
#ifdef _HB_BRUSH
    return HB_SampleBrush(surface.positionWS, HB_FacingNormal(surface), _BrushObjectSpace, surface.anchorOS,
                          max(half(_BrushScale), half(HB_EPSILON)), HB_SampleBrushMask(surface.uv));
#else
    return HB_NoBrush();
#endif
}

void HB_ResolveSurface(HBSurfaceInput surface, float2 uvShift, float2 uvDdx, float2 uvDdy, float3 strokeShift,
                       out SurfaceData surfaceData, out half3 mappedNormalWS)
{
#ifdef _HB_TRIPLANAR
    HBTriplanar triplanar = HB_SurfaceTriplanar(surface, HB_FacingNormal(surface));

    HB_InitializeTriplanarSurfaceData(triplanar, HB_TriplanarShift(strokeShift, triplanar.localSpace) * _BrushPaint,
                                      surfaceData, mappedNormalWS);
#else
    HB_InitializeSurfaceData(surface.uv, uvShift, uvDdx, uvDdy, surfaceData);
    mappedNormalWS = half3(0.0h, 0.0h, 0.0h);
#endif
}

#endif
