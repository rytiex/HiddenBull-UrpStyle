#ifndef HIDDENBULL_URPSTYLE_PARTICLE_PASS_INCLUDED
#define HIDDENBULL_URPSTYLE_PARTICLE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half _DiffuseWrap;
    half _DiffuseSoftness;
    half _HalfTone;
    half _SoftDistance;
    half _CameraFadeNear;
    half _CameraFadeFar;
    half _BrushScale;
    half _StrokeAlpha;
CBUFFER_END

#ifdef HB_PARTICLE_COLOR
    #include "../Library/StyleLighting.hlsl"
#endif

#include "../Library/StyleBrush.hlsl"
#include "../Library/StyleFog.hlsl"
#include "../Library/StyleTransparency.hlsl"

struct ParticleAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    half4 color       : COLOR;
    float4 texcoords  : TEXCOORD0;
#ifdef _FLIPBOOKBLENDING_ON
    float texcoordBlend : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ParticleVaryings
{
    float4 positionCS : SV_POSITION;
    float4 uv         : TEXCOORD0;
#ifdef _FLIPBOOKBLENDING_ON
    float3 blendUv    : TEXCOORD1;
#endif
    float3 positionWS : TEXCOORD2;
    half3 normalWS    : TEXCOORD3;
    half4 color       : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct HiddenBullParticleSurface
{
    half3 colour;
    half alpha;
    half3 emission;
};

half4 HB_ParticleVertexColor(half4 color)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED) && !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    color = lerp(half4(1.0h, 1.0h, 1.0h, 1.0h), color, unity_ParticleUseMeshColors);
    color *= half4(UnpackFromR8G8B8A8(data.color));
#endif
    return color;
}

void HB_ParticleTexcoords(float4 texcoords, float blend, out float2 uv, out float3 blendUv)
{
    uv = texcoords.xy;
    blendUv = float3(texcoords.zw, blend);

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    if (unity_ParticleUVShiftData.x != 0.0)
    {
        UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

        float tilesX = unity_ParticleUVShiftData.y;
        float2 scale = unity_ParticleUVShiftData.zw;

    #ifdef UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME
        float frame = 0.0;
    #else
        float frame = data.animFrame;
    #endif

        float first = floor(frame);
        float firstRow = floor(first / tilesX);
        uv = texcoords.xy * scale + float2((first - firstRow * tilesX) * scale.x,
                                           (1.0 - scale.y) - firstRow * scale.y);

        float second = first + 1.0;
        float secondRow = floor(second / tilesX);
        blendUv = float3(texcoords.xy * scale + float2((second - secondRow * tilesX) * scale.x,
                                                       (1.0 - scale.y) - secondRow * scale.y),
                         frac(frame));
    }
#endif
}

ParticleVaryings HiddenBullParticleVertex(ParticleAttributes input)
{
    ParticleVaryings output = (ParticleVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionWS = positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.color = HB_ParticleVertexColor(input.color);

#ifdef _FLIPBOOKBLENDING_ON
    float blend = input.texcoordBlend;
#else
    float blend = 0.0;
#endif

    float2 uv;
    float3 blendUv;
    HB_ParticleTexcoords(input.texcoords, blend, uv, blendUv);

    output.uv = float4(TRANSFORM_TEX(uv, _BaseMap), input.texcoords.xy);

#ifdef _FLIPBOOKBLENDING_ON
    output.blendUv = float3(TRANSFORM_TEX(blendUv.xy, _BaseMap), blendUv.z);
#endif

    return output;
}

half HB_ParticleStrokes(float2 uv)
{
#ifdef _HB_BRUSH
    float2 brushUv = uv * max(float(_BrushScale), 1e-3);
    float2 brushDx = ddx(brushUv);
    float2 brushDy = ddy(brushUv);

    if (HB_BRUSH_ATLAS_BOUND < 0.5h)
        return 0.0h;

    return HB_BrushPlane(brushUv, brushDx, brushDy).coverage * 2.0h - 1.0h;
#else
    return 0.0h;
#endif
}

HiddenBullParticleSurface HB_ParticleSurface(ParticleVaryings input, float2 screenUV)
{
    half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);

#ifdef _FLIPBOOKBLENDING_ON
    texel = lerp(texel, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.blendUv.xy), half(input.blendUv.z));
#endif

    half4 tint = _BaseColor * input.color;
    half fade = tint.a;

    float eye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());

#ifdef _HB_SOFT_PARTICLES
    float scene = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
    fade *= half(saturate((scene - eye) / max(float(_SoftDistance), 1e-3)));
#endif

