#ifndef HIDDENBULL_URPSTYLE_LIT_DEPTH_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_DEPTH_PASS_INCLUDED

#include "LitSurfaceVertex.hlsl"

#if defined(HB_SHADOW_CASTER)

half4 HiddenBullShadowFragment(HiddenBullSurfaceVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(HB_SurfaceFromVaryings(input, true), 0.0h);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return 0;
}

#elif defined(HB_SHADOW_TINT)

#define HB_GLASS_SHADOW 0.8h

half4 HiddenBullShadowTintFragment(HiddenBullSurfaceVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 colour = HB_SampleSurfaceBase(HB_SurfaceFromVaryings(input, true));
    half alpha = AlphaDiscard(colour.a, _Cutoff);

    half3 filter = lerp(half3(1.0h, 1.0h, 1.0h), colour.rgb, alpha);

#ifdef _HB_REFRACTION
    return half4(filter * HB_GLASS_SHADOW, 1.0h);
#else
    return half4(filter, 1.0h - alpha);
#endif
}

#elif defined(HB_DEPTH_NORMALS)

void HiddenBullDepthNormalsFragment(
    HiddenBullSurfaceVaryings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HiddenBullSurfaceInput surface = HB_SurfaceFromVaryings(input, IS_FRONT_VFACE(frontFace, true, false));

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(surface, 0.0h);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    outNormalWS = half4(HB_ResolveNormal(surface, surface.normalWS * surface.facing), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HiddenBullDepthFragment(HiddenBullSurfaceVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(HB_SurfaceFromVaryings(input, true), 0.0h);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return input.positionCS.z;
}

#endif

#endif
