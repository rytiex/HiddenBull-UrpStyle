#ifndef HB_STYLE_SHADOW_CASTER_INCLUDED
#define HB_STYLE_SHADOW_CASTER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

float4 HB_ShadowCasterPosition(float3 positionWS, float3 biasNormalWS)
{
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, biasNormalWS, lightDirectionWS)));
}

#endif
