#ifndef HB_REFRACTION_BLUR_PASS_INCLUDED
#define HB_REFRACTION_BLUR_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_HB_BlurSource);
float4 _HB_BlurSourceTexel;
float4 _HB_BlurTargetSize;
float _HB_BlurLod;

float4 HB_RefractionBlurVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

half3 HB_BlurTap(float2 uv, float2 offset)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_BlurSource, sampler_LinearClamp, uv + offset * _HB_BlurSourceTexel.xy,
                                _HB_BlurLod).rgb;
}

half4 HB_RefractionDownsampleFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * _HB_BlurTargetSize.zw;

    half3 centre = HB_BlurTap(uv, float2(-1.0, -1.0)) + HB_BlurTap(uv, float2(1.0, -1.0))
                 + HB_BlurTap(uv, float2(-1.0, 1.0)) + HB_BlurTap(uv, float2(1.0, 1.0));

    half3 middle = HB_BlurTap(uv, float2(0.0, 0.0));
    half3 sides = HB_BlurTap(uv, float2(-2.0, 0.0)) + HB_BlurTap(uv, float2(2.0, 0.0))
                + HB_BlurTap(uv, float2(0.0, -2.0)) + HB_BlurTap(uv, float2(0.0, 2.0));
    half3 corners = HB_BlurTap(uv, float2(-2.0, -2.0)) + HB_BlurTap(uv, float2(2.0, -2.0))
                  + HB_BlurTap(uv, float2(-2.0, 2.0)) + HB_BlurTap(uv, float2(2.0, 2.0));

    half3 colour = centre * 0.125h + middle * 0.125h + sides * 0.0625h + corners * 0.03125h;

    return half4(colour, 1.0h);
}

#endif
