#ifndef HIDDENBULL_URPSTYLE_BILLBOARD_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_BILLBOARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "../Library/StyleBillboardInstancing.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BillboardCenter;
    half4 _BaseColor;
    float _BillboardSize;
    float _Frames;
    half _Cutoff;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _WindStrength;
    half _ReceiveWeather;
    half _Wetness;
    half _SnowCover;
CBUFFER_END

TEXTURE2D(_NormalAtlas);
SAMPLER(sampler_NormalAtlas);

#if defined(HB_BILLBOARD_FORWARD)
    #include "../Library/StyleLighting.hlsl"
    #include "../Library/StyleWeatherSurface.hlsl"
#else
    #include "../Library/StyleCommon.hlsl"
#endif

#if defined(HB_BILLBOARD_SHADOW)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;
#endif

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define HB_BILLBOARD_SWAY_SPEED 1.1
#define HB_BILLBOARD_SWAY_REACH 0.03

struct BillboardAttributes
{
    float4 positionOS : POSITION;
    float2 corner     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct BillboardVaryings
{
    float4 positionCS : SV_POSITION;
    float2 corner     : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 frame      : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
#if !defined(HB_BILLBOARD_SHADOW)
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

float3 HB_BillboardFacing(float3 centerWS)
{
#if defined(HB_BILLBOARD_SHADOW)
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

BillboardVaryings HiddenBullBillboardVertex(BillboardAttributes input)
{
    BillboardVaryings output = (BillboardVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#if !defined(HB_BILLBOARD_SHADOW)
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

    float3 centerWS = TransformObjectToWorld(_BillboardCenter.xyz);
    float3 scale = float3(length(GetObjectToWorldMatrix()._m00_m10_m20),
                          length(GetObjectToWorldMatrix()._m01_m11_m21),
                          length(GetObjectToWorldMatrix()._m02_m12_m22));

    float3 up = normalize(GetObjectToWorldMatrix()._m01_m11_m21);
    float3 toward = HB_BillboardFacing(centerWS);
    toward -= up * dot(toward, up);
    toward = dot(toward, toward) > 1e-8 ? normalize(toward) : normalize(GetObjectToWorldMatrix()._m02_m12_m22);

    float3 right = cross(up, -toward);
    float size = _BillboardSize * max(scale.x, scale.z);
    float height = _BillboardSize * scale.y;

    float2 offset = input.corner - 0.5;
    float3 positionWS = centerWS + right * (offset.x * size) + up * (offset.y * height);

    float sway = sin(_Time.y * HB_BILLBOARD_SWAY_SPEED + dot(centerWS.xz, float2(0.37, 0.23)))
               * HB_WIND_STRENGTH * _WindStrength * HB_BILLBOARD_SWAY_REACH * height * input.corner.y;
    positionWS.xz += HB_WIND_DIRECTION * sway;

    float3 towardOS = TransformWorldToObjectDir(toward);
    float angle = atan2(towardOS.x, towardOS.z);
    float frame = frac(angle / TWO_PI) * _Frames;
    float first = floor(frame);

    output.frame = float3(first, fmod(first + 1.0, _Frames), frame - first);
    output.corner = input.corner;
    output.positionWS = positionWS;

#if defined(HB_BILLBOARD_SHADOW)
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, toward, toward)));
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif

    return output;
}

float2 HB_BillboardAtlasUV(float2 corner, float frame)
{
    return float2((frame + corner.x) / _Frames, corner.y);
}

half4 HB_BillboardAlbedo(BillboardVaryings input, out float2 first, out float2 second)
{
    first = HB_BillboardAtlasUV(input.corner, input.frame.x);
    second = HB_BillboardAtlasUV(input.corner, input.frame.y);

    half4 a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, first);
    half4 b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, second);

    half4 albedo = lerp(a, b, half(input.frame.z)) * _BaseColor;
    clip(albedo.a - _Cutoff);

    return albedo;
}

half3 HB_BillboardNormal(BillboardVaryings input, float2 first, float2 second, out half3 weatherWS)
{
    half4 a = SAMPLE_TEXTURE2D(_NormalAtlas, sampler_NormalAtlas, first);
    half4 b = SAMPLE_TEXTURE2D(_NormalAtlas, sampler_NormalAtlas, second);
    half4 mixed = lerp(a, b, half(input.frame.z));
    half3 normalOS = mixed.rgb * 2.0h - 1.0h;

    half up = saturate(mixed.a);
    half2 side = normalOS.xz * rsqrt(max(dot(normalOS.xz, normalOS.xz), 1e-4h)) * sqrt(1.0h - up * up);

    weatherWS = NormalizeNormalPerPixel(TransformObjectToWorldNormal(float3(side.x, up, side.y)));
    weatherWS.y = abs(weatherWS.y);

    return NormalizeNormalPerPixel(TransformObjectToWorldNormal(float3(normalOS)));
}

#if defined(HB_BILLBOARD_FORWARD)

half4 HiddenBullBillboardFragment(BillboardVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 first;
    float2 second;
    half4 albedo = HB_BillboardAlbedo(input, first, second);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    half3 weatherNormalWS;
    half3 normalWS = HB_BillboardNormal(input, first, second, weatherNormalWS);

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.bakedGI = HB_SampleProbeGI(inputData.positionWS, normalWS, inputData.viewDirectionWS,
                                         input.positionCS.xy, inputData.shadowMask);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo.rgb;
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    HiddenBullWeather weather = HB_ResolveWeather(input.positionWS, weatherNormalWS, 0.0h, float3(0.0, 0.0, 0.0),
                                                  float3(0.0, 0.0, 0.0), half(_ReceiveWeather),
                                                  half4(0.0h, 0.0h, 0.0h, 0.0h), half2(1.0h, 1.0h),
                                                  half2(_Wetness, _SnowCover));

    HB_ApplyWeather(weather, surfaceData, normalWS);

    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedSoftness = 0.5h;

    half sunVisibility;
    half lightReach;

    half3 colour = HiddenBullFragmentLit(inputData, surfaceData, style, HB_NoBrush(), weather.wet, sunVisibility,
                                         lightReach).rgb;

    return half4(colour, 1.0h);
}

#elif defined(HB_BILLBOARD_DEPTH_NORMALS)

half4 HiddenBullBillboardDepthNormalsFragment(BillboardVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 first;
    float2 second;
    HB_BillboardAlbedo(input, first, second);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    half3 weatherNormalWS;

    return half4(HB_BillboardNormal(input, first, second, weatherNormalWS), 0.0h);
}

#else

half4 HiddenBullBillboardDepthFragment(BillboardVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
#if !defined(HB_BILLBOARD_SHADOW)
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#endif

    float2 first;
    float2 second;
    HB_BillboardAlbedo(input, first, second);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return 0;
}

#endif

#endif
