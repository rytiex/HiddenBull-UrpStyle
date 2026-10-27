#ifndef HB_GRASS_PASS_INCLUDED
#define HB_GRASS_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleCore.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassInput.hlsl"

#if defined(HB_GRASS_FORWARD)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Lighting/StyleAmbient.hlsl"
#endif

StructuredBuffer<uint3> _HB_GrassBlades;
StructuredBuffer<int4> _HB_GrassSlots;
StructuredBuffer<uint> _HB_GrassList;

TEXTURE2D_ARRAY(_HB_GrassTufts);
SAMPLER(sampler_HB_GrassTufts);

float4 _HB_LayerGrass[16];
float4 _HB_LayerGrassTip[16];
float4 _HB_GrassMode;
float4 _HB_GrassStyle;

#define HB_GRASS_FAR                (_HB_GrassMode.x > 0.5)
#define HB_GRASS_STYLE_WRAP         _HB_GrassStyle.x
#define HB_GRASS_STYLE_SOFTNESS     _HB_GrassStyle.y
#define HB_GRASS_STYLE_HALF_TONE    _HB_GrassStyle.z
#define HB_GRASS_CUTOFF       0.5h
#define HB_GRASS_DETAIL_SCALE 2.0h
#define HB_GRASS_TIP_START    0.35h
#define HB_GRASS_ROOT_SHADE   0.55h
#define HB_GRASS_NORMAL_UP    0.7
#define HB_GRASS_LEAN         0.25
#define HB_GRASS_SWAY         0.12
#define HB_GRASS_SWAY_CALM    0.04
#define HB_GRASS_SWAY_RATE    1.7
#define HB_DEBUG_GRASS_CELLS  16

static const float2 HB_GRASS_CORNERS[6] =
{
    float2(0.0, 0.0), float2(1.0, 0.0), float2(1.0, 1.0),
    float2(0.0, 0.0), float2(1.0, 1.0), float2(0.0, 1.0)
};

struct HBGrassVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float4 uv         : TEXCOORD1;
    float3 normalWS   : TEXCOORD2;
    float3 ground     : TEXCOORD3;
    nointerpolation uint2 data : TEXCOORD4;
};

HBGrassVaryings HB_GrassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    uint index = _HB_GrassList[instanceID];
    uint slot = index / HB_GRASS_PER_SLOT;
    uint local = index - slot * HB_GRASS_PER_SLOT;
    int2 cell = _HB_GrassSlots[slot].xy;
    HBGrassBlade blade = HB_GrassUnpack(_HB_GrassBlades[index], cell, local);

    float gap = distance(blade.root, HB_GRASS_CAMERA);
    float width = blade.width;
    uint card = vertexID / 6u;
    float2 corner = HB_GRASS_CORNERS[vertexID - card * 6u];
    float3 across;

    if (HB_GRASS_FAR)
    {
        float3 toCamera = HB_GRASS_CAMERA - blade.root;

        across = normalize(float3(toCamera.z, 0.0, -toCamera.x) + float3(1e-5, 0.0, 0.0));
        width *= rsqrt(max(HB_GrassKeep(gap), 1e-3));
    }
    else
    {
        float yaw = blade.rand * TWO_PI + card * HALF_PI;

        across = float3(cos(yaw), 0.0, sin(yaw));
    }

    float2 wind = float2(sin(HB_WIND_HEADING), cos(HB_WIND_HEADING));
    float sway = (HB_GRASS_SWAY_CALM + HB_GRASS_SWAY * HB_WIND)
               * sin(_Time.y * HB_GRASS_SWAY_RATE + blade.rand * TWO_PI * 4.0);
    float bend = corner.y * corner.y * blade.height;

    float3 positionWS = blade.root + across * ((corner.x - 0.5) * width);
    positionWS.y += corner.y * blade.height;
    positionWS.xz += wind * ((HB_GRASS_LEAN * HB_WIND + sway) * bend);

    float3 facing = cross(across, float3(0.0, 1.0, 0.0));
    facing *= dot(facing, HB_GRASS_CAMERA - blade.root) < 0.0 ? -1.0 : 1.0;

    HBGrassVaryings output;
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.uv = float4(corner, corner.y, gap);
    output.normalWS = normalize(lerp(facing, float3(0.0, 1.0, 0.0), HB_GRASS_NORMAL_UP));
    output.ground = blade.ground;
    output.data = uint2(blade.layer, (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663));
    return output;
}

