#ifndef HIDDENBULL_URPSTYLE_LIT_TRANSPARENCY_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_LIT_TRANSPARENCY_PASS_INCLUDED

#ifdef HB_OIT_MOMENTS

#define HB_WEATHER_SNOW_ONLY
#include "LitSurfaceVertex.hlsl"
#include "../Library/StyleFog.hlsl"
#include "../Library/StyleTransparency.hlsl"

void HiddenBullOITMomentsFragment(HiddenBullSurfaceVaryings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC,
                                  out float2 outB0 : SV_Target0, out float4 outFront : SV_Target1,
                                  out float4 outBehind : SV_Target2)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HiddenBullSurfaceInput surface = HB_SurfaceFromVaryings(input, IS_FRONT_VFACE(frontFace, true, false));

    half coverage = 0.0h;

#ifdef _HB_BRUSH
    UNITY_BRANCH
    if (_StrokeAlpha > 0.0 || (_ReceiveWeather > 0.0 && HB_SURFACE_SNOW > 0.0))
        coverage = HB_ResolveBrush(surface).coverage;
#endif

    half alpha = HB_ResolveAlpha(surface, coverage);
    HiddenBullWeather weather = HB_ResolveSurfaceWeather(surface, coverage);

#ifdef HB_GLASS
    alpha = weather.snow;
#else
    alpha = max(alpha, weather.snow);
#endif

    float depth = HB_OITDepth(surface.positionWS);
    float b0;
    float4 moments;

    HB_OITMoments(HB_OITAbsorbance(alpha), depth, b0, moments);

    bool behind = HB_OITBehindRefraction(uint2(input.positionCS.xy), depth);

    outB0 = behind ? float2(0.0, b0) : float2(b0, 0.0);
    outFront = behind ? float4(0.0, 0.0, 0.0, 0.0) : moments;
    outBehind = behind ? moments : float4(0.0, 0.0, 0.0, 0.0);
}

half4 HiddenBullOITRefractionFragment(HiddenBullSurfaceVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifndef _HB_REFRACTION
    clip(-1.0);
#endif

    HiddenBullSurfaceInput surface = HB_SurfaceFromVaryings(input, true);

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(surface, 0.0h);
#endif

    float depth = HB_OITDepth(surface.positionWS);
    half clear = 1.0h - HB_FogAmountAt(surface.positionWS, GetNormalizedScreenSpaceUV(input.positionCS));

    return half4(0.5 - depth * 0.5, _RefractionBlur * clear, _Dispersion * clear, 0.0h);
}

#else

#include "LitForwardPass.hlsl"
#include "../Library/StyleTransparency.hlsl"

void HiddenBullOITColorFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC,
                                out half4 outFront : SV_Target0, out half4 outBehind : SV_Target1,
                                out float4 outOffset : SV_Target2, out half4 outTint : SV_Target3,
                                out half4 outBehindTint : SV_Target4)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    InputData inputData;
    half sunVisibility;
    float3 strokeShift;
    half snow;

    bool front = IS_FRONT_VFACE(frontFace, true, false);

    half4 color = HB_LitColour(input, front, inputData, sunVisibility, strokeShift, snow);

#ifdef HB_GLASS
    half fogAmount = HB_FogAmountAt(inputData.positionWS, inputData.normalizedScreenSpaceUV);
    half clear = 1.0h - fogAmount;
    half3 snowColour = color.rgb;

    UNITY_BRANCH
    if (snow > 0.0h)
        snowColour = HB_FogSurface(color.rgb,
                                   HB_ResolveFog(inputData.positionWS, inputData.normalizedScreenSpaceUV, false));

    color.rgb = lerp(HB_FogLight(color.rgb, fogAmount), snowColour, snow);
#elif defined(_HB_ADDITIVE)
    color.rgb = HB_FogLight(color.rgb * color.a,
                            HB_FogAmountAt(inputData.positionWS, inputData.normalizedScreenSpaceUV));
#else
    color.a = max(color.a, snow);

    HB_FogResult fog = HB_ResolveFog(inputData.positionWS, inputData.normalizedScreenSpaceUV, false);

#ifdef _HB_PREMULTIPLY
    fog.colour *= color.a;
#endif

    color.rgb = HB_FogSurface(color.rgb, fog);
#endif

    uint2 pixel = uint2(input.positionCS.xy);
    float depth = HB_OITDepth(inputData.positionWS);

    bool behind = HB_OITBehindRefraction(pixel, depth);

    half transmittance = half(behind ? HB_OITBehindTransmittanceAt(pixel, depth)
                                     : HB_OITFrontTransmittanceAt(pixel, depth));

#if defined(HB_GLASS)
    half4 contribution = half4(color.rgb * transmittance, snow * transmittance);
#elif defined(_HB_ADDITIVE)
    half4 contribution = half4(color.rgb * transmittance, 0.0h);
#elif defined(_HB_PREMULTIPLY)
    half4 contribution = half4(color.rgb * transmittance, transmittance * color.a);
#else
    half4 contribution = half4(color.rgb * (transmittance * color.a), transmittance * color.a);
#endif

    outFront = behind ? half4(0.0h, 0.0h, 0.0h, 0.0h) : contribution;
    outBehind = behind ? contribution : half4(0.0h, 0.0h, 0.0h, 0.0h);

    half4 white = half4(1.0h, 1.0h, 1.0h, 1.0h);

#ifdef HB_GLASS
    half3 outward = front ? inputData.normalWS : -inputData.normalWS;

    float2 bend = HB_RefractionOffset(inputData.positionWS, outward, inputData.viewDirectionWS,
                                      _Refraction, strokeShift, _StrokeBend)
                 * (transmittance * clear * (1.0h - snow));
    half3 surfaceTint = HB_ResolveTint(HB_LitSurfaceInput(input, front), color.a);
    half4 tint = half4(lerp(white.rgb, surfaceTint, clear * (1.0h - snow)), 1.0h);

    outOffset = behind ? float4(0.0, 0.0, bend) : float4(bend, 0.0, 0.0);
    outTint = behind ? white : tint;
    outBehindTint = behind ? tint : white;
#else
    outOffset = float4(0.0, 0.0, 0.0, 0.0);
    outTint = white;
    outBehindTint = white;
#endif
}

#endif

#endif
