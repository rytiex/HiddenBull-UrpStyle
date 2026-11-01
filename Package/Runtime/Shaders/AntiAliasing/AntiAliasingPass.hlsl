#ifndef HB_ANTI_ALIASING_PASS_INCLUDED
#define HB_ANTI_ALIASING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_HB_AntiAliasingSource);
float4 _HB_AntiAliasingStyle;

#define HB_AA_STEPS         ((int)_HB_AntiAliasingStyle.x)
#define HB_AA_SOFTNESS      half(_HB_AntiAliasingStyle.y)
#define HB_AA_EDGE_LEAST    0.0312h
#define HB_AA_EDGE_SHARE    0.125h
#define HB_AA_END           0.25h

static const float HB_AA_STRIDES[12] = { 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0, 8.0, 8.0 };

float4 HB_AntiAliasingVertex(uint vertexID : SV_VertexID) : SV_POSITION
{
    return GetFullScreenTriangleVertexPosition(vertexID);
}

half4 HB_AASample(float2 uv)
{
    return half4(SAMPLE_TEXTURE2D_LOD(_HB_AntiAliasingSource, sampler_LinearClamp, uv, 0));
}

half HB_AALuma(half3 colour)
{
    return sqrt(dot(saturate(colour), half3(0.299h, 0.587h, 0.114h)));
}

half HB_AALumaAt(float2 uv)
{
    return HB_AALuma(HB_AASample(uv).rgb);
}

half4 HB_AntiAliasingFragment(float4 positionCS : SV_POSITION) : SV_Target
{
    float width, height;
    _HB_AntiAliasingSource.GetDimensions(width, height);

    float2 texel = 1.0 / float2(width, height);
    float2 uv = positionCS.xy * texel;
    half4 colour = HB_AASample(uv);
    half middle = HB_AALuma(colour.rgb);
    half north = HB_AALumaAt(uv + float2(0.0, -texel.y));
    half south = HB_AALumaAt(uv + float2(0.0, texel.y));
    half east = HB_AALumaAt(uv + float2(texel.x, 0.0));
    half west = HB_AALumaAt(uv + float2(-texel.x, 0.0));
    half most = max(middle, max(max(north, south), max(east, west)));
    half least = min(middle, min(min(north, south), min(east, west)));
    half range = most - least;

    if (range < max(HB_AA_EDGE_LEAST, most * HB_AA_EDGE_SHARE))
        return colour;

    half northWest = HB_AALumaAt(uv - texel);
    half southEast = HB_AALumaAt(uv + texel);
    half northEast = HB_AALumaAt(uv + float2(texel.x, -texel.y));
    half southWest = HB_AALumaAt(uv + float2(-texel.x, texel.y));

    half across = abs(northWest + southWest - 2.0h * west) + abs(north + south - 2.0h * middle) * 2.0h
                + abs(northEast + southEast - 2.0h * east);
    half along = abs(northWest + northEast - 2.0h * north) + abs(west + east - 2.0h * middle) * 2.0h
               + abs(southWest + southEast - 2.0h * south);
    bool flat = across >= along;

    half around = (2.0h * (north + south + east + west) + northWest + northEast + southWest + southEast) / 12.0h;
    half detail = saturate(abs(around - middle) / range);

    detail = detail * detail * (3.0h - 2.0h * detail);
    detail *= detail * HB_AA_SOFTNESS;

    half before = flat ? north : west;
    half after = flat ? south : east;
    half riseBefore = abs(before - middle);
    half riseAfter = abs(after - middle);
    bool toBefore = riseBefore >= riseAfter;
    half limit = max(riseBefore, riseAfter) * HB_AA_END;
    half level = ((toBefore ? before : after) + middle) * 0.5h;
    float side = (flat ? texel.y : texel.x) * (toBefore ? -1.0 : 1.0);
    float2 walk = flat ? float2(texel.x, 0.0) : float2(0.0, texel.y);
    float2 start = uv + (flat ? float2(0.0, side * 0.5) : float2(side * 0.5, 0.0));
    float2 forward = start + walk;
    float2 backward = start - walk;
    half gapForward = HB_AALumaAt(forward) - level;
    half gapBackward = HB_AALumaAt(backward) - level;
    bool openForward = abs(gapForward) < limit;
    bool openBackward = abs(gapBackward) < limit;

    [loop]
    for (int step = 1; step < HB_AA_STEPS && (openForward || openBackward); step++)
    {
        if (openForward)
        {
            forward += walk * HB_AA_STRIDES[step];
            gapForward = HB_AALumaAt(forward) - level;
            openForward = abs(gapForward) < limit;
        }

        if (openBackward)
        {
            backward -= walk * HB_AA_STRIDES[step];
            gapBackward = HB_AALumaAt(backward) - level;
            openBackward = abs(gapBackward) < limit;
        }
    }

    float reachForward = flat ? forward.x - uv.x : forward.y - uv.y;
    float reachBackward = flat ? uv.x - backward.x : uv.y - backward.y;
    bool forwardNearer = reachForward <= reachBackward;
    float nearest = forwardNearer ? reachForward : reachBackward;
    half gap = forwardNearer ? gapForward : gapBackward;
    float blend = (gap < 0.0h) != (middle - level < 0.0h) ? 0.5 - nearest / (reachForward + reachBackward) : 0.0;

    blend = max(blend, float(detail));

    return HB_AASample(uv + (flat ? float2(0.0, side) : float2(side, 0.0)) * blend);
}

#endif
