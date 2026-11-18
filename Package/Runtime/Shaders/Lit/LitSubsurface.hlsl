#ifndef HB_LIT_SUBSURFACE_INCLUDED
#define HB_LIT_SUBSURFACE_INCLUDED

#ifdef _HB_SUBSURFACE

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleSubsurface.hlsl"

TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

void HB_SubsurfaceSurface(float2 uv, HBSurfaceInput surface, inout InputData inputData)
{
    hb_Subsurface.colour = _SubsurfaceColor.rgb;
    hb_Subsurface.width = _SubsurfaceWidth;
    hb_Subsurface.strength = _SubsurfaceStrength;
    hb_Subsurface.transmissionColour = _TransmissionColor.rgb;
    hb_Subsurface.transmission = _Transmission;
    hb_Subsurface.bend = _TransmissionBend;
    hb_Subsurface.focus = _TransmissionFocus;
    hb_Subsurface.sheenColour = _SheenColor.rgb;
    hb_Subsurface.sheenStrength = _SheenStrength;
    hb_Subsurface.sheenPower = _SheenPower;
    hb_Subsurface.translucency = 1.0h;

#ifdef _HB_THICKNESS_MAP
    hb_Subsurface.translucency = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, uv).r;
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
        inputData.normalWS = HB_ClothWeave(inputData.normalWS, inputData.positionWS, uv, _Weave, _WeaveScale);
    #endif
    }
#endif
}

#endif

#endif
