#ifndef HIDDENBULL_URPSTYLE_FOLIAGE_HOOKS_INCLUDED
#define HIDDENBULL_URPSTYLE_FOLIAGE_HOOKS_INCLUDED

#define HB_VERTEX_COLOR
#define HB_ANCHOR_REST
#define HB_TWO_SIDED_WEATHER

#define HB_MATERIAL_EXTRA \
    half4 _TranslucencyColor; \
    half _WindStrength; \
    half _WindHeight; \
    half _WindBend; \
    half _WindFlutter; \
    half _WindSource;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "../Lit/LitInput.hlsl"

#define HB_WIND_SWAY_SPEED    1.3
#define HB_WIND_GUST_SPEED    0.37
#define HB_WIND_GUST_SCALE    0.045
#define HB_WIND_FLUTTER_SPEED 7.0
#define HB_WIND_FLUTTER_REACH 0.06
#define HB_WIND_REACH         0.35
#define HB_WIND_DROP          0.3
#define HB_TRANSLUCENT_BASE    0.4h
#define HB_TRANSLUCENT_HIGH    0.55h
#define HB_TRANSLUCENT_LOW     0.2h
#define HB_TRANSLUCENT_SHADOW  0.35h
#define HB_TRANSLUCENT_ALBEDO  0.5h
#define HB_CANOPY_CENTRE       0.6

#ifndef HB_SKY_PASS_FLAG
#define HB_SKY_PASS_FLAG
float _HB_SkyOcclusionPass;
#endif

#define HB_HAS_VERTEX_HOOK

void HB_VertexHook(inout float3 positionOS, half4 color)
{
    float strength = HB_WIND_STRENGTH * _WindStrength;

    if (strength <= 0.0 || _HB_SkyOcclusionPass > 0.5)
        return;

    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 origin = GetObjectToWorldMatrix()._m03_m13_m23;

    float height = saturate((positionWS.y - origin.y) / max(float(_WindHeight), 0.01));
    float flutterMask = _WindSource > 0.5 ? float(color.r) : height;

    float time = _Time.y;
    float phase = dot(origin.xz, float2(0.37, 0.23));

    float gust = 0.5 + 0.5 * sin(time * HB_WIND_GUST_SPEED + (positionWS.x + positionWS.z) * HB_WIND_GUST_SCALE);
    float sway = 0.6 + 0.4 * sin(time * HB_WIND_SWAY_SPEED + phase);

    float bend = height * height * _WindBend * strength * sway * (0.6 + 0.4 * gust);
    float2 direction = HB_WIND_DIRECTION;

    float3 offset = float3(direction.x, 0.0, direction.y) * (bend * HB_WIND_REACH * _WindHeight);
    offset.y -= length(offset.xz) * height * HB_WIND_DROP;

    float flutter = sin(time * HB_WIND_FLUTTER_SPEED + dot(positionWS, float3(3.1, 2.3, 4.7)) + phase)
                  * flutterMask * _WindFlutter * strength * HB_WIND_FLUTTER_REACH;

    offset += float3(direction.y, 0.6, -direction.x) * flutter;

    positionOS = TransformWorldToObject(positionWS + offset);
}

#define HB_HAS_NORMAL_HOOK

half3 HB_NormalHook(half3 normalWS, float3 restOS)
{
    float3 low = unity_RendererBounds_Min.xyz;
    float3 high = unity_RendererBounds_Max.xyz;
    float3 crown = float3(0.5 * (low.x + high.x), lerp(low.y, high.y, HB_CANOPY_CENTRE), 0.5 * (low.z + high.z));
    float3 centre = TransformWorldToObject(crown);

    return NormalizeNormalPerPixel(half3(TransformObjectToWorldNormal(SafeNormalize(restOS - centre))));
}

#define HB_HAS_EXTRA_LIGHT

half3 HB_ExtraLight(Light light, InputData inputData, SurfaceData surfaceData, half3 normalWS)
{
    half facing = saturate(dot(-inputData.viewDirectionWS, light.direction) * 0.5h + 0.5h);
    half facingAway = saturate(-dot(normalWS, light.direction) * 0.5h + 0.5h);
    half lowSun = saturate((HB_TRANSLUCENT_HIGH - light.direction.y) / (HB_TRANSLUCENT_HIGH - HB_TRANSLUCENT_LOW));
    half level = 1.0h - saturate(inputData.viewDirectionWS.y);
    half through = facingAway * lerp(HB_TRANSLUCENT_BASE, 1.0h, facing * facing) * lowSun * level;
    half shadow = lerp(HB_TRANSLUCENT_SHADOW, 1.0h, light.shadowAttenuation);
    half3 tint = _TranslucencyColor.rgb * lerp(half3(1.0h, 1.0h, 1.0h), surfaceData.albedo, HB_TRANSLUCENT_ALBEDO);

    return light.color * light.distanceAttenuation * shadow * tint * through;
}

#endif
