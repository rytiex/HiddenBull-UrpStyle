#ifndef HB_LIT_SUBSURFACE_INCLUDED
#define HB_LIT_SUBSURFACE_INCLUDED

#ifdef _HB_SUBSURFACE

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Shadow/StyleShadow.hlsl"

TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

#define HB_SKIN_THICKNESS_REACH 0.04
#define HB_CLOTH_WEAVE_TILT     0.35h

static half hb_Translucency = 1.0h;

void HB_SubsurfaceSurface(float2 uv, HBSurfaceInput surface, inout InputData inputData)
{
#ifdef _HB_THICKNESS_MAP
    hb_Translucency = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, uv).r;
#endif

#ifdef _HB_CLOTH
    UNITY_BRANCH
    if (_Weave > 0.0)
    {
    #ifdef _HB_TRIPLANAR
        HBTriplanar triplanar = HB_SurfaceTriplanar(surface, HB_FacingNormal(surface));

        float3 weave = triplanar.position * _WeaveScale;
        float3 wave = sin(weave * TWO_PI);
        half3 blend = triplanar.blend;

        float3 tilt = float3(0.0, wave.y, wave.z) * blend.x
                    + float3(wave.x, 0.0, wave.z) * blend.y
                    + float3(wave.x, wave.y, 0.0) * blend.z;

        float fade = saturate(1.0 - max(length(triplanar.positionDx), length(triplanar.positionDy)) * _WeaveScale);

        if (triplanar.localSpace > 0.5h)
            tilt = TransformObjectToWorldDir(tilt, false);

        float strength = _Weave * HB_CLOTH_WEAVE_TILT * fade;

        inputData.normalWS = half3(normalize(float3(inputData.normalWS) + tilt * strength));
    #else
        float2 weave = uv * _WeaveScale;
        float2 wave = sin(weave * TWO_PI);

        float3 dpdx = ddx(inputData.positionWS);
        float3 dpdy = ddy(inputData.positionWS);
        float2 duvdx = ddx(weave);
        float2 duvdy = ddy(weave);

        float3 normal = float3(inputData.normalWS);
        float3 edgeY = cross(dpdy, normal);
        float3 edgeX = cross(normal, dpdx);

        float3 tangent = edgeY * duvdx.x + edgeX * duvdy.x;
        float3 bitangent = edgeY * duvdx.y + edgeX * duvdy.y;
        float scale = rsqrt(max(max(dot(tangent, tangent), dot(bitangent, bitangent)), 1e-12));

        float fade = saturate(1.0 - max(length(duvdx), length(duvdy)));

        inputData.normalWS = half3(normalize(normal + (tangent * wave.x + bitangent * wave.y)
                                             * (scale * _Weave * HB_CLOTH_WEAVE_TILT * fade)));
    #endif
    }
#endif
}

half HB_SubsurfaceThin(Light light, float3 positionWS)
{
#if defined(_HB_THICKNESS_MAP)
    return hb_Translucency;
#elif defined(HB_MAIN_LIGHT_SHADOW_MAP)
    half thin = 1.0h;

    bool sun = all(abs(light.direction - half3(_MainLightPosition.xyz)) < 1e-3h);

    float4 coord = TransformWorldToShadowCoord(positionWS);

    UNITY_BRANCH
    if (sun && !BEYOND_SHADOW_FAR(coord) && _MainLightShadowmapSize.z >= 2.0)
    {
        float perMetre, depthPerMetre;
        HB_ShadowScale(positionWS, perMetre, depthPerMetre);

        float entry = SAMPLE_TEXTURE2D_LOD(_MainLightShadowmapTexture, sampler_LinearClamp, coord.xy, 0).r;

    #if UNITY_REVERSED_Z
        float travelled = (entry - coord.z) / depthPerMetre;
    #else
        float travelled = (coord.z - entry) / depthPerMetre;
    #endif

        thin = half(exp(-max(travelled, 0.0) / HB_SKIN_THICKNESS_REACH));
    }

    return thin;
#else
    return 1.0h;
#endif
}

half HB_Transmission(Light light, half3 viewDirectionWS, half3 normalWS)
{
    half3 through = -normalize(light.direction + normalWS * half(_TransmissionBend));

    return PositivePow(saturate(dot(viewDirectionWS, through)), max(half(_TransmissionFocus), 1.0h))
         * half(_Transmission);
}

#define HB_HAS_EXTRA_LIGHT

half3 HB_ExtraLight(Light light, InputData inputData, SurfaceData surfaceData, half3 normalWS)
{
    half3 radiance = light.color * light.distanceAttenuation;
    half NdotL = dot(normalWS, light.direction);
    half transmission = HB_Transmission(light, inputData.viewDirectionWS, normalWS);

#ifdef _HB_CLOTH
    half grazing = 1.0h - saturate(dot(normalWS, inputData.viewDirectionWS));
    half sheen = PositivePow(grazing, max(half(_SheenPower), 1.0h)) * saturate(NdotL * 0.5h + 0.5h);

    return radiance * light.shadowAttenuation
         * (_SheenColor.rgb * (sheen * half(_SheenStrength)) + _TransmissionColor.rgb * transmission);
#else
    half width = max(half(_SubsurfaceWidth), 0.01h);
    half wide = saturate((NdotL + width) / (1.0h + width));
    half band = saturate(wide - saturate(NdotL)) * half(_SubsurfaceStrength) * light.shadowAttenuation;

    UNITY_BRANCH
    if (transmission > 0.0h)
        transmission *= HB_SubsurfaceThin(light, inputData.positionWS);

    return radiance * _SubsurfaceColor.rgb * (surfaceData.albedo * band + transmission);
#endif
}

#endif

#endif
