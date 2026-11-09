#ifndef HB_REFLECT_DEPTH_PASS_INCLUDED
#define HB_REFLECT_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleScreenReflection.hlsl"

TEXTURE2D_FLOAT(_HB_ReflectGlass);

float HB_ReflectDepthFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord.xy;
    float raw = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0).r;
    float eye = raw == UNITY_RAW_FAR_CLIP_VALUE ? HB_REFLECT_SKY_EYE : LinearEyeDepth(raw, _ZBufferParams);
    float glass = SAMPLE_TEXTURE2D_LOD(_HB_ReflectGlass, sampler_PointClamp, uv, 0).r;

    return glass > 0.0 ? min(eye, rcp(glass)) : eye;
}

#endif
