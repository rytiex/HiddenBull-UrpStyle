#ifndef HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED
#define HIDDENBULL_URPSTYLE_AMBIENT_INCLUDED

#include "StyleCommon.hlsl"

float4 _HB_AmbientSky;
float4 _HB_AmbientHorizon;
float4 _HB_AmbientGround;
float4 _HB_AmbientParams;

#define HB_AMBIENT_SKY_FALLOFF   _HB_AmbientParams.x
#define HB_AMBIENT_GROUND_FALLOFF _HB_AmbientParams.y
#define HB_AMBIENT_INTENSITY     _HB_AmbientParams.z
#define HB_AMBIENT_BAKED_WEIGHT  _HB_AmbientParams.w

half3 HB_GradientAmbient(half3 directionWS)
{
    half t = directionWS.y;

    half up = HB_Smooth01(saturate(HB_SafeDiv(t, HB_AMBIENT_SKY_FALLOFF)));
    half down = HB_Smooth01(saturate(HB_SafeDiv(-t, HB_AMBIENT_GROUND_FALLOFF)));

    half3 color = _HB_AmbientHorizon.rgb;
    color += (_HB_AmbientSky.rgb - _HB_AmbientHorizon.rgb) * up;
    color += (_HB_AmbientGround.rgb - _HB_AmbientHorizon.rgb) * down;

    return color * HB_AMBIENT_INTENSITY;
}

half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI)
{
    return lerp(HB_GradientAmbient(normalWS), bakedGI, HB_AMBIENT_BAKED_WEIGHT);
}

half3 HB_AmbientSkyColor()
{
    return _HB_AmbientSky.rgb * HB_AMBIENT_INTENSITY;
}

#endif
