#ifndef HB_CAVITY_PASS_INCLUDED
#define HB_CAVITY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

#define HB_CAVITY_RIDGE      half(_HB_Cavity.x)
#define HB_CAVITY_VALLEY     half(_HB_Cavity.y)
#define HB_CAVITY_WIDTH      _HB_Cavity.z
#define HB_CAVITY_DEPTH_GAP  0.03
#define HB_CAVITY_DISTANCE   _HB_Cavity.w
#define HB_CAVITY_GAIN       2.0h
#define HB_CAVITY_RIDGE_GAIN 2.0h
#define HB_CAVITY_FADE       0.25
#define HB_CAVITY_NEAR       8.0
#define HB_CAVITY_NEAR_MOST  4.0
#define HB_CAVITY_STEP       0.02

struct HBCavityVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

HBCavityVaryings HB_CavityVertex(uint vertexID : SV_VertexID)
{
    HBCavityVaryings output;
    output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
    output.uv = GetFullScreenTriangleTexCoord(vertexID);
    return output;
}

bool HB_CavitySurface(float depth)
{
#if UNITY_REVERSED_Z
    return depth > 0.0;
#else
    return depth < 1.0;
#endif
}

half HB_CavityOn(float2 uv)
{
    return half(saturate(SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture,
                                            UnityStereoTransformScreenSpaceTex(uv)).a));
}

float3 HB_CavityNormal(float2 uv)
{
    return mul((float3x3)UNITY_MATRIX_V, SampleSceneNormals(uv));
}

float3 HB_CavityTap(float2 uv, float eye, float3 centre)
{
    float tap = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);

    return abs(tap - eye) > eye * HB_CAVITY_DEPTH_GAP ? centre : HB_CavityNormal(uv);
}

float HB_CavityAxis(float change)
{
    return sign(change) * max(abs(change) - HB_CAVITY_STEP, 0.0);
}

half HB_CavityCurve(float2 uv)
{
    float depth = SampleSceneDepth(uv);

    if (!HB_CavitySurface(depth))
        return 0.0h;

    float eye = LinearEyeDepth(depth, _ZBufferParams);
    float reachEnd = max(HB_CAVITY_DISTANCE, 1.0);
    half fade = half(saturate((reachEnd - eye) / (reachEnd * HB_CAVITY_FADE))) * HB_CavityOn(uv);

    if (fade <= 0.0h)
        return 0.0h;

    float3 centre = HB_CavityNormal(uv);
    float width = max(HB_CAVITY_WIDTH, 0.5) * clamp(HB_CAVITY_NEAR / eye, 1.0, HB_CAVITY_NEAR_MOST);
    float2 reach = _ScreenSize.zw * width;

#if UNITY_UV_STARTS_AT_TOP
    reach.y = -reach.y;
#endif

    float3 right = HB_CavityTap(uv + float2(reach.x, 0.0), eye, centre);
    float3 left = HB_CavityTap(uv - float2(reach.x, 0.0), eye, centre);
    float3 up = HB_CavityTap(uv + float2(0.0, reach.y), eye, centre);
    float3 down = HB_CavityTap(uv - float2(0.0, reach.y), eye, centre);

    return half(HB_CavityAxis(right.x - left.x) + HB_CavityAxis(up.y - down.y)) * HB_CAVITY_GAIN * fade;
}

half4 HB_CavityFragment(HBCavityVaryings input) : SV_Target
{
    half curve = HB_CavityCurve(input.uv);
    half ridge = saturate(curve) * HB_CAVITY_RIDGE * HB_CAVITY_RIDGE_GAIN;
    half valley = saturate(-curve) * HB_CAVITY_VALLEY;

    return half4(ridge, ridge, ridge, 1.0h - valley);
}

half4 HB_CavityDebugFragment(HBCavityVaryings input) : SV_Target
{
    half curve = HB_CavityCurve(input.uv);
    half tone = saturate(0.5h + curve);

    return half4(tone, tone, tone, 1.0h);
}

#endif
