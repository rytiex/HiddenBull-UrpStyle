#ifndef HIDDENBULL_URPSTYLE_DECAL_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_DECAL_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#ifdef _DECAL_LAYERS
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
#endif

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _BumpScale;
    half _NormalBlend;
    half _Metallic;
    half _Smoothness;
    half _MAOSOpacity;
    half _AffectAlbedo;
    half _AffectNormal;
    half _AffectMAOS;
    half _Wear;
    half _BrushScale;
    half _StrokeAlpha;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    float _DrawOrder;
CBUFFER_END

UNITY_INSTANCING_BUFFER_START(Decal)
    UNITY_DEFINE_INSTANCED_PROP(half4x4, _NormalToWorld)
    UNITY_DEFINE_INSTANCED_PROP(float, _DecalLayerMaskFromDecal)
UNITY_INSTANCING_BUFFER_END(Decal)

#if defined(HB_DECAL_SCREEN_SPACE)
    #include "../Library/StyleLighting.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#endif

#include "../Library/StyleBrush.hlsl"

#define HB_DECAL_WEAR_BAND 0.2h

struct DecalAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DecalVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

DecalVaryings HiddenBullDecalVertex(DecalAttributes input)
{
    DecalVaryings output = (DecalVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    return output;
}

struct HiddenBullDecalSurface
{
    float3 positionWS;
    half3 geometryNormalWS;
    half3 normalWS;
    half3 albedo;
    half alpha;
    half normalAlpha;
    half surfaceAlpha;
    HiddenBullBrushSample brush;
};

HiddenBullDecalSurface HB_DecalSurface(DecalVaryings input)
{
    float2 pixel = input.positionCS.xy;

#if defined(HB_DECAL_SCREEN_SPACE)
    TransformScreenUV(pixel, _ScreenSize.y);
#endif

#ifdef _DECAL_LAYERS
    uint surfaceLayer = LoadSceneRenderingLayer(uint2(pixel));
    uint projectorLayer = asuint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
    clip((surfaceLayer & projectorLayer) - 0.1);
#endif

#if UNITY_REVERSED_Z
    float depth = LoadSceneDepth(uint2(pixel));
#else
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, LoadSceneDepth(uint2(pixel)));
#endif

    float3 positionWS = ComputeWorldSpacePosition(input.positionCS.xy * _ScreenSize.zw, depth, UNITY_MATRIX_I_VP);
    float3 positionDS = TransformWorldToObject(positionWS) * float3(1.0, -1.0, 1.0);

    clip(0.5 - Max3(abs(positionDS.x), abs(positionDS.y), abs(positionDS.z)));

    half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);

    float2 box = positionDS.xz + 0.5;
    float2 uv = box * float2(normalToWorld[3][0], normalToWorld[3][1]) + float2(normalToWorld[3][2], normalToWorld[3][3]);
    uv = TRANSFORM_TEX(uv, _BaseMap);

    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    half3 geometryNormal = half3(normalize(cross(ddy(positionWS), ddx(positionWS))));
    geometryNormal = dot(geometryNormal, viewDirectionWS) < 0.0h ? -geometryNormal : geometryNormal;

    half fade = saturate(normalToWorld[0][3]);
    half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);

    if (angleFade.y < 0.0h)
    {
        half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
        half facing = dot(geometryNormal, decalNormal);
        fade *= saturate(angleFade.x + angleFade.y * (facing * (facing - 2.0h)));
    }

    half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

    HiddenBullDecalSurface surface;
    surface.positionWS = positionWS;
    surface.geometryNormalWS = geometryNormal;
    surface.brush = HB_SampleBrush(positionWS, geometryNormal, 0.0h, float3(0.0, 0.0, 0.0),
                                   max(half(_BrushScale), HB_EPSILON), 1.0h);

    half coverage = surface.brush.coverage;
    half edge = half(min(min(box.x, 1.0 - box.x), min(box.y, 1.0 - box.y)));
    half keep = saturate(edge / max(half(_Wear) * HB_DECAL_WEAR_BAND, HB_EPSILON) + coverage * half(_Wear));

    half alpha = base.a * fade * smoothstep(0.35h, 0.65h, keep);
    alpha = saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);

    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);

    surface.normalWS = normalize(mul((half3x3)normalToWorld, normalTS));
    surface.albedo = base.rgb;
    surface.alpha = alpha * half(_AffectAlbedo);
    surface.normalAlpha = alpha * half(_NormalBlend) * half(_AffectNormal);
    surface.surfaceAlpha = alpha * half(_MAOSOpacity) * half(_AffectMAOS);

    return surface;
}

#if defined(HB_DECAL_SCREEN_SPACE)

half4 HiddenBullDecalScreenFragment(DecalVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HiddenBullDecalSurface surface = HB_DecalSurface(input);

    half3 normalWS = normalize(lerp(surface.geometryNormalWS, surface.normalWS,
                                    half(_NormalBlend) * half(_AffectNormal)));

    InputData inputData = (InputData)0;
    inputData.positionWS = surface.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(surface.positionWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.bakedGI = HB_SampleProbeGI(inputData.positionWS, normalWS, inputData.viewDirectionWS,
                                         input.positionCS.xy, inputData.shadowMask);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(surface.positionWS);
#endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = surface.albedo;
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Smoothness;
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedSoftness = 0.5h;

    half sunVisibility;
    half lightReach;

    half3 colour = HiddenBullFragmentLit(inputData, surfaceData, style, surface.brush, 0.0h, sunVisibility,
                                         lightReach).rgb;

    return half4(colour, surface.alpha);
}

#else

void HiddenBullDecalDBufferFragment(DecalVaryings input, OUTPUT_DBUFFER(outDBuffer))
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HiddenBullDecalSurface surface = HB_DecalSurface(input);

    DecalSurfaceData decal;
    ZERO_INITIALIZE(DecalSurfaceData, decal);

    decal.baseColor = half4(surface.albedo, surface.alpha);
    decal.normalWS = half4(surface.normalWS, surface.normalAlpha);
    decal.metallic = _Metallic;
    decal.occlusion = 1.0h;
    decal.smoothness = _Smoothness;
    decal.MAOSAlpha = surface.surfaceAlpha;

    ENCODE_INTO_DBUFFER(decal, outDBuffer);
}

#endif

#endif
