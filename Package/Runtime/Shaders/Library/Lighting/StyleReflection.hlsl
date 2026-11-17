#ifndef HB_STYLE_REFLECTION_INCLUDED
#define HB_STYLE_REFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleEnvironmentSky.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleScreenReflection.hlsl"

#define HB_SPECULAR_SHEEN       0.15h
#define HB_SPECULAR_CORE        0.5h
#define HB_SPECULAR_SHEEN_LEVEL 0.3h
#define HB_SPECULAR_SHEEN_MOST  1.5h
#define HB_SPECULAR_BRUSH       1.0h
#define HB_SUN_GLOSS            half(1.0 - _HB_CloudLook.w)

half3 HB_ProbeReflection(half3 reflectVector, float3 positionWS, half mip, float2 screenUV, out half weight)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    float total = 0.0;

#if USE_CLUSTER_LIGHT_LOOP && CLUSTER_HAS_REFLECTION_PROBES
    uint probeIndex;
    ClusterIterator it = ClusterInit(screenUV, positionWS, 1);

    [loop] while (ClusterNext(it, probeIndex) && total < 0.99)
    {
        probeIndex -= URP_FP_PROBES_BEGIN;

        float probeWeight = min(CalculateProbeWeight(positionWS, urp_ReflProbes_BoxMin[probeIndex],
                                                     urp_ReflProbes_BoxMax[probeIndex]), 1.0 - total);

        half3 direction = reflectVector;

        if (_REFLECTION_PROBE_BOX_PROJECTION)
        {
            direction = BoxProjectedCubemapDirection(reflectVector, positionWS,
                                                     urp_ReflProbes_ProbePosition[probeIndex],
                                                     urp_ReflProbes_BoxMin[probeIndex],
                                                     urp_ReflProbes_BoxMax[probeIndex]);
        }

        half probeMip = min(mip, half(abs(urp_ReflProbes_ProbePosition[probeIndex].w) - 1.0));
        float2 uv = saturate(PackNormalOctQuadEncode(direction) * 0.5 + 0.5);

        float mip0 = floor(probeMip);
        float4 scaleOffset0 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0];
        float4 scaleOffset1 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0 + 1];

        half3 sample0 = SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp,
                                             uv * scaleOffset0.xy + scaleOffset0.zw, 0.0).rgb;
        half3 sample1 = SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp,
                                             uv * scaleOffset1.xy + scaleOffset1.zw, 0.0).rgb;

        irradiance += probeWeight * lerp(sample0, sample1, probeMip - mip0);
        total += probeWeight;
    }
#elif !USE_CLUSTER_LIGHT_LOOP
    if (_REFLECTION_PROBE_BLENDING)
    {
        half volume0 = CalculateProbeVolumeSqrMagnitude(unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
        half volume1 = CalculateProbeVolumeSqrMagnitude(unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
        half volumeDiff = volume0 - volume1;
        float importance = unity_SpecCube1_BoxMin.w;

        bool dominant0 = importance > 0.0 || (importance == 0.0 && volumeDiff < -0.0001h);
        bool dominant1 = importance < 0.0 || (importance == 0.0 && volumeDiff > 0.0001h);

        float desired0 = CalculateProbeWeight(positionWS, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
        float desired1 = CalculateProbeWeight(positionWS, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

        float weight0 = dominant1 ? min(desired0, 1.0 - desired1) : desired0;
        float weight1 = dominant0 ? min(desired1, 1.0 - desired0) : desired1;

        total = weight0 + weight1;
        weight0 /= max(total, 1.0);
        weight1 /= max(total, 1.0);
        total = min(total, 1.0);

        if (weight0 > 0.01)
        {
            half3 direction = _REFLECTION_PROBE_BOX_PROJECTION
                ? BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition,
                                               unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax)
                : reflectVector;

            irradiance += weight0 * DecodeHDREnvironment(
                SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, mip), unity_SpecCube0_HDR);
        }

        if (weight1 > 0.01)
        {
            half3 direction = _REFLECTION_PROBE_BOX_PROJECTION
                ? BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube1_ProbePosition,
                                               unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax)
                : reflectVector;

            irradiance += weight1 * DecodeHDREnvironment(
                SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, direction, mip), unity_SpecCube1_HDR);
        }
    }
#endif

    weight = half(total);

    return irradiance;
}

half3 HB_EnvironmentReflection(half3 reflectVector, half3 normalWS, float3 positionWS, half perceptualRoughness,
                               float2 screenUV, half skyVisibility)
{
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half weight;

    half3 probes = HB_ProbeReflection(reflectVector, positionWS, mip, screenUV, weight);
    half3 sky = HB_EnvironmentSky(reflectVector, normalWS, perceptualRoughness) * skyVisibility;

    return probes + sky * (1.0h - weight);
}

half3 HB_DirectSpecular(BRDFData brdfData, half3 normalWS, half3 lightDirection, half3 viewDirection, half softness,
                        half offset)
{
    half term = DirectBRDFSpecular(brdfData, normalWS, lightDirection, viewDirection);
    half peak = rcp(max(brdfData.roughness2 * brdfData.normalizationTerm, half(HB_EPSILON)));

    half level = saturate(term / peak);
    half shaped = level * (1.0h + offset * HB_SPECULAR_BRUSH);
    half width = max(softness * 0.1h, 0.01h);

    half sheen = smoothstep(HB_SPECULAR_SHEEN - width, HB_SPECULAR_SHEEN + width, shaped);
    half core = smoothstep(HB_SPECULAR_CORE - width, HB_SPECULAR_CORE + width, shaped);

    half haze = min(HB_SPECULAR_SHEEN_LEVEL, HB_SPECULAR_SHEEN_MOST / peak);
    half banded = lerp(min(level, haze), lerp(haze, 1.0h, core), sheen);

    return brdfData.specular * (peak * banded);
}

#endif
