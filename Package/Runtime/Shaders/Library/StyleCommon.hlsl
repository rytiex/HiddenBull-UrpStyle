#ifndef HIDDENBULL_URPSTYLE_COMMON_INCLUDED
#define HIDDENBULL_URPSTYLE_COMMON_INCLUDED

#define HB_EPSILON 1e-4h

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