half4 HB_GrassTuft(HBGrassVaryings input, out half cover)
{
    float slice = _HB_LayerGrass[input.data.x].w;
    half4 tuft = half4(SAMPLE_TEXTURE2D_ARRAY(_HB_GrassTufts, sampler_HB_GrassTufts, input.uv.xy, slice));

    float gap = input.uv.w;
    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    float fade = saturate((HB_GRASS_DISTANCE - gap) / max(HB_GRASS_FADE, 1e-3));
    float nearShare = saturate((HB_GRASS_NEAR + HB_GRASS_DITHER * 0.5 - gap) / max(HB_GRASS_DITHER, 1e-3));
    float keep = HB_GRASS_FAR ? (dither > nearShare ? 1.0 : -1.0) : (dither <= nearShare ? 1.0 : -1.0);

    cover = min(tuft.a - HB_GRASS_CUTOFF, half(min(fade - dither, keep)));
    return tuft;
}

#if defined(HB_GRASS_FORWARD)

half HB_GrassShadow(float3 positionWS)
{
    half shadow = 1.0h;

#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    shadow = MainLightRealtimeShadow(ComputeScreenPos(TransformWorldToHClip(positionWS)));
#elif defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);

    UNITY_BRANCH
    if (!BEYOND_SHADOW_FAR(shadowCoord))
    {
        shadow = half(SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_LinearClampCompare,
                                              shadowCoord.xyz));
        shadow = LerpWhiteTo(shadow, GetMainLightShadowParams().x);
    }

    shadow = lerp(shadow, 1.0h, GetMainLightShadowFade(positionWS));
#endif

    return shadow;
}

half4 HB_GrassFragment(HBGrassVaryings input) : SV_Target
{
    half cover;
    half4 tuft = HB_GrassTuft(input, cover);

    clip(cover);

    half height = half(input.uv.z);
    half detail = tuft.r * HB_GRASS_DETAIL_SCALE;
    float4 tip = _HB_LayerGrassTip[input.data.x];
    half tipShare = half(tip.w) * smoothstep(HB_GRASS_TIP_START, 1.0h, height);
    half3 albedo = lerp(half3(input.ground), half3(tip.rgb), tipShare) * detail;
    half shade = lerp(HB_GRASS_ROOT_SHADE, 1.0h, height);
    half3 normalWS = half3(normalize(input.normalWS));

    half wrap = half(HB_GRASS_STYLE_WRAP);
    half inverseWrap = rcp(1.0h + wrap);
    half lambert = saturate((dot(normalWS, half3(_MainLightPosition.xyz)) + wrap) * inverseWrap);
    half tone = HB_ToneBands(lambert * HB_GrassShadow(input.positionWS), wrap * inverseWrap,
                             half(HB_GRASS_STYLE_HALF_TONE), half(HB_GRASS_STYLE_SOFTNESS));
    half3 direct = half3(_MainLightColor.rgb) * tone;
    half reach;
    half3 ambient = HB_ResolveAmbient(normalWS, HB_GradientAmbient(normalWS), 0.0h, 0.0h, 0.0h, reach);
    half3 colour = albedo * (direct + ambient) * shade;

#if defined(HB_STYLE_DEBUG)
    if (HB_DEBUG_VIEW == HB_DEBUG_GRASS_CELLS)
    {
        uint hash = input.data.y * 2654435761u;
        half3 cellColour = half3((hash >> 8u) & 255u, (hash >> 16u) & 255u, (hash >> 24u) & 255u) / 255.0h;

        colour = cellColour * (HB_GRASS_FAR ? 0.5h : 1.0h) * lerp(0.6h, 1.0h, height);
    }
#endif

    return half4(colour, 1.0h);
}

#elif defined(HB_DEPTH_NORMALS)

void HB_GrassDepthNormalsFragment(
    HBGrassVaryings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    half cover;
    HB_GrassTuft(input, cover);
    clip(cover);

    outNormalWS = half4(half3(normalize(input.normalWS)), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HB_GrassDepthFragment(HBGrassVaryings input) : SV_Target
{
    half cover;
    HB_GrassTuft(input, cover);
    clip(cover);

    return input.positionCS.z;
}

#endif

#endif
