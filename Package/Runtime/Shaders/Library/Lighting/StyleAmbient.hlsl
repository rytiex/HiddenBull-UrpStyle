#ifndef HB_STYLE_AMBIENT_INCLUDED
#define HB_STYLE_AMBIENT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSkyLut.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleTones.hlsl"

#define HB_AMBIENT_TIME       _HB_Environment.y
#define HB_AMBIENT_LIGHT_BIAS _HB_Environment.z
#define HB_AMBIENT_INTENSITY  _HB_Environment.w
#define HB_AMBIENT_FLOOR      _HB_AmbientFloor.rgb
#define HB_AMBIENT_PROBES     _HB_AmbientFloor.w

half3 HB_SampleAmbientLut(half up)
{
    return HB_SampleSkyLut(up, HB_AMBIENT_TIME, HB_LUT_AMBIENT) * half(HB_AMBIENT_INTENSITY);
}

half3 HB_GradientAmbient(half3 directionWS)
{
    return HB_SampleAmbientLut(directionWS.y);
}

half3 HB_AmbientSkyColor()
{
    return HB_SampleAmbientLut(1.0h);
}

half3 HB_ResolveAmbient(half3 normalWS, half3 bakedGI, half brush, half bakedTones, half softness,
                        out half bakedVisibility)
{
    half3 gradient = HB_SampleAmbientLut(normalWS.y + brush);

    half3 light = half3(_MainLightPosition.xyz);
    half bias = half(HB_AMBIENT_LIGHT_BIAS) * step(half(HB_EPSILON), max(_MainLightColor.r,
                                                                      max(_MainLightColor.g, _MainLightColor.b)));

    UNITY_BRANCH
    if (bias > 0.0h)
    {
        half3 alongLight = HB_SampleAmbientLut(dot(normalWS, light) + brush) - half3(HB_AMBIENT_FLOOR);

        gradient += alongLight * bias;
    }

    bakedVisibility = 1.0h;

#if !defined(LIGHTMAP_ON)
    if (HB_AMBIENT_PROBES < 0.5)
        return gradient;
#endif

    half ratio = Luminance(bakedGI) / max(Luminance(gradient), half(HB_EPSILON));

    bakedVisibility = saturate(ratio);

    half banded = HB_BakedToneBands(ratio, softness) / max(ratio, half(HB_EPSILON));

    return bakedGI * lerp(1.0h, banded, saturate(bakedTones));
}

#endif
