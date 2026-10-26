#ifndef HB_TERRAIN_DEPTH_PASS_INCLUDED
#define HB_TERRAIN_DEPTH_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainInput.hlsl"

#ifdef HB_SHADOW_CASTER
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;
#elif !defined(HB_DEPTH_NORMALS)
float _HB_ShelterPass;
#endif

struct HBTerrainDepthAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBTerrainDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
#ifndef HB_SHADOW_CASTER
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

HBTerrainDepthVaryings HB_TerrainDepthVertex(HBTerrainDepthAttributes input)
{
    HBTerrainDepthVaryings output = (HBTerrainDepthVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
#ifndef HB_SHADOW_CASTER
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

    HBTerrainVertex vertex = HB_TerrainVertex(input.positionOS.xyz);
    output.positionWS = vertex.positionWS;

#ifdef HB_SHADOW_CASTER
    float3 normalWS = HB_TerrainNormal(vertex.positionWS.xz);

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - vertex.positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    output.positionCS = ApplyShadowClamping(
        TransformWorldToHClip(ApplyShadowBias(vertex.positionWS, normalWS, lightDirectionWS)));
#else
    output.positionCS = TransformWorldToHClip(vertex.positionWS);

    #ifndef HB_DEPTH_NORMALS
    if (_HB_ShelterPass > 0.5)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);
    #endif
#endif

    return output;
}

#if defined(HB_SHADOW_CASTER)

half4 HB_TerrainShadowFragment(HBTerrainDepthVaryings input) : SV_TARGET
{
    HB_TerrainClipHoles(input.positionWS);
    return 0;
}

#elif defined(HB_DEPTH_NORMALS)

void HB_TerrainDepthNormalsFragment(
    HBTerrainDepthVaryings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_TerrainClipHoles(input.positionWS);
    outNormalWS = half4(half3(HB_TerrainNormal(input.positionWS.xz)), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HB_TerrainDepthFragment(HBTerrainDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_TerrainClipHoles(input.positionWS);
    return input.positionCS.z;
}

#endif

#endif
