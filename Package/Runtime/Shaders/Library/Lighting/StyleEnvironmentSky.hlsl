#ifndef HB_STYLE_ENVIRONMENT_SKY_INCLUDED
#define HB_STYLE_ENVIRONMENT_SKY_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Sky/StyleSky.hlsl"

#define HB_REFLECTION_HORIZON 0.04h

TEXTURE2D(_HB_SkyMap);
float4 _HB_SkyMapSet;

half3 HB_SkyMapSample(half3 direction, half perceptualRoughness)
{
    float2 uv = PackNormalOctQuadEncode(normalize(float3(direction.xzy))) * 0.5 + 0.5;

    return SAMPLE_TEXTURE2D_LOD(_HB_SkyMap, sampler_LinearClamp, uv,
                                PerceptualRoughnessToMipmapLevel(perceptualRoughness)).rgb;
}

half3 HB_EnvironmentSky(half3 reflectVector, half3 normalWS, half perceptualRoughness)
{
    half roughness = perceptualRoughness * perceptualRoughness;
    half3 skyDirection = normalize(lerp(reflectVector, normalWS, roughness));

    half horizon = smoothstep(-HB_REFLECTION_HORIZON, HB_REFLECTION_HORIZON, skyDirection.y);
    half3 sky;

    UNITY_BRANCH
    if (_HB_SkyMapSet.x > 0.5)
        sky = HB_SkyMapSample(skyDirection, perceptualRoughness);
    else
        sky = HB_SkySample(skyDirection, max(skyDirection.y, 0.0h));

    return lerp(HB_GradientAmbient(skyDirection), sky, horizon);
}

#endif
