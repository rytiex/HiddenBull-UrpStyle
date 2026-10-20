#ifndef HIDDENBULL_URPSTYLE_GRASS_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_GRASS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#define HB_WEATHER_NO_BOUNDS
#define HB_WET_FILM_SMOOTHNESS 0.5h
#define _HB_BRUSH

float4 _HB_GrassDraw;

#define HB_BAKED_INPUT _HB_GrassDraw.w

#ifndef _CLUSTER_LIGHT_LOOP
    #undef _ADDITIONAL_LIGHTS
#endif

#ifdef _HB_GRASS_FAR
    #undef _ADDITIONAL_LIGHTS
    #define HB_SHADOW_SIMPLE
#else
    #define HB_SHADOW_CRISP_REACH 6.0
#endif

#include "../Library/StyleLighting.hlsl"
#include "../Library/StyleWeatherSurface.hlsl"
#include "StyleGrassInstance.hlsl"

#define HB_GRASS_CUTOFF       0.4h
#define HB_GRASS_GLOW_BASE    0.4h
#define HB_GRASS_GLOW_HIGH    0.55h
#define HB_GRASS_GLOW_LOW     0.2h
#define HB_GRASS_SNOW_TIPS    0.6h
#define HB_GRASS_FLOWER_GLOW  2.0h

StructuredBuffer<HiddenBullGrassInstance> _HB_GrassBuffer;

TEXTURE2D(_HB_GrassTexture);
SAMPLER(sampler_HB_GrassTexture);
TEXTURE2D(_HB_GrassLightmap);
TEXTURE2D(_HB_GrassShadowMask);

float4 _HB_GrassStyle;
float4 _HB_GrassBrush;
float4 _HB_GrassBrushLook;
float4 _HB_GrassGlowSun;
float4 _HB_GrassGlowShade;
float4 _HB_GrassFar;
float4 _HB_GrassGround;
float4 _HB_GrassLightmapST;
float4 _HB_GrassBenders[16];
float4 _HB_GrassBenderLooks[16];

#define HB_GRASS_CAPACITY       _HB_GrassDraw.x
#define HB_GRASS_RECEIVE        _HB_GrassDraw.y
#define HB_GRASS_VARIANTS       _HB_GrassDraw.z
#define HB_GRASS_TRANSLUCENCY   _HB_GrassStyle.w
#define HB_GRASS_BRUSH_ALBEDO   _HB_GrassBrushLook.x
#define HB_GRASS_BRUSH_ENABLED  _HB_GrassBrushLook.y
#define HB_GRASS_BAKED_TONES    _HB_GrassBrushLook.z
#define HB_GRASS_BAKED_SOFTNESS _HB_GrassBrushLook.w
#define HB_GRASS_FAR            _HB_GrassFar.x
#define HB_GRASS_FAR_RATE       _HB_GrassFar.y
#define HB_GRASS_BENDERS        _HB_GrassFar.z

#include "StyleGrassVertex.hlsl"

half HB_GrassCover(GrassVaryings input, out half3 shade)
{
    float fade = saturate((HB_GRASS_FAR - distance(input.rootWS, GetCameraPositionWS())) * HB_GRASS_FAR_RATE);
    float dither = frac(52.9829189 * frac(dot(input.positionCS.xy, float2(0.06711056, 0.00583715))));

    half4 picture = SAMPLE_TEXTURE2D(_HB_GrassTexture, sampler_HB_GrassTexture, input.uv);

    shade = picture.rgb;

#ifdef _HB_GRASS_FLOWERS
    if (input.uv.x < 0.0)
    {
        shade = input.tip;

        return half(fade - dither);
    }
#endif

    return min(picture.a - HB_GRASS_CUTOFF, half(fade - dither));
}

#ifdef HB_GRASS_DEPTH

half4 HiddenBullGrassDepthFragment(GrassVaryings input) : SV_Target
{
    half3 shade;

    clip(HB_GrassCover(input, shade));

    return 0;
}

#else

half3 HB_GrassGlow(half3 viewDirectionWS, half sunVisibility, half rise, half snow)
{
    bool keyed = dot(_HB_KeyDirection.xyz, _HB_KeyDirection.xyz) > 0.5;
    half3 sunDirection = keyed ? half3(_HB_KeyDirection.xyz) : half3(_MainLightPosition.xyz);
    half3 sunColour = keyed ? half3(_HB_KeyColor.rgb) : half3(_MainLightColor.rgb);

    half facing = saturate(dot(-viewDirectionWS, sunDirection) * 0.5h + 0.5h);
    half lowSun = saturate((HB_GRASS_GLOW_HIGH - sunDirection.y) / (HB_GRASS_GLOW_HIGH - HB_GRASS_GLOW_LOW));
    half level = 1.0h - saturate(viewDirectionWS.y);
    half through = lerp(HB_GRASS_GLOW_BASE, 1.0h, facing * facing) * lowSun * level;

    half3 glow = half3(_HB_GrassGlowSun.rgb) * sunColour * sunVisibility
               + half3(_HB_GrassGlowShade.rgb) * (1.0h - sunVisibility);

    return glow * (rise * rise * through * half(HB_GRASS_TRANSLUCENCY) * (1.0h - snow));
}

