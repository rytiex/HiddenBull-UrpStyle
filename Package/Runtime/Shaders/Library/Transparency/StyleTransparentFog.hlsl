#ifndef HB_STYLE_TRANSPARENT_FOG_INCLUDED
#define HB_STYLE_TRANSPARENT_FOG_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Atmosphere/StyleAtmosphere.hlsl"

struct HBTransparentFog
{
    half3 colour;
    half clear;
};

HBTransparentFog HB_TransparentFog(float3 positionWS)
{
    float3 cameraPositionWS = GetCameraPositionWS();
    half amount = HB_FogAmount(positionWS, cameraPositionWS);

    HBTransparentFog fog;
    fog.colour = half3(0.0h, 0.0h, 0.0h);
    fog.clear = 1.0h;

    UNITY_BRANCH
    if (amount > 0.0h)
    {
        fog.colour = HB_FogColour(half3(normalize(positionWS - cameraPositionWS))) * amount;
        fog.clear = 1.0h - amount;
    }

    return fog;
}

half HB_TransparentClear(float3 positionWS)
{
    return 1.0h - HB_FogAmount(positionWS, GetCameraPositionWS());
}

#endif
