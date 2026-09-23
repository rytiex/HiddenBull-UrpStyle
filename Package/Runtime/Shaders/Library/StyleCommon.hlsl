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

struct HiddenBullStyleData
{
    half diffuseWrap;
    half diffuseSoftness;
    half3 rimColor;
    half rimPower;
    half rimIntensity;
    half brushObjectSpace;
    half brushAmbient;
    half brushUvWarp;
    float3 brushAnchor;
    half brushShading;
    half brushAlbedo;
    half brushRelief;
};

#endif
