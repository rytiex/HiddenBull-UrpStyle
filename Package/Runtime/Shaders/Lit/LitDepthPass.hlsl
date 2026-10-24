#ifndef HB_LIT_DEPTH_PASS_INCLUDED
#define HB_LIT_DEPTH_PASS_INCLUDED

#include "Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitSurface.hlsl"

#if defined(HB_DEPTH_NORMALS)
    #define HB_VERTEX_NORMAL
    #if defined(_ALPHATEST_ON) || defined(_NORMALMAP)
        #define HB_VERTEX_UV
        #ifdef _HB_TRIPLANAR
            #define HB_VERTEX_POSITION
        #endif
    #endif
    #if defined(_NORMALMAP) && !defined(_HB_TRIPLANAR)
        #define HB_VERTEX_TANGENT
    #endif
#elif defined(_ALPHATEST_ON)
    #define HB_VERTEX_UV
    #ifdef _HB_TRIPLANAR
        #define HB_VERTEX_POSITION
        #define HB_VERTEX_NORMAL
    #endif
#endif

#if defined(HB_VERTEX_POSITION) && defined(HB_VERTEX_NORMAL) && defined(_HB_BRUSH_ANCHOR)
    #define HB_VERTEX_ANCHOR
#endif

#ifndef HB_SHADOW_CASTER
    #define HB_VERTEX_STEREO
#endif

#ifdef HB_SHADOW_CASTER
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;
#endif

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct HBDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
#ifdef HB_VERTEX_TANGENT
    float4 tangentOS  : TANGENT;
#endif
#ifdef HB_VERTEX_UV
    float2 texcoord   : TEXCOORD0;
#endif
#ifdef HB_VERTEX_ANCHOR
    float3 anchorOS   : TEXCOORD3;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBDepthVaryings
{
#ifdef HB_VERTEX_UV
    float2 uv          : TEXCOORD0;
#endif
#ifdef HB_VERTEX_POSITION
    float3 positionWS  : TEXCOORD1;
#endif
#ifdef HB_VERTEX_NORMAL
    half3 normalWS     : TEXCOORD2;
#endif
#ifdef HB_VERTEX_TANGENT
    half3 tangentWS    : TEXCOORD3;
    half3 bitangentWS  : TEXCOORD4;
#endif
#ifdef HB_VERTEX_ANCHOR
    float3 anchorOS    : TEXCOORD5;
#endif
    float4 positionCS  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
#ifdef HB_VERTEX_STEREO
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

HBDepthVaryings HB_DepthVertex(HBDepthAttributes input)
{
    HBDepthVaryings output = (HBDepthVaryings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
#ifdef HB_VERTEX_STEREO
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

#ifdef HB_SHADOW_CASTER
    float3 biasNormalWS = TransformObjectToWorldNormal(input.normalOS);

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    output.positionCS = ApplyShadowClamping(
        TransformWorldToHClip(ApplyShadowBias(positionWS, biasNormalWS, lightDirectionWS)));
#else
    output.positionCS = TransformWorldToHClip(positionWS);
#endif

#ifdef HB_VERTEX_UV
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif

#ifdef HB_VERTEX_POSITION
    output.positionWS = positionWS;
#endif

#ifdef HB_VERTEX_TANGENT
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
#elif defined(HB_VERTEX_NORMAL)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#ifdef HB_VERTEX_ANCHOR
    output.anchorOS = input.anchorOS;
#endif

    return output;
}

HBSurfaceInput HB_DepthSurface(HBDepthVaryings input, bool frontFace)
{
    float2 uv = float2(0.0, 0.0);
    float3 positionWS = float3(0.0, 0.0, 0.0);
    half3 normalWS = half3(0.0h, 0.0h, 1.0h);
    half3 tangentWS = half3(1.0h, 0.0h, 0.0h);
    half3 bitangentWS = half3(0.0h, 1.0h, 0.0h);
    float3 anchorOS = float3(0.0, 0.0, 0.0);

#ifdef HB_VERTEX_UV
    uv = input.uv;
#endif
#ifdef HB_VERTEX_POSITION
    positionWS = input.positionWS;
#endif
#ifdef HB_VERTEX_NORMAL
    normalWS = input.normalWS;
#endif
#ifdef HB_VERTEX_TANGENT
    tangentWS = input.tangentWS;
    bitangentWS = input.bitangentWS;
#endif
#ifdef HB_VERTEX_ANCHOR
    anchorOS = input.anchorOS;
#endif

    return HB_SurfaceInput(uv, positionWS, normalWS, tangentWS, bitangentWS, anchorOS, frontFace);
}

#if defined(HB_SHADOW_CASTER)

half4 HB_ShadowFragment(HBDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(HB_DepthSurface(input, true));
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return 0;
}

#elif defined(HB_DEPTH_NORMALS)

void HB_DepthNormalsFragment(
    HBDepthVaryings input
    , FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    HBSurfaceInput surface = HB_DepthSurface(input, IS_FRONT_VFACE(frontFace, true, false));

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(surface);
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    outNormalWS = half4(HB_ResolveNormal(surface, surface.normalWS * surface.facing), 0.0h);

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#else

half HB_DepthFragment(HBDepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#ifdef _ALPHATEST_ON
    HB_ResolveAlpha(HB_DepthSurface(input, true));
#endif

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    return input.positionCS.z;
}

#endif

#endif
