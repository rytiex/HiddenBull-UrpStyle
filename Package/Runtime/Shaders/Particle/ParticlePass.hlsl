#ifndef HB_PARTICLE_PASS_INCLUDED
#define HB_PARTICLE_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticleInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#ifdef _HB_PARTICLE_LIT
    #define HB_VOLUME_LIGHT

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleLighting.hlsl"
#endif

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Brush/StyleBrush.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparency.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Transparency/StyleTransparentFog.hlsl"

struct Attributes
{
    float4 positionOS     : POSITION;
    half4 color           : COLOR;
    float4 texcoords      : TEXCOORD0;
#ifdef _FLIPBOOKBLENDING_ON
    float texcoordBlend   : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uv                 : TEXCOORD0;
    float3 positionWS         : TEXCOORD1;
    half4 color               : TEXCOORD3;

#ifdef _FLIPBOOKBLENDING_ON
    float3 blendUv            : TEXCOORD4;
#endif

#ifdef _HB_PARTICLE_LIT
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLight         : TEXCOORD5;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord        : TEXCOORD6;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

    #ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion     : TEXCOORD9;
    #endif
#endif

    float4 positionCS         : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#ifdef _HB_PARTICLE_LIT
    #define HB_NO_BRUSH_SHIFT
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleForwardInput.hlsl"
#endif

struct HBParticleSurface
{
    half3 colour;
    half alpha;
    half3 glow;
    half3 emission;
};

half4 HB_ParticleVertexColour(half4 colour)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED) && !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    colour = lerp(half4(1.0h, 1.0h, 1.0h, 1.0h), colour, unity_ParticleUseMeshColors);
    colour *= half4(UnpackFromR8G8B8A8(data.color));
#endif
    return colour;
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

Varyings HB_ParticleVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionWS = positionWS;
    output.color = HB_ParticleVertexColour(input.color);

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

#ifdef _HB_PARTICLE_LIT
    half3 normalWS = half3(0.0h, 1.0h, 0.0h);

    OUTPUT_SH4(positionWS, normalWS, GetWorldSpaceNormalizeViewDir(positionWS), output.vertexSH, output.probeOcclusion);

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLight = VertexLighting(positionWS, normalWS);
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    VertexPositionInputs vertexInput = (VertexPositionInputs)0;
    vertexInput.positionWS = positionWS;
    vertexInput.positionCS = output.positionCS;
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif
#endif

    return output;
}

half HB_ParticleStrokes(float2 uv)
{
#ifdef _HB_BRUSH
    float2 brushUv = uv * max(float(_BrushScale), 1e-3);
    float2 brushDx = ddx(brushUv);
    float2 brushDy = ddy(brushUv);
    half coverage = 0.0h;

    UNITY_BRANCH
    if (HB_BRUSH_ATLAS_BOUND >= 0.5)
        coverage = HB_BrushPlane(brushUv, brushDx, brushDy).coverage * 2.0h - 1.0h;

    return coverage;
#else
    return 0.0h;
#endif
}

HBParticleSurface HB_ParticleSurface(Varyings input)
{
    half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);

#ifdef _FLIPBOOKBLENDING_ON
    texel = lerp(texel, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.blendUv.xy), half(input.blendUv.z));
#endif

    half4 tint = _BaseColor * input.color;
    half fade = tint.a;

    float eye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());

#ifdef _HB_SOFT_PARTICLES
    float scene = LinearEyeDepth(LoadSceneDepth(uint2(input.positionCS.xy)), _ZBufferParams);
    fade *= half(saturate((scene - eye) / max(float(_SoftDistance), 1e-3)));
#endif

#ifdef _HB_CAMERA_FADE
    fade *= half(saturate((eye - _CameraFadeNear) / max(float(_CameraFadeFar - _CameraFadeNear), 1e-3)));
#endif

    half alpha = texel.a * fade;
    half coverage = HB_ParticleStrokes(input.uv.zw);
    half stroked = saturate(alpha + coverage * half(_StrokeAlpha) * alpha * (1.0h - alpha) * 4.0h);

    half strokeRatio = stroked / max(alpha, half(HB_EPSILON));

    HBParticleSurface surface;
    surface.alpha = stroked;

#ifdef _HB_PREMULTIPLY
    half3 premultiplied = texel.rgb * tint.rgb * fade;
    half3 covered = min(premultiplied, alpha);

    surface.colour = covered * strokeRatio;
    surface.glow = (premultiplied - covered) * strokeRatio;
#else
    surface.colour = texel.rgb * tint.rgb * stroked;
    surface.glow = half3(0.0h, 0.0h, 0.0h);
#endif

    fade *= strokeRatio;

#ifdef _HB_EMISSION
    surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv.xy).rgb
                     * half3(_EmissionColor.rgb) * fade;
#else
    surface.emission = half3(0.0h, 0.0h, 0.0h);
#endif

    return surface;
}

half3 HB_ParticleLight(Varyings input)
{
#ifdef _HB_PARTICLE_LIT
    half3 viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
    half3 normalWS = half3(0.0h, 1.0h, 0.0h);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
    surfaceData.alpha = 1.0h;
    surfaceData.occlusion = 1.0h;
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);

    InputData inputData;
    HB_InitializeInputData(input, normalWS, viewDirectionWS, surfaceData.normalTS, inputData);
    HB_InitializeBakedGI(input, float2(0.0, 0.0), inputData);

    HBStyleData style = (HBStyleData)0;
    style.diffuseSoftness = _DiffuseSoftness;
    style.halfTone = _HalfTone;
    style.bakedSoftness = 0.5h;

    return HB_StyleLit(inputData, surfaceData, style, HB_NoBrush(), false, 0.0h).rgb;
#else
    return half3(1.0h, 1.0h, 1.0h);
#endif
}

half4 HB_ParticleFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float eye = LinearEyeDepth(input.positionWS, GetWorldToViewMatrix());

    UNITY_BRANCH
    if (HB_TransparentOutsideStage(uint2(input.positionCS.xy), eye))
    {
        discard;
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    HBParticleSurface surface = HB_ParticleSurface(input);
    HBTransparentFog fog = HB_TransparentFog(input.positionWS);

    half3 light = HB_ParticleLight(input);
    half3 colour = surface.colour * light * fog.clear;
    half3 glow = (surface.emission + surface.glow * light) * fog.clear;

#ifdef _HB_ADDITIVE
    return half4(colour + glow, 0.0h);
#else
    return half4(colour + fog.colour * surface.alpha + glow, surface.alpha);
#endif
}

#endif
