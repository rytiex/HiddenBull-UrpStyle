#ifndef HB_TRANSPARENT_SHADOW_PASS_INCLUDED
#define HB_TRANSPARENT_SHADOW_PASS_INCLUDED

#ifndef HB_TRANSPARENT
    #define HB_TRANSPARENT
#endif

#define HB_SHADOW_CASTER
#define HB_TINT_CASTER

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"

#define HB_SHADOW_TINT_SCATTER  0.5
#define HB_SHADOW_TINT_FLOOR    1e-4

float4 HB_ShadowTintFragment(HBDepthVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 base = HB_SampleSurfaceBase(HB_DepthSurface(input, IS_FRONT_VFACE(frontFace, true, false)));

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    half3 transmission = _BlendMode > 2.5 ? HB_TintTransmission(base.rgb, base.a)
                                          : (1.0h - base.a) + base.rgb * (base.a * HB_SHADOW_TINT_SCATTER);
    half grey = HB_TintMean(transmission);

    transmission = _ColouredShadow > 0.5 ? transmission : half3(grey, grey, grey);

#if UNITY_REVERSED_Z
    float depth = input.positionCS.z;
#else
    float depth = 1.0 - input.positionCS.z;
#endif

    return float4(-log(max(float3(transmission), HB_SHADOW_TINT_FLOOR)), depth);
}

#endif
