#ifndef HB_DECAL_PASS_INCLUDED
#define HB_DECAL_PASS_INCLUDED

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
    half _AffectNormal;
    half _AffectMAOS;
    half _Wear;
    half _BrushScale;
    half _StrokeAlpha;
    float _DrawOrder;
CBUFFER_END

UNITY_INSTANCING_BUFFER_START(Decal)
    UNITY_DEFINE_INSTANCED_PROP(half4x4, _NormalToWorld)
    UNITY_DEFINE_INSTANCED_PROP(float, _DecalLayerMaskFromDecal)
UNITY_INSTANCING_BUFFER_END(Decal)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"

#define HB_DECAL_WEAR_BAND 0.2h
#define HB_DECAL_KEEP_FROM 0.35h
#define HB_DECAL_KEEP_TO   0.65h

struct HBDecalAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBDecalVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

HBDecalVaryings HB_DecalVertex(HBDecalAttributes input)
{
    HBDecalVaryings output = (HBDecalVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    return output;
}

void HB_DecalFragment(HBDecalVaryings input, OUTPUT_DBUFFER(outDBuffer))
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 pixel = input.positionCS.xy;

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

    float3 positionWS = ComputeWorldSpacePosition(pixel * _ScreenSize.zw, depth, UNITY_MATRIX_I_VP);
    float3 positionDS = TransformWorldToObject(positionWS) * float3(1.0, -1.0, 1.0);

    clip(0.5 - Max3(abs(positionDS.x), abs(positionDS.y), abs(positionDS.z)));

    half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);

    float2 box = positionDS.xz + 0.5;
    float2 uv = box * float2(normalToWorld[3][0], normalToWorld[3][1])
              + float2(normalToWorld[3][2], normalToWorld[3][3]);

    uv = TRANSFORM_TEX(uv, _BaseMap);

    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    half3 geometryNormalWS = half3(normalize(cross(ddy(positionWS), ddx(positionWS))));

    geometryNormalWS = dot(geometryNormalWS, viewDirectionWS) < 0.0h ? -geometryNormalWS : geometryNormalWS;

    half fade = saturate(normalToWorld[0][3]);
    half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);

    if (angleFade.y < 0.0h)
    {
        half3 decalNormalWS = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
        half facing = dot(geometryNormalWS, decalNormalWS);

        fade *= saturate(angleFade.x + angleFade.y * (facing * (facing - 2.0h)));
    }

    half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
    half coverage = HB_SampleBrush(positionWS, geometryNormalWS, 0.0h, float3(0.0, 0.0, 0.0),
                                   max(half(_BrushScale), half(HB_EPSILON)), 1.0h).coverage;

    half edge = half(min(min(box.x, 1.0 - box.x), min(box.y, 1.0 - box.y)));
    half keep = saturate(edge / max(half(_Wear) * HB_DECAL_WEAR_BAND, half(HB_EPSILON)) + coverage * half(_Wear));

    half alpha = base.a * fade * smoothstep(HB_DECAL_KEEP_FROM, HB_DECAL_KEEP_TO, keep);

    alpha = saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);

    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);

    DecalSurfaceData decal;
    ZERO_INITIALIZE(DecalSurfaceData, decal);

    decal.baseColor = half4(base.rgb, alpha);
    decal.normalWS = half4(normalize(mul((half3x3)normalToWorld, normalTS)),
                           alpha * half(_NormalBlend) * half(_AffectNormal));
    decal.metallic = _Metallic;
    decal.occlusion = 1.0h;
    decal.smoothness = _Smoothness;
    decal.MAOSAlpha = alpha * half(_AffectMAOS);

    ENCODE_INTO_DBUFFER(decal, outDBuffer);
}

#endif
