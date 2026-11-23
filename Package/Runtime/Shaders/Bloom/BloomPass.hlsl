#ifndef HB_BLOOM_PASS_INCLUDED
#define HB_BLOOM_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_HB_BloomSource);
TEXTURE2D(_HB_BloomLow);
float4 _HB_BloomParams;
float4 _HB_BloomTint;

#define HB_BLOOM_THRESHOLD  _HB_BloomParams.x
#define HB_BLOOM_KNEE       _HB_BloomParams.y
#define HB_BLOOM_SPREAD     _HB_BloomParams.z
#define HB_BLOOM_INTENSITY  _HB_BloomParams.w
#define HB_BLOOM_CEILING    64.0

float4 HB_BloomVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

float3 HB_BloomTap(TEXTURE2D(source), float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(source, sampler_LinearClamp, uv, 0).rgb;
}

float2 HB_BloomTexel(TEXTURE2D(source))
{
    float width, height;
    source.GetDimensions(width, height);

    return 1.0 / float2(width, height);
}

float3 HB_BloomThreshold(float3 colour)
{
    colour = min(colour, HB_BLOOM_CEILING);

    float bright = max(colour.r, max(colour.g, colour.b));
    float soft = clamp(bright - HB_BLOOM_THRESHOLD + HB_BLOOM_KNEE, 0.0, 2.0 * HB_BLOOM_KNEE);

    soft = soft * soft / (4.0 * HB_BLOOM_KNEE + 1e-4);

    return colour * (max(soft, bright - HB_BLOOM_THRESHOLD) / max(bright, 1e-4));
}

float HB_BloomWeight(float3 colour)
{
    return 1.0 / (1.0 + max(colour.r, max(colour.g, colour.b)));
}

float3 HB_BloomBox(TEXTURE2D(source), float2 uv, float2 texel)
{
    return (HB_BloomTap(source, uv + texel * float2(-1.0, -1.0)) + HB_BloomTap(source, uv + texel * float2(1.0, -1.0))
          + HB_BloomTap(source, uv + texel * float2(-1.0, 1.0)) + HB_BloomTap(source, uv + texel * float2(1.0, 1.0)))
          * 0.25;
}

float3 HB_BloomStable(TEXTURE2D(source), float2 uv, float2 texel)
{
    float3 inner = HB_BloomBox(source, uv, texel);
    float3 a = HB_BloomBox(source, uv + texel * float2(-1.0, -1.0), texel);
    float3 b = HB_BloomBox(source, uv + texel * float2(1.0, -1.0), texel);
    float3 c = HB_BloomBox(source, uv + texel * float2(-1.0, 1.0), texel);
    float3 d = HB_BloomBox(source, uv + texel * float2(1.0, 1.0), texel);

    float wi = HB_BloomWeight(inner) * 0.5;
    float wa = HB_BloomWeight(a) * 0.125;
    float wb = HB_BloomWeight(b) * 0.125;
    float wc = HB_BloomWeight(c) * 0.125;
    float wd = HB_BloomWeight(d) * 0.125;

    return (inner * wi + a * wa + b * wb + c * wc + d * wd) / (wi + wa + wb + wc + wd);
}

float4 HB_BloomPrefilter(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 texel = HB_BloomTexel(_HB_BloomSource);
    float2 uv = positionCS.xy * 2.0 * texel;

#if defined(HB_BLOOM_STABLE)
    float3 colour = HB_BloomStable(_HB_BloomSource, uv, texel);
#else
    float3 colour = HB_BloomBox(_HB_BloomSource, uv, texel);
#endif

    return float4(HB_BloomThreshold(colour), 0.0);
}

float4 HB_BloomDown(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 texel = HB_BloomTexel(_HB_BloomSource);
    float2 uv = positionCS.xy * 2.0 * texel;
    float3 colour = HB_BloomTap(_HB_BloomSource, uv) * 4.0 + HB_BloomBox(_HB_BloomSource, uv, texel) * 4.0;

    return float4(colour * 0.125, 0.0);
}

float3 HB_BloomTent(float2 uv, float2 texel)
{
    float3 colour = HB_BloomTap(_HB_BloomLow, uv) * 4.0;

    colour += (HB_BloomTap(_HB_BloomLow, uv + texel * float2(-1.0, 0.0))
             + HB_BloomTap(_HB_BloomLow, uv + texel * float2(1.0, 0.0))
             + HB_BloomTap(_HB_BloomLow, uv + texel * float2(0.0, -1.0))
             + HB_BloomTap(_HB_BloomLow, uv + texel * float2(0.0, 1.0))) * 2.0;

    colour += HB_BloomTap(_HB_BloomLow, uv + texel * float2(-1.0, -1.0))
            + HB_BloomTap(_HB_BloomLow, uv + texel * float2(1.0, -1.0))
            + HB_BloomTap(_HB_BloomLow, uv + texel * float2(-1.0, 1.0))
            + HB_BloomTap(_HB_BloomLow, uv + texel * float2(1.0, 1.0));

    return colour / 16.0;
}

float4 HB_BloomUp(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 uv = positionCS.xy * HB_BloomTexel(_HB_BloomSource);
    float3 high = HB_BloomTap(_HB_BloomSource, uv);
    float3 low = HB_BloomTent(uv, HB_BloomTexel(_HB_BloomLow));

    return float4(lerp(high, low, HB_BLOOM_SPREAD), 0.0);
}

float4 HB_BloomComposite(float4 positionCS : SV_POSITION) : SV_Target
{
    float2 texel = HB_BloomTexel(_HB_BloomLow);
    float width, height;
    _HB_BloomLow.GetDimensions(width, height);

    float2 uv = positionCS.xy / (float2(width, height) * 2.0);
    float3 glow = HB_BloomTent(uv, texel);

    return float4(glow * _HB_BloomTint.rgb * HB_BLOOM_INTENSITY, 0.0);
}

#endif
