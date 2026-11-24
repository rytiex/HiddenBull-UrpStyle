#ifndef HB_LENS_FLARE_SUN_INCLUDED
#define HB_LENS_FLARE_SUN_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleClouds.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_HB_FlareSun);
float4 _HB_FlareParams;
float4 _HB_BloomParams;

#define HB_FLARE_SUN_UV       _HB_FlareParams.xy
#define HB_FLARE_SUN_IN_FRONT _HB_FlareParams.z
#define HB_FLARE_SUN_ASPECT   _HB_FlareParams.w
#define HB_FLARE_SUN_TAPS     5
#define HB_FLARE_SUN_RADIUS   0.012
#define HB_FLARE_SUN_MARGIN   0.04
#define HB_FLARE_SUN_HORIZON  0.02
#define HB_FLARE_DEBUG_RING   0.03
#define HB_FLARE_DEBUG_LINE   0.004
#define HB_FLARE_DEBUG_BAR    0.04
#define HB_FLARE_DEBUG_WIDTH  0.3

float4 HB_FlareSunVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

float HB_FlareSunSky(float2 uv, float2 size)
{
    uint2 pixel = (uint2)min(saturate(uv) * size, size - 1.0);
    float raw = LOAD_TEXTURE2D_X(_CameraDepthTexture, pixel).r;

#if UNITY_REVERSED_Z
    return raw <= 0.0 ? 1.0 : 0.0;
#else
    return raw >= 1.0 ? 1.0 : 0.0;
#endif
}

half HB_FlareSunClouds(half3 direction)
{
    half clear = 1.0h;

    UNITY_BRANCH
    if (_HB_CloudParams.x > 0.0 && direction.y > 0.0h)
        clear = 1.0h - saturate(HB_CloudsGrad(direction, 0.0, 0.0).a * smoothstep(0.0h, 0.03h, direction.y));

    return clear;
}

float4 HB_FlareSunVisibility(float4 positionCS : SV_POSITION) : SV_Target
{
    UNITY_BRANCH
    if (HB_FLARE_SUN_IN_FRONT <= 0.0)
        return 0.0;

    float3 direction = normalize(_HB_SunDirection.xyz);
    float2 uv = HB_FLARE_SUN_UV;
    float2 inside = min(uv, 1.0 - uv);
    float edge = saturate(min(inside.x, inside.y) / HB_FLARE_SUN_MARGIN);

    UNITY_BRANCH
    if (edge <= 0.0)
        return float4(uv, 0.0, 0.0);

    float2 radius = HB_FLARE_SUN_RADIUS * float2(1.0 / HB_FLARE_SUN_ASPECT, 1.0);
    float sky = 0.0;

    UNITY_UNROLL
    for (int y = 0; y < HB_FLARE_SUN_TAPS; y++)
    {
        UNITY_UNROLL
        for (int x = 0; x < HB_FLARE_SUN_TAPS; x++)
        {
            float2 grid = float2(x, y) * (2.0 / (HB_FLARE_SUN_TAPS - 1)) - 1.0;

            sky += HB_FlareSunSky(uv + grid * radius, _HB_BloomParams.xy);
        }
    }

    sky /= HB_FLARE_SUN_TAPS * HB_FLARE_SUN_TAPS;

    float horizon = smoothstep(-HB_FLARE_SUN_HORIZON, HB_FLARE_SUN_HORIZON, direction.y);

    return float4(uv, sky * edge * horizon * HB_FlareSunClouds(half3(direction)), 0.0);
}

float4 HB_FlareSunDebug(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * _HB_BloomParams.zw;
    float3 colour = HB_FlareSunSky(uv, 1.0 / _HB_BloomParams.zw) * 0.3;
    float ring = abs(length((uv - HB_FLARE_SUN_UV) * float2(HB_FLARE_SUN_ASPECT, 1.0)) - HB_FLARE_DEBUG_RING);

    if (HB_FLARE_SUN_IN_FRONT > 0.0 && ring < HB_FLARE_DEBUG_LINE)
        colour = float3(1.0, 0.1, 0.1);

    if (uv.y > 1.0 - HB_FLARE_DEBUG_BAR && uv.x < HB_FLARE_DEBUG_WIDTH)
    {
        float visibility = LOAD_TEXTURE2D(_HB_FlareSun, int2(0, 0)).z;

        colour = _HB_BloomParams.x < 0.5 ? float3(1.0, 0.0, 1.0)
               : uv.x < visibility * HB_FLARE_DEBUG_WIDTH ? float3(0.1, 1.0, 0.1) : float3(0.05, 0.05, 0.05);
    }

    return float4(colour, 1.0);
}

#endif
