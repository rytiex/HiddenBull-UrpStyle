#ifndef HB_STYLE_SHELTER_INCLUDED
#define HB_STYLE_SHELTER_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

#define HB_SHELTER_CASCADES 3

TEXTURE2D_ARRAY(_HB_Shelter);

float4 _HB_ShelterArea[HB_SHELTER_CASCADES];
float4 _HB_ShelterState;

#define HB_SHELTER_BOUND         _HB_ShelterState.x
#define HB_SHELTER_SIZE          _HB_ShelterState.y
#define HB_SHELTER_BLEND_TEXELS  16.0

#define HB_SHELTER_LIFT          0.35
#define HB_SHELTER_BIAS          0.1
#define HB_SHELTER_RANGE         0.6
#define HB_SHELTER_NONE          -1e5
#define HB_SHELTER_UNDER_SOFT    0.05
#define HB_SHELTER_LIQUID_BIAS   -0.45
#define HB_SHELTER_LIQUID_SOFT   0.3

struct HBShelterSpot
{
    float3 uv;
    float3 next;
    float edge;
};

HBShelterSpot HB_ShelterSpot(float2 xz)
{
    float2 a = (xz - _HB_ShelterArea[0].xy) * _HB_ShelterArea[0].z;
    float2 b = (xz - _HB_ShelterArea[1].xy) * _HB_ShelterArea[1].z;
    float2 c = (xz - _HB_ShelterArea[2].xy) * _HB_ShelterArea[2].z;

    float inA = 0.5 - max(abs(a.x), abs(a.y));
    float inB = 0.5 - max(abs(b.x), abs(b.y));
    float inC = 0.5 - max(abs(c.x), abs(c.y));

    HBShelterSpot spot;

    if (inA > 0.0)
    {
        spot.uv = float3(a + 0.5, 0.0);
        spot.next = float3(b + 0.5, inB > 0.0 ? 1.0 : -1.0);
        spot.edge = inA;
    }
    else if (inB > 0.0)
    {
        spot.uv = float3(b + 0.5, 1.0);
        spot.next = float3(c + 0.5, inC > 0.0 ? 2.0 : -1.0);
        spot.edge = inB;
    }
    else
    {
        spot.uv = float3(c + 0.5, 2.0);
        spot.next = float3(0.0, 0.0, -1.0);
        spot.edge = inC;
    }

    return spot;
}

half4 HB_ShelterBSpline(half t)
{
    half t2 = t * t;
    half t3 = t2 * t;
    half s = 1.0h - t;

    return half4(s * s * s, 3.0h * t3 - 6.0h * t2 + 4.0h, -3.0h * t3 + 3.0h * t2 + 3.0h * t + 1.0h, t3)
         * (1.0h / 6.0h);
}

half HB_ShelterBlock(half4 values, half2 weightX, half2 weightZ)
{
    return dot(half2(values.w, values.z), weightX) * weightZ.x
         + dot(half2(values.x, values.y), weightX) * weightZ.y;
}

// One Gather returns four texels in the order x (0, 1), y (1, 1), z (1, 0), w (0, 0).
half4 HB_ShelterCovered(float3 uv, float y)
{
    float4 roof = _HB_Shelter.GatherRed(sampler_PointClamp, uv);

    return half4(roof > HB_SHELTER_NONE ? saturate((roof - y - HB_SHELTER_BIAS) / HB_SHELTER_RANGE) : 0.0);
}

half HB_ShelterOverhang(float3 uv, float2 blend, float y)
{
    float4 under = _HB_Shelter.GatherGreen(sampler_PointClamp, uv);
    float4 overhang = under > HB_SHELTER_NONE ? saturate((under - y) / HB_SHELTER_UNDER_SOFT) : 0.0;

    return half(lerp(lerp(overhang.w, overhang.z, blend.x), lerp(overhang.x, overhang.y, blend.x), blend.y));
}

