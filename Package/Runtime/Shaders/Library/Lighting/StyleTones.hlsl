#ifndef HB_STYLE_TONES_INCLUDED
#define HB_STYLE_TONES_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"

struct HBStyleData
{
    half diffuseWrap;
    half diffuseSoftness;
    half halfTone;
    half bakedTones;
    half bakedSoftness;
    half3 rimColor;
    half rimPower;
    half rimIntensity;
    half brushShading;
    half brushAmbient;
    half brushAlbedo;
    half brushRelief;
};

half HB_ToneEdge(half light, half edge, half halfBand)
{
    half e0 = max(edge - halfBand, 0.0h);
    half e1 = max(min(edge + halfBand, 1.0h), e0 + half(HB_EPSILON));

    return smoothstep(e0, e1, light);
}

half HB_ToneBands(half light, half terminator, half halfTone, half softness)
{
    half halfBand = 0.5h * max(softness, half(HB_EPSILON));
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

#endif
