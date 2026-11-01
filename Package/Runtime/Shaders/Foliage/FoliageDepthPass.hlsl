#ifndef HB_FOLIAGE_DEPTH_PASS_INCLUDED
#define HB_FOLIAGE_DEPTH_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageInput.hlsl"

#if defined(HB_DEPTH_NORMALS) && defined(_NORMALMAP)
    #define HB_PLANT_TANGENT
#endif

#if defined(_ALPHATEST_ON) || defined(HB_PLANT_TANGENT)
    #define HB_PLANT_UV
#endif

#ifdef HB_SHADOW_CASTER
    #include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Shadow/StyleShadowCaster.hlsl"
#elif defined(HB_PLANT_SCENE)
int _ObjectId;
int _PassValue;
float4 _SelectionID;
#elif !defined(HB_DEPTH_NORMALS)
float _HB_ShelterPass;
#endif

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct HBPlantDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    float3 plantData  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBPlantDepthVaryings
{
#ifdef HB_PLANT_UV
    float2 uv         : TEXCOORD0;
#endif
#ifdef HB_DEPTH_NORMALS
    half3 normalWS    : TEXCOORD1;
#endif
#ifdef HB_PLANT_TANGENT
    half3 tangentWS   : TEXCOORD2;
    half3 bitangentWS : TEXCOORD3;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
#ifndef HB_SHADOW_CASTER
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

HBPlantDepthVaryings HB_PlantDepthVertex(HBPlantDepthAttributes input)
{
    HBPlantDepthVaryings output = (HBPlantDepthVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#ifndef HB_SHADOW_CASTER
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

    float3 restWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 positionWS = HB_PlantPosition(restWS, HB_PlantShare(input.plantData), input.plantData.y);

#ifdef HB_SHADOW_CASTER
    output.positionCS = HB_ShadowCasterPosition(positionWS, TransformObjectToWorldNormal(input.normalOS));
#elif defined(HB_DEPTH_NORMALS) || defined(HB_PLANT_SCENE)
    output.positionCS = HB_PlantClipPosition(positionWS);
#else
    output.positionCS = HB_PlantClipPosition(_HB_ShelterPass > 0.5 ? restWS : positionWS);

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    if (_HB_ShelterPass > 0.5 && _HB_ShelterPass < HB_PLANT_OWN_SHELTER)
        output.positionCS = float4(0.0, 0.0, 0.0, 0.0);
    #endif
#endif

#ifdef HB_PLANT_UV
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif

#ifdef HB_PLANT_TANGENT
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
#elif defined(HB_DEPTH_NORMALS)
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    #ifndef HB_PLANT_BODY
    if (HB_PlantBaked(input.plantData))
        normalWS = lerp(normalWS, TransformObjectToWorldNormal(input.tangentOS.xyz), float(_CanopyShape));
    #endif

    output.normalWS = normalWS;
#endif

    return output;
}

void HB_PlantDepthClip(HBPlantDepthVaryings input)
{
#ifdef _ALPHATEST_ON
    HB_PlantAlpha(input.uv);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif
}

#if defined(HB_SHADOW_CASTER)

half4 HB_PlantShadowFragment(HBPlantDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    HB_PlantDepthClip(input);

    return 0;
}

#elif defined(HB_PLANT_SCENE)

half4 HB_PlantSceneFragment(HBPlantDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_PlantDepthClip(input);

#ifdef SCENEPICKINGPASS
    return unity_SelectionID;
#else
    return half4(_ObjectId, _PassValue, 1.0h, 1.0h);
#endif
}

#elif defined(HB_DEPTH_NORMALS)

void HB_PlantDepthNormalsFragment(
    HBPlantDepthVaryings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_PlantDepthClip(input);

#ifdef HB_PLANT_BODY
    half3 normalWS = IS_FRONT_VFACE(frontFace, input.normalWS, -input.normalWS);
#else
    half3 normalWS = input.normalWS;
#endif

#ifdef HB_PLANT_TANGENT
    normalWS = TransformTangentToWorld(HB_PlantNormalMap(input.uv),
                                       half3x3(input.tangentWS, input.bitangentWS, normalWS));
#endif

    outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HB_PlantDepthFragment(HBPlantDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HB_PlantDepthClip(input);

    return input.positionCS.z;
}

#endif

#endif
