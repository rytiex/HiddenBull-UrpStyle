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
    HBAtmosphere atmosphere = HB_AtmosphereAmount(positionWS, cameraPositionWS);

    HBTransparentFog fog;
    fog.colour = half3(0.0h, 0.0h, 0.0h);
    fog.clear = 1.0h;

    UNITY_BRANCH
    if (atmosphere.fog + atmosphere.horizon > 0.0h)
    {
        half3 direction = half3(normalize(positionWS - cameraPositionWS));

        fog.colour = HB_SkySample(direction, direction.y) * (atmosphere.horizon * (1.0h - atmosphere.fog))
                   + HB_FogColour(direction) * atmosphere.fog;
        fog.clear = (1.0h - atmosphere.horizon) * (1.0h - atmosphere.fog);
    }

    return fog;
}

half HB_TransparentClear(float3 positionWS)
{
    HBAtmosphere atmosphere = HB_AtmosphereAmount(positionWS, GetCameraPositionWS());

    return (1.0h - atmosphere.horizon) * (1.0h - atmosphere.fog);
}

#endif