half HB_ShelterCoarse(float3 uv, float y)
{
    float2 position = uv.xy * HB_SHELTER_SIZE - 0.5;
    float2 cell = floor(position);
    half2 blend = half2(position - cell);
    half4 covered = HB_ShelterCovered(float3((cell + 1.0) / HB_SHELTER_SIZE, uv.z), y);

    return lerp(lerp(covered.w, covered.z, blend.x), lerp(covered.x, covered.y, blend.x), blend.y);
}

half HB_ShelterCoverAt(HBShelterSpot spot, float y)
{
    half cover = 1.0h;

    if (spot.edge > 0.0)
    {
        float2 state = SAMPLE_TEXTURE2D_ARRAY_LOD(_HB_Shelter, sampler_LinearClamp, spot.uv.xy, spot.uv.z, 0).ba;
        float under = saturate((state.x - y - HB_SHELTER_LIQUID_BIAS) / HB_SHELTER_LIQUID_SOFT);

        cover = half(state.y * (1.0 - under));
    }

    return cover;
}

half HB_ShelterOpenAt(HBShelterSpot spot, float y)
{
    half covered = 0.0h;

    UNITY_BRANCH
    if (spot.edge > 0.0)
    {
        float size = HB_SHELTER_SIZE;
        float2 position = spot.uv.xy * size - 0.5;
        float2 cell = floor(position);

        half4 weightX = HB_ShelterBSpline(half(position.x - cell.x));
        half4 weightZ = HB_ShelterBSpline(half(position.y - cell.y));

        float3 corner = float3(cell / size, spot.uv.z);
        float step = 2.0 / size;

        covered = HB_ShelterBlock(HB_ShelterCovered(corner, y), weightX.xy, weightZ.xy)
                + HB_ShelterBlock(HB_ShelterCovered(corner + float3(step, 0.0, 0.0), y), weightX.zw, weightZ.xy)
                + HB_ShelterBlock(HB_ShelterCovered(corner + float3(0.0, step, 0.0), y), weightX.xy, weightZ.zw)
                + HB_ShelterBlock(HB_ShelterCovered(corner + float3(step, step, 0.0), y), weightX.zw, weightZ.zw);

        covered = max(covered, HB_ShelterOverhang(float3((cell + 1.0) / size, spot.uv.z), position - cell, y));

        half share = half(saturate(spot.edge * size / HB_SHELTER_BLEND_TEXELS));

        UNITY_BRANCH
        if (share < 1.0h)
        {
            half outer = spot.next.z >= 0.0 ? HB_ShelterCoarse(spot.next, y) : 0.0h;

            covered = lerp(outer, covered, share);
        }
    }

    return 1.0h - saturate(covered);
}

half HB_ShelterCover(float3 positionWS)
{
    half cover = 1.0h;

    UNITY_BRANCH
    if (HB_SHELTER_BOUND > 0.5)
        cover = HB_ShelterCoverAt(HB_ShelterSpot(positionWS.xz), positionWS.y);

    return cover;
}

half HB_ShelterOpen(float3 positionWS, half3 normalWS)
{
    half open = 1.0h;

    UNITY_BRANCH
    if (HB_SHELTER_BOUND > 0.5)
        open = HB_ShelterOpenAt(HB_ShelterSpot(positionWS.xz + float2(normalWS.xz) * HB_SHELTER_LIFT), positionWS.y);

    return open;
}

half HB_ShelterExposure(float3 positionWS, half3 normalWS, out half cover)
{
    half open = 1.0h;

    cover = 1.0h;

    UNITY_BRANCH
    if (HB_SHELTER_BOUND > 0.5)
    {
        HBShelterSpot spot = HB_ShelterSpot(positionWS.xz + float2(normalWS.xz) * HB_SHELTER_LIFT);

        cover = HB_ShelterCoverAt(spot, positionWS.y);
        open = HB_ShelterOpenAt(spot, positionWS.y);
    }

    return open;
}

#endif
