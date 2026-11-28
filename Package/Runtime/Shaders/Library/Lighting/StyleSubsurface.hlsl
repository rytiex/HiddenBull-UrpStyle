#ifndef HB_STYLE_SUBSURFACE_INCLUDED
#define HB_STYLE_SUBSURFACE_INCLUDED

#ifdef _HB_SUBSURFACE

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Shadow/StyleShadow.hlsl"

#define HB_SKIN_THICKNESS_REACH 0.04
#define HB_CLOTH_WEAVE_TILT     0.35h

struct HBSubsurfaceData
{
    half3 colour;
    half width;
    half strength;
    half3 transmissionColour;
    half transmission;
    half bend;
    half focus;
    half3 sheenColour;
    half sheenStrength;
    half sheenPower;
    half translucency;
};

static HBSubsurfaceData hb_Subsurface;

half3 HB_ClothWeave(half3 normalWS, float3 positionWS, float2 uv, half strength, float scale)
{
    float2 weave = uv * scale;
    float2 wave = sin(weave * TWO_PI);

    float3 dpdx = ddx(positionWS);
    float3 dpdy = ddy(positionWS);
    float2 duvdx = ddx(weave);
    float2 duvdy = ddy(weave);

    float3 normal = float3(normalWS);
    float3 edgeY = cross(dpdy, normal);
    float3 edgeX = cross(normal, dpdx);

    float3 tangent = edgeY * duvdx.x + edgeX * duvdy.x;
    float3 bitangent = edgeY * duvdx.y + edgeX * duvdy.y;
    float reach = rsqrt(max(max(dot(tangent, tangent), dot(bitangent, bitangent)), 1e-12));

    float fade = saturate(1.0 - max(length(duvdx), length(duvdy)));

    return half3(normalize(normal + (tangent * wave.x + bitangent * wave.y)
                                    * (reach * strength * HB_CLOTH_WEAVE_TILT * fade)));
}

#if defined(HB_MAIN_LIGHT_SHADOW_MAP)
half HB_SubsurfaceThinTexels(float2 uv, float depth, float depthPerMetre)
{
    float2 texel = uv * _MainLightShadowmapSize.zw - 0.5;
    float2 corner = floor(texel);
    float2 weight = texel - corner;
    float4 entry = GATHER_RED_TEXTURE2D(_MainLightShadowmapTexture, sampler_PointClamp,
                                        (corner + 1.0) * _MainLightShadowmapSize.xy);

#if UNITY_REVERSED_Z
    float4 travelled = (entry - depth) / depthPerMetre;
#else
    float4 travelled = (depth - entry) / depthPerMetre;
#endif

    float4 thin = exp(-max(travelled, 0.0) / HB_SKIN_THICKNESS_REACH);
    float top = lerp(thin.w, thin.z, weight.x);
    float bottom = lerp(thin.x, thin.y, weight.x);

    return half(lerp(top, bottom, weight.y));
}
#endif

half HB_SubsurfaceThin(Light light, float3 positionWS)
{
#if defined(_HB_THICKNESS_MAP)
    return hb_Subsurface.translucency;
#elif defined(HB_MAIN_LIGHT_SHADOW_MAP)
    half thin = 1.0h;

    bool sun = all(abs(light.direction - half3(_MainLightPosition.xyz)) < 1e-3h);

    float4 coord = TransformWorldToShadowCoord(positionWS);

    UNITY_BRANCH
    if (sun && !BEYOND_SHADOW_FAR(coord) && _MainLightShadowmapSize.z >= 2.0)
    {
        float perMetre, depthPerMetre;
        HB_ShadowScale(HB_ShadowCascade(positionWS), perMetre, depthPerMetre);

        float2 spread = _MainLightShadowmapSize.xy * 0.5;

        thin = (HB_SubsurfaceThinTexels(coord.xy + float2(-spread.x, -spread.y), coord.z, depthPerMetre)
              + HB_SubsurfaceThinTexels(coord.xy + float2(spread.x, -spread.y), coord.z, depthPerMetre)
              + HB_SubsurfaceThinTexels(coord.xy + float2(-spread.x, spread.y), coord.z, depthPerMetre)
              + HB_SubsurfaceThinTexels(coord.xy + float2(spread.x, spread.y), coord.z, depthPerMetre)) * 0.25h;
    }

    return thin;
#else
    return 1.0h;
#endif
}

half HB_Transmission(Light light, half3 viewDirectionWS, half3 normalWS)
{
    half3 through = -normalize(light.direction + normalWS * hb_Subsurface.bend);

    return PositivePow(saturate(dot(viewDirectionWS, through)), max(hb_Subsurface.focus, 1.0h))
         * hb_Subsurface.transmission;
}

#define HB_HAS_EXTRA_LIGHT

half3 HB_ExtraLight(Light light, InputData inputData, SurfaceData surfaceData, half3 normalWS)
{
    half3 radiance = light.color * light.distanceAttenuation;
    half NdotL = dot(normalWS, light.direction);
    half transmission = HB_Transmission(light, inputData.viewDirectionWS, normalWS);

#ifdef _HB_CLOTH
    half grazing = 1.0h - saturate(dot(normalWS, inputData.viewDirectionWS));
    half sheen = PositivePow(grazing, max(hb_Subsurface.sheenPower, 1.0h)) * saturate(NdotL * 0.5h + 0.5h);

    return radiance * light.shadowAttenuation
         * (hb_Subsurface.sheenColour * (sheen * hb_Subsurface.sheenStrength)
            + hb_Subsurface.transmissionColour * transmission);
#else
    half width = max(hb_Subsurface.width, 0.01h);
    half wide = saturate((NdotL + width) / (1.0h + width));
    half band = saturate(wide - saturate(NdotL)) * hb_Subsurface.strength * light.shadowAttenuation;

    UNITY_BRANCH
    if (transmission > 0.0h)
        transmission *= HB_SubsurfaceThin(light, inputData.positionWS);

    return radiance * hb_Subsurface.colour * (surfaceData.albedo * band + transmission);
#endif
}

#endif

#endif
