#ifndef HB_TRANSPARENT_PASS_INCLUDED
#define HB_TRANSPARENT_PASS_INCLUDED

#ifndef HB_TRANSPARENT
    #define HB_TRANSPARENT
#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"

static half4 hb_TransparentBase;

half HB_StrokeAlpha(half alpha, half coverage)
{
#ifdef _HB_BRUSH
    return saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);
#else
    return alpha;
#endif
}

void HB_TransparentSurface(inout SurfaceData surfaceData, HBBrushSample brush)
{
    surfaceData.alpha = HB_StrokeAlpha(surfaceData.alpha, brush.coverage);
    hb_TransparentBase = half4(surfaceData.albedo, surfaceData.alpha);

#ifdef _HB_TINTED
    surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
#endif
}

static half3 hb_TransparentNormal;
static float3 hb_TransparentStroke;

void HB_TransparentInput(InputData inputData, float3 strokeShift)
{
    hb_TransparentNormal = inputData.normalWS;
    hb_TransparentStroke = strokeShift;
}

#define HB_LIT_SURFACE_HOOK(surfaceData, brush) HB_TransparentSurface(surfaceData, brush)
#define HB_LIT_INPUT_HOOK(inputData, strokeShift) HB_TransparentInput(inputData, strokeShift)

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparentFog.hlsl"

void HB_TransparentMomentsFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC,
                                   out float outB0 : SV_Target0, out float4 outMoments : SV_Target1,
                                   out float4 outChroma : SV_Target2)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float depth = HB_OITDepth(input.positionWS);

    outB0 = 0.0;
    outMoments = float4(0.0, 0.0, 0.0, 0.0);
    outChroma = float4(0.0, 0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (HB_OITOutsideRound(uint2(input.positionCS.xy), depth))
    {
        discard;
        return;
    }

    HBSurfaceInput surface = HB_LitSurfaceInput(input, IS_FRONT_VFACE(frontFace, true, false));
    half4 base = HB_SampleSurfaceBase(surface);

#ifdef _HB_BRUSH
    UNITY_BRANCH
    if (_StrokeAlpha > 0.0)
        base.a = HB_StrokeAlpha(base.a, HB_ResolveBrush(surface).coverage);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

#ifdef _HB_TINTED
    half3 tint = HB_TintTransmission(base.rgb, base.a * HB_TransparentClear(input.positionWS));
    half transmission = HB_TintMean(tint);

    outChroma = HB_OITChroma(tint, depth);
#else
    half transmission = 1.0h - base.a;
#endif

    HB_OITMoments(HB_OITAbsorbance(transmission), depth, outB0, outMoments);
}

float HB_TransparentGlassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return HB_GlassNearness(HB_OITDepth(input.positionWS));
}

void HB_TransparentColorFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC,
                                 out half4 outAccumulated : SV_Target0, out half4 outTint : SV_Target1,
                                 out half4 outLight : SV_Target2, out float4 outRefraction : SV_Target3)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    uint2 pixel = uint2(input.positionCS.xy);
    float depth = HB_OITDepth(input.positionWS);

    outAccumulated = half4(0.0h, 0.0h, 0.0h, 0.0h);
    outTint = half4(1.0h, 1.0h, 1.0h, 1.0h);
    outLight = half4(0.0h, 0.0h, 0.0h, 0.0h);
    outRefraction = float4(0.0, 0.0, 0.0, 0.0);

    UNITY_BRANCH
    if (HB_OITOutsideRound(pixel, depth))
    {
        discard;
        return;
    }

    outTint.a = 0.0h;

    half4 colour = HB_LitColour(input, IS_FRONT_VFACE(frontFace, true, false));
    HBTransparentFog fog = HB_TransparentFog(input.positionWS);

#if defined(_HB_ADDITIVE)
    float absorbance = 0.0;
#elif defined(_HB_TINTED)
    float absorbance = HB_OITAbsorbance(HB_TintMean(HB_TintTransmission(hb_TransparentBase.rgb,
                                                                        hb_TransparentBase.a * fog.clear)));
#else
    float absorbance = HB_OITAbsorbance(1.0h - colour.a);
#endif

    half transmittance = half(HB_OITTransmittanceAt(pixel, depth, absorbance));

    UNITY_BRANCH
    if (HB_OIT_REFRACTIVE && _BlendMode > 3.5)
    {
        half3 viewWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
        float2 offset = HB_RefractionOffset(input.positionWS, hb_TransparentNormal, viewWS, half(_Refraction),
                                            hb_TransparentStroke, half(_StrokeBend));
        float nearest = HB_OITBehindGlass(pixel, depth) ? 0.0 : 1.0;

        outRefraction = float4(offset, float2(_RefractionBlur, _Dispersion) * nearest) * (transmittance * fog.clear);
    }

#if defined(_HB_ADDITIVE)
    outLight.rgb = colour.rgb * (colour.a * fog.clear * transmittance);
#elif defined(_HB_TINTED)
    half3 tint = HB_TintTransmission(hb_TransparentBase.rgb, hb_TransparentBase.a * fog.clear);

    outTint.rgb = tint / max(HB_TintMean(tint), half(HB_OIT_MIN_TRANSMISSION));
    outLight.rgb = colour.rgb * (fog.clear * transmittance);
#elif defined(_HB_PREMULTIPLY)
    half3 lit = colour.rgb * fog.clear + fog.colour * colour.a;

    outAccumulated = half4(lit * transmittance, colour.a * transmittance);
#else
    half3 lit = colour.rgb * fog.clear + fog.colour;

    outAccumulated = half4(lit * (colour.a * transmittance), colour.a * transmittance);
#endif

    half3 chroma = HB_OITChromaAt(pixel, depth);

    outAccumulated.rgb *= chroma;
    outLight.rgb *= chroma;
}

#endif