#ifdef _HB_CAMERA_FADE
    fade *= half(saturate((eye - _CameraFadeNear) / max(float(_CameraFadeFar - _CameraFadeNear), 1e-3)));
#endif

    half alpha = texel.a * fade;
    half coverage = HB_ParticleStrokes(input.uv.zw);
    half stroked = saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);

    fade *= stroked / max(alpha, HB_EPSILON);

    HiddenBullParticleSurface surface;
    surface.alpha = stroked;

#if defined(_HB_PREMULTIPLY)
    surface.colour = texel.rgb * tint.rgb * fade;
#else
    surface.colour = texel.rgb * tint.rgb * stroked;
#endif

#ifdef _HB_EMISSION
    surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv.xy).rgb
                     * half3(_EmissionColor.rgb) * fade;
#else
    surface.emission = half3(0.0h, 0.0h, 0.0h);
#endif

    return surface;
}

#ifdef HB_PARTICLE_COLOR

half3 HB_ParticleLight(ParticleVaryings input, float2 screenUV)
{
#ifdef _HB_PARTICLE_LIT
    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    normalWS = dot(normalWS, viewDirectionWS) < 0.0h ? -normalWS : normalWS;

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirectionWS;
    inputData.normalizedScreenSpaceUV = screenUV;
    inputData.bakedGI = HB_SampleProbeGI(inputData.positionWS, normalWS, inputData.viewDirectionWS,
                                         input.positionCS.xy, inputData.shadowMask);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
    surfaceData.alpha = 1.0h;
    surfaceData.occlusion = 1.0h;

    HiddenBullStyleData style = (HiddenBullStyleData)0;
    style.diffuseWrap = _DiffuseWrap;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedSoftness = 0.5h;

    half sunVisibility;
    half lightReach;

    return HiddenBullFragmentLit(inputData, surfaceData, style, HB_NoBrush(), 0.0h, sunVisibility, lightReach).rgb;
#else
    return half3(1.0h, 1.0h, 1.0h);
#endif
}

void HiddenBullParticleColorFragment(ParticleVaryings input, out half4 outFront : SV_Target0,
                                     out half4 outBehind : SV_Target1, out float4 outOffset : SV_Target2,
                                     out half4 outTint : SV_Target3, out half4 outBehindTint : SV_Target4)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

    HiddenBullParticleSurface surface = HB_ParticleSurface(input, screenUV);
    half3 colour = surface.colour * HB_ParticleLight(input, screenUV);

    HB_FogResult fog = HB_ResolveFog(input.positionWS, screenUV, false);

#ifdef _HB_ADDITIVE
    colour = HB_FogLight(colour, fog.amount);
    half weight = 0.0h;
#else
    colour = lerp(colour, fog.colour * surface.alpha, fog.amount);
    half weight = surface.alpha;
#endif

    colour += HB_FogLight(surface.emission, fog.amount);

    uint2 pixel = uint2(input.positionCS.xy);
    float depth = HB_OITDepth(input.positionWS);

    bool behind = HB_OITBehindRefraction(pixel, depth);

    half transmittance = half(behind ? HB_OITBehindTransmittanceAt(pixel, depth)
                                     : HB_OITFrontTransmittanceAt(pixel, depth));

    half4 contribution = half4(colour * transmittance, weight * transmittance);
    half4 white = half4(1.0h, 1.0h, 1.0h, 1.0h);

    outFront = behind ? half4(0.0h, 0.0h, 0.0h, 0.0h) : contribution;
    outBehind = behind ? contribution : half4(0.0h, 0.0h, 0.0h, 0.0h);
    outOffset = float4(0.0, 0.0, 0.0, 0.0);
    outTint = white;
    outBehindTint = white;
}

#else

void HiddenBullParticleMomentsFragment(ParticleVaryings input, out float2 outB0 : SV_Target0,
                                       out float4 outFront : SV_Target1, out float4 outBehind : SV_Target2)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
    HiddenBullParticleSurface surface = HB_ParticleSurface(input, screenUV);

    float depth = HB_OITDepth(input.positionWS);
    float b0;
    float4 moments;

    HB_OITMoments(HB_OITAbsorbance(surface.alpha), depth, b0, moments);

    bool behind = HB_OITBehindRefraction(uint2(input.positionCS.xy), depth);

    outB0 = behind ? float2(0.0, b0) : float2(b0, 0.0);
    outFront = behind ? float4(0.0, 0.0, 0.0, 0.0) : moments;
    outBehind = behind ? moments : float4(0.0, 0.0, 0.0, 0.0);
}

#endif

#endif
