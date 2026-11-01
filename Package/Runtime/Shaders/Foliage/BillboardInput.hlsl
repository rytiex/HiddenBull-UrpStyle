#ifndef HB_BILLBOARD_INPUT_INCLUDED
#define HB_BILLBOARD_INPUT_INCLUDED

#define HB_PLANT_BODY

#define HB_SNOW_NO_SLOPE   -0.2h
#define HB_SNOW_FULL_SLOPE 0.0h
#define HB_SNOW_EDGE_DELAY 0.0h

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Weather/StyleWind.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageInstancing.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageSway.hlsl"

#ifdef HB_SHADOW_CASTER
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Shadow/StyleShadowCaster.hlsl"
#endif

CBUFFER_START(UnityPerMaterial)
    float4 _BillboardCenter;
    float _BillboardSize;
    float _BillboardTop;
    float _BillboardRadius;
    float _BillboardSway;
    float _Frames;
    half _Cutoff;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _BakedTones;
    half _BakedSoftness;
    half _Wetness;
    half _SnowCover;
CBUFFER_END

TEXTURE2D(_NormalAtlas);
SAMPLER(sampler_NormalAtlas);

#define HB_BILLBOARD_REACH 0.75

float3 HB_BillboardFacing(float3 centerWS)
{
#ifdef HB_SHADOW_CASTER
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 toward = _LightPosition - centerWS;
    #else
    float3 toward = _LightDirection;
    #endif

    if (dot(toward.xz, toward.xz) > 1e-4)
        return toward;
#endif

    return GetCameraPositionWS() - centerWS;
}

float3 HB_BillboardPosition(float2 corner, out float3 toward, out float3 frame)
{
    float4x4 objectToWorld = GetObjectToWorldMatrix();
    float3 centerWS = TransformObjectToWorld(_BillboardCenter.xyz);
    float3 scale = float3(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21),
                          length(objectToWorld._m02_m12_m22));
    float3 up = normalize(objectToWorld._m01_m11_m21);

    toward = HB_BillboardFacing(centerWS);
    toward -= up * dot(toward, up);
    toward = dot(toward, toward) > 1e-8 ? normalize(toward) : normalize(objectToWorld._m02_m12_m22);

    float3 right = cross(up, -toward);
    float2 offset = (corner - 0.5) * _BillboardSize;
    float3 restWS = centerWS + right * (offset.x * max(scale.x, scale.z)) + up * (offset.y * scale.y);

    float3 towardOS = TransformWorldToObjectDir(toward);
    float angle = frac(atan2(towardOS.x, towardOS.z) / TWO_PI) * _Frames;
    float first = floor(angle);

    frame = float3(first, fmod(first + 1.0, _Frames), angle - first);

    precise float3 positionWS = restWS + HB_PlantSway(centerWS, corner.y * HB_BILLBOARD_REACH,
                                                      _BillboardSway * scale.y);

    return positionWS;
}

float3 HB_BillboardTop()
{
    return TransformObjectToWorld(float3(_BillboardCenter.x, _BillboardTop, _BillboardCenter.z));
}

float2 HB_BillboardAtlasUV(float2 corner, float frame)
{
    return float2((frame + corner.x) / _Frames, corner.y);
}

void HB_BillboardClip(float2 corner, float3 frame)
{
    half first = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, HB_BillboardAtlasUV(corner, frame.x)).a;
    half second = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, HB_BillboardAtlasUV(corner, frame.y)).a;

    clip(lerp(first, second, half(frame.z)) - _Cutoff);
}

half4 HB_BillboardNormal(float2 corner, float3 frame)
{
    half4 first = SAMPLE_TEXTURE2D(_NormalAtlas, sampler_NormalAtlas, HB_BillboardAtlasUV(corner, frame.x));
    half4 second = SAMPLE_TEXTURE2D(_NormalAtlas, sampler_NormalAtlas, HB_BillboardAtlasUV(corner, frame.y));
    half4 mixed = lerp(first, second, half(frame.z));

    return half4(NormalizeNormalPerPixel(TransformObjectToWorldNormal(float3(mixed.rgb * 2.0h - 1.0h))), mixed.a);
}

#endif
