#ifndef HB_TRANSPARENT_PASS_INCLUDED
#define HB_TRANSPARENT_PASS_INCLUDED

#ifndef HB_TRANSPARENT
    #define HB_TRANSPARENT
#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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

half4 HB_TransparentTintFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float eye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());

    UNITY_BRANCH
    if (HB_TransparentOutsideStage(uint2(input.positionCS.xy), eye) || (HB_TRANSPARENT_FRONT && _BlendMode > 3.5))
    {
        discard;
        return half4(1.0h, 1.0h, 1.0h, 1.0h);
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

    return half4(HB_TintTransmission(base.rgb, base.a * HB_TransparentClear(input.positionWS)), 1.0h);
}

float2 HB_TransparentNearestFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

#ifdef HB_REFRACTIVE_NEAREST
    UNITY_BRANCH
    if (_BlendMode < 3.5)
    {
        discard;
        return float2(0.0, 0.0);
    }
#endif

    float nearest = HB_NearestValue(LinearEyeDepth(input.positionWS, GetWorldToViewMatrix()));

    return float2(nearest, nearest);
}

half4 HB_TransparentFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    uint2 pixel = uint2(input.positionCS.xy);
    float eye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());

    UNITY_BRANCH
    if (HB_TransparentOutsideStage(pixel, eye))
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    half4 colour = HB_LitColour(input, IS_FRONT_VFACE(frontFace, true, false));
    HBTransparentFog fog = HB_TransparentFog(input.positionWS);

#ifdef _HB_TINTED
    UNITY_BRANCH
    if (HB_TRANSPARENT_FRONT && _BlendMode > 3.5)
    {
        half3 viewWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
        float2 uv = (float2(pixel) + 0.5) * _ScreenSize.zw;
        float2 offset = HB_RefractionOffset(input.positionWS, hb_TransparentNormal, viewWS, half(_Refraction),
                                            hb_TransparentStroke, half(_StrokeBend));
        float scene = LinearEyeDepth(SampleSceneDepth(saturate(uv + offset)), _ZBufferParams);

        offset = scene < eye ? float2(0.0, 0.0) : offset;

        half3 tint = HB_TintTransmission(hb_TransparentBase.rgb, hb_TransparentBase.a * fog.clear);
        half3 behind = HB_Refracted(uv, offset, half(_RefractionBlur), half(_Dispersion));

        return half4(behind * tint + colour.rgb * fog.clear, 1.0h);
    }
#endif

#if defined(_HB_ADDITIVE)
    return half4(colour.rgb * (colour.a * fog.clear), 0.0h);
#elif defined(_HB_TINTED)
    return half4(colour.rgb * fog.clear, 0.0h);
#elif defined(_HB_PREMULTIPLY)
    return half4(colour.rgb * fog.clear + fog.colour * colour.a, colour.a);
#else
    return half4((colour.rgb * fog.clear + fog.colour) * colour.a, colour.a);
#endif
}

#endif
