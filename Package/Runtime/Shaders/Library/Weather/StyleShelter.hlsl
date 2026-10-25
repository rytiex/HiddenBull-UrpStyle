#ifndef HB_STYLE_SHELTER_INCLUDED
#define HB_STYLE_SHELTER_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

TEXTURE2D_FLOAT(_HB_ShelterDepth);
TEXTURE2D_FLOAT(_HB_ShelterUnder);
TEXTURE2D_FLOAT(_HB_ShelterFarDepth);

float4x4 _HB_ShelterViewProj;
float4 _HB_ShelterHeight;
float4 _HB_ShelterState;
float4x4 _HB_ShelterFarViewProj;
float4 _HB_ShelterFarHeight;

#define HB_SHELTER_HEIGHT_SCALE  _HB_ShelterHeight.x
#define HB_SHELTER_HEIGHT_BIAS   _HB_ShelterHeight.y
#define HB_SHELTER_RESOLUTION    _HB_ShelterHeight.z
#define HB_SHELTER_BOUND         _HB_ShelterState.x
#define HB_SHELTER_FAR_BOUND     _HB_ShelterState.y
#define HB_SHELTER_FAR_SCALE     _HB_ShelterFarHeight.x
#define HB_SHELTER_FAR_BIAS      _HB_ShelterFarHeight.y
#define HB_SHELTER_FAR_SIZE      _HB_ShelterFarHeight.z
#define HB_SHELTER_BLEND_TEXELS  16.0

#define HB_SHELTER_LIFT          0.35
#define HB_SHELTER_BIAS          0.1
#define HB_SHELTER_RANGE         0.6
#define HB_SHELTER_EMPTY_MARGIN  0.05
#define HB_SHELTER_UNDER_SOFT    0.05

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
half4 HB_ShelterCovered(float2 uv, float y)
{
    float4 depth = GATHER_RED_TEXTURE2D(_HB_ShelterDepth, sampler_PointClamp, uv);
    float4 roof = depth * HB_SHELTER_HEIGHT_SCALE + HB_SHELTER_HEIGHT_BIAS;
    float empty = HB_SHELTER_HEIGHT_BIAS + min(HB_SHELTER_HEIGHT_SCALE, 0.0) + HB_SHELTER_EMPTY_MARGIN;
    float4 covered = saturate((roof - y - HB_SHELTER_BIAS) / HB_SHELTER_RANGE);

    return half4(roof > empty ? covered : 0.0);
}

half HB_ShelterOverhang(float2 cell, float2 blend, float y)
{
    float4 depth = GATHER_RED_TEXTURE2D(_HB_ShelterUnder, sampler_PointClamp, (cell + 1.0) / HB_SHELTER_RESOLUTION);
    float4 under = depth * HB_SHELTER_HEIGHT_SCALE + HB_SHELTER_HEIGHT_BIAS;
    float nearest = max(HB_SHELTER_HEIGHT_BIAS, HB_SHELTER_HEIGHT_SCALE + HB_SHELTER_HEIGHT_BIAS);
    float4 overhang = under < nearest - HB_SHELTER_EMPTY_MARGIN ? saturate((under - y) / HB_SHELTER_UNDER_SOFT) : 0.0;

    return half(lerp(lerp(overhang.w, overhang.z, blend.x), lerp(overhang.x, overhang.y, blend.x), blend.y));
}

float2 HB_ShelterUV(float4x4 viewProjection, float2 xz, out float edge)
{
    float2 uv = mul(viewProjection, float4(xz.x, 0.0, xz.y, 1.0)).xy;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = -uv.y;
#endif
    edge = 0.5 - 0.5 * max(abs(uv.x), abs(uv.y));
    return uv * 0.5 + 0.5;
}

half HB_ShelterFarCovered(float2 uv, float y)
{
    float2 position = uv * HB_SHELTER_FAR_SIZE - 0.5;
    float2 cell = floor(position);
    float2 blend = position - cell;

    float4 depth = GATHER_RED_TEXTURE2D(_HB_ShelterFarDepth, sampler_PointClamp, (cell + 1.0) / HB_SHELTER_FAR_SIZE);
    float4 roof = depth * HB_SHELTER_FAR_SCALE + HB_SHELTER_FAR_BIAS;
    float empty = HB_SHELTER_FAR_BIAS + min(HB_SHELTER_FAR_SCALE, 0.0) + HB_SHELTER_EMPTY_MARGIN;
    float4 covered = roof > empty ? saturate((roof - y - HB_SHELTER_BIAS) / HB_SHELTER_RANGE) : 0.0;

    return half(lerp(lerp(covered.w, covered.z, blend.x), lerp(covered.x, covered.y, blend.x), blend.y));
}

half HB_ShelterOpen(float3 positionWS, half3 normalWS)
{
    half open = 1.0h;

    UNITY_BRANCH
    if (HB_SHELTER_BOUND > 0.5)
    {
        float2 probe = positionWS.xz + float2(normalWS.xz) * HB_SHELTER_LIFT;
        float edge;
        float2 uv = HB_ShelterUV(_HB_ShelterViewProj, probe, edge);
        half covered = 0.0h;
        half nearShare = 0.0h;

        UNITY_BRANCH
        if (edge > 0.0)
        {
            float size = HB_SHELTER_RESOLUTION;
            float2 position = uv * size - 0.5;
            float2 cell = floor(position);

            half4 weightX = HB_ShelterBSpline(half(position.x - cell.x));
            half4 weightZ = HB_ShelterBSpline(half(position.y - cell.y));

            float2 corner = cell / size;
            float step = 2.0 / size;
            float y = positionWS.y;

            covered = HB_ShelterBlock(HB_ShelterCovered(corner, y), weightX.xy, weightZ.xy)
                    + HB_ShelterBlock(HB_ShelterCovered(corner + float2(step, 0.0), y), weightX.zw, weightZ.xy)
                    + HB_ShelterBlock(HB_ShelterCovered(corner + float2(0.0, step), y), weightX.xy, weightZ.zw)
                    + HB_ShelterBlock(HB_ShelterCovered(corner + float2(step, step), y), weightX.zw, weightZ.zw);

            covered = max(covered, HB_ShelterOverhang(cell, position - cell, y));
            nearShare = half(saturate(edge * size / HB_SHELTER_BLEND_TEXELS));
        }

        UNITY_BRANCH
        if (nearShare < 1.0h && HB_SHELTER_FAR_BOUND > 0.5)
        {
            float farEdge;
            float2 farUV = HB_ShelterUV(_HB_ShelterFarViewProj, probe, farEdge);
            half far = farEdge > 0.0 ? HB_ShelterFarCovered(farUV, positionWS.y) : 0.0h;

            covered = lerp(far, covered, nearShare);
        }

        open = 1.0h - saturate(covered);
    }

    return open;
}

#endif
