#ifndef HIDDENBULL_URPSTYLE_COMMON_INCLUDED
#define HIDDENBULL_URPSTYLE_COMMON_INCLUDED

#define HB_EPSILON 1e-4h

half HB_SafeDiv(half a, half b)
{
    return a * rcp(max(b, HB_EPSILON));
}

half HB_Smooth01(half x)
{
    return x * x * (3.0h - 2.0h * x);
}

struct HiddenBullStyleData
{
    half diffuseWrap;
    half diffuseSoftness;
    half shadowTerminator;
    half3 rimColor;
    half rimPower;
    half rimIntensity;
    half brushObjectSpace;
    half brushShading;
    half brushAlbedo;
    half brushRelief;
    half brushWarmth;
};

#endif