half4 HiddenBullGrassFragment(GrassVaryings input) : SV_Target
{
    half3 shade;

    HB_GrassCover(input, shade);

    half3 normalWS = normalize(input.normalWS);
    half rise = smoothstep(0.0h, 1.0h, half(input.uv.y));

    InputData inputData = (InputData)0;
    inputData.positionWS = HB_GrassLift(input.positionWS);
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

    UNITY_BRANCH
    if (HB_BAKED_INPUT > 0.5)
    {
        float2 lightmapUV = (input.rootWS.xz - _HB_GrassGround.xy) * _HB_GrassGround.zw * _HB_GrassLightmapST.xy
                          + _HB_GrassLightmapST.zw;

        inputData.bakedGI = DecodeLightmap(
            SAMPLE_TEXTURE2D_LOD(_HB_GrassLightmap, sampler_LinearClamp, lightmapUV, 0.0),
            half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h));
        inputData.shadowMask = SAMPLE_TEXTURE2D_LOD(_HB_GrassShadowMask, sampler_LinearClamp, lightmapUV, 0.0);
    }

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#endif

    SurfaceData surfaceData = (SurfaceData)0;

#ifdef _HB_GRASS_FLOWERS
    surfaceData.albedo = shade;
#else
    surfaceData.albedo = lerp(input.root, input.tip * shade, rise);
#endif
    surfaceData.occlusion = 1.0h;
    surfaceData.alpha = 1.0h;

    HiddenBullBrushSample brush = HB_NoBrush();
    half ground = brush.coverage;

#ifndef _HB_GRASS_FAR
    UNITY_BRANCH
    if (HB_GRASS_BRUSH_ENABLED > 0.5)
        brush = HB_SampleBrush(input.positionWS, normalWS, 0.0h, float3(0.0, 0.0, 0.0),
                               max(half(_HB_GrassBrush.x), HB_EPSILON), 1.0h);

    ground = brush.coverage;

    UNITY_BRANCH
    if (HB_GRASS_BRUSH_ENABLED > 0.5 && HB_GRASS_RECEIVE > 0.0
        && (HB_SURFACE_WET > 0.0 || HB_SURFACE_SNOW > 0.0))
        ground = HB_SampleBrush(input.rootWS, normalWS, 0.0h, float3(0.0, 0.0, 0.0),
                                max(half(_HB_GrassBrush.x), HB_EPSILON), 1.0h).coverage;
#endif

    HiddenBullWeather weather = HB_ResolveWeather(input.rootWS, normalWS, ground, float3(0.0, 0.0, 0.0),
                                                  float3(0.0, 0.0, 0.0), half(HB_GRASS_RECEIVE),
                                                  half4(0.0h, 0.0h, 0.0h, 0.0h), half2(1.0h, 1.0h), input.limits);

    weather.snow *= 1.0h - HB_GRASS_SNOW_TIPS * rise;
    weather.flow = 0.0h;

    half film = HB_ApplyGroundWeather(weather, surfaceData, normalWS);

    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = half(_HB_GrassStyle.x);
    style.diffuseSoftness = half(_HB_GrassStyle.y);
    style.halfTone = half(_HB_GrassStyle.z);
    style.bakedTones = half(HB_GRASS_BAKED_TONES);
    style.bakedSoftness = half(HB_GRASS_BAKED_SOFTNESS);
    style.brushRelief = half(_HB_GrassBrush.y);
    style.brushShading = half(_HB_GrassBrush.z);
    style.brushAmbient = half(_HB_GrassBrush.w);
    style.brushAlbedo = half(HB_GRASS_BRUSH_ALBEDO);

    HB_SoftenBrushUnderSnow(weather.snow, style);

    half sunVisibility;
    half lightReach;

    half3 colour = HiddenBullFragmentLit(inputData, surfaceData, style, brush, film, sunVisibility, lightReach).rgb;

    half close = half(saturate(1.0 - distance(input.rootWS, GetCameraPositionWS()) / HB_GRASS_FAR));

#ifdef _HB_GRASS_FLOWERS
    half through = input.uv.x < 0.0 ? rise : 1.0h;

    colour += HB_GrassGlow(inputData.viewDirectionWS, sunVisibility, through, weather.snow)
            * (surfaceData.albedo * (HB_GRASS_FLOWER_GLOW * close));
#else
    colour += HB_GrassGlow(inputData.viewDirectionWS, sunVisibility, rise, weather.snow) * close;
#endif

    UNITY_BRANCH
    if (HB_WEATHER_DEBUG > 0.5)
        colour = weather.debug;

    return half4(colour, 1.0h);
}

#endif

#endif
