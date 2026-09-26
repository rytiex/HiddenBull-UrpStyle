#ifndef HIDDENBULL_URPSTYLE_COMMON_INCLUDED
#define HIDDENBULL_URPSTYLE_COMMON_INCLUDED

#define HB_EPSILON 1e-4h
#define HB_BRUSH_FORM_RELIEF 0.55h

float4 _HB_KeyDirection;
float4 _HB_KeyColor;

half HB_WrappedDiffuse(half NdotL, half wrap, half softness)
{
    half w = saturate(wrap);
    half invW = rcp(1.0h + w);

    half d = saturate((NdotL + w) * invW);

    half terminator = w * invW;

    half halfBand = 0.5h * max(softness, HB_EPSILON);

    half e0 = max(terminator - halfBand, 0.0h);
    half e1 = max(min(terminator + halfBand, 1.0h), e0 + HB_EPSILON);

    return smoothstep(e0, e1, d);
}

half HB_ToneEdge(half light, half edge, half halfBand)
{
    half e0 = max(edge - halfBand, 0.0h);
    half e1 = max(min(edge + halfBand, 1.0h), e0 + HB_EPSILON);

    return smoothstep(e0, e1, light);
}

half HB_ToneBands(half light, half terminator, half halfTone, half softness)
{
    half halfBand = 0.5h * max(softness, HB_EPSILON);
    half upper = terminator + (1.0h - terminator) * saturate(halfTone);

    return 0.5h * (HB_ToneEdge(light, terminator, halfBand) + HB_ToneEdge(light, upper, halfBand));
}

half HB_BakedToneBands(half ratio, half softness)
{
    float stops = log2(max(float(ratio), 1e-4));
    float tread = floor(stops);
    float riser = 0.5 * max(float(softness), 1e-3);

    return half(exp2(tread + smoothstep(0.5 - riser, 0.5 + riser, stops - tread)));
}

struct HiddenBullStyleData
{
    half diffuseWrap;
    half diffuseSoftness;
    half halfTone;
    half bakedTones;
    half bakedSoftness;
    half3 rimColor;
    half rimPower;
    half rimIntensity;
    half brushObjectSpace;
    float3 brushAnchor;
    half brushAmbient;
    half brushShading;
    half brushAlbedo;
    half brushRelief;
};

#endif
