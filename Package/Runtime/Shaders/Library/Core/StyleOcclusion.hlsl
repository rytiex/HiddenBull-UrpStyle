#ifndef HB_STYLE_OCCLUSION_INCLUDED
#define HB_STYLE_OCCLUSION_INCLUDED

#define HB_OCCLUSION_FINER 2
#define HB_OCCLUSION_CLOSEST 1.5
#define HB_OCCLUSION_OPEN 1.0e9

Texture2D<float> _HB_OcclusionDepth;

float4x4 _HB_OcclusionView;
float4 _HB_OcclusionLens;
float4 _HB_OcclusionSize;

bool HB_BoxHidden(float3 center, float3 extent)
{
    if (_HB_OcclusionSize.w < 0.5)
        return false;

    float3 reach = extent + _HB_OcclusionLens.w;
    float3 middle = mul(_HB_OcclusionView, float4(center, 1.0)).xyz;
    float3 sideX = _HB_OcclusionView._m00_m10_m20 * reach.x;
    float3 sideY = _HB_OcclusionView._m01_m11_m21 * reach.y;
    float3 sideZ = _HB_OcclusionView._m02_m12_m22 * reach.z;
    float2 low = 1.0e9;
    float2 high = -1.0e9;
    float nearest = 1.0e9;

    [unroll]
    for (int corner = 0; corner < 8; corner++)
    {
        float3 pick = float3(corner & 1, (corner >> 1) & 1, corner >> 2) * 2.0 - 1.0;
        float3 view = middle + sideX * pick.x + sideY * pick.y + sideZ * pick.z;
        float2 uv = view.xy / max(-view.z, _HB_OcclusionLens.z) * _HB_OcclusionLens.xy * 0.5 + 0.5;

        low = min(low, uv);
        high = max(high, uv);
        nearest = min(nearest, -view.z);
    }

    if (nearest <= _HB_OcclusionLens.z || any(high <= 0.0) || any(low >= 1.0))
        return false;

    float2 shift = _HB_OcclusionLens.w / HB_OCCLUSION_CLOSEST * abs(_HB_OcclusionLens.xy) * 0.5;

    low = saturate(low - shift);
    high = saturate(high + shift);

    float2 span = (high - low) * _HB_OcclusionSize.xy;
    int level = max((int)ceil(log2(max(max(span.x, span.y), 1.0))) - HB_OCCLUSION_FINER, 0);

    if (level >= (int)_HB_OcclusionSize.z)
        return false;

    uint2 size = (uint2)_HB_OcclusionSize.xy;
    uint2 origin = level == 0 ? uint2(0u, 0u) : uint2(size.x, size.y - (size.y >> (level - 1)));
    uint2 texels = size >> level;
    uint2 first = (uint2)(low * (float2)texels);
    uint2 last = min((uint2)(high * (float2)texels), texels - 1u);

    for (uint y = first.y; y <= last.y; y++)
    {
        for (uint x = first.x; x <= last.x; x++)
        {
            if (_HB_OcclusionDepth.Load(int3(origin + uint2(x, y), 0)) >= nearest)
                return false;
        }
    }

    return true;
}

#endif
